#nullable enable
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="DINOForge.Runtime.Plugin"/> static properties and state.
/// These validate the static surface area of the BepInEx plugin entry point.
/// </summary>
public class PluginTests
{
    [Fact]
    public void PluginInfo_HasCorrectGuid()
    {
        DINOForge.Runtime.PluginInfo.GUID.Should().Be("com.dinoforge.runtime");
    }

    [Fact]
    public void PluginInfo_HasCorrectName()
    {
        DINOForge.Runtime.PluginInfo.NAME.Should().Be("DINOForge Runtime");
    }

    [Fact]
    public void PluginInfo_HasVersionString()
    {
        DINOForge.Runtime.PluginInfo.VERSION.Should().NotBeNullOrWhiteSpace();
        DINOForge.Runtime.PluginInfo.VERSION.Should().Contain(".");
    }

    [Fact]
    public void Plugin_ResurrectionParamsReady_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.ResurrectionParamsReady.Should().BeFalse();
    }

    [Fact]
    public void Plugin_PendingF9Toggle_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.PendingF9Toggle.Should().BeFalse();
    }

    [Fact]
    public void Plugin_PendingF10Toggle_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.PendingF10Toggle.Should().BeFalse();
    }

    [Fact]
    public void Plugin_MaxResurrectionAttempts_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.Plugin)
            .GetField("MaxResurrectionAttempts",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().BeGreaterThan(0);
    }
}
