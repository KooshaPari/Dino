#nullable enable
using System.Collections.Generic;
using System.Linq;

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
    ///
    /// **Race Condition Handling**: ModPlatform.LoadPacks() may call SetPacks()
    /// before WireUguiToModPlatform() has called SetTarget(). This proxy queues
    /// pending SetPacks/SetStatus calls and flushes them once the target is set.
    /// </summary>
    internal sealed class ModMenuOverlayProxy : ModMenuOverlay
    {
        private ModMenuPanel? _target;

        // Queue for pending calls if _target is not yet set
        private List<(bool isSetPacks, IEnumerable<PackDisplayInfo>? packs, string? message, int errorCount)> _pendingCalls = new();

        /// <summary>Sets the UGUI panel that receives forwarded calls.</summary>
        public void SetTarget(ModMenuPanel panel)
        {
            _target = panel;

            // Forward callbacks from ModPlatform back to UGUI panel
            OnReloadRequested = () => _target?.OnReloadRequested?.Invoke();
            OnPackToggled = (id, enabled) => _target?.OnPackToggled?.Invoke(id, enabled);

            // Flush any pending calls that arrived before SetTarget was called
            FlushPendingCalls();
        }

        /// <inheritdoc />
        public override void SetPacks(IEnumerable<PackDisplayInfo> packs)
        {
            // If target is not yet set, queue the call
            if (_target == null)
            {
                _pendingCalls.Add((isSetPacks: true, packs: packs.ToList(), message: null, errorCount: 0));
                return;
            }

            // Forward to UGUI panel; do NOT call base (no IMGUI state needed)
            _target.SetPacks(packs);
        }

        /// <inheritdoc />
        public override void SetStatus(string message, int errorCount = 0)
        {
            // If target is not yet set, queue the call
            if (_target == null)
            {
                _pendingCalls.Add((isSetPacks: false, packs: null, message: message, errorCount: errorCount));
                return;
            }

            // Forward to UGUI panel; do NOT call base
            _target.SetStatus(message, errorCount);
        }

        /// <summary>
        /// Flushes all pending SetPacks/SetStatus calls to the target.
        /// Called automatically when SetTarget() is invoked.
        /// </summary>
        private void FlushPendingCalls()
        {
            if (_target == null || _pendingCalls.Count == 0) return;

            foreach (var (isSetPacks, packs, message, errorCount) in _pendingCalls)
            {
                if (isSetPacks && packs != null)
                {
                    _target.SetPacks(packs);
                }
                else if (!isSetPacks && message != null)
                {
                    _target.SetStatus(message, errorCount);
                }
            }

            _pendingCalls.Clear();
        }
    }
}
