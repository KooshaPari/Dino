using DINOForge.Runtime.Generation;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public sealed class GenerationStoreIntegrationCandidateTests
    {
        [Fact]
        public void ActualLookupConsumers_AckFutureLookupGeneration()
        {
            var generation = new RuntimeGeneration("g2", new RegistryManager());

            var spawner = DINOForge.Runtime.Bridge.PackUnitSpawner.ApplyLookupGeneration(generation);
            var waves = DINOForge.Runtime.Bridge.WaveInjector.ApplyLookupGeneration(generation);

            spawner.Qualifies.Should().BeTrue();
            waves.Qualifies.Should().BeTrue();
            spawner.ObservedGeneration.Should().Be("g2");
            waves.ObservedGeneration.Should().Be("g2");
            spawner.Detail.Should().Contain("existing spawned entities are out of scope");
            waves.Detail.Should().Contain("active waves retain materialized definitions");
        }

        [Fact]
        public void NackOrMissingRequiredConsumer_KeepsPriorGenerationActive()
        {
            var store = new GenerationStore(new[] { "spawner", "build-menu" });
            var g1 = new RuntimeGeneration("g1", new RegistryManager());
            var g2 = new RuntimeGeneration("g2", new RegistryManager());
            store.SeedActive(g1);
            store.BeginProposal(g2);

            store.Record(new GenerationConsumerResult(
                "spawner", "g2", GenerationConsumerDisposition.LookupOnDemand,
                GenerationConsumerStatus.Ack, observedGeneration: "g2")).Should().BeTrue();

            var receipt = store.Evaluate();
            receipt.Committed.Should().BeFalse();
            receipt.ActiveGeneration.Should().Be("g1");
            receipt.DesiredGeneration.Should().Be("g2");
        }

        [Fact]
        public void MaterializedConsumer_MustReportMaterializedGenerationBeforeCommit()
        {
            var store = new GenerationStore(new[] { "spawner", "build-menu" });
            store.SeedActive(new RuntimeGeneration("g1", new RegistryManager()));
            store.BeginProposal(new RuntimeGeneration("g2", new RegistryManager()));

            store.Record(new GenerationConsumerResult(
                "spawner", "g2", GenerationConsumerDisposition.LookupOnDemand,
                GenerationConsumerStatus.Ack, observedGeneration: "g2")).Should().BeTrue();

            store.Record(new GenerationConsumerResult(
                "build-menu", "g2", GenerationConsumerDisposition.MaterializedReconciliable,
                GenerationConsumerStatus.Ack, observedGeneration: "g2",
                materializedGeneration: "g1")).Should().BeTrue();

            store.Evaluate().Committed.Should().BeFalse();

            store.Record(new GenerationConsumerResult(
                "build-menu", "g2", GenerationConsumerDisposition.MaterializedReconciliable,
                GenerationConsumerStatus.Ack, observedGeneration: "g2",
                materializedGeneration: "g2")).Should().BeTrue();

            var committed = store.Evaluate();
            committed.Committed.Should().BeTrue();
            committed.ActiveGeneration.Should().Be("g2");
        }

        [Fact]
        public void StaleConsumerAck_IsRejected()
        {
            var store = new GenerationStore(new[] { "spawner" });
            store.SeedActive(new RuntimeGeneration("g1", new RegistryManager()));
            store.BeginProposal(new RuntimeGeneration("g2", new RegistryManager()));

            store.Record(new GenerationConsumerResult(
                "spawner", "g1", GenerationConsumerDisposition.LookupOnDemand,
                GenerationConsumerStatus.Ack, observedGeneration: "g1")).Should().BeFalse();

            store.Evaluate().Committed.Should().BeFalse();
            store.Active!.Id.Should().Be("g1");
        }

        [Fact]
        public void RestartRequired_IsNotACommitGreen()
        {
            var store = new GenerationStore(new[] { "aerial-building-sweep" });
            store.SeedActive(new RuntimeGeneration("g1", new RegistryManager()));
            store.BeginProposal(new RuntimeGeneration("g2", new RegistryManager()));

            store.Record(new GenerationConsumerResult(
                "aerial-building-sweep", "g2", GenerationConsumerDisposition.RestartRequired,
                GenerationConsumerStatus.RestartRequired, observedGeneration: "g2",
                materializedGeneration: "g1")).Should().BeTrue();

            var receipt = store.Evaluate();
            receipt.Committed.Should().BeFalse();
            receipt.ActiveGeneration.Should().Be("g1");
        }
    }
}
