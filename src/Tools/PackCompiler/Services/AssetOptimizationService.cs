#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DINOForge.Tools.PackCompiler.Models;

namespace DINOForge.Tools.PackCompiler.Services
{
    /// <summary>
    /// Service for creating LOD metadata from imported assets.
    /// For Week 1 (v0.7.0), LOD generation is deferred to Unity Editor via Addressables.
    /// This service prepares the asset structure and validates LOD configuration.
    /// </summary>
    public class AssetOptimizationService
    {
        private readonly AssetValidationService _validationService = new();

        /// <summary>
        /// Prepare an imported asset for LOD generation.
        /// For Week 1, LOD variants are generated during Unity prefab creation.
        /// This validates LOD config and prepares metadata.
        /// </summary>
        public async Task<OptimizedAsset> PrepareForLODAsync(ImportedAsset asset, AssetDefinition definition)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                // Validate input
                var validationResult = _validationService.ValidateImportedAsset(asset, definition);
                if (!validationResult.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Asset validation failed:\n{string.Join("\n", validationResult.Errors)}"
                    );
                }

                var lodLevels = definition.LOD.Levels;
                if (lodLevels == null || lodLevels.Count < 3)
                {
                    throw new ArgumentException("LOD levels must specify at least 3 levels (100, 60, 30)");
                }

                stopwatch.Stop();

                // For Week 1, LOD0 is the original, LOD1/LOD2 will be generated in Unity Editor
                // This prepares the metadata structure for later processing
                var optimized = new OptimizedAsset
                {
                    AssetId = asset.AssetId,
                    LOD0 = asset.Mesh,
                    LOD1 = CreatePlaceholderLOD(asset.Mesh, lodLevels[1] / 100f),
                    LOD2 = CreatePlaceholderLOD(asset.Mesh, lodLevels[2] / 100f),
                    Materials = asset.Materials,
                    Skeleton = asset.Skeleton,
                    Metadata = new OptimizationMetadata
                    {
                        OriginalPolyCount = asset.Mesh.TriangleCount,
                        LOD0PolyCount = asset.Mesh.TriangleCount,
                        LOD1PolyCount = (int)(asset.Mesh.TriangleCount * lodLevels[1] / 100f),
                        LOD2PolyCount = (int)(asset.Mesh.TriangleCount * lodLevels[2] / 100f),
                        OptimizationMethod = "Deferred to Unity Editor",
                        OptimizationTimeSeconds = stopwatch.Elapsed.TotalSeconds,
                        Notes = new() { "LOD generation deferred to v0.8.0 with external tool integration" }
                    },
                    ScreenSizes = GenerateScreenSizes(definition)
                };

                return optimized;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                throw new InvalidOperationException(
                    $"Failed to prepare asset '{asset.AssetId}' after {stopwatch.Elapsed.TotalSeconds:F2}s: {ex.Message}",
                    ex
                );
            }
        }

        /// <summary>
        /// Create a placeholder LOD mesh (will be replaced by actual LOD in Unity Editor).
        /// </summary>
        private MeshData CreatePlaceholderLOD(MeshData source, float qualityFactor)
        {
            // Simple placeholder: same mesh with name indicating it needs processing
            int targetTriangleCount = (int)(source.TriangleCount * qualityFactor);

            return new MeshData
            {
                Name = $"{source.Name}_LOD_{(int)(qualityFactor * 100)}",
                Vertices = source.Vertices,
                Indices = source.Indices,
                Normals = source.Normals,
                UVs = source.UVs,
                BoneWeights = source.BoneWeights,
                Bounds = source.Bounds
            };
        }
        /// <summary>
        /// Generate LOD screen size configuration from asset definition.
        /// </summary>
        private LODScreenSize GenerateScreenSizes(AssetDefinition definition)
        {
            var screenSizes = definition.LOD.ScreenSizes;

            if (screenSizes != null && screenSizes.Count >= 3)
            {
                return new LODScreenSize
                {
                    LOD0Max = 100,
                    LOD0Min = screenSizes[0],
                    LOD1Min = screenSizes[0],
                    LOD1Max = screenSizes[1],
                    LOD2Min = screenSizes[1]
                };
            }

            // Default screen sizes
            return new LODScreenSize
            {
                LOD0Max = 100,
                LOD0Min = 50,
                LOD1Min = 50,
                LOD1Max = 20,
                LOD2Min = 20
            };
        }
    }
}
