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
        /// Starts a background thread that monitors for the DINOForge_HotReload signal file.
        /// When the file is detected, invokes reload directly from the background thread.
        ///
        /// CRITICAL: MonoBehaviour.Update() NEVER fires in DINO (scene transitions destroy it).
        /// We invoke reload methods directly from this background thread, using the same pattern
        /// as F9/F10 which work via KeyInputSystem callbacks from background thread input polling.
        ///
        /// Direct thread calls work in Mono 2021.3 on DontDestroyOnLoad objects.
        /// </summary>
        private void StartHmrWatcher()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try { System.Threading.Thread.CurrentThread.Name = "DINOForge.HmrWatcher"; } catch { /* safe-swallow: thread name set is best-effort diagnostics */ }
                try
                {
                    string signalPath = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "DINOForge_HotReload");
                    // #873: cooperative shutdown — observe _destroyed AND wake immediately via stop-event
                    // mirrors StartBackgroundPollingThread (line 920+) pattern.
                    while (!_destroyed)
                    {
                        // Wait returns true when the event is signaled (OnDestroy) → exit promptly.
#pragma warning disable DF0116 // Intentional blocking poll interval on the background watcher thread.
                        if (_backgroundPollStopEvent.Wait(2000))
#pragma warning restore DF0116
                        {
                            break;
                        }
                        if (_destroyed) break;

                        if (System.IO.File.Exists(signalPath))
                        {
                            // Read optional path hint written alongside the signal (first line of file).
                            string changedPath = string.Empty;
                            try
                            {
                                string signalContent = System.IO.File.ReadAllText(signalPath).Trim();
                                changedPath = signalContent;
                            }
                            catch { } // safe-swallow: path hint is optional; empty string → HandleUnknown()

                            try { System.IO.File.Delete(signalPath); } catch { } // safe-swallow: HMR signal file cleanup, non-critical

                            _log?.LogInfo($"[RuntimeDriver] HMR: Signal detected. changedPath='{changedPath}'");

                            // #898: tiered reload — classify changed path and act accordingly.
                            HotReload.HmrTieredReloader? reloader = _hmrTieredReloader;
                            if (reloader != null)
                            {
                                try
                                {
                                    if (string.IsNullOrEmpty(changedPath))
                                        reloader.HandleUnknown();
                                    else
                                        reloader.Handle(changedPath);
                                    _log?.LogInfo("[RuntimeDriver] HMR: Tiered reloader handled signal.");
                                }
                                catch (System.Exception ex)
                                {
                                    _log?.LogWarning($"[RuntimeDriver] HMR: TieredReloader.Handle failed, falling back to flat reload: {ex}");
                                    // Fall through to legacy path below
                                    reloader = null;
                                }
                            }

                            if (reloader == null)
                            {
                                // #891: legacy flat reload path — used when tiered reloader is unavailable.
                                try
                                {
                                    RuntimeDriver? driver = Plugin.PersistentRoot?.GetComponent<RuntimeDriver>();
                                    if (driver != null)
                                    {
                                        driver.RequestPackReload("HMR signal (fallback)");
                                    }
                                    else
                                    {
                                        Bridge.KeyInputSystem.OnPackReloadRequested?.Invoke();
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    _log?.LogWarning($"[RuntimeDriver] HMR: Pack reload enqueue failed: {ex}");
                                }
                            }

                            _log?.LogInfo("[RuntimeDriver] HMR: Signal handling complete.");
                        }
                    }
                    // #873: explicit exit log — proves thread terminated cleanly on OnDestroy.
                    _log?.LogInfo("[RuntimeDriver] HMR watcher thread exiting (destroyed=true)");
                }
                catch { } // safe-swallow: HMR reload best-effort, non-critical
            });
        }
    }
}
