using System.Collections.Generic;
using System.Linq;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public record TestItem(string Name);

    public class RegistryTests
    {
        [Fact]
        public void Register_And_Get_ReturnsEntry()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("Raptor"), RegistrySource.BaseGame, "base-game");

            TestItem? result = registry.Get("raptor");

            result.Should().NotBeNull();
            result!.Name.Should().Be("Raptor");
        }

        [Fact]
        public void Get_NonExistent_ReturnsNull()
        {
            var registry = new Registry<TestItem>();

            TestItem? result = registry.Get("does-not-exist");

            result.Should().BeNull();
        }

        [Fact]
        public void Contains_Registered_ReturnsTrue()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("Raptor"), RegistrySource.BaseGame, "base-game");

            registry.Contains("raptor").Should().BeTrue();
        }

        [Fact]
        public void Contains_NotRegistered_ReturnsFalse()
        {
            var registry = new Registry<TestItem>();

            registry.Contains("raptor").Should().BeFalse();
        }

        [Fact]
        public void All_ReturnsHighestPriorityEntries()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("BaseRaptor"), RegistrySource.BaseGame, "base-game");
            registry.Register("raptor", new TestItem("PackRaptor"), RegistrySource.Pack, "dino-pack");

            var all = registry.All;

            all.Should().ContainKey("raptor");
            all["raptor"].Data.Name.Should().Be("PackRaptor");
        }

        [Fact]
        public void Override_HigherPriority_ReplacesExisting()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("BaseRaptor"), RegistrySource.BaseGame, "base-game");
            registry.Override("raptor", new TestItem("PackRaptor"), RegistrySource.Pack, "dino-pack");

            registry.Get("raptor")!.Name.Should().Be("PackRaptor");
        }

        [Fact]
        public void Override_SamePriority_CreatesConflict()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("PackA"), RegistrySource.Pack, "pack-a", 100);
            registry.Override("raptor", new TestItem("PackB"), RegistrySource.Pack, "pack-b", 100);

            var conflicts = registry.DetectConflicts();

            conflicts.Should().HaveCount(1);
            conflicts[0].EntryId.Should().Be("raptor");
        }

        [Fact]
        public void DetectConflicts_NoConflicts_ReturnsEmpty()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("BaseRaptor"), RegistrySource.BaseGame, "base-game");
            registry.Register("raptor", new TestItem("PackRaptor"), RegistrySource.Pack, "dino-pack");

            var conflicts = registry.DetectConflicts();

            conflicts.Should().BeEmpty();
        }

        [Fact]
        public void DetectConflicts_SamePriority_ReturnsConflict()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("Alpha"), RegistrySource.Pack, "pack-alpha", 100);
            registry.Register("raptor", new TestItem("Beta"), RegistrySource.Pack, "pack-beta", 100);

            var conflicts = registry.DetectConflicts();

            conflicts.Should().HaveCount(1);
            conflicts[0].EntryId.Should().Be("raptor");
        }

        [Fact]
        public void Register_MultipleEntries_AllAccessible()
        {
            var registry = new Registry<TestItem>();
            registry.Register("raptor", new TestItem("Raptor"), RegistrySource.BaseGame, "base");
            registry.Register("triceratops", new TestItem("Triceratops"), RegistrySource.BaseGame, "base");
            registry.Register("ankylosaur", new TestItem("Ankylosaur"), RegistrySource.BaseGame, "base");

            registry.Contains("raptor").Should().BeTrue();
            registry.Contains("triceratops").Should().BeTrue();
            registry.Contains("ankylosaur").Should().BeTrue();
            registry.Get("triceratops")!.Name.Should().Be("Triceratops");
        }
    }
}
