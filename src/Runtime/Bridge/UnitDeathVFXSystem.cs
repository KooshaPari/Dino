#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// ECS SystemBase that spawns death VFX when units die (faction-aware).
    ///
    /// Architecture:
    ///   1. Queries for units with Health component (alive or dead state)
    ///   2. Detects health transitions (alive -> dead) via frame-based tracking
    ///   3. Spawns faction-specific death effect at unit center
    ///   4. Returns particles to pool after 2-3 seconds
    ///
    /// Faction-specific effects:
    ///   - Republic (player, lacks Enemy tag): ascending disintegration (blue)
    ///   - CIS (enemy, has Enemy tag): explosive burst (orange)
    ///
    /// TODO: Integrate audio system for death cues (death sound + faction variant)
    ///
    /// Testing:
    ///   See src/Tests/UnitDeathVFXSystemTests.cs for unit tests with mock pool.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class UnitDeathVFXSystem : SystemBase
    {
        /// <summary>
        /// Singleton reference to the particle pool manager.
        /// Initialized on first access; if null, system gracefully skips VFX spawning.
        /// </summary>
        private static ParticlePoolManager? _poolManager;

        /// <summary>
        /// Tracks units that have already had death VFX spawned.
        /// Prevents duplicate spawning across multiple frames.
        /// </summary>
        private readonly HashSet<Entity> _processedDeaths = new HashSet<Entity>();

        /// <summary>
        /// Tracks active death VFX instances and their remaining lifetime.
        /// </summary>
        private readonly Dictionary<GameObject, float> _activeVFX =
            new Dictionary<GameObject, float>();

        private int _frameCount;

        /// <summary>
        /// Minimum frames to wait before attempting VFX spawning.
        /// Allows pool manager and game initialization.
        /// </summary>
        private const int MinFrameDelay = 600; // ~10 seconds at 60fps

        /// <summary>
        /// How long (in seconds) to keep death VFX alive before returning to pool.
        /// </summary>
        private const float VFXLifetime = 2.5f;

        /// <summary>
        /// Initialize the system with a particle pool manager.
        /// Called by ModPlatform during startup.
        /// </summary>
        /// <param name="poolManager">The particle pool manager instance.</param>
        public static void SetPoolManager(ParticlePoolManager? poolManager)
        {
            _poolManager = poolManager;
            WriteDebug("UnitDeathVFXSystem.SetPoolManager: Pool initialized");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            WriteDebug("UnitDeathVFXSystem.OnCreate");
        }

        protected override void OnUpdate()
        {
            _frameCount++;

            // Wait for game initialization before attempting VFX spawning
            if (_frameCount < MinFrameDelay)
                return;

            // Gracefully skip if pool manager is not available
            if (_poolManager == null)
            {
                if (_frameCount == MinFrameDelay + 1)
                {
                    WriteDebug("UnitDeathVFXSystem: Pool manager not initialized, skipping VFX spawning");
                }
                return;
            }

            // Update active VFX lifetimes and clean up expired ones
            UpdateActiveVFX();

            // Query for units that have recently died
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

            ComponentType? unitType = EntityQueries.ResolveComponentType("Components.Unit");
            ComponentType? healthType = EntityQueries.ResolveComponentType("Components.Health");

            if (unitType == null || healthType == null)
            {
                return; // Components not loaded yet
            }

            EntityQueryDesc desc = new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly(unitType.Value.TypeIndex),
                    ComponentType.ReadOnly(healthType.Value.TypeIndex)
                }
            };

            EntityQuery query = em.CreateEntityQuery(desc);
            using NativeArray<Entity> units = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (Entity unit in units)
                {
                    // Skip if already processed
                    if (_processedDeaths.Contains(unit))
                        continue;

                    try
                    {
                        // Get unit health
                        var health = em.GetComponentData<Components.Health>(unit);

                        // Check if unit is dead (health <= 0)
                        if (health.currentHealth > 0)
                            continue;

                        // Mark as processed
                        _processedDeaths.Add(unit);

                        // Determine faction from Enemy tag
                        bool isEnemy = em.HasComponent<Components.Enemy>(unit);
                        string vfxPoolKey = isEnemy ? "UnitDeathVFX_CIS" : "UnitDeathVFX_Rep";

                        // Get unit position
                        var position = em.GetComponentData<Unity.Transforms.Translation>(unit);
                        Vector3 unitPos = position.Value;

                        // Request VFX from pool
                        GameObject? vfxInstance = _poolManager.Get(vfxPoolKey);
                        if (vfxInstance == null)
                        {
                            WriteDebug($"UnitDeathVFXSystem: Pool returned null for '{vfxPoolKey}'");
                            continue;
                        }

                        // Position VFX at unit center
                        vfxInstance.transform.position = unitPos;

                        // Play particle system
                        ParticleSystem? ps = vfxInstance.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            ps.Play();
                            _activeVFX[vfxInstance] = VFXLifetime;
                            WriteDebug($"UnitDeathVFXSystem: Spawned {vfxPoolKey} at {unitPos}");
                        }
                        else
                        {
                            WriteDebug($"UnitDeathVFXSystem: No ParticleSystem on {vfxPoolKey}");
                        }

                        // TODO: Play audio cue for death (faction-aware)
                        // AudioManager.PlayDeathSound(isEnemy ? "CIS" : "Republic", unitPos);
                    }
                    catch (Exception ex)
                    {
                        WriteDebug($"UnitDeathVFXSystem: Error processing unit: {ex.Message}");
                    }
                }
            }
            finally
            {
                units.Dispose();
            }
        }

        /// <summary>
        /// Update lifetimes of active VFX and return expired ones to pool.
        /// </summary>
        private void UpdateActiveVFX()
        {
            List<GameObject> expired = new List<GameObject>();
            float deltaTime = Time.DeltaTime;

            foreach (var kvp in _activeVFX)
            {
                GameObject vfxInstance = kvp.Key;
                float remainingLifetime = kvp.Value - deltaTime;

                if (remainingLifetime <= 0)
                {
                    expired.Add(vfxInstance);
                }
                else
                {
                    _activeVFX[vfxInstance] = remainingLifetime;
                }
            }

            // Return expired VFX to pool
            foreach (GameObject vfxInstance in expired)
            {
                try
                {
                    ParticleSystem? ps = vfxInstance.GetComponent<ParticleSystem>();
                    if (ps != null)
                        ps.Stop();

                    // Return to pool (infer pool key from prefab name)
                    string poolKey = vfxInstance.name.Replace("(Clone)", "").Trim();
                    _poolManager?.Return(vfxInstance, poolKey);
                    _activeVFX.Remove(vfxInstance);
                    WriteDebug($"UnitDeathVFXSystem: Returned {poolKey} to pool");
                }
                catch (Exception ex)
                {
                    WriteDebug($"UnitDeathVFXSystem: Error returning VFX to pool: {ex.Message}");
                }
            }
        }

        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = Path.Combine(
                    BepInEx.Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [UnitDeathVFXSystem] {msg}\n");
            }
            catch { }
        }
    }
}
