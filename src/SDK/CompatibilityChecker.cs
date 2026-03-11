#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;

namespace DINOForge.SDK
{
    /// <summary>
    /// Provides version compatibility checking for DINOForge packs against
    /// DINOForge framework, game, BepInEx, and Unity versions.
    ///
    /// Version ranges use simple semver-like syntax:
    /// - wildcard = any version (wildcard)
    /// - exact version number
    /// - greater than or equal syntax
    /// - less than or equal syntax
    /// - strictly greater
    /// - strictly less
    /// - equal (alternate syntax)
    ///
    /// Multiple constraints can be combined with spaces.
    /// </summary>
    public static class CompatibilityChecker
        /// <summary>        /// Defines the installer action tier for a given compatibility status.        /// Maps status values from compat.json to installer directives.        /// </summary>        public enum StatusTier        {            /// <summary>Proceed with installation without warnings.</summary>            Proceed,            /// <summary>Warn user but allow installation.</summary>            Warn,            /// <summary>Warn strongly but allow installation.</summary>            WarnStrongly,            /// <summary>Block installation completely.</summary>            Block,            /// <summary>Unknown compatibility tier.</summary>            Unknown        }        /// <summary>        /// Gets the installer action tier for a compatibility status value.        /// Translates status strings from compat.json into actionable installer tiers.        /// </summary>        /// <param name="status">The status from compat.json (e.g., "optimal", "stable", "broken").</param>        /// <returns>The corresponding installer action tier.</returns>        public static StatusTier GetStatusTier(string status)        {            return status?.ToLowerInvariant() switch            {                "optimal" => StatusTier.Proceed,                "stable" => StatusTier.Proceed,                "maintenance" => StatusTier.Warn,                "unstable" => StatusTier.WarnStrongly,                "broken" => StatusTier.Block,                "unknown" => StatusTier.Warn,                _ => StatusTier.Unknown            };        }
    {
        /// <summary>
        /// Gets the current DINOForge framework version from the SDK assembly.
        /// </summary>
        public static Version FrameworkVersion
        {
            get
            {
                var version = typeof(CompatibilityChecker).Assembly.GetName().Version;
                return version ?? new Version(0, 1, 0);
            }
        }

        /// <summary>
        /// Checks if a pack manifest is compatible with the specified versions.
        /// </summary>
        /// <param name="manifest">The pack manifest to validate.</param>
        /// <param name="dinoGameVersion">Current DINO game version, e.g. "1.0.0".</param>
        /// <param name="bepinexVersion">Current BepInEx version.</param>
        /// <param name="unityVersion">Current Unity version, e.g. "2021.3.45f2".</param>
        /// <returns>A CompatibilityResult with compatibility status and any warnings/errors.</returns>
        public static CompatibilityResult CheckPack(
            PackManifest manifest,
            string dinoGameVersion = "*",
            string bepinexVersion = "*",
            string unityVersion = "*")
        {
            var result = new CompatibilityResult();

            // Check framework version
            if (!IsVersionInRange(FrameworkVersion.ToString(), manifest.FrameworkVersion))
            {
                result.Errors.Add(
                    $"Pack requires DINOForge {manifest.FrameworkVersion}, but {FrameworkVersion} is installed.");
            }

            // Check game version
            if (!IsVersionInRange(dinoGameVersion, manifest.GameVersion))
            {
                result.Warnings.Add(
                    $"Pack specifies game_version '{manifest.GameVersion}', but current game is {dinoGameVersion}. " +
                    "This may cause compatibility issues.");
            }

            // Check BepInEx version
            if (!IsVersionInRange(bepinexVersion, manifest.BepInExVersion))
            {
                result.Warnings.Add(
                    $"Pack specifies bepinex_version '{manifest.BepInExVersion}', but current BepInEx is {bepinexVersion}. " +
                    "This may cause compatibility issues.");
            }

            // Check Unity version
            if (!IsVersionInRange(unityVersion, manifest.UnityVersion))
            {
                result.Warnings.Add(
                    $"Pack specifies unity_version '{manifest.UnityVersion}', but current Unity is {unityVersion}. " +
                    "This may cause compatibility issues.");
            }

            // Pack is compatible if there are no errors (warnings are allowed)
            result.IsCompatible = result.Errors.Count == 0;

            return result;
        }

        /// <summary>
        /// Determines if a version satisfies a version range constraint.
        /// </summary>
        /// <param name="version">The version to check, e.g. "1.5.0".</param>
        /// <param name="range">The range constraint, e.g. multiple constraints separated by spaces.</param>
        /// <returns>True if version satisfies the range; false otherwise.</returns>
        public static bool IsVersionInRange(string version, string range)
        {
            // Wildcard always matches
            if (range == "*" || string.IsNullOrWhiteSpace(range))
            {
                return true;
            }

            // Split on spaces to get individual constraints
            var constraints = range.Split(' ');

            // Each constraint must be satisfied
            foreach (var constraint in constraints)
            {
                if (!IsSingleConstraintSatisfied(version, constraint))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if a version satisfies a single constraint like ">=1.0.0" or "<2.0.0".
        /// </summary>
        private static bool IsSingleConstraintSatisfied(string version, string constraint)
        {
            if (string.IsNullOrWhiteSpace(constraint))
            {
                return true;
            }

            // Extract operator and version number
            var (op, constraintVersion) = ExtractOperatorAndVersion(constraint);

            // Try to parse versions, handle formats like "2021.3.45f2" (Unity)
            if (!TryParseVersion(version, out var v) || !TryParseVersion(constraintVersion, out var cv))
            {
                // If we can't parse, fall back to string comparison (conservative)
                return version.Equals(constraintVersion, StringComparison.OrdinalIgnoreCase);
            }

            switch (op)
            {
                case ">=":
                    return v >= cv;
                case "<=":
                    return v <= cv;
                case ">":
                    return v > cv;
                case "<":
                    return v < cv;
                case "=":
                case "==":
                    return v == cv;
                case "~":
                    return IsCaretCompatible(v, cv);
                case "^":
                    return IsTildeCompatible(v, cv);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Extracts the operator and version from a constraint.
        /// </summary>
        private static (string op, string version) ExtractOperatorAndVersion(string constraint)
        {
            if (constraint.StartsWith(">="))
                return (">=", constraint.Substring(2));
            if (constraint.StartsWith("<="))
                return ("<=", constraint.Substring(2));
            if (constraint.StartsWith("=="))
                return ("==", constraint.Substring(2));
            if (constraint.StartsWith("~"))
                return ("~", constraint.Substring(1));
            if (constraint.StartsWith("^"))
                return ("^", constraint.Substring(1));
            if (constraint.StartsWith(">"))
                return (">", constraint.Substring(1));
            if (constraint.StartsWith("<"))
                return ("<", constraint.Substring(1));
            if (constraint.StartsWith("="))
                return ("=", constraint.Substring(1));

            // No operator, assume exact match
            return ("=", constraint);
        }

        /// <summary>
        /// Tries to parse a version string into a Version object.
        /// Handles standard semver and Unity-style versions.
        /// </summary>
        private static bool TryParseVersion(string versionStr, out Version version)
        {
            version = new Version(0, 0, 0);

            if (string.IsNullOrWhiteSpace(versionStr))
            {
                return false;
            }

            // Remove common suffixes like 'f2', 'rc1', etc. for parsing
            var cleaned = System.Text.RegularExpressions.Regex.Replace(versionStr, "[a-zA-Z]+\\d*$", "");

            if (Version.TryParse(cleaned, out var parsed))
            {
                version = parsed;
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tilde compatibility: allows patch changes only.
        /// </summary>
        private static bool IsCaretCompatible(Version actual, Version specified)
        {
            if (actual < specified)
                return false;

            // Allow patch changes, but not minor/major
            return actual.Major == specified.Major &&
                   actual.Minor == specified.Minor;
        }

        /// <summary>
        /// Caret compatibility: allows minor and patch changes only.
        /// </summary>
        private static bool IsTildeCompatible(Version actual, Version specified)
        {
            if (actual < specified)
                return false;

            // Allow minor/patch changes, but not major
            return actual.Major == specified.Major;
        }
    }

    /// <summary>
    /// Encapsulates the result of a compatibility check.
    /// </summary>
    public sealed class CompatibilityResult
    {
        /// <summary>
        /// True if the pack is compatible (no errors, warnings allowed).
        /// </summary>
        public bool IsCompatible { get; set; } = true;

        /// <summary>
        /// List of warning messages (compatibility issues that don't block loading).
        /// </summary>
        public List<string> Warnings { get; set; } = new List<string>();

        /// <summary>
        /// List of error messages (compatibility issues that block loading).
        /// </summary>
        public List<string> Errors { get; set; } = new List<string>();
    }
}
