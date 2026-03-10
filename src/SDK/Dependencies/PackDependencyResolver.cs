using System;
using System.Collections.Generic;
using System.Linq;

namespace DINOForge.SDK.Dependencies
{
    using DINOForge.SDK;

    public class PackDependencyResolver
    {
        public DependencyResult ResolveDependencies(IEnumerable<PackManifest> available, PackManifest target)
        {
            var availableList = available.ToList();
            var availableIds = new HashSet<string>(availableList.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);

            var missing = target.DependsOn
                .Where(dep => !availableIds.Contains(dep))
                .Select(dep => $"Pack '{target.Id}' requires missing dependency: '{dep}'")
                .ToList();

            if (missing.Count > 0)
                return DependencyResult.Failure(missing);

            // Include the target and all available packs so ComputeLoadOrder can resolve
            // transitive dependencies across the full set.
            var scope = availableList.Where(p => !string.Equals(p.Id, target.Id, StringComparison.OrdinalIgnoreCase))
                                     .Append(target);

            return ComputeLoadOrder(scope);
        }

        public IReadOnlyList<string> DetectConflicts(IEnumerable<PackManifest> active)
        {
            var activeList = active.ToList();
            var activeIds = new HashSet<string>(activeList.Select(p => p.Id), StringComparer.OrdinalIgnoreCase);
            var errors = new List<string>();

            foreach (var pack in activeList)
            {
                foreach (var conflictId in pack.ConflictsWith)
                {
                    if (activeIds.Contains(conflictId))
                        errors.Add($"Pack '{pack.Id}' conflicts with active pack '{conflictId}'.");
                }
            }

            return errors.AsReadOnly();
        }

        public DependencyResult ComputeLoadOrder(IEnumerable<PackManifest> packs)
        {
            var packList = packs.ToList();
            var packById = packList.ToDictionary(p => p.Id, StringComparer.OrdinalIgnoreCase);

            // Build in-degree map and adjacency list (dep -> dependents).
            var inDegree = packList.ToDictionary(p => p.Id, _ => 0, StringComparer.OrdinalIgnoreCase);
            var dependents = packList.ToDictionary(
                p => p.Id,
                _ => new List<string>(),
                StringComparer.OrdinalIgnoreCase);

            var errors = new List<string>();

            foreach (var pack in packList)
            {
                foreach (var dep in pack.DependsOn)
                {
                    if (!packById.ContainsKey(dep))
                    {
                        errors.Add($"Pack '{pack.Id}' depends on unknown pack '{dep}'.");
                        continue;
                    }
                    dependents[dep].Add(pack.Id);
                    inDegree[pack.Id]++;
                }
            }

            if (errors.Count > 0)
                return DependencyResult.Failure(errors);

            // Kahn's algorithm - use LoadOrder as tiebreaker via sorted queue.
            var ready = new SortedSet<(int LoadOrder, string Id)>(
                packList.Where(p => inDegree[p.Id] == 0)
                        .Select(p => (p.LoadOrder, p.Id)));

            var sorted = new List<PackManifest>();

            while (ready.Count > 0)
            {
                var (_, nextId) = ready.Min;
                ready.Remove(ready.Min);

                sorted.Add(packById[nextId]);

                foreach (var dependentId in dependents[nextId])
                {
                    inDegree[dependentId]--;
                    if (inDegree[dependentId] == 0)
                        ready.Add((packById[dependentId].LoadOrder, dependentId));
                }
            }

            if (sorted.Count != packList.Count)
                return DependencyResult.Failure(new List<string> { "Circular dependency detected among packs." });

            return DependencyResult.Success(sorted);
        }

        public bool CheckFrameworkCompatibility(PackManifest pack, string frameworkVersion)
        {
            // Basic string equality check. Semver range parsing will be added later.
            if (string.IsNullOrWhiteSpace(pack.FrameworkVersion))
                return true;

            string required = pack.FrameworkVersion.TrimStart('>', '<', '=', '~', '^', ' ');
            return string.Equals(required, frameworkVersion, StringComparison.OrdinalIgnoreCase);
        }
    }
}
