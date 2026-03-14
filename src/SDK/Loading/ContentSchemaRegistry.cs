using System;
using System.Collections.Generic;
using System.Linq;

namespace DINOForge.SDK
{
    /// <summary>
    /// Central mapping from loader content types to schema names.
    /// </summary>
    internal sealed class SchemaResolverService : ISchemaResolverService
    {
        private static readonly Dictionary<string, string> SchemaNames =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "units", "unit" },
                { "buildings", "building" },
                { "factions", "faction" },
                { "weapons", "weapon" },
                { "projectiles", "projectile" },
                { "doctrines", "doctrine" },
                { "stats", "stat-override" },
                { "faction_patches", "faction-patch" }
            };

        /// <inheritdoc />
        public IReadOnlyCollection<string> ContentTypes => SchemaNames.Keys.ToList();

        /// <inheritdoc />
        public bool TryResolveSchemaName(string contentType, out string schemaName)
        {
            if (SchemaNames.TryGetValue(contentType, out string? resolved))
            {
                schemaName = resolved;
                return true;
            }

            schemaName = string.Empty;
            return false;
        }
    }
}
