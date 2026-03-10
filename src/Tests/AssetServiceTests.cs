using System;
using System.Collections.Generic;
using System.IO;
using DINOForge.SDK.Assets;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests
{
    public class AssetServiceTests
    {
        // ── Data type construction ──────────────────────────────────────────

        [Fact]
        public void BundleInfo_StoresAllProperties()
        {
            var info = new BundleInfo("/path/to/bundle.bundle", "bundle.bundle", 1024L, 42);

            info.Path.Should().Be("/path/to/bundle.bundle");
            info.Name.Should().Be("bundle.bundle");
            info.SizeBytes.Should().Be(1024L);
            info.AssetCount.Should().Be(42);
        }

        [Fact]
        public void BundleInfo_NullPath_Throws()
        {
            Action act = () => new BundleInfo(null!, "name", 0, 0);
            act.Should().Throw<ArgumentNullException>().WithParameterName("path");
        }

        [Fact]
        public void BundleInfo_NullName_Throws()
        {
            Action act = () => new BundleInfo("/path", null!, 0, 0);
            act.Should().Throw<ArgumentNullException>().WithParameterName("name");
        }

        [Fact]
        public void AssetInfo_StoresAllProperties()
        {
            var info = new AssetInfo("MyTexture", "Texture2D", 12345L, 2048);

            info.Name.Should().Be("MyTexture");
            info.TypeName.Should().Be("Texture2D");
            info.PathId.Should().Be(12345L);
            info.SizeBytes.Should().Be(2048);
        }

        [Fact]
        public void AssetInfo_NullName_Throws()
        {
            Action act = () => new AssetInfo(null!, "Texture2D", 0, 0);
            act.Should().Throw<ArgumentNullException>().WithParameterName("name");
        }

        [Fact]
        public void AssetInfo_NullTypeName_Throws()
        {
            Action act = () => new AssetInfo("name", null!, 0, 0);
            act.Should().Throw<ArgumentNullException>().WithParameterName("typeName");
        }

        [Fact]
        public void AssetValidationResult_ValidResult_HasNoErrors()
        {
            var assets = new List<AssetInfo>
            {
                new AssetInfo("TestAsset", "Texture2D", 1, 512)
            };
            var result = new AssetValidationResult(true, "2021.3.45f2", Array.Empty<string>(), assets);

            result.IsValid.Should().BeTrue();
            result.UnityVersion.Should().Be("2021.3.45f2");
            result.Errors.Should().BeEmpty();
            result.Assets.Should().HaveCount(1);
        }

        [Fact]
        public void AssetValidationResult_Failure_HasErrorsAndNoAssets()
        {
            var result = AssetValidationResult.Failure(new[] { "File not found" });

            result.IsValid.Should().BeFalse();
            result.UnityVersion.Should().Be("unknown");
            result.Errors.Should().ContainSingle().Which.Should().Contain("File not found");
            result.Assets.Should().BeEmpty();
        }

        [Fact]
        public void AssetValidationResult_NullUnityVersion_Throws()
        {
            Action act = () => new AssetValidationResult(true, null!, Array.Empty<string>(), Array.Empty<AssetInfo>());
            act.Should().Throw<ArgumentNullException>().WithParameterName("unityVersion");
        }

        // ── AssetService constructor ────────────────────────────────────────

        [Fact]
        public void Constructor_NullGameDir_Throws()
        {
            Action act = () => new AssetService(null!);
            act.Should().Throw<ArgumentNullException>().WithParameterName("gameDir");
        }

        [Fact]
        public void Constructor_NonExistentDir_DoesNotThrow()
        {
            using var service = new AssetService("/nonexistent/game/dir");
            // Should not throw; directory existence is checked lazily
        }

        // ── ListBundles ─────────────────────────────────────────────────────

        [Fact]
        public void ListBundles_NonExistentDir_ReturnsEmpty()
        {
            using var service = new AssetService("/nonexistent/game/dir");
            IReadOnlyList<BundleInfo> bundles = service.ListBundles();
            bundles.Should().BeEmpty();
        }

        // ── ValidateModBundle ───────────────────────────────────────────────

        [Fact]
        public void ValidateModBundle_NonExistentFile_ReturnsErrors()
        {
            using var service = new AssetService("/nonexistent/game/dir");
            AssetValidationResult result = service.ValidateModBundle("/nonexistent/mod.bundle");

            result.IsValid.Should().BeFalse();
            result.Errors.Should().ContainSingle().Which.Should().Contain("not found");
        }

        // ── ExtractAsset ────────────────────────────────────────────────────

        [Fact]
        public void ExtractAsset_NonExistentBundle_ReturnsNull()
        {
            using var service = new AssetService("/nonexistent/game/dir");
            byte[]? data = service.ExtractAsset("/nonexistent/bundle.bundle", "SomeAsset");
            data.Should().BeNull();
        }

        // ── ReadCatalog ─────────────────────────────────────────────────────

        [Fact]
        public void ReadCatalog_NonExistentDir_ReturnsEmpty()
        {
            using var service = new AssetService("/nonexistent/game/dir");
            IReadOnlyDictionary<string, string> catalog = service.ReadCatalog();
            catalog.Should().BeEmpty();
        }

        // ── AddressablesCatalog ─────────────────────────────────────────────

        [Fact]
        public void AddressablesCatalog_NonExistentFile_Throws()
        {
            Action act = () => AddressablesCatalog.Load("/nonexistent/catalog.json");
            act.Should().Throw<FileNotFoundException>();
        }

        [Fact]
        public void ResolveBundlePath_ReplacesPlaceholder()
        {
            string input = @"{UnityEngine.AddressableAssets.Addressables.RuntimePath}\StandaloneWindows64\test.bundle";
            string gameDir = @"C:\Games\DINO";

            string resolved = AddressablesCatalog.ResolveBundlePath(input, gameDir);

            resolved.Should().Contain("StreamingAssets");
            resolved.Should().Contain("test.bundle");
            resolved.Should().NotContain("{UnityEngine");
        }

        [Fact]
        public void ResolveBundlePath_NoPlaceholder_ReturnsOriginal()
        {
            string input = "/some/direct/path/test.bundle";
            string resolved = AddressablesCatalog.ResolveBundlePath(input, "/game");

            resolved.Should().Be(input);
        }

        // ── Dispose ─────────────────────────────────────────────────────────

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes()
        {
            var service = new AssetService("/nonexistent/game/dir");
            service.Dispose();
            // Second dispose should not throw
            Action act = () => service.Dispose();
            act.Should().NotThrow();
        }
    }
}
