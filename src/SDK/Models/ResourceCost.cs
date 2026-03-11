using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Shared resource cost model used by units, buildings, and other definitions.
    /// </summary>
    public class ResourceCost
    {
        /// <summary>Food resource cost.</summary>
        [YamlMember(Alias = "food")]
        public int Food { get; set; } = 0;

        /// <summary>Wood resource cost.</summary>
        [YamlMember(Alias = "wood")]
        public int Wood { get; set; } = 0;

        /// <summary>Stone resource cost.</summary>
        [YamlMember(Alias = "stone")]
        public int Stone { get; set; } = 0;

        /// <summary>Iron resource cost.</summary>
        [YamlMember(Alias = "iron")]
        public int Iron { get; set; } = 0;

        /// <summary>Gold resource cost.</summary>
        [YamlMember(Alias = "gold")]
        public int Gold { get; set; } = 0;

        /// <summary>Population cost (housing slots consumed).</summary>
        [YamlMember(Alias = "population")]
        public int Population { get; set; } = 0;
    }
}
