using System;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DINOForge.SDK
{
    /// <summary>
    /// Loads and validates pack manifest files (pack.yaml).
    /// Wraps YamlDotNet for deserialization (ADR-008).
    /// </summary>
    public class PackLoader
    {
        private readonly IDeserializer _deserializer;

        public PackLoader()
        {
            _deserializer = new DeserializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .IgnoreUnmatchedProperties()
                .Build();
        }

        /// <summary>
        /// Load a pack manifest from a YAML file.
        /// </summary>
        public PackManifest LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException($"Pack manifest not found: {filePath}");

            string yaml = File.ReadAllText(filePath);
            return LoadFromString(yaml);
        }

        /// <summary>
        /// Load a pack manifest from a YAML string.
        /// </summary>
        public PackManifest LoadFromString(string yaml)
        {
            PackManifest manifest = _deserializer.Deserialize<PackManifest>(yaml);

            if (string.IsNullOrWhiteSpace(manifest.Id))
                throw new InvalidOperationException("Pack manifest missing required field: id");

            if (string.IsNullOrWhiteSpace(manifest.Name))
                throw new InvalidOperationException("Pack manifest missing required field: name");

            if (string.IsNullOrWhiteSpace(manifest.Version))
                throw new InvalidOperationException("Pack manifest missing required field: version");

            return manifest;
        }
    }
}
