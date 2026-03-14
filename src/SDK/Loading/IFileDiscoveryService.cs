using System.Collections.Generic;
using System.IO;

namespace DINOForge.SDK
{
    /// <summary>
    /// Abstracts filesystem enumeration for loading-related services.
    /// </summary>
    internal interface IFileDiscoveryService
    {
        /// <summary>
        /// Gets files matching the provided patterns.
        /// </summary>
        string[] GetFiles(string directory, string[] searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Gets subdirectories in the provided directory.
        /// </summary>
        string[] GetDirectories(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly);

        /// <summary>
        /// Discovers pack directories containing a <c>pack.yaml</c> manifest.
        /// </summary>
        string[] DiscoverPackDirectories(string rootDirectory);

        /// <summary>
        /// Gets the current exclusion list.
        /// </summary>
        IReadOnlyList<string> DefaultExclusions { get; }
    }
}
