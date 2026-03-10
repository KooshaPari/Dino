using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Models
{
    /// <summary>
    /// Strongly-typed representation of a DINOForge faction definition (factions/*.yaml).
    /// Corresponds to schemas/faction.schema.yaml.
    /// </summary>
    public class FactionDefinition
    {
        [YamlMember(Alias = "faction")]
        public FactionInfo Faction { get; set; } = new FactionInfo();

        [YamlMember(Alias = "economy")]
        public FactionEconomy Economy { get; set; } = new FactionEconomy();

        [YamlMember(Alias = "army")]
        public FactionArmy Army { get; set; } = new FactionArmy();

        /// <summary>
        /// Maps abstract role slots to concrete unit IDs.
        /// Keys: cheap_infantry, line_infantry, elite_infantry, anti_armor, support_weapon,
        /// recon, light_vehicle, heavy_vehicle, artillery, hero_commander, spike_unit.
        /// </summary>
        [YamlMember(Alias = "roster")]
        public FactionRoster Roster { get; set; } = new FactionRoster();

        /// <summary>
        /// Maps abstract building roles to concrete building IDs.
        /// Keys: barracks, workshop, artillery_foundry, tower_mg, heavy_defense,
        /// command_center, economy_primary, economy_secondary, research_facility, wall_segment.
        /// </summary>
        [YamlMember(Alias = "buildings")]
        public FactionBuildings Buildings { get; set; } = new FactionBuildings();

        [YamlMember(Alias = "visuals")]
        public FactionVisuals? Visuals { get; set; }

        [YamlMember(Alias = "audio")]
        public FactionAudio? Audio { get; set; }
    }

    public class FactionInfo
    {
        [YamlMember(Alias = "id")]
        public string Id { get; set; } = "";

        [YamlMember(Alias = "display_name")]
        public string DisplayName { get; set; } = "";

        [YamlMember(Alias = "description")]
        public string? Description { get; set; }

        /// <summary>
        /// Theme. Valid values: starwars, modern, futuristic, fantasy, custom.
        /// </summary>
        [YamlMember(Alias = "theme")]
        public string Theme { get; set; } = "";

        /// <summary>
        /// Archetype. Valid values: order, industrial_swarm, asymmetric, custom.
        /// </summary>
        [YamlMember(Alias = "archetype")]
        public string Archetype { get; set; } = "";

        /// <summary>
        /// Doctrine identifier (e.g. elite_discipline, mechanized_attrition).
        /// </summary>
        [YamlMember(Alias = "doctrine")]
        public string? Doctrine { get; set; }

        [YamlMember(Alias = "icon")]
        public string? Icon { get; set; }
    }

    public class FactionEconomy
    {
        /// <summary>
        /// Resource gather rate multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "gather_bonus")]
        public float GatherBonus { get; set; } = 1.0f;

        /// <summary>
        /// Unit upkeep cost multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "upkeep_modifier")]
        public float UpkeepModifier { get; set; } = 1.0f;

        /// <summary>
        /// Research speed multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "research_speed")]
        public float ResearchSpeed { get; set; } = 1.0f;

        /// <summary>
        /// Building construction speed multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "build_speed")]
        public float BuildSpeed { get; set; } = 1.0f;
    }

    public class FactionArmy
    {
        /// <summary>
        /// Morale style. Valid values: disciplined, mechanical, fanatical, irregular, custom.
        /// </summary>
        [YamlMember(Alias = "morale_style")]
        public string? MoraleStyle { get; set; }

        /// <summary>
        /// Unit cap multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "unit_cap_modifier")]
        public float UnitCapModifier { get; set; } = 1.0f;

        /// <summary>
        /// Elite unit cost multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "elite_cost_modifier")]
        public float EliteCostModifier { get; set; } = 1.0f;

        /// <summary>
        /// Unit spawn rate multiplier. Default 1.0.
        /// </summary>
        [YamlMember(Alias = "spawn_rate_modifier")]
        public float SpawnRateModifier { get; set; } = 1.0f;
    }

    public class FactionRoster
    {
        [YamlMember(Alias = "cheap_infantry")]
        public string? CheapInfantry { get; set; }

        [YamlMember(Alias = "line_infantry")]
        public string? LineInfantry { get; set; }

        [YamlMember(Alias = "elite_infantry")]
        public string? EliteInfantry { get; set; }

        [YamlMember(Alias = "anti_armor")]
        public string? AntiArmor { get; set; }

        [YamlMember(Alias = "support_weapon")]
        public string? SupportWeapon { get; set; }

        [YamlMember(Alias = "recon")]
        public string? Recon { get; set; }

        [YamlMember(Alias = "light_vehicle")]
        public string? LightVehicle { get; set; }

        [YamlMember(Alias = "heavy_vehicle")]
        public string? HeavyVehicle { get; set; }

        [YamlMember(Alias = "artillery")]
        public string? Artillery { get; set; }

        [YamlMember(Alias = "hero_commander")]
        public string? HeroCommander { get; set; }

        [YamlMember(Alias = "spike_unit")]
        public string? SpikeUnit { get; set; }
    }

    public class FactionBuildings
    {
        [YamlMember(Alias = "barracks")]
        public string? Barracks { get; set; }

        [YamlMember(Alias = "workshop")]
        public string? Workshop { get; set; }

        [YamlMember(Alias = "artillery_foundry")]
        public string? ArtilleryFoundry { get; set; }

        [YamlMember(Alias = "tower_mg")]
        public string? TowerMg { get; set; }

        [YamlMember(Alias = "heavy_defense")]
        public string? HeavyDefense { get; set; }

        [YamlMember(Alias = "command_center")]
        public string? CommandCenter { get; set; }

        [YamlMember(Alias = "economy_primary")]
        public string? EconomyPrimary { get; set; }

        [YamlMember(Alias = "economy_secondary")]
        public string? EconomySecondary { get; set; }

        [YamlMember(Alias = "research_facility")]
        public string? ResearchFacility { get; set; }

        [YamlMember(Alias = "wall_segment")]
        public string? WallSegment { get; set; }
    }

    public class FactionVisuals
    {
        /// <summary>
        /// Hex color string, e.g. "#FF0000".
        /// </summary>
        [YamlMember(Alias = "primary_color")]
        public string? PrimaryColor { get; set; }

        /// <summary>
        /// Hex color string, e.g. "#FF0000".
        /// </summary>
        [YamlMember(Alias = "accent_color")]
        public string? AccentColor { get; set; }

        [YamlMember(Alias = "projectile_pack")]
        public string? ProjectilePack { get; set; }

        [YamlMember(Alias = "ui_skin")]
        public string? UiSkin { get; set; }
    }

    public class FactionAudio
    {
        [YamlMember(Alias = "weapon_pack")]
        public string? WeaponPack { get; set; }

        [YamlMember(Alias = "structure_pack")]
        public string? StructurePack { get; set; }

        [YamlMember(Alias = "ambient_pack")]
        public string? AmbientPack { get; set; }

        [YamlMember(Alias = "music_pack")]
        public string? MusicPack { get; set; }
    }
}
