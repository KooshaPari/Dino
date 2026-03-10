using System;
using System.Collections.Generic;
using System.IO;
using DINOForge.SDK;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class ContentLoaderTests : IDisposable
    {
        private readonly RegistryManager _registries;
        private readonly ContentLoader _loader;
        private readonly string _tempRoot;

        public ContentLoaderTests()
        {
            _registries = new RegistryManager();
            _loader = new ContentLoader(_registries);
            _tempRoot = Path.Combine(Path.GetTempPath(), "dinoforge_test_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempRoot);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }

        [Fact]
        public void LoadPack_ValidPack_Succeeds()
        {
            // Arrange
            string packDir = CreatePackDirectory("test-pack", @"
id: test-pack
name: Test Pack
version: 0.1.0
author: Test
type: content
loads:
  units:
    - units
");
            CreateContentFile(packDir, "units", "soldier.yaml", @"
id: soldier
display_name: Soldier
unit_class: CoreLineInfantry
faction_id: test-faction
tier: 1
stats:
  hp: 100
  damage: 15
");

            // Act
            ContentLoadResult result = _loader.LoadPack(packDir);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.LoadedPacks.Should().Contain("test-pack");
            result.Errors.Should().BeEmpty();
            _registries.Units.Contains("soldier").Should().BeTrue();
            _registries.Units.Get("soldier")!.DisplayName.Should().Be("Soldier");
        }

        [Fact]
        public void LoadPack_MissingManifest_Fails()
        {
            // Arrange
            string emptyDir = Path.Combine(_tempRoot, "empty-pack");
            Directory.CreateDirectory(emptyDir);

            // Act
            ContentLoadResult result = _loader.LoadPack(emptyDir);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle()
                .Which.Should().Contain("pack.yaml");
        }

        [Fact]
        public void LoadPack_InvalidContent_ReportsErrors()
        {
            // Arrange - create a pack with a YAML file that cannot be deserialized to a unit
            string packDir = CreatePackDirectory("bad-pack", @"
id: bad-pack
name: Bad Pack
version: 0.1.0
author: Test
type: content
loads:
  units:
    - units
");
            // Write a file that is valid YAML but will cause a deserialization exception
            // because it contains a nested array where a string is expected for 'id'
            CreateContentFile(packDir, "units", "broken.yaml", @"
id:
  - this
  - is
  - wrong
display_name: Broken
unit_class: CoreLineInfantry
faction_id: test
");

            // Act
            ContentLoadResult result = _loader.LoadPack(packDir);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void LoadPacks_ResolvesOrder()
        {
            // Arrange - create two packs where pack-b depends on pack-a
            string packsRoot = Path.Combine(_tempRoot, "packs");
            Directory.CreateDirectory(packsRoot);

            string packADir = CreatePackDirectory(
                "pack-a",
                @"
id: pack-a
name: Pack A
version: 0.1.0
author: Test
type: content
load_order: 10
loads:
  buildings:
    - buildings
",
                packsRoot);
            CreateContentFile(packADir, "buildings", "tower.yaml", @"
id: tower
display_name: Watch Tower
building_type: defense
health: 200
");

            string packBDir = CreatePackDirectory(
                "pack-b",
                @"
id: pack-b
name: Pack B
version: 0.1.0
author: Test
type: content
depends_on:
  - pack-a
load_order: 20
loads:
  units:
    - units
",
                packsRoot);
            CreateContentFile(packBDir, "units", "archer.yaml", @"
id: archer
display_name: Archer
unit_class: Skirmisher
faction_id: defenders
tier: 1
stats:
  hp: 60
  range: 10
");

            // Act
            ContentLoadResult result = _loader.LoadPacks(packsRoot);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.LoadedPacks.Should().HaveCount(2);
            result.LoadedPacks[0].Should().Be("pack-a");
            result.LoadedPacks[1].Should().Be("pack-b");
            _registries.Buildings.Contains("tower").Should().BeTrue();
            _registries.Units.Contains("archer").Should().BeTrue();
        }

        [Fact]
        public void LoadPack_NoLoadsSection_ScansConventionalDirs()
        {
            // Arrange - manifest without loads section, content in conventional dirs
            string packDir = CreatePackDirectory("auto-scan", @"
id: auto-scan
name: Auto Scan Pack
version: 0.1.0
author: Test
type: content
");
            CreateContentFile(packDir, "units", "scout.yaml", @"
id: scout
display_name: Scout
unit_class: Recon
faction_id: defenders
");

            // Act
            ContentLoadResult result = _loader.LoadPack(packDir);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _registries.Units.Contains("scout").Should().BeTrue();
        }

        [Fact]
        public void LoadPack_FactionContent_RegistersByFactionId()
        {
            // Arrange
            string packDir = CreatePackDirectory("faction-pack", @"
id: faction-pack
name: Faction Pack
version: 0.1.0
author: Test
type: content
loads:
  factions:
    - factions
");
            CreateContentFile(packDir, "factions", "rebels.yaml", @"
faction:
  id: rebels
  display_name: The Rebels
  theme: fantasy
  archetype: asymmetric
");

            // Act
            ContentLoadResult result = _loader.LoadPack(packDir);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _registries.Factions.Contains("rebels").Should().BeTrue();
            _registries.Factions.Get("rebels")!.Faction.DisplayName.Should().Be("The Rebels");
        }

        /// <summary>
        /// Helper: creates a pack directory with a pack.yaml manifest.
        /// </summary>
        private string CreatePackDirectory(string name, string manifestYaml, string? parentDir = null)
        {
            string dir = Path.Combine(parentDir ?? _tempRoot, name);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "pack.yaml"), manifestYaml);
            return dir;
        }

        /// <summary>
        /// Helper: creates a content file in a subdirectory of a pack.
        /// </summary>
        private void CreateContentFile(string packDir, string subDir, string fileName, string yamlContent)
        {
            string dir = Path.Combine(packDir, subDir);
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, fileName), yamlContent);
        }
    }
}
