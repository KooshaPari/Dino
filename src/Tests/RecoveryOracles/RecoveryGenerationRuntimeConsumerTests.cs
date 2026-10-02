using System.Reflection;
using DINOForge.Runtime.Aviation;
using DINOForge.Runtime.Bridge;
using DINOForge.SDK.Registry;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public sealed class RecoveryGenerationRuntimeConsumerTests
    {
        [Fact]
        public void KnownStaticRegistryConsumers_CanRebindFromGenerationOneToGenerationTwo()
        {
            var g1 = new RegistryManager();
            var g2 = new RegistryManager();

            PackUnitSpawner.Initialize(g1);
            AerialSpawnSystem.Initialize(g1);
            BuildMenuInjector.Initialize(g1);
            WaveInjector.SetRegistryManager(g1);

            AssertPrivateStaticRegistry(typeof(PackUnitSpawner), "_registry", g1);
            AssertPrivateStaticRegistry(typeof(AerialSpawnSystem), "_registry", g1);
            AssertPrivateStaticRegistry(typeof(BuildMenuInjector), "_registry", g1);
            AssertPrivateStaticRegistry(typeof(WaveInjector), "_registry", g1);

            PackUnitSpawner.Initialize(g2);
            AerialSpawnSystem.Initialize(g2);
            BuildMenuInjector.Initialize(g2);
            WaveInjector.SetRegistryManager(g2);

            AssertPrivateStaticRegistry(typeof(PackUnitSpawner), "_registry", g2);
            AssertPrivateStaticRegistry(typeof(AerialSpawnSystem), "_registry", g2);
            AssertPrivateStaticRegistry(typeof(BuildMenuInjector), "_registry", g2);
            AssertPrivateStaticRegistry(typeof(WaveInjector), "_registry", g2);
        }

        private static void AssertPrivateStaticRegistry(System.Type type, string fieldName, RegistryManager expected)
        {
            FieldInfo? field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            field.Should().NotBeNull($"{type.Name} must expose the known retained registry field to this source-bound recovery test");
            field!.GetValue(null).Should().BeSameAs(expected);
        }
    }
}
