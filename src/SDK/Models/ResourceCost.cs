using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Shared resource cost model used by units, buildings, and other definitions.
    /// </summary>
    public class ResourceCost
    {
        [YamlMember(Alias = "food")]
        public int Food { get; set; } = 0;

        [YamlMember(Alias = "wood")]
        public int Wood { get; set; } = 0;

        [YamlMember(Alias = "stone")]
        public int Stone { get; set; } = 0;

        [YamlMember(Alias = "iron")]
        public int Iron { get; set; } = 0;

        [YamlMember(Alias = "gold")]
        public int Gold { get; set; } = 0;

        [YamlMember(Alias = "population")]
        public int Population { get; set; } = 0;
    }
}
