using System;
using System.IO;

namespace DINOForge.Runtime.Generation
{
    internal static class PackRootResolver
    {
        public static string Resolve(string configuredRoot, string? requestedRoot)
        {
            string selected = string.IsNullOrWhiteSpace(requestedRoot)
                ? configuredRoot
                : requestedRoot!;

            if (string.IsNullOrWhiteSpace(selected))
                throw new ArgumentException("Pack root is required.", nameof(requestedRoot));

            return Path.GetFullPath(selected);
        }
    }
}
