using System.Collections.Generic;

namespace DINOForge.SDK.Registry
{
    public interface IRegistry<T>
    {
        void Register(string id, T entry, RegistrySource source, string sourcePackId, int loadOrder = 100);
        T? Get(string id);
        bool Contains(string id);
        IReadOnlyDictionary<string, RegistryEntry<T>> All { get; }
        void Override(string id, T entry, RegistrySource source, string sourcePackId, int loadOrder = 100);
        IReadOnlyList<RegistryConflict> DetectConflicts();
    }
}
