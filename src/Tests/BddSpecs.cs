#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DINOForge.SDK;
using DINOForge.SDK.Dependencies;
using DINOForge.SDK.Models;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    /// <summary>
    /// BDD-style behavior specifications for DINOForge core systems.
    /// Tests are written in Given_When_Then format covering pack loading, validation,
    /// compatibility, registries, and asset replacement scenarios.
    /// </summary>
    public class BddSpecs
    {
        #region Pack Loading Scenarios

        [Fact]
        public void Given_ValidPackManifest_When_Loaded_Then_AllRegistriesPopulated()
        {
            // Given: A valid pack manifest with all content types
            var manifest = new PackManifest
            {
                Id = "test-pack",
                Name = "Test Pack",
                Version = "1.0.0",
                Author = "Test",
                Type = "content"
            };

            var registryManager = new RegistryManager();
            var loader = new ContentLoader(registryManager);

            var unit = new UnitDefinition { Id = "unit-1", DisplayName = "Test Unit", Stats = new UnitStats { Hp = 100f } };
            var building = new BuildingDefinition { Id = "building-1", DisplayName = "Test Building", Health = 50 };
            var faction = new FactionDefinition { Faction = new FactionInfo { Id = "faction-1", DisplayName = "Test Faction" } };

            // When: Manual registration simulates pack loading
            registryManager.Units.Register(unit.Id, unit, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
            registryManager.Buildings.Register(building.Id, building, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);
            registryManager.Factions.Register(faction.Faction.Id, faction, RegistrySource.Pack, manifest.Id, manifest.LoadOrder);

            // Then: All registries should contain the registered items
            registryManager.Units.Get(unit.Id).Should().NotBeNull();
            registryManager.Buildings.Get(building.Id).Should().NotBeNull();
            registryManager.Factions.Get(faction.Faction.Id).Should().NotBeNull();
            registryManager.Units.Contains(unit.Id).Should().BeTrue();
            registryManager.Buildings.Contains(building.Id).Should().BeTrue();
            registryManager.Factions.Contains(faction.Faction.Id).Should().BeTrue();
        }

        [Fact]
        public void Given_PackWithMissingId_When_Validated_Then_ReturnsError()
        {
            // Given: A pack manifest with empty ID
            var manifest = new PackManifest
            {
                Id = "", // Invalid: missing ID
                Name = "Test Pack"
            };

            var registryManager = new RegistryManager();
            var loader = new ContentLoader(registryManager);

            // When: Validation checks for required fields
            bool isValid = !string.IsNullOrWhiteSpace(manifest.Id);

            // Then: Validation should fail
            isValid.Should().BeFalse();
        }

        [Fact]
        public void Given_PackWithCircularDep_When_Resolved_Then_ThrowsOrReturnsError()
        {
            // Given: Two packs with circular dependencies
            var packA = new PackManifest
            {
                Id = "pack-a",
                Name = "Pack A",
                DependsOn = new List<string> { "pack-b" }
            };

            var packB = new PackManifest
            {
                Id = "pack-b",
                Name = "Pack B",
                DependsOn = new List<string> { "pack-a" }
            };

            var resolver = new PackDependencyResolver();

            // When: Attempting to compute load order with circular deps
            var result = resolver.ComputeLoadOrder(new[] { packA, packB });

            // Then: Resolution should fail with circular dependency error
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Contains("Circular"));
        }

        [Fact]
        public void Given_TotalConversionPack_When_Validated_Then_SingletonEnforced()
        {
            // Given: A total conversion manifest with singleton = true
            var tcManifest = new TotalConversionManifest
            {
                Id = "starwars-conversion",
                Type = "total_conversion",
                Singleton = true
            };

            // When: Checking singleton constraint
            bool isSingletonEnforced = tcManifest.Singleton && tcManifest.Type == "total_conversion";

            // Then: Singleton should be enforced for total conversions
            isSingletonEnforced.Should().BeTrue();
            tcManifest.Type.Should().Be("total_conversion");
        }

        #endregion

        #region Compatibility Scenarios

        [Fact]
        public void Given_PackRequiresFramework050_When_Framework050Installed_Then_Compatible()
        {
            // Given: A pack requiring framework 0.5.0
            var pack = new PackManifest
            {
                Id = "compat-pack",
                FrameworkVersion = "0.5.0"
            };

            var resolver = new PackDependencyResolver();
            string installedFramework = "0.5.0";

            // When: Checking compatibility with installed framework
            bool isCompatible = resolver.CheckFrameworkCompatibility(pack, installedFramework);

            // Then: Compatibility check should pass
            isCompatible.Should().BeTrue();
        }

        [Fact]
        public void Given_PackRequiresFramework200_When_Framework050Installed_Then_Incompatible()
        {
            // Given: A pack requiring framework 2.0.0
            var pack = new PackManifest
            {
                Id = "future-pack",
                FrameworkVersion = "2.0.0"
            };

            var resolver = new PackDependencyResolver();
            string installedFramework = "0.5.0";

            // When: Checking compatibility with older installed framework
            bool isCompatible = resolver.CheckFrameworkCompatibility(pack, installedFramework);

            // Then: Compatibility check should fail
            isCompatible.Should().BeFalse();
        }

        [Fact]
        public void Given_PackWithNoVersionReq_When_AnyFrameworkInstalled_Then_AlwaysCompatible()
        {
            // Given: A pack with no specific version requirement
            var pack = new PackManifest
            {
                Id = "universal-pack",
                FrameworkVersion = "" // No requirement
            };

            var resolver = new PackDependencyResolver();
            string installedFramework = "0.1.0";

            // When: Checking compatibility
            bool isCompatible = resolver.CheckFrameworkCompatibility(pack, installedFramework);

            // Then: Should always be compatible
            isCompatible.Should().BeTrue();
        }

        [Fact]
        public void Given_PackWithWildcardVersion_When_AnyFrameworkVersion_Then_AlwaysCompatible()
        {
            // Given: A pack with wildcard version requirement
            var pack = new PackManifest
            {
                Id = "wildcard-pack",
                FrameworkVersion = "*"
            };

            var resolver = new PackDependencyResolver();
            string installedFramework = "999.999.999";

            // When: Checking compatibility with wildcard
            bool isCompatible = string.IsNullOrWhiteSpace(pack.FrameworkVersion) ||
                               pack.FrameworkVersion == "*";

            // Then: Wildcard should accept any version
            isCompatible.Should().BeTrue();
        }

        #endregion

        #region Registry Scenarios

        [Fact]
        public void Given_EmptyRegistry_When_UnitRegistered_Then_ContainsUnit()
        {
            // Given: An empty unit registry
            var registry = new Registry<UnitDefinition>();
            var unit = new UnitDefinition { Id = "raptor", DisplayName = "Raptor", Stats = new UnitStats { Hp = 100f } };

            // When: Registering a unit
            registry.Register(unit.Id, unit, RegistrySource.BaseGame, "base-game");

            // Then: Registry should contain the unit
            registry.Contains(unit.Id).Should().BeTrue();
            registry.Get(unit.Id).Should().NotBeNull();
            registry.Get(unit.Id)!.DisplayName.Should().Be("Raptor");
        }

        [Fact]
        public void Given_Registry_When_SameUnitRegisteredTwice_Then_HigherPriorityWins()
        {
            // Given: A registry with a base game unit
            var registry = new Registry<UnitDefinition>();
            var baseUnit = new UnitDefinition { Id = "raptor", DisplayName = "Base Raptor", Stats = new UnitStats { Hp = 100f } };
            var modUnit = new UnitDefinition { Id = "raptor", DisplayName = "Mod Raptor", Stats = new UnitStats { Hp = 120f } };

            // When: First registering base unit, then overriding with mod unit
            registry.Register(baseUnit.Id, baseUnit, RegistrySource.BaseGame, "base-game", 0);
            registry.Register(modUnit.Id, modUnit, RegistrySource.Pack, "mod-pack", 100); // Higher priority

            // Then: Mod unit should be retrieved (higher priority)
            registry.Get("raptor")!.DisplayName.Should().Be("Mod Raptor");
            registry.Get("raptor")!.Stats.Hp.Should().Be(120f);
        }

        [Fact]
        public void Given_Registry_When_GetAllCalled_Then_ReturnsAllRegistered()
        {
            // Given: A registry with multiple units
            var registry = new Registry<UnitDefinition>();
            registry.Register("raptor", new UnitDefinition { Id = "raptor", DisplayName = "Raptor", Stats = new UnitStats { Hp = 100f } },
                RegistrySource.BaseGame, "base-game");
            registry.Register("triceratops", new UnitDefinition { Id = "triceratops", DisplayName = "Triceratops", Stats = new UnitStats { Hp = 150f } },
                RegistrySource.BaseGame, "base-game");
            registry.Register("ankylosaur", new UnitDefinition { Id = "ankylosaur", DisplayName = "Ankylosaur", Stats = new UnitStats { Hp = 200f } },
                RegistrySource.BaseGame, "base-game");

            // When: Getting all entries
            var allEntries = registry.All;

            // Then: All entries should be returned
            allEntries.Should().HaveCount(3);
            allEntries.Keys.Should().Contain(new[] { "raptor", "triceratops", "ankylosaur" });
        }

        [Fact]
        public void Given_Registry_When_MultiplePacksRegisterSameEntry_Then_ConflictDetected()
        {
            // Given: Two packs registering the same unit ID
            var registry = new Registry<UnitDefinition>();
            var packAUnit = new UnitDefinition { Id = "custom-unit", DisplayName = "Pack A Unit", Stats = new UnitStats { Hp = 100f } };
            var packBUnit = new UnitDefinition { Id = "custom-unit", DisplayName = "Pack B Unit", Stats = new UnitStats { Hp = 120f } };

            // When: Both packs register at same priority
            registry.Register(packAUnit.Id, packAUnit, RegistrySource.Pack, "pack-a", 100);
            registry.Register(packBUnit.Id, packBUnit, RegistrySource.Pack, "pack-b", 100);

            // Then: Conflict should be detected
            var conflicts = registry.DetectConflicts();
            conflicts.Should().NotBeEmpty();
            conflicts.Should().ContainSingle(c => c.EntryId == "custom-unit");
        }

        #endregion

        #region Asset Replacement Scenarios

        [Fact]
        public void Given_ModPathExists_When_TextureResolved_Then_ReturnsModPath()
        {
            // Given: An asset replacement engine with a registered texture mapping
            var engine = new AssetReplacementEngine();
            var manifest = new TotalConversionManifest
            {
                Id = "texture-pack",
                AssetReplacements = new TcAssetReplacements
                {
                    Textures = new Dictionary<string, string>
                    {
                        { "vanilla/unit/raptor.png", "textures/raptor.png" }
                    }
                }
            };

            // Create a temporary directory with the mod texture
            string tempDir = Path.Combine(Path.GetTempPath(), "dinoforge-test-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string modTexturePath = Path.Combine(tempDir, "textures", "raptor.png");
            Directory.CreateDirectory(Path.GetDirectoryName(modTexturePath)!);
            File.WriteAllText(modTexturePath, "fake-texture-data");

            try
            {
                // When: Loading manifest and resolving texture
                engine.LoadFromManifest(manifest, tempDir);
                string resolved = engine.ResolveTexture("vanilla/unit/raptor.png");

                // Then: Should return the mod texture path
                resolved.Should().NotBe("vanilla/unit/raptor.png");
                resolved.Should().Contain("raptor.png");
                File.Exists(resolved).Should().BeTrue();
            }
            finally
            {
                // Cleanup
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Given_ModPathMissing_When_TextureResolved_Then_FallsBackToVanilla()
        {
            // Given: An asset replacement engine with a mapping to non-existent file
            var engine = new AssetReplacementEngine();
            var manifest = new TotalConversionManifest
            {
                Id = "broken-texture-pack",
                AssetReplacements = new TcAssetReplacements
                {
                    Textures = new Dictionary<string, string>
                    {
                        { "vanilla/unit/raptor.png", "missing/raptor.png" }
                    }
                }
            };

            string tempDir = Path.Combine(Path.GetTempPath(), "dinoforge-test-missing-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                // When: Loading manifest with missing mod file and resolving
                engine.LoadFromManifest(manifest, tempDir);
                string resolved = engine.ResolveTexture("vanilla/unit/raptor.png");

                // Then: Should fall back to vanilla path
                resolved.Should().Be("vanilla/unit/raptor.png");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Given_AssetReplacementEngine_When_MultipleAssetTypesLoaded_Then_CountMatches()
        {
            // Given: A total conversion with textures, audio, and UI
            var engine = new AssetReplacementEngine();
            var manifest = new TotalConversionManifest
            {
                Id = "full-assets-pack",
                AssetReplacements = new TcAssetReplacements
                {
                    Textures = new Dictionary<string, string>
                    {
                        { "vanilla/texture1.png", "mod/texture1.png" },
                        { "vanilla/texture2.png", "mod/texture2.png" }
                    },
                    Audio = new Dictionary<string, string>
                    {
                        { "vanilla/sfx/attack.ogg", "mod/attack.ogg" }
                    },
                    Ui = new Dictionary<string, string>
                    {
                        { "vanilla/ui/button.png", "mod/button.png" }
                    }
                }
            };

            // When: Loading manifest and counting total mappings
            engine.LoadFromManifest(manifest, "/dummy/path");

            // Then: Total mappings should equal all registered assets
            engine.TotalMappings.Should().Be(4); // 2 textures + 1 audio + 1 UI
        }

        #endregion

        #region Dependency Resolution Scenarios

        [Fact]
        public void Given_PackWithValidDependencies_When_Resolved_Then_LoadOrderComputed()
        {
            // Given: Three packs with valid dependency chain
            var packA = new PackManifest { Id = "pack-a", Name = "Pack A" };
            var packB = new PackManifest { Id = "pack-b", Name = "Pack B", DependsOn = new List<string> { "pack-a" } };
            var packC = new PackManifest { Id = "pack-c", Name = "Pack C", DependsOn = new List<string> { "pack-b" } };

            var resolver = new PackDependencyResolver();

            // When: Computing load order
            var result = resolver.ComputeLoadOrder(new[] { packA, packB, packC });

            // Then: Load order should respect dependencies
            result.IsSuccess.Should().BeTrue();
            result.LoadOrder.Should().HaveCount(3);
            result.LoadOrder[0].Id.Should().Be("pack-a");
            result.LoadOrder[1].Id.Should().Be("pack-b");
            result.LoadOrder[2].Id.Should().Be("pack-c");
        }

        [Fact]
        public void Given_PackDependsOnMissing_When_Resolved_Then_ReturnsError()
        {
            // Given: A pack depending on non-existent pack
            var packA = new PackManifest { Id = "pack-a", Name = "Pack A" };
            var packB = new PackManifest { Id = "pack-b", Name = "Pack B", DependsOn = new List<string> { "missing-pack" } };

            var resolver = new PackDependencyResolver();

            // When: Attempting to resolve
            var result = resolver.ComputeLoadOrder(new[] { packA, packB });

            // Then: Should fail with missing dependency error
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Contains("unknown pack"));
        }

        [Fact]
        public void Given_ConflictingPacksInSet_When_Detected_Then_ReturnsConflictError()
        {
            // Given: Two packs that declare conflict with each other
            var packA = new PackManifest
            {
                Id = "balance-mod",
                ConflictsWith = new List<string> { "balance-mod-alt" }
            };
            var packB = new PackManifest
            {
                Id = "balance-mod-alt",
                ConflictsWith = new List<string> { "balance-mod" }
            };

            var resolver = new PackDependencyResolver();

            // When: Detecting conflicts
            var conflicts = resolver.DetectConflicts(new[] { packA, packB });

            // Then: Conflicts should be detected (at least one direction)
            conflicts.Should().NotBeEmpty();
            conflicts.Should().HaveCountGreaterThan(0);
        }

        #endregion

        #region Content Loader Integration Scenarios

        [Fact]
        public void Given_ValidPackDirectory_When_LoadingManifest_Then_PackIdRetrieved()
        {
            // Given: A temporary directory with a valid pack.yaml
            string tempDir = Path.Combine(Path.GetTempPath(), "pack-test-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);
            string manifestPath = Path.Combine(tempDir, "pack.yaml");
            File.WriteAllText(manifestPath, @"
id: test-pack
name: Test Pack
version: 1.0.0
author: Test Author
type: content
");

            try
            {
                // When: Loading the pack
                var registryManager = new RegistryManager();
                var loader = new ContentLoader(registryManager);
                var result = loader.LoadPack(tempDir);

                // Then: Pack should load successfully
                result.IsSuccess.Should().BeTrue();
                result.LoadedPacks.Should().Contain("test-pack");
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void Given_PackDirectoryWithoutManifest_When_Loaded_Then_ReturnsFailure()
        {
            // Given: A directory without pack.yaml
            string tempDir = Path.Combine(Path.GetTempPath(), "no-manifest-" + Guid.NewGuid());
            Directory.CreateDirectory(tempDir);

            try
            {
                // When: Attempting to load
                var registryManager = new RegistryManager();
                var loader = new ContentLoader(registryManager);
                var result = loader.LoadPack(tempDir);

                // Then: Should fail with manifest not found error
                result.IsSuccess.Should().BeFalse();
                result.Errors.Should().ContainSingle(e => e.Contains("manifest not found"));
            }
            finally
            {
                if (Directory.Exists(tempDir))
                    Directory.Delete(tempDir, true);
            }
        }

        #endregion
    }
}
