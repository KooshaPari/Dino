using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
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

        public Generation Build(string packsRoot, string? generationId = null)
        {
            var registries = new RegistryManager();
            var loader = new ContentLoader(registries);
            ContentLoadResult result = loader.LoadPacks(packsRoot);
            return new Generation(generationId ?? ComputeGenerationId(packsRoot), registries, result);
        }

        public static string ComputeGenerationId(string packsRoot)
        {
            using var sha = SHA256.Create();
            using var stream = new MemoryStream();
            foreach (string file in Directory.EnumerateFiles(packsRoot, "*", SearchOption.AllDirectories)
                         .OrderBy(p => Path.GetRelativePath(packsRoot, p), StringComparer.Ordinal))
            {
                string relative = Path.GetRelativePath(packsRoot, file).Replace('\\', '/');
                byte[] name = Encoding.UTF8.GetBytes(relative);
                stream.Write(BitConverter.GetBytes(name.Length));
                stream.Write(name);
                byte[] bytes = File.ReadAllBytes(file);
                stream.Write(BitConverter.GetBytes(bytes.Length));
                stream.Write(bytes);
            }
            stream.Position = 0;
            return Convert.ToHexString(sha.ComputeHash(stream)).ToLowerInvariant();
        }

        public bool TryPublish(Generation candidate)
        {
            bool hasHardErrors = candidate.Result.Errors.Any(e =>
                !e.StartsWith("[patch]", StringComparison.OrdinalIgnoreCase) &&
                !e.StartsWith("Applying ", StringComparison.OrdinalIgnoreCase));
            if (hasHardErrors)
                return false;
            Published = candidate;
            return true;
        }
    }

    internal sealed class RecoveryGenerationReceipt
    {
        public string DesiredGeneration { get; }
        public string? ActiveGeneration { get; }
        public bool FullyActive { get; }
        public System.Collections.Generic.IReadOnlyDictionary<string, string> Nacks { get; }

        public RecoveryGenerationReceipt(
            string desiredGeneration,
            string? activeGeneration,
            bool fullyActive,
            System.Collections.Generic.IReadOnlyDictionary<string, string> nacks)
        {
            DesiredGeneration = desiredGeneration;
            ActiveGeneration = activeGeneration;
            FullyActive = fullyActive;
            Nacks = nacks;
        }
    }

    internal sealed class RecoveryActivationCoordinator
    {
        private readonly System.Collections.Generic.HashSet<string> _required;
        private readonly System.Collections.Generic.HashSet<string> _acks = new();
        private readonly System.Collections.Generic.Dictionary<string, string> _nacks = new();

        public RecoveryGenerationPrototype.Generation? Desired { get; private set; }
        public RecoveryGenerationPrototype.Generation? Active { get; private set; }

        public RecoveryActivationCoordinator(System.Collections.Generic.IEnumerable<string> requiredConsumers)
        {
            _required = new System.Collections.Generic.HashSet<string>(requiredConsumers, StringComparer.Ordinal);
        }

        public void Propose(RecoveryGenerationPrototype.Generation generation)
        {
            Desired = generation;
            _acks.Clear();
            _nacks.Clear();
        }

        public bool Ack(string consumer, string generationId)
        {
            if (Desired == null || !_required.Contains(consumer) ||
                !string.Equals(Desired.Id, generationId, StringComparison.Ordinal))
                return false;
            _nacks.Remove(consumer);
            _acks.Add(consumer);
            if (_required.SetEquals(_acks))
                Active = Desired;
            return true;
        }

        public bool Nack(string consumer, string generationId, string reason)
        {
            if (Desired == null || !_required.Contains(consumer) ||
                !string.Equals(Desired.Id, generationId, StringComparison.Ordinal))
                return false;
            _acks.Remove(consumer);
            _nacks[consumer] = reason;
            return true;
        }

        public bool IsFullyActive => Desired != null && Active != null &&
            string.Equals(Desired.Id, Active.Id, StringComparison.Ordinal) && _required.SetEquals(_acks);

        public System.Collections.Generic.IReadOnlyDictionary<string, string> Nacks => _nacks;

        public RecoveryGenerationReceipt Receipt() =>
            new RecoveryGenerationReceipt(
                Desired?.Id ?? "",
                Active?.Id,
                IsFullyActive,
                new System.Collections.Generic.Dictionary<string, string>(_nacks, StringComparer.Ordinal));
    }

    internal sealed class RecoveryGenerationObservations
    {
        private readonly string _publishedGeneration;
        private readonly System.Collections.Generic.HashSet<string> _required;
        private readonly System.Collections.Generic.HashSet<string> _observed = new();

        public RecoveryGenerationObservations(string publishedGeneration, System.Collections.Generic.IEnumerable<string> requiredConsumers)
        {
            _publishedGeneration = publishedGeneration;
            _required = new System.Collections.Generic.HashSet<string>(requiredConsumers, StringComparer.Ordinal);
        }

        public bool Acknowledge(string consumer, string generation)
        {
            if (!_required.Contains(consumer) || !string.Equals(generation, _publishedGeneration, StringComparison.Ordinal))
                return false;
            _observed.Add(consumer);
            return true;
        }

        public bool IsFullyObserved => _required.SetEquals(_observed);
    }


    internal enum RecoveryConsumerDisposition
    {
        LookupOnDemand,
        MaterializedReconciled,
        RestartRequired
    }

    internal sealed class RecoveryConsumerAdapter
    {
        public string Id { get; }
        public RecoveryConsumerDisposition Disposition { get; private set; }
        public string? ObservedGeneration { get; private set; }
        public string? MaterializedGeneration { get; private set; }

        public RecoveryConsumerAdapter(string id, RecoveryConsumerDisposition disposition, string? materializedGeneration = null)
        {
            Id = id;
            Disposition = disposition;
            MaterializedGeneration = materializedGeneration;
        }

        public bool ApplyLookupGeneration(string generation)
        {
            if (Disposition != RecoveryConsumerDisposition.LookupOnDemand)
                return false;
            ObservedGeneration = generation;
            return true;
        }

        public bool ReconcileMaterialized(string generation)
        {
            if (Disposition != RecoveryConsumerDisposition.MaterializedReconciled)
                return false;
            ObservedGeneration = generation;
            MaterializedGeneration = generation;
            return true;
        }

        public void RequireRestart(string desiredGeneration)
        {
            Disposition = RecoveryConsumerDisposition.RestartRequired;
            ObservedGeneration = desiredGeneration;
        }

        public bool Qualifies(string desiredGeneration) =>
            Disposition switch
            {
                RecoveryConsumerDisposition.LookupOnDemand =>
                    string.Equals(ObservedGeneration, desiredGeneration, StringComparison.Ordinal),
                RecoveryConsumerDisposition.MaterializedReconciled =>
                    string.Equals(ObservedGeneration, desiredGeneration, StringComparison.Ordinal) &&
                    string.Equals(MaterializedGeneration, desiredGeneration, StringComparison.Ordinal),
                RecoveryConsumerDisposition.RestartRequired => false,
                _ => false
            };
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
            File.WriteAllText(Path.Combine(target, "pack.yaml"),
@"id: pack-b
name: Pack B
version: 1.0.0
framework_version: '>=0.1.0 <99.0.0'
author: recovery
type: content
load_order: 100
");
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
            publisher.TryPublish(g1).Should().BeTrue(
                "fresh generation should accept the repository-standard patch fixture; errors: {0}",
                string.Join(" | ", g1.Result.Errors));
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

        [Fact]
        public void GenerationId_IsStableForIdenticalBytes_AndChangesWithCandidateBytes()
        {
            string pack = CreatePack("identity-pack", "1.0.0");
            WriteUnits(pack, Unit("id-x", "Identity", 10));

            string a = RecoveryGenerationPrototype.ComputeGenerationId(_root);
            string b = RecoveryGenerationPrototype.ComputeGenerationId(_root);
            b.Should().Be(a, "identical candidate bytes must produce the same generation identity");

            WriteUnits(pack, Unit("id-x", "Identity v2", 11));
            string changed = RecoveryGenerationPrototype.ComputeGenerationId(_root);
            changed.Should().NotBe(a, "candidate identity must change when effective input bytes change");
        }

        [Fact]
        public void GenerationReceipt_CannotReportFullyActiveWhileRequiredConsumerNacks()
        {
            string pack = CreatePack("receipt-pack", "1.0.0");
            WriteUnits(pack, Unit("receipt-x", "Receipt", 10));

            var builder = new RecoveryGenerationPrototype();
            var g1 = builder.Build(_root);
            var coordinator = new RecoveryActivationCoordinator(new[] { "spawner", "menu" });
            coordinator.Propose(g1);
            coordinator.Ack("spawner", g1.Id).Should().BeTrue();
            coordinator.Nack("menu", g1.Id, "apply failed").Should().BeTrue();

            RecoveryGenerationReceipt receipt = coordinator.Receipt();
            receipt.DesiredGeneration.Should().Be(g1.Id);
            receipt.FullyActive.Should().BeFalse();
            receipt.Nacks.Should().ContainKey("menu");
            receipt.ActiveGeneration.Should().BeNull(
                "no prior fully active generation exists and a required consumer rejected this one");
        }

        [Fact]
        public void TwoPhaseActivation_NackKeepsPriorActiveGenerationUntilAllConsumersAck()
        {
            string pack = CreatePack("two-phase-pack", "1.0.0");
            WriteUnits(pack, Unit("phase-x", "Phase v1", 10));

            var builder = new RecoveryGenerationPrototype();
            var g1 = builder.Build(_root);
            var coordinator = new RecoveryActivationCoordinator(new[] { "spawner", "build-menu" });
            coordinator.Propose(g1);
            coordinator.Ack("spawner", g1.Id).Should().BeTrue();
            coordinator.IsFullyActive.Should().BeFalse();
            coordinator.Ack("build-menu", g1.Id).Should().BeTrue();
            coordinator.Active!.Id.Should().Be(g1.Id);

            WriteUnits(pack, Unit("phase-x", "Phase v2", 20));
            var g2 = builder.Build(_root);
            g2.Id.Should().NotBe(g1.Id);
            coordinator.Propose(g2);

            coordinator.Ack("spawner", g2.Id).Should().BeTrue();
            coordinator.Nack("build-menu", g2.Id, "cannot apply live").Should().BeTrue();
            coordinator.Active!.Id.Should().Be(g1.Id,
                "a NACKed desired generation must not become the fully active generation");
            coordinator.IsFullyActive.Should().BeFalse();
            coordinator.Nacks.Should().ContainKey("build-menu");

            coordinator.Ack("build-menu", g2.Id).Should().BeTrue();
            coordinator.Active!.Id.Should().Be(g2.Id);
            coordinator.IsFullyActive.Should().BeTrue();
        }

        [Fact]
        public void ConsumerAcknowledgement_DistinguishesPublishedFromFullyObservedGeneration()
        {
            string pack = CreatePack("ack-pack", "1.0.0");
            WriteUnits(pack, Unit("ack-x", "Ack", 10));

            var publisher = new RecoveryGenerationPrototype();
            var g1 = publisher.Build(_root, "g1");
            publisher.TryPublish(g1).Should().BeTrue();

            var observations = new RecoveryGenerationObservations(
                g1.Id,
                new[] { "spawner", "build-menu", "wave-injector" });

            observations.IsFullyObserved.Should().BeFalse();
            observations.Acknowledge("spawner", g1.Id).Should().BeTrue();
            observations.Acknowledge("build-menu", "g0").Should().BeFalse(
                "a consumer acknowledgement for a stale generation must not qualify the published generation");
            observations.IsFullyObserved.Should().BeFalse();
            observations.Acknowledge("build-menu", g1.Id).Should().BeTrue();
            observations.Acknowledge("wave-injector", g1.Id).Should().BeTrue();
            observations.IsFullyObserved.Should().BeTrue();
        }


        [Fact]
        public void RealConsumerClasses_PointerRebindCannotQualifyOneShotMaterialization()
        {
            var spawner = new RecoveryConsumerAdapter("pack-unit-spawner", RecoveryConsumerDisposition.LookupOnDemand);
            var buildMenu = new RecoveryConsumerAdapter(
                "build-menu",
                RecoveryConsumerDisposition.MaterializedReconciled,
                materializedGeneration: "g1");

            spawner.ApplyLookupGeneration("g2").Should().BeTrue();
            spawner.Qualifies("g2").Should().BeTrue(
                "future PackUnitSpawner-style lookups can qualify after rebinding");

            buildMenu.Qualifies("g2").Should().BeFalse(
                "BuildMenuInjector-style cached/live materialization remains on g1 until explicit reconciliation");
            buildMenu.ReconcileMaterialized("g2").Should().BeTrue();
            buildMenu.Qualifies("g2").Should().BeTrue();
        }

        [Fact]
        public void RestartRequiredConsumer_CannotAckDesiredGenerationAsFullyApplied()
        {
            var aerialSweep = new RecoveryConsumerAdapter(
                "aerial-building-sweep",
                RecoveryConsumerDisposition.MaterializedReconciled,
                materializedGeneration: "g1");

            aerialSweep.RequireRestart("g2");
            aerialSweep.ObservedGeneration.Should().Be("g2",
                "the consumer can observe the desired generation without having materialized it");
            aerialSweep.MaterializedGeneration.Should().Be("g1");
            aerialSweep.Qualifies("g2").Should().BeFalse(
                "restart-required is a first-class non-ACK state, not a green");
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
