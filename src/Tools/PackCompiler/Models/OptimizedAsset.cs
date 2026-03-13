#nullable enable
using System;
using System.Collections.Generic;

namespace DINOForge.Tools.PackCompiler.Models
{
    /// <summary>
    /// Optimized asset with LOD variants and compressed textures.
    /// Ready for prefab generation and Addressables integration.
    /// </summary>
    public class OptimizedAsset
    {
        /// <summary>Asset identifier</summary>
        public required string AssetId { get; init; }

        /// <summary>LOD0: Full detail mesh</summary>
        public required MeshData LOD0 { get; init; }

        /// <summary>LOD1: 60% detail mesh (medium distance)</summary>
        public required MeshData LOD1 { get; init; }

        /// <summary>LOD2: 30% detail mesh (far distance)</summary>
        public required MeshData LOD2 { get; init; }

        /// <summary>Screen size percentage thresholds for LOD transitions</summary>
        public LODScreenSize ScreenSizes { get; init; } = new();

        /// <summary>Optimized materials</summary>
        public List<MaterialData> Materials { get; init; } = new();

        /// <summary>Skeleton data (if present)</summary>
        public SkeletonData? Skeleton { get; init; }

        /// <summary>Optimization metadata</summary>
        public OptimizationMetadata Metadata { get; init; } = new();

        /// <summary>Timestamp of optimization</summary>
        public DateTime OptimizedAt { get; init; } = DateTime.UtcNow;
    }

    /// <summary>LOD screen size configuration</summary>
    public class LODScreenSize
    {
        /// <summary>LOD0 shown from 100% to this % of screen</summary>
        public int LOD0Max { get; set; } = 100;

        /// <summary>LOD0 shown from this % (start) to LOD1Max</summary>
        public int LOD0Min { get; set; } = 50;

        /// <summary>LOD1 shown from this % to this % of screen</summary>
        public int LOD1Min { get; set; } = 50;
        public int LOD1Max { get; set; } = 20;

        /// <summary>LOD2 shown below this % of screen</summary>
        public int LOD2Min { get; set; } = 20;

        /// <summary>Transition time in seconds</summary>
        public float TransitionTime { get; set; } = 0.5f;
    }

    /// <summary>Optimization results and metrics</summary>
    public class OptimizationMetadata
    {
        /// <summary>Original polycount</summary>
        public int OriginalPolyCount { get; set; }

        /// <summary>LOD0 polycount (should be ~100% of original)</summary>
        public int LOD0PolyCount { get; set; }

        /// <summary>LOD1 polycount (target 60%)</summary>
        public int LOD1PolyCount { get; set; }

        /// <summary>LOD2 polycount (target 30%)</summary>
        public int LOD2PolyCount { get; set; }

        /// <summary>LOD1 quality: actual % of original</summary>
        public float LOD1Quality => LOD0PolyCount > 0 ? (LOD1PolyCount / (float)LOD0PolyCount) * 100f : 0f;

        /// <summary>LOD2 quality: actual % of original</summary>
        public float LOD2Quality => LOD0PolyCount > 0 ? (LOD2PolyCount / (float)LOD0PolyCount) * 100f : 0f;

        /// <summary>Optimization method used</summary>
        public string? OptimizationMethod { get; set; } = "FastQuadricMeshSimplifier";

        /// <summary>Total optimization time in seconds</summary>
        public double OptimizationTimeSeconds { get; set; }

        /// <summary>Any warnings/notes from optimization</summary>
        public List<string> Notes { get; init; } = new();
    }
}
