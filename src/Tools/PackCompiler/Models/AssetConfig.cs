#nullable enable
using System;
using System.Collections.Generic;

namespace DINOForge.Tools.PackCompiler.Models
{
    /// <summary>
    /// Asset pipeline configuration loaded from asset_pipeline.yaml.
    /// Defines all model imports, LOD targets, materials, and build outputs.
    /// </summary>
    public class AssetPipelineConfig
    {
        public AssetPipelineConfig() { }

        public required string Version { get; init; }
        public required string PackId { get; init; }
        public required string TargetUnityVersion { get; init; }

        public required AssetSettings AssetSettings { get; init; }
        public required Dictionary<string, MaterialDefinition> Materials { get; init; }
        public required Dictionary<string, AssetPhase> Phases { get; init; }
        public required BuildConfig Build { get; init; }
    }

    /// <summary>Global asset pipeline settings</summary>
    public class AssetSettings
    {
        public required string BasePath { get; init; }
        public required string OutputPath { get; init; }
        public string MaterialsPath { get; init; } = "materials";
        public string TextureQuality { get; init; } = "high";  // low | medium | high
        public string LODStrategy { get; init; } = "aggressive";  // aggressive | balanced | conservative
    }

    /// <summary>Material definition with faction colors</summary>
    public class MaterialDefinition
    {
        public required string Faction { get; init; }
        public required string BaseColor { get; init; }  // #RRGGBB
        public required string EmissionColor { get; init; }
        public required float EmissionIntensity { get; init; }
        public float Roughness { get; init; } = 0.5f;
        public float Metallic { get; init; } = 0f;
    }

    /// <summary>Asset import phase (v0.7.0, v0.8.0, etc.)</summary>
    public class AssetPhase
    {
        public required string Description { get; init; }
        public required List<AssetDefinition> Models { get; init; }
    }

    /// <summary>Individual asset definition</summary>
    public class AssetDefinition
    {
        public required string Id { get; init; }
        public required string File { get; init; }
        public required string Type { get; init; }  // infantry | hero | heavy | vehicle | building | etc.
        public required string Faction { get; init; }
        public required int PolyCountTarget { get; init; }

        public float Scale { get; init; } = 1.0f;

        public required LODDefinition LOD { get; init; }
        public required string Material { get; init; }
        public required string AddressableKey { get; init; }
        public required string OutputPrefab { get; init; }

        public DefinitionUpdateConfig? UpdateDefinition { get; init; }
        public Dictionary<string, object>? Metadata { get; init; }
    }

    /// <summary>LOD generation configuration</summary>
    public class LODDefinition
    {
        public bool Enabled { get; init; } = true;
        public required List<int> Levels { get; init; }  // [100, 60, 30]
        public List<int>? ScreenSizes { get; init; }  // [100, 50, 20]
    }

    /// <summary>Game definition auto-update configuration</summary>
    public class DefinitionUpdateConfig
    {
        public bool Enabled { get; init; } = false;
        public string? File { get; init; }
        public string? Id { get; init; }
        public string? Field { get; init; }  // visual_asset
    }

    /// <summary>Build output configuration</summary>
    public class BuildConfig
    {
        public required string OutputDirectory { get; init; }
        public required string AddressablesOutput { get; init; }
        public string? LogFile { get; init; }
        public bool GenerateHtmlReport { get; init; } = false;
        public PerformanceTargets? PerformanceTargets { get; init; }
    }

    /// <summary>Performance/timing targets</summary>
    public class PerformanceTargets
    {
        public int ImportTimeSeconds { get; init; } = 5;
        public int LODGenerationSeconds { get; init; } = 10;
        public int PrefabGenerationSeconds { get; init; } = 2;
        public int TotalPipelineSeconds { get; init; } = 120;
    }
}
