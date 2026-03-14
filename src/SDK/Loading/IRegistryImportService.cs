using System.Collections.Generic;
using DINOForge.SDK.Models;

namespace DINOForge.SDK
{
    /// <summary>
    /// Imports validated YAML content into registries.
    /// </summary>
    internal interface IRegistryImportService
    {
        /// <summary>
        /// Gets loaded stat overrides.
        /// </summary>
        IReadOnlyList<StatOverrideDefinition> LoadedOverrides { get; }

        /// <summary>
        /// Loads and registers content from a YAML file.
        /// </summary>
        void LoadAndRegisterContent(
            string yamlFilePath,
            string contentType,
            PackManifest manifest,
            IList<string> errors);
    }
}
