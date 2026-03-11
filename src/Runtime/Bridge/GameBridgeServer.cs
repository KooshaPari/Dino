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
                    pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    _currentPipe = pipe;
                    WriteDebug("[GameBridgeServer] Waiting for connection...");
                    pipe.WaitForConnection();
                    WriteDebug("[GameBridgeServer] Client connected.");

                    using (StreamReader reader = new StreamReader(pipe, Encoding.UTF8, false, 4096, leaveOpen: true))
                    using (StreamWriter writer = new StreamWriter(pipe, Encoding.UTF8, 4096, leaveOpen: true))
                    {
                        writer.AutoFlush = true;

                        while (_running && pipe.IsConnected)
                        {
                            string? line = null;
                            try
                            {
                                line = reader.ReadLine();
                            }
                            catch (IOException)
                            {
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
                                writer.WriteLine(response);
                            }
                            catch (IOException)
                            {
                                break;
                            }
                            catch (ObjectDisposedException)
                            {
                                break;
                            }
                        }
                    }

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
            switch (method)
            {
                case "game.ping":
                    return HandlePing();
                case "game.status":
                    return HandleStatus();
                case "game.getCatalog":
                    return HandleGetCatalog();
                case "game.getComponentMap":
                    return HandleGetComponentMap(parameters);
                case "game.getStat":
                    return HandleGetStat(parameters);
                case "game.applyOverride":
                    return HandleApplyOverride(parameters);
                case "game.queryEntities":
                    return HandleQueryEntities(parameters);
                case "game.reloadPacks":
                    return HandleReloadPacks(parameters);
                case "game.getResources":
                    return HandleGetResources();
                case "game.screenshot":
                    return HandleScreenshot(parameters);
                case "game.dumpState":
                    return HandleDumpState(parameters);
                case "game.verifyMod":
                    return HandleVerifyMod(parameters);
                case "game.waitForWorld":
                    return HandleWaitForWorld(parameters);
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

            // Entity count requires main thread access
            if (_platform.IsWorldReady)
            {
                try
                {
                    int entityCount = MainThreadDispatcher.RunOnMainThread(() =>
                    {
                        World? world = World.DefaultGameObjectInjectionWorld;
                        if (world == null || !world.IsCreated) return 0;
                        NativeArray<Entity> entities = world.EntityManager.GetAllEntities(Allocator.Temp);
                        int count = entities.Length;
                        entities.Dispose();
                        return count;
                    }).Result;
                    status.EntityCount = entityCount;

                    string worldName = MainThreadDispatcher.RunOnMainThread(() =>
                    {
                        World? world = World.DefaultGameObjectInjectionWorld;
                        return world?.Name ?? "";
                    }).Result;
                    status.WorldName = worldName;
                }
                catch (Exception ex)
                {
                    WriteDebug($"[GameBridgeServer] Error reading entity count: {ex.Message}");
                }
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
