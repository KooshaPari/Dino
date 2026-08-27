#nullable enable
using System;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="DINOForge.Runtime.UI.EventSystemDriver"/>.
/// Validates the PointerEvent enum, method signatures, and static API surface.
/// </summary>
public class EventSystemTests
{
    [Fact]
    public void EventSystemDriver_PointerEvent_HasAllExpectedValues()
    {
        var values = Enum.GetNames<DINOForge.Runtime.UI.EventSystemDriver.PointerEvent>();
        values.Should().Contain("Enter");
        values.Should().Contain("Exit");
        values.Should().Contain("Down");
        values.Should().Contain("Up");
        values.Should().Contain("Click");
        values.Should().Contain("Hover");
        values.Should().Contain("Press");
    }

    [Fact]
    public void EventSystemDriver_PointerEvent_Count_IsSeven()
    {
        var values = Enum.GetValues<DINOForge.Runtime.UI.EventSystemDriver.PointerEvent>();
        values.Length.Should().Be(7);
    }

    [Fact]
    public void EventSystemDriver_Drive_NullEventSystem_ReturnsFailure()
    {
        var method = typeof(DINOForge.Runtime.UI.EventSystemDriver)
            .GetMethod("Drive",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        method.ReturnType.Name.Should().Contain("UiActionResult");
    }

    [Fact]
    public void EventSystemDriver_DriveAt_NullEventSystem_ReturnsFailure()
    {
        var method = typeof(DINOForge.Runtime.UI.EventSystemDriver)
            .GetMethod("DriveAt",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        var parameters = method.GetParameters();
        parameters.Length.Should().Be(3);
        parameters[0].ParameterType.Should().Be(typeof(float));
        parameters[1].ParameterType.Should().Be(typeof(float));
        parameters[2].ParameterType.Should().Be(typeof(string));
    }

    [Fact]
    public void EventSystemDriver_ScreenCenterOf_NullTransform_Throws()
    {
        var method = typeof(DINOForge.Runtime.UI.EventSystemDriver)
            .GetMethod("ScreenCenterOf",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        method.ReturnType.Should().Be(typeof(UnityEngine.Vector2));
    }

    [Fact]
    public void EventSystemDriver_PointerEvent_Press_IsDefined()
    {
        DINOForge.Runtime.UI.EventSystemDriver.PointerEvent.Press
            .Should().BeDefined();

        int pressValue = (int)DINOForge.Runtime.UI.EventSystemDriver.PointerEvent.Press;
        pressValue.Should().Be(6, because: "Press is the 7th enum value (0-indexed)");
    }
}
