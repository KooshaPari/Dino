#nullable enable
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DINOForge.Tools.Cli.Assetctl;

/// <summary>
/// Filter inputs for <see cref="AssetctlPipeline.Search"/>.
/// </summary>
public sealed class AssetctlSearchFilters
{
    /// <summary>
    /// Optional license filter value, e.g. <c>cc-by</c>.
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; set; }

    /// <summary>
    /// Minimum accepted polycount estimate.
    /// </summary>
    [JsonPropertyName("min_poly")]
    public int? MinPoly { get; set; }

    /// <summary>
    /// Maximum accepted polycount estimate.
    /// </summary>
    [JsonPropertyName("max_poly")]
    public int? MaxPoly { get; set; }
}

/// <summary>
/// Scored candidate result for search output.
/// </summary>
public sealed class RankedCandidate
{
    /// <summary>
    /// Candidate referenced by the search.
    /// </summary>
    [JsonPropertyName("candidate")]
    public AssetCandidate Candidate { get; set; } = new();

    /// <summary>
    /// Final combined score.
    /// </summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }

    /// <summary>
    /// Per-dimension score breakdown used for ranking.
    /// </summary>
    [JsonPropertyName("score_breakdown")]
    public Dictionary<string, double> ScoreBreakdown { get; set; } = new();
}

/// <summary>
/// Result payload for <see cref="AssetctlPipeline.Intake"/>.
/// </summary>
public sealed class AssetctlIntakeResult
{
    /// <summary>
    /// Indicates the command outcome.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Optional error or status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Assigned pipeline asset ID.
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Path to the generated manifest.
    /// </summary>
    [JsonPropertyName("manifest_path")]
    public string? ManifestPath { get; set; }

    /// <summary>
    /// Candidate that was consumed during intake.
    /// </summary>
    [JsonPropertyName("candidate")]
    public AssetCandidate? Candidate { get; set; }

    /// <summary>
    /// Directory containing raw intake artifacts.
    /// </summary>
    [JsonPropertyName("raw_dir")]
    public string? RawDir { get; set; }
}

/// <summary>
/// Result payload for <see cref="AssetctlPipeline.Normalize"/>.
/// </summary>
public sealed class AssetctlNormalizeResult
{
    /// <summary>
    /// Indicates the command outcome.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Optional error or status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Pipeline asset ID.
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Path to working directory.
    /// </summary>
    [JsonPropertyName("working_dir")]
    public string? WorkingDir { get; set; }

    /// <summary>
    /// Manifest path in the working directory.
    /// </summary>
    [JsonPropertyName("working_manifest_path")]
    public string? WorkingManifestPath { get; set; }
}

/// <summary>
/// Result payload for <see cref="AssetctlPipeline.Validate"/>.
/// </summary>
public sealed class AssetctlValidationResult
{
    /// <summary>
    /// Indicates the command outcome.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Optional error or status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Pipeline asset ID.
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Technical status after validation.
    /// </summary>
    [JsonPropertyName("technical_status")]
    public string? TechnicalStatus { get; set; }

    /// <summary>
    /// Validation errors.
    /// </summary>
    [JsonPropertyName("errors")]
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Path to validation report.
    /// </summary>
    [JsonPropertyName("validation_path")]
    public string? ValidationPath { get; set; }
}

/// <summary>
/// Result payload for <see cref="AssetctlPipeline.Stylize"/>.
/// </summary>
public sealed class AssetctlStylizeResult
{
    /// <summary>
    /// Indicates the command outcome.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Optional error or status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Pipeline asset ID.
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Stylization profile applied.
    /// </summary>
    [JsonPropertyName("profile")]
    public string? Profile { get; set; }

    /// <summary>
    /// Stylization metadata report path.
    /// </summary>
    [JsonPropertyName("stylize_report_path")]
    public string? StylizeReportPath { get; set; }
}

/// <summary>
/// Result payload for <see cref="AssetctlPipeline.Register"/>.
/// </summary>
public sealed class AssetctlRegisterResult
{
    /// <summary>
    /// Indicates the command outcome.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Optional error or status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Pipeline asset ID.
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Registry file path.
    /// </summary>
    [JsonPropertyName("registry_path")]
    public string? RegistryPath { get; set; }

    /// <summary>
    /// Total entries in registry after this operation.
    /// </summary>
    [JsonPropertyName("total_registered")]
    public int TotalRegistered { get; set; }
}

/// <summary>
/// Result payload for <see cref="AssetctlPipeline.ExportUnity"/>.
/// </summary>
public sealed class AssetctlExportResult
{
    /// <summary>
    /// Indicates the command outcome.
    /// </summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }

    /// <summary>
    /// Optional error or status message.
    /// </summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>
    /// Pipeline asset ID.
    /// </summary>
    [JsonPropertyName("asset_id")]
    public string? AssetId { get; set; }

    /// <summary>
    /// Target bundle.
    /// </summary>
    [JsonPropertyName("bundle")]
    public string? Bundle { get; set; }

    /// <summary>
    /// Export directory path.
    /// </summary>
    [JsonPropertyName("export_dir")]
    public string? ExportDir { get; set; }
}
