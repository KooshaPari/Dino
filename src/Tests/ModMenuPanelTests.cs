#nullable enable
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="DINOForge.Runtime.UI.ModMenuPanel"/> layout constants.
/// Validates that the panel dimension constants are positive and within expected ranges.
/// </summary>
public class ModMenuPanelTests
{
    private static float GetPrivateConstFloat(string fieldName)
    {
        var field = typeof(DINOForge.Runtime.UI.ModMenuPanel)
            .GetField(fieldName,
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull($"ModMenuPanel.{fieldName} must exist").Subject;

        return (float)field.GetValue(null)!;
    }

    [Fact]
    public void ModMenuPanel_PanelWidth_IsPositive()
    {
        float value = GetPrivateConstFloat("PanelWidth");
        value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ModMenuPanel_PanelHeight_IsPositive()
    {
        float value = GetPrivateConstFloat("PanelHeight");
        value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ModMenuPanel_HeaderHeight_IsPositive()
    {
        float value = GetPrivateConstFloat("HeaderHeight");
        value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ModMenuPanel_FooterHeight_IsPositive()
    {
        float value = GetPrivateConstFloat("FooterHeight");
        value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ModMenuPanel_ListWidth_IsPositive()
    {
        float value = GetPrivateConstFloat("ListWidth");
        value.Should().BeGreaterThan(0f);
    }

    [Fact]
    public void ModMenuPanel_AnimDuration_IsPositive()
    {
        float value = GetPrivateConstFloat("AnimDuration");
        value.Should().BeGreaterThan(0f);
        value.Should().BeLessThan(2f, because: "animation duration should be under 2 seconds");
    }
}
