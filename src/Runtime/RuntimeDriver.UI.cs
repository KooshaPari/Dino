#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.Localization;
using DINOForge.Runtime.Telemetry;
using DINOForge.Runtime.UI;
using DINOForge.Runtime.Updates;
using DINOForge.SDK;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace DINOForge.Runtime
{
    internal partial class RuntimeDriver
    {
        /// <summary>
        /// Emits a single unambiguous launch-time heartbeat summarising engine-UI readiness so the
        /// user (and tooling) can confirm state at a glance from the BepInEx console / LogOutput.log.
        /// Logged at most once per scene (re-armed on scene change).
        /// </summary>
        private void LogEngineUiHeartbeat(string reason)
        {
            if (_engineUiHeartbeatLogged) return;

            int packs = 0;
            try { packs = _modPlatform?.GetLoadedPackDisplayInfos().Count ?? 0; }
            catch { /* safe-swallow: heartbeat is diagnostic-only and must not throw */ }

            bool modsButton = _nativeMenuInjector != null && _nativeMenuInjector.IsModsButtonInjected;
            bool f9 = _debugOverlay != null || _dfCanvas != null;       // F9 debug panel host present
            bool f10 = _modMenuHost != null || _dfCanvas?.ModMenuPanel != null; // F10 mods panel host present

            // Only mark the heartbeat as "logged" (final) once the MODS button is in OR we were
            // called from the retry path; the first injectionless call may re-emit after retries.
            if (modsButton || string.Equals(reason, "self-heal retry", StringComparison.Ordinal))
            {
                _engineUiHeartbeatLogged = true;
            }

            string readyLine = $"[DINOForge] ENGINE-UI READY: packs={packs} modsButton={modsButton} f9={f9} f10={f10} (via {reason})";
            _log.LogInfo(readyLine);
            // iter-149b: also mirror to dinoforge_debug.log so live verification (which reads the
            // DINOForge debug log, not BepInEx LogOutput.log) can confirm engine-UI readiness.
            DebugLog.Write("Plugin", readyLine);
        }

        private void PushLoadedPacksToUgui(string reason)
        {
            _log?.LogInfo($"[RuntimeDriver] PushLoadedPacksToUgui({reason}) ENTRY: dfCanvas={(_dfCanvas != null ? "OK" : "NULL")}, modPlatform={(_modPlatform != null ? "OK" : "NULL")}, modMenuPanel={(_dfCanvas?.ModMenuPanel != null ? "OK" : "NULL")}, hasLastLoadResult={_modPlatform?.HasLastLoadResult.ToString() ?? "NULL"}, lastLoad={_modPlatform?.DescribeLastLoadResult() ?? "modPlatform=NULL"}");

            if (_dfCanvas == null)
            {
                _log?.LogWarning($"[RuntimeDriver] PushLoadedPacksToUgui({reason}) skipped — _dfCanvas is NULL.");
                return;
            }

            if (_modPlatform == null)
            {
                _log?.LogWarning($"[RuntimeDriver] PushLoadedPacksToUgui({reason}) skipped — _modPlatform is NULL.");
                return;
            }

            if (_dfCanvas.ModMenuPanel == null)
            {
                _log?.LogWarning($"[RuntimeDriver] PushLoadedPacksToUgui({reason}) skipped — ModMenuPanel is NULL.");
                return;
            }

            try
            {
                IReadOnlyList<PackDisplayInfo> packInfos = _modPlatform.GetLoadedPackDisplayInfos();
                _log?.LogInfo($"[RuntimeDriver] PushLoadedPacksToUgui({reason}) resolved packInfos.Count={packInfos.Count}; {_modPlatform.DescribeLastLoadResult()}");
                if (packInfos.Count == 0)
                {
                    _log?.LogWarning($"[RuntimeDriver] PushLoadedPacksToUgui({reason}) resolved 0 packs — registry or load-result path may be empty.");
                }

                _dfCanvas.ModMenuPanel.SetPacks(packInfos);

                ContentLoadResult? lastResult = _modPlatform.GetLastLoadResult();
                if (lastResult != null)
                {
                    int errorCount = lastResult.Errors.Count;
                    string statusMsg = lastResult.IsSuccess
                        ? $"{lastResult.LoadedPacks.Count} packs loaded"
                        : $"{lastResult.LoadedPacks.Count} loaded, {errorCount} error(s)";
                    _dfCanvas.ModMenuPanel.SetStatus(statusMsg, errorCount);
                }

                _log?.LogInfo($"[RuntimeDriver] UGUI mod menu refreshed after {reason}.");
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[RuntimeDriver] Failed to refresh UGUI mod menu after {reason}: {ex}");
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

        private void TryWireNativeMenuInjectorHost()
        {
            if (_nativeMenuInjector == null || _dfCanvas?.ModMenuPanel == null)
            {
                return;
            }

            // Fix #30/#884: UGUI can be wired before NativeMenuInjector is created. Keep this
            // idempotent and call it from both paths so the native MODS button never fires with
            // _menuHost == null.
            NativeMainMenuModMenu nativeHost = new NativeMainMenuModMenu();
            if (_log != null) nativeHost.SetLogger(_log);
            // Fix (iter-149): give the native MODS screen a live pack source. ModPlatform.UpdateUI
            // only pushes packs to the overlay host it owns, not to this contextual host, so the
            // native page would otherwise list zero packs.
            nativeHost.PackDataProvider = () =>
                _modPlatform?.GetLoadedPackDisplayInfos()
                ?? (System.Collections.Generic.IReadOnlyList<PackDisplayInfo>)System.Array.Empty<PackDisplayInfo>();
            ContextualModMenuHost contextualHost = new ContextualModMenuHost(
                _dfCanvas.ModMenuPanel, nativeHost);
            _nativeMenuInjector.SetModMenuHost(contextualHost);
            _log?.LogInfo("[RuntimeDriver] NativeMenuInjector wired via ContextualModMenuHost (native menu active, overlay fallback).");
        }

    }
}
