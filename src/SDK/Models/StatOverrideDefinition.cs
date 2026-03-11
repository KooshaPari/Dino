#nullable enable

using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Represents a collection of stat overrides loaded from a pack's stats YAML file.
    /// Each override targets a specific model path and applies a value modification.
    /// </summary>
    public class StatOverrideDefinition
    {
        /// <summary>
        /// The list of stat override entries to apply.
        /// </summary>
        [YamlMember(Alias = "overrides")]
        public List<StatOverrideEntry> Overrides { get; set; } = new List<StatOverrideEntry>();
    }

    /// <summary>
    /// A single stat override entry that targets a specific model path
    /// and applies a value using the specified mode.
    /// </summary>
    public class StatOverrideEntry
    {
        /// <summary>
        /// SDK model path to target (e.g. "unit.stats.hp").
        /// </summary>
        [YamlMember(Alias = "target")]
        public string Target { get; set; } = "";

        /// <summary>
        /// The numeric value to apply.
        /// </summary>
        [YamlMember(Alias = "value")]
        public float Value { get; set; }

        /// <summary>
        /// How the value is applied: "override" replaces, "add" adds, "multiply" multiplies.
        /// Defaults to "override".
        /// </summary>
        [YamlMember(Alias = "mode")]
        public string Mode { get; set; } = "override";

        /// <summary>
        /// Optional ECS component filter (e.g. "Components.MeleeUnit").
        /// When set, the override only applies to entities matching this filter.
        /// </summary>
        [YamlMember(Alias = "filter")]
        public string? Filter { get; set; }
    }
}
