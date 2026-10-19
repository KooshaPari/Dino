using System;
using System.IO;
using System.Linq;
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

        [Fact]
        public void ReloadPacks_RemovingPatch_DoesNotReuseStalePatchedYaml()
        {
            string target = Path.Combine(_root, "pack-b");
            Directory.CreateDirectory(Path.Combine(target, "units"));
            File.WriteAllText(Path.Combine(target, "pack.yaml"),
@"id: pack-b
name: Pack B
version: 1.0.0
framework_version: '>=0.1.0 <99.0.0'
type: content
");
            string unitPath = Path.Combine(target, "units", "warrior.yaml");
            File.WriteAllText(unitPath, Unit("warrior", "Warrior", 100));

            string patcher = Path.Combine(_root, "pack-a");
            Directory.CreateDirectory(patcher);
            File.WriteAllText(Path.Combine(patcher, "pack.yaml"),
@"id: pack-a
name: Pack A
version: 1.0.0
framework_version: '>=0.1.0 <99.0.0'
type: content
patches:
  - target_pack: pack-b
    operations:
      - op: replace
        path: /units/warrior/stats/hp
        value: 150
");

            ContentLoadResult initial = _loader.LoadPacks(_root);
            initial.Errors.Where(e => !e.StartsWith("[patch]", StringComparison.OrdinalIgnoreCase))
                .Should().BeEmpty("patch progress messages are currently multiplexed into Errors but are not hard failures");
            _registry.Units.Get("warrior")!.Stats.Hp.Should().BeApproximately(150f, 0.01f);

            // New generation: patch removed and target bytes changed at the same path.
            File.WriteAllText(Path.Combine(patcher, "pack.yaml"),
@"id: pack-a
name: Pack A
version: 2.0.0
framework_version: '>=0.1.0 <99.0.0'
type: content
");
            File.WriteAllText(unitPath, Unit("warrior", "Warrior v2", 200));

            ContentLoadResult reload = _loader.LoadPacks(_root);
            reload.Errors.Where(e => !e.StartsWith("[patch]", StringComparison.OrdinalIgnoreCase))
                .Should().BeEmpty(
                    "a successful new generation must not silently consume patched bytes cached from the prior patch set");

            _registry.Units.Get("warrior")!.Stats.Hp.Should().BeApproximately(
                200f, 0.01f,
                "removing the patch must expose the current on-disk target bytes rather than stale patched YAML");
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
