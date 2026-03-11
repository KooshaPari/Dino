#nullable enable
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Represents a Total Conversion pack manifest (type: total_conversion).
    /// A total conversion replaces all vanilla factions and assets with a themed mod.
    /// Only one TC pack may be active at a time (<see cref="Singleton"/> = true).
    /// Corresponds to schemas/total-conversion.schema.json.
    /// </summary>
    public class TotalConversionManifest
    {
        /// <summary>Unique pack identifier. Must match pattern ^[a-z][a-z0-9-]*$.</summary>
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        /// <summary>Human-readable display name for the TC pack.</summary>
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = "";

        /// <summary>Semantic version string for this pack (e.g. "0.1.0").</summary>
        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "";

        /// <summary>Pack type discriminator. Must always be "total_conversion".</summary>
        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "total_conversion";

        /// <summary>Author or team name for attribution.</summary>
        [YamlMember(Alias = "author")]
        public string Author { get; set; } = "";

        /// <summary>Optional thematic label for the pack (e.g. "sci-fi", "fantasy").</summary>
        [YamlMember(Alias = "theme")]
        public string? Theme { get; set; }

        /// <summary>Optional description of the TC pack's setting and scope.</summary>
        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Minimum DINOForge framework version required. Defaults to "*" (any version).
        /// Accepts semver range expressions such as ">=0.5.0".
        /// </summary>
        [YamlMember(Alias = "framework_version")]
        public string FrameworkVersion { get; set; } = "*";

        /// <summary>
        /// Whether this pack enforces singleton enforcement — only one TC pack may be active.
        /// Defaults to <c>true</c>. Setting to <c>false</c> is not recommended.
        /// </summary>
        [YamlMember(Alias = "singleton")]
        public bool Singleton { get; set; } = true;

        /// <summary>
        /// Maps vanilla faction IDs to the replacement mod faction IDs defined in this pack.
        /// Keys are vanilla IDs (e.g. "player", "enemy_classic", "enemy_guerrilla").
        /// Values are mod-defined faction IDs declared in <see cref="Factions"/>.
        /// </summary>
        [YamlMember(Alias = "replaces_vanilla")]
        public Dictionary<string, string> ReplacesVanilla { get; set; } = new Dictionary<string, string>();

        /// <summary>List of faction entries defined by this TC pack.</summary>
        [YamlMember(Alias = "factions")]
        public List<TcFactionEntry> Factions { get; set; } = new List<TcFactionEntry>();

        /// <summary>Asset replacement mappings for textures, audio, and UI elements.</summary>
        [YamlMember(Alias = "asset_replacements")]
        public TcAssetReplacements AssetReplacements { get; set; } = new TcAssetReplacements();

        /// <summary>
        /// List of pack IDs that conflict with this TC pack and cannot be loaded simultaneously.
        /// </summary>
        [YamlMember(Alias = "conflicts_with")]
        public List<string> ConflictsWith { get; set; } = new List<string>();
    }

    /// <summary>
    /// Describes a single faction introduced or redefined by a Total Conversion pack.
    /// </summary>
    public class TcFactionEntry
    {
        /// <summary>Unique faction identifier within this pack.</summary>
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        /// <summary>Human-readable name shown in-game for this faction.</summary>
        [YamlMember(Alias = "name")]
        public string Name { get; set; } = "";

        /// <summary>Optional flavor or lore text describing the faction.</summary>
        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// The vanilla faction ID this entry replaces (e.g. "player", "enemy_classic").
        /// </summary>
        [YamlMember(Alias = "replaces_vanilla")]
        public string? ReplacesVanilla { get; set; }
    }

    /// <summary>
    /// Holds asset replacement mappings for a Total Conversion pack.
    /// Each dictionary maps a vanilla asset path to the mod-supplied asset path.
    /// </summary>
    public class TcAssetReplacements
    {
        /// <summary>
        /// Texture replacements. Key = vanilla asset path, value = mod asset path.
        /// </summary>
        [YamlMember(Alias = "textures")]
        public Dictionary<string, string> Textures { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Audio replacements. Key = vanilla asset path, value = mod asset path.
        /// </summary>
        [YamlMember(Alias = "audio")]
        public Dictionary<string, string> Audio { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// UI element replacements. Key = vanilla asset path, value = mod asset path.
        /// </summary>
        [YamlMember(Alias = "ui")]
        public Dictionary<string, string> Ui { get; set; } = new Dictionary<string, string>();
    }
}
