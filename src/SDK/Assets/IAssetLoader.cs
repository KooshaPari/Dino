using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DINOForge.SDK.Assets
{
    /// <summary>
    /// Abstraction for loading modded assets from various sources (Addressables, file system, remote, etc.).
    /// Implementations are source-agnostic and are typically provided by the Runtime layer (BepInEx plugin).
    /// Supports timeout, retry logic, and memory management patterns.
    ///
    /// This is a design specification interface. Actual implementation lives in Runtime/Assets/
    /// and depends on UnityEngine references.
    /// </summary>
    public interface IAssetLoader
    {
        /// <summary>
        /// Loads a single asset by address asynchronously.
        /// </summary>
        /// <param name="address">Asset address (e.g., "warfare-starwars/units/rep_clone_trooper.fbx").</param>
        /// <param name="timeout">Maximum wait time for loading (default 5 seconds). Null = infinite wait.</param>
        /// <returns>Loaded asset instance as object, or null if not found or timeout occurs.</returns>
        /// <exception cref="ArgumentNullException">Thrown when address is null or empty.</exception>
        /// <remarks>
        /// The loader maintains an internal cache of loaded assets. Subsequent calls with the same
        /// address will return the cached instance (unless explicitly unloaded).
        ///
        /// Implementations should:
        /// - Log all load attempts (success, failure, cache hits)
        /// - Enforce timeout with graceful fallback
        /// - Track failed loads in stats
        /// - Support IAssetLoader.GetStats() queries
        ///
        /// Generic constraint removed here to avoid Unity.Engine dependency in SDK.
        /// Runtime implementation will enforce type safety.
        /// </remarks>
        Task<object?> LoadAsync(string address, TimeSpan? timeout = null);

        /// <summary>
        /// Loads all assets matching a specific label or bundle identifier.
        /// </summary>
        /// <param name="label">Label to search (e.g., "warfare-starwars-units" bundle label).</param>
        /// <returns>Read-only list of loaded assets matching the label. Empty list if none found.</returns>
        /// <remarks>
        /// Useful for loading entire content groups at once (e.g., all unit meshes for a faction).
        /// Implementations may use bundle/label metadata from AddressablesCatalog to optimize the query.
        /// </remarks>
        Task<IReadOnlyList<object>> LoadAllAsync(string label);

        /// <summary>
        /// Unloads a single asset from memory, freeing associated resources.
        /// </summary>
        /// <param name="address">Asset address to unload (must match the original LoadAsync call).</param>
        /// <remarks>
        /// Safe to call multiple times for the same address (idempotent).
        /// If the asset is still referenced elsewhere, it may not be immediately freed
        /// by the runtime garbage collector.
        /// </remarks>
        Task UnloadAsync(string address);

        /// <summary>
        /// Clears all cached assets from memory, freeing all associated resources.
        /// </summary>
        /// <remarks>
        /// Use this when transitioning between game scenes or when memory pressure is high.
        /// This is a nuclear option; prefer UnloadAsync for selective unloading.
        /// After this call, all previous LoadAsync calls will return new instances on retry.
        /// </remarks>
        Task ClearCacheAsync();

        /// <summary>
        /// Returns diagnostic statistics about asset loader performance and memory usage.
        /// </summary>
        /// <returns>Snapshot of current loader statistics.</returns>
        /// <remarks>
        /// Used for debug overlays, memory monitoring, and performance profiling.
        /// </remarks>
        IAssetLoaderStats GetStats();
    }

    /// <summary>
    /// Diagnostic statistics for asset loader performance and memory usage.
    /// </summary>
    public interface IAssetLoaderStats
    {
        /// <summary>
        /// Total number of assets currently loaded and cached in memory.
        /// </summary>
        int LoadedAssetCount { get; }

        /// <summary>
        /// Approximate total memory used by all loaded assets (bytes).
        /// This is a rough estimate calculated from asset dimensions and types.
        /// </summary>
        long MemoryUsageBytes { get; }

        /// <summary>
        /// Cumulative count of failed load attempts since loader initialization.
        /// </summary>
        int FailedLoadCount { get; }

        /// <summary>
        /// Last error message encountered during asset loading (or null if no errors).
        /// Useful for debugging.
        /// </summary>
        string? LastErrorMessage { get; }
    }
}
