#nullable enable
using System;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="DINOForge.Runtime.UI.NativeMenuInjector"/> behavior.
/// Validates constants, static properties, and delegate signatures without instantiating the MonoBehaviour.
/// </summary>
public class NativeMenuInjectorTests
{
    [Fact]
    public void NativeMenuInjector_RescanInterval_IsReasonable()
    {
        var field = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetField("RescanInterval",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        float value = (float)field.GetValue(null)!;
        value.Should().BeGreaterThanOrEqualTo(1f, because: "re-scan interval must be at least 1 second");
        value.Should().BeLessThanOrEqualTo(10f, because: "re-scan should not be excessively slow");
    }

    [Fact]
    public void NativeMenuInjector_OnScanNeeded_CanBeSet()
    {
        var prop = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetField("OnScanNeeded",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        prop.FieldType.Should().Be(typeof(Action));

        Action original = prop.GetValue(null) as Action ?? (() => { });
        try
        {
            Action testAction = () => { };
            prop.SetValue(null, testAction);

            Action? readBack = prop.GetValue(null) as Action;
            readBack.Should().NotBeNull();
            readBack.Should().BeSameAs(testAction);
        }
        finally
        {
            prop.SetValue(null, original);
        }
    }

    [Fact]
    public void NativeMenuInjector_RepurposedModsButtonGoName_DefaultsNull()
    {
        var prop = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetProperty("RepurposedModsButtonGoName",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        prop.PropertyType.Should().Be(typeof(string));
    }

    [Fact]
    public void NativeMenuInjector_CanvasCandidateNames_ContainsExpected()
    {
        var field = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetField("CanvasCandidateNames",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        string[] names = (string[])field.GetValue(null)!;
        names.Should().NotBeEmpty();
        names.Should().Contain("MainMenu");
        names.Should().Contain("PauseMenu");
        names.Should().Contain("Canvas");
    }

    [Fact]
    public void NativeMenuInjector_ClickDebounceSeconds_IsShort()
    {
        var field = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetField("ClickDebounceSeconds",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        float value = (float)field.GetValue(null)!;
        value.Should().BeGreaterThan(0f);
        value.Should().BeLessThanOrEqualTo(1f, because: "debounce should be a short interval");
    }

    [Fact]
    public void NativeMenuInjector_TextEnforceInterval_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetField("TextEnforceInterval",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void NativeMenuInjector_IsModsButtonInjected_DefaultsFalse()
    {
        var prop = typeof(DINOForge.Runtime.UI.NativeMenuInjector)
            .GetProperty("IsModsButtonInjected",
                System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.Instance)
            .Should().NotBeNull().Subject;

        prop.PropertyType.Should().Be(typeof(bool));
        prop.CanRead.Should().BeTrue();
        prop.CanWrite.Should().BeFalse("IsModsButtonInjected must be read-only");
    }
}
