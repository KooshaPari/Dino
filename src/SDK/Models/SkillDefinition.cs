using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Strongly-typed representation of a DINOForge skill definition (skills/*.yaml).
    /// Maps to DINO's Components.Skills.* ECS components (HealSkillData, RageSkillData,
    /// MeteorCastSkillData, SummonSkillData, etc.).
    /// </summary>
    public class SkillDefinition
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "display_name")]
        public string DisplayName { get; set; } = "";

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Skill class category. Valid values: heal, mass_heal, buff, debuff,
        /// aoe_damage, summon, resurrection, passive.
        /// </summary>
        [YamlMember(Alias = "skill_class")]
        public string SkillClass { get; set; } = "";

        /// <summary>
        /// Targeting type. Valid values: self, single_ally, single_enemy,
        /// aoe_allies, aoe_enemies, aoe_all.
        /// </summary>
        [YamlMember(Alias = "target_type")]
        public string TargetType { get; set; } = "";

        /// <summary>
        /// Cooldown in seconds between uses.
        /// </summary>
        [YamlMember(Alias = "cooldown")]
        public float Cooldown { get; set; } = 0f;

        /// <summary>
        /// Duration in seconds. 0 = instant.
        /// </summary>
        [YamlMember(Alias = "duration")]
        public float Duration { get; set; } = 0f;

        /// <summary>
        /// Effective range in world units.
        /// </summary>
        [YamlMember(Alias = "range")]
        public float Range { get; set; } = 0f;

        /// <summary>
        /// Area-of-effect radius in world units (for AOE skills).
        /// </summary>
        [YamlMember(Alias = "radius")]
        public float Radius { get; set; } = 0f;

        /// <summary>
        /// Stat effects applied by this skill.
        /// </summary>
        [YamlMember(Alias = "effects")]
        public List<SkillEffect> Effects { get; set; } = new List<SkillEffect>();

        /// <summary>
        /// The vanilla DINO skill component this maps to (e.g. "Components.Skills.HealSkillData").
        /// </summary>
        [YamlMember(Alias = "vanilla_mapping")]
        public string? VanillaMapping { get; set; }
    }

    /// <summary>
    /// A single stat modifier effect applied by a skill.
    /// </summary>
    public class SkillEffect
    {
        /// <summary>
        /// Stat to modify (e.g. health, speed, damage, armor).
        /// </summary>
        [YamlMember(Alias = "stat")]
        public string Stat { get; set; } = "";

        /// <summary>
        /// Modifier type: flat or percent.
        /// </summary>
        [YamlMember(Alias = "modifier_type")]
        public string ModifierType { get; set; } = "flat";

        /// <summary>
        /// Modifier value. Positive = buff, negative = debuff.
        /// </summary>
        [YamlMember(Alias = "value")]
        public float Value { get; set; } = 0f;
    }
}
