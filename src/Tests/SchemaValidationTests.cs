using System.Collections.Generic;
using System.IO;
using DINOForge.SDK.Validation;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class SchemaValidationTests
    {
        private readonly NJsonSchemaValidator _validator;

        public SchemaValidationTests()
        {
            // Load JSON schemas from the schemas/ directory.
            // NJsonSchemaValidator accepts schema content keyed by name.
            // It converts YAML->JSON internally, but our schemas are already JSON,
            // so we pass them as-is (JSON is valid YAML).
            string schemasDir = FindSchemasDirectory();
            var schemas = new Dictionary<string, string>();

            foreach (string file in Directory.GetFiles(schemasDir, "*.schema.json"))
            {
                string name = Path.GetFileNameWithoutExtension(file)
                    .Replace(".schema", "");
                schemas[name] = File.ReadAllText(file);
            }

            _validator = new NJsonSchemaValidator(schemas);
        }

        [Fact]
        public void ValidManifest_PassesValidation()
        {
            string yaml = @"
id: test-pack
name: Test Pack
version: 0.1.0
author: Test Author
type: content
depends_on: []
conflicts_with: []
";

            ValidationResult result = _validator.Validate("pack-manifest", yaml);

            result.IsValid.Should().BeTrue(
                because: "a valid manifest should pass schema validation. Errors: {0}",
                string.Join("; ", result.Errors));
        }

        [Fact]
        public void InvalidManifest_MissingRequiredFields_FailsValidation()
        {
            // Missing required fields: id, name, version, author, type
            string yaml = @"
description: An incomplete manifest
";

            ValidationResult result = _validator.Validate("pack-manifest", yaml);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void ValidUnit_PassesValidation()
        {
            string yaml = @"
id: test-soldier
display_name: Test Soldier
unit_class: CoreLineInfantry
faction_id: test-faction
tier: 2
stats:
  hp: 100
  damage: 15
  armor: 2
  range: 0
  speed: 3.0
  accuracy: 0.7
  fire_rate: 1.0
  morale: 80
  cost:
    food: 30
    wood: 10
    iron: 5
defense_tags:
  - InfantryArmor
  - Biological
behavior_tags:
  - HoldLine
";

            ValidationResult result = _validator.Validate("unit", yaml);

            result.IsValid.Should().BeTrue(
                because: "a valid unit should pass schema validation. Errors: {0}",
                string.Join("; ", result.Errors));
        }

        [Fact]
        public void InvalidUnit_MissingId_FailsValidation()
        {
            string yaml = @"
display_name: No ID Unit
unit_class: CoreLineInfantry
faction_id: test-faction
";

            ValidationResult result = _validator.Validate("unit", yaml);

            result.IsValid.Should().BeFalse();
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public void ValidBuilding_PassesValidation()
        {
            string yaml = @"
id: test-barracks
display_name: Test Barracks
building_type: barracks
cost:
  wood: 50
  stone: 30
health: 500
";

            ValidationResult result = _validator.Validate("building", yaml);

            result.IsValid.Should().BeTrue(
                because: "a valid building should pass schema validation. Errors: {0}",
                string.Join("; ", result.Errors));
        }

        [Fact]
        public void ValidFaction_PassesValidation()
        {
            string yaml = @"
faction:
  id: test-faction
  display_name: Test Faction
  theme: fantasy
  archetype: order
economy:
  gather_bonus: 1.0
army:
  morale_style: disciplined
";

            ValidationResult result = _validator.Validate("faction", yaml);

            result.IsValid.Should().BeTrue(
                because: "a valid faction should pass schema validation. Errors: {0}",
                string.Join("; ", result.Errors));
        }

        /// <summary>
        /// Walks up from the test assembly output directory to find the schemas/ directory.
        /// </summary>
        private static string FindSchemasDirectory()
        {
            // Start from the current directory and walk up to find schemas/
            string? dir = Directory.GetCurrentDirectory();
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "schemas");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.schema.json").Length > 0)
                    return candidate;
                dir = Directory.GetParent(dir)?.FullName;
            }

            // Fallback: try relative to the assembly location
            string assemblyDir = Path.GetDirectoryName(typeof(SchemaValidationTests).Assembly.Location) ?? "";
            dir = assemblyDir;
            while (dir != null)
            {
                string candidate = Path.Combine(dir, "schemas");
                if (Directory.Exists(candidate) && Directory.GetFiles(candidate, "*.schema.json").Length > 0)
                    return candidate;
                dir = Directory.GetParent(dir)?.FullName;
            }

            throw new DirectoryNotFoundException(
                "Could not find schemas/ directory. Searched up from: " + Directory.GetCurrentDirectory());
        }
    }
}
