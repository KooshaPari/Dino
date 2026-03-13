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
    /// ECS SystemBase that spawns building collapse VFX when structures are destroyed.
    ///
    /// Architecture:
    ///   1. Queries for buildings with BuildingBase component (intact or destroyed state)
    ///   2. Detects destruction transitions via frame-based tracking
    ///   3. Spawns large dust cloud at building center, scaled by building size
    ///   4. Returns particles to pool after 5 seconds
    ///
    /// Faction-specific effects:
    ///   - Republic (player, lacks Enemy tag): blue-tinted dust cloud
    ///   - CIS (enemy, has Enemy tag): orange-tinted dust cloud
    ///
    /// Scale modulation:
    ///   - Particle count multiplied by building size (0.8-1.2x range typical)
    ///   - Larger buildings trigger more intensive visual effects
    ///
    /// TODO: Integrate audio system for destruction cues (destruction + collapse sounds)
    ///
    /// Testing:
    ///   See src/Tests/BuildingDestructionVFXSystemTests.cs for unit tests with mock pool.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class BuildingDestructionVFXSystem : SystemBase
    {
        /// <summary>
        /// Singleton reference to the particle pool manager.
        /// Initialized on first access; if null, system gracefully skips VFX spawning.
        /// </summary>
        private static ParticlePoolManager? _poolManager;

        /// <summary>
        /// Tracks buildings that have already had destruction VFX spawned.
        /// Prevents duplicate spawning across multiple frames.
        /// </summary>
        private readonly HashSet<Entity> _processedDestructions = new HashSet<Entity>();

        /// <summary>
        /// Tracks active destruction VFX instances and their remaining lifetime.
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
        /// How long (in seconds) to keep destruction VFX alive before returning to pool.
        /// </summary>
        private const float VFXLifetime = 5.0f;

        /// <summary>
        /// Minimum building size multiplier for particle scaling.
        /// </summary>
        private const float MinSizeMultiplier = 0.8f;

        /// <summary>
        /// Maximum building size multiplier for particle scaling.
        /// </summary>
        private const float MaxSizeMultiplier = 1.2f;

        /// <summary>
        /// Initialize the system with a particle pool manager.
        /// Called by ModPlatform during startup.
        /// </summary>
        /// <param name="poolManager">The particle pool manager instance.</param>
        public static void SetPoolManager(ParticlePoolManager? poolManager)
        {
            _poolManager = poolManager;
            WriteDebug("BuildingDestructionVFXSystem.SetPoolManager: Pool initialized");
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            WriteDebug("BuildingDestructionVFXSystem.OnCreate");
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
                    WriteDebug("BuildingDestructionVFXSystem: Pool manager not initialized, skipping VFX spawning");
                }
                return;
            }

            // Update active VFX lifetimes and clean up expired ones
            UpdateActiveVFX();

            // Query for buildings that have recently been destroyed
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

            ComponentType? buildingType = EntityQueries.ResolveComponentType("Components.BuildingBase");
            ComponentType? healthType = EntityQueries.ResolveComponentType("Components.Health");

            if (buildingType == null || healthType == null)
            {
                return; // Components not loaded yet
            }

            EntityQueryDesc desc = new EntityQueryDesc
            {
                All = new[]
                {
                    ComponentType.ReadOnly(buildingType.Value.TypeIndex),
                    ComponentType.ReadOnly(healthType.Value.TypeIndex)
                }
            };

            EntityQuery query = em.CreateEntityQuery(desc);
            using NativeArray<Entity> buildings = query.ToEntityArray(Allocator.Temp);

            try
            {
                foreach (Entity building in buildings)
                {
                    // Skip if already processed
                    if (_processedDestructions.Contains(building))
                        continue;

                    try
                    {
                        // Get building health
                        var health = em.GetComponentData<Components.Health>(building);

                        // Check if building is destroyed (health <= 0)
                        if (health.currentHealth > 0)
                            continue;

                        // Mark as processed
                        _processedDestructions.Add(building);

                        // Determine faction from Enemy tag
                        bool isEnemy = em.HasComponent<Components.Enemy>(building);
                        string vfxPoolKey = isEnemy ? "BuildingCollapse_CIS" : "BuildingCollapse_Rep";

                        // Get building position
                        var position = em.GetComponentData<Unity.Transforms.Translation>(building);
                        Vector3 buildingPos = position.Value;

                        // Get building scale for particle modulation
                        float sizeMultiplier = GetBuildingSizeMultiplier(building, em);

                        // Request VFX from pool
                        GameObject? vfxInstance = _poolManager.Get(vfxPoolKey);
                        if (vfxInstance == null)
                        {
                            WriteDebug($"BuildingDestructionVFXSystem: Pool returned null for '{vfxPoolKey}'");
                            continue;
                        }

                        // Position VFX at building center
                        vfxInstance.transform.position = buildingPos;

                        // Play particle system with size scaling
                        ParticleSystem? ps = vfxInstance.GetComponent<ParticleSystem>();
                        if (ps != null)
                        {
                            ps.Play();

                            // Scale particle emission rate by building size
                            var emission = ps.emission;
                            emission.rateOverTime = emission.rateOverTime.constant * sizeMultiplier;

                            _activeVFX[vfxInstance] = VFXLifetime;
                            WriteDebug($"BuildingDestructionVFXSystem: Spawned {vfxPoolKey} at {buildingPos} (scale: {sizeMultiplier:F2}x)");
                        }
                        else
                        {
                            WriteDebug($"BuildingDestructionVFXSystem: No ParticleSystem on {vfxPoolKey}");
                        }

                        // TODO: Trigger dual audio cues (destruction + collapse)
                        // AudioManager.PlayDestructionSound(isEnemy ? "CIS" : "Republic", buildingPos);
                        // AudioManager.PlayCollapseSound(buildingPos);
                    }
                    catch (Exception ex)
                    {
                        WriteDebug($"BuildingDestructionVFXSystem: Error processing building: {ex.Message}");
                    }
                }
            }
            finally
            {
                buildings.Dispose();
            }
        }

        /// <summary>
        /// Calculate the size multiplier for a building based on its scale/dimensions.
        /// Returns a value in the range [MinSizeMultiplier, MaxSizeMultiplier].
        /// </summary>
        private float GetBuildingSizeMultiplier(Entity building, EntityManager em)
        {
            try
            {
                // Try to get building scale from Scale component
                ComponentType? scaleType = EntityQueries.ResolveComponentType("Components.Scale");
                if (scaleType != null && em.HasComponent<Unity.Transforms.Scale>(building))
                {
                    var scale = em.GetComponentData<Unity.Transforms.Scale>(building);
                    float magnitude = scale.Value;
                    // Clamp to expected range
                    return Mathf.Clamp(magnitude, MinSizeMultiplier, MaxSizeMultiplier);
                }

                // Fallback: default multiplier if no scale component
                return 1.0f;
            }
            catch
            {
                // On any error, return default multiplier
                return 1.0f;
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
                    WriteDebug($"BuildingDestructionVFXSystem: Returned {poolKey} to pool");
                }
                catch (Exception ex)
                {
                    WriteDebug($"BuildingDestructionVFXSystem: Error returning VFX to pool: {ex.Message}");
                }
            }
        }

        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = Path.Combine(
                    BepInEx.Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [BuildingDestructionVFXSystem] {msg}\n");
            }
            catch { }
        }
    }
}
