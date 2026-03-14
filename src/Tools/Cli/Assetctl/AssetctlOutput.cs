#nullable enable
using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DINOForge.Tools.Cli.Assetctl
{
    /// <summary>
    /// Shared output formatting helpers for assetctl CLI commands.
    /// Supports both human-readable table output (default) and JSON output.
    /// </summary>
    public static class AssetctlOutput
    {
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
        {
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        /// <summary>
        /// Returns true when the given format string requests JSON output.
        /// </summary>
        public static bool IsJsonOutput(string? format) =>
            string.Equals(format, "json", StringComparison.OrdinalIgnoreCase);

        /// <summary>
        /// Serialises <paramref name="value"/> as indented JSON and writes it to stdout.
        /// </summary>
        public static void WriteJson(object value) =>
            Console.WriteLine(JsonSerializer.Serialize(value, _jsonOptions));
    }
}
