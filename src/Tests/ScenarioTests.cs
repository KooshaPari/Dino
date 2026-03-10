using System;
using System.Collections.Generic;
using DINOForge.Domains.Scenario;
using DINOForge.Domains.Scenario.Balance;
using DINOForge.Domains.Scenario.Models;
using DINOForge.Domains.Scenario.Scripting;
using DINOForge.SDK.Models;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class ScenarioTests
    {
        // ── DifficultyScaler ────────────────────────────────

        [Theory]
        [InlineData(Difficulty.Easy, 1.5f)]
        [InlineData(Difficulty.Normal, 1.0f)]
        [InlineData(Difficulty.Hard, 0.7f)]
        [InlineData(Difficulty.Nightmare, 0.5f)]
        public void DifficultyScaler_GetMultiplier_ReturnsCorrectValues(Difficulty difficulty, float expected)
        {
            DifficultyScaler scaler = new DifficultyScaler();
            scaler.GetDifficultyMultiplier(difficulty).Should().Be(expected);
        }

        [Fact]
        public void DifficultyScaler_ScaleResources_ScalesByDifficulty()
        {
            DifficultyScaler scaler = new DifficultyScaler();
            ResourceCost baseResources = new ResourceCost
            {
                Food = 100,
                Wood = 200,
                Stone = 50,
                Iron = 30,
                Gold = 10
            };

            ResourceCost easy = scaler.ScaleResources(baseResources, Difficulty.Easy);
            easy.Food.Should().Be(150); // 100 * 1.5
            easy.Wood.Should().Be(300); // 200 * 1.5

            ResourceCost hard = scaler.ScaleResources(baseResources, Difficulty.Hard);
            hard.Food.Should().Be(70); // 100 * 0.7
        }

        [Fact]
        public void DifficultyScaler_ScaleResources_ClampsToZero()
        {
            DifficultyScaler scaler = new DifficultyScaler();
            ResourceCost baseResources = new ResourceCost { Food = 0, Wood = 0 };

            ResourceCost result = scaler.ScaleResources(baseResources, Difficulty.Nightmare);
            result.Food.Should().BeGreaterOrEqualTo(0);
            result.Wood.Should().BeGreaterOrEqualTo(0);
        }

        [Fact]
        public void DifficultyScaler_ScaleWaveIntensity_IncreasesWithWaveNumber()
        {
            DifficultyScaler scaler = new DifficultyScaler();
            float wave1 = scaler.ScaleWaveIntensity(1.0f, Difficulty.Normal, 1);
            float wave5 = scaler.ScaleWaveIntensity(1.0f, Difficulty.Normal, 5);
            float wave10 = scaler.ScaleWaveIntensity(1.0f, Difficulty.Normal, 10);

            wave5.Should().BeGreaterThan(wave1);
            wave10.Should().BeGreaterThan(wave5);
        }

        [Fact]
        public void DifficultyScaler_ScaleWaveIntensity_HarderDifficultyIsMoreIntense()
        {
            DifficultyScaler scaler = new DifficultyScaler();
            float easy = scaler.ScaleWaveIntensity(1.0f, Difficulty.Easy, 5);
            float normal = scaler.ScaleWaveIntensity(1.0f, Difficulty.Normal, 5);
            float hard = scaler.ScaleWaveIntensity(1.0f, Difficulty.Hard, 5);

            hard.Should().BeGreaterThan(normal);
            normal.Should().BeGreaterThan(easy);
        }

        [Fact]
        public void DifficultyScaler_ScaleWaveIntensity_RejectsZeroWave()
        {
            DifficultyScaler scaler = new DifficultyScaler();
            Action act = () => scaler.ScaleWaveIntensity(1.0f, Difficulty.Normal, 0);
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        // ── ScenarioRunner ──────────────────────────────────

        [Fact]
        public void ScenarioRunner_Initialize_SetsCurrentScenario()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition { Id = "test" };

            runner.Initialize(scenario);

            runner.IsInitialized.Should().BeTrue();
            runner.CurrentScenario!.Id.Should().Be("test");
        }

        [Fact]
        public void ScenarioRunner_CheckVictory_ThrowsIfNotInitialized()
        {
            ScenarioRunner runner = new ScenarioRunner();
            GameState state = new GameState();

            Action act = () => runner.CheckVictoryConditions(state);
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void ScenarioRunner_CheckVictory_SurviveWaves_TriggersAtTargetWave()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "survive-test",
                VictoryConditions = new List<VictoryCondition>
                {
                    new VictoryCondition { ConditionType = VictoryConditionType.SurviveWaves, TargetValue = 10 }
                }
            };
            runner.Initialize(scenario);

            GameState earlyState = new GameState { CurrentWave = 5 };
            runner.CheckVictoryConditions(earlyState).Should().BeFalse();

            GameState lateState = new GameState { CurrentWave = 10 };
            runner.CheckVictoryConditions(lateState).Should().BeTrue();
        }

        [Fact]
        public void ScenarioRunner_CheckVictory_ReachPopulation()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "pop-test",
                VictoryConditions = new List<VictoryCondition>
                {
                    new VictoryCondition { ConditionType = VictoryConditionType.ReachPopulation, TargetValue = 100 }
                }
            };
            runner.Initialize(scenario);

            GameState state = new GameState { Population = 100 };
            runner.CheckVictoryConditions(state).Should().BeTrue();
        }

        [Fact]
        public void ScenarioRunner_CheckVictory_AccumulateResource()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "resource-test",
                VictoryConditions = new List<VictoryCondition>
                {
                    new VictoryCondition
                    {
                        ConditionType = VictoryConditionType.AccumulateResource,
                        TargetValue = 5000,
                        TargetId = "gold"
                    }
                }
            };
            runner.Initialize(scenario);

            GameState state = new GameState { Resources = new Dictionary<string, int> { { "gold", 5000 } } };
            runner.CheckVictoryConditions(state).Should().BeTrue();
        }

        [Fact]
        public void ScenarioRunner_CheckDefeat_CommandCenterDestroyed()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "defeat-test",
                DefeatConditions = new List<DefeatCondition>
                {
                    new DefeatCondition { ConditionType = DefeatConditionType.CommandCenterDestroyed }
                }
            };
            runner.Initialize(scenario);

            GameState alive = new GameState { CommandCenterAlive = true };
            runner.CheckDefeatConditions(alive).Should().BeFalse();

            GameState destroyed = new GameState { CommandCenterAlive = false };
            runner.CheckDefeatConditions(destroyed).Should().BeTrue();
        }

        [Fact]
        public void ScenarioRunner_CheckDefeat_PopulationZero()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "pop-defeat-test",
                DefeatConditions = new List<DefeatCondition>
                {
                    new DefeatCondition { ConditionType = DefeatConditionType.PopulationZero }
                }
            };
            runner.Initialize(scenario);

            GameState state = new GameState { Population = 0 };
            runner.CheckDefeatConditions(state).Should().BeTrue();
        }

        [Fact]
        public void ScenarioRunner_CheckDefeat_NoConditions_ReturnsFalse()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition { Id = "no-defeat" };
            runner.Initialize(scenario);

            GameState state = new GameState { CommandCenterAlive = false };
            runner.CheckDefeatConditions(state).Should().BeFalse();
        }

        [Fact]
        public void ScenarioRunner_GetPendingEvents_FiresOnce()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "events-test",
                ScriptedEvents = new List<ScriptedEvent>
                {
                    new ScriptedEvent
                    {
                        Id = "wave3-event",
                        TriggerType = TriggerType.OnWave,
                        TriggerValue = 3,
                        Actions = new List<EventAction>
                        {
                            new EventAction { ActionType = ActionType.ShowMessage }
                        }
                    }
                }
            };
            runner.Initialize(scenario);

            GameState state = new GameState { CurrentWave = 3 };
            IReadOnlyList<ScriptedEvent> events = runner.GetPendingEvents(state);
            events.Should().HaveCount(1);
            events[0].Id.Should().Be("wave3-event");

            // Should not fire again
            IReadOnlyList<ScriptedEvent> eventsAgain = runner.GetPendingEvents(state);
            eventsAgain.Should().BeEmpty();
        }

        [Fact]
        public void ScenarioRunner_ResetEvents_AllowsRefire()
        {
            ScenarioRunner runner = new ScenarioRunner();
            ScenarioDefinition scenario = new ScenarioDefinition
            {
                Id = "reset-test",
                ScriptedEvents = new List<ScriptedEvent>
                {
                    new ScriptedEvent { Id = "evt1", TriggerType = TriggerType.OnWave, TriggerValue = 1 }
                }
            };
            runner.Initialize(scenario);

            GameState state = new GameState { CurrentWave = 1 };
            runner.GetPendingEvents(state).Should().HaveCount(1);
            runner.GetPendingEvents(state).Should().BeEmpty();

            runner.ResetEvents();
            runner.GetPendingEvents(state).Should().HaveCount(1);
        }

        // ── ScenarioPlugin ──────────────────────────────────

        [Fact]
        public void ScenarioPlugin_Constructor_InitializesSubsystems()
        {
            RegistryManager registries = new RegistryManager();
            ScenarioPlugin plugin = new ScenarioPlugin(registries);

            plugin.Runner.Should().NotBeNull();
            plugin.Validator.Should().NotBeNull();
            plugin.DifficultyScaler.Should().NotBeNull();
        }

        [Fact]
        public void ScenarioPlugin_ValidatePack_ReturnsValidForEmptyPack()
        {
            RegistryManager registries = new RegistryManager();
            ScenarioPlugin plugin = new ScenarioPlugin(registries);

            ScenarioValidationResult result = plugin.ValidatePack("test-pack", new List<ScenarioDefinition>());
            result.IsValid.Should().BeTrue();
            result.ScenarioCount.Should().Be(0);
        }

        [Fact]
        public void ScenarioPlugin_ValidatePack_WarnsAboutMissingConditions()
        {
            RegistryManager registries = new RegistryManager();
            ScenarioPlugin plugin = new ScenarioPlugin(registries);

            List<ScenarioDefinition> scenarios = new List<ScenarioDefinition>
            {
                new ScenarioDefinition { Id = "no-conditions", DisplayName = "No Conditions Test", WaveCount = 5 }
            };

            ScenarioValidationResult result = plugin.ValidatePack("test-pack", scenarios);
            result.IsValid.Should().BeTrue(); // warnings don't make it invalid
            result.Warnings.Should().Contain(w => w.Contains("no victory conditions"));
            result.Warnings.Should().Contain(w => w.Contains("no defeat conditions"));
        }

        [Fact]
        public void ScenarioPlugin_ValidatePack_RejectsEmptyPackId()
        {
            RegistryManager registries = new RegistryManager();
            ScenarioPlugin plugin = new ScenarioPlugin(registries);

            Action act = () => plugin.ValidatePack("", new List<ScenarioDefinition>());
            act.Should().Throw<ArgumentException>();
        }
    }
}
