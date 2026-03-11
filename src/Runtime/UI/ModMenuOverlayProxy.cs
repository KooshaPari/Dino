#nullable enable
using System.Collections.Generic;

namespace DINOForge.Runtime.UI
{
    /// <summary>
    /// Thin subclass of <see cref="ModMenuOverlay"/> that forwards
    /// <see cref="SetPacks"/> and <see cref="SetStatus"/> calls to a
    /// <see cref="ModMenuPanel"/> (UGUI). Used by RuntimeDriver to bridge
    /// ModPlatform's typed dependency on ModMenuOverlay to the UGUI system
    /// without modifying ModPlatform.
    ///
    /// The base IMGUI rendering is never triggered because this component
    /// is disabled (enabled = false) in RuntimeDriver.
    /// </summary>
    internal sealed class ModMenuOverlayProxy : ModMenuOverlay
    {
        private ModMenuPanel? _target;

        /// <summary>Sets the UGUI panel that receives forwarded calls.</summary>
        public void SetTarget(ModMenuPanel panel)
        {
            _target = panel;

            // Forward callbacks from ModPlatform back to UGUI panel
            OnReloadRequested = () => _target?.OnReloadRequested?.Invoke();
            OnPackToggled = (id, enabled) => _target?.OnPackToggled?.Invoke(id, enabled);
        }

        /// <inheritdoc />
        public override void SetPacks(IEnumerable<PackDisplayInfo> packs)
        {
            // Forward to UGUI panel; do NOT call base (no IMGUI state needed)
            _target?.SetPacks(packs);
        }

        /// <inheritdoc />
        public override void SetStatus(string message, int errorCount = 0)
        {
            // Forward to UGUI panel; do NOT call base
            _target?.SetStatus(message, errorCount);
        }
    }
}
