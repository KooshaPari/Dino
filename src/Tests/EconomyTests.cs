using System.Collections.Generic;
using DINOForge.Domains.Economy;
using DINOForge.Domains.Economy.Models;
using DINOForge.Domains.Economy.Rates;
using DINOForge.Domains.Economy.Trade;
using DINOForge.SDK.Models;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class EconomyTests
    {
        // ── ResourceRate ────────────────────────────────────

        [Fact]
        public void ResourceRate_EffectiveRate_MultipliesBaseByMultiplier()
        {
            ResourceRate rate = new ResourceRate { BaseRate = 10f, Multiplier = 1.5f };
            rate.EffectiveRate.Should().Be(15f);
        }

        [Fact]
        public void ResourceRate_DefaultMultiplier_IsOne()
        {
            ResourceRate rate = new ResourceRate { BaseRate = 5f };
            rate.EffectiveRate.Should().Be(5f);
        }

        [Fact]
        public void ResourceRate_ValidTypes_ContainsAllFiveResources()
        {
            ResourceRate.ValidResourceTypes.Should().BeEquivalentTo(
                new[] { "food", "wood", "stone", "iron", "gold" });
        }

        // ── EconomyProfile ──────────────────────────────────

        [Fact]
        public void EconomyProfile_GetProductionMultiplier_ReturnsSetValue()
        {
            EconomyProfile profile = new EconomyProfile();
            profile.ProductionMultipliers["wood"] = 2.0f;
            profile.GetProductionMultiplier("wood").Should().Be(2.0f);
        }

        [Fact]
        public void EconomyProfile_GetProductionMultiplier_DefaultsToOne()
        {
            EconomyProfile profile = new EconomyProfile();
            profile.GetProductionMultiplier("stone").Should().Be(1.0f);
        }

        [Fact]
        public void EconomyProfile_GetConsumptionMultiplier_DefaultsToOne()
        {
            EconomyProfile profile = new EconomyProfile();
            profile.GetConsumptionMultiplier("food").Should().Be(1.0f);
        }

        // ── TradeRoute ──────────────────────────────────────

        [Fact]
        public void TradeRoute_DefaultValues_AreValid()
        {
            TradeRoute route = new TradeRoute();
            route.ExchangeRate.Should().Be(1.0f);
            route.CooldownTicks.Should().Be(60);
            route.Enabled.Should().BeTrue();
        }

        // ── TradeEngine ─────────────────────────────────────

        [Fact]
        public void TradeEngine_CalculateExchangeRate_AppliesProfileModifier()
        {
            TradeEngine engine = new TradeEngine();
            TradeRoute route = new TradeRoute { ExchangeRate = 10.0f };
            EconomyProfile profile = new EconomyProfile { TradeRateModifier = 0.8f };

            float rate = engine.CalculateExchangeRate(route, profile);
            rate.Should().Be(8.0f);
        }

        [Fact]
        public void TradeEngine_EvaluateTradeRoute_DetectsProfitableTradeWhenSurplusAndDeficit()
        {
            TradeEngine engine = new TradeEngine();
            TradeRoute route = new TradeRoute
            {
                SourceResource = "wood",
                TargetResource = "gold",
                ExchangeRate = 10.0f,
                Enabled = true
            };
            EconomyProfile profile = new EconomyProfile { TradeRateModifier = 1.0f };
            Dictionary<string, float> available = new Dictionary<string, float>
            {
                { "wood", 500f }, { "gold", 0f }
            };
            Dictionary<string, float> balance = new Dictionary<string, float>
            {
                { "wood", 50f }, { "gold", -10f }
            };

            TradeEvaluation eval = engine.EvaluateTradeRoute(route, profile, available, balance);
            eval.IsProfitable.Should().BeTrue();
            eval.EffectiveExchangeRate.Should().Be(10.0f);
            eval.MaxTargetPerExecution.Should().Be(50f); // 500 / 10
        }

        [Fact]
        public void TradeEngine_EvaluateTradeRoute_NotProfitableWhenNoSurplus()
        {
            TradeEngine engine = new TradeEngine();
            TradeRoute route = new TradeRoute
            {
                SourceResource = "wood",
                TargetResource = "gold",
                ExchangeRate = 10.0f,
                Enabled = true
            };
            EconomyProfile profile = new EconomyProfile();
            Dictionary<string, float> available = new Dictionary<string, float>
            {
                { "wood", 100f }, { "gold", 0f }
            };
            Dictionary<string, float> balance = new Dictionary<string, float>
            {
                { "wood", -5f }, { "gold", -10f }
            };

            TradeEvaluation eval = engine.EvaluateTradeRoute(route, profile, available, balance);
            eval.IsProfitable.Should().BeFalse();
        }

        [Fact]
        public void TradeEngine_GetOptimalTrades_SuggestsTradeForDeficit()
        {
            TradeEngine engine = new TradeEngine();
            List<TradeRoute> routes = new List<TradeRoute>
            {
                new TradeRoute
                {
                    Id = "wood-to-gold",
                    SourceResource = "wood",
                    TargetResource = "gold",
                    ExchangeRate = 10.0f,
                    Enabled = true
                }
            };
            EconomyProfile profile = new EconomyProfile();
            Dictionary<string, float> available = new Dictionary<string, float>
            {
                { "wood", 1000f }, { "gold", 0f }
            };
            Dictionary<string, float> balance = new Dictionary<string, float>
            {
                { "wood", 50f }, { "gold", -20f }
            };

            List<TradeSuggestion> suggestions = engine.GetOptimalTrades(routes, profile, available, balance);
            suggestions.Should().HaveCount(1);
            suggestions[0].Route.Id.Should().Be("wood-to-gold");
            suggestions[0].ExpectedReturn.Should().BeGreaterThan(0);
        }

        [Fact]
        public void TradeEngine_GetOptimalTrades_ReturnsEmptyWhenNoDeficits()
        {
            TradeEngine engine = new TradeEngine();
            List<TradeRoute> routes = new List<TradeRoute>
            {
                new TradeRoute { SourceResource = "wood", TargetResource = "gold", ExchangeRate = 10.0f, Enabled = true }
            };
            EconomyProfile profile = new EconomyProfile();
            Dictionary<string, float> available = new Dictionary<string, float>
            {
                { "wood", 1000f }, { "gold", 500f }
            };
            Dictionary<string, float> balance = new Dictionary<string, float>
            {
                { "wood", 50f }, { "gold", 10f }
            };

            List<TradeSuggestion> suggestions = engine.GetOptimalTrades(routes, profile, available, balance);
            suggestions.Should().BeEmpty();
        }

        // ── ProductionCalculator ────────────────────────────

        [Fact]
        public void ProductionCalculator_BuildingOutput_AppliesProfileMultiplier()
        {
            ProductionCalculator calc = new ProductionCalculator();
            BuildingDefinition farm = new BuildingDefinition
            {
                Id = "farm",
                Production = new Dictionary<string, int> { { "food", 10 } }
            };
            EconomyProfile profile = new EconomyProfile();
            profile.ProductionMultipliers["food"] = 1.5f;

            Dictionary<string, float> output = calc.CalculateBuildingOutput(farm, profile);
            output["food"].Should().Be(15f); // 10 * 1.5 * 1(worker) * 1.0(efficiency)
        }

        [Fact]
        public void ProductionCalculator_BuildingOutput_AppliesWorkerCount()
        {
            ProductionCalculator calc = new ProductionCalculator();
            BuildingDefinition mine = new BuildingDefinition
            {
                Id = "mine",
                Production = new Dictionary<string, int> { { "iron", 5 } }
            };
            EconomyProfile profile = new EconomyProfile();
            Dictionary<string, int> workers = new Dictionary<string, int> { { "mine", 3 } };

            Dictionary<string, float> output = calc.CalculateBuildingOutput(mine, profile, workers);
            output["iron"].Should().Be(15f); // 5 * 1.0 * 3 * 1.0
        }

        [Fact]
        public void ProductionCalculator_ResourceBalance_CalculatesNetCorrectly()
        {
            ProductionCalculator calc = new ProductionCalculator();
            Dictionary<string, float> production = new Dictionary<string, float>
            {
                { "food", 100f }, { "wood", 50f }
            };
            Dictionary<string, float> consumption = new Dictionary<string, float>
            {
                { "food", 80f }, { "iron", 10f }
            };

            Dictionary<string, float> balance = calc.GetResourceBalance(production, consumption);
            balance["food"].Should().Be(20f);
            balance["wood"].Should().Be(50f);
            balance["iron"].Should().Be(-10f);
        }

        // ── EconomyPlugin ───────────────────────────────────

        [Fact]
        public void EconomyPlugin_Constructor_InitializesAllSubsystems()
        {
            RegistryManager registries = new RegistryManager();
            EconomyPlugin plugin = new EconomyPlugin(registries);

            plugin.Production.Should().NotBeNull();
            plugin.Trade.Should().NotBeNull();
            plugin.Balance.Should().NotBeNull();
            plugin.Validator.Should().NotBeNull();
        }

        [Fact]
        public void EconomyPlugin_ValidatePack_ReturnsValidForEmptyPack()
        {
            RegistryManager registries = new RegistryManager();
            EconomyPlugin plugin = new EconomyPlugin(registries);

            EconomyValidationResult result = plugin.ValidatePack(
                "test-pack",
                new List<EconomyProfile>(),
                new List<TradeRoute>());

            result.IsValid.Should().BeTrue();
        }

        [Fact]
        public void EconomyPlugin_ValidatePack_RejectsEmptyPackId()
        {
            RegistryManager registries = new RegistryManager();
            EconomyPlugin plugin = new EconomyPlugin(registries);

            System.Action act = () => plugin.ValidatePack(
                "",
                new List<EconomyProfile>(),
                new List<TradeRoute>());

            act.Should().Throw<System.ArgumentException>();
        }
    }
}
