using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Strongly-typed representation of a DINOForge unit definition (units/*.yaml).
    /// Corresponds to schemas/unit.schema.yaml.
    /// </summary>
    public class UnitDefinition
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "display_name")]
        public string DisplayName { get; set; } = "";

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Unit class. Valid values: MilitiaLight, CoreLineInfantry, EliteLineInfantry,
        /// HeavyInfantry, Skirmisher, AntiArmor, ShockMelee, SwarmFodder, FastVehicle,
        /// MainBattleVehicle, HeavySiege, Artillery, WalkerHeavy, StaticMG, StaticAT,
        /// StaticArtillery, SupportEngineer, Recon, HeroCommander, AirstrikeProxy, ShieldedElite.
        /// </summary>
        [YamlMember(Alias = "unit_class")]
        public string UnitClass { get; set; } = "";

        [YamlMember(Alias = "faction_id")]
        public string FactionId { get; set; } = "";

        /// <summary>
        /// Tech tier: 1 = cheap/early, 2 = mid, 3 = late/expensive.
        /// </summary>
        [YamlMember(Alias = "tier")]
        public int? Tier { get; set; }

        [YamlMember(Alias = "stats")]
        public UnitStats Stats { get; set; } = new UnitStats();

        /// <summary>
        /// Weapon class ID referencing a weapon in the weapon registry.
        /// </summary>
        [YamlMember(Alias = "weapon")]
        public string? Weapon { get; set; }

        /// <summary>
        /// Defense tags. Valid values: Unarmored, InfantryArmor, HeavyArmor,
        /// Fortified, Shielded, Mechanical, Biological, Heroic.
        /// </summary>
        [YamlMember(Alias = "defense_tags")]
        public List<string> DefenseTags { get; set; } = new List<string>();

        /// <summary>
        /// Behavior tags. Valid values: HoldLine, AdvanceFire, Charge, Kite,
        /// Swarm, SiegePriority, AntiStructure, AntiMass, AntiArmor, MoralePressure.
        /// </summary>
        [YamlMember(Alias = "behavior_tags")]
        public List<string> BehaviorTags { get; set; } = new List<string>();

        [YamlMember(Alias = "visuals")]
        public UnitVisuals? Visuals { get; set; }

        [YamlMember(Alias = "audio")]
        public UnitAudio? Audio { get; set; }

        /// <summary>
        /// The vanilla DINO unit ID this unit replaces or maps to.
        /// </summary>
        [YamlMember(Alias = "vanilla_mapping")]
        public string? VanillaMapping { get; set; }

        /// <summary>
        /// Tech node ID required to unlock this unit.
        /// </summary>
        [YamlMember(Alias = "tech_requirement")]
        public string? TechRequirement { get; set; }
    }

    public class UnitStats
    {
        [YamlMember(Alias = "hp")]
        public float Hp { get; set; } = 1f;

        [YamlMember(Alias = "damage")]
        public float Damage { get; set; } = 0f;

        [YamlMember(Alias = "armor")]
        public float Armor { get; set; } = 0f;

        [YamlMember(Alias = "range")]
        public float Range { get; set; } = 0f;

        [YamlMember(Alias = "speed")]
        public float Speed { get; set; } = 0f;

        [YamlMember(Alias = "cost")]
        public ResourceCost Cost { get; set; } = new ResourceCost();

        /// <summary>
        /// Hit chance, clamped 0.0 – 1.0. Default 0.7.
        /// </summary>
        [YamlMember(Alias = "accuracy")]
        public float Accuracy { get; set; } = 0.7f;

        /// <summary>
        /// Attacks per second. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "fire_rate")]
        public float FireRate { get; set; } = 1.0f;

        /// <summary>
        /// Base morale value. Default 100.
        /// </summary>
        [YamlMember(Alias = "morale")]
        public float Morale { get; set; } = 100f;
    }

    public class UnitVisuals
    {
        [YamlMember(Alias = "icon")]
        public string? Icon { get; set; }

        [YamlMember(Alias = "portrait")]
        public string? Portrait { get; set; }

        [YamlMember(Alias = "model_override")]
        public string? ModelOverride { get; set; }

        [YamlMember(Alias = "projectile_vfx")]
        public string? ProjectileVfx { get; set; }

        [YamlMember(Alias = "muzzle_vfx")]
        public string? MuzzleVfx { get; set; }
    }

    public class UnitAudio
    {
        [YamlMember(Alias = "attack_sound")]
        public string? AttackSound { get; set; }

        [YamlMember(Alias = "death_sound")]
        public string? DeathSound { get; set; }

        [YamlMember(Alias = "select_sound")]
        public string? SelectSound { get; set; }

        [YamlMember(Alias = "move_sound")]
        public string? MoveSound { get; set; }
    }
}
