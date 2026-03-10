using System;
using DINOForge.SDK;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class PackLoaderTests
    {
        private readonly PackLoader _loader = new();

        [Fact]
        public void LoadFromString_ValidManifest_DeserializesCorrectly()
        {
            string yaml = @"
id: test-pack
name: Test Pack
version: 0.1.0
framework_version: '>=0.1.0'
author: Test Author
type: content
depends_on:
  - core
conflicts_with: []
loads:
  factions:
    - republic
    - cis
";
            PackManifest manifest = _loader.LoadFromString(yaml);

            manifest.Id.Should().Be("test-pack");
            manifest.Name.Should().Be("Test Pack");
            manifest.Version.Should().Be("0.1.0");
            manifest.Author.Should().Be("Test Author");
            manifest.Type.Should().Be("content");
            manifest.DependsOn.Should().Contain("core");
            manifest.Loads.Should().NotBeNull();
            manifest.Loads!.Factions.Should().Contain("republic");
            manifest.Loads!.Factions.Should().Contain("cis");
        }

        [Fact]
        public void LoadFromString_MissingId_Throws()
        {
            string yaml = @"
name: No ID Pack
version: 0.1.0
author: Test
type: content
";
            Action act = () => _loader.LoadFromString(yaml);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*id*");
        }

        [Fact]
        public void LoadFromString_MissingName_Throws()
        {
            string yaml = @"
id: no-name
version: 0.1.0
author: Test
type: content
";
            Action act = () => _loader.LoadFromString(yaml);
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("*name*");
        }

        [Fact]
        public void LoadFromString_MinimalManifest_UsesDefaults()
        {
            string yaml = @"
id: minimal
name: Minimal Pack
version: 0.1.0
author: Test
type: balance
";
            PackManifest manifest = _loader.LoadFromString(yaml);

            manifest.LoadOrder.Should().Be(100);
            manifest.DependsOn.Should().BeEmpty();
            manifest.ConflictsWith.Should().BeEmpty();
            manifest.Loads.Should().BeNull();
        }

        [Fact]
        public void LoadFromString_TotalConversion_ParsesFullManifest()
        {
            string yaml = @"
id: starwars-republic-cis
name: Republic vs CIS Pack
version: 0.1.0
framework_version: '>=0.4.0 <0.5.0'
author: DINOForge Agents
type: total_conversion
depends_on:
  - dino-core
  - dino-warfare-domain
conflicts_with:
  - modern-west-pack
load_order: 50
loads:
  factions:
    - republic
    - cis
  doctrines:
    - elite_discipline
    - mechanized_attrition
  audio:
    - sw_blaster_pack
  visuals:
    - sw_projectiles
  localization:
    - sw_english
";
            PackManifest manifest = _loader.LoadFromString(yaml);

            manifest.Type.Should().Be("total_conversion");
            manifest.LoadOrder.Should().Be(50);
            manifest.DependsOn.Should().HaveCount(2);
            manifest.ConflictsWith.Should().Contain("modern-west-pack");
            manifest.Loads!.Doctrines.Should().Contain("mechanized_attrition");
            manifest.Loads!.Audio.Should().Contain("sw_blaster_pack");
        }

        [Fact]
        public void LoadFromFile_NonexistentFile_Throws()
        {
            Action act = () => _loader.LoadFromFile("/nonexistent/path/pack.yaml");
            act.Should().Throw<System.IO.FileNotFoundException>();
        }
    }
}
