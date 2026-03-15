#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DINOForge.SDK.Assets;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// ECS System that applies pending asset swaps registered via <see cref="AssetSwapRegistry"/>.
    ///
    /// Lifecycle:
    ///   1. Mod pack loaders call <see cref="AssetSwapRegistry.Register"/> (SDK layer, any thread).
    ///   2. This system waits <see cref="MinFrameDelay"/> frames for the game world to fully load.
    ///   3. On each update cycle after the delay, pending swaps are drained from
    ///      <see cref="AssetSwapRegistry"/>, patched bundles are written to
    ///      <c>BepInEx/dinoforge_patched_bundles/</c> via <see cref="AssetService.ReplaceAsset"/>,
    ///      and <see cref="AssetSwapRegistry.MarkApplied"/> is called on success.
    ///   4. The system also applies RenderMesh visual swaps for ECS entities matching
    ///      the source asset address - bridging the bundle write path to live entities.
    ///
    /// Thread safety:
    ///   - <see cref="AssetSwapRegistry"/> is thread-safe; this system only reads from the
    ///     main Unity thread (ECS SystemBase guarantee).
    ///
    /// Architecture notes:
    ///   - DINO uses Unity's Hybrid Renderer V2 (or similar) for ECS rendering.
    ///   - Visual data is stored in RenderMesh shared components.
    ///   - Asset replacement works by (a) patching the vanilla bundle file with the mod's bytes
    ///     and (b) swapping Mesh/Material references on matched entities so the live game sees
    ///     the new assets without a scene reload.
    ///
    /// Manual testing:
    ///   1. Build a test AssetBundle with a replacement mesh/material.
    ///   2. Register a swap via <see cref="AssetSwapRegistry.Register"/>.
    ///   3. Load game and verify visual change on target entities.
    ///   4. Check <c>BepInEx/dinoforge_debug.log</c> for swap results.
    ///
    /// Entity dump analysis confirms DINO uses Unity.Rendering.RenderMesh shared
    /// components (Hybrid Renderer V1 style). Static environment archetypes show
    /// RenderMesh + BuiltinMaterialPropertyColor + RenderBounds + PerInstanceCullingTag.
    /// The swap targets the RenderMesh shared component to replace mesh/material refs.
    /// </summary>
    [UpdateInGroup(typeof(PresentationSystemGroup))]
    public class AssetSwapSystem : SystemBase
    {
        /// <summary>
        /// Cache of loaded AssetBundles keyed by file path (used for RenderMesh visual swap).
        /// </summary>
        private readonly Dictionary<string, AssetBundle> _loadedBundles =
            new Dictionary<string, AssetBundle>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Tracks which asset addresses have had their disk bundle patched (phase 1).
        /// Live entity swaps (phase 2) are attempted separately once RenderMesh entities exist.
        /// </summary>
        private readonly HashSet<string> _patchedAddresses =
            new HashSet<string>(StringComparer.Ordinal);

        /// <summary>
        /// Subdirectory under BepInEx root where patched bundles are written.
        /// </summary>
        private const string PatchedBundlesDir = "dinoforge_patched_bundles";

        /// <inheritdoc/>
        protected override void OnCreate()
        {
            base.OnCreate();
            WriteDebug("AssetSwapSystem.OnCreate");

            // Phase 1: patch bundles on disk immediately at system creation.
            // This has no ECS dependency — vanilla bundle files can be overwritten as soon as
            // the system exists, before any entity data is available.
            PatchPendingBundlesOnDisk();
        }

        /// <inheritdoc/>
        protected override void OnUpdate()
        {
            // Phase 2: live RenderMesh entity swap.
            // Only attempt once RenderMesh entities actually exist; no fixed frame delay.
            Type? renderMeshType = ResolveRenderMeshType();
            if (renderMeshType == null)
                return;

            // Identify requests that need a live entity swap (registered after OnCreate,
            // or whose bundle patch succeeded but whose entities weren't ready yet).
            IReadOnlyList<AssetSwapRequest> pending = AssetSwapRegistry.GetPending();
            if (pending.Count == 0)
                return;

            // Check if any RenderMesh entities exist yet; bail early if none.
            EntityQuery probeQuery = EntityManager.CreateEntityQuery(
                new EntityQueryDesc { All = new[] { ComponentType.ReadOnly(renderMeshType) } });
            int entityCount = probeQuery.CalculateEntityCount();
            probeQuery.Dispose();
            if (entityCount == 0)
                return;

            WriteDebug($"AssetSwapSystem: {entityCount} RenderMesh entities found — " +
                       $"applying {pending.Count} live entity swap(s)");

            foreach (AssetSwapRequest request in pending)
            {
                try
                {
                    string modBundleFullPath = ResolveModBundlePath(request.ModBundlePath);
                    bool entitySwapped = TrySwapRenderMeshFromBundle(
                        modBundleFullPath, request.AssetName, request.VanillaMapping);

                    if (entitySwapped || _patchedAddresses.Contains(request.AssetAddress))
                    {
                        AssetSwapRegistry.MarkApplied(request.AssetAddress);
                        WriteDebug($"AssetSwapSystem: swap complete — address='{request.AssetAddress}' " +
                                   $"entitySwap={entitySwapped} bundlePatched={_patchedAddresses.Contains(request.AssetAddress)}");
                    }
                    else
                    {
                        WriteDebug($"AssetSwapSystem: live swap failed — address='{request.AssetAddress}'");
                    }
                }
                catch (Exception ex)
                {
                    WriteDebug($"AssetSwapSystem: swap exception for '{request.AssetAddress}': {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Patches vanilla bundle files on disk for all currently pending swap requests.
        /// Called from <see cref="OnCreate"/> so patches are written at load time,
        /// not deferred until entities are present.
        /// </summary>
        private void PatchPendingBundlesOnDisk()
        {
            IReadOnlyList<AssetSwapRequest> pending = AssetSwapRegistry.GetPending();
            if (pending.Count == 0)
            {
                WriteDebug("PatchPendingBundlesOnDisk: no pending swaps at OnCreate");
                return;
            }

            WriteDebug($"PatchPendingBundlesOnDisk: patching {pending.Count} bundle(s) at load time");

            string patchDir = Path.Combine(BepInEx.Paths.BepInExRootPath, PatchedBundlesDir);
            AssetService assetService = new AssetService(BepInEx.Paths.GameRootPath);

            int patched = 0;
            int skipped = 0;

            foreach (AssetSwapRequest request in pending)
            {
                try
                {
                    string modBundleFullPath = ResolveModBundlePath(request.ModBundlePath);
                    if (!File.Exists(modBundleFullPath))
                    {
                        WriteDebug($"PatchPendingBundlesOnDisk: mod bundle not found: {modBundleFullPath}");
                        skipped++;
                        continue;
                    }

                    byte[]? modAssetBytes = assetService.ExtractAsset(modBundleFullPath, request.AssetName);
                    if (modAssetBytes == null || modAssetBytes.Length == 0)
                    {
                        skipped++;
                        continue;
                    }

                    IReadOnlyDictionary<string, string> catalog = assetService.ReadCatalog();
                    if (!catalog.TryGetValue(request.AssetAddress, out string? vanillaBundleRelPath)
                        || string.IsNullOrEmpty(vanillaBundleRelPath))
                    {
                        skipped++;
                        continue;
                    }

                    string vanillaBundlePath = AddressablesCatalog.ResolveBundlePath(
                        vanillaBundleRelPath, BepInEx.Paths.GameRootPath);

                    if (!File.Exists(vanillaBundlePath))
                    {
                        skipped++;
                        continue;
                    }

                    string outputPath = Path.Combine(patchDir, Path.GetFileName(vanillaBundlePath));
                    bool ok = assetService.ReplaceAsset(
                        vanillaBundlePath, request.AssetAddress, modAssetBytes, outputPath);

                    if (ok)
                    {
                        _patchedAddresses.Add(request.AssetAddress);
                        patched++;
                        WriteDebug($"PatchPendingBundlesOnDisk: patched '{request.AssetAddress}' → '{outputPath}'");
                    }
                    else
                    {
                        skipped++;
                    }
                }
                catch (Exception ex)
                {
                    skipped++;
                    WriteDebug($"PatchPendingBundlesOnDisk: exception for '{request.AssetAddress}': {ex.Message}");
                }
            }

            assetService.Dispose();
            WriteDebug($"PatchPendingBundlesOnDisk: {patched} patched, {skipped} skipped");
        }

        /// <summary>
        /// Attempts to load a Mesh or Material from the mod bundle and apply it to ECS entities
        /// carrying a RenderMesh shared component.
        /// When <paramref name="vanillaMapping"/> is provided the entity query is narrowed to only
        /// entities that also carry the corresponding unit-archetype component (e.g.
        /// <c>Components.MeleeUnit</c>), preventing the replacement from touching unrelated geometry.
        /// </summary>
        private bool TrySwapRenderMeshFromBundle(
            string modBundlePath, string assetName, string? vanillaMapping)
        {
            AssetBundle? bundle = LoadBundle(modBundlePath);
            if (bundle == null) return false;

            Mesh? replacementMesh = bundle.LoadAsset<Mesh>(assetName);
            Material? replacementMat = bundle.LoadAsset<Material>(assetName);

            // Bundles built from Unity prefabs store a GameObject hierarchy, not a bare Mesh/Material.
            // Fall back to loading the prefab and extracting its mesh and material.
            // Prefer SkinnedMeshRenderer (animated characters) so mesh+material always come from
            // the same component — avoids mismatches when both SMR and static MF/MR exist.
            if (replacementMesh == null && replacementMat == null)
            {
                GameObject? prefab = bundle.LoadAsset<GameObject>(assetName);
                if (prefab != null)
                {
                    SkinnedMeshRenderer? smr = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                    if (smr != null && smr.sharedMesh != null)
                    {
                        replacementMesh = smr.sharedMesh;
                        if (smr.sharedMaterials.Length > 0)
                            replacementMat = smr.sharedMaterials[0];
                    }
                    else
                    {
                        // Static mesh fallback — extract from the same object to stay consistent.
                        MeshFilter? mf = prefab.GetComponentInChildren<MeshFilter>();
                        if (mf != null)
                            replacementMesh = mf.sharedMesh;

                        MeshRenderer? mr = prefab.GetComponentInChildren<MeshRenderer>();
                        if (mr != null && mr.sharedMaterials.Length > 0)
                            replacementMat = mr.sharedMaterials[0];
                    }

                    if (replacementMesh != null || replacementMat != null)
                        WriteDebug($"TrySwapRenderMeshFromBundle: extracted from prefab '{assetName}'");
                }
            }

            if (replacementMesh == null && replacementMat == null)
            {
                WriteDebug(
                    $"TrySwapRenderMeshFromBundle: no Mesh/Material named '{assetName}' in bundle");
                return false;
            }

            Type? renderMeshType = ResolveRenderMeshType();
            if (renderMeshType == null)
            {
                WriteDebug("TrySwapRenderMeshFromBundle: Unity.Rendering.RenderMesh type not found");
                return false;
            }

            // Resolve vanilla_mapping → ECS component type for targeted entity filtering.
            // When the mapping is absent or unrecognised we fall back to RenderMesh-only query,
            // which at minimum avoids modifying non-unit geometry in cases like buildings.
            ComponentType[] queryComponents;
            if (!string.IsNullOrWhiteSpace(vanillaMapping)
                && PackStatMappings.TryResolveMapping(vanillaMapping, out string? archetypeTypeName)
                && !string.IsNullOrEmpty(archetypeTypeName))
            {
                Type? archetypeType = ResolveTypeByName(archetypeTypeName!);
                if (archetypeType != null)
                {
                    queryComponents = new[]
                    {
                        ComponentType.ReadOnly(renderMeshType),
                        ComponentType.ReadOnly(archetypeType),
                    };
                    WriteDebug(
                        $"TrySwapRenderMeshFromBundle: filtering by '{archetypeTypeName}' " +
                        $"for vanilla_mapping='{vanillaMapping}'");
                }
                else
                {
                    WriteDebug(
                        $"TrySwapRenderMeshFromBundle: archetype type '{archetypeTypeName}' not " +
                        $"found in assemblies; falling back to RenderMesh-only query");
                    queryComponents = new[] { ComponentType.ReadOnly(renderMeshType) };
                }
            }
            else
            {
                queryComponents = new[] { ComponentType.ReadOnly(renderMeshType) };
            }

            EntityQuery query = EntityManager.CreateEntityQuery(
                new EntityQueryDesc { All = queryComponents });
            NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

            MethodInfo? getShared = typeof(EntityManager).GetMethod(
                "GetSharedComponentData", new[] { typeof(Entity) });
            MethodInfo? setShared = typeof(EntityManager).GetMethod("SetSharedComponentData");

            if (getShared == null || setShared == null)
            {
                WriteDebug(
                    "TrySwapRenderMeshFromBundle: GetSharedComponentData/SetSharedComponentData not found");
                entities.Dispose();
                query.Dispose();
                return false;
            }

            MethodInfo genericGet = getShared.MakeGenericMethod(renderMeshType);
            MethodInfo genericSet = setShared.MakeGenericMethod(renderMeshType);

            FieldInfo? meshField = renderMeshType.GetField("mesh");
            FieldInfo? materialField = renderMeshType.GetField("material");

            int swapCount = 0;
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
                    if (replacementMat != null && materialField != null)
                    {
                        materialField.SetValue(renderMesh, replacementMat);
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
                    WriteDebug(
                        $"TrySwapRenderMeshFromBundle: failed on entity {entities[i].Index}: {ex.Message}");
                }
            }

            WriteDebug($"TrySwapRenderMeshFromBundle: swapped {swapCount}/{entities.Length} entities");
            entities.Dispose();
            query.Dispose();

            return swapCount > 0;
        }

        // ------------------------------------------------------------------ helpers

        private static Type? _renderMeshType;
        private static bool _renderMeshResolved;

        /// <summary>
        /// Resolves the Unity.Rendering.RenderMesh type from loaded assemblies.
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

        private static readonly Dictionary<string, Type?> _resolvedTypeCache =
            new Dictionary<string, Type?>(StringComparer.Ordinal);

        /// <summary>
        /// Resolves a fully-qualified type name (e.g. "Components.MeleeUnit") from any loaded assembly.
        /// Results are cached to avoid repeated assembly scans.
        /// </summary>
        private static Type? ResolveTypeByName(string typeName)
        {
            if (_resolvedTypeCache.TryGetValue(typeName, out Type? cached))
                return cached;

            Type? found = null;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    found = asm.GetType(typeName, throwOnError: false);
                    if (found != null) break;
                }
                catch { }
            }

            _resolvedTypeCache[typeName] = found;
            return found;
        }

        /// <summary>
        /// Resolves a mod bundle path. Relative paths are joined against the BepInEx plugins dir.
        /// </summary>
        private static string ResolveModBundlePath(string path)
        {
            return Path.IsPathRooted(path)
                ? path
                : Path.Combine(BepInEx.Paths.PluginPath, path);
        }

        /// <summary>
        /// Loads an AssetBundle from disk, caching the result.
        /// </summary>
        private AssetBundle? LoadBundle(string path)
        {
            if (_loadedBundles.TryGetValue(path, out AssetBundle? cached))
                return cached;

            string fullPath = ResolveModBundlePath(path);

            if (!File.Exists(fullPath))
            {
                WriteDebug($"LoadBundle: file not found: {fullPath}");
                return null;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
                if (bundle != null)
                {
                    _loadedBundles[path] = bundle;
                    WriteDebug($"LoadBundle: loaded '{fullPath}'");
                }
                return bundle;
            }
            catch (Exception ex)
            {
                WriteDebug($"LoadBundle: failed '{fullPath}': {ex.Message}");
                return null;
            }
        }

        /// <inheritdoc/>
        protected override void OnDestroy()
        {
            foreach (AssetBundle bundle in _loadedBundles.Values)
            {
                try { bundle.Unload(false); }
                catch { }
            }
            _loadedBundles.Clear();

            base.OnDestroy();
            WriteDebug("AssetSwapSystem.OnDestroy - bundles unloaded");
        }

        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = Path.Combine(
                    BepInEx.Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now:u}] {msg}\n");
            }
            catch { }
        }
    }
}
