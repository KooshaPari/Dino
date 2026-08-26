#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using BepInEx.Logging;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.UI;
using DINOForge.SDK;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime
{
    internal partial class RuntimeDriver
    {
        internal void RequestPackReload(string reason)
        {
            lock (_deferredWorkLock)
            {
                _pendingPackReload = true;
                _pendingPackReloadReason = reason;
            }
        }

        private void RequestPackToggle(string packId, bool enabled)
        {
            lock (_deferredWorkLock)
            {
                _pendingPackToggle = true;
                _pendingPackToggleId = packId;
                _pendingPackToggleEnabled = enabled;
            }
        }

        private bool TryDequeuePendingWorldReady(out World? world)
        {
            lock (_deferredWorkLock)
            {
                if (_hasPendingWorldReady && !_worldReadyProcessing && _pendingWorldReady != null)
                {
                    world = _pendingWorldReady;
                    _pendingWorldReady = null;
                    _hasPendingWorldReady = false;
                    _worldReadyProcessing = true;
                    return true;
                }
            }

            world = null;
            return false;
        }

        private bool TryDequeuePendingPackReload(out string? reason)
        {
            lock (_deferredWorkLock)
            {
                if (_pendingPackReload && !_worldReadyProcessing && !_pendingPackToggle)
                {
                    reason = _pendingPackReloadReason ?? "queued";
                    _pendingPackReload = false;
                    _pendingPackReloadReason = null;
                    return true;
                }
            }

            reason = null;
            return false;
        }

        private bool TryDequeuePendingPackToggle(out string? packId, out bool enabled)
        {
            lock (_deferredWorkLock)
            {
                if (_pendingPackToggle && !_worldReadyProcessing)
                {
                    packId = _pendingPackToggleId;
                    enabled = _pendingPackToggleEnabled;
                    _pendingPackToggle = false;
                    _pendingPackToggleId = null;
                    return !string.IsNullOrEmpty(packId);
                }
            }

            packId = null;
            enabled = false;
            return false;
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
                System.Collections.Generic.IReadOnlyList<PackDisplayInfo> packInfos = _modPlatform.GetLoadedPackDisplayInfos();
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
    }
}
