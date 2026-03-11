using System;
using System.Collections.Generic;
using System.IO;
using DINOForge.SDK;
using Unity.Collections;
using Unity.Entities;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// ECS system that spawns units defined in loaded DINOForge packs.
    /// Coordinates with VanillaCatalog and StatModifierSystem to clone and customize
    /// vanilla unit archetypes based on pack definitions.
    ///
    /// Architecture:
    ///   1. Requests are enqueued via IUnitFactory.RequestSpawn (thread-safe)
    ///   2. PackUnitSpawner processes the queue on its OnUpdate
    ///   3. For each request, it:
    ///      - Looks up the unit definition by ID
    ///      - Maps the unit class to a vanilla archetype via VanillaArchetypeMapper
    ///      - Finds a sample vanilla entity of that archetype
    ///      - Clones it using EntityManager.Instantiate
    ///      - Applies the pack unit's stat overrides via StatModifierSystem
    ///      - Tags the entity with a custom component for pack tracking
    ///
    /// Manual testing:
    ///   1. Load game with packs that define units
    ///   2. Call PackUnitSpawner.RequestSpawn("pack-id:unit-id", 10, 20) from console
    ///   3. Check BepInEx/dinoforge_debug.log for spawn results
    ///   4. Verify in-game that the custom unit appears
    ///   5. Dump entities and confirm custom unit has pack tagging component
    ///
    /// TODO: M9 implementation
    ///   - Implement full spawn queue processing
    ///   - Wire up entity instantiation and stat application
    ///   - Add pack unit registry lookup
    ///   - Implement faction tagging (Enemy component)
    ///   - Test with mock ECS systems
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class PackUnitSpawner : SystemBase, IUnitFactory
    {
        /// <summary>
        /// Static queue for spawn requests. Thread-safe via lock.
        /// Kept static so it can be accessed from non-SystemBase contexts.
        /// </summary>
        private static readonly Queue<UnitSpawnRequest> _spawnQueue = new Queue<UnitSpawnRequest>();

        private int _frameCount;
        private int _spawnedCount;

        /// <summary>Minimum frames to wait before processing spawns.</summary>
        private const int MinFrameDelay = 1800; // ~30 seconds at 60fps

        protected override void OnCreate()
        {
            base.OnCreate();
            WriteDebug("PackUnitSpawner.OnCreate");
        }

        protected override void OnUpdate()
        {
            _frameCount++;

            // Wait for game initialization before attempting spawns
            if (_frameCount < MinFrameDelay)
                return;

            // Dequeue pending requests
            List<UnitSpawnRequest> batch;
            lock (_spawnQueue)
            {
                if (_spawnQueue.Count == 0)
                    return;

                batch = new List<UnitSpawnRequest>();
                while (_spawnQueue.Count > 0)
                {
                    batch.Add(_spawnQueue.Dequeue());
                }
            }

            WriteDebug($"PackUnitSpawner processing {batch.Count} spawn requests");

            // TODO: M9 implementation
            // For each spawn request:
            //   1. Look up unit definition from registry
            //   2. Get its unit class
            //   3. Map class to vanilla archetype component type via VanillaArchetypeMapper
            //   4. Query for vanilla entities of that archetype
            //   5. Pick a template entity
            //   6. Clone via EntityManager.Instantiate(templateEntity, out NativeArray<Entity> clones)
            //   7. Apply pack stats to the cloned entity via StatModifierSystem.Enqueue
            //   8. Tag the entity with a custom component indicating it's a pack unit
            //   9. Set world position if available
            //   10. Add Enemy component if isEnemy = true
            //
            // Expected errors / edge cases:
            //   - Unit definition not found → log warning, skip
            //   - Vanilla archetype not found → log warning, skip
            //   - EntityManager.Instantiate fails → log error, skip
            //   - Stat application fails → logged by StatModifierSystem
            //
            // Pseudocode:
            // foreach (request in batch)
            // {
            //     var unitDef = unitRegistry.Get(request.UnitDefinitionId);
            //     if (unitDef == null) { LogWarning(...); continue; }
            //
            //     var componentType = VanillaArchetypeMapper.MapUnitClassToComponentType(unitDef.UnitClass);
            //     if (componentType == null) { LogWarning(...); continue; }
            //
            //     var query = EntityQueries.GetUnitsByClass(EntityManager, componentType);
            //     var entities = query.ToEntityArray(Allocator.Temp);
            //     if (entities.Length == 0) { LogWarning(...); continue; }
            //
            //     var templateEntity = entities[0];
            //     var cloned = EntityManager.Instantiate(templateEntity);
            //
            //     // Apply position
            //     // Apply faction
            //     // Queue stat modifications
            //     // Tag entity as pack-owned
            //
            //     _spawnedCount++;
            // }

            WriteDebug($"PackUnitSpawner: Processed {batch.Count} requests, spawned {_spawnedCount} total units");
        }

        /// <summary>
        /// Request a unit spawn at the given world position.
        /// Thread-safe: can be called from pack loaders on any thread.
        /// The actual spawn will be processed on the next ECS frame.
        /// </summary>
        /// <param name="unitDefinitionId">Pack unit definition ID.</param>
        /// <param name="x">World X coordinate.</param>
        /// <param name="z">World Z coordinate.</param>
        public static void RequestSpawnStatic(string unitDefinitionId, float x, float z, bool isEnemy = false)
        {
            if (string.IsNullOrWhiteSpace(unitDefinitionId))
                return;

            var request = new UnitSpawnRequest(unitDefinitionId, x, z, isEnemy);
            lock (_spawnQueue)
            {
                _spawnQueue.Enqueue(request);
            }
        }

        // IUnitFactory implementation
        public bool CanSpawn(string unitDefinitionId)
        {
            // TODO: M9 implementation
            // Check if unit definition exists in registry
            // Check if its unit class can be mapped to a vanilla archetype
            // Check if vanilla archetype entities exist in the world
            return false; // Placeholder
        }

        public void RequestSpawn(string unitDefinitionId, float x, float z)
        {
            RequestSpawnStatic(unitDefinitionId, x, z, isEnemy: false);
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
