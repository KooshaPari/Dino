#nullable enable
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Manifest for a total conversion pack — replaces all vanilla game content
    /// with themed alternatives (e.g. Star Wars, Modern Warfare).
    /// </summary>
    public class TotalConversionManifest
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string Version { get; set; } = "0.1.0";
        public string Type { get; set; } = "total_conversion";
        public string Author { get; set; } = "";
        public string? Theme { get; set; }
        public string? Description { get; set; }

        [YamlMember(Alias = "framework_version")]
        public string FrameworkVersion { get; set; } = "*";

        /// <summary>Maps vanilla faction IDs to replacement faction IDs.</summary>
        [YamlMember(Alias = "replaces_vanilla")]
        public Dictionary<string, string> ReplacesVanilla { get; set; } = new();

        public List<TcFactionEntry> Factions { get; set; } = new();

        [YamlMember(Alias = "asset_replacements")]
        public TcAssetReplacements AssetReplacements { get; set; } = new();

        [YamlMember(Alias = "conflicts_with")]
        public List<string> ConflictsWith { get; set; } = new();

        /// <summary>If true, only one total conversion can be active at a time.</summary>
        public bool Singleton { get; set; } = true;
    }

    /// <summary>A single faction replacement entry in a total conversion.</summary>
    public class TcFactionEntry
    {
        public string Id { get; set; } = "";
        /// <summary>Vanilla faction ID being replaced.</summary>
        public string Replaces { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Theme { get; set; }
        public string Archetype { get; set; } = "custom";
        public List<string> Units { get; set; } = new();
        public List<string> Buildings { get; set; } = new();
    }

    /// <summary>Asset replacement mappings for a total conversion.</summary>
    public class TcAssetReplacements
    {
        public Dictionary<string, string> Textures { get; set; } = new();
        public Dictionary<string, string> Audio { get; set; } = new();
        public Dictionary<string, string> Ui { get; set; } = new();
    }
}
