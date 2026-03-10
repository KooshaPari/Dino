using System.Collections.Generic;
using System.Linq;
using DINOForge.SDK;
using DINOForge.SDK.Dependencies;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class DependencyResolverTests
    {
        private readonly PackLoader _loader = new();
        private readonly PackDependencyResolver _resolver = new();

        private PackManifest MakePack(string id, string[]? dependsOn = null, string[]? conflictsWith = null, int loadOrder = 100)
        {
            string deps = dependsOn is { Length: > 0 }
                ? "depends_on:\n" + string.Join("", System.Array.ConvertAll(dependsOn, d => $"  - {d}\n"))
                : "";

            string conflicts = conflictsWith is { Length: > 0 }
                ? "conflicts_with:\n" + string.Join("", System.Array.ConvertAll(conflictsWith, c => $"  - {c}\n"))
                : "";

            string yaml = $@"
id: {id}
name: {id}
version: 0.1.0
author: Test
type: content
load_order: {loadOrder}
{deps}{conflicts}";

            return _loader.LoadFromString(yaml);
        }

        // ── ResolveDependencies ─────────────────────────────────────────────

        [Fact]
        public void ResolveDeps_NoDependencies_Succeeds()
        {
            PackManifest target = MakePack("standalone");
            var available = new List<PackManifest> { target };

            DependencyResult result = _resolver.ResolveDependencies(available, target);

            result.IsSuccess.Should().BeTrue();
            result.LoadOrder.Should().ContainSingle(p => p.Id == "standalone");
        }

        [Fact]
        public void ResolveDeps_SatisfiedDependency_Succeeds()
        {
            PackManifest core = MakePack("core");
            PackManifest addon = MakePack("addon", dependsOn: new[] { "core" });
            var available = new List<PackManifest> { core, addon };

            DependencyResult result = _resolver.ResolveDependencies(available, addon);

            result.IsSuccess.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.LoadOrder.Should().Contain(p => p.Id == "addon");
        }

        [Fact]
        public void ResolveDeps_MissingDependency_Fails()
        {
            PackManifest addon = MakePack("addon", dependsOn: new[] { "missing-pack" });
            var available = new List<PackManifest> { addon };

            DependencyResult result = _resolver.ResolveDependencies(available, addon);

            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().HaveCount(1);
            result.Errors[0].Should().Contain("missing-pack");
        }

        // ── DetectConflicts ─────────────────────────────────────────────────

        [Fact]
        public void DetectConflicts_NoConflicts_ReturnsEmpty()
        {
            PackManifest alpha = MakePack("alpha");
            PackManifest beta = MakePack("beta");
            var active = new List<PackManifest> { alpha, beta };

            var conflicts = _resolver.DetectConflicts(active);

            conflicts.Should().BeEmpty();
        }

        [Fact]
        public void DetectConflicts_ConflictingPacks_ReturnsConflicts()
        {
            PackManifest alpha = MakePack("alpha", conflictsWith: new[] { "beta" });
            PackManifest beta = MakePack("beta");
            var active = new List<PackManifest> { alpha, beta };

            var conflicts = _resolver.DetectConflicts(active);

            conflicts.Should().HaveCount(1);
            conflicts[0].Should().Contain("alpha");
            conflicts[0].Should().Contain("beta");
        }

        // ── ComputeLoadOrder ────────────────────────────────────────────────

        [Fact]
        public void ComputeLoadOrder_SinglePack_ReturnsSingle()
        {
            PackManifest solo = MakePack("solo");
            var packs = new List<PackManifest> { solo };

            DependencyResult result = _resolver.ComputeLoadOrder(packs);

            result.IsSuccess.Should().BeTrue();
            result.LoadOrder.Should().ContainSingle(p => p.Id == "solo");
        }

        [Fact]
        public void ComputeLoadOrder_WithDeps_RespectsDependencyOrder()
        {
            PackManifest core = MakePack("core", loadOrder: 50);
            PackManifest addon = MakePack("addon", dependsOn: new[] { "core" }, loadOrder: 100);
            var packs = new List<PackManifest> { addon, core }; // deliberately reversed

            DependencyResult result = _resolver.ComputeLoadOrder(packs);

            result.IsSuccess.Should().BeTrue();
            var ids = result.LoadOrder.Select(p => p.Id).ToList();
            ids.IndexOf("core").Should().BeLessThan(ids.IndexOf("addon"), "core must be loaded before addon");
        }

        [Fact]
        public void ComputeLoadOrder_CircularDeps_Fails()
        {
            PackManifest alpha = MakePack("alpha", dependsOn: new[] { "beta" });
            PackManifest beta = MakePack("beta", dependsOn: new[] { "alpha" });
            var packs = new List<PackManifest> { alpha, beta };

            DependencyResult result = _resolver.ComputeLoadOrder(packs);

            result.IsSuccess.Should().BeFalse();
            result.Errors[0].Should().Contain("ircular");
        }
    }
}
