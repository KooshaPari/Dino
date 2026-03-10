using System.Collections.Generic;

namespace DINOForge.SDK.Registry
{
    public sealed class RegistryConflict
    {
        public string EntryId { get; }
        public IReadOnlyList<string> ConflictingPackIds { get; }
        public string Message { get; }

        public RegistryConflict(string entryId, IReadOnlyList<string> conflictingPackIds, string message)
        {
            EntryId = entryId;
            ConflictingPackIds = conflictingPackIds;
            Message = message;
        }
    }
}
