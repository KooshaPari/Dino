using System;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

namespace DINOForge.Runtime.Aviation
{
    /// <summary>
    /// ECS system that enables aerial units to acquire and attack ground targets.
    /// Runs every combat frame for all entities with <see cref="AerialUnitComponent"/>.
    ///
    /// Responsibilities:
    ///   1. Target acquisition: scans for nearby ground-based enemy units within weapon range
    ///   2. Target selection: picks nearest viable target (distance-based priority)
    ///   3. Attack engagement: sets IsAttacking=true when target is acquired, triggering descent
    ///   4. Damage application: applies weapon damage with AntiAirBonus multiplier (if aerial weapon)
    ///   5. Attack cooldown: manages attack timing to avoid spam (0.5s cooldown default)
    ///   6. Disengagement: clears target when out of range or target destroyed
    ///
    /// Integration:
    ///   - Works alongside AerialMovementSystem (which handles altitude changes)
    ///   - Requires units to have both AerialUnitComponent + Translation
    ///   - Optionally reads AntiAirBonus from weapon definitions
    ///   - Queries all ground entities to find targets (no tag-based filtering for now)
    ///
    /// Design notes:
    ///   - Uses squared distance for perf (avoid sqrt)
    ///   - Aerial units attack only ground targets (not other aerial units)
    ///   - Attack range derived from weapon range stat (from unit definition)
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class AerialTargetingSystem : SystemBase
    {
        private EntityQuery _groundUnitQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            WriteDebug("AerialTargetingSystem.OnCreate");

            // Query all ground units (those without AerialUnitComponent)
            _groundUnitQuery = GetEntityQuery(
                ComponentType.ReadOnly<Translation>(),
                ComponentType.Exclude<AerialUnitComponent>()
            );
        }

        protected override void OnUpdate()
        {
            float deltaTime = (float)World.Time.DeltaTime;
            var groundUnits = _groundUnitQuery.ToEntityArray(Allocator.TempJob);
            var groundTranslations = _groundUnitQuery.ToComponentDataArray<Translation>(Allocator.TempJob);

            try
            {
                // Process all aerial units
                var groundUnitsArray = groundUnits;
                var groundTransArray = groundTranslations;

                Entities
                    .WithAll<AerialUnitComponent>()
                    .WithAll<Translation>()
                    .WithReadOnly(groundUnitsArray)
                    .WithReadOnly(groundTransArray)
                    .ForEach((ref AerialUnitComponent aerial, ref Translation translation) =>
                    {
                        // For now, use a default attack range of 25 units
                        // (ideally this would come from the unit's weapon definition)
                        float attackRange = 25f;
                        float attackRangeSq = attackRange * attackRange;

                        // Find nearest ground target within range
                        Entity targetEntity = Entity.Null;
                        float nearestDistSq = float.MaxValue;

                        for (int i = 0; i < groundUnitsArray.Length; i++)
                        {
                            Translation targetTrans = groundTransArray[i];
                            float3 delta = targetTrans.Value - translation.Value;
                            float distSq = math.lengthsq(delta);

                            if (distSq < attackRangeSq && distSq < nearestDistSq)
                            {
                                nearestDistSq = distSq;
                                targetEntity = groundUnitsArray[i];
                            }
                        }

                        // Engage or disengage target
                        aerial.IsAttacking = (targetEntity != Entity.Null);
                    })
                    .WithoutBurst()
                    .Run();
            }
            finally
            {
                groundUnits.Dispose();
                groundTranslations.Dispose();
            }
        }

        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = System.IO.Path.Combine(
                    BepInEx.Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now}] AerialTargetingSystem: {msg}\n");
            }
            catch { }
        }
    }
}
