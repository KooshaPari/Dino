using System.Collections.Generic;

namespace DINOForge.DesktopCompanion.Data
{
    /// <summary>
    /// Flat DTO representing a discovered pack for display in the companion UI.
    /// Does not reference any Runtime type — constructed purely from filesystem data.
    /// </summary>
    public sealed class PackViewModel
    {
        /// <summary>Unique pack identifier (from pack.yaml id field).</summary>
        public string Id { get; init; } = "";

        /// <summary>Human-readable pack name.</summary>
        public string Name { get; init; } = "";

        /// <summary>Semantic version string (e.g. "0.1.0").</summary>
        public string Version { get; init; } = "0.1.0";

        /// <summary>Pack author or organization.</summary>
        public string Author { get; init; } = "";

        /// <summary>Pack type: content, balance, ruleset, total_conversion, utility.</summary>
        public string Type { get; init; } = "content";

        /// <summary>Optional description of pack purpose.</summary>
        public string? Description { get; init; }

        /// <summary>Whether this pack is currently enabled (not in disabled_packs.json).</summary>
        public bool Enabled { get; set; } = true;

        /// <summary>Number of validation errors detected for this pack.</summary>
        public int ErrorCount { get; init; } = 0;

        /// <summary>True when this pack has one or more validation errors.</summary>
        public bool HasErrors => ErrorCount > 0;

        /// <summary>List of error messages for this pack.</summary>
        public IReadOnlyList<string> Errors { get; init; } = System.Array.Empty<string>();

        /// <summary>Pack IDs this pack depends on.</summary>
        public IReadOnlyList<string> DependsOn { get; init; } = System.Array.Empty<string>();

        /// <summary>Pack IDs that conflict with this pack.</summary>
        public IReadOnlyList<string> ConflictsWith { get; init; } = System.Array.Empty<string>();

        /// <summary>Load order priority.</summary>
        public int LoadOrder { get; init; } = 100;

        /// <summary>Absolute path to the pack directory on disk.</summary>
        public string PackDirectory { get; init; } = "";
    }
}
