#nullable enable
using System.Collections.Generic;
using System.IO;
using DINOForge.SDK.Models;

namespace DINOForge.SDK
{
    /// <summary>
    /// Manages texture, audio, and UI asset path substitution for a Total Conversion pack.
    /// When a mod-supplied asset file exists on disk the engine substitutes the mod path;
    /// otherwise it falls back transparently to the original vanilla path.
    /// </summary>
    public class AssetReplacementEngine
    {
        private readonly Dictionary<string, string> _textures;
        private readonly Dictionary<string, string> _audio;
        private readonly Dictionary<string, string> _ui;
        private readonly string _packBasePath;

        /// <summary>
        /// Initializes the engine with the asset replacement mappings from the supplied manifest.
        /// </summary>
        /// <param name="manifest">
        /// The <see cref="TotalConversionManifest"/> whose
        /// <see cref="TcAssetReplacements"/> provide the vanilla-to-mod path mappings.
        /// </param>
        /// <param name="packBasePath">
        /// The base directory of the pack on disk. Mod asset paths are resolved relative to this root
        /// when performing existence checks.
        /// </param>
        public AssetReplacementEngine(TotalConversionManifest manifest, string packBasePath)
        {
            _packBasePath = packBasePath ?? "";
            _textures = manifest.AssetReplacements?.Textures ?? new Dictionary<string, string>();
            _audio    = manifest.AssetReplacements?.Audio    ?? new Dictionary<string, string>();
            _ui       = manifest.AssetReplacements?.Ui       ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Resolves a vanilla texture path to its mod replacement.
        /// Returns the mod path when the file exists on disk; otherwise returns
        /// <paramref name="vanillaPath"/> unchanged.
        /// </summary>
        /// <param name="vanillaPath">The original vanilla asset path to look up.</param>
        /// <returns>The resolved asset path (mod or vanilla).</returns>
        public string ResolveTexture(string vanillaPath)
            => Resolve(_textures, vanillaPath);

        /// <summary>
        /// Resolves a vanilla audio path to its mod replacement.
        /// Returns the mod path when the file exists on disk; otherwise returns
        /// <paramref name="vanillaPath"/> unchanged.
        /// </summary>
        /// <param name="vanillaPath">The original vanilla asset path to look up.</param>
        /// <returns>The resolved asset path (mod or vanilla).</returns>
        public string ResolveAudio(string vanillaPath)
            => Resolve(_audio, vanillaPath);

        /// <summary>
        /// Resolves a vanilla UI element path to its mod replacement.
        /// Returns the mod path when the file exists on disk; otherwise returns
        /// <paramref name="vanillaPath"/> unchanged.
        /// </summary>
        /// <param name="vanillaPath">The original vanilla asset path to look up.</param>
        /// <returns>The resolved asset path (mod or vanilla).</returns>
        public string ResolveUi(string vanillaPath)
            => Resolve(_ui, vanillaPath);

        /// <summary>Gets the number of texture replacement mappings registered.</summary>
        public int TextureCount => _textures.Count;

        /// <summary>Gets the number of audio replacement mappings registered.</summary>
        public int AudioCount => _audio.Count;

        /// <summary>Gets the number of UI replacement mappings registered.</summary>
        public int UiCount => _ui.Count;

        private string Resolve(Dictionary<string, string> map, string vanillaPath)
        {
            if (vanillaPath == null)
                return vanillaPath!;

            if (!map.TryGetValue(vanillaPath, out string? modPath) || modPath == null)
                return vanillaPath;

            string fullPath = Path.IsPathRooted(modPath)
                ? modPath
                : Path.Combine(_packBasePath, modPath);

            return File.Exists(fullPath) ? modPath : vanillaPath;
        }
    }
}
