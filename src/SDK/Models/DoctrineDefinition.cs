using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Stub model for a DINOForge doctrine definition (doctrines/*.yaml).
    /// </summary>
    public class DoctrineDefinition
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "display_name")]
        public string DisplayName { get; set; } = "";

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Faction archetype this doctrine is designed for
        /// (e.g. order, industrial_swarm, asymmetric, custom).
        /// </summary>
        [YamlMember(Alias = "faction_archetype")]
        public string? FactionArchetype { get; set; }

        /// <summary>
        /// Flat key/value modifiers applied by this doctrine.
        /// Keys are stat or system identifiers; values are multiplier or additive amounts.
        /// </summary>
        [YamlMember(Alias = "modifiers")]
        public Dictionary<string, float> Modifiers { get; set; } = new Dictionary<string, float>();
    }
}
