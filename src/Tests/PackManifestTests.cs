#nullable enable
using System.Collections.Generic;
using DINOForge.Runtime.UI;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests;

/// <summary>
/// Tests for <see cref="PackDisplayInfo"/> -- the immutable display model for packs in the mod menu.
/// Validates property assignment via the constructor and property access.
/// </summary>
public class PackManifestTests
{
    private static PackDisplayInfo CreateTestPack(
        string id = "test-pack",
        string name = "Test Pack",
        string version = "1.0.0",
        string author = "TestAuthor")
    {
        return new PackDisplayInfo(
            id: id,
            name: name,
            version: version,
            author: author,
            type: "content",
            description: null,
            loadOrder: 0,
            isEnabled: true,
            dependencies: new List<string>().AsReadOnly(),
            conflicts: new List<string>().AsReadOnly());
    }

    [Fact]
    public void PackDisplayInfo_HasExpectedProperties()
    {
        var pack = CreateTestPack();

        pack.Should().NotBeNull();
        pack.Id.Should().NotBeNullOrWhiteSpace();
        pack.Name.Should().NotBeNullOrWhiteSpace();
        pack.Version.Should().NotBeNullOrWhiteSpace();
        pack.Author.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void PackDisplayInfo_Id_IsSettable()
    {
        var pack = CreateTestPack(id: "my-custom-id");
        pack.Id.Should().Be("my-custom-id");
    }

    [Fact]
    public void PackDisplayInfo_Name_IsSettable()
    {
        var pack = CreateTestPack(name: "My Custom Pack");
        pack.Name.Should().Be("My Custom Pack");
    }

    [Fact]
    public void PackDisplayInfo_IsEnabled_IsSettable()
    {
        var pack = CreateTestPack();
        pack.IsEnabled.Should().BeTrue();

        var disabledPack = pack.WithEnabled(false);
        disabledPack.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void PackDisplayInfo_Version_IsSettable()
    {
        var pack = CreateTestPack(version: "2.5.1");
        pack.Version.Should().Be("2.5.1");
    }

    [Fact]
    public void PackDisplayInfo_Author_IsSettable()
    {
        var pack = CreateTestPack(author: "SpecificAuthor");
        pack.Author.Should().Be("SpecificAuthor");
    }
}
