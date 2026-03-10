using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Stub model for a DINOForge projectile definition (projectiles/*.yaml).
    /// </summary>
    public class ProjectileDefinition
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "display_name")]
        public string DisplayName { get; set; } = "";

        /// <summary>
        /// Travel speed in world units per second.
        /// </summary>
        [YamlMember(Alias = "speed")]
        public float Speed { get; set; } = 0f;

        [YamlMember(Alias = "damage")]
        public float Damage { get; set; } = 0f;

        /// <summary>
        /// Area-of-effect blast radius in world units. 0 = no AoE.
        /// </summary>
        [YamlMember(Alias = "aoe_radius")]
        public float AoeRadius { get; set; } = 0f;

        /// <summary>
        /// Visual prefab asset ID for the projectile in flight.
        /// </summary>
        [YamlMember(Alias = "visual_prefab")]
        public string? VisualPrefab { get; set; }

        /// <summary>
        /// VFX asset ID played on impact.
        /// </summary>
        [YamlMember(Alias = "impact_effect")]
        public string? ImpactEffect { get; set; }
    }
}
