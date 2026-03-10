using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;

using YamlDotNet.Serialization;

namespace DINOForge.SDK.Validation
{
    /// <summary>
    /// Converts YAML content to JSON format for use with NJsonSchema.
    /// NJsonSchema only understands JSON schemas, so this utility bridges the gap
    /// between our YAML-based schema/content definitions and the JSON Schema validation library.
    /// Performs scalar type coercion so that YAML numbers become JSON numbers and
    /// YAML booleans become JSON booleans (YamlDotNet's default <c>Deserialize&lt;object&gt;</c>
    /// returns all scalars as strings).
    /// </summary>
    internal static class YamlSchemaConverter
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder().Build();

        /// <summary>
        /// Converts YAML content to a JSON string with proper type preservation.
        /// </summary>
        /// <param name="yamlContent">Raw YAML text.</param>
        /// <returns>A JSON string representation.</returns>
        public static string ConvertYamlToJson(string yamlContent)
        {
            var yamlObject = Deserializer.Deserialize<object>(yamlContent);
            var coerced = CoerceTypes(yamlObject);
            return JsonConvert.SerializeObject(coerced);
        }

        /// <summary>
        /// Recursively walks the deserialized YAML object graph and coerces
        /// string scalars to their appropriate CLR types (bool, long, double).
        /// </summary>
        private static object? CoerceTypes(object? value)
        {
            if (value == null)
                return null;

            if (value is Dictionary<object, object> dict)
            {
                var result = new Dictionary<string, object?>();
                foreach (var kvp in dict)
                {
                    string key = kvp.Key?.ToString() ?? "";
                    result[key] = CoerceTypes(kvp.Value);
                }
                return result;
            }

            if (value is List<object> list)
            {
                var result = new List<object?>();
                foreach (var item in list)
                    result.Add(CoerceTypes(item));
                return result;
            }

            if (value is string s)
            {
                // Try boolean
                if (string.Equals(s, "true", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(s, "false", StringComparison.OrdinalIgnoreCase))
                    return false;

                // Try null
                if (string.Equals(s, "null", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(s, "~", StringComparison.Ordinal))
                    return null;

                // Try integer
                if (long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out long longVal))
                    return longVal;

                // Try floating point
                if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out double doubleVal))
                    return doubleVal;

                return s;
            }

            return value;
        }
    }
}
