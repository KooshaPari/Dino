#nullable enable
using System;
using DINOForge.Runtime.Settings;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="PackSettingsStore"/> -- per-pack runtime settings persistence.
/// Validates the parameterless constructor, get/set/has patterns, and disposal.
/// </summary>
public class ConfigTests
{
    [Fact]
    public void PackSettingsStore_ParameterlessConstructor_CreatesTempPath()
    {
        using var store = new PackSettingsStore();

        string result = store.Get("nonexistent", "key", "fallback");
        result.Should().Be("fallback");
    }

    [Fact]
    public void PackSettingsStore_GetOrDefault_ReturnsDefault()
    {
        using var store = new PackSettingsStore();

        string result = store.Get("test-pack", "missing-key", "default-value");
        result.Should().Be("default-value");
    }

    [Fact]
    public void PackSettingsStore_Get_WithUnknownKey_ReturnsDefault()
    {
        using var store = new PackSettingsStore();

        int result = store.Get("unknown-pack", "unknown-key", 42);
        result.Should().Be(42);
    }

    [Fact]
    public void PackSettingsStore_Dispose_DoesNotThrow()
    {
        var store = new PackSettingsStore();
        Action dispose = () => store.Dispose();
        dispose.Should().NotThrow();
    }

    [Fact]
    public void PackSettingsStore_Constructor_WithEmptyPath_Throws()
    {
        Action act = () => new PackSettingsStore(string.Empty);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void PackSettingsStore_HasPack_ReturnsFalseForUnknown()
    {
        using var store = new PackSettingsStore();

        store.HasPack("definitely-not-a-real-pack").Should().BeFalse();
    }
}
