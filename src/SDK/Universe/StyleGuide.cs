using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Universe
{
    /// <summary>
    /// Visual and audio style rules for a themed universe.
    /// Defines color palettes, audio themes, and architecture styles per faction.
    /// </summary>
    public class StyleGuide
    {
        /// <summary>
        /// Per-faction visual/audio style definitions.
        /// </summary>
        [YamlMember(Alias = "faction_styles")]
        public Dictionary<string, FactionStyle> FactionStyles { get; set; } = new Dictionary<string, FactionStyle>(System.StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Global style properties shared across all factions.
        /// </summary>
        [YamlMember(Alias = "global")]
        public GlobalStyle? Global { get; set; }
    }

    /// <summary>
    /// Style definition for a specific faction.
    /// </summary>
    public class FactionStyle
    {
        /// <summary>
        /// Color palette for this faction.
        /// </summary>
        [YamlMember(Alias = "colors")]
        public ColorPalette Colors { get; set; } = new ColorPalette();

        /// <summary>
        /// Audio theme identifiers for this faction.
        /// </summary>
        [YamlMember(Alias = "audio")]
        public AudioTheme? Audio { get; set; }

        /// <summary>
        /// Architecture/building aesthetic style.
        /// </summary>
        [YamlMember(Alias = "architecture")]
        public string? Architecture { get; set; }

        /// <summary>
        /// General visual theme descriptor (e.g. "sleek_white", "industrial_grey").
        /// </summary>
        [YamlMember(Alias = "visual_theme")]
        public string? VisualTheme { get; set; }
    }

    /// <summary>
    /// Color palette for a faction.
    /// </summary>
    public class ColorPalette
    {
        /// <summary>
        /// Primary faction color (hex, e.g. "#FFFFFF").
        /// </summary>
        [YamlMember(Alias = "primary")]
        public string Primary { get; set; } = "#FFFFFF";

        /// <summary>
        /// Secondary/accent color (hex).
        /// </summary>
        [YamlMember(Alias = "secondary")]
        public string Secondary { get; set; } = "#000000";

        /// <summary>
        /// Tertiary/trim color (hex).
        /// </summary>
        [YamlMember(Alias = "tertiary")]
        public string? Tertiary { get; set; }

        /// <summary>
        /// UI highlight color (hex).
        /// </summary>
        [YamlMember(Alias = "ui_highlight")]
        public string? UiHighlight { get; set; }
    }

    /// <summary>
    /// Audio theme for a faction.
    /// </summary>
    public class AudioTheme
    {
        /// <summary>
        /// March/movement audio theme identifier.
        /// </summary>
        [YamlMember(Alias = "march")]
        public string? March { get; set; }

        /// <summary>
        /// Ambient background audio theme.
        /// </summary>
        [YamlMember(Alias = "ambient")]
        public string? Ambient { get; set; }

        /// <summary>
        /// Combat audio theme.
        /// </summary>
        [YamlMember(Alias = "combat")]
        public string? Combat { get; set; }

        /// <summary>
        /// Victory fanfare theme.
        /// </summary>
        [YamlMember(Alias = "victory")]
        public string? Victory { get; set; }

        /// <summary>
        /// Defeat theme.
        /// </summary>
        [YamlMember(Alias = "defeat")]
        public string? Defeat { get; set; }
    }

    /// <summary>
    /// Global style properties shared across all factions.
    /// </summary>
    public class GlobalStyle
    {
        /// <summary>
        /// Overall visual tone (e.g. "gritty", "clean", "stylized").
        /// </summary>
        [YamlMember(Alias = "tone")]
        public string? Tone { get; set; }

        /// <summary>
        /// UI theme identifier.
        /// </summary>
        [YamlMember(Alias = "ui_theme")]
        public string? UiTheme { get; set; }

        /// <summary>
        /// Font style preference.
        /// </summary>
        [YamlMember(Alias = "font_style")]
        public string? FontStyle { get; set; }
    }
}
