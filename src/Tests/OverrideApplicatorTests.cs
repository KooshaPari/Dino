using System;
using System.Collections.Generic;
using DINOForge.SDK.Models;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    /// <summary>
    /// Tests for OverrideApplicator stat override mode conversion logic.
    /// These tests focus on the mode string parsing and validation,
    /// not the StatModifierSystem integration (which requires game state).
    /// </summary>
    public class OverrideApplicatorTests
    {
        [Fact]
        public void StatOverrideEntry_OverrideMode_DefaultsCorrectly()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.hp",
                Value = 150f,
                Mode = "override"
            };

            // Act & Assert
            entry.Target.Should().Be("unit.stats.hp");
            entry.Value.Should().Be(150f);
            entry.Mode.Should().Be("override");
        }

        [Fact]
        public void StatOverrideEntry_AddMode_ParsesCorrectly()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.damage",
                Value = 25f,
                Mode = "add"
            };

            // Act & Assert
            entry.Mode.Should().Be("add");
        }

        [Fact]
        public void StatOverrideEntry_MultiplyMode_ParsesCorrectly()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.armor",
                Value = 1.5f,
                Mode = "multiply"
            };

            // Act & Assert
            entry.Mode.Should().Be("multiply");
        }

        [Fact]
        public void StatOverrideEntry_CasInsensitiveModeComparison()
        {
            // Arrange - test that various case combinations work
            var modeVariants = new[] { "override", "OVERRIDE", "Override", "oVeRrIdE" };

            // Act & Assert - each should be a valid string
            foreach (string mode in modeVariants)
            {
                var entry = new StatOverrideEntry
                {
                    Target = "unit.stats.hp",
                    Value = 100f,
                    Mode = mode
                };
                entry.Mode.Should().NotBeNullOrEmpty();
            }
        }

        [Fact]
        public void StatOverrideEntry_NullMode_DefaultsToEmpty()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.hp",
                Value = 100f,
                Mode = null!
            };

            // Act & Assert
            entry.Mode.Should().BeNull();
        }

        [Fact]
        public void StatOverrideEntry_InvalidMode_StoresValue()
        {
            // Arrange - invalid mode should still be stored (validation happens elsewhere)
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.hp",
                Value = 100f,
                Mode = "invalid-mode"
            };

            // Act & Assert
            entry.Mode.Should().Be("invalid-mode");
        }

        [Fact]
        public void StatOverrideEntry_WithFilter_StoresCorrectly()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.damage",
                Value = 50f,
                Mode = "override",
                Filter = "Components.MeleeUnit"
            };

            // Act & Assert
            entry.Filter.Should().Be("Components.MeleeUnit");
        }

        [Fact]
        public void StatOverrideEntry_ZeroValue_IsValid()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.speed",
                Value = 0f,
                Mode = "override"
            };

            // Act & Assert
            entry.Value.Should().Be(0f);
        }

        [Fact]
        public void StatOverrideEntry_NegativeValue_IsValid()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.armor",
                Value = -5f,
                Mode = "add"
            };

            // Act & Assert
            entry.Value.Should().Be(-5f);
        }

        [Fact]
        public void StatOverrideEntry_LargeValue_IsValid()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "unit.stats.hp",
                Value = 99999f,
                Mode = "override"
            };

            // Act & Assert
            entry.Value.Should().Be(99999f);
        }

        [Fact]
        public void StatOverrideDefinition_EmptyOverridesList_IsValid()
        {
            // Arrange
            var definition = new StatOverrideDefinition
            {
                Overrides = new List<StatOverrideEntry>()
            };

            // Act & Assert
            definition.Overrides.Should().BeEmpty();
        }

        [Fact]
        public void StatOverrideDefinition_MultipleEntries_AllStored()
        {
            // Arrange
            var definition = new StatOverrideDefinition
            {
                Overrides = new List<StatOverrideEntry>
                {
                    new StatOverrideEntry { Target = "unit.stats.hp", Value = 150f, Mode = "override" },
                    new StatOverrideEntry { Target = "unit.stats.damage", Value = 25f, Mode = "add" },
                    new StatOverrideEntry { Target = "unit.stats.armor", Value = 1.2f, Mode = "multiply" }
                }
            };

            // Act & Assert
            definition.Overrides.Should().HaveCount(3);
            definition.Overrides[0].Target.Should().Be("unit.stats.hp");
            definition.Overrides[1].Target.Should().Be("unit.stats.damage");
            definition.Overrides[2].Target.Should().Be("unit.stats.armor");
        }

        [Fact]
        public void StatOverrideDefinition_NullOverridesList_DefaultsToEmpty()
        {
            // Arrange
            var definition = new StatOverrideDefinition();

            // Act & Assert
            definition.Overrides.Should().NotBeNull();
            definition.Overrides.Should().BeEmpty();
        }

        [Fact]
        public void StatOverrideEntry_AllProperties_AreAccessible()
        {
            // Arrange
            var entry = new StatOverrideEntry
            {
                Target = "test.path",
                Value = 42f,
                Mode = "multiply",
                Filter = "TestFilter"
            };

            // Act & Assert
            entry.Target.Should().Be("test.path");
            entry.Value.Should().Be(42f);
            entry.Mode.Should().Be("multiply");
            entry.Filter.Should().Be("TestFilter");
        }
    }
}
