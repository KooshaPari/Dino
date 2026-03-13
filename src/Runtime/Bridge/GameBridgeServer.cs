#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Reflection;
using System.Text;
using System.Threading;
using DINOForge.Bridge.Protocol;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// Named pipe server implementing JSON-RPC 2.0 over NDJSON for IPC communication.
    /// Runs on a background thread and dispatches Unity-thread-required operations
    /// through <see cref="MainThreadDispatcher"/>.
    /// </summary>
    public sealed class GameBridgeServer : IDisposable
    {
        /// <summary>The well-known pipe name used by the DINOForge bridge.</summary>
        public const string PipeName = "dinoforge-game-bridge";

        private readonly ModPlatform _platform;
        private readonly DateTime _startTime;
        private Thread? _serverThread;
        private volatile bool _running;
        private NamedPipeServerStream? _currentPipe;

        /// <summary>
        /// Creates a new game bridge server.
        /// </summary>
        /// <param name="platform">The ModPlatform instance for accessing subsystems.</param>
        public GameBridgeServer(ModPlatform platform)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _startTime = DateTime.UtcNow;
        }

        /// <summary>
        /// Starts the named pipe server on a background thread.
        /// </summary>
        public void Start()
        {
            if (_running) return;
            _running = true;

            _serverThread = new Thread(ServerLoop)
            {
                Name = "DINOForge-Bridge-Server",
                IsBackground = true
            };
            _serverThread.Start();

            WriteDebug("[GameBridgeServer] Started on pipe: " + PipeName);
        }

        /// <summary>
        /// Stops the server and releases all resources.
        /// </summary>
        public void Stop()
        {
            _running = false;

            try
            {
                _currentPipe?.Dispose();
            }
            catch { }

            _currentPipe = null;
            WriteDebug("[GameBridgeServer] Stopped.");
        }

        /// <summary>
        /// Disposes the server, stopping it if running.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Main server loop: accepts pipe connections and processes NDJSON messages.
        /// Reconnects automatically after each client disconnects.
        /// </summary>
        private void ServerLoop()
        {
            while (_running)
            {
                NamedPipeServerStream? pipe = null;
                try
                {
                    // Use None (synchronous mode) — this server runs on a dedicated background
                    // thread so async pipe mode is not needed and causes ReadLine() to block
                    // indefinitely on Windows when no data is available.
                    pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None);

                    _currentPipe = pipe;
                    WriteDebug("[GameBridgeServer] Waiting for connection...");
                    pipe.WaitForConnection();
                    WriteDebug("[GameBridgeServer] Client connected.");

                    WriteDebug("[GameBridgeServer] Setting up line reader");
                    // Read lines manually byte-by-byte to avoid StreamReader buffering issues
                    // on Mono with synchronous named pipes.
                    while (_running && pipe.IsConnected)
                    {
                        string? line = null;
                        try
                        {
                            WriteDebug("[GameBridgeServer] Reading line...");
                            line = ReadLineFromPipe(pipe);
                            WriteDebug($"[GameBridgeServer] Got line: {(line == null ? "null" : $"len={line.Length}")}");
                        }
                        catch (IOException ex)
                        {
                            WriteDebug($"[GameBridgeServer] Read IOException: {ex.Message}");
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }

                        if (line == null) break;
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        string response = ProcessMessage(line);

                        try
                        {
                            byte[] responseBytes = Encoding.UTF8.GetBytes(response + "\n");
                            pipe.Write(responseBytes, 0, responseBytes.Length);
                            pipe.Flush();
                        }
                        catch (IOException ex)
                        {
                            WriteDebug($"[GameBridgeServer] Write IOException: {ex.Message}");
                            break;
                        }
                        catch (ObjectDisposedException)
                        {
                            break;
                        }
                    }
                    WriteDebug("[GameBridgeServer] Exited read loop");

                    WriteDebug("[GameBridgeServer] Client disconnected.");
                }
                catch (ObjectDisposedException)
                {
                    // Server is shutting down
                }
                catch (Exception ex)
                {
                    WriteDebug($"[GameBridgeServer] Error in server loop: {ex.Message}");
                }
                finally
                {
                    try { pipe?.Dispose(); }
                    catch { }
                    _currentPipe = null;
                }

                // Brief pause before re-listening to avoid tight loop on errors
                if (_running)
                {
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>
        /// Parses a single NDJSON line as a JSON-RPC request, dispatches to the
        /// appropriate handler, and returns the serialized response.
        /// </summary>
        private string ProcessMessage(string json)
        {
            JsonRpcRequest? request;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(json);
            }
            catch (Exception ex)
            {
                return SerializeError(null, -32700, "Parse error: " + ex.Message);
            }

            if (request == null || string.IsNullOrEmpty(request.Method))
            {
                return SerializeError(request?.Id, -32600, "Invalid request");
            }

            try
            {
                JToken result = DispatchMethod(request.Method, request.Params);
                return SerializeSuccess(request.Id, result);
            }
            catch (Exception ex)
            {
                WriteDebug($"[GameBridgeServer] Handler error for '{request.Method}': {ex}");
                return SerializeError(request.Id, -32603, "Internal error: " + ex.Message);
            }
        }

        /// <summary>
        /// Routes a method name to the appropriate handler and returns the result as a JToken.
        /// </summary>
        private JToken DispatchMethod(string method, JObject? parameters)
        {
            // Normalize: accept both "game.status" and "status" formats
            string m = method.StartsWith("game.") ? method.Substring(5) : method;
            switch (m)
            {
                case "ping":
                    return HandlePing();
                case "status":
                    return HandleStatus();
                case "getCatalog":
                    return HandleGetCatalog();
                case "getComponentMap":
                    return HandleGetComponentMap(parameters);
                case "getStat":
                    return HandleGetStat(parameters);
                case "applyOverride":
                    return HandleApplyOverride(parameters);
                case "queryEntities":
                    return HandleQueryEntities(parameters);
                case "reloadPacks":
                    return HandleReloadPacks(parameters);
                case "getResources":
                    return HandleGetResources();
                case "screenshot":
                    return HandleScreenshot(parameters);
                case "dumpState":
                    return HandleDumpState(parameters);
                case "verifyMod":
                    return HandleVerifyMod(parameters);
                case "waitForWorld":
                    return HandleWaitForWorld(parameters);
                case "loadScene":
                    return HandleLoadScene(parameters);
                case "startGame":
                    return HandleStartGame(parameters);
                case "loadSave":
                    return HandleLoadSave(parameters);
                case "listSaves":
                    return HandleListSaves();
                case "pressKey":
                    return HandlePressKey(parameters);
                case "dismissLoadScreen":
                    return HandleDismissLoadScreen();
                default:
                    throw new InvalidOperationException($"Method not found: {method}");
            }
        }

        // ──────────────────────────────────────────────
        //  Handler Implementations
        // ──────────────────────────────────────────────

        private JToken HandlePing()
        {
            PingResult result = new PingResult
            {
                Pong = true,
                Version = PluginInfo.VERSION,
                UptimeSeconds = (DateTime.UtcNow - _startTime).TotalSeconds
            };
            return JToken.FromObject(result);
        }

        private JToken HandleStatus()
        {
            // Status can be partially read from background thread (simple field reads)
            GameStatus status = new GameStatus
            {
                Running = true,
                WorldReady = _platform.IsWorldReady,
                ModPlatformReady = _platform.IsInitialized,
                Version = PluginInfo.VERSION,
                LoadedPacks = new List<string>()
            };

            // Entity count + world name require main thread — use timeout to avoid deadlock.
            // Both tasks are enqueued together so they can both be drained in consecutive frames.
            WriteDebug("[GameBridgeServer] HandleStatus: enqueuing main-thread tasks");
            try
            {
                System.Threading.Tasks.Task<int> entityTask = MainThreadDispatcher.RunOnMainThread(() =>
                {
                    World? world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated) return 0;
                    // Avoid scanning all 45K+ entities — just get the count cheaply
                    EntityQuery all = world.EntityManager.CreateEntityQuery(new EntityQueryDesc
                    {
                        Options = EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabled
                    });
                    int count = all.CalculateEntityCount();
                    all.Dispose();
                    return count;
                });

                System.Threading.Tasks.Task<string> nameTask = MainThreadDispatcher.RunOnMainThread(() =>
                {
                    World? world = World.DefaultGameObjectInjectionWorld;
                    return world?.Name ?? "";
                });

                WriteDebug("[GameBridgeServer] HandleStatus: waiting for entity count (3s)");
                bool entityDone = entityTask.Wait(3000);
                WriteDebug($"[GameBridgeServer] HandleStatus: entityDone={entityDone}");
                bool nameDone = nameTask.Wait(500);

                status.EntityCount = entityDone ? entityTask.Result : -1;
                status.WorldName = nameDone ? nameTask.Result : "";
                WriteDebug($"[GameBridgeServer] HandleStatus: EntityCount={status.EntityCount} WorldName={status.WorldName}");
            }
            catch (Exception ex)
            {
                WriteDebug($"[GameBridgeServer] Error in HandleStatus: {ex.Message}");
                status.EntityCount = -1;
            }

            // Populate loaded pack names from platform (background-thread safe)
            if (_platform.IsInitialized)
            {
                try
                {
                    System.Collections.Generic.IReadOnlyList<string>? packs = _platform.GetLoadedPackIds();
                    if (packs != null)
                    {
                        foreach (string id in packs)
                            status.LoadedPacks.Add(id);
                    }
                }
                catch { }
            }

            return JToken.FromObject(status);
        }

        private JToken HandleGetCatalog()
        {
            VanillaCatalog? catalog = _platform.Catalog;
            CatalogSnapshot snapshot = new CatalogSnapshot();

            if (catalog == null || !catalog.IsBuilt)
                return JToken.FromObject(snapshot);

            foreach (VanillaEntityInfo info in catalog.Units)
            {
                snapshot.Units.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                {
                    InferredId = info.InferredId,
                    ComponentCount = info.ComponentTypes.Length,
                    EntityCount = info.EntityCount,
                    Category = info.Category
                });
            }

            foreach (VanillaEntityInfo info in catalog.Buildings)
            {
                snapshot.Buildings.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                {
                    InferredId = info.InferredId,
                    ComponentCount = info.ComponentTypes.Length,
                    EntityCount = info.EntityCount,
                    Category = info.Category
                });
            }

            foreach (VanillaEntityInfo info in catalog.Projectiles)
            {
                snapshot.Projectiles.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                {
                    InferredId = info.InferredId,
                    ComponentCount = info.ComponentTypes.Length,
                    EntityCount = info.EntityCount,
                    Category = info.Category
                });
            }

            foreach (VanillaEntityInfo info in catalog.Other)
            {
                snapshot.Other.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                {
                    InferredId = info.InferredId,
                    ComponentCount = info.ComponentTypes.Length,
                    EntityCount = info.EntityCount,
                    Category = info.Category
                });
            }

            return JToken.FromObject(snapshot);
        }

        private JToken HandleGetComponentMap(JObject? parameters)
        {
            string? sdkPath = parameters?.Value<string>("sdkPath");

            ComponentMapResult result = new ComponentMapResult();

            if (sdkPath != null)
            {
                ComponentMapping? mapping = ComponentMap.Find(sdkPath);
                if (mapping != null)
                {
                    result.Mappings.Add(MappingToEntry(mapping));
                }
            }
            else
            {
                foreach (KeyValuePair<string, ComponentMapping> kvp in ComponentMap.All)
                {
                    result.Mappings.Add(MappingToEntry(kvp.Value));
                }
            }

            return JToken.FromObject(result);
        }

        private JToken HandleGetStat(JObject? parameters)
        {
            string sdkPath = parameters?.Value<string>("sdkPath") ?? "";
            int? entityIndex = parameters?.Value<int?>("entityIndex");

            if (string.IsNullOrEmpty(sdkPath))
                throw new ArgumentException("sdkPath is required");

            ComponentMapping? mapping = ComponentMap.Find(sdkPath);
            if (mapping == null)
                throw new ArgumentException($"Unknown SDK path: {sdkPath}");

            // Reading ECS data requires main thread
            StatResult statResult = MainThreadDispatcher.RunOnMainThread(() =>
            {
                return ReadStatFromEcs(mapping, entityIndex);
            }).Result;

            return JToken.FromObject(statResult);
        }

        private JToken HandleApplyOverride(JObject? parameters)
        {
            string sdkPath = parameters?.Value<string>("sdkPath") ?? "";
            float value = parameters?.Value<float>("value") ?? 0f;
            string modeStr = parameters?.Value<string>("mode") ?? "override";
            string? filter = parameters?.Value<string>("filter");

            if (string.IsNullOrEmpty(sdkPath))
                throw new ArgumentException("sdkPath is required");

            ModifierMode mode;
            switch (modeStr.ToLowerInvariant())
            {
                case "add":
                    mode = ModifierMode.Add;
                    break;
                case "multiply":
                    mode = ModifierMode.Multiply;
                    break;
                default:
                    mode = ModifierMode.Override;
                    break;
            }

            StatModification mod = new StatModification(sdkPath, value, mode, filter);
            StatModifierSystem.Enqueue(mod);

            OverrideResult result = new OverrideResult
            {
                Success = true,
                SdkPath = sdkPath,
                Message = $"Enqueued {modeStr} override for {sdkPath} = {value}"
            };

            return JToken.FromObject(result);
        }

        private JToken HandleQueryEntities(JObject? parameters)
        {
            string? componentType = parameters?.Value<string>("componentType");
            string? category = parameters?.Value<string>("category");

            QueryResult queryResult = MainThreadDispatcher.RunOnMainThread(() =>
            {
                return QueryEntitiesOnMainThread(componentType, category);
            }).Result;

            return JToken.FromObject(queryResult);
        }

        private JToken HandleReloadPacks(JObject? parameters)
        {
            ReloadResult reloadResult;
            try
            {
                // Pack loading involves file IO and registry updates
                SDK.ContentLoadResult loadResult = MainThreadDispatcher.RunOnMainThread(() =>
                {
                    return _platform.LoadPacks();
                }).Result;

                reloadResult = new ReloadResult
                {
                    Success = loadResult.IsSuccess,
                    LoadedPacks = new List<string>(loadResult.LoadedPacks),
                    Errors = new List<string>(loadResult.Errors)
                };
            }
            catch (Exception ex)
            {
                reloadResult = new ReloadResult
                {
                    Success = false,
                    Errors = new List<string> { ex.Message }
                };
            }

            return JToken.FromObject(reloadResult);
        }

        private JToken HandleGetResources()
        {
            ResourceSnapshot snapshot = MainThreadDispatcher.RunOnMainThread(() =>
            {
                World? world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return new ResourceSnapshot();
                return ResourceReader.ReadResources(world.EntityManager);
            }).Result;

            return JToken.FromObject(snapshot);
        }

        private JToken HandleLoadScene(JObject? parameters)
        {
            string sceneName = parameters?.Value<string>("scene") ?? "level0";
            int buildIndex = parameters?.Value<int>("buildIndex") ?? -1;

            // If scene is purely numeric, treat as build index
            if (buildIndex < 0 && int.TryParse(sceneName, out int parsed))
                buildIndex = parsed;

            var loadResult = MainThreadDispatcher.RunOnMainThread(() =>
            {
                int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
                WriteDebug($"[GameBridgeServer] LoadScene: buildIndex={buildIndex} sceneName={sceneName} totalScenes={count}");
                try
                {
                    if (buildIndex >= 0)
                        UnityEngine.SceneManagement.SceneManager.LoadScene(buildIndex);
                    else
                        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
                    return new { success = true, sceneCount = count };
                }
                catch (Exception ex)
                {
                    WriteDebug($"[GameBridgeServer] LoadScene failed: {ex.Message}");
                    return new { success = false, sceneCount = count };
                }
            });

            bool timedOut = !loadResult.Wait(5000);
            bool success = !timedOut && loadResult.Result.success;
            int sceneCount = timedOut ? -1 : loadResult.Result.sceneCount;

            return JToken.FromObject(new { success, scene = sceneName, buildIndex, sceneCount });
        }

        private JToken HandleScreenshot(JObject? parameters)
        {
            string path = parameters?.Value<string>("path") ?? "";
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(
                    BepInEx.Paths.BepInExRootPath,
                    "screenshots",
                    $"dinoforge_{DateTime.Now:yyyyMMdd_HHmmss}.png");
            }

            ScreenshotResult ssResult = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    string? dir = Path.GetDirectoryName(path);
                    if (dir != null && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    UnityEngine.ScreenCapture.CaptureScreenshot(path);

                    return new ScreenshotResult
                    {
                        Success = true,
                        Path = path,
                        Width = Screen.width,
                        Height = Screen.height
                    };
                }
                catch (Exception)
                {
                    return new ScreenshotResult
                    {
                        Success = false,
                        Path = path
                    };
                }
            }).Result;

            return JToken.FromObject(ssResult);
        }

        private JToken HandleDumpState(JObject? parameters)
        {
            string? category = parameters?.Value<string>("category");

            // Rebuild the catalog for a fresh dump
            CatalogSnapshot snapshot = MainThreadDispatcher.RunOnMainThread(() =>
            {
                World? world = World.DefaultGameObjectInjectionWorld;
                if (world == null || !world.IsCreated)
                    return new CatalogSnapshot();

                VanillaCatalog freshCatalog = new VanillaCatalog();
                freshCatalog.Build(world.EntityManager);

                return BuildCatalogSnapshot(freshCatalog, category);
            }).Result;

            return JToken.FromObject(snapshot);
        }

        private JToken HandleVerifyMod(JObject? parameters)
        {
            string packPath = parameters?.Value<string>("packPath") ?? "";
            VerifyResult verifyResult = new VerifyResult();

            if (string.IsNullOrEmpty(packPath))
            {
                verifyResult.Errors.Add("packPath is required");
                return JToken.FromObject(verifyResult);
            }

            try
            {
                SDK.PackLoader loader = new SDK.PackLoader();
                string manifestPath = packPath;
                if (Directory.Exists(packPath))
                {
                    manifestPath = Path.Combine(packPath, "pack.yaml");
                }

                if (!File.Exists(manifestPath))
                {
                    verifyResult.Errors.Add($"Manifest not found: {manifestPath}");
                    return JToken.FromObject(verifyResult);
                }

                SDK.PackManifest manifest = loader.LoadFromFile(manifestPath);
                verifyResult.PackId = manifest.Id;
                verifyResult.Loaded = true;

                // Report stat changes that would be applied
                verifyResult.StatChanges.Add($"Pack '{manifest.Id}' v{manifest.Version} verified successfully");
            }
            catch (Exception ex)
            {
                verifyResult.Errors.Add($"Verification failed: {ex.Message}");
            }

            return JToken.FromObject(verifyResult);
        }

        private JToken HandleWaitForWorld(JObject? parameters)
        {
            int timeoutMs = parameters?.Value<int?>("timeoutMs") ?? 30000;

            DateTime deadline = DateTime.UtcNow.AddMilliseconds(timeoutMs);
            while (DateTime.UtcNow < deadline && _running)
            {
                if (_platform.IsWorldReady)
                {
                    string worldName = "";
                    try
                    {
                        worldName = MainThreadDispatcher.RunOnMainThread(() =>
                        {
                            World? world = World.DefaultGameObjectInjectionWorld;
                            return world?.Name ?? "";
                        }).Result;
                    }
                    catch { }

                    WaitResult readyResult = new WaitResult
                    {
                        Ready = true,
                        WorldName = worldName
                    };
                    return JToken.FromObject(readyResult);
                }

                Thread.Sleep(200);
            }

            WaitResult timeoutResult = new WaitResult
            {
                Ready = false,
                WorldName = ""
            };
            return JToken.FromObject(timeoutResult);
        }

        // ──────────────────────────────────────────────
        //  Helper Methods
        // ──────────────────────────────────────────────

        /// <summary>
        /// Reads stat values from the ECS world for a given component mapping.
        /// Must be called on the main thread.
        /// </summary>
        private StatResult ReadStatFromEcs(ComponentMapping mapping, int? entityIndex)
        {
            StatResult result = new StatResult
            {
                SdkPath = mapping.SdkModelPath,
                ComponentType = mapping.EcsComponentType,
                FieldName = mapping.TargetFieldName ?? ""
            };

            Type? clrType = mapping.ResolvedType;
            if (clrType == null)
            {
                result.EntityCount = 0;
                return result;
            }

            World? world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return result;

            EntityManager em = world.EntityManager;
            ComponentType? ct = EntityQueries.ResolveComponentType(mapping.EcsComponentType);
            if (ct == null) return result;

            EntityQueryDesc desc = new EntityQueryDesc
            {
                All = new[] { ct.Value }
            };
            EntityQuery query = em.CreateEntityQuery(desc);
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            result.EntityCount = entities.Length;

            if (entities.Length == 0)
            {
                entities.Dispose();
                query.Dispose();
                return result;
            }

            MethodInfo? getMethod = typeof(EntityManager)
                .GetMethod("GetComponentData", new[] { typeof(Entity) });
            if (getMethod == null)
            {
                entities.Dispose();
                query.Dispose();
                return result;
            }

            MethodInfo genericGet = getMethod.MakeGenericMethod(clrType);
            string fieldName = mapping.TargetFieldName ?? "value";
            FieldInfo? field = clrType.GetField(fieldName,
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            if (field == null)
            {
                entities.Dispose();
                query.Dispose();
                return result;
            }

            result.Values = new List<float>();
            float sum = 0f;

            int start = entityIndex.HasValue ? entityIndex.Value : 0;
            int end = entityIndex.HasValue ? Math.Min(entityIndex.Value + 1, entities.Length) : entities.Length;

            for (int i = start; i < end; i++)
            {
                try
                {
                    object? data = genericGet.Invoke(em, new object[] { entities[i] });
                    if (data == null) continue;

                    object? rawValue = field.GetValue(data);
                    float floatVal = 0f;
                    if (rawValue is float f) floatVal = f;
                    else if (rawValue is int iv) floatVal = iv;

                    result.Values.Add(floatVal);
                    sum += floatVal;
                }
                catch { }
            }

            if (result.Values.Count > 0)
                result.Value = sum / result.Values.Count;

            entities.Dispose();
            query.Dispose();
            return result;
        }

        /// <summary>
        /// Queries entities on the main thread, optionally filtering by component type or category.
        /// </summary>
        private QueryResult QueryEntitiesOnMainThread(string? componentType, string? category)
        {
            QueryResult result = new QueryResult();

            World? world = World.DefaultGameObjectInjectionWorld;
            if (world == null || !world.IsCreated)
                return result;

            EntityManager em = world.EntityManager;

            if (!string.IsNullOrEmpty(componentType))
            {
                ComponentType? ct = EntityQueries.ResolveComponentType(componentType!);
                if (ct == null)
                {
                    result.Count = 0;
                    return result;
                }

                EntityQueryDesc desc = new EntityQueryDesc
                {
                    All = new[] { ct.Value }
                };
                EntityQuery query = em.CreateEntityQuery(desc);
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

                result.Count = entities.Length;

                // Return up to 100 entity summaries
                int limit = Math.Min(entities.Length, 100);
                for (int i = 0; i < limit; i++)
                {
                    EntityInfo info = new EntityInfo { Index = entities[i].Index };
                    try
                    {
                        NativeArray<ComponentType> types = em.GetComponentTypes(entities[i], Allocator.Temp);
                        for (int j = 0; j < types.Length; j++)
                        {
                            Type? managed = types[j].GetManagedType();
                            info.Components.Add(managed?.FullName ?? $"Unknown({types[j].TypeIndex})");
                        }
                        types.Dispose();
                    }
                    catch { }

                    result.Entities.Add(info);
                }

                entities.Dispose();
                query.Dispose();
            }
            else if (!string.IsNullOrEmpty(category))
            {
                // Use VanillaCatalog to filter by category
                VanillaCatalog? catalog = _platform.Catalog;
                if (catalog != null && catalog.IsBuilt)
                {
                    IReadOnlyList<VanillaEntityInfo> list;
                    switch (category!.ToLowerInvariant())
                    {
                        case "unit":
                            list = catalog.Units;
                            break;
                        case "building":
                            list = catalog.Buildings;
                            break;
                        case "projectile":
                            list = catalog.Projectiles;
                            break;
                        default:
                            list = catalog.Other;
                            break;
                    }

                    int totalCount = 0;
                    foreach (VanillaEntityInfo entry in list)
                    {
                        totalCount += entry.EntityCount;
                        EntityInfo info = new EntityInfo
                        {
                            Index = -1, // archetype-level, not individual entity
                            Components = new List<string>(entry.ComponentTypes)
                        };
                        result.Entities.Add(info);
                    }
                    result.Count = totalCount;
                }
            }
            else
            {
                // Return total entity count
                NativeArray<Entity> all = em.GetAllEntities(Allocator.Temp);
                result.Count = all.Length;
                all.Dispose();
            }

            return result;
        }

        /// <summary>
        /// Builds a CatalogSnapshot from a VanillaCatalog, optionally filtered by category.
        /// </summary>
        private static CatalogSnapshot BuildCatalogSnapshot(VanillaCatalog catalog, string? category)
        {
            CatalogSnapshot snapshot = new CatalogSnapshot();
            bool all = string.IsNullOrEmpty(category) ||
                        string.Equals(category, "all", StringComparison.OrdinalIgnoreCase);

            if (all || string.Equals(category, "unit", StringComparison.OrdinalIgnoreCase))
            {
                foreach (VanillaEntityInfo info in catalog.Units)
                {
                    snapshot.Units.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                    {
                        InferredId = info.InferredId,
                        ComponentCount = info.ComponentTypes.Length,
                        EntityCount = info.EntityCount,
                        Category = info.Category
                    });
                }
            }

            if (all || string.Equals(category, "building", StringComparison.OrdinalIgnoreCase))
            {
                foreach (VanillaEntityInfo info in catalog.Buildings)
                {
                    snapshot.Buildings.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                    {
                        InferredId = info.InferredId,
                        ComponentCount = info.ComponentTypes.Length,
                        EntityCount = info.EntityCount,
                        Category = info.Category
                    });
                }
            }

            if (all || string.Equals(category, "projectile", StringComparison.OrdinalIgnoreCase))
            {
                foreach (VanillaEntityInfo info in catalog.Projectiles)
                {
                    snapshot.Projectiles.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                    {
                        InferredId = info.InferredId,
                        ComponentCount = info.ComponentTypes.Length,
                        EntityCount = info.EntityCount,
                        Category = info.Category
                    });
                }
            }

            if (all || string.Equals(category, "other", StringComparison.OrdinalIgnoreCase))
            {
                foreach (VanillaEntityInfo info in catalog.Other)
                {
                    snapshot.Other.Add(new DINOForge.Bridge.Protocol.CatalogEntry
                    {
                        InferredId = info.InferredId,
                        ComponentCount = info.ComponentTypes.Length,
                        EntityCount = info.EntityCount,
                        Category = info.Category
                    });
                }
            }

            return snapshot;
        }

        /// <summary>
        /// Converts a ComponentMapping to a protocol ComponentMapEntry.
        /// </summary>
        private static ComponentMapEntry MappingToEntry(ComponentMapping mapping)
        {
            return new ComponentMapEntry
            {
                SdkPath = mapping.SdkModelPath,
                EcsType = mapping.EcsComponentType,
                FieldName = mapping.TargetFieldName ?? "",
                Resolved = mapping.ResolvedType != null,
                Description = mapping.Description ?? ""
            };
        }

        private JToken HandleStartGame(JObject? parameters)
        {
            string saveName = parameters?.Value<string>("saveName") ?? "";

            // Trigger the game's own world-loading system by creating the
            // BeginGameWorldLoadingSingleton ECS entity, which SceneLoadingSystem listens for.
            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    World? world = World.DefaultGameObjectInjectionWorld;
                    if (world == null || !world.IsCreated)
                        return new { success = false, message = "No ECS world" };

                    // Resolve BeginGameWorldLoadingSingleton type dynamically
                    Type? singletonType = null;
                    foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        singletonType = asm.GetType("Components.SingletonComponents.BeginGameWorldLoadingSingleton");
                        if (singletonType != null) break;
                    }

                    if (singletonType == null)
                        return new { success = false, message = "BeginGameWorldLoadingSingleton type not found" };

                    // Dump the singleton's fields for diagnostics
                    FieldInfo[] fields = singletonType.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    string fieldList = string.Join(", ", System.Array.ConvertAll(fields, f => $"{f.FieldType.Name} {f.Name}"));
                    WriteDebug($"[GameBridgeServer] BeginGameWorldLoadingSingleton fields: [{fieldList}]");

                    ComponentType ct = ComponentType.ReadWrite(singletonType);
                    Entity e = world.EntityManager.CreateEntity(ct);
                    WriteDebug($"[GameBridgeServer] Created BeginGameWorldLoadingSingleton entity {e.Index}");

                    // If the singleton has a NameToLoad field, try to set it via reflection
                    // (ECS components are structs so we use SetComponentData via reflection)
                    if (!string.IsNullOrEmpty(saveName))
                    {
                        FieldInfo? nameField = singletonType.GetField("NameToLoad",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        WriteDebug($"[GameBridgeServer] NameToLoad field: {(nameField == null ? "not found" : nameField.FieldType.Name)}");
                    }

                    return new { success = true, message = $"Created singleton entity {e.Index}, fields=[{fieldList}]" };
                }
                catch (Exception ex)
                {
                    WriteDebug($"[GameBridgeServer] HandleStartGame failed: {ex.Message}");
                    return new { success = false, message = ex.Message };
                }
            });

            bool completed = result.Wait(5000);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            return JToken.FromObject(result.Result);
        }

        private JToken HandleDismissLoadScreen()
        {
            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // DINO's loading screen uses UI.LoadingProgressBar which has a _startAction field
                    // (a UnityAction) that gets invoked when the player presses any key.
                    var allMBs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    string found = "";
                    foreach (var mb in allMBs)
                    {
                        if (mb == null) continue;
                        string tName = mb.GetType().Name;
                        found += $"[{tName}]";

                        // Target: UI.LoadingProgressBar
                        if (tName == "LoadingProgressBar")
                        {
                            // Try _startAction field (UnityAction)
                            FieldInfo? startField = mb.GetType().GetField("_startAction",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (startField != null)
                            {
                                object? action = startField.GetValue(mb);
                                if (action is UnityEngine.Events.UnityAction ua)
                                {
                                    ua.Invoke();
                                    return new { success = true, message = $"Invoked _startAction on LoadingProgressBar" };
                                }
                                // Try invoking as delegate
                                if (action is System.Delegate del)
                                {
                                    del.DynamicInvoke();
                                    return new { success = true, message = $"DynamicInvoked _startAction on LoadingProgressBar" };
                                }
                                WriteDebug($"[GameBridgeServer] _startAction type: {(action?.GetType().Name ?? "null")}");
                            }

                            // Fallback: call Update() to simulate time passing with anyKeyDown
                            // Actually try GetComponent on the progress GameObject
                            FieldInfo? progressField = mb.GetType().GetField("_progress",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (progressField != null)
                            {
                                UnityEngine.GameObject? progressGO = progressField.GetValue(mb) as UnityEngine.GameObject;
                                if (progressGO != null)
                                    progressGO.SetActive(false); // hide progress bar panel
                            }

                            // Try destroying the component to let the scene proceed
                            return new { success = false, message = $"LoadingProgressBar found but _startAction invoke failed. Action type: {startField?.GetValue(mb)?.GetType().Name ?? "null"}" };
                        }
                    }

                    return new { success = false, message = $"No dismiss handler found. MBs: {found}" };
                }
                catch (Exception ex)
                {
                    return new { success = false, message = ex.Message };
                }
            });

            bool completed = result.Wait(5000);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            return JToken.FromObject(result.Result);
        }

        private JToken HandlePressKey(JObject? parameters)
        {
            string keyName = parameters?.Value<string>("key") ?? "space";

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Use Unity's Input simulation — unfortunately legacy Input doesn't support injection
                    // But we can try to find the component that's waiting for input and trigger it
                    WriteDebug($"[GameBridgeServer] PressKey: {keyName}");

                    // Look for any component that has an Update loop listening for Input.anyKeyDown
                    // These are usually named things like: PressAnyKey, LoadingTip, LoadingScreen
                    var allMBs = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                    var candidates = new System.Text.StringBuilder();
                    foreach (var mb in allMBs)
                    {
                        if (mb == null || !mb.gameObject.activeInHierarchy) continue;
                        string n = mb.GetType().Name;
                        if (n.Length < 30 && !n.StartsWith("Unity") && !n.StartsWith("System"))
                            candidates.Append($"[{n}] ");
                    }
                    WriteDebug($"[GameBridgeServer] Active MBs: {candidates.ToString().Substring(0, Math.Min(500, candidates.Length))}");

                    return new { success = false, message = "Key press via bridge not implemented yet" };
                }
                catch (Exception ex)
                {
                    return new { success = false, message = ex.Message };
                }
            });

            bool completed = result.Wait(5000);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            return JToken.FromObject(result.Result);
        }

        private JToken HandleListSaves()
        {
            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Find save manager via reflection
                    Type? saveManagerType = null;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        foreach (string typeName in new[] {
                            "Systems.SaveLoadSystem", "Systems.GameWorldLoaderSystem",
                            "Systems.Save.SaveSystem", "Systems.SaveSystem",
                            "SaveManager", "SaveSystem"
                        })
                        {
                            saveManagerType = asm.GetType(typeName);
                            if (saveManagerType != null) break;
                        }
                        if (saveManagerType != null) break;
                    }

                    // Search for saves in DNO's actual paths
                    string persistPath = Application.persistentDataPath;
                    var saves = new List<string>();

                    // DINO saves: persistentDataPath/DNOPersistentData/<branch>/
                    string dnoDataDir = System.IO.Path.Combine(persistPath, "DNOPersistentData");
                    string saveDir = dnoDataDir;
                    if (System.IO.Directory.Exists(dnoDataDir))
                    {
                        foreach (string branchDir in System.IO.Directory.GetDirectories(dnoDataDir))
                        {
                            string branchName = System.IO.Path.GetFileName(branchDir);
                            foreach (var f in System.IO.Directory.GetFiles(branchDir, "*.dat"))
                                saves.Add($"{branchName}/{System.IO.Path.GetFileNameWithoutExtension(f)}");
                        }
                    }
                    else
                    {
                        // Fallback to standard Saves dir
                        saveDir = System.IO.Path.Combine(persistPath, "Saves");
                        if (System.IO.Directory.Exists(saveDir))
                        {
                            foreach (var f in System.IO.Directory.GetFiles(saveDir, "*.sav"))
                                saves.Add(System.IO.Path.GetFileNameWithoutExtension(f));
                            foreach (var f in System.IO.Directory.GetFiles(saveDir, "*.dat"))
                                saves.Add(System.IO.Path.GetFileNameWithoutExtension(f));
                        }
                    }

                    return new {
                        saveManagerType = saveManagerType?.FullName ?? "not found",
                        persistentDataPath = persistPath,
                        saveDir = saveDir,
                        saveDirExists = System.IO.Directory.Exists(saveDir),
                        saves = saves,
                        dataPath = Application.dataPath
                    };
                }
                catch (Exception ex)
                {
                    return new {
                        saveManagerType = "error",
                        persistentDataPath = "",
                        saveDir = "",
                        saveDirExists = false,
                        saves = new List<string>(),
                        dataPath = ex.Message
                    };
                }
            });

            bool completed = result.Wait(5000);
            if (!completed) return JToken.FromObject(new { error = "Timed out" });
            return JToken.FromObject(result.Result);
        }

        private JToken HandleLoadSave(JObject? parameters)
        {
            string saveName = parameters?.Value<string>("saveName") ?? "AutoSave_1";

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    WriteDebug($"[GameBridgeServer] HandleLoadSave: '{saveName}'");

                    // Strategy 0: Create a LoadRequest ECS entity — the game's SaveLoadSystem
                    // reads Components.RawComponents.LoadRequest singletons and triggers a load.
                    // Fields: NameToLoad (FixedString128Bytes), FromMenu (Boolean)
                    World? world = World.DefaultGameObjectInjectionWorld;
                    if (world != null && world.IsCreated)
                    {
                        Type? loadRequestType = null;
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            loadRequestType = asm.GetType("Components.RawComponents.LoadRequest");
                            if (loadRequestType != null) break;
                        }

                        if (loadRequestType != null)
                        {
                            WriteDebug($"[GameBridgeServer] Found LoadRequest type: {loadRequestType.FullName}");

                            // Create the component value
                            object loadRequest = System.Activator.CreateInstance(loadRequestType);

                            // Set NameToLoad — it's a Unity.Collections.FixedString128Bytes
                            FieldInfo? nameField = loadRequestType.GetField("NameToLoad",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            FieldInfo? fromMenuField = loadRequestType.GetField("FromMenu",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                            if (nameField != null)
                            {
                                // FixedString128Bytes can be set from a regular string via implicit conversion
                                // We need to box/unbox correctly
                                Type fsType = nameField.FieldType; // Unity.Collections.FixedString128Bytes
                                // Try to create FixedString128Bytes from string
                                try
                                {
                                    // FixedString128Bytes has implicit operator from string in Unity
                                    MethodInfo? op = fsType.GetMethod("op_Implicit",
                                        BindingFlags.Public | BindingFlags.Static,
                                        null, new[] { typeof(string) }, null);
                                    if (op != null)
                                    {
                                        object? fs = op.Invoke(null, new object[] { saveName });
                                        nameField.SetValue(loadRequest, fs);
                                        WriteDebug($"[GameBridgeServer] Set NameToLoad = '{saveName}' via op_Implicit");
                                    }
                                    else
                                    {
                                        // Try ctor with string
                                        System.Reflection.ConstructorInfo? ctor = fsType.GetConstructor(new[] { typeof(string) });
                                        if (ctor != null)
                                        {
                                            object? fs = ctor.Invoke(new object[] { saveName });
                                            nameField.SetValue(loadRequest, fs);
                                            WriteDebug($"[GameBridgeServer] Set NameToLoad via ctor");
                                        }
                                        else
                                        {
                                            WriteDebug($"[GameBridgeServer] No string ctor or op_Implicit for {fsType.Name}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    WriteDebug($"[GameBridgeServer] NameToLoad set failed: {ex.Message}");
                                }
                            }

                            if (fromMenuField != null)
                                fromMenuField.SetValue(loadRequest, true);

                            // Create entity and add LoadRequest component
                            try
                            {
                                ComponentType ct = ComponentType.ReadWrite(loadRequestType);
                                Entity e = world.EntityManager.CreateEntity(ct);

                                // Set the component data via reflection
                                MethodInfo? setComp = typeof(EntityManager).GetMethod("SetComponentData",
                                    BindingFlags.Public | BindingFlags.Instance);
                                if (setComp != null)
                                {
                                    MethodInfo genSet = setComp.MakeGenericMethod(loadRequestType);
                                    genSet.Invoke(world.EntityManager, new object[] { e, loadRequest });
                                    WriteDebug($"[GameBridgeServer] Created LoadRequest entity {e.Index} with NameToLoad='{saveName}'");
                                    return new { success = true, message = $"Created LoadRequest entity {e.Index} NameToLoad='{saveName}'", foundPath = "" };
                                }
                            }
                            catch (Exception ex)
                            {
                                WriteDebug($"[GameBridgeServer] LoadRequest entity creation failed: {ex.Message}");
                            }
                        }
                        else
                        {
                            WriteDebug($"[GameBridgeServer] LoadRequest type NOT found");
                        }
                    }

                    // Find the save file in DINO's DNOPersistentData structure
                    string persistPath = Application.persistentDataPath;
                    string dnoDataDir = System.IO.Path.Combine(persistPath, "DNOPersistentData");

                    string foundPath = "";
                    if (System.IO.Directory.Exists(dnoDataDir))
                    {
                        foreach (string branchDir in System.IO.Directory.GetDirectories(dnoDataDir))
                        {
                            foreach (string f in System.IO.Directory.GetFiles(branchDir, "*.dat"))
                            {
                                string fn = System.IO.Path.GetFileNameWithoutExtension(f).ToUpperInvariant();
                                string sn = saveName.ToUpperInvariant();
                                if (fn.Contains(sn) || sn.Contains(fn))
                                {
                                    foundPath = f;
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(foundPath)) break;
                        }
                    }

                    WriteDebug($"[GameBridgeServer] Save file found: '{foundPath}'");
                    WriteDebug($"[GameBridgeServer] PersistentDataPath: {persistPath}");

                    // Strategy 3: Find the game's native UI buttons via Unity's UI system
                    // Use Resources.FindObjectsOfTypeAll to find ALL button instances including inactive
                    var allButtons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
                    WriteDebug($"[GameBridgeServer] Found {allButtons.Length} buttons (Resources.FindObjectsOfTypeAll)");

                    // Also try FindObjectsOfType (scene-only)
                    var sceneButtons = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>();
                    WriteDebug($"[GameBridgeServer] Found {sceneButtons.Length} buttons (FindObjectsOfType scene-only)");

                    // Dump ALL GameObjects to find what the menu uses
                    if (allButtons.Length == 0 && sceneButtons.Length == 0)
                    {
                        // Search for any MonoBehaviour with "Click" or "Button" in name
                        var allMBs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                        var interesting = new System.Text.StringBuilder();
                        foreach (var mb in allMBs)
                        {
                            if (mb == null) continue;
                            string tName = mb.GetType().Name;
                            if (tName.Contains("Button") || tName.Contains("Click") || tName.Contains("Menu") || tName.Contains("Interactable"))
                                interesting.Append($"[{tName}:{mb.gameObject.name}] ");
                        }
                        WriteDebug($"[GameBridgeServer] Button-like MonoBehaviours: {interesting}");
                    }

                    var saveNameUpper = saveName.ToUpperInvariant();
                    UnityEngine.UI.Button? targetButton = null;
                    UnityEngine.UI.Button? continueButton = null;
                    UnityEngine.UI.Button? okButton = null;
                    var buttonSummary = new System.Text.StringBuilder();

                    foreach (var btn in allButtons)
                    {
                        if (btn == null) continue;
                        // Skip the DINOForge mods button only
                        if (btn.name == "DINOForge_ModsButton") continue;
                        // Skip inactive
                        if (!btn.gameObject.activeInHierarchy) continue;

                        var txt = btn.GetComponentInChildren<UnityEngine.UI.Text>();
                        var tmptxt = btn.GetComponentInChildren<TMPro.TMP_Text>();
                        string label = (txt?.text ?? tmptxt?.text ?? "").Trim();
                        string btnName = btn.name;
                        buttonSummary.Append($"[{btnName}:'{label}'] ");

                        string labelUpper = label.ToUpperInvariant();
                        string nameUpper = btnName.ToUpperInvariant();

                        if (labelUpper == "OK" && nameUpper == "BUTTON_INTERCEPTED")
                        {
                            // Only capture unnamed "Button" as OK — not named buttons like Continue
                            if (okButton == null) okButton = btn;
                        }
                        string nameBase = btnName.Replace("_intercepted", "").ToUpperInvariant();
                        if (nameBase == "CONTINUE" || labelUpper == "CONTINUE")
                        {
                            continueButton = btn;
                        }
                        if (!string.IsNullOrEmpty(saveNameUpper))
                        {
                            // Match save name against button label or name
                            if (labelUpper.Contains(saveNameUpper) || nameBase.Contains(saveNameUpper))
                            {
                                targetButton = btn;
                            }
                            // Special: if searching for CONTINUE, match the Continue button
                            if (saveNameUpper == "CONTINUE" && (nameBase == "CONTINUE" || labelUpper == "CONTINUE"))
                                targetButton = btn;
                            // Special: if searching for OK or CONFIRM, match the ok button
                            if ((saveNameUpper == "OK" || saveNameUpper == "CONFIRM") && labelUpper == "OK")
                                targetButton = btn;
                            // Match Load buttons: LOAD_1, LOAD buttons by date position
                            if (saveNameUpper.StartsWith("LOAD") && nameBase == "LOAD")
                            {
                                if (targetButton == null) targetButton = btn; // first Load button
                            }
                        }
                    }

                    WriteDebug($"[GameBridgeServer] Active buttons: {buttonSummary}");
                    WriteDebug($"[GameBridgeServer] okButton={okButton?.name ?? "null"} continueButton={continueButton?.name ?? "null"} targetButton={targetButton?.name ?? "null"}");

                    // Priority: explicit name match > CONTINUE > OK fallback
                    UnityEngine.UI.Button? toInvoke = targetButton ?? continueButton ?? okButton;
                    if (toInvoke != null)
                    {
                        WriteDebug($"[GameBridgeServer] Invoking button: {toInvoke.name}");
                        // Try ExecuteEvents for proper UI simulation, fall back to onClick.Invoke
                        try
                        {
                            UnityEngine.EventSystems.ExecuteEvents.Execute(
                                toInvoke.gameObject,
                                new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
                                UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        }
                        catch
                        {
                            toInvoke.onClick.Invoke();
                        }
                        return new { success = true, message = $"Invoked button '{toInvoke.name}' (label search: '{saveName}')", foundPath };
                    }

                    return new { success = false, message = $"No suitable button found for '{saveName}'. Active buttons: {buttonSummary}. Save path: '{foundPath}'", foundPath };
                }
                catch (Exception ex)
                {
                    WriteDebug($"[GameBridgeServer] HandleLoadSave failed: {ex.Message}");
                    return new { success = false, message = ex.Message, foundPath = "" };
                }
            });

            bool completed = result.Wait(10000);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            return JToken.FromObject(result.Result);
        }

        /// <summary>
        /// Reads a single UTF-8 line from the pipe byte-by-byte.
        /// Returns null if the pipe is closed. This avoids StreamReader buffering
        /// issues on Mono where a large buffer causes blocking on partial reads.
        /// </summary>
        private static string? ReadLineFromPipe(Stream pipe)
        {
            var sb = new System.Text.StringBuilder();
            int b;
            while ((b = pipe.ReadByte()) != -1)
            {
                char c = (char)b;
                if (c == '\n') return sb.ToString();
                if (c != '\r') sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>
        /// Serializes a successful JSON-RPC response.
        /// </summary>
        private static string SerializeSuccess(string? id, JToken result)
        {
            JsonRpcResponse response = new JsonRpcResponse
            {
                Id = id,
                Result = result
            };
            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        /// <summary>
        /// Serializes a JSON-RPC error response.
        /// </summary>
        private static string SerializeError(string? id, int code, string message)
        {
            JsonRpcResponse response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message
                }
            };
            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = Path.Combine(
                    BepInEx.Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now}] {msg}\n");
            }
            catch { }
        }
    }
}
