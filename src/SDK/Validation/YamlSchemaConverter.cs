using Newtonsoft.Json;
using YamlDotNet.Serialization;

namespace DINOForge.SDK.Validation
{
    /// <summary>
    /// Converts YAML schema content to JSON format for use with NJsonSchema.
    /// NJsonSchema only understands JSON schemas, so this utility bridges the gap
    /// between our YAML-based schema definitions and the JSON Schema validation library.
    /// </summary>
    internal static class YamlSchemaConverter
    {
        /// <summary>
        /// Converts YAML schema content to a JSON schema string.
        /// </summary>
        /// <param name="yamlContent">Raw YAML schema text.</param>
        /// <returns>A JSON string representation of the schema.</returns>
        public static string ConvertYamlToJson(string yamlContent)
        {
            var deserializer = new DeserializerBuilder().Build();
            var schemaObject = deserializer.Deserialize<object>(yamlContent);

            var jsonContent = JsonConvert.SerializeObject(schemaObject);
            return jsonContent;
        }
    }
}
