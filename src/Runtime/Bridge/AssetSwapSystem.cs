using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// Describes a pending asset swap: which entities to target and what to replace.
    /// </summary>
    public sealed class AssetSwapRequest
    {
        /// <summary>
        /// ECS component type name used to query target entities
        /// (e.g. "Components.MeleeUnit" to swap all melee unit visuals).
        /// </summary>
        public string TargetComponentType { get; }

        /// <summary>
        /// Path to the AssetBundle containing replacement assets.
        /// Relative to the mod pack's assets directory.
        /// </summary>
        public string AssetBundlePath { get; }

        /// <summary>
        /// Asset name within the bundle for the replacement mesh. Null to keep original.
        /// </summary>
        public string? MeshAssetName { get; set; }

        /// <summary>
        /// Asset name within the bundle for the replacement material. Null to keep original.
        /// </summary>
        public string? MaterialAssetName { get; set; }

        /// <summary>
        /// Optional filter: only swap if entity also has this component.
        /// Null means swap all matching entities.
        /// </summary>
        public string? FilterComponentType { get; set; }

        public AssetSwapRequest(string targetComponentType, string assetBundlePath)
        {
            TargetComponentType = targetComponentType ??
                throw new ArgumentNullException(nameof(targetComponentType));
            AssetBundlePath = assetBundlePath ??
                throw new ArgumentNullException(nameof(assetBundlePath));
        }
    }

    /// <summary>
    /// ECS System that swaps visual assets (meshes, materials, textures) on entities
    /// based on loaded mod packs. This is the core of total conversion mods.
    ///
    /// Runs in PresentationSystemGroup to modify render data after game logic
    /// but before the frame is rendered.
    ///
    /// Architecture notes:
    ///   - DINO uses Unity's Hybrid Renderer V2 (or similar) for ECS rendering
    ///   - Visual data is stored in RenderMesh shared components
    ///   - Asset replacement works by loading a mod AssetBundle and swapping
    ///     the Mesh/Material references on matched entities
    ///   - This system processes a queue of AssetSwapRequests and applies them once
    ///
    /// Manual testing:
    ///   1. Build a test AssetBundle with a replacement mesh/material
    ///   2. Queue a swap via AssetSwapSystem.Enqueue()
    ///   3. Load game and verify visual change on target entities
    ///   4. Check BepInEx/dinoforge_debug.log for swap results
    ///
    /// Entity dump analysis confirms DINO uses Unity.Rendering.RenderMesh shared
    /// components (Hybrid Renderer V1 style). Static environment archetypes show
    /// RenderMesh + BuiltinMaterialPropertyColor + RenderBounds + PerInstanceCullingTag.
    /// The swap targets the RenderMesh shared component to replace mesh/material refs.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class AssetSwapSystem : SystemBase
    {
        private static readonly Queue<AssetSwapRequest> _pendingSwaps =
            new Queue<AssetSwapRequest>();

        /// <summary>
        /// Cache of loaded AssetBundles keyed by file path.
        /// </summary>
        private readonly Dictionary<string, AssetBundle> _loadedBundles =
            new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

        private bool _applied;
        private int _frameCount;

        /// <summary>
        /// Minimum frames to wait before applying swaps.
        /// Must wait for entities to be fully initialized with render data.
        /// </summary>
        private const int MinFrameDelay = 600; // ~10 seconds at 60fps

        /// <summary>
        /// Queue an asset swap request for processing.
        /// Thread-safe: can be called from pack loaders on any thread.
        /// </summary>
        public static void Enqueue(AssetSwapRequest request)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            lock (_pendingSwaps)
            {
                _pendingSwaps.Enqueue(request);
            }
        }

        protected override void OnCreate()
        {
            base.OnCreate();
            WriteDebug("AssetSwapSystem.OnCreate");
        }

        protected override void OnUpdate()
        {
            _frameCount++;

            if (_frameCount < MinFrameDelay)
                return;

            List<AssetSwapRequest> batch;
            lock (_pendingSwaps)
            {
                if (_pendingSwaps.Count == 0)
                {
                    if (_applied)
                    {
                        Enabled = false;
                    }
                    return;
                }

                batch = new List<AssetSwapRequest>();
                while (_pendingSwaps.Count > 0)
                {
                    batch.Add(_pendingSwaps.Dequeue());
                }
            }

            WriteDebug($"AssetSwapSystem processing {batch.Count} swap requests");

            int successCount = 0;
            int failCount = 0;

            foreach (AssetSwapRequest request in batch)
            {
                try
                {
                    bool result = ProcessSwap(request);
                    if (result)
                        successCount++;
                    else
                        failCount++;
                }
                catch (Exception ex)
                {
                    WriteDebug($"AssetSwapSystem: Swap failed for {request.TargetComponentType}: {ex.Message}");
                    failCount++;
                }
            }

            WriteDebug($"AssetSwapSystem: {successCount} succeeded, {failCount} failed");
            _applied = true;
        }

        private bool ProcessSwap(AssetSwapRequest request)
        {
            // Resolve the target component type
            ComponentType? targetCt = Bridge.EntityQueries.ResolveComponentType(request.TargetComponentType);
            if (targetCt == null)
            {
                WriteDebug($"Cannot resolve target component: {request.TargetComponentType}");
                return false;
            }

            // Build query for target entities
            EntityQueryDesc queryDesc;
            if (request.FilterComponentType != null)
            {
                ComponentType? filterCt = Bridge.EntityQueries.ResolveComponentType(request.FilterComponentType);
                if (filterCt == null)
                {
                    WriteDebug($"Cannot resolve filter component: {request.FilterComponentType}");
                    return false;
                }
                queryDesc = new EntityQueryDesc
                {
                    All = new[] { targetCt.Value, filterCt.Value }
                };
            }
            else
            {
                queryDesc = new EntityQueryDesc
                {
                    All = new[] { targetCt.Value }
                };
            }

            EntityQuery query = EntityManager.CreateEntityQuery(queryDesc);
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            if (entities.Length == 0)
            {
                WriteDebug($"No entities matched for swap target: {request.TargetComponentType}");
                entities.Dispose();
                query.Dispose();
                return false;
            }

            WriteDebug($"Found {entities.Length} entities for asset swap");

            // Load the AssetBundle
            AssetBundle? bundle = LoadBundle(request.AssetBundlePath);
            if (bundle == null)
            {
                WriteDebug($"Failed to load AssetBundle: {request.AssetBundlePath}");
                entities.Dispose();
                query.Dispose();
                return false;
            }

            // Load replacement assets from bundle
            Mesh? replacementMesh = null;
            Material? replacementMaterial = null;

            if (request.MeshAssetName != null)
            {
                replacementMesh = bundle.LoadAsset<Mesh>(request.MeshAssetName);
                if (replacementMesh == null)
                {
                    WriteDebug($"Mesh asset not found in bundle: {request.MeshAssetName}");
                }
            }

            if (request.MaterialAssetName != null)
            {
                replacementMaterial = bundle.LoadAsset<Material>(request.MaterialAssetName);
                if (replacementMaterial == null)
                {
                    WriteDebug($"Material asset not found in bundle: {request.MaterialAssetName}");
                }
            }

            if (replacementMesh == null && replacementMaterial == null)
            {
                WriteDebug("No replacement assets loaded — nothing to swap");
                entities.Dispose();
                query.Dispose();
                return false;
            }

            // Apply RenderMesh swap via reflection.
            // Entity dumps confirm DINO uses Unity.Rendering.RenderMesh (Hybrid Renderer V1).
            // RenderMesh is a shared component with fields: mesh, material, subMesh, layer.
            // We use reflection since Unity.Rendering may not be directly referenced.
            Type? renderMeshType = ResolveRenderMeshType();
            if (renderMeshType == null)
            {
                WriteDebug("Cannot resolve Unity.Rendering.RenderMesh type — " +
                           "visual swap skipped, entities matched but render path unavailable");
                entities.Dispose();
                query.Dispose();
                return false;
            }

            int swapCount = 0;
            MethodInfo? getShared = typeof(EntityManager).GetMethod("GetSharedComponentData",
                new[] { typeof(Entity) });
            MethodInfo? setShared = typeof(EntityManager).GetMethod("SetSharedComponentData");

            if (getShared == null || setShared == null)
            {
                WriteDebug("Cannot find GetSharedComponentData/SetSharedComponentData on EntityManager");
                entities.Dispose();
                query.Dispose();
                return false;
            }

            MethodInfo genericGet = getShared.MakeGenericMethod(renderMeshType);
            MethodInfo genericSet = setShared.MakeGenericMethod(renderMeshType);

            FieldInfo? meshField = renderMeshType.GetField("mesh");
            FieldInfo? materialField = renderMeshType.GetField("material");

            for (int i = 0; i < entities.Length; i++)
            {
                try
                {
                    if (!EntityManager.HasComponent(entities[i], ComponentType.ReadOnly(renderMeshType)))
                        continue;

                    object? renderMesh = genericGet.Invoke(EntityManager, new object[] { entities[i] });
                    if (renderMesh == null) continue;

                    bool changed = false;
                    if (replacementMesh != null && meshField != null)
                    {
                        meshField.SetValue(renderMesh, replacementMesh);
                        changed = true;
                    }
                    if (replacementMaterial != null && materialField != null)
                    {
                        materialField.SetValue(renderMesh, replacementMaterial);
                        changed = true;
                    }

                    if (changed)
                    {
                        genericSet.Invoke(EntityManager, new object[] { entities[i], renderMesh });
                        swapCount++;
                    }
                }
                catch (Exception ex)
                {
                    WriteDebug($"Failed to swap entity {entities[i].Index}: {ex.Message}");
                }
            }

            WriteDebug($"Asset swap complete: {swapCount}/{entities.Length} entities swapped " +
                        $"(mesh={request.MeshAssetName ?? "none"}, " +
                        $"material={request.MaterialAssetName ?? "none"})");

            entities.Dispose();
            query.Dispose();
            return swapCount > 0;
        }

        private static Type? _renderMeshType;
        private static bool _renderMeshResolved;

        /// <summary>
        /// Resolve the Unity.Rendering.RenderMesh type from loaded assemblies.
        /// DINO uses Hybrid Renderer V1 which provides RenderMesh as a shared component.
        /// </summary>
        private static Type? ResolveRenderMeshType()
        {
            if (_renderMeshResolved) return _renderMeshType;
            _renderMeshResolved = true;

            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    _renderMeshType = asm.GetType("Unity.Rendering.RenderMesh", throwOnError: false);
                    if (_renderMeshType != null) return _renderMeshType;
                }
                catch { }
            }
            return null;
        }

        /// <summary>
        /// Load an AssetBundle from disk, caching the result.
        /// </summary>
        private AssetBundle? LoadBundle(string path)
        {
            if (_loadedBundles.TryGetValue(path, out AssetBundle? cached))
                return cached;

            // Resolve relative paths against BepInEx plugins directory
            string fullPath = Path.IsPathRooted(path)
                ? path
                : Path.Combine(BepInEx.Paths.PluginPath, path);

            if (!File.Exists(fullPath))
            {
                WriteDebug($"AssetBundle not found: {fullPath}");
                return null;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
                if (bundle != null)
                {
                    _loadedBundles[path] = bundle;
                    WriteDebug($"Loaded AssetBundle: {fullPath}");
                }
                return bundle;
            }
            catch (Exception ex)
            {
                WriteDebug($"Failed to load AssetBundle {fullPath}: {ex.Message}");
                return null;
            }
        }

        protected override void OnDestroy()
        {
            // Unload all cached bundles
            foreach (AssetBundle bundle in _loadedBundles.Values)
            {
                try
                {
                    bundle.Unload(false);
                }
                catch { }
            }
            _loadedBundles.Clear();

            base.OnDestroy();
            WriteDebug("AssetSwapSystem.OnDestroy — bundles unloaded");
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
