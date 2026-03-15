#nullable enable
using System;
using System.Collections.Generic;

namespace DINOForge.Runtime.UI
{
    /// <summary>
    /// Integrates the DINOForge mod menu as a native screen within the game's main menu UI.
    /// When <see cref="CanUseNativeScreen"/> returns true, <see cref="ContextualModMenuHost"/>
    /// prefers this host over the DFCanvas overlay.
    /// </summary>
    /// <remarks>
    /// Stub implementation. Full native menu integration is deferred to M11.5.
    /// See WBS WI-004a. At runtime this host always reports <see cref="CanUseNativeScreen"/> = false,
    /// causing <see cref="ContextualModMenuHost"/> to fall back transparently to the overlay host.
    /// </remarks>
    internal sealed class NativeMainMenuModMenu : IModMenuHost
    {
        /// <inheritdoc />
        public Action? OnReloadRequested { get; set; }

        /// <inheritdoc />
        public Action<string, bool>? OnPackToggled { get; set; }

        /// <inheritdoc />
        public bool IsVisible => false;

        /// <summary>
        /// Returns false until native main-menu injection is implemented in M11.5 (WBS WI-004a).
        /// <see cref="ContextualModMenuHost"/> uses this to decide which host receives input.
        /// </summary>
        public bool CanUseNativeScreen => false;

        /// <inheritdoc />
        /// <remarks>No-op stub — native menu not yet available (M11.5, WBS WI-004a).</remarks>
        public void Show() { }

        /// <inheritdoc />
        /// <remarks>No-op stub — native menu not yet available (M11.5, WBS WI-004a).</remarks>
        public void Hide() { }

        /// <inheritdoc />
        /// <remarks>No-op stub — native menu not yet available (M11.5, WBS WI-004a).</remarks>
        public void Toggle() { }

        /// <inheritdoc />
        /// <remarks>No-op stub — native menu not yet available (M11.5, WBS WI-004a).</remarks>
        public void SetPacks(IEnumerable<PackDisplayInfo> packs) { }

        /// <inheritdoc />
        /// <remarks>No-op stub — native menu not yet available (M11.5, WBS WI-004a).</remarks>
        public void SetStatus(string message, int errorCount = 0) { }
    }
}
