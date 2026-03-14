#nullable enable
using System;
using System.IO;
using System.Reflection;
using DINOForge.Bridge.Protocol;
using Unity.Collections;
using Unity.Entities;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// Reads current resource stockpile values from the ECS world.
    ///
    /// Root causes of the "resources reads 0" bug (fixed here):
    ///   1. Missing IncludePrefab — DINO stores even singleton-like resource entities
    ///      as ECS prefab entities. Without EntityQueryOptions.IncludePrefab, the query
    ///      returns 0 entities regardless of whether the component type resolved correctly.
    ///   2. Unverified component type names — "Components.RawComponents.CurrentFood" et al.
    ///      are best guesses. A fallback table of alternative names is tried if the primary
    ///      type does not resolve or yields no entities.
    ///   3. Unverified field names — the resource value field may be named differently
    ///      across DINO versions. A per-mapping fallback chain is tried in order.
    ///   4. GetComponentData reflection path — field traversal now logs every failed
    ///      field attempt so the debug log can identify the correct field name quickly.
    /// </summary>
    public static class ResourceReader
    {
        // ---------------------------------------------------------------------------
        // Alternative component type names to try when the primary name resolves but
        // yields 0 entities (or the primary name itself cannot be resolved).
        // Order matters: more-specific names first.
        // ---------------------------------------------------------------------------
        private static readonly (string EcsType, string[] FieldPaths)[] FoodAlternatives =
        {
            ("Components.RawComponents.CurrentFood",   new[] { "value", "amount", "current", "count" }),
            ("Components.CurrentFood",                  new[] { "value", "amount", "current", "count" }),
            ("Components.FoodAmount",                   new[] { "value", "amount", "current", "count" }),
            ("Components.ResourceData",                 new[] { "food", "foodAmount", "currentFood"   }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] WoodAlternatives =
        {
            ("Components.RawComponents.CurrentWood",   new[] { "value", "amount", "current", "count" }),
            ("Components.CurrentWood",                  new[] { "value", "amount", "current", "count" }),
            ("Components.WoodAmount",                   new[] { "value", "amount", "current", "count" }),
            ("Components.ResourceData",                 new[] { "wood", "woodAmount", "currentWood"   }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] StoneAlternatives =
        {
            ("Components.RawComponents.CurrentStone",  new[] { "value", "amount", "current", "count" }),
            ("Components.CurrentStone",                 new[] { "value", "amount", "current", "count" }),
            ("Components.StoneAmount",                  new[] { "value", "amount", "current", "count" }),
            ("Components.ResourceData",                 new[] { "stone", "stoneAmount", "currentStone" }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] IronAlternatives =
        {
            ("Components.RawComponents.CurrentIron",   new[] { "value", "amount", "current", "count" }),
            ("Components.CurrentIron",                  new[] { "value", "amount", "current", "count" }),
            ("Components.IronAmount",                   new[] { "value", "amount", "current", "count" }),
            ("Components.ResourceData",                 new[] { "iron", "ironAmount", "currentIron"   }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] MoneyAlternatives =
        {
            ("Components.RawComponents.CurrentMoney",  new[] { "value", "amount", "current", "count" }),
            ("Components.CurrentMoney",                 new[] { "value", "amount", "current", "count" }),
            ("Components.MoneyAmount",                  new[] { "value", "amount", "current", "count" }),
            ("Components.ResourceData",                 new[] { "money", "gold", "currentMoney"        }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] SoulsAlternatives =
        {
            ("Components.RawComponents.CurrentSouls",  new[] { "value", "amount", "current", "count" }),
            ("Components.CurrentSouls",                 new[] { "value", "amount", "current", "count" }),
            ("Components.SoulAmount",                   new[] { "value", "amount", "current", "count" }),
            ("Components.ResourceData",                 new[] { "souls", "soulAmount", "currentSouls"  }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] BonesAlternatives =
        {
            // Primary uses "valueContainer.value" path (nested struct) — try flat paths too
            ("Components.RawComponents.CurrentBones",  new[] { "valueContainer.value", "value", "amount", "current" }),
            ("Components.CurrentBones",                 new[] { "value", "amount", "current"                        }),
            ("Components.ResourceData",                 new[] { "bones", "bonesAmount", "currentBones"              }),
        };

        private static readonly (string EcsType, string[] FieldPaths)[] SpiritAlternatives =
        {
            ("Components.RawComponents.CurrentSpirit", new[] { "valueContainer.value", "value", "amount", "current" }),
            ("Components.CurrentSpirit",                new[] { "value", "amount", "current"                        }),
            ("Components.ResourceData",                 new[] { "spirit", "spiritAmount", "currentSpirit"           }),
        };

        /// <summary>
        /// Reads all known resource stockpile values from the entity manager.
        /// Uses a primary mapping with automatic fallback to alternative component
        /// type names and field paths to tolerate game version differences.
        /// </summary>
        /// <param name="em">The EntityManager to query.</param>
        /// <returns>A snapshot of current resource values.</returns>
        public static ResourceSnapshot ReadResources(EntityManager em)
        {
            ResourceSnapshot snapshot = new ResourceSnapshot();

            snapshot.Food = ReadWithFallback(em, FoodAlternatives, "food");
            snapshot.Wood = ReadWithFallback(em, WoodAlternatives, "wood");
            snapshot.Stone = ReadWithFallback(em, StoneAlternatives, "stone");
            snapshot.Iron = ReadWithFallback(em, IronAlternatives, "iron");
            snapshot.Money = ReadWithFallback(em, MoneyAlternatives, "money");
            snapshot.Souls = ReadWithFallback(em, SoulsAlternatives, "souls");
            snapshot.Bones = ReadWithFallback(em, BonesAlternatives, "bones");
            snapshot.Spirit = ReadWithFallback(em, SpiritAlternatives, "spirit");

            return snapshot;
        }

        /// <summary>
        /// Tries each (EcsType, fieldPaths[]) alternative in order and returns the
        /// first non-zero reading. Returns 0 only when every alternative fails.
        /// </summary>
        private static int ReadWithFallback(
            EntityManager em,
            (string EcsType, string[] FieldPaths)[] alternatives,
            string resourceName)
        {
            foreach ((string ecsType, string[] fieldPaths) in alternatives)
            {
                foreach (string fieldPath in fieldPaths)
                {
                    int result = ReadSingletonInt(em, ecsType, fieldPath);
                    if (result != 0)
                    {
                        WriteDebug($"[ResourceReader] {resourceName}: resolved via {ecsType}.{fieldPath} = {result}");
                        return result;
                    }
                }
            }

            WriteDebug($"[ResourceReader] {resourceName}: all alternatives returned 0 — check dinoforge_debug.log for field errors");
            return 0;
        }

        /// <summary>
        /// Reads a single integer value from a component by ECS type name and dotted field path.
        ///
        /// Bug fix: EntityQueryOptions.IncludePrefab is mandatory — DINO stores resource
        /// singleton entities (and all other live entities) as ECS Prefab entities.
        /// Without this flag the query returns an empty result even when the component
        /// type resolves and entities genuinely exist.
        /// </summary>
        private static int ReadSingletonInt(EntityManager em, string ecsTypeName, string fieldPath)
        {
            try
            {
                // Step 1: resolve the CLR type
                Type? clrType = null;
                foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        clrType = asm.GetType(ecsTypeName, throwOnError: false);
                        if (clrType != null) break;
                    }
                    catch { }
                }

                if (clrType == null)
                    return 0; // type not present in this game version — silent, normal path

                // Step 2: resolve to Unity ComponentType
                ComponentType? ct = EntityQueries.ResolveComponentType(ecsTypeName);
                if (ct == null)
                {
                    WriteDebug($"[ResourceReader] ResolveComponentType failed for {ecsTypeName}");
                    return 0;
                }

                // Step 3: query entities — MUST include IncludePrefab.
                // DINO marks all live entities (including resource singletons) as ECS Prefabs.
                // Without this flag, queries return 0 results even when entities exist.
                EntityQueryDesc desc = new EntityQueryDesc
                {
                    All = new[] { ct.Value },
                    Options = EntityQueryOptions.IncludePrefab
                };
                EntityQuery query = em.CreateEntityQuery(desc);
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

                if (entities.Length == 0)
                {
                    entities.Dispose();
                    query.Dispose();
                    return 0;
                }

                // Pick the first entity (resource singletons have exactly one)
                Entity entity = entities[0];
                entities.Dispose();
                query.Dispose();

                // Step 4: read component data via reflection
                MethodInfo? getMethod = typeof(EntityManager)
                    .GetMethod("GetComponentData", new[] { typeof(Entity) });
                if (getMethod == null) return 0;

                MethodInfo genericGet = getMethod.MakeGenericMethod(clrType);
                object? data = genericGet.Invoke(em, new object[] { entity });
                if (data == null) return 0;

                // Step 5: walk the dotted field path (e.g. "valueContainer.value")
                string[] segments = fieldPath.Split('.');
                object? current = data;
                Type currentType = clrType;

                foreach (string seg in segments)
                {
                    if (current == null) return 0;

                    FieldInfo? field = currentType.GetField(seg,
                        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                    if (field == null)
                    {
                        // Log available fields so we can identify the correct name from the debug log
                        FieldInfo[] available = currentType.GetFields(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        string fieldList = string.Join(", ", Array.ConvertAll(available, f => f.Name));
                        WriteDebug($"[ResourceReader] Field '{seg}' not found on {currentType.FullName}. " +
                                   $"Available fields: [{fieldList}]");
                        return 0;
                    }

                    current = field.GetValue(current);
                    currentType = field.FieldType;
                }

                if (current is int intVal) return intVal;
                if (current is float floatVal) return (int)floatVal;
                if (current is long longVal) return (int)longVal;
                if (current is double dblVal) return (int)dblVal;

                WriteDebug($"[ResourceReader] Unexpected value type {current?.GetType().FullName} at path {ecsTypeName}.{fieldPath}");
                return 0;
            }
            catch (Exception ex)
            {
                WriteDebug($"[ResourceReader] Exception reading {ecsTypeName}.{fieldPath}: {ex.Message}");
                return 0;
            }
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
