#nullable enable
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="DINOForge.Runtime.Bridge.GameBridgeServer"/> constants.
/// Validates pipe name, timeout constants, and shutdown poll interval.
/// </summary>
public class GameBridgeServerTests
{
    [Fact]
    public void GameBridgeServer_PipeName_IsExpected()
    {
        DINOForge.Runtime.Bridge.GameBridgeServer.PipeName.Should().Be("dinoforge-game-bridge");
    }

    [Fact]
    public void GameBridgeServer_MainThreadWaitTimeoutMs_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.Bridge.GameBridgeServer)
            .GetField("MainThreadWaitTimeoutMs",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GameBridgeServer_MainThreadInputWaitTimeoutMs_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.Bridge.GameBridgeServer)
            .GetField("MainThreadInputWaitTimeoutMs",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GameBridgeServer_MainThreadHeavyWaitTimeoutMs_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.Bridge.GameBridgeServer)
            .GetField("MainThreadHeavyWaitTimeoutMs",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GameBridgeServer_ShutdownPollIntervalMs_IsPositive()
    {
        var field = typeof(DINOForge.Runtime.Bridge.GameBridgeServer)
            .GetField("ShutdownPollIntervalMs",
                BindingFlags.NonPublic | BindingFlags.Static)
            .Should().NotBeNull().Subject;

        int value = (int)field.GetValue(null)!;
        value.Should().BeGreaterThan(0);
    }
}
