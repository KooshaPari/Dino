#nullable enable
using DINOForge.Bridge.Protocol;
using DINOForge.Tests.Integration.Fixtures;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests.Integration.Tests;

/// <summary>
/// Tests pack loading and reloading through the bridge.
/// </summary>
[Collection("Game")]
[Trait("Category", "Integration")]
public class PackLoadingTests
{
    private readonly GameFixture _fixture;

    /// <summary>Initializes a new instance of <see cref="PackLoadingTests"/>.</summary>
    public PackLoadingTests(GameFixture fixture) => _fixture = fixture;

    /// <summary>Verifies that reload packs succeeds.</summary>
    [Fact]
    public async Task ReloadPacks_Succeeds()
    {
        if (!_fixture.GameAvailable)
            return; // Game not available for integration testing

        ReloadResult result = await _fixture.Client.ReloadPacksAsync();

        result.Success.Should().BeTrue();
    }

    /// <summary>Verifies that the loaded packs list is non-empty after reload.</summary>
    [Fact]
    public async Task ReloadPacks_LoadedPacksNonEmpty()
    {
        if (!_fixture.GameAvailable)
            return; // Game not available for integration testing

        // Reload first to ensure state is fresh
        ReloadResult result = await _fixture.Client.ReloadPacksAsync();

        result.LoadedPacks.Should().NotBeEmpty("at least one pack should be loaded after reload");
    }
}
