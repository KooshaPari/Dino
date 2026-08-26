#nullable enable
using System;
using System.IO;
using BepInEx.Logging;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.UI;
using DINOForge.SDK;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DINOForge.Runtime
{
    internal partial class RuntimeDriver
    {
        // ------------------------------------------------------------------ //
        // Engine-UI MainMenu-mode init (deterministic, idempotent, self-healing)
        // ------------------------------------------------------------------ //

        /// <summary>
        /// Loads packs, wires the UGUI mod menu, and attempts native MODS-button injection
        /// WITHOUT requiring an ECS World. DINO only creates ECS worlds when entering gameplay,
        /// so the ECS-gated <see cref="ProcessWorldReadyCoroutine"/> never runs at the main menu —
        /// this is the only path that brings up the engine UI there.
        ///
        /// Idempotent: safe to call repeatedly. <see cref="ModPlatform.LoadPacks"/> is pure YAML
        /// parsing, and <see cref="UI.NativeMenuInjector.TryInjectMenuButton"/> short-circuits when
        /// the MODS button already exists. Every failure is logged (no silent swallow — Pattern
        /// #104/#111) so the cause is visible in the BepInEx console.
        /// </summary>
        /// <param name="reason">Diagnostic tag for the log (e.g. "initialize", "scene-change").</param>
        private void RunMainMenuInit(string reason)
        {
            if (_modPlatform == null)
            {
                _log.LogWarning($"[RuntimeDriver] MainMenu-mode init ({reason}) skipped — _modPlatform is null.");
                return;
            }

            try
            {
                _log.LogInfo($"[RuntimeDriver] MainMenu-mode init ({reason}): calling LoadPacks() (no ECS world required).");
                ContentLoadResult result = _modPlatform.LoadPacks();
                _log.LogInfo($"[RuntimeDriver] MainMenu-mode init ({reason}) pack-load complete: success={result.IsSuccess}, loaded={result.LoadedPacks.Count}, errors={result.Errors.Count}");

                WireUguiToModPlatform();
                PushLoadedPacksToUgui("main-menu init");

                // Hide loading screen now that packs are loaded.
                if (_loadingScreen != null)
                {
                    _loadingScreen.BeginFadeOut();
                    _log.LogInfo("[RuntimeDriver] LoadingScreenController faded out (MainMenu-mode init complete).");
                }

                // Apply total_conversion theme to main menu (best-effort; pump loop retries).
                try
                {
                    _mainMenuThemer = new MainMenuThemer(_log, _modPlatform.PacksDirectory);
                    System.Collections.Generic.IReadOnlyList<PackDisplayInfo> packInfos = _modPlatform.GetLoadedPackDisplayInfos();
                    _mainMenuThemer.TryApplyTheme(packInfos);

                    // Color-skin every non-MainMenu page (Settings + GAME/VIDEO/SOUND/CONTROLS/
                    // TWITCH sub-tabs, game create/select) with the active total_conversion theme.
                    // Sub-panels are created lazily on navigation, so the pump loop re-runs this.
                    _canvasReskinner = new UI.CanvasReskinner(_log, _modPlatform.PacksDirectory);
                    _canvasReskinner.Invalidate();
                    _reskinRetryCount = 0;
                    _canvasReskinner.ReskinAllPages(packInfos);
                }
                catch (Exception themeEx)
                {
                    _log.LogWarning($"[RuntimeDriver] MainMenuThemer failed: {themeEx.Message}");
                }

                // Kick a native injection attempt immediately; the pump loop bounded-retry
                // handles the case where the menu canvas is not ready on this exact frame.
                if (_nativeMenuInjector != null)
                {
                    try { _nativeMenuInjector.TryInjectMenuButton(); }
                    catch (Exception injEx)
                    {
                        _log.LogWarning($"[RuntimeDriver] MainMenu-mode init ({reason}) injection attempt failed: {injEx.Message}");
                    }
                }

                // Emit the single launch-time engine-UI heartbeat (idempotent: only once unless
                // a scene change re-arms it). If injection is still pending the pump-loop retry
                // re-emits with the final state.
                LogEngineUiHeartbeat(reason);
            }
            catch (Exception ex)
            {
                _log.LogError($"[RuntimeDriver] MainMenu-mode init ({reason}) FAILED: {ex}");
            }
        }

        /// <summary>
        /// Self-heal hook: when DINO transitions to a menu scene (no active gameplay ECS world),
        /// re-arm the bounded retry and re-run the idempotent menu-mode init so the engine UI is
        /// rebuilt after returning from gameplay. Never throws to Unity.
        /// </summary>
        private void OnRuntimeDriverSceneChanged(Scene previous, Scene next)
        {
            try
            {
                if (_destroyed) return;
                _log.LogInfo($"[RuntimeDriver] activeSceneChanged: '{previous.name}' → '{next.name}' — re-arming engine-UI menu-mode init.");
                // Re-arm: the scene swap destroyed the previous canvas + injected button, so allow
                // a fresh injection attempt and a fresh heartbeat for the new scene.
                _menuInitRetryFrames = 0;
                _engineUiHeartbeatLogged = false;
                RunMainMenuInit("scene-change");
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[RuntimeDriver] OnRuntimeDriverSceneChanged failed: {ex.Message}");
            }
        }

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

        /// <summary>
        /// Wires the NativeMenuInjector to a ContextualModMenuHost that delegates to the
        /// UGUI overlay when the native menu is active, or directly to the UGUI panel
        /// otherwise. Idempotent and safe to call from either the NativeMenuInjector setup
        /// path or the UGUI wiring path.
        /// </summary>
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
