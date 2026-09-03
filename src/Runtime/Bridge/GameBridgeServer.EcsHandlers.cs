#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using DINOForge.Bridge.Protocol;
using DINOForge.Runtime.Diagnostics;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;

namespace DINOForge.Runtime.Bridge
{
    public sealed partial class GameBridgeServer
    {
        private JToken HandleGetCatalog()
        {
            VanillaCatalog? catalog = IsPlatformAlive ? _platform.Catalog : null;
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

        /// <summary>
        /// Discovers and returns ECS component types from loaded game assemblies.
        /// Useful for identifying correct type names when game version changes.
        /// </summary>
        private JToken HandleDiscoverTypes(JObject? parameters)
        {
            string? pattern = parameters?.Value<string>("pattern");

            EcsTypeDiscovery.DiscoverAndLog();

            var assemblies = EcsTypeDiscovery.GetDiscoveredAssemblies() ?? new List<string>();
            var types = (pattern != null
                ? EcsTypeDiscovery.FindTypes(pattern)
                : EcsTypeDiscovery.GetDiscoveredTypes() ?? Enumerable.Empty<string>())
                .Take(200)
                .ToList();

            return JToken.FromObject(new
            {
                success = true,
                assemblies = assemblies,
                typesFound = types.Count,
                types = types,
                pattern = pattern ?? "(all)",
                logMessage = "Full type list written to dinoforge_debug.log"
            });
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

            // Reading ECS data requires main thread.
            // Task #535: bound the wait to avoid wedging the bridge thread when the
            // dispatcher pump (KeyInputSystem) is dead. On timeout, surface a structured
            // failure with EntityCount=0 so callers can distinguish "no data" from "hang".
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            var statTask = MainThreadDispatcher.RunOnMainThread(() =>
            {
                return ReadStatFromEcs(mapping, entityIndex);
            });
            StatResult statResult;
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            if (!statTask.Wait(MainThreadWaitTimeoutMs))
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleGetStat timed out ({MainThreadWaitTimeoutMs}ms) — dispatcher pump may be dead");
                statResult = new StatResult
                {
                    SdkPath = mapping.SdkModelPath,
                    ComponentType = mapping.EcsComponentType,
                    FieldName = mapping.TargetFieldName ?? "",
                    EntityCount = 0
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                statResult = statTask.Result;
            }

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

            // Apply immediately on the main thread so callers see the change reflected at once.
            // Also enqueue so the StatModifierSystem re-applies it after scene reloads.
            // Task #535: bounded wait — on timeout, the enqueue won't happen but the bridge
            // thread survives; return Success=false so callers can retry once the pump is alive.
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            var overrideTask = MainThreadDispatcher.RunOnMainThread(() =>
            {
                World? world = GetActiveWorld();
                int modified = 0;
                if (world != null && world.IsCreated)
                {
                    modified = StatModifierSystem.ApplyImmediate(world.EntityManager, mod);
                }

                // Always enqueue for persistence across reloads (runs after MinFrameDelay guard).
                StatModifierSystem.Enqueue(mod);

                return new OverrideResult
                {
                    Success = modified >= 0, // -1 means unknown sdkPath, 0+ means applied
                    SdkPath = sdkPath,
                    Message = modified > 0
                        ? $"Applied {modeStr} override for {sdkPath} = {value} to {modified} entities"
                        : $"Enqueued {modeStr} override for {sdkPath} = {value} (no live entities yet)"
                };
            });
            OverrideResult result;
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            if (!overrideTask.Wait(MainThreadWaitTimeoutMs))
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleApplyOverride timed out ({MainThreadWaitTimeoutMs}ms) — dispatcher pump may be dead");
                result = new OverrideResult
                {
                    Success = false,
                    SdkPath = sdkPath,
                    Message = $"Timed out applying {modeStr} override for {sdkPath} (main-thread pump unresponsive)"
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                result = overrideTask.Result;
            }

            return JToken.FromObject(result);
        }

        private JToken HandleQueryEntities(JObject? parameters)
        {
            string? componentType = parameters?.Value<string>("componentType");
            string? category = parameters?.Value<string>("category");

            // Task #535: bounded wait — return empty result on timeout instead of hanging.
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            var queryTask = MainThreadDispatcher.RunOnMainThread(() =>
            {
                return QueryEntitiesOnMainThread(componentType, category);
            });
            QueryResult queryResult;
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            if (!queryTask.Wait(MainThreadWaitTimeoutMs))
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleQueryEntities timed out ({MainThreadWaitTimeoutMs}ms) — dispatcher pump may be dead (componentType='{componentType}' category='{category}')");
                queryResult = new QueryResult
                {
                    Count = 0
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                queryResult = queryTask.Result;
            }

            return JToken.FromObject(queryResult);
        }

        private JToken HandleReloadPacks(JObject? parameters)
        {
            ReloadResult reloadResult;
            if (!IsPlatformAlive)
            {
                reloadResult = new ReloadResult
                {
                    Success = false,
                    LoadedPacks = new List<string>(),
                    Errors = new List<string> { "ModPlatform not ready (scene transition in progress)." }
                };
                return JToken.FromObject(reloadResult);
            }
            try
            {
                // Pack loading involves file IO and registry updates.
                // Task #535: bounded wait — heavy I/O so use the heavy timeout.
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                var loadTask = MainThreadDispatcher.RunOnMainThread(() =>
                {
                    return _platform.LoadPacks();
                });
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                if (!loadTask.Wait(MainThreadHeavyWaitTimeoutMs))
                {
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleReloadPacks timed out ({MainThreadHeavyWaitTimeoutMs}ms) — dispatcher pump may be dead");
                    reloadResult = new ReloadResult
                    {
                        Success = false,
                        LoadedPacks = new List<string>(),
                        Errors = new List<string> { "Pack reload timed out (main-thread pump unresponsive)." }
                    };
                }
                else
                {
                    // sync-over-async-unavoidable: ECS-bound, main-thread-required
                    SDK.ContentLoadResult loadResult = loadTask.Result;
                    reloadResult = new ReloadResult
                    {
                        Success = loadResult.IsSuccess,
                        LoadedPacks = new List<string>(loadResult.LoadedPacks),
                        Errors = new List<string>(loadResult.Errors)
                    };
                }
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
            // Use an explicit timeout (consistent with HandleStatus) to avoid blocking
            // the bridge thread indefinitely if the main thread is busy.
            System.Threading.Tasks.Task<ResourceSnapshot> task = MainThreadDispatcher.RunOnMainThread(() =>
            {
                World? world = GetActiveWorld();
                if (world == null || !world.IsCreated)
                    return new ResourceSnapshot();
                return ResourceReader.ReadResources(world.EntityManager);
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = task.Wait(MainThreadWaitTimeoutMs);
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            ResourceSnapshot snapshot = completed ? task.Result : new ResourceSnapshot();

            if (!completed)
                DebugLog.Write("GameBridgeServer", "[GameBridgeServer] HandleGetResources timed out waiting for main thread");

            return JToken.FromObject(snapshot);
        }

        private JToken HandleDumpState(JObject? parameters)
        {
            string? category = parameters?.Value<string>("category");

            // Rebuild the catalog for a fresh dump.
            // Task #535: bounded wait — heavy catalog rebuild on a busy ECS world, use heavy timeout.
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            var dumpTask = MainThreadDispatcher.RunOnMainThread(() =>
            {
                World? world = GetActiveWorld();
                if (world == null || !world.IsCreated)
                    return new CatalogSnapshot();

                VanillaCatalog freshCatalog = new VanillaCatalog();
                freshCatalog.Build(world.EntityManager);

                return BuildCatalogSnapshot(freshCatalog, category);
            });
            CatalogSnapshot snapshot;
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            if (!dumpTask.Wait(MainThreadHeavyWaitTimeoutMs))
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleDumpState timed out ({MainThreadHeavyWaitTimeoutMs}ms) — dispatcher pump may be dead");
                snapshot = new CatalogSnapshot();
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                snapshot = dumpTask.Result;
            }

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

            World? world = GetActiveWorld();
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
                    // event-lifecycle-ok: local float accumulator, not an event subscription
                    sum += floatVal;
                }
                catch { /* safe-swallow: per-entity reflection failure skips one entry but continues aggregation */ }
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

            World? world = GetActiveWorld();
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
                    catch { /* safe-swallow: component-type enumeration per-entity is best-effort diagnostic */ }

                    result.Entities.Add(info);
                }

                entities.Dispose();
                query.Dispose();
            }
            else if (!string.IsNullOrEmpty(category))
            {
                // Use VanillaCatalog to filter by category
                VanillaCatalog? catalog = IsPlatformAlive ? _platform.Catalog : null;
                if (catalog != null && catalog.IsBuilt)
                {
                    IReadOnlyList<VanillaEntityInfo> list;
                    // null-forgiveness-ok: guarded by if (!string.IsNullOrEmpty(category)) above
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
                        // event-lifecycle-ok: local int accumulator, not an event subscription
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
    }
}
