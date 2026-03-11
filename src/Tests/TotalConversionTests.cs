#nullable enable
using System.Collections.Generic;
using System.IO;
using DINOForge.SDK;
using DINOForge.SDK.Models;
using DINOForge.SDK.Validation;
using FluentAssertions;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace DINOForge.Tests
{
    public class TotalConversionValidatorTests
    {
        private static TotalConversionManifest ValidManifest() => new TotalConversionManifest
        {
            Id = "warfare-starwars",
            Name = "Star Wars: Clone Wars",
            Version = "0.1.0",
            Type = "total_conversion",
            Author = "DINOForge Community",
            Singleton = true,
            ReplacesVanilla = new Dictionary<string, string>
            {
                { "player", "republic" },
                { "enemy_classic", "cis-droid-army" },
                { "enemy_guerrilla", "cis-infiltrators" }
            },
            Factions = new List<TcFactionEntry>
            {
                new TcFactionEntry { Id = "republic",       Name = "Galactic Republic",                ReplacesVanilla = "player" },
                new TcFactionEntry { Id = "cis-droid-army", Name = "Confederacy of Independent Systems", ReplacesVanilla = "enemy_classic" },
                new TcFactionEntry { Id = "cis-infiltrators", Name = "CIS Infiltrators",               ReplacesVanilla = "enemy_guerrilla" }
            }
        };

        [Fact]
        public void Validator_RejectsNullId()
        {
            TotalConversionManifest manifest = ValidManifest();
            manifest.Id = "";

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Contains("id is required"));
        }

        [Fact]
        public void Validator_RejectsWrongType()
        {
            TotalConversionManifest manifest = ValidManifest();
            manifest.Type = "content";

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Contains("total_conversion") && e.Contains("content"));
        }

        [Fact]
        public void Validator_WarnsSingletonFalse()
        {
            TotalConversionManifest manifest = ValidManifest();
            manifest.Singleton = false;

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeTrue("singleton=false is a warning, not an error");
            result.Warnings.Should().ContainSingle(w => w.Contains("singleton=false"));
        }

        [Fact]
        public void Validator_WarnsUnknownVanillaFaction()
        {
            TotalConversionManifest manifest = ValidManifest();
            manifest.ReplacesVanilla["unknown_faction"] = "some-mod-faction";

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeTrue("unknown vanilla faction is a warning, not an error");
            result.Warnings.Should().ContainSingle(w => w.Contains("Unknown vanilla faction") && w.Contains("unknown_faction"));
        }

        [Fact]
        public void Validator_WarnsNoFactions()
        {
            TotalConversionManifest manifest = ValidManifest();
            manifest.Factions = new List<TcFactionEntry>();

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeTrue("no factions is a warning, not an error");
            result.Warnings.Should().ContainSingle(w => w.Contains("no factions"));
        }

        [Fact]
        public void Validator_RejectsDuplicateFactionIds()
        {
            TotalConversionManifest manifest = ValidManifest();
            manifest.Factions.Add(new TcFactionEntry { Id = "republic", Name = "Duplicate Republic" });

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle(e => e.Contains("Duplicate faction id") && e.Contains("republic"));
        }

        [Fact]
        public void Validator_PassesValidManifest()
        {
            TotalConversionManifest manifest = ValidManifest();

            TcValidationResult result = TotalConversionValidator.Validate(manifest);

            result.IsValid.Should().BeTrue();
            result.Errors.Should().BeEmpty();
            result.Warnings.Should().BeEmpty();
        }
    }

    public class AssetReplacementEngineTests
    {
        [Fact]
        public void AssetEngine_ReturnsVanillaPathWhenModFileMissing()
        {
            TotalConversionManifest manifest = new TotalConversionManifest
            {
                Id = "test-tc",
                Name = "Test",
                Version = "0.1.0",
                Type = "total_conversion",
                Author = "Test",
                AssetReplacements = new TcAssetReplacements
                {
                    Textures = new Dictionary<string, string>
                    {
                        { "vanilla/ui/icon.png", "mods/test-tc/ui/icon.png" }
                    }
                }
            };

            // Use a non-existent base path so the mod file will not be found
            AssetReplacementEngine engine = new AssetReplacementEngine(manifest, "/nonexistent/pack/base");

            string resolved = engine.ResolveTexture("vanilla/ui/icon.png");

            resolved.Should().Be("vanilla/ui/icon.png", "mod file does not exist so vanilla path is returned");
        }

        [Fact]
        public void AssetEngine_CountsCorrectly()
        {
            TotalConversionManifest manifest = new TotalConversionManifest
            {
                Id = "count-tc",
                Name = "Count Test",
                Version = "0.1.0",
                Type = "total_conversion",
                Author = "Test",
                AssetReplacements = new TcAssetReplacements
                {
                    Textures = new Dictionary<string, string>
                    {
                        { "vanilla/tex/a.png", "mods/a.png" },
                        { "vanilla/tex/b.png", "mods/b.png" }
                    },
                    Audio = new Dictionary<string, string>
                    {
                        { "vanilla/sfx/bang.wav", "mods/bang.wav" }
                    },
                    Ui = new Dictionary<string, string>()
                }
            };

            AssetReplacementEngine engine = new AssetReplacementEngine(manifest, "/base");

            engine.TextureCount.Should().Be(2);
            engine.AudioCount.Should().Be(1);
            engine.UiCount.Should().Be(0);
        }
    }

    public class TotalConversionManifestSerializationTests
    {
        [Fact]
        public void Manifest_Serializes_ToYaml()
        {
            TotalConversionManifest manifest = new TotalConversionManifest
            {
                Id = "warfare-starwars",
                Name = "Star Wars: Clone Wars",
                Version = "0.1.0",
                Type = "total_conversion",
                Author = "DINOForge Community",
                Theme = "sci-fi",
                Singleton = true,
                Factions = new List<TcFactionEntry>
                {
                    new TcFactionEntry { Id = "republic", Name = "Galactic Republic" }
                }
            };

            ISerializer serializer = new SerializerBuilder()
                .WithNamingConvention(UnderscoredNamingConvention.Instance)
                .Build();

            string yaml = serializer.Serialize(manifest);

            yaml.Should().NotBeNullOrWhiteSpace();
            yaml.Should().Contain("warfare-starwars");
            yaml.Should().Contain("total_conversion");
            yaml.Should().Contain("republic");
        }
    }
}
