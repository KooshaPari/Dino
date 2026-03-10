using System;
using System.Collections.Generic;
using DINOForge.SDK.Registry;

namespace DINOForge.Domains.UI
{
    /// <summary>
    /// Entry point for the UI domain plugin. Registers UI-related content types
    /// and provides access to UI subsystems (menu management, HUD injection).
    /// </summary>
    public class UIPlugin
    {
        private readonly RegistryManager _registries;

        /// <summary>
        /// Manager for mod menu state and panel coordination.
        /// </summary>
        public MenuManager MenuManager { get; }

        /// <summary>
        /// Available UI content type names that packs can declare in their loads section.
        /// </summary>
        public static IReadOnlyList<string> ContentTypes { get; } = new List<string>
        {
            "ui_panels",
            "ui_themes",
            "ui_overlays",
            "hud_elements"
        }.AsReadOnly();

        /// <summary>
        /// Initializes the UI plugin with pre-loaded registries.
        /// </summary>
        /// <param name="registries">The registry manager containing all loaded content.</param>
        public UIPlugin(RegistryManager registries)
        {
            _registries = registries ?? throw new ArgumentNullException(nameof(registries));
            MenuManager = new MenuManager();
        }

        /// <summary>
        /// Validates UI-related content for a given pack.
        /// </summary>
        /// <param name="packId">The pack identifier to scope validation to.</param>
        /// <returns>List of validation errors (empty if valid).</returns>
        public IReadOnlyList<string> ValidatePack(string packId)
        {
            if (string.IsNullOrWhiteSpace(packId))
                throw new ArgumentException("Pack ID is required.", nameof(packId));

            // Currently no UI content types registered in the registry manager.
            // This is a placeholder for future UI content validation (panels, themes, overlays).
            return new List<string>().AsReadOnly();
        }
    }
}
