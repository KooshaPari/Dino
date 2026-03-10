using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Strongly-typed representation of a DINOForge building definition (buildings/*.yaml).
    /// </summary>
    public class BuildingDefinition
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "display_name")]
        public string DisplayName { get; set; } = "";

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Functional category of this building
        /// (e.g. barracks, economy, defense, research, command).
        /// </summary>
        [YamlMember(Alias = "building_type")]
        public string? BuildingType { get; set; }

        [YamlMember(Alias = "cost")]
        public ResourceCost Cost { get; set; } = new ResourceCost();

        /// <summary>
        /// Total hit points of the building.
        /// </summary>
        [YamlMember(Alias = "health")]
        public int Health { get; set; } = 0;

        /// <summary>
        /// Production rates for this building.
        /// Keys are resource or unit identifiers; values are production amounts per tick.
        /// </summary>
        [YamlMember(Alias = "production")]
        public Dictionary<string, int> Production { get; set; } = new Dictionary<string, int>();
    }
}
