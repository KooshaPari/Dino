using System.Collections.Generic;

namespace DINOForge.SDK
{
    /// <summary>
    /// Resolves content types to schema names used for validation.
    /// </summary>
    internal interface ISchemaResolverService
    {
        /// <summary>
        /// Gets all content types known to the loader.
        /// </summary>
        IReadOnlyCollection<string> ContentTypes { get; }

        /// <summary>
        /// Resolves a content type to its schema name.
        /// </summary>
        bool TryResolveSchemaName(string contentType, out string schemaName);
    }
}
