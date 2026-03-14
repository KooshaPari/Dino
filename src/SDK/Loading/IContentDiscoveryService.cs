using System.Collections.Generic;

namespace DINOForge.SDK
{
    /// <summary>
    /// Discovers pack directories and content YAML files.
    /// </summary>
    internal interface IContentDiscoveryService
    {
        /// <summary>
        /// Discovers pack directories below the given root.
        /// </summary>
        IReadOnlyList<string> DiscoverPackDirectories(string packsRootDirectory);

        /// <summary>
        /// Discovers YAML files for a specific content type.
        /// </summary>
        IReadOnlyList<string> DiscoverYamlFiles(
            string packDirectory,
            string contentType,
            IReadOnlyList<string>? declaredPaths);
    }
}
