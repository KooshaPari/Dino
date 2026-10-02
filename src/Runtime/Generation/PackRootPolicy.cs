using System;
using System.IO;

namespace DINOForge.Runtime.Generation
{
    internal sealed class PackRootPolicy
    {
        public string ConfiguredRoot { get; }
        public bool AllowOverrideOutsideConfiguredRoot { get; }

        public PackRootPolicy(string configuredRoot, bool allowOverrideOutsideConfiguredRoot = false)
        {
            ConfiguredRoot = Path.GetFullPath(configuredRoot ?? throw new ArgumentNullException(nameof(configuredRoot)));
            AllowOverrideOutsideConfiguredRoot = allowOverrideOutsideConfiguredRoot;
        }

        public string ResolveAuthorized(string? requestedRoot)
        {
            string resolved = PackRootResolver.Resolve(ConfiguredRoot, requestedRoot);
            if (AllowOverrideOutsideConfiguredRoot)
                return resolved;

            string rootWithSeparator = ConfiguredRoot.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            if (string.Equals(resolved, ConfiguredRoot, StringComparison.OrdinalIgnoreCase) ||
                resolved.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
                return resolved;

            throw new UnauthorizedAccessException(
                $"Requested pack root '{resolved}' is outside configured root '{ConfiguredRoot}'.");
        }
    }
}
