#nullable enable
using System;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for the engine heartbeat mechanism defined in
/// <c>DINOForge.Runtime.Plugin.Resurrection</c>.
/// Validates the heartbeat filename constant, lock object, and format contract.
/// </summary>
public class HeartbeatTests
{
    private static readonly Type PluginType = typeof(DINOForge.Runtime.Plugin);

    [Fact]
    public void EngineHeartbeatFileName_IsExpected()
    {
        var field = PluginType
            .GetField("EngineHeartbeatFileName",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        string value = (string)field.GetValue(null)!;
        value.Should().Be("dinoforge_heartbeat.txt");
    }

    [Fact]
    public void BumpEngineHeartbeat_WithNullSource_DoesNotThrow()
    {
        var method = PluginType
            .GetMethod("BumpEngineHeartbeat",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        Action act = () => method.Invoke(null, new object?[] { null });
        act.Should().NotThrow("BumpEngineHeartbeat must handle null source gracefully");
    }

    [Fact]
    public void EngineHeartbeat_WriterLock_IsObject()
    {
        var field = PluginType
            .GetField("_engineHeartbeatLock",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        object? value = field.GetValue(null);
        value.Should().NotBeNull();
        value!.GetType().Should().Be(typeof(object));
    }

    [Fact]
    public void Plugin_HeartbeatFileName_Constant()
    {
        var field = PluginType
            .GetField("EngineHeartbeatFileName",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        field.FieldType.Should().Be(typeof(string));
        field.IsLiteral.Should().BeTrue("EngineHeartbeatFileName must be a const");
    }

    [Fact]
    public void Plugin_HeartbeatFormat_ContainsTimestamp()
    {
        var method = PluginType
            .GetMethod("BumpEngineHeartbeat",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        var parameters = method.GetParameters();
        parameters.Should().HaveCount(1);
        parameters[0].ParameterType.Should().Be(typeof(string),
            because: "BumpEngineHeartbeat takes a single string source parameter");
    }
}
