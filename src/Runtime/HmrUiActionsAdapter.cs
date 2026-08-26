#nullable enable
using System;
using DINOForge.Runtime.Diagnostics;

namespace DINOForge.Runtime
{
    /// <summary>
    /// Bridges <see cref="HotReload.IHmrUiActions"/> to <see cref="DFCanvas"/> /
    /// <see cref="UI.ModMenuPanel"/>. Called from the HMR background thread;
    /// MonoBehaviour calls are permitted for DontDestroyOnLoad objects in Mono 2021.3
    /// (confirmed by existing F9/F10 background-thread pattern).
    /// </summary>
    internal sealed class HmrUiActionsAdapter : HotReload.IHmrUiActions
    {
        private readonly RuntimeDriver _driver;

        internal HmrUiActionsAdapter(RuntimeDriver driver)
        {
            _driver = driver;
        }

        /// <inheritdoc/>
        public void ShowToast(string message, HotReload.HmrToastKind kind)
        {
            try
            {
                UI.ToastType toastType = kind switch
                {
                    HotReload.HmrToastKind.Warning => UI.ToastType.Warning,
                    HotReload.HmrToastKind.Error => UI.ToastType.Error,
                    _ => UI.ToastType.Info,
                };

                if (_driver._uguiReady && _driver._dfCanvas != null)
                {
                    _driver._dfCanvas.ShowToast(message, toastType);
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[HmrUiActionsAdapter] ShowToast failed: {ex.Message}");
            }
        }

        /// <inheritdoc/>
        public void ShowConfirmDialog(string message, Action onConfirm, Action onCancel)
        {
            try
            {
                UI.ModMenuPanel? panel = _driver._dfCanvas?.ModMenuPanel;
                if (panel != null)
                {
                    panel.ShowConfirmDialog(message, onConfirm, onCancel);
                }
                else
                {
                    // No panel available — auto-cancel so we never silently block.
                    DebugLog.Write("Plugin", "[HmrUiActionsAdapter] ShowConfirmDialog: ModMenuPanel unavailable, auto-cancelling.");
                    onCancel?.Invoke();
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[HmrUiActionsAdapter] ShowConfirmDialog failed: {ex.Message}");
                onCancel?.Invoke();
            }
        }
    }
}
