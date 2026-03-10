using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DINOForge.SDK.Universe;
using FluentAssertions;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DINOForge.Tests
{
    /// <summary>
    /// Tests for the Universe Bible system: CrosswalkDictionary, PackGenerator,
    /// UniverseLoader, NamingGuide, and related models.
    /// </summary>
    public class UniverseBibleTests
    {
        private static readonly IDeserializer Deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .IgnoreUnmatchedProperties()
            .Build();

        // ──────────────────────── CrosswalkDictionary ────────────────────────

        [Fact]
        public void CrosswalkDictionary_ExactLookup_ReturnsEntry()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            CrosswalkEntry? entry = dict.LookupByVanillaId("militia_spearman");

            entry.Should().NotBeNull();
            entry!.ThemedId.Should().Be("clone_trooper_recruit");
            entry.ThemedName.Should().Be("Clone Trooper Recruit");
        }

        [Fact]
        public void CrosswalkDictionary_ExactLookup_CaseInsensitive()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            CrosswalkEntry? entry = dict.LookupByVanillaId("MILITIA_SPEARMAN");

            entry.Should().NotBeNull();
            entry!.ThemedId.Should().Be("clone_trooper_recruit");
        }

        [Fact]
        public void CrosswalkDictionary_ExactLookup_MissingEntry_ReturnsNull()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            CrosswalkEntry? entry = dict.LookupByVanillaId("nonexistent_unit");

            entry.Should().BeNull();
        }

        [Fact]
        public void CrosswalkDictionary_WildcardPattern_MatchesAndApplies()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            CrosswalkEntry? entry = dict.LookupByVanillaId("enemy_heavy_tank");

            entry.Should().NotBeNull();
            entry!.ThemedId.Should().Be("cis_heavy_tank");
            entry.VanillaId.Should().Be("enemy_heavy_tank");
        }

        [Fact]
        public void CrosswalkDictionary_WildcardPattern_NoMatch_ReturnsNull()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            CrosswalkEntry? entry = dict.LookupByVanillaId("friendly_unit");

            entry.Should().BeNull();
        }

        [Fact]
        public void CrosswalkDictionary_ReverseLookup_FindsVanillaId()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            string? vanillaId = dict.LookupByThemedId("clone_trooper_recruit");

            vanillaId.Should().Be("militia_spearman");
        }

        [Fact]
        public void CrosswalkDictionary_ReverseLookup_CaseInsensitive()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            string? vanillaId = dict.LookupByThemedId("CLONE_TROOPER_RECRUIT");

            vanillaId.Should().Be("militia_spearman");
        }

        [Fact]
        public void CrosswalkDictionary_ReverseLookup_WildcardPattern()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            string? vanillaId = dict.LookupByThemedId("cis_scout");

            vanillaId.Should().Be("enemy_scout");
        }

        [Fact]
        public void CrosswalkDictionary_GetAllEntries_ReturnsAll()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            IReadOnlyList<CrosswalkEntry> entries = dict.GetAllEntries();

            entries.Should().HaveCount(2);
        }

        [Fact]
        public void CrosswalkDictionary_StatModifiers_Preserved()
        {
            CrosswalkDictionary dict = CreateTestCrosswalk();

            CrosswalkEntry? entry = dict.LookupByVanillaId("militia_spearman");

            entry!.StatModifiers.Should().NotBeNull();
            entry.StatModifiers!["accuracy"].Should().Be(1.1f);
        }

        // ──────────────────────── NamingGuide ────────────────────────

        [Fact]
        public void NamingGuide_ApplyPrefix_Works()
        {
            NamingGuide guide = CreateTestNamingGuide();

            string result = guide.ApplyNaming("republic", "unit", "Soldier");

            result.Should().Be("Clone Soldier");
        }

        [Fact]
        public void NamingGuide_ApplyOverride_TakesPrecedence()
        {
            NamingGuide guide = CreateTestNamingGuide();

            string result = guide.ApplyNaming("republic", "weapon", "rifle");

            result.Should().Be("DC-15A Blaster Rifle");
        }

        [Fact]
        public void NamingGuide_NoMatchingFaction_FallsBackToGlobal()
        {
            NamingGuide guide = CreateTestNamingGuide();

            string result = guide.ApplyNaming("unknown_faction", "unit", "Soldier");

            // Global rules have no prefix/suffix, so base name returned
            result.Should().Be("Soldier");
        }

        [Fact]
        public void NamingGuide_PatternWithPlaceholder_Works()
        {
            NamingGuide guide = CreateTestNamingGuide();

            string result = guide.ApplyNaming("republic", "vehicle", "TE");

            result.Should().Be("AT-TE");
        }

        // ──────────────────────── UniverseLoader ────────────────────────

        [Fact]
        public void UniverseLoader_LoadFromYaml_ParsesCorrectly()
        {
            string yaml = @"
id: test-universe
name: Test Universe
description: A test universe
era: Test Era
version: '0.1.0'
";
            UniverseLoader loader = new UniverseLoader();

            UniverseBible bible = loader.LoadFromYaml(yaml);

            bible.Id.Should().Be("test-universe");
            bible.Name.Should().Be("Test Universe");
            bible.Era.Should().Be("Test Era");
            bible.Version.Should().Be("0.1.0");
        }

        [Fact]
        public void UniverseLoader_LoadFromDirectory_LoadsSeparateFiles()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"universe_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                // Write universe.yaml
                File.WriteAllText(Path.Combine(tempDir, "universe.yaml"),
                    "id: test-load\nname: Test Load\nversion: '0.1.0'\n");

                // Write crosswalk.yaml
                File.WriteAllText(Path.Combine(tempDir, "crosswalk.yaml"),
                    "entries:\n  unit_a:\n    vanilla_id: unit_a\n    themed_id: themed_a\n    themed_name: Themed A\n");

                // Write factions.yaml
                File.WriteAllText(Path.Combine(tempDir, "factions.yaml"),
                    "factions:\n  - id: faction_a\n    name: Faction A\n    alignment: Player\n    archetype: order\n");

                UniverseLoader loader = new UniverseLoader();
                UniverseBible bible = loader.LoadFromDirectory(tempDir);

                bible.Id.Should().Be("test-load");
                bible.CrosswalkDictionary.Entries.Should().HaveCount(1);
                bible.CrosswalkDictionary.Entries["unit_a"].ThemedId.Should().Be("themed_a");
                bible.FactionTaxonomy.Factions.Should().HaveCount(1);
                bible.FactionTaxonomy.Factions[0].Id.Should().Be("faction_a");
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        [Fact]
        public void UniverseLoader_MissingUniverseYaml_Throws()
        {
            string tempDir = Path.Combine(Path.GetTempPath(), $"universe_test_{Guid.NewGuid():N}");
            Directory.CreateDirectory(tempDir);
            try
            {
                UniverseLoader loader = new UniverseLoader();

                Action act = () => loader.LoadFromDirectory(tempDir);

                act.Should().Throw<FileNotFoundException>();
            }
            finally
            {
                Directory.Delete(tempDir, true);
            }
        }

        // ──────────────────────── PackGenerator ────────────────────────

        [Fact]
        public void PackGenerator_GeneratesManifestAndFiles()
        {
            string outputDir = Path.Combine(Path.GetTempPath(), $"packgen_test_{Guid.NewGuid():N}");
            try
            {
                UniverseBible bible = CreateTestBible();
                PackGenerator generator = new PackGenerator();

                PackGeneratorResult result = generator.Generate(bible, null, outputDir);

                result.GeneratedFiles.Should().HaveCountGreaterThan(0);
                File.Exists(Path.Combine(outputDir, "pack.yaml")).Should().BeTrue();
                Directory.Exists(Path.Combine(outputDir, "factions")).Should().BeTrue();
                Directory.Exists(Path.Combine(outputDir, "units")).Should().BeTrue();
            }
            finally
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void PackGenerator_ManifestContainsCorrectId()
        {
            string outputDir = Path.Combine(Path.GetTempPath(), $"packgen_test_{Guid.NewGuid():N}");
            try
            {
                UniverseBible bible = CreateTestBible();
                PackGenerator generator = new PackGenerator();

                generator.Generate(bible, null, outputDir);

                string manifestContent = File.ReadAllText(Path.Combine(outputDir, "pack.yaml"));
                manifestContent.Should().Contain("test-bible");
            }
            finally
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        [Fact]
        public void PackGenerator_FiltersByFaction()
        {
            string outputDir = Path.Combine(Path.GetTempPath(), $"packgen_test_{Guid.NewGuid():N}");
            try
            {
                UniverseBible bible = CreateTestBible();
                // Add a second faction
                bible.FactionTaxonomy.Factions.Add(new TaxonomyFaction
                {
                    Id = "faction_b",
                    Name = "Faction B",
                    Alignment = "Enemy",
                    Archetype = "industrial_swarm"
                });
                PackGenerator generator = new PackGenerator();

                PackGeneratorResult result = generator.Generate(
                    bible,
                    new List<string> { "test_faction" },
                    outputDir);

                // Should only have one faction file
                string[] factionFiles = Directory.GetFiles(Path.Combine(outputDir, "factions"), "*.yaml");
                factionFiles.Should().HaveCount(1);
                factionFiles[0].Should().Contain("test_faction");
            }
            finally
            {
                if (Directory.Exists(outputDir))
                    Directory.Delete(outputDir, true);
            }
        }

        // ──────────────────────── Pattern matching internals ────────────────────────

        [Theory]
        [InlineData("enemy_scout", "enemy_*", true)]
        [InlineData("enemy_heavy_tank", "enemy_*", true)]
        [InlineData("friendly_unit", "enemy_*", false)]
        [InlineData("building_barracks", "building_*", true)]
        [InlineData("unit_archer", "unit_*", true)]
        [InlineData("", "enemy_*", false)]
        public void MatchesPattern_VariousInputs_CorrectResult(string value, string pattern, bool expected)
        {
            bool result = CrosswalkDictionary.MatchesPattern(value, pattern);

            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractWildcardValue_ExtractsCorrectPart()
        {
            string result = CrosswalkDictionary.ExtractWildcardValue("enemy_heavy_tank", "enemy_*");

            result.Should().Be("heavy_tank");
        }

        // ──────────────────────── Helper factories ────────────────────────

        private static CrosswalkDictionary CreateTestCrosswalk()
        {
            return new CrosswalkDictionary
            {
                Entries = new Dictionary<string, CrosswalkEntry>(StringComparer.OrdinalIgnoreCase)
                {
                    ["militia_spearman"] = new CrosswalkEntry
                    {
                        VanillaId = "militia_spearman",
                        ThemedId = "clone_trooper_recruit",
                        ThemedName = "Clone Trooper Recruit",
                        StatModifiers = new Dictionary<string, float> { ["accuracy"] = 1.1f }
                    },
                    ["line_swordsman"] = new CrosswalkEntry
                    {
                        VanillaId = "line_swordsman",
                        ThemedId = "clone_trooper",
                        ThemedName = "Clone Trooper"
                    }
                },
                Patterns = new List<CrosswalkPattern>
                {
                    new CrosswalkPattern
                    {
                        VanillaPattern = "enemy_*",
                        ThemedPattern = "cis_*",
                        ThemedNamePattern = "CIS *"
                    }
                }
            };
        }

        private static NamingGuide CreateTestNamingGuide()
        {
            return new NamingGuide
            {
                FactionRules = new Dictionary<string, FactionNamingRules>(StringComparer.OrdinalIgnoreCase)
                {
                    ["republic"] = new FactionNamingRules
                    {
                        Rules = new Dictionary<string, NamingRuleSet>(StringComparer.OrdinalIgnoreCase)
                        {
                            ["unit"] = new NamingRuleSet
                            {
                                Prefix = "Clone "
                            },
                            ["weapon"] = new NamingRuleSet
                            {
                                Overrides = new Dictionary<string, string>
                                {
                                    ["rifle"] = "DC-15A Blaster Rifle"
                                }
                            },
                            ["vehicle"] = new NamingRuleSet
                            {
                                Pattern = "AT-{name}"
                            }
                        }
                    }
                },
                GlobalRules = new NamingRuleSet()
            };
        }

        private static UniverseBible CreateTestBible()
        {
            return new UniverseBible
            {
                Id = "test-bible",
                Name = "Test Bible",
                Version = "0.1.0",
                FactionTaxonomy = new FactionTaxonomy
                {
                    Factions = new List<TaxonomyFaction>
                    {
                        new TaxonomyFaction
                        {
                            Id = "test_faction",
                            Name = "Test Faction",
                            Alignment = "Player",
                            Archetype = "order"
                        }
                    }
                },
                CrosswalkDictionary = new CrosswalkDictionary
                {
                    Entries = new Dictionary<string, CrosswalkEntry>(StringComparer.OrdinalIgnoreCase)
                    {
                        ["vanilla_unit"] = new CrosswalkEntry
                        {
                            VanillaId = "vanilla_unit",
                            ThemedId = "themed_unit",
                            ThemedName = "Themed Unit"
                        }
                    }
                },
                NamingGuide = new NamingGuide(),
                StyleGuide = new StyleGuide()
            };
        }
    }
}
