#nullable enable
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for resurrection logic defined in <see cref="DINOForge.Runtime.Plugin.Resurrection"/>.
/// Validates the default state of static flags and resurrection constants.
/// </summary>
public class ResurrectionTests
{
    [Fact]
    public void Plugin_MaxResurrectionAttempts_IsThree()
    {
        var field = typeof(DINOForge.Runtime.Plugin)
            .GetField("MaxResurrectionAttempts",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().Be(3, because: "SPEC-004 KIS-NF4 caps resurrection at 3 consecutive attempts");
    }

    [Fact]
    public void Plugin_NeedsResurrection_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.NeedsResurrection.Should().BeFalse();
    }

    [Fact]
    public void Plugin_NeedsDeferredResurrection_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.NeedsDeferredResurrection.Should().BeFalse();
    }

    [Fact]
    public void Plugin_s_rootJustDestroyed_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.s_rootJustDestroyed.Should().BeFalse();
    }

    [Fact]
    public void Plugin_s_skipBundleUnload_DefaultsFalse()
    {
        DINOForge.Runtime.Plugin.s_skipBundleUnload.Should().BeFalse();
    }

    [Fact]
    public void Plugin_ResurrectionParamsReady_BeforeSet_IsFalse()
    {
        DINOForge.Runtime.Plugin.ResurrectionParamsReady.Should().BeFalse();
    }

    [Fact]
    public void Plugin_LastSceneNameForResurrection_DefaultsNull()
    {
        DINOForge.Runtime.Plugin.LastSceneNameForResurrection.Should().BeNull();
    }
}
