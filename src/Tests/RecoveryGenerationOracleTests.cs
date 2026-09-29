using System;
using System.IO;
using DINOForge.SDK;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    /// <summary>
    /// Mature-first recovery oracles. These are deliberately stronger than the
    /// existing hot-reload smoke tests and are expected to expose current
    /// generation-replacement gaps. Non-grading until executed/reviewed against
    /// the bound source candidate.
    /// </summary>
    public sealed class RecoveryGenerationOracleTests : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(), "dinoforge_recovery_generation_" + Guid.NewGuid().ToString("N"));
        private readonly RegistryManager _registry = new RegistryManager();
        private readonly ContentLoader _loader;

        public RecoveryGenerationOracleTests()
        {
            Directory.CreateDirectory(_root);
            _loader = new ContentLoader(_registry);
        }

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        }

        [Fact]
        public void ReloadPack_RemovedItem_NoLongerEffectiveOrReportsUnsupported()
        {
            string pack = CreatePack("recovery-pack", "1.0.0");
            WriteUnits(pack,
                Unit("recovery-x", "X v1", 10),
                Unit("recovery-y", "Y v1", 20));

            _loader.LoadPack(pack).Errors.Should().BeEmpty();
            _registry.Units.Get("recovery-y").Should().NotBeNull();

            File.WriteAllText(Path.Combine(pack, "pack.yaml"), Manifest("recovery-pack", "2.0.0"));
            WriteUnits(pack, Unit("recovery-x", "X v2", 30)); // y deliberately removed

            ContentLoadResult reload = ((DINOForge.SDK.HotReload.IPackReloadService)_loader).ReloadPack(pack);
            reload.Errors.Should().BeEmpty(
                "if hot removal is unsupported, reload must return an explicit error/restart-required outcome instead of silent stale state");

            _registry.Units.Get("recovery-y").Should().BeNull(
                "content removed from the current pack generation must not remain effective after a successful reload");
            _registry.Units.Get("recovery-x")!.DisplayName.Should().Be("X v2");
        }

        [Fact]
        public void ReloadPack_RepeatedIdenticalBytes_ConvergesWithoutConflictAccumulation()
        {
            string pack = CreatePack("repeat-pack", "1.0.0");
            WriteUnits(pack, Unit("repeat-x", "Repeat", 10));

            _loader.LoadPack(pack).Errors.Should().BeEmpty();
            int conflictsAfterFirst = _registry.Units.DetectConflicts().Count;

            for (int i = 0; i < 3; i++)
                ((DINOForge.SDK.HotReload.IPackReloadService)_loader).ReloadPack(pack)
                    .Errors.Should().BeEmpty();

            _registry.Units.DetectConflicts().Count.Should().Be(
                conflictsAfterFirst,
                "reloading identical candidate bytes must converge rather than manufacture same-pack generation conflicts");
        }

        private string CreatePack(string id, string version)
        {
            string pack = Path.Combine(_root, id);
            Directory.CreateDirectory(pack);
            File.WriteAllText(Path.Combine(pack, "pack.yaml"), Manifest(id, version));
            return pack;
        }

        private static string Manifest(string id, string version) =>
$@"id: {id}
name: Recovery {id}
version: {version}
author: recovery
type: content
load_order: 100
loads:
  units:
    - units.yaml
";

        private static void WriteUnits(string pack, params string[] units) =>
            File.WriteAllText(Path.Combine(pack, "units.yaml"), string.Join(Environment.NewLine, units));

        private static string Unit(string id, string name, int hp) =>
$@"- id: {id}
  display_name: {name}
  unit_class: CoreLineInfantry
  faction_id: recovery-faction
  stats:
    hp: {hp}
    accuracy: 0.7
";
    }
}
