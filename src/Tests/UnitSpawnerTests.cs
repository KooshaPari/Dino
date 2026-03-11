using Xunit;
using DINOForge.Runtime.Bridge;

namespace DINOForge.Tests
{
    /// <summary>
    /// Unit tests for the M9 unit spawner architecture.
    /// Tests focus on mapper logic and data model validation.
    /// Full ECS integration tests will be added during M9 implementation.
    /// </summary>
    public class VanillaArchetypeMapperTests
    {
        [Theory]
        [InlineData("MilitiaLight", "Components.MeleeUnit")]
        [InlineData("CoreLineInfantry", "Components.MeleeUnit")]
        [InlineData("EliteLineInfantry", "Components.MeleeUnit")]
        [InlineData("HeavyInfantry", "Components.MeleeUnit")]
        [InlineData("ShockMelee", "Components.MeleeUnit")]
        [InlineData("SwarmFodder", "Components.MeleeUnit")]
        [InlineData("Skirmisher", "Components.RangeUnit")]
        [InlineData("AntiArmor", "Components.RangeUnit")]
        [InlineData("FastVehicle", "Components.CavalryUnit")]
        [InlineData("MainBattleVehicle", "Components.SiegeUnit")]
        [InlineData("HeavySiege", "Components.SiegeUnit")]
        [InlineData("Artillery", "Components.SiegeUnit")]
        [InlineData("WalkerHeavy", "Components.SiegeUnit")]
        [InlineData("Archer", "Components.Archer")]
        [InlineData("StaticMG", "Components.MeleeUnit")]
        [InlineData("StaticAT", "Components.MeleeUnit")]
        [InlineData("StaticArtillery", "Components.SiegeUnit")]
        [InlineData("SupportEngineer", "Components.MeleeUnit")]
        [InlineData("Recon", "Components.RangeUnit")]
        [InlineData("HeroCommander", "Components.MeleeUnit")]
        [InlineData("AirstrikeProxy", "Components.MeleeUnit")]
        [InlineData("ShieldedElite", "Components.MeleeUnit")]
        public void MapUnitClassToComponentType_ValidClasses_ReturnCorrectComponentType(
            string unitClass, string expectedComponentType)
        {
            var result = VanillaArchetypeMapper.MapUnitClassToComponentType(unitClass);

            Assert.Equal(expectedComponentType, result);
        }

        [Theory]
        [InlineData("MilitiaLight")]
        [InlineData("CoreLineInfantry")]
        [InlineData("Skirmisher")]
        [InlineData("Artillery")]
        [InlineData("Archer")]
        [InlineData("HeroCommander")]
        public void IsSpawnable_ValidClasses_ReturnsTrue(string unitClass)
        {
            var result = VanillaArchetypeMapper.IsSpawnable(unitClass);

            Assert.True(result);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("InvalidClass")]
        [InlineData("MagicMissile")]
        [InlineData("RandomUnit")]
        public void IsSpawnable_InvalidClasses_ReturnsFalse(string unitClass)
        {
            var result = VanillaArchetypeMapper.IsSpawnable(unitClass);

            Assert.False(result);
        }

        [Fact]
        public void IsSpawnable_NullClass_ReturnsFalse()
        {
            var result = VanillaArchetypeMapper.IsSpawnable(null!);

            Assert.False(result);
        }

        [Theory]
        [InlineData("MilitiaLight")]
        [InlineData("CoreLineInfantry")]
        [InlineData("Artillery")]
        [InlineData("Archer")]
        public void ValidateUnitClass_ValidClasses_ReturnsTrue(string unitClass)
        {
            var result = VanillaArchetypeMapper.ValidateUnitClass(unitClass, out string? error);

            Assert.True(result);
            Assert.Null(error);
        }

        [Theory]
        [InlineData("", "Unit class is empty or null")]
        [InlineData("   ", "Unit class is empty or null")]
        public void ValidateUnitClass_EmptyOrWhitespace_ReturnsFalseWithMessage(string unitClass, string expectedErrorStart)
        {
            var result = VanillaArchetypeMapper.ValidateUnitClass(unitClass, out string? error);

            Assert.False(result);
            Assert.NotNull(error);
            Assert.Contains(expectedErrorStart, error);
        }

        [Fact]
        public void ValidateUnitClass_Null_ReturnsFalseWithMessage()
        {
            var result = VanillaArchetypeMapper.ValidateUnitClass(null!, out string? error);

            Assert.False(result);
            Assert.NotNull(error);
            Assert.Contains("Unit class is empty or null", error);
        }

        [Fact]
        public void ValidateUnitClass_UnknownClass_ReturnsFalseWithInformativeMessage()
        {
            var result = VanillaArchetypeMapper.ValidateUnitClass("InvalidClass", out string? error);

            Assert.False(result);
            Assert.NotNull(error);
            Assert.Contains("Unknown unit class 'InvalidClass'", error);
            Assert.Contains("Valid classes:", error);
        }

        [Fact]
        public void MapUnitClassToComponentType_CaseInsensitive_MapsCorrectly()
        {
            // The mapping should use case-insensitive comparison
            var result1 = VanillaArchetypeMapper.MapUnitClassToComponentType("MILITIALIGHT");
            var result2 = VanillaArchetypeMapper.MapUnitClassToComponentType("militialight");
            var result3 = VanillaArchetypeMapper.MapUnitClassToComponentType("MilitiaLight");

            Assert.Equal("Components.MeleeUnit", result1);
            Assert.Equal("Components.MeleeUnit", result2);
            Assert.Equal("Components.MeleeUnit", result3);
        }

        [Fact]
        public void MapUnitClassToComponentType_NullOrEmpty_ReturnsNull()
        {
            Assert.Null(VanillaArchetypeMapper.MapUnitClassToComponentType(null!));
            Assert.Null(VanillaArchetypeMapper.MapUnitClassToComponentType(""));
            Assert.Null(VanillaArchetypeMapper.MapUnitClassToComponentType("   "));
        }
    }

    /// <summary>
    /// Unit tests for UnitSpawnRequest data structure.
    /// </summary>
    public class UnitSpawnRequestTests
    {
        [Fact]
        public void Constructor_WithAllParameters_InitializesCorrectly()
        {
            var request = new UnitSpawnRequest("modern:m1_abrams", 10.5f, 20.3f, isEnemy: true);

            Assert.Equal("modern:m1_abrams", request.UnitDefinitionId);
            Assert.Equal(10.5f, request.X);
            Assert.Equal(20.3f, request.Z);
            Assert.True(request.IsEnemy);
        }

        [Fact]
        public void Constructor_DefaultFaction_InitializesToPlayerFaction()
        {
            var request = new UnitSpawnRequest("modern:m1_abrams", 10f, 20f);

            Assert.False(request.IsEnemy);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var request = new UnitSpawnRequest("pack:unit", 5f, 15f, isEnemy: false);
            var result = request.ToString();

            Assert.Contains("UnitSpawnRequest", result);
            Assert.Contains("pack:unit", result);
            Assert.Contains("player", result);
        }

        [Fact]
        public void ToString_EnemyFaction_IncludesEnemyLabel()
        {
            var request = new UnitSpawnRequest("pack:unit", 5f, 15f, isEnemy: true);
            var result = request.ToString();

            Assert.Contains("enemy", result);
        }

        [Fact]
        public void Equality_SameValues_ReturnsEqual()
        {
            var request1 = new UnitSpawnRequest("pack:unit", 10f, 20f, isEnemy: true);
            var request2 = new UnitSpawnRequest("pack:unit", 10f, 20f, isEnemy: true);

            // Structs use value equality by default
            Assert.Equal(request1, request2);
        }

        [Fact]
        public void Equality_DifferentValues_ReturnsNotEqual()
        {
            var request1 = new UnitSpawnRequest("pack:unit", 10f, 20f, isEnemy: true);
            var request2 = new UnitSpawnRequest("pack:unit", 10f, 20f, isEnemy: false);

            Assert.NotEqual(request1, request2);
        }
    }

    /// <summary>
    /// Integration tests for PackUnitSpawner (placeholder for M9 implementation).
    /// </summary>
    public class PackUnitSpawnerTests
    {
        [Fact(Skip = "M9 implementation pending")]
        public void RequestSpawn_ValidUnit_EnqueuesRequest()
        {
            // TODO: M9 implementation
            // This test will require an ECS test environment.
            // See: StatModifierSystemTests pattern for ECS testing setup
        }

        [Fact(Skip = "M9 implementation pending")]
        public void OnUpdate_ProcessesSpawnQueue_CreatesEntities()
        {
            // TODO: M9 implementation
            // Verify that spawn requests are processed
            // and entities are created in the world
        }

        [Fact(Skip = "M9 implementation pending")]
        public void CanSpawn_ValidUnitDefinition_ReturnsTrue()
        {
            // TODO: M9 implementation
            // Mock unit registry and test that CanSpawn correctly evaluates
            // whether a unit can be spawned
        }

        [Fact(Skip = "M9 implementation pending")]
        public void RequestSpawn_UnknownUnitClass_SkipsSpawn()
        {
            // TODO: M9 implementation
            // Verify that spawn requests for unmapped unit classes are logged and skipped
        }

        [Fact(Skip = "M9 implementation pending")]
        public void IUnitFactory_Implementation_WorksCorrectly()
        {
            // TODO: M9 implementation
            // Verify that PackUnitSpawner correctly implements IUnitFactory interface
        }
    }
}
