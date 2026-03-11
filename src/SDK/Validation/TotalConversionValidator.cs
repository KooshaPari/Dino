#nullable enable
using System.Collections.Generic;
using DINOForge.SDK.Models;

namespace DINOForge.SDK.Validation
{
    /// <summary>
    /// Result of validating a <see cref="TotalConversionManifest"/>.
    /// </summary>
    public sealed class TcValidationResult
    {
        /// <summary>Whether the manifest passed all error-level checks (warnings do not affect this).</summary>
        public bool IsValid => Errors.Count == 0;

        /// <summary>List of error messages. Any error causes <see cref="IsValid"/> to be <c>false</c>.</summary>
        public List<string> Errors { get; }

        /// <summary>List of advisory warning messages that do not fail validation.</summary>
        public List<string> Warnings { get; }

        /// <summary>
        /// Initializes a new <see cref="TcValidationResult"/> with the provided error and warning collections.
        /// </summary>
        /// <param name="errors">Validation errors accumulated during the run.</param>
        /// <param name="warnings">Validation warnings accumulated during the run.</param>
        public TcValidationResult(List<string> errors, List<string> warnings)
        {
            Errors = errors;
            Warnings = warnings;
        }
    }

    /// <summary>
    /// Validates a <see cref="TotalConversionManifest"/> against DINOForge TC pack rules.
    /// </summary>
    public static class TotalConversionValidator
    {
        private static readonly List<string> _vanillaFactionIds = new List<string>
        {
            "player",
            "enemy_classic",
            "enemy_guerrilla"
        };

        /// <summary>
        /// The set of known vanilla faction IDs that a TC pack may replace.
        /// </summary>
        public static IReadOnlyList<string> VanillaFactionIds => _vanillaFactionIds.AsReadOnly();

        /// <summary>
        /// Validates the supplied <paramref name="manifest"/> and returns a <see cref="TcValidationResult"/>
        /// containing any errors and warnings found.
        /// </summary>
        /// <param name="manifest">The <see cref="TotalConversionManifest"/> to validate.</param>
        /// <returns>
        /// A <see cref="TcValidationResult"/> whose <see cref="TcValidationResult.IsValid"/> property
        /// is <c>true</c> only when no error-level violations were found.
        /// </returns>
        public static TcValidationResult Validate(TotalConversionManifest manifest)
        {
            List<string> errors = new List<string>();
            List<string> warnings = new List<string>();

            // Rule: id must not be null or empty
            if (string.IsNullOrWhiteSpace(manifest.Id))
            {
                errors.Add("TC pack id is required");
            }

            // Rule: type must equal "total_conversion"
            if (manifest.Type != "total_conversion")
            {
                errors.Add($"Pack type must be total_conversion, got {manifest.Type}");
            }

            // Rule: singleton should be true
            if (!manifest.Singleton)
            {
                warnings.Add("TC pack singleton=false is not recommended");
            }

            // Rule: vanilla faction IDs in ReplacesVanilla must be from the known set
            foreach (string vanillaId in manifest.ReplacesVanilla.Keys)
            {
                if (!_vanillaFactionIds.Contains(vanillaId))
                {
                    warnings.Add($"Unknown vanilla faction: {vanillaId}");
                }
            }

            // Rule: at least one faction must be defined
            if (manifest.Factions == null || manifest.Factions.Count == 0)
            {
                warnings.Add("TC pack has no factions defined");
            }
            else
            {
                HashSet<string> seenIds = new HashSet<string>();
                foreach (TcFactionEntry entry in manifest.Factions)
                {
                    // Rule: every faction entry must have a non-null id
                    if (string.IsNullOrWhiteSpace(entry.Id))
                    {
                        errors.Add("Faction entry missing id");
                        continue;
                    }

                    // Rule: faction IDs must be unique
                    if (!seenIds.Add(entry.Id))
                    {
                        errors.Add($"Duplicate faction id: {entry.Id}");
                    }
                }
            }

            return new TcValidationResult(errors, warnings);
        }
    }
}
