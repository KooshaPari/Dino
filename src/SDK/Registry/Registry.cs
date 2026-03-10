using System;
using System.Collections.Generic;
using System.Linq;

namespace DINOForge.SDK.Registry
{
    public class Registry<T> : IRegistry<T>
    {
        private readonly Dictionary<string, List<RegistryEntry<T>>> _entries =
            new Dictionary<string, List<RegistryEntry<T>>>(StringComparer.OrdinalIgnoreCase);

        public void Register(string id, T entry, RegistrySource source, string sourcePackId, int loadOrder = 100)
        {
            var registryEntry = new RegistryEntry<T>(id, entry, source, sourcePackId, loadOrder);

            if (!_entries.TryGetValue(id, out var list))
            {
                list = new List<RegistryEntry<T>>();
                _entries[id] = list;
            }

            list.Add(registryEntry);
            list.Sort((a, b) => b.Priority.CompareTo(a.Priority));
        }

        public T? Get(string id)
        {
            if (_entries.TryGetValue(id, out var list) && list.Count > 0)
                return list[0].Data;
            return default;
        }

        public bool Contains(string id)
        {
            return _entries.ContainsKey(id) && _entries[id].Count > 0;
        }

        public IReadOnlyDictionary<string, RegistryEntry<T>> All
        {
            get
            {
                var result = new Dictionary<string, RegistryEntry<T>>(StringComparer.OrdinalIgnoreCase);
                foreach (var kvp in _entries)
                {
                    if (kvp.Value.Count > 0)
                        result[kvp.Key] = kvp.Value[0];
                }
                return result;
            }
        }

        public void Override(string id, T entry, RegistrySource source, string sourcePackId, int loadOrder = 100)
        {
            Console.WriteLine($"[Registry] Override: id={id} source={source} pack={sourcePackId}");
            Register(id, entry, source, sourcePackId, loadOrder);
        }

        public IReadOnlyList<RegistryConflict> DetectConflicts()
        {
            var conflicts = new List<RegistryConflict>();

            foreach (var kvp in _entries)
            {
                var list = kvp.Value;
                if (list.Count < 2)
                    continue;

                int topPriority = list[0].Priority;
                var tied = list.Where(e => e.Priority == topPriority).ToList();

                if (tied.Count >= 2)
                {
                    // Extract conflicting pack IDs from tied entries
                    var conflictingPackIds = tied
                        .Select(e => e.SourcePackId)
                        .ToList()
                        .AsReadOnly();

                    conflicts.Add(new RegistryConflict(
                        kvp.Key,
                        conflictingPackIds,
                        $"Entry '{kvp.Key}' has {tied.Count} entries with equal priority {topPriority}."));
                }
            }

            return conflicts;
        }
    }
}
