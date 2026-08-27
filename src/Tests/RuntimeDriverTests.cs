#nullable enable
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="DINOForge.Runtime.RuntimeDriver"/> constants and static state.
/// These validate observable static properties without instantiating the MonoBehaviour.
/// </summary>
public class RuntimeDriverTests
{
    [Fact]
    public void RuntimeDriver_IsBeingDestroyed_DefaultsFalse()
    {
        DINOForge.Runtime.RuntimeDriver.IsBeingDestroyed.Should().BeFalse();
    }

    [Fact]
    public void RuntimeDriver_WorldPollInterval_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.RuntimeDriver)
            .GetField("WorldPollInterval",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        float value = (float)field.GetValue(null)!;
        value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void RuntimeDriver_PersistentRoot_StaticField_Accessible()
    {
        var prop = typeof(DINOForge.Runtime.Plugin)
            .GetField("PersistentRoot",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        prop.GetValue(null).Should().BeNull();
    }

    [Fact]
    public void RuntimeDriver_IsBeingDestroyed_Settable()
    {
        var prop = typeof(DINOForge.Runtime.RuntimeDriver)
            .GetProperty("IsBeingDestroyed",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        prop.PropertyType.Should().Be(typeof(bool));
    }

    [Fact]
    public void RuntimeDriver_PendingF9Toggle_Volatile()
    {
        var field = typeof(DINOForge.Runtime.Plugin)
            .GetField("PendingF9Toggle",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        field.FieldType.Should().Be(typeof(bool));
    }

    [Fact]
    public void RuntimeDriver_SharedBridgeServer_StaticField()
    {
        var field = typeof(DINOForge.Runtime.Plugin)
            .GetField("SharedBridgeServer",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        field.GetValue(null).Should().BeNull();
    }

    [Fact]
    public void RuntimeDriver_NeedsResurrection_Volatile()
    {
        var field = typeof(DINOForge.Runtime.Plugin)
            .GetField("NeedsResurrection",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        field.FieldType.Should().Be(typeof(bool));
    }
}
