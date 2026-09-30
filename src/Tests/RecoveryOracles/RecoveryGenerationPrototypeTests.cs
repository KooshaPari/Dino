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
    /// Architecture experiment only: build each candidate in fresh SDK state and
    /// publish the generation handle only after construction succeeds.
    /// This does not change production ContentLoader/RegistryManager behavior.
    /// </summary>
    internal sealed class RecoveryGenerationPrototype
    {
        public sealed class Generation
        {
            public string Id { get; }
            public RegistryManager Registries { get; }
            public ContentLoadResult Result { get; }

            public Generation(string id, RegistryManager registries, ContentLoadResult result)
            {
                Id = id;
                Registries = registries;
                Result = result;
            }
        }

        public Generation? Published { get; private set; }

        public Generation Build(string packsRoot, string generationId)
        {
            var registries = new RegistryManager();
            var loader = new ContentLoader(registries);
            ContentLoadResult result = loader.LoadPacks(packsRoot);
            return new Generation(generationId, registries, result);
        }

        public bool TryPublish(Generation candidate)
        {
            bool hasHardErrors = candidate.Result.Errors.Any(
                e => !e.StartsWith("[patch]", StringComparison.OrdinalIgnoreCase));
            if (hasHardErrors)
                return false;
            Published = candidate;
            return true;
        }
    }

    public sealed class RecoveryGenerationPrototypeTests : IDisposable
    {
        private readonly string _root = Path.Combine(
            Path.GetTempPath(), "dinoforge_generation_prototype_" + Guid.NewGuid().ToString("N"));

        public RecoveryGenerationPrototypeTests() => Directory.CreateDirectory(_root);

        public void Dispose()
        {
            try { if (Directory.Exists(_root)) Directory.Delete(_root, true); } catch { }
        }

        [Fact]
        public void FreshGeneration_RemovalConvergesWithoutHistoricalRegistration()
        {
            string pack = CreatePack("gen-pack", "1.0.0");
            WriteUnits(pack, Unit("x", "X v1", 10), Unit("y", "Y v1", 20));

            var publisher = new RecoveryGenerationPrototype();
            var g1 = publisher.Build(_root, "g1");
            publisher.TryPublish(g1).Should().BeTrue();
            publisher.Published!.Registries.Units.Get("y").Should().NotBeNull();

            File.WriteAllText(Path.Combine(pack, "pack.yaml"), Manifest("gen-pack", "2.0.0"));
            WriteUnits(pack, Unit("x", "X v2", 30));

            var g2 = publisher.Build(_root, "g2");
            publisher.TryPublish(g2).Should().BeTrue();
            publisher.Published!.Registries.Units.Get("y").Should().BeNull();
            publisher.Published.Registries.Units.Get("x")!.DisplayName.Should().Be("X v2");
            publisher.Published.Registries.Units.DetectConflicts().Should().BeEmpty();
        }

        [Fact]
        public void FreshGeneration_RemovingPatchUsesCurrentDiskBytes()
        {
            string target = CreatePack("pack-b", "1.0.0");
            Directory.CreateDirectory(Path.Combine(target, "units"));
            File.WriteAllText(Path.Combine(target, "units", "warrior.yaml"), Unit("warrior", "Warrior", 100));

            string patcher = CreatePack("pack-a", "1.0.0");
            File.WriteAllText(Path.Combine(patcher, "pack.yaml"),
@"id: pack-a
name: Pack A
version: 1.0.0
framework_version: '>=0.1.0 <99.0.0'
author: recovery
type: content
load_order: 100
patches:
  - target_pack: pack-b
    operations:
      - op: replace
        path: /units/warrior/stats/hp
        value: 150
");

            var publisher = new RecoveryGenerationPrototype();
            var g1 = publisher.Build(_root, "g1");
            publisher.TryPublish(g1).Should().BeTrue();
            publisher.Published!.Registries.Units.Get("warrior")!.Stats.Hp.Should().BeApproximately(150f, 0.01f);

            File.WriteAllText(Path.Combine(patcher, "pack.yaml"), Manifest("pack-a", "2.0.0"));
            File.WriteAllText(Path.Combine(target, "units", "warrior.yaml"), Unit("warrior", "Warrior v2", 200));

            var g2 = publisher.Build(_root, "g2");
            publisher.TryPublish(g2).Should().BeTrue();
            publisher.Published!.Registries.Units.Get("warrior")!.Stats.Hp.Should().BeApproximately(200f, 0.01f);
        }

        [Fact]
        public void FailedCandidate_DoesNotReplacePublishedGeneration()
        {
            string pack = CreatePack("safe-pack", "1.0.0");
            WriteUnits(pack, Unit("safe-x", "Safe", 10));

            var publisher = new RecoveryGenerationPrototype();
            publisher.TryPublish(publisher.Build(_root, "g1")).Should().BeTrue();
            var accepted = publisher.Published;

            File.WriteAllText(Path.Combine(pack, "units.yaml"), "this: is: invalid: yaml: [");
            var bad = publisher.Build(_root, "g2-bad");
            publisher.TryPublish(bad).Should().BeFalse();
            publisher.Published.Should().BeSameAs(accepted);
            publisher.Published!.Registries.Units.Get("safe-x").Should().NotBeNull();
        }

        private string CreatePack(string id, string version, string extra = "")
        {
            string pack = Path.Combine(_root, id);
            Directory.CreateDirectory(pack);
            File.WriteAllText(Path.Combine(pack, "pack.yaml"), Manifest(id, version, extra));
            File.WriteAllText(Path.Combine(pack, "units.yaml"), string.Empty);
            return pack;
        }

        private static string Manifest(string id, string version, string extra = "") =>
$@"id: {id}
name: Recovery {id}
version: {version}
framework_version: '>=0.1.0 <99.0.0'
author: recovery
type: content
load_order: 100
loads:
  units:
    - units.yaml
{extra}";

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
