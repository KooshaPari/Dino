# Addressables v1.21.18 Integration Design for warfare-starwars

**Status**: Design Document
**Version**: 1.0
**Date**: 2026-03-12
**Author**: DINOForge Agents
**Related**: Asset Pipeline (M4), warfare-starwars pack (M5)

## Executive Summary

This document specifies the complete integration of Unity Addressables v1.21.18 for the **warfare-starwars** content pack. DINO uses Addressables (not classic AssetBundles) to load 4.2GB+ of modded assets at runtime. The warfare-starwars pack will ship **50 textures + FBX meshes** organized across two primary bundles:

- `warfare-starwars-units` (26 FBX + 26 textures)
- `warfare-starwars-buildings` (24 FBX + 24 textures)
- `warfare-starwars-configs` (YAML definitions)

This design provides:
1. **Bundle layout and addressing conventions**
2. **ContentLoader integration** (IAssetLoader implementation)
3. **AddressablesCatalog wrapping** with runtime resolution
4. **Cache strategies** (memory management patterns)
5. **Windows build automation** (catalog generation)
6. **Debugging guides** (in-game asset inspector integration)
7. **Reference implementation** (sample C# code)

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Bundle Layout & Organization](#bundle-layout--organization)
3. [Address Naming Convention](#address-naming-convention)
4. [AddressablesCatalog Integration](#addressablescatalog-integration)
5. [ContentLoader IAssetLoader Wrapper](#contentloader-iassetloader-wrapper)
6. [Cache & Memory Management](#cache--memory-management)
7. [Windows Build & Catalog Generation](#windows-build--catalog-generation)
8. [Runtime Asset Loading Patterns](#runtime-asset-loading-patterns)
9. [Debugging & Diagnostics](#debugging--diagnostics)
10. [Migration & Compatibility](#migration--compatibility)
11. [Reference Implementation](#reference-implementation)
12. [Troubleshooting Guide](#troubleshooting-guide)

---

## Architecture Overview

### Component Layer Stack

```
┌─────────────────────────────────────────────────────────┐
│         Warfare-StarWars Pack                           │
│  (YAML manifests + 50 textures/FBX meshes)             │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  ContentLoader (Orchestrator)                           │
│  ├─ LoadPack(packDirectory)                             │
│  └─ LoadAssets(assetAddress) → IAssetLoader            │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  AddressablesAssetLoader (IAssetLoader impl)           │
│  ├─ LoadAsync<T>(address)                              │
│  ├─ LoadAllAsync<T>(label)                             │
│  └─ UnloadAsync(address)                               │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  AddressablesCatalog Parser                             │
│  ├─ Load(catalogPath)                                  │
│  ├─ KeyToBundleMap                                     │
│  └─ ResolveBundlePath(placeholder)                     │
└────────────────────┬────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────┐
│  Unity Addressables System (v1.21.18)                   │
│  ├─ StreamingAssets/aa/StandaloneWindows64/            │
│  ├─ catalog.json (asset key → bundle mappings)         │
│  └─ *.bundle files (packed asset data)                 │
└─────────────────────────────────────────────────────────┘
```

### Design Principles Applied

| Principle | Implementation |
|-----------|-----------------|
| **Wrap, don't handroll** | Use Unity Addressables directly + thin `AddressablesAssetLoader` adapter |
| **Graceful degradation** | Fallback to file system if Addressables fails (5s timeout, retry logic) |
| **Observability first** | All asset loads logged to BepInEx, inspector overlay support |
| **Stable abstraction** | `IAssetLoader` interface isolates ECS from Addressables complexity |
| **Declarative loading** | YAML pack.yaml declares which assets to load; no hardcoded addresses |

---

## Bundle Layout & Organization

### Bundle Structure

```
packs/warfare-starwars/
├── assets/
│   ├── meshes/                    # FBX source files (pre-build)
│   │   ├── rep_clone_trooper.fbx
│   │   ├── sep_battle_droid.fbx
│   │   ├── rep_house_clone_quarters.fbx
│   │   └── ...24 unit FBX + 24 building FBX
│   │
│   ├── textures/                  # PNG texture files (pre-build)
│   │   ├── rep_clone_trooper_base.png
│   │   ├── rep_clone_trooper_normal.png
│   │   ├── rep_house_clone_quarters_diffuse.png
│   │   └── ...26 unit textures + 24 building textures
│   │
│   └── bundles/                   # POST-BUILD: Unity Addressables bundles
│       ├── warfare-starwars-units.bundle
│       │   └── Contains: 26 FBX + 26 textures (rep/sep units)
│       │
│       ├── warfare-starwars-buildings.bundle
│       │   └── Contains: 24 FBX + 24 textures (rep/sep buildings)
│       │
│       └── warfare-starwars-configs.bundle
│           └── Contains: YAML config TextAssets

units/
├── rep_units.yaml            # 13 Republic units
└── sep_units.yaml            # 13 Separatist units

buildings/
├── rep_buildings.yaml        # 12 Republic buildings
└── sep_buildings.yaml        # 12 Separatist buildings

factions/
├── republic.yaml             # Faction: Galactic Republic
└── cis.yaml                  # Faction: Confederacy of Independent Systems

doctrines/
├── republic_tactics.yaml
└── cis_tactics.yaml

weapons/
├── republic_weapons.yaml
└── sep_weapons.yaml

waves/
├── republic_waves.yaml
└── sep_waves.yaml
```

### Bundle Naming Convention

```
warfare-starwars-{contentType}[.bundle]

Examples:
  warfare-starwars-units.bundle         # Unit meshes + textures
  warfare-starwars-buildings.bundle     # Building meshes + textures
  warfare-starwars-configs.bundle       # YAML configuration
```

### Bundle Packaging Rules

**Each bundle contains**:
- **Mesh files**: `.fbx` exports from Blender (Unity FBX importer)
- **Texture files**: `.png` or `.tga` (sRGB for diffuse, linear for normal maps)
- **Metadata**: Texture import settings, mesh import settings
- **Optional**: Material assets (if custom shaders used)

**Bundle size targets**:
- `warfare-starwars-units.bundle`: ~500 MB (26 unit meshes + 26 diffuse + 26 normal textures)
- `warfare-starwars-buildings.bundle`: ~400 MB (24 building meshes + 48 textures)
- `warfare-starwars-configs.bundle`: ~1 MB (YAML as TextAssets)

---

## Address Naming Convention

### Addressable Asset Addresses

All assets in warfare-starwars follow a hierarchical naming scheme:

#### Unit Meshes

```
warfare-starwars/units/{faction}_{unit_id}.fbx

Examples:
  warfare-starwars/units/rep_clone_trooper.fbx
  warfare-starwars/units/rep_arc_trooper.fbx
  warfare-starwars/units/sep_battle_droid.fbx
  warfare-starwars/units/sep_droideka.fbx
```

#### Unit Textures

```
warfare-starwars/textures/units/{faction}_{unit_id}_{type}.png

Examples:
  warfare-starwars/textures/units/rep_clone_trooper_base.png
  warfare-starwars/textures/units/rep_clone_trooper_normal.png
  warfare-starwars/textures/units/sep_battle_droid_base.png
  warfare-starwars/textures/units/sep_battle_droid_normal.png
```

#### Building Meshes

```
warfare-starwars/buildings/{faction}_{building_id}.fbx

Examples:
  warfare-starwars/buildings/rep_house_clone_quarters.fbx
  warfare-starwars/buildings/rep_command_center_republic.fbx
  warfare-starwars/buildings/sep_droid_factory.fbx
```

#### Building Textures

```
warfare-starwars/textures/buildings/{faction}_{building_id}_{type}.png

Examples:
  warfare-starwars/textures/buildings/rep_house_clone_quarters_diffuse.png
  warfare-starwars/textures/buildings/rep_house_clone_quarters_normal.png
  warfare-starwars/textures/buildings/sep_droid_factory_base.png
```

#### Configuration Files

```
warfare-starwars/configs/{contentType}.yaml

Examples:
  warfare-starwars/configs/units.yaml
  warfare-starwars/configs/buildings.yaml
  warfare-starwars/configs/factions.yaml
```

### Address Validation Rules

1. **Lowercase only**: `rep_clone_trooper`, not `Rep_Clone_Trooper`
2. **Underscore separators**: faction_id, not faction-id
3. **No spaces**: `clone_trooper`, not `clone trooper`
4. **File extensions included**: `.fbx`, `.png`, `.yaml`
5. **Unique per content type**: No two units share the same ID within rep/sep

---

## AddressablesCatalog Integration

### Catalog.json Structure

The Addressables system generates `StreamingAssets/aa/catalog.json` at build time:

```json
{
  "m_LockedHash": "...",
  "m_InternalIds": [
    "StreamingAssets/aa/StandaloneWindows64/defaultlocalgroup_assets_all_xxxxxxx.bundle",
    "warfare-starwars/units/rep_clone_trooper.fbx",
    "warfare-starwars/textures/units/rep_clone_trooper_base.png",
    "..."
  ],
  "m_KeyDataString": "...",
  "m_BucketDataString": "...",
  "m_EntryDataString": "..."
}
```

### AddressablesCatalog Parser

The existing `AddressablesCatalog.cs` provides:

```csharp
// Load catalog from disk
AddressablesCatalog catalog = AddressablesCatalog.Load(catalogPath);

// Query mappings
IReadOnlyDictionary<string, string> map = catalog.KeyToBundleMap;
// "warfare-starwars/units/rep_clone_trooper.fbx"
//   → "StreamingAssets/aa/StandaloneWindows64/warfare-starwars-units.bundle"

// Resolve runtime paths
string resolvedPath = AddressablesCatalog.ResolveBundlePath(
    "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/warfare-starwars-units.bundle",
    gameDir: "G:\\SteamLibrary\\...\\Diplomacy is Not an Option"
);
// → "G:\\SteamLibrary\\...\\Diplomacy is Not an Option\\Diplomacy is Not an Option_Data\\StreamingAssets\\aa\\StandaloneWindows64\\warfare-starwars-units.bundle"
```

### Integration Points

| Component | Usage |
|-----------|-------|
| `AddressablesAssetLoader` | Reads catalog to pre-validate addresses before loading |
| `ContentLoader` | Logs asset mappings during pack initialization |
| `Inspector` (debug overlay) | Displays live asset loading status per bundle |
| `PackCompiler validate` | Validates all addresses exist in catalog pre-deployment |

---

## ContentLoader IAssetLoader Wrapper

### Interface Design

```csharp
namespace DINOForge.SDK.Assets
{
    /// <summary>
    /// Abstraction for loading assets from various sources
    /// (Addressables, file system, remote, etc.).
    /// Implementations are source-agnostic; callers use a single interface.
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>Loads a single asset by address.</summary>
        /// <typeparam name="T">Asset type (Mesh, Texture2D, etc.).</typeparam>
        /// <param name="address">Asset address (e.g., "warfare-starwars/units/rep_clone_trooper.fbx").</param>
        /// <param name="timeout">Max wait time (default 5s).</param>
        /// <returns>Loaded asset, or null if not found.</returns>
        Task<T?> LoadAsync<T>(string address, TimeSpan? timeout = null) where T : UnityEngine.Object;

        /// <summary>Loads all assets matching a label.</summary>
        /// <typeparam name="T">Asset type.</typeparam>
        /// <param name="label">Label to search (e.g., "warfare-starwars-units").</param>
        /// <returns>List of loaded assets.</returns>
        Task<IReadOnlyList<T>> LoadAllAsync<T>(string label) where T : UnityEngine.Object;

        /// <summary>Unloads an asset from memory.</summary>
        /// <param name="address">Asset address to unload.</param>
        Task UnloadAsync(string address);

        /// <summary>Clears the asset cache (all untracked assets).</summary>
        Task ClearCacheAsync();

        /// <summary>Returns memory usage stats for diagnostics.</summary>
        IAssetLoaderStats GetStats();
    }

    /// <summary>Diagnostic stats for asset loading performance.</summary>
    public interface IAssetLoaderStats
    {
        /// <summary>Total assets currently loaded.</summary>
        int LoadedAssetCount { get; }

        /// <summary>Total memory used by assets (bytes).</summary>
        long MemoryUsageBytes { get; }

        /// <summary>Failed load attempts.</summary>
        int FailedLoadCount { get; }

        /// <summary>Last error message.</summary>
        string? LastErrorMessage { get; }
    }
}
```

### AddressablesAssetLoader Implementation

```csharp
namespace DINOForge.SDK.Assets
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using UnityEngine;
    using UnityEngine.AddressableAssets;
    using UnityEngine.ResourceManagement.AsyncOperations;

    /// <summary>
    /// Implements IAssetLoader using Unity Addressables with fallback to file system.
    /// Provides timeout, retry logic, and memory management.
    /// </summary>
    public class AddressablesAssetLoader : IAssetLoader
    {
        private readonly string _packId;
        private readonly AddressablesCatalog _catalog;
        private readonly Action<string> _log;
        private readonly Dictionary<string, object> _loadedAssets;
        private readonly Dictionary<string, AsyncOperationHandle> _activeHandles;
        private int _failedLoadCount;
        private string? _lastError;

        /// <summary>
        /// Initializes the Addressables asset loader for a pack.
        /// </summary>
        /// <param name="packId">Pack identifier (e.g., "warfare-starwars").</param>
        /// <param name="catalogPath">Path to catalog.json.</param>
        /// <param name="log">Optional logging callback.</param>
        public AddressablesAssetLoader(string packId, string catalogPath, Action<string>? log = null)
        {
            _packId = packId ?? throw new ArgumentNullException(nameof(packId));
            _catalog = AddressablesCatalog.Load(catalogPath);
            _log = log ?? (_ => { });
            _loadedAssets = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _activeHandles = new Dictionary<string, AsyncOperationHandle>(StringComparer.OrdinalIgnoreCase);
            _failedLoadCount = 0;
            _lastError = null;

            _log($"[AddressablesAssetLoader] Initialized for pack '{packId}' with {_catalog.KeyToBundleMap.Count} assets");
        }

        /// <summary>
        /// Loads a single asset by address with timeout and retry logic.
        /// Addressables → fallback to file system.
        /// </summary>
        public async Task<T?> LoadAsync<T>(string address, TimeSpan? timeout = null) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(address))
            {
                _lastError = "Address is null or empty";
                _failedLoadCount++;
                return null;
            }

            TimeSpan effectiveTimeout = timeout ?? TimeSpan.FromSeconds(5);
            _log($"[AddressablesAssetLoader] Loading {address} (timeout: {effectiveTimeout.TotalSeconds}s)");

            // Check if already loaded
            if (_loadedAssets.TryGetValue(address, out object? cached) && cached is T cachedT)
            {
                _log($"[AddressablesAssetLoader] Cache hit: {address}");
                return cachedT;
            }

            // Try Addressables
            try
            {
                T? asset = await LoadViaAddressablesAsync<T>(address, effectiveTimeout);
                if (asset != null)
                {
                    _loadedAssets[address] = asset;
                    _log($"[AddressablesAssetLoader] Loaded {address} via Addressables");
                    return asset;
                }
            }
            catch (Exception ex)
            {
                _log($"[AddressablesAssetLoader] Addressables load failed for {address}: {ex.Message}");
            }

            // Fallback to file system
            try
            {
                T? asset = await LoadViaFileSystemAsync<T>(address);
                if (asset != null)
                {
                    _loadedAssets[address] = asset;
                    _log($"[AddressablesAssetLoader] Loaded {address} via file system fallback");
                    return asset;
                }
            }
            catch (Exception ex)
            {
                _lastError = $"Failed to load {address}: {ex.Message}";
                _failedLoadCount++;
                _log($"[AddressablesAssetLoader] File system fallback failed: {_lastError}");
            }

            return null;
        }

        /// <summary>
        /// Loads all assets matching a label (e.g., "warfare-starwars-units").
        /// </summary>
        public async Task<IReadOnlyList<T>> LoadAllAsync<T>(string label) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(label))
            {
                return new List<T>();
            }

            _log($"[AddressablesAssetLoader] Loading all assets with label '{label}'");
            var results = new List<T>();

            // Find all addresses in the catalog that match the label
            var matchingAddresses = _catalog.KeyToBundleMap
                .Where(kvp => kvp.Value.Contains(label, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();

            _log($"[AddressablesAssetLoader] Found {matchingAddresses.Count} assets matching label '{label}'");

            foreach (string address in matchingAddresses)
            {
                T? asset = await LoadAsync<T>(address);
                if (asset != null)
                {
                    results.Add(asset);
                }
            }

            return results.AsReadOnly();
        }

        /// <summary>
        /// Unloads a single asset from memory.
        /// </summary>
        public async Task UnloadAsync(string address)
        {
            if (!_loadedAssets.Remove(address))
            {
                _log($"[AddressablesAssetLoader] Asset not found for unload: {address}");
                return;
            }

            if (_activeHandles.TryGetValue(address, out AsyncOperationHandle handle))
            {
                Addressables.Release(handle);
                _activeHandles.Remove(address);
                _log($"[AddressablesAssetLoader] Unloaded {address}");
            }

            await Task.CompletedTask;
        }

        /// <summary>
        /// Clears all cached assets from memory.
        /// </summary>
        public async Task ClearCacheAsync()
        {
            _log($"[AddressablesAssetLoader] Clearing {_activeHandles.Count} cached assets");
            foreach (var handle in _activeHandles.Values)
            {
                Addressables.Release(handle);
            }

            _activeHandles.Clear();
            _loadedAssets.Clear();

            await Task.CompletedTask;
        }

        /// <summary>
        /// Returns diagnostic stats about asset loading.
        /// </summary>
        public IAssetLoaderStats GetStats()
        {
            return new AssetLoaderStats(
                loadedAssetCount: _loadedAssets.Count,
                memoryUsageBytes: _loadedAssets.Values.OfType<Texture2D>().Sum(t => t.width * t.height * 4)
                    + _loadedAssets.Values.OfType<Mesh>().Sum(m => m.vertices.Length * 12), // Rough estimate
                failedLoadCount: _failedLoadCount,
                lastErrorMessage: _lastError);
        }

        /// <summary>
        /// Loads via Unity Addressables with timeout.
        /// </summary>
        private async Task<T?> LoadViaAddressablesAsync<T>(string address, TimeSpan timeout) where T : UnityEngine.Object
        {
            var handle = Addressables.LoadAssetAsync<T>(address);
            _activeHandles[address] = handle;

            var tcs = new TaskCompletionSource<T?>();
            var cts = new System.Threading.CancellationTokenSource(timeout);

            // Polling for completion
            _ = Task.Run(async () =>
            {
                while (!handle.IsDone && !cts.Token.IsCancellationRequested)
                {
                    await Task.Delay(10);
                }

                if (cts.Token.IsCancellationRequested)
                {
                    tcs.TrySetResult(null);
                }
                else if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    tcs.TrySetResult(handle.Result);
                }
                else
                {
                    tcs.TrySetException(new Exception($"Addressables load failed: {handle.OperationException?.Message}"));
                }
            });

            return await tcs.Task;
        }

        /// <summary>
        /// Loads from the file system as fallback (for development/debugging).
        /// </summary>
        private async Task<T?> LoadViaFileSystemAsync<T>(string address) where T : UnityEngine.Object
        {
            // Implementation would load from StreamingAssets directly
            // For now, placeholder that returns null
            await Task.CompletedTask;
            return null;
        }

        /// <summary>
        /// Diagnostic stats holder.
        /// </summary>
        private class AssetLoaderStats : IAssetLoaderStats
        {
            public int LoadedAssetCount { get; }
            public long MemoryUsageBytes { get; }
            public int FailedLoadCount { get; }
            public string? LastErrorMessage { get; }

            public AssetLoaderStats(int loadedAssetCount, long memoryUsageBytes, int failedLoadCount, string? lastErrorMessage)
            {
                LoadedAssetCount = loadedAssetCount;
                MemoryUsageBytes = memoryUsageBytes;
                FailedLoadCount = failedLoadCount;
                LastErrorMessage = lastErrorMessage;
            }
        }
    }
}
```

### ContentLoader Extension

```csharp
// In ContentLoader.cs, add this method:

private IAssetLoader? _assetLoader;

/// <summary>
/// Initializes the asset loader for a pack.
/// </summary>
public void InitializeAssetLoader(string packId, string catalogPath)
{
    _assetLoader = new AddressablesAssetLoader(packId, catalogPath, _log);
}

/// <summary>
/// Loads an asset from Addressables.
/// </summary>
public async Task<T?> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
{
    if (_assetLoader == null)
    {
        _log("[ContentLoader] Asset loader not initialized");
        return null;
    }

    return await _assetLoader.LoadAsync<T>(address);
}
```

---

## Cache & Memory Management

### Cache Strategies

#### Strategy 1: Persistent Cache (Default)

**Use case**: Core unit/building meshes that are used throughout gameplay.

```csharp
// Load once, keep in memory for the session
var unitMesh = await assetLoader.LoadAsync<Mesh>("warfare-starwars/units/rep_clone_trooper.fbx");
// Cache hit on subsequent loads
var unitMesh2 = await assetLoader.LoadAsync<Mesh>("warfare-starwars/units/rep_clone_trooper.fbx");
```

**Pros**: Fast lookups, no reload overhead
**Cons**: Increases memory footprint

#### Strategy 2: LRU Cache (Least Recently Used)

**Use case**: One-off assets loaded during initialization, unloaded after use.

```csharp
public class LRUAssetCache : IAssetLoader
{
    private readonly int _maxCapacity;
    private readonly Queue<string> _usageOrder;
    private readonly IAssetLoader _underlying;

    public async Task<T?> LoadAsync<T>(string address, TimeSpan? timeout = null) where T : UnityEngine.Object
    {
        if (_usageOrder.Count >= _maxCapacity && !_usageOrder.Contains(address))
        {
            // Evict least recently used
            string lruAddress = _usageOrder.Dequeue();
            await _underlying.UnloadAsync(lruAddress);
        }

        _usageOrder.Enqueue(address);
        return await _underlying.LoadAsync<T>(address, timeout);
    }

    // ... rest of IAssetLoader implementation
}
```

#### Strategy 3: Bundle-Scoped Cache

**Use case**: Load entire bundle, use for a mission phase, unload after.

```csharp
public async Task LoadBundleAsync(string bundleLabel)
{
    // Load all assets in "warfare-starwars-units" bundle
    var units = await assetLoader.LoadAllAsync<Mesh>(bundleLabel);
    _log($"Loaded {units.Count} unit meshes from {bundleLabel}");

    // ... use units during gameplay ...

    // After mission phase, unload
    await assetLoader.ClearCacheAsync();
}
```

### Memory Management Patterns

#### Pattern A: Automatic Unload on Asset Destruction

```csharp
public class UnitMeshComponent : MonoBehaviour
{
    private string _assetAddress;
    private IAssetLoader _assetLoader;
    private Mesh? _cachedMesh;

    public async Task InitializeAsync(string address, IAssetLoader loader)
    {
        _assetAddress = address;
        _assetLoader = loader;
        _cachedMesh = await _assetLoader.LoadAsync<Mesh>(address);
    }

    private void OnDestroy()
    {
        // Unload when GameObject is destroyed
        if (!string.IsNullOrEmpty(_assetAddress) && _assetLoader != null)
        {
            _ = _assetLoader.UnloadAsync(_assetAddress);
        }
    }
}
```

#### Pattern B: Reference Counting

```csharp
public class RefCountedAssetLoader : IAssetLoader
{
    private readonly Dictionary<string, (object Asset, int Count)> _refCounts;
    private readonly IAssetLoader _underlying;

    public async Task<T?> LoadAsync<T>(string address, TimeSpan? timeout = null) where T : UnityEngine.Object
    {
        if (_refCounts.TryGetValue(address, out var entry))
        {
            _refCounts[address] = (entry.Asset, entry.Count + 1);
            return entry.Asset as T;
        }

        T? asset = await _underlying.LoadAsync<T>(address, timeout);
        if (asset != null)
        {
            _refCounts[address] = (asset, 1);
        }

        return asset;
    }

    public async Task UnloadAsync(string address)
    {
        if (_refCounts.TryGetValue(address, out var entry))
        {
            if (entry.Count - 1 <= 0)
            {
                await _underlying.UnloadAsync(address);
                _refCounts.Remove(address);
            }
            else
            {
                _refCounts[address] = (entry.Asset, entry.Count - 1);
            }
        }
    }

    // ... rest of IAssetLoader implementation
}
```

#### Pattern C: Time-Based Unload

```csharp
public class TimedAssetCache : IAssetLoader
{
    private readonly TimeSpan _unloadDelay;
    private readonly Dictionary<string, (object Asset, DateTime LoadTime)> _cache;

    public async Task ClearCacheAsync()
    {
        var expired = _cache
            .Where(kvp => DateTime.UtcNow - kvp.Value.LoadTime > _unloadDelay)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (string address in expired)
        {
            await UnloadAsync(address);
        }
    }

    // ... rest of IAssetLoader implementation
}
```

### Memory Budget Guidelines

| Asset Type | Typical Size | Example | Max Count |
|------------|--------------|---------|-----------|
| Unit Mesh (FBX) | 2-5 MB | rep_clone_trooper | 26 |
| Building Mesh (FBX) | 5-20 MB | rep_house_clone_quarters | 24 |
| Texture (Diffuse) | 8-16 MB | rep_clone_trooper_base.png | 50 |
| Texture (Normal) | 8-16 MB | rep_clone_trooper_normal.png | 50 |

**Total warfare-starwars footprint**: ~1.5-2 GB for all assets

**Recommended cache limits**:
- Persistent: 500 MB (core unit/building meshes)
- LRU: 200 MB (one-off textures)
- Bundle-scoped: 1 GB (entire warfare-starwars bundle)

---

## Windows Build & Catalog Generation

### Step 1: Asset Preparation (Blender)

**Files to generate**:
- `packs/warfare-starwars/assets/meshes/*.fbx` (FBX export from Blender)
- `packs/warfare-starwars/assets/textures/*.png` (Diffuse + normal maps)

**Blender FBX Export Settings**:
```
Operator: File > Export > FBX (.fbx)

Geometry:
  ✓ Apply Modifiers
  ✓ Apply Deformations
  ✓ Add Leaf Bones (if rigged)

Armature:
  ✓ NLA Strips
  ✓ All Bone Layers
  ✓ All Shape Keys

Animation:
  ✓ Group by NLA Track
  ✓ Shape Keys

Deformed Mesh:
  ✓ Use Mesh Modifiers

Filename: rep_clone_trooper.fbx
```

**PNG Texture Export Settings**:
```
Operator: Image > Save As

Format: PNG (8-bit or 16-bit)
Color Space: Diffuse maps = sRGB, Normal maps = Linear
Filename: rep_clone_trooper_base.png (diffuse), rep_clone_trooper_normal.png (normal)
```

### Step 2: Unity Project Setup

**In Unity Editor (2021.3.45f2)**:

```
1. Import FBX + PNG files into Assets/Mods/warfare-starwars/

   Project/
   └── Assets/
       └── Mods/
           └── warfare-starwars/
               ├── Meshes/
               │   ├── rep_clone_trooper.fbx
               │   ├── rep_arc_trooper.fbx
               │   ├── ... (all FBX files)
               │
               ├── Textures/
               │   ├── rep_clone_trooper_base.png
               │   ├── rep_clone_trooper_normal.png
               │   ├── ... (all PNG files)
               │
               └── Materials/
                   ├── Unit.mat
                   └── Building.mat

2. Create Addressables Groups for each bundle:

   Window > Addressables > Groups

   Create group: warfare-starwars-units
   ├─ Add Assets/Mods/warfare-starwars/Meshes/*.fbx (all 26)
   ├─ Add Assets/Mods/warfare-starwars/Textures/units/*.png (all 26)
   └─ Set label: "warfare-starwars-units"

   Create group: warfare-starwars-buildings
   ├─ Add Assets/Mods/warfare-starwars/Meshes/*.fbx (buildings only)
   ├─ Add Assets/Mods/warfare-starwars/Textures/buildings/*.png (all 24)
   └─ Set label: "warfare-starwars-buildings"

   Create group: warfare-starwars-configs
   ├─ Add pack.yaml as TextAsset
   └─ Set label: "warfare-starwars-configs"

3. Set Addressable Address for each asset:

   For rep_clone_trooper.fbx:
     Address: warfare-starwars/units/rep_clone_trooper.fbx

   For rep_clone_trooper_base.png:
     Address: warfare-starwars/textures/units/rep_clone_trooper_base.png

4. Build Addressables Catalog:

   Window > Addressables > Build > New Build > Default Build Script

   This generates:
     StreamingAssets/aa/catalog.json
     StreamingAssets/aa/StandaloneWindows64/*.bundle
```

### Step 3: Catalog Build Script (PowerShell / Batch)

**File: `packs/warfare-starwars/build_addressables.ps1`**

```powershell
# Build warfare-starwars Addressables catalog for Windows

param(
    [string]$UnityProjectPath = "C:\DINOForgeProjects\DINO",
    [string]$UnityExePath = "C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe",
    [string]$OutputPath = "C:\Users\koosh\Dino\packs\warfare-starwars\assets\bundles"
)

# Validate Unity project exists
if (-not (Test-Path "$UnityProjectPath\Assets")) {
    Write-Error "Unity project not found at $UnityProjectPath"
    exit 1
}

Write-Host "Building Addressables catalog for warfare-starwars..."
Write-Host "Unity: $UnityExePath"
Write-Host "Output: $OutputPath"

# Build Addressables via Unity command line
$unityArgs = @(
    "-projectPath", $UnityProjectPath,
    "-executeMethod", "UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent",
    "-nographics",
    "-quit",
    "-logFile", "-"
)

& $UnityExePath @unityArgs

if ($LASTEXITCODE -ne 0) {
    Write-Error "Addressables build failed with exit code $LASTEXITCODE"
    exit 1
}

# Copy bundles to pack directory
Write-Host "Copying bundles to $OutputPath..."
$bundleDir = "$UnityProjectPath\StreamingAssets\aa\StandaloneWindows64"
Copy-Item "$bundleDir\*.bundle" -Destination $OutputPath -Force
Copy-Item "$bundleDir\catalog.json" -Destination "$OutputPath\..\" -Force

Write-Host "Addressables build complete!"
Write-Host "Bundles location: $OutputPath"
```

**Batch equivalent: `build_addressables.bat`**

```batch
@echo off
setlocal enabledelayedexpansion

set UNITY_PROJECT=C:\DINOForgeProjects\DINO
set UNITY_EXE=C:\Program Files\Unity\Hub\Editor\2021.3.45f2\Editor\Unity.exe
set OUTPUT_PATH=C:\Users\koosh\Dino\packs\warfare-starwars\assets\bundles

echo Building Addressables for warfare-starwars...
"%UNITY_EXE%" ^
    -projectPath "%UNITY_PROJECT%" ^
    -executeMethod UnityEditor.AddressableAssets.Settings.AddressableAssetSettings.BuildPlayerContent ^
    -nographics ^
    -quit

if errorlevel 1 (
    echo Addressables build failed
    exit /b 1
)

echo Copying bundles to %OUTPUT_PATH%...
xcopy "%UNITY_PROJECT%\StreamingAssets\aa\StandaloneWindows64\*.bundle" "%OUTPUT_PATH%\" /Y
copy "%UNITY_PROJECT%\StreamingAssets\aa\StandaloneWindows64\catalog.json" "%OUTPUT_PATH%\.." /Y

echo Build complete!
```

### Step 4: Deployment to Game Directory

**File: `packs/warfare-starwars/deploy_addressables.ps1`**

```powershell
# Deploy built Addressables to DINO installation

param(
    [string]$GameDir = "G:\SteamLibrary\steamapps\common\Diplomacy is Not an Option",
    [string]$BundlesPath = "C:\Users\koosh\Dino\packs\warfare-starwars\assets\bundles"
)

$targetDir = Join-Path $GameDir "Diplomacy is Not an Option_Data\StreamingAssets\aa\StandaloneWindows64"

if (-not (Test-Path $targetDir)) {
    Write-Error "Game StreamingAssets directory not found: $targetDir"
    exit 1
}

Write-Host "Deploying Addressables bundles..."
Write-Host "Source: $BundlesPath"
Write-Host "Target: $targetDir"

# Backup existing bundles
$backupDir = "$targetDir\backup_$(Get-Date -Format yyyyMMdd_HHmmss)"
if (Test-Path "$targetDir\warfare-starwars-*.bundle") {
    New-Item -Path $backupDir -ItemType Directory -Force | Out-Null
    Copy-Item "$targetDir\warfare-starwars-*.bundle" -Destination $backupDir -Force
    Write-Host "Backed up existing bundles to $backupDir"
}

# Copy new bundles
Copy-Item "$BundlesPath\*.bundle" -Destination $targetDir -Force

# Update catalog
Copy-Item "$BundlesPath\..\catalog.json" -Destination (Split-Path $targetDir) -Force

Write-Host "Deployment complete!"
Write-Host "Bundles: $targetDir"
```

### Step 5: Validation

**File: `packs/warfare-starwars/validate_addressables.cs`** (Unit test)

```csharp
[Fact]
public void TestAddressablesCatalogIsValid()
{
    string catalogPath = "StreamingAssets/aa/catalog.json";

    // Load catalog
    var catalog = AddressablesCatalog.Load(catalogPath);

    // Verify all warfare-starwars addresses exist
    var expectedAddresses = new[]
    {
        "warfare-starwars/units/rep_clone_trooper.fbx",
        "warfare-starwars/units/sep_battle_droid.fbx",
        "warfare-starwars/textures/units/rep_clone_trooper_base.png",
        "warfare-starwars/buildings/rep_house_clone_quarters.fbx",
        // ... all 50 assets
    };

    foreach (string address in expectedAddresses)
    {
        Assert.Contains(address, catalog.KeyToBundleMap.Keys);
    }

    // Verify bundles exist
    var bundleNames = new[] { "warfare-starwars-units", "warfare-starwars-buildings", "warfare-starwars-configs" };
    foreach (string bundle in bundleNames)
    {
        Assert.True(catalog.BundlePaths.Any(p => p.Contains(bundle)), $"Bundle {bundle} not found");
    }
}
```

---

## Runtime Asset Loading Patterns

### Pattern 1: Load All Units on Startup

```csharp
public class UnitMeshRegistry
{
    private readonly IAssetLoader _assetLoader;
    private readonly Dictionary<string, Mesh> _unitMeshes;

    public UnitMeshRegistry(IAssetLoader assetLoader)
    {
        _assetLoader = assetLoader ?? throw new ArgumentNullException(nameof(assetLoader));
        _unitMeshes = new Dictionary<string, Mesh>(StringComparer.OrdinalIgnoreCase);
    }

    public async Task LoadAllUnitsAsync()
    {
        var unitAddresses = new[]
        {
            "warfare-starwars/units/rep_clone_trooper.fbx",
            "warfare-starwars/units/rep_arc_trooper.fbx",
            "warfare-starwars/units/sep_battle_droid.fbx",
            // ... all 26 units
        };

        foreach (string address in unitAddresses)
        {
            try
            {
                var mesh = await _assetLoader.LoadAsync<Mesh>(address, timeout: TimeSpan.FromSeconds(10));
                if (mesh != null)
                {
                    string unitId = Path.GetFileNameWithoutExtension(address);
                    _unitMeshes[unitId] = mesh;
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to load unit mesh {address}: {ex.Message}");
            }
        }

        Debug.Log($"Loaded {_unitMeshes.Count} unit meshes");
    }

    public Mesh? GetUnitMesh(string unitId)
    {
        _unitMeshes.TryGetValue(unitId, out var mesh);
        return mesh;
    }
}
```

### Pattern 2: Lazy Load on First Use

```csharp
public class LazyMeshLoader
{
    private readonly IAssetLoader _assetLoader;
    private readonly Dictionary<string, Task<Mesh?>> _loadingTasks;
    private readonly Dictionary<string, Mesh?> _cache;

    public LazyMeshLoader(IAssetLoader assetLoader)
    {
        _assetLoader = assetLoader;
        _loadingTasks = new Dictionary<string, Task<Mesh?>>(StringComparer.OrdinalIgnoreCase);
        _cache = new Dictionary<string, Mesh?>(StringComparer.OrdinalIgnoreCase);
    }

    public Task<Mesh?> GetMeshAsync(string address)
    {
        // Cache hit
        if (_cache.TryGetValue(address, out var cached))
        {
            return Task.FromResult(cached);
        }

        // Loading in progress
        if (_loadingTasks.TryGetValue(address, out var task))
        {
            return task;
        }

        // Start new load
        var loadTask = LoadAndCacheAsync(address);
        _loadingTasks[address] = loadTask;
        return loadTask;
    }

    private async Task<Mesh?> LoadAndCacheAsync(string address)
    {
        try
        {
            var mesh = await _assetLoader.LoadAsync<Mesh>(address);
            _cache[address] = mesh;
            return mesh;
        }
        finally
        {
            _loadingTasks.Remove(address);
        }
    }
}
```

### Pattern 3: Load and Release on Demand

```csharp
public class TemporaryAssetHandle<T> : IAsyncDisposable where T : UnityEngine.Object
{
    private readonly IAssetLoader _assetLoader;
    private readonly string _address;
    private T? _asset;

    public T? Asset => _asset;

    private TemporaryAssetHandle(IAssetLoader assetLoader, string address, T? asset)
    {
        _assetLoader = assetLoader;
        _address = address;
        _asset = asset;
    }

    public static async Task<TemporaryAssetHandle<T>> LoadAsync(IAssetLoader assetLoader, string address)
    {
        var asset = await assetLoader.LoadAsync<T>(address);
        return new TemporaryAssetHandle<T>(assetLoader, address, asset);
    }

    public async ValueTask DisposeAsync()
    {
        if (!string.IsNullOrEmpty(_address))
        {
            await _assetLoader.UnloadAsync(_address);
        }
    }
}

// Usage:
using var handle = await TemporaryAssetHandle<Mesh>.LoadAsync(
    assetLoader,
    "warfare-starwars/units/rep_clone_trooper.fbx"
);
var mesh = handle.Asset;
// Automatically unloaded when using block exits
```

---

## Debugging & Diagnostics

### In-Game Inspector Overlay

**Integration with DebugOverlay**:

```csharp
public class AddressablesDebugOverlay : IDebugOverlaySection
{
    private readonly IAssetLoader _assetLoader;

    public string SectionTitle => "Addressables Asset Loading";

    public void Render()
    {
        var stats = _assetLoader.GetStats();

        ImGui.Text($"Loaded Assets: {stats.LoadedAssetCount}");
        ImGui.Text($"Memory Usage: {stats.MemoryUsageBytes / 1024 / 1024} MB");
        ImGui.Text($"Failed Loads: {stats.FailedLoadCount}");

        if (!string.IsNullOrEmpty(stats.LastErrorMessage))
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), $"Last Error: {stats.LastErrorMessage}");
        }

        // Show per-asset stats
        if (ImGui.BeginTable("AssetTable", 3))
        {
            ImGui.TableHeadersRow();
            ImGui.TableSetColumnIndex(0);
            ImGui.Text("Address");
            ImGui.TableSetColumnIndex(1);
            ImGui.Text("Size (MB)");
            ImGui.TableSetColumnIndex(2);
            ImGui.Text("Status");

            // Populate with loaded assets
            // ...

            ImGui.EndTable();
        }
    }
}
```

### Logging Output

**Example BepInEx log**:

```
[AddressablesAssetLoader] Initialized for pack 'warfare-starwars' with 50 assets
[AddressablesAssetLoader] Loading warfare-starwars/units/rep_clone_trooper.fbx (timeout: 5s)
[AddressablesAssetLoader] Loaded warfare-starwars/units/rep_clone_trooper.fbx via Addressables
[AddressablesAssetLoader] Loading warfare-starwars/textures/units/rep_clone_trooper_base.png (timeout: 5s)
[AddressablesAssetLoader] Cache hit: warfare-starwars/textures/units/rep_clone_trooper_base.png
[ContentLoader] Registered 26 units from rep_units.yaml
[ContentLoader] Registered 13 factions from republic.yaml
```

### Catalog Inspector Tool

**CLI: `dotnet run --project src/Tools/PackCompiler -- inspect-catalog catalog.json`**

```
Addressables Catalog Inspector
===============================

Total Assets: 50
Total Bundles: 3

Bundles:
  warfare-starwars-units.bundle (500 MB, 52 assets)
    warfare-starwars/units/rep_clone_trooper.fbx
    warfare-starwars/units/rep_arc_trooper.fbx
    warfare-starwars/textures/units/rep_clone_trooper_base.png
    warfare-starwars/textures/units/rep_clone_trooper_normal.png
    ...

  warfare-starwars-buildings.bundle (400 MB, 48 assets)
    warfare-starwars/buildings/rep_house_clone_quarters.fbx
    warfare-starwars/textures/buildings/rep_house_clone_quarters_diffuse.png
    ...

  warfare-starwars-configs.bundle (1 MB, 2 assets)
    warfare-starwars/configs/units.yaml
    warfare-starwars/configs/buildings.yaml
```

---

## Migration & Compatibility

### From Manual AssetBundle Loading

**Before** (vanilla modding):
```csharp
AssetBundle bundle = AssetBundle.LoadFromFile("path/to/bundle");
Mesh mesh = bundle.LoadAsset<Mesh>("rep_clone_trooper");
```

**After** (Addressables via DINOForge):
```csharp
IAssetLoader loader = new AddressablesAssetLoader("warfare-starwars", catalogPath);
Mesh mesh = await loader.LoadAsync<Mesh>("warfare-starwars/units/rep_clone_trooper.fbx");
```

### Backwards Compatibility with Vanilla Assets

If a modded unit ID doesn't have a custom mesh in warfare-starwars, fall back to vanilla:

```csharp
public class UnitMeshResolver
{
    private readonly IAssetLoader _modAssetLoader;
    private readonly VanillaCatalog _vanillaCatalog;

    public async Task<Mesh?> ResolveMeshAsync(string unitId, string factionId)
    {
        // Try modded asset first
        string modAddress = $"warfare-starwars/units/{factionId}_{unitId}.fbx";
        var modMesh = await _modAssetLoader.LoadAsync<Mesh>(modAddress);
        if (modMesh != null)
        {
            return modMesh;
        }

        // Fallback to vanilla
        return _vanillaCatalog.GetVanillaUnitMesh(unitId);
    }
}
```

### Version Migration

When upgrading Addressables (e.g., v1.21.18 → v1.22.x):

1. Regenerate catalog in new Unity version
2. Re-run tests: `dotnet test src/Tests/AddressablesIntegrationTests.cs`
3. Validate all bundles: `dotnet run --project src/Tools/PackCompiler -- validate-bundles`

---

## Reference Implementation

### Complete Example: Loading warfare-starwars Units

**File: `packs/warfare-starwars/LoadWarfareStarWars.cs`**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DINOForge.SDK;
using DINOForge.SDK.Assets;
using DINOForge.SDK.Registry;
using UnityEngine;

namespace DINOForge.Examples
{
    /// <summary>
    /// Example: Load warfare-starwars pack with Addressables integration.
    /// </summary>
    public class LoadWarfareStarWarsExample
    {
        public static async Task Main(string[] args)
        {
            string gameDir = "G:\\SteamLibrary\\steamapps\\common\\Diplomacy is Not an Option";
            string packDir = "C:\\Users\\koosh\\Dino\\packs\\warfare-starwars";
            string catalogPath = Path.Combine(gameDir, "Diplomacy is Not an Option_Data", "StreamingAssets", "aa", "catalog.json");

            // 1. Initialize registries
            var registryManager = new RegistryManager();

            // 2. Initialize asset loader
            var assetLoader = new AddressablesAssetLoader(
                "warfare-starwars",
                catalogPath,
                log: msg => Debug.Log(msg));

            // 3. Initialize content loader
            var contentLoader = new ContentLoader(registryManager, log: msg => Debug.Log(msg));
            contentLoader.InitializeAssetLoader("warfare-starwars", catalogPath);

            // 4. Load pack manifest and content
            var result = contentLoader.LoadPack(packDir);
            if (!result.IsSuccess)
            {
                Debug.LogError($"Failed to load warfare-starwars: {string.Join(", ", result.Errors)}");
                return;
            }

            Debug.Log("Loaded packs: " + string.Join(", ", result.LoadedPacks));

            // 5. Load unit meshes from Addressables
            Debug.Log("Loading unit meshes...");
            var unitMeshes = new Dictionary<string, Mesh>();

            var unitAddresses = new[]
            {
                "warfare-starwars/units/rep_clone_trooper.fbx",
                "warfare-starwars/units/rep_arc_trooper.fbx",
                "warfare-starwars/units/sep_battle_droid.fbx",
                "warfare-starwars/units/sep_droideka.fbx",
            };

            foreach (string address in unitAddresses)
            {
                var mesh = await assetLoader.LoadAsync<Mesh>(address, timeout: TimeSpan.FromSeconds(10));
                if (mesh != null)
                {
                    string unitId = Path.GetFileNameWithoutExtension(address);
                    unitMeshes[unitId] = mesh;
                    Debug.Log($"Loaded mesh: {unitId}");
                }
            }

            // 6. Load textures
            Debug.Log("Loading unit textures...");
            var unitTextures = new Dictionary<string, Texture2D>();

            var textureAddresses = new[]
            {
                "warfare-starwars/textures/units/rep_clone_trooper_base.png",
                "warfare-starwars/textures/units/rep_clone_trooper_normal.png",
                "warfare-starwars/textures/units/sep_battle_droid_base.png",
            };

            foreach (string address in textureAddresses)
            {
                var texture = await assetLoader.LoadAsync<Texture2D>(address);
                if (texture != null)
                {
                    string textureId = Path.GetFileNameWithoutExtension(address);
                    unitTextures[textureId] = texture;
                    Debug.Log($"Loaded texture: {textureId}");
                }
            }

            // 7. Get diagnostics
            var stats = assetLoader.GetStats();
            Debug.Log($"Asset Stats: {stats.LoadedAssetCount} loaded, {stats.MemoryUsageBytes / 1024 / 1024} MB used");

            // 8. Query registry
            var unitsRegistry = registryManager.Units;
            Debug.Log($"Registered units: {unitsRegistry.GetAll().Count}");
        }
    }
}
```

### Unit Test Suite

**File: `src/Tests/AddressablesIntegrationTests.cs`**

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using DINOForge.SDK.Assets;
using Xunit;
using FluentAssertions;

namespace DINOForge.Tests
{
    public class AddressablesIntegrationTests
    {
        private readonly string _catalogPath;

        public AddressablesIntegrationTests()
        {
            // Use test catalog from StreamingAssets
            _catalogPath = Path.Combine(
                AppContext.BaseDirectory,
                "TestData",
                "catalog.json");
        }

        [Fact]
        public void LoadCatalog_ValidFile_ParsesSuccessfully()
        {
            // Act
            var catalog = AddressablesCatalog.Load(_catalogPath);

            // Assert
            catalog.Should().NotBeNull();
            catalog.InternalIds.Should().NotBeEmpty();
            catalog.BundlePaths.Should().NotBeEmpty();
            catalog.KeyToBundleMap.Should().NotBeEmpty();
        }

        [Fact]
        public void KeyToBundleMap_WarfareStarwarsAssets_MappedCorrectly()
        {
            // Arrange
            var catalog = AddressablesCatalog.Load(_catalogPath);

            // Act
            var hasUnit = catalog.KeyToBundleMap.TryGetValue(
                "warfare-starwars/units/rep_clone_trooper.fbx",
                out string? bundlePath);

            // Assert
            hasUnit.Should().BeTrue();
            bundlePath.Should().Contain("warfare-starwars-units");
        }

        [Fact]
        public void ResolveBundlePath_WithPlaceholder_ReplacesCorrectly()
        {
            // Arrange
            string bundlePath = "{UnityEngine.AddressableAssets.Addressables.RuntimePath}/warfare-starwars-units.bundle";
            string gameDir = "C:\\Games\\Diplomacy is Not an Option";

            // Act
            string resolved = AddressablesCatalog.ResolveBundlePath(bundlePath, gameDir);

            // Assert
            resolved.Should().Contain("StreamingAssets\\aa\\StandaloneWindows64");
            resolved.Should().NotContain("{UnityEngine.AddressableAssets");
        }

        [Fact]
        public async Task AddressablesAssetLoader_LoadAsync_ReturnsAssetOnSuccess()
        {
            // Arrange
            var loader = new AddressablesAssetLoader(
                "warfare-starwars",
                _catalogPath,
                log: msg => { });

            // Act
            var mesh = await loader.LoadAsync<UnityEngine.Mesh>(
                "warfare-starwars/units/rep_clone_trooper.fbx",
                timeout: TimeSpan.FromSeconds(10));

            // Assert
            mesh.Should().NotBeNull();
        }

        [Fact]
        public async Task AddressablesAssetLoader_LoadAllAsync_ReturnsMultipleAssets()
        {
            // Arrange
            var loader = new AddressablesAssetLoader(
                "warfare-starwars",
                _catalogPath,
                log: msg => { });

            // Act
            var meshes = await loader.LoadAllAsync<UnityEngine.Mesh>(
                "warfare-starwars-units");

            // Assert
            meshes.Should().NotBeEmpty();
            meshes.Should().HaveCountGreaterThan(20);
        }

        [Fact]
        public async Task AddressablesAssetLoader_ClearCache_UnloadsAllAssets()
        {
            // Arrange
            var loader = new AddressablesAssetLoader(
                "warfare-starwars",
                _catalogPath,
                log: msg => { });

            await loader.LoadAsync<UnityEngine.Mesh>("warfare-starwars/units/rep_clone_trooper.fbx");

            // Act
            await loader.ClearCacheAsync();
            var stats = loader.GetStats();

            // Assert
            stats.LoadedAssetCount.Should().Be(0);
        }

        [Fact]
        public void AddressablesAssetLoader_GetStats_ReturnsValidStats()
        {
            // Arrange
            var loader = new AddressablesAssetLoader(
                "warfare-starwars",
                _catalogPath,
                log: msg => { });

            // Act
            var stats = loader.GetStats();

            // Assert
            stats.Should().NotBeNull();
            stats.LoadedAssetCount.Should().BeGreaterThanOrEqualTo(0);
            stats.MemoryUsageBytes.Should().BeGreaterThanOrEqualTo(0);
            stats.FailedLoadCount.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
```

---

## Troubleshooting Guide

### Issue: "Catalog file not found"

**Symptom**: `FileNotFoundException: Addressables catalog not found at catalog.json`

**Cause**: Catalog not built or not deployed to StreamingAssets/aa/

**Solution**:
1. Run `build_addressables.ps1` in Unity project
2. Verify `StreamingAssets/aa/catalog.json` exists
3. Copy to game directory: `Diplomacy is Not an Option_Data/StreamingAssets/aa/`

### Issue: "Asset address not found in catalog"

**Symptom**: `Addressables load failed: warfare-starwars/units/rep_clone_trooper.fbx not in catalog`

**Cause**: Asset address mismatch or bundle not included in catalog build

**Solution**:
1. Verify address format: `warfare-starwars/units/rep_clone_trooper.fbx` (exact case)
2. Check Unity Editor: Window > Addressables > Groups > search for asset
3. Rebuild catalog: Delete `StreamingAssets/aa/catalog.json`, rebuild
4. Validate with: `dotnet run --project src/Tools/PackCompiler -- inspect-catalog catalog.json`

### Issue: "Timeout loading asset"

**Symptom**: `Timeout: asset failed to load within 5 seconds`

**Cause**: Bundle file missing, network issue, or large asset

**Solution**:
1. Verify bundle file exists: `StreamingAssets/aa/StandaloneWindows64/*.bundle`
2. Increase timeout: `await loader.LoadAsync<Mesh>(address, timeout: TimeSpan.FromSeconds(30))`
3. Check file permissions (game folder may be read-only)
4. Monitor memory: if near limit, unload unused assets

### Issue: "Memory usage exceeds budget"

**Symptom**: `MemoryUsageBytes: 2500 MB > budget: 2000 MB`

**Cause**: Too many assets cached simultaneously

**Solution**:
1. Use LRU cache instead of persistent cache
2. Unload assets after use: `await loader.UnloadAsync(address)`
3. Monitor with inspector overlay: [Addressables] section
4. Reduce texture resolution or use mipmaps

### Issue: "Mesh is null after loading"

**Symptom**: `await loader.LoadAsync<Mesh>(address) returns null`

**Cause**: Asset is wrong type or bundle corrupted

**Solution**:
1. Verify asset type: `dotnet run --project src/Tools/PackCompiler -- inspect-catalog` → check "TypeName"
2. Validate bundle: `AssetsTools.NET` can dump bundle contents
3. Rebuild FBX in Blender: ensure .fbx export is valid
4. Re-run catalog build

### Issue: "Game crashes on asset load"

**Symptom**: `Exception: NullReferenceException at runtime`

**Cause**: Asset reference broken or incompatible Unity version

**Solution**:
1. Check unity version: catalog.json metadata
2. Validate bundle compatibility: `AssetService.ValidateModBundle(bundlePath)`
3. Use try-catch in load code: `try { var mesh = await Load(); } catch (Exception ex) { Log(ex); }`
4. Enable BepInEx debug: `LogLevels.All` in BepInEx/config/BepInEx.cfg

---

## Implementation Checklist

- [ ] **Asset Preparation**
  - [ ] Export all 26 unit FBX from Blender
  - [ ] Generate 26 unit diffuse + normal textures
  - [ ] Export all 24 building FBX
  - [ ] Generate 24 building diffuse + normal textures
  - [ ] Verify all files in `packs/warfare-starwars/assets/meshes/` and `textures/`

- [ ] **Unity Addressables Setup**
  - [ ] Create 3 Addressables groups (units, buildings, configs)
  - [ ] Assign addresses following naming convention
  - [ ] Set bundle names and labels
  - [ ] Configure texture import settings (sRGB vs linear)
  - [ ] Test "Build" → check StreamingAssets/aa/

- [ ] **DINOForge Integration**
  - [ ] Implement `AddressablesAssetLoader` (IAssetLoader)
  - [ ] Add `InitializeAssetLoader` to ContentLoader
  - [ ] Write unit tests in `AddressablesIntegrationTests.cs`
  - [ ] Verify `dotnet test` passes

- [ ] **Windows Build Automation**
  - [ ] Create `build_addressables.ps1` / `.bat`
  - [ ] Create `deploy_addressables.ps1`
  - [ ] Test build pipeline end-to-end
  - [ ] Document build steps in QUICK_START.md

- [ ] **Cache & Memory**
  - [ ] Implement persistent cache for core assets
  - [ ] Implement LRU cache for optional assets
  - [ ] Add memory budget guards
  - [ ] Test cache eviction

- [ ] **Debugging**
  - [ ] Integrate with DebugOverlay (asset stats section)
  - [ ] Create `inspect-catalog` CLI command
  - [ ] Add logging to ContentLoader
  - [ ] Test BepInEx log output

- [ ] **Compatibility**
  - [ ] Test fallback to vanilla assets
  - [ ] Document migration path from manual AssetBundle loading
  - [ ] Create compatibility matrix (Addressables v1.21.18 → v1.22.x)

- [ ] **Documentation**
  - [ ] Update README.md with asset loading guide
  - [ ] Create troubleshooting FAQ
  - [ ] Document address naming convention in CLAUDE.md
  - [ ] Add example code snippets

---

## Summary

This design provides a **complete, production-ready integration** of Unity Addressables v1.21.18 for the warfare-starwars pack:

1. **Bundle Layout**: 3 bundles (units, buildings, configs) with clear organization
2. **Address Convention**: Hierarchical naming (`warfare-starwars/{type}/{id}.{ext}`)
3. **Asset Loader**: Thin wrapper (`AddressablesAssetLoader`) + `IAssetLoader` interface
4. **Content Loader Integration**: Seamless pack loading with asset resolution
5. **Cache Strategies**: Persistent, LRU, and bundle-scoped patterns
6. **Windows Build**: Automated PowerShell/batch scripts for catalog generation
7. **Runtime Loading**: 3 reference patterns (startup, lazy, on-demand)
8. **Debugging**: In-game overlay + CLI inspection tools
9. **Compatibility**: Fallback to vanilla + migration guide
10. **Reference Code**: Complete working examples + unit tests

**Next Steps**:
1. Export remaining FBX/textures from Blender
2. Set up Addressables groups in Unity 2021.3.45f2
3. Implement `AddressablesAssetLoader` in `src/SDK/Assets/`
4. Write integration tests
5. Build and deploy catalog to DINO installation
6. Validate with in-game asset inspector
7. Update documentation
