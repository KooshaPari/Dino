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
                    IReadOnlyList<PackDisplayInfo> packInfos = _modPlatform.GetLoadedPackDisplayInfos();
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

        private IEnumerator ProcessWorldReadyCoroutine(World ecsWorld)
        {
            try
            {
                _log.LogInfo($"[RuntimeDriver] ECS World available: {ecsWorld.Name}");
                _registeredWorldInstance = ecsWorld;

                if (_dumpOnStartup)
                {
                    try
                    {
                        DumpSystem.Configure(_log, _dumpOutputPath);
                        ecsWorld.GetOrCreateSystem<DumpSystem>();
                        _log.LogInfo("[RuntimeDriver] DumpSystem registered in default world.");
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning($"[RuntimeDriver] DumpSystem registration failed: {ex}");
                    }
                }

                if (_modPlatform == null)
                {
                    yield break;
                }

                yield return null;

                RunPhaseWithAbortGuard("ModPlatform.OnWorldReady", () =>
                {
                    _modPlatform.OnWorldReady(ecsWorld);
                    _log.LogInfo("[RuntimeDriver] ModPlatform notified of world readiness.");
                });

                WireUguiToModPlatform();

                yield return null;

                ContentLoadResult loadResult = null!;
                bool loadCompleted = false;
                ModPlatform modPlatform = _modPlatform;
                RunPhaseWithAbortGuard("ModPlatform.LoadPacks", () =>
                {
                    loadResult = modPlatform.LoadPacks();
                    loadCompleted = true;
                });

                if (loadCompleted)
                {
                    _log?.LogInfo($"[RuntimeDriver.diag] LoadPacks returned, modPlatformReady={modPlatform != null}, packCount={loadResult.LoadedPacks.Count} — entering UGUI push block");
                    _log?.LogInfo($"[RuntimeDriver] Pack loading complete: success={loadResult.IsSuccess}, " +
                        $"loaded={loadResult.LoadedPacks.Count}, errors={loadResult.Errors.Count}");
                    _log?.LogInfo($"[RuntimeDriver.diag] ABOUT TO CALL PushLoadedPacksToUgui('initial load') — dfCanvas={_dfCanvas != null}, modPlatform={modPlatform != null}");
                    PushLoadedPacksToUgui("initial load");
                    QueueSceneDumpIfRequested(ecsWorld);

                    // Hide the loading screen now that world is ready and packs are loaded
                    if (_loadingScreen != null)
                    {
                        _loadingScreen.BeginFadeOut();
                        _log?.LogInfo("[RuntimeDriver] LoadingScreenController faded out (world ready).");
                    }
                }

                yield return null;

                RunPhaseWithAbortGuard("ModPlatform.StartHotReload", () =>
                {
                    modPlatform?.StartHotReload();
                    _log?.LogInfo("[RuntimeDriver] Hot reload started.");
                });

                yield return null;

                RunPhaseWithAbortGuard("ModSettingsPanel.DiscoverSettings", () =>
                {
                    if (_modSettingsHost is ModSettingsPanel settingsPanel)
                    {
                        settingsPanel.DiscoverSettings();
                        _log?.LogInfo("[RuntimeDriver] Mod settings discovered.");
                    }
                });

                if (_debugOverlay != null)
                {
                    _debugOverlay.SetModPlatform(modPlatform);
                }
            }
            finally
            {
                lock (_deferredWorkLock)
                {
                    _worldReadyProcessing = false;
                }
            }
        }

        private void QueueSceneDumpIfRequested(World ecsWorld)
        {
            if (_sceneDumpQueued)
            {
                return;
            }

            string? dumpPath = Environment.GetEnvironmentVariable("DINO_DUMP");
            if (string.IsNullOrWhiteSpace(dumpPath))
            {
                return;
            }

            _sceneDumpQueued = true;
            _log?.LogInfo($"[RuntimeDriver] Scene dump requested via DINO_DUMP='{dumpPath}'.");

            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    World? bestWorld = ecsWorld;
                    EntityManager bestEm = FindBestEntityManagerForDump(out bestWorld);
                    if (bestWorld == null || !bestWorld.IsCreated)
                    {
                        _log?.LogWarning("[RuntimeDriver] Scene dump skipped: no created ECS world available.");
                        return;
                    }

                    SceneDumper dumper = new SceneDumper(
                        dumpPath,
                        _modPlatform?.PacksDirectory ?? string.Empty,
                        () => _modPlatform?.GetLoadedPackIds(),
                        () => _modPlatform?.GetLoadedPackDisplayInfos());

                    dumper.Dump(bestWorld, bestEm);
                    _log?.LogInfo($"[RuntimeDriver] Scene dump written to '{dumpPath}'.");
                }
                catch (Exception ex)
                {
                    _log?.LogWarning($"[RuntimeDriver] Scene dump failed: {ex.Message}");
                }
            });
        }

        private EntityManager FindBestEntityManagerForDump(out World? bestWorld)
        {
            EntityManager best = default;
            bestWorld = World.DefaultGameObjectInjectionWorld;
            int bestCount = -1;
            string bestName = bestWorld?.Name ?? "";

            try
            {
                foreach (World world in World.All)
                {
                    if (world == null || !world.IsCreated)
                    {
                        continue;
                    }

                    int count;
                    try
                    {
                        count = world.EntityManager.UniversalQuery.CalculateEntityCount();
                    }
                    catch
                    {
                        continue;
                    }

                    if (count > bestCount)
                    {
                        bestCount = count;
                        best = world.EntityManager;
                        bestWorld = world;
                        bestName = world.Name;
                    }
                }
            }
            catch
            {
            }

            if (bestWorld != null && bestCount < 0)
            {
                best = bestWorld.EntityManager;
            }

            _log?.LogInfo($"[RuntimeDriver] Scene dump using world '{bestName}' with entityCount={Math.Max(bestCount, 0)}.");
            return best;
        }

        private IEnumerator ProcessPackReloadCoroutine(string reason)
        {
            if (_modPlatform == null)
            {
                yield break;
            }

            _log.LogInfo($"[RuntimeDriver] Processing deferred pack reload ({reason}).");
            yield return null;

            ContentLoadResult loadResult = null!;
            bool loadCompleted = false;
            RunPhaseWithAbortGuard("ModPlatform.LoadPacks", () =>
            {
                loadResult = _modPlatform.LoadPacks();
                loadCompleted = true;
            });

            if (loadCompleted)
            {
                _log.LogInfo($"[RuntimeDriver] Deferred pack reload complete: success={loadResult.IsSuccess}, " +
                    $"loaded={loadResult.LoadedPacks.Count}, errors={loadResult.Errors.Count}");
                _log?.LogInfo($"[RuntimeDriver.diag] ABOUT TO CALL PushLoadedPacksToUgui('deferred reload') — dfCanvas={_dfCanvas != null}, modPlatform={_modPlatform != null}");
                PushLoadedPacksToUgui("deferred reload");

                // Update header status line and show toast so the user knows reload completed.
                string statusMsg = loadResult.IsSuccess
                    ? $"Reloaded — {loadResult.LoadedPacks.Count} pack(s) loaded"
                    : $"Reload failed — {loadResult.Errors.Count} error(s)";
                _dfCanvas?.ModMenuPanel?.SetStatus(statusMsg, loadResult.Errors.Count);
                ToastType toastType = loadResult.IsSuccess ? ToastType.Info : ToastType.Warning;
                _dfCanvas?.ShowToast(statusMsg, toastType);
            }

            yield return null;
        }

        private IEnumerator ProcessPackToggleCoroutine(string packId, bool enabled)
        {
            if (_modPlatform == null)
            {
                yield break;
            }

            _log.LogInfo($"[RuntimeDriver] Processing deferred pack toggle: {packId} enabled={enabled}.");
            yield return null;

            RunPhaseWithAbortGuard("ModPlatform.SetPackEnabled", () =>
            {
                _modPlatform.SetPackEnabled(packId, enabled);
            });

            yield return ProcessPackReloadCoroutine($"pack toggle {packId}");

            if (_dfCanvas?.ModMenuPanel != null)
            {
                _dfCanvas.ModMenuPanel.SetStatus($"Pack '{packId}' {(enabled ? "enabled" : "disabled")} and reloaded");
            }
        }
    }
}
