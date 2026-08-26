#nullable enable
using System;
using System.IO;
using BepInEx.Logging;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.UI;
using UnityEngine;
using UnityEngine.UI;

namespace DINOForge.Runtime
{
    internal partial class RuntimeDriver
    {
        private void CleanupUiInterceptors()
        {
            try
            {
                UiEventInterceptor[] interceptors = Resources.FindObjectsOfTypeAll<UiEventInterceptor>();
                foreach (UiEventInterceptor interceptor in interceptors)
                {
                    if (interceptor == null) continue;
                    _log.LogWarning($"[RuntimeDriver] Destroying stale UiEventInterceptor on '{interceptor.gameObject.name}'.");
                    Destroy(interceptor);
                }

                Button[] buttons = Resources.FindObjectsOfTypeAll<Button>();
                int renamedCount = 0;
                foreach (Button button in buttons)
                {
                    if (button == null) continue;
                    string currentName = button.gameObject.name;
                    int suffixIndex = currentName.IndexOf("_intercepted", StringComparison.Ordinal);
                    if (suffixIndex < 0) continue;

                    button.gameObject.name = currentName.Substring(0, suffixIndex);
                    renamedCount++;
                }

                if (interceptors.Length > 0 || renamedCount > 0)
                {
                    _log.LogInfo($"[RuntimeDriver] Removed {interceptors.Length} interceptor component(s) and restored {renamedCount} button name(s).");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] UiEventInterceptor cleanup failed: {ex}");
            }
        }

        /// <summary>
        /// Activates the IMGUI fallback UI (ModMenuOverlay + ModSettingsPanel + HudIndicator).
        /// Safe to call from Update() as well as Initialize().
        /// No-ops if already activated.
        /// </summary>
        private void ActivateImguiFallback()
        {
            // Guard: only activate once
            if (_modMenuHost != null) return;

            try
            {
                ModMenuOverlay overlay = gameObject.AddComponent<ModMenuOverlay>();
                ModSettingsPanel settingsPanel = gameObject.AddComponent<ModSettingsPanel>();

                // Wire settings panel into mod menu for its inline Settings button
                overlay.SetSettingsPanel(settingsPanel);

                if (_modPlatform != null)
                {
                    _modPlatform.SetUI(overlay, settingsPanel);
                }

                // Wire the active menu host into NativeMenuInjector for the native Mods button
                if (_nativeMenuInjector != null)
                {
                    _nativeMenuInjector.SetModMenuHost(overlay);
                }

                _modMenuHost = overlay;
                _modSettingsHost = settingsPanel;

                _log.LogInfo("[RuntimeDriver] IMGUI fallback — Added ModMenuOverlay + ModSettingsPanel.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[RuntimeDriver] IMGUI fallback ModMenuOverlay setup failed: {ex}");
            }

            try
            {
                _hudIndicator = gameObject.AddComponent<HudIndicator>();
                _hudIndicator.SetModMenu(_modMenuHost);

                if (_modMenuHost != null)
                {
                    _modMenuHost.OnReloadRequested += () => _hudIndicator?.ShowToast("Packs reloaded");
                }

                // Wire HudIndicator so IMGUI counter also receives pack counts on every load/reload.
                if (_modPlatform != null)
                {
                    HudIndicator hud = _hudIndicator;
                    _modPlatform.OnHudCountsChanged = (p, e) => hud.UpdateCounts(p, e);
                }

                _log.LogInfo("[RuntimeDriver] IMGUI fallback — Added HudIndicator.");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] HudIndicator setup failed: {ex}");
            }
        }

        /// <summary>
        /// Wires UGUI DFCanvas to ModPlatform once DFCanvas.Start() has succeeded.
        /// Called the first frame that DFCanvas.IsReady becomes true.
        /// </summary>
        private void WireUguiToModPlatform()
        {
            if (_dfCanvas == null || _modPlatform == null) return;
            if (ReferenceEquals(_modMenuHost, _dfCanvas.ModMenuPanel))
            {
                TryWireNativeMenuInjectorHost();
                return;
            }
            ModPlatform platform = _modPlatform;

            try
            {
                if (_dfCanvas.ModMenuPanel != null)
                {
                    _dfCanvas.ModMenuPanel.OnReloadRequested = () => RequestPackReload("UGUI reload button");
                }

                IModSettingsHost settingsHost = new NoOpSettingsHost();

                if (_dfCanvas.ModMenuPanel == null)
                {
                    throw new InvalidOperationException("DFCanvas did not create ModMenuPanel.");
                }

                platform.SetUI(_dfCanvas.ModMenuPanel, settingsHost);
                _dfCanvas.ModMenuPanel.OnReloadRequested = () => RequestPackReload("UGUI reload button");
                _dfCanvas.ModMenuPanel.OnPackToggled = RequestPackToggle;

                // ── Profiles (#918) ──────────────────────────────────────────
                if (_profileManager != null)
                {
                    RuntimeDriver capturedDriver = this;
                    _dfCanvas.ModMenuPanel.SetProfileManager(_profileManager);
                    _dfCanvas.ModMenuPanel.OnProfileLoaded = enabledPackIds =>
                    {
                        try
                        {
                            // Disable all packs then enable only those in the profile
                            foreach (UI.PackDisplayInfo p in platform.GetLoadedPackDisplayInfos())
                            {
                                bool shouldEnable = false;
                                foreach (string id in enabledPackIds)
                                {
                                    if (string.Equals(id, p.Id, StringComparison.Ordinal))
                                    {
                                        shouldEnable = true;
                                        break;
                                    }
                                }
                                if (p.IsEnabled != shouldEnable)
                                    capturedDriver.RequestPackToggle(p.Id, shouldEnable);
                            }
                        }
                        catch (Exception ex)
                        {
                            _log?.LogWarning($"[RuntimeDriver] OnProfileLoaded failed: {ex.Message}");
                        }
                    };
                    _log.LogInfo("[RuntimeDriver] ProfileManager wired to ModMenuPanel.");
                }

                TryWireNativeMenuInjectorHost();

                // Wire UGUI DebugPanel to ModPlatform so it displays platform status
                if (_dfCanvas.DebugPanel != null && _modPlatform != null)
                {
                    _dfCanvas.DebugPanel.SetModPlatform(platform);
                    _log.LogInfo("[RuntimeDriver] UGUI DebugPanel wired to ModPlatform.");
                }

                _modMenuHost = _dfCanvas.ModMenuPanel;
                _modSettingsHost = settingsHost;

                // Wire HudStrip so it receives pack counts on every load/reload.
                if (_dfCanvas.HudStrip != null)
                {
                    UI.HudStrip hudStrip = _dfCanvas.HudStrip;
                    platform.OnHudCountsChanged = (p, e) => hudStrip.SetStatus(p, e);
                }

                _log.LogInfo("[RuntimeDriver] UGUI wired to ModPlatform via IModMenuHost.");

                _log?.LogInfo($"[RuntimeDriver.diag] ABOUT TO CALL PushLoadedPacksToUgui('late UGUI wiring') — dfCanvas={_dfCanvas != null}, modPlatform={_modPlatform != null}");
                PushLoadedPacksToUgui("late UGUI wiring immediate sync");

                // Fix #31/#32: LoadPacks() may have run before the UI host was wired
                // (ModPlatform.UpdateUI() returns early when _modMenuHost is null).
                // Now that the host is registered, replay a LoadPacks() so ModMenuPanel
                // receives the pack list and DebugPanel receives ModPlatform data.
                // This is a no-op if packs have not been loaded yet.
                if (platform.GetLoadedPackIds() != null)
                {
                    _log?.LogInfo("[RuntimeDriver] Queuing LoadPacks() to populate UGUI panels after late wiring.");
                    RequestPackReload("late UGUI wiring");
                }
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] UGUI→ModPlatform wiring failed, activating IMGUI fallback: {ex}");
                _uguiReady = false;
                ActivateImguiFallback();
            }
        }
    }
}
