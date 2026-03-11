#nullable enable
using System;
using System.Collections.Generic;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using FluentAssertions;
using Newtonsoft.Json;
using Xunit;

namespace DINOForge.Tests;

public class BridgeClientTests
{
    [Fact]
    public void GameClientOptions_HasCorrectDefaults()
    {
        GameClientOptions options = new();

        options.PipeName.Should().Be("dinoforge-game-bridge");
        options.ConnectTimeoutMs.Should().Be(5000);
        options.ReadTimeoutMs.Should().Be(30000);
        options.RetryCount.Should().Be(3);
        options.RetryDelayMs.Should().Be(1000);
    }

    [Fact]
    public void ConnectionState_HasExpectedValues()
    {
        Enum.GetValues<ConnectionState>().Should().HaveCount(4);
        ((int)ConnectionState.Disconnected).Should().Be(0);
        ((int)ConnectionState.Connecting).Should().Be(1);
        ((int)ConnectionState.Connected).Should().Be(2);
        ((int)ConnectionState.Error).Should().Be(3);
    }

    [Fact]
    public void GameClient_DefaultConstructor_DoesNotThrow()
    {
        using GameClient client = new();

        client.State.Should().Be(ConnectionState.Disconnected);
        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void GameClient_WithOptions_DoesNotThrow()
    {
        GameClientOptions options = new() { PipeName = "test-pipe", ConnectTimeoutMs = 1000 };
        using GameClient client = new(options);

        client.State.Should().Be(ConnectionState.Disconnected);
        client.IsConnected.Should().BeFalse();
    }

    [Fact]
    public void GameProcessManager_IsRunning_ReturnsFalseWhenGameNotRunning()
    {
        GameProcessManager manager = new();

        manager.IsRunning.Should().BeFalse();
        manager.GetProcessId().Should().BeNull();
    }

    [Fact]
    public void PingResult_RoundTrip_SerializesCorrectly()
    {
        PingResult original = new() { Pong = true, Version = "0.1.0", UptimeSeconds = 123.45 };

        string json = JsonConvert.SerializeObject(original);
        PingResult? deserialized = JsonConvert.DeserializeObject<PingResult>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Pong.Should().Be(original.Pong);
        deserialized.Version.Should().Be(original.Version);
        deserialized.UptimeSeconds.Should().Be(original.UptimeSeconds);
    }

    [Fact]
    public void GameStatus_RoundTrip_SerializesCorrectly()
    {
        GameStatus original = new()
        {
            Running = true,
            WorldReady = true,
            WorldName = "TestWorld",
            EntityCount = 42,
            ModPlatformReady = true,
            LoadedPacks = new List<string> { "pack-a", "pack-b" },
            Version = "0.1.0"
        };

        string json = JsonConvert.SerializeObject(original);
        GameStatus? deserialized = JsonConvert.DeserializeObject<GameStatus>(json);

        deserialized.Should().NotBeNull();
        deserialized!.WorldReady.Should().BeTrue();
        deserialized.WorldName.Should().Be("TestWorld");
        deserialized.EntityCount.Should().Be(42);
        deserialized.LoadedPacks.Should().HaveCount(2);
    }

    [Fact]
    public void JsonRpcRequest_HasCorrectDefaults()
    {
        JsonRpcRequest request = new() { Method = "ping" };

        request.Jsonrpc.Should().Be("2.0");
        request.Method.Should().Be("ping");
        request.Params.Should().BeNull();
    }

    [Fact]
    public void JsonRpcResponse_WithError_SerializesCorrectly()
    {
        JsonRpcResponse response = new()
        {
            Id = "test-id",
            Error = new JsonRpcError { Code = -32600, Message = "Invalid request" }
        };

        string json = JsonConvert.SerializeObject(response);
        JsonRpcResponse? deserialized = JsonConvert.DeserializeObject<JsonRpcResponse>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Error.Should().NotBeNull();
        deserialized.Error!.Code.Should().Be(-32600);
        deserialized.Error.Message.Should().Be("Invalid request");
        deserialized.Result.Should().BeNull();
    }

    [Fact]
    public void ResourceSnapshot_RoundTrip_SerializesCorrectly()
    {
        ResourceSnapshot original = new()
        {
            Food = 100,
            Wood = 200,
            Stone = 50,
            Iron = 30,
            Money = 500,
            Souls = 10,
            Bones = 5,
            Spirit = 2
        };

        string json = JsonConvert.SerializeObject(original);
        ResourceSnapshot? deserialized = JsonConvert.DeserializeObject<ResourceSnapshot>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Food.Should().Be(100);
        deserialized.Wood.Should().Be(200);
        deserialized.Stone.Should().Be(50);
    }

    [Fact]
    public void GameClientException_PreservesMessage()
    {
        GameClientException ex = new("test error");
        ex.Message.Should().Be("test error");
    }

    [Fact]
    public void GameClientException_PreservesInnerException()
    {
        InvalidOperationException inner = new("inner");
        GameClientException ex = new("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
    }

    [Fact]
    public void GameClient_Dispose_TransitionsToDisconnected()
    {
        GameClient client = new();
        client.Dispose();

        client.State.Should().Be(ConnectionState.Disconnected);
    }

    [Fact]
    public void ComponentMapResult_RoundTrip_SerializesCorrectly()
    {
        ComponentMapResult original = new()
        {
            Mappings = new List<ComponentMapEntry>
            {
                new ComponentMapEntry
                {
                    SdkPath = "unit.stats.hp",
                    EcsType = "Components.Health",
                    FieldName = "currentHealth",
                    Resolved = true,
                    Description = "HP tracking"
                }
            }
        };

        string json = JsonConvert.SerializeObject(original);
        ComponentMapResult? deserialized = JsonConvert.DeserializeObject<ComponentMapResult>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Mappings.Should().HaveCount(1);
        deserialized.Mappings[0].SdkPath.Should().Be("unit.stats.hp");
        deserialized.Mappings[0].Resolved.Should().BeTrue();
    }

    [Fact]
    public void CatalogSnapshot_RoundTrip_SerializesCorrectly()
    {
        CatalogSnapshot original = new()
        {
            Units = new List<CatalogEntry>
            {
                new CatalogEntry
                {
                    InferredId = "vanilla:melee_unit",
                    ComponentCount = 15,
                    EntityCount = 42,
                    Category = "unit"
                }
            }
        };

        string json = JsonConvert.SerializeObject(original);
        CatalogSnapshot? deserialized = JsonConvert.DeserializeObject<CatalogSnapshot>(json);

        deserialized.Should().NotBeNull();
        deserialized!.Units.Should().HaveCount(1);
        deserialized.Units[0].InferredId.Should().Be("vanilla:melee_unit");
        deserialized.Units[0].EntityCount.Should().Be(42);
    }
}
