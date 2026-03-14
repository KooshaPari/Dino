using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DINOForge.SDK
{
    /// <summary>
    /// Default implementation of <see cref="IFileDiscoveryService"/> with directory exclusions
    /// for generated and archived content.
    /// </summary>
    internal sealed class FileDiscoveryService : IFileDiscoveryService
    {
        private static readonly string[] DefaultExclusionsArray =
        {
            "_archived",
            "archived",
            "export",
            "generated",
            "bin",
            "obj",
            "node_modules",
            ".git",
            ".vs",
            ".idea",
            "packages"
        };

        private readonly HashSet<string> _exclusions = new HashSet<string>(DefaultExclusionsArray, StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc />
        public IReadOnlyList<string> DefaultExclusions => Array.AsReadOnly(DefaultExclusionsArray);

        /// <inheritdoc />
        public string[] GetFiles(string directory, string[] searchPatterns, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory) || searchPatterns.Length == 0)
            {
                return Array.Empty<string>();
            }

            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (string pattern in searchPatterns)
            {
                foreach (string file in GetFilesInternal(directory, pattern, searchOption))
                {
                    results.Add(file);
                }
            }

            return results.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        /// <inheritdoc />
        public string[] GetDirectories(string directory, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            if (string.IsNullOrEmpty(directory) || !Directory.Exists(directory))
            {
                return Array.Empty<string>();
            }

            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return Directory.GetDirectories(directory)
                    .Where(path => !ShouldExclude(Path.GetFileName(path)))
                    .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToArray();
            }

            HashSet<string> results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            GetDirectoriesRecursive(directory, results);
            return results.OrderBy(path => path, StringComparer.OrdinalIgnoreCase).ToArray();
        }

        /// <inheritdoc />
        public string[] DiscoverPackDirectories(string rootDirectory)
        {
            return GetDirectories(rootDirectory)
                .Where(directory => File.Exists(Path.Combine(directory, "pack.yaml")))
                .ToArray();
        }

        private IEnumerable<string> GetFilesInternal(string directory, string searchPattern, SearchOption searchOption)
        {
            if (searchOption == SearchOption.TopDirectoryOnly)
            {
                return Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly);
            }

            List<string> files = new List<string>();
            GetFilesRecursive(directory, searchPattern, files);
            return files;
        }

        private void GetFilesRecursive(string directory, string searchPattern, List<string> results)
        {
            try
            {
                results.AddRange(Directory.GetFiles(directory, searchPattern, SearchOption.TopDirectoryOnly));
                foreach (string subDirectory in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (ShouldExclude(Path.GetFileName(subDirectory)))
                    {
                        continue;
                    }

                    GetFilesRecursive(subDirectory, searchPattern, results);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private void GetDirectoriesRecursive(string directory, HashSet<string> results)
        {
            try
            {
                foreach (string subDirectory in Directory.GetDirectories(directory, "*", SearchOption.TopDirectoryOnly))
                {
                    if (ShouldExclude(Path.GetFileName(subDirectory)))
                    {
                        continue;
                    }

                    results.Add(subDirectory);
                    GetDirectoriesRecursive(subDirectory, results);
                }
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (DirectoryNotFoundException)
            {
            }
        }

        private bool ShouldExclude(string? directoryName)
        {
            return !string.IsNullOrEmpty(directoryName) && _exclusions.Contains(directoryName);
        }
    }
}
