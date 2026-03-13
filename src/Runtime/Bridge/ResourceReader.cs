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
    /// Reads current resource stockpile values from the ECS world singletons.
    /// Each resource is stored as a singleton component with a "value" field.
    /// </summary>
    public static class ResourceReader
    {
        /// <summary>
        /// Reads all known resource singleton values from the entity manager.
        /// </summary>
        /// <param name="em">The EntityManager to query.</param>
        /// <returns>A snapshot of current resource values.</returns>
        public static ResourceSnapshot ReadResources(EntityManager em)
        {
            ResourceSnapshot snapshot = new ResourceSnapshot();

            snapshot.Food = ReadSingletonInt(em, ComponentMap.ResourceFood);
            snapshot.Wood = ReadSingletonInt(em, ComponentMap.ResourceWood);
            snapshot.Stone = ReadSingletonInt(em, ComponentMap.ResourceStone);
            snapshot.Iron = ReadSingletonInt(em, ComponentMap.ResourceIron);
            snapshot.Money = ReadSingletonInt(em, ComponentMap.ResourceMoney);
            snapshot.Souls = ReadSingletonInt(em, ComponentMap.ResourceSouls);
            snapshot.Bones = ReadSingletonInt(em, ComponentMap.ResourceBones);
            snapshot.Spirit = ReadSingletonInt(em, ComponentMap.ResourceSpirit);

            return snapshot;
        }

        /// <summary>
        /// Reads a single integer value from a singleton component using the ComponentMap entry.
        /// </summary>
        private static int ReadSingletonInt(EntityManager em, ComponentMapping mapping)
        {
            try
            {
                Type? clrType = mapping.ResolvedType;
                if (clrType == null)
                {
                    WriteDebug($"[ResourceReader] Cannot resolve type: {mapping.EcsComponentType}");
                    return 0;
                }

                // Resolve to Unity.Entities.ComponentType
                ComponentType? ct = EntityQueries.ResolveComponentType(mapping.EcsComponentType);
                if (ct == null)
                {
                    WriteDebug($"[ResourceReader] Cannot create ComponentType for: {mapping.EcsComponentType}");
                    return 0;
                }

                // Create query for entities with this component
                EntityQueryDesc desc = new EntityQueryDesc
                {
                    All = new[] { ct.Value }
                };
                EntityQuery query = em.CreateEntityQuery(desc);
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

                if (entities.Length == 0)
                {
                    entities.Dispose();
                    query.Dispose();
                    return 0;
                }

                // Read the first entity's component data via reflection
                Entity entity = entities[0];
                entities.Dispose();
                query.Dispose();

                // Use EntityManager.GetComponentData<T>(entity) via reflection
                MethodInfo? getMethod = typeof(EntityManager)
                    .GetMethod("GetComponentData", new[] { typeof(Entity) });
                if (getMethod == null) return 0;

                MethodInfo genericGet = getMethod.MakeGenericMethod(clrType);
                object? data = genericGet.Invoke(em, new object[] { entity });
                if (data == null) return 0;

                // Read the target field — supports dotted paths like "valueContainer.value"
                string fieldPath = mapping.TargetFieldName ?? "value";
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
                        WriteDebug($"[ResourceReader] Field '{seg}' not found on {currentType.FullName} (path: {fieldPath})");
                        return 0;
                    }
                    current = field.GetValue(current);
                    currentType = field.FieldType;
                }

                if (current is int intVal) return intVal;
                if (current is float floatVal) return (int)floatVal;

                return 0;
            }
            catch (Exception ex)
            {
                WriteDebug($"[ResourceReader] Error reading {mapping.SdkModelPath}: {ex.Message}");
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
