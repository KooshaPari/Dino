using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK
{
    /// <summary>
    /// Strongly-typed representation of a DINOForge pack manifest (pack.yaml).
    /// Corresponds to schemas/pack-manifest.schema.yaml.
    /// </summary>
    public class PackManifest
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "name")]
        public string Name { get; set; } = "";

        [YamlMember(Alias = "version")]
        public string Version { get; set; } = "0.1.0";

        [YamlMember(Alias = "framework_version")]
        public string FrameworkVersion { get; set; } = ">=0.1.0";

        [YamlMember(Alias = "author")]
        public string Author { get; set; } = "";

        [YamlMember(Alias = "type")]
        public string Type { get; set; } = "content";

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        [YamlMember(Alias = "depends_on")]
        public List<string> DependsOn { get; set; } = new List<string>();

        [YamlMember(Alias = "conflicts_with")]
        public List<string> ConflictsWith { get; set; } = new List<string>();

        [YamlMember(Alias = "load_order")]
        public int LoadOrder { get; set; } = 100;

        [YamlMember(Alias = "game_version")]
        public string? GameVersion { get; set; }

        [YamlMember(Alias = "loads")]
        public PackLoads? Loads { get; set; }

        [YamlMember(Alias = "overrides")]
        public PackOverrides? Overrides { get; set; }
    }

    public class PackLoads
    {
        [YamlMember(Alias = "factions")]
        public List<string>? Factions { get; set; }

        [YamlMember(Alias = "units")]
        public List<string>? Units { get; set; }

        [YamlMember(Alias = "buildings")]
        public List<string>? Buildings { get; set; }

        [YamlMember(Alias = "weapons")]
        public List<string>? Weapons { get; set; }

        [YamlMember(Alias = "doctrines")]
        public List<string>? Doctrines { get; set; }

        [YamlMember(Alias = "audio")]
        public List<string>? Audio { get; set; }

        [YamlMember(Alias = "visuals")]
        public List<string>? Visuals { get; set; }

        [YamlMember(Alias = "localization")]
        public List<string>? Localization { get; set; }

        [YamlMember(Alias = "wave_templates")]
        public List<string>? WaveTemplates { get; set; }

        [YamlMember(Alias = "tech_nodes")]
        public List<string>? TechNodes { get; set; }

        [YamlMember(Alias = "scenarios")]
        public List<string>? Scenarios { get; set; }
    }

    public class PackOverrides
    {
        [YamlMember(Alias = "units")]
        public List<string>? Units { get; set; }

        [YamlMember(Alias = "buildings")]
        public List<string>? Buildings { get; set; }

        [YamlMember(Alias = "stats")]
        public List<string>? Stats { get; set; }
    }
}
