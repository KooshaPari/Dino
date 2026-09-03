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
        /// Starts a background thread that handles all polling previously done in Update().
        /// MonoBehaviour.Update() NEVER fires in DINO, so we run:
        ///   - F9/F10 key polling via Win32 GetAsyncKeyState (works from background thread)
        ///   - UGUI canvas readiness checks
        ///   - ECS World availability polling
        ///   - VanillaCatalog rebuild once world is fully loaded
        ///   - Heartbeat logging
        ///
        /// Uses UnityEngine.Object.FindObjectsOfType (NOT FindObjectsOfTypeAll) to avoid
        /// deadlock during asset loading in Mono 2021.3.
        /// </summary>
        private void StartBackgroundPollingThread()
        {
            System.Threading.ThreadPool.QueueUserWorkItem(_ =>
            {
                try { System.Threading.Thread.CurrentThread.Name = "DINOForge.BackgroundPoll"; } catch { /* safe-swallow: thread name set is best-effort diagnostics */ }
                try
                {
                    int heartbeatCounter = 0;
                    while (true)
                    {
                        // sync-over-async-unavoidable: background thread control signal (50ms timeout, no deadlock)
                        if (_backgroundPollStopEvent.Wait(50))  // Signaled = destroyed
                            break;

                        // Guard: only run if initialized
                        if (!_initialized) continue;

                        // Heartbeat logging (every 1 sec for first 10, then every 10 sec)
                        heartbeatCounter++;
                        bool earlyHeartbeat = heartbeatCounter <= 200; // ~10 seconds at 50ms interval
                        bool laterHeartbeat = heartbeatCounter % 200 == 0; // Every 10 seconds
                        if (earlyHeartbeat || laterHeartbeat)
                        {
                            _log?.LogDebug($"[RuntimeDriver] Background poll heartbeat #{heartbeatCounter} worldFound={_worldFound}");
                        }

                        // ── Deferred TryResurrect ───────────────────────────────────
                        // If OnSceneLoaded or KeyInputSystem.OnCreate set NeedsDeferredResurrection,
                        // call TryResurrect now. The background thread runs AFTER Plugin.Awake() completes,
                        // so TryResurrect will succeed (Plugin.Awake() sets _resurrectionLog and _resurrectionConfig).
                        if (Plugin.NeedsDeferredResurrection)
                        {
                            Plugin.NeedsDeferredResurrection = false;
                            try
                            {
                                DebugLog.Write("Plugin", "[RuntimeDriver] Background poll: calling TryResurrect (deferred)");
                                Plugin.TryResurrect(Plugin.LastSceneNameForResurrection ?? "unknown", "BackgroundPoll_Deferred");
                            }
                            catch (Exception ex)
                            {
                                DebugLog.Write("Plugin", $"[RuntimeDriver] Deferred TryResurrect failed: {ex.Message}");
                            }
                        }

                        // ── F9/F10 key polling DISABLED ───────────────────────────────
                        // F9/F10 are now handled exclusively by KeyInputSystem ECS callbacks
                        // (OnF9Pressed/OnF10Pressed) which reliably see both physical and
                        // synthetic key presses. GetAsyncKeyState from this background thread
                        // does NOT reliably see synthetic keybd_event from external processes.
                        // Background polling caused double-toggles when both paths were active.
                        //
                        // F10 background thread DEAD CODE (kept for reference):
#pragma warning disable CS0162 // Disabled reference block kept for operator comparison during hotfix validation.
                        if (false) // DISABLED
                        {
                            System.Threading.Thread.Sleep(50); // Debounce
                            if (false)
                            {
                                try
                                {
                                    _log?.LogDebug("[RuntimeDriver] F10 pressed (background thread)");
                                    if (_uguiReady && _dfCanvas != null)
                                    {
                                        _dfCanvas.ToggleModMenu();
                                    }
                                    else if (_modMenuHost != null)
                                    {
                                        _modMenuHost.Toggle();
                                    }
                                }
                                catch (System.Exception ex)
                                {
                                    _log?.LogWarning($"[RuntimeDriver] F10 toggle failed: {ex}");
                                }

                                // Wait for key release (dead code)
                                System.Threading.Thread.Sleep(50);
                            }
                        }
#pragma warning restore CS0162

                        // ── DFCanvas readiness is handled by OnInitSuccess callback ──────────────
                        // No need to poll IsReady from background thread (causes UnityException).
                        // The callback is invoked synchronously from DFCanvas.Initialize() on main thread.

                        // ── ECS World polling ────────────────────────────────────────────
                        if (!_worldFound)
                        {
                            // Bail out if RuntimeDriver was destroyed (e.g., during scene transition).
                            // OnDestroy sets _destroyed=true so the background thread exits cleanly.
                            if (_destroyed) break;

                            _worldPollTimer += 0.05f; // Add 50ms per poll iteration
                            if (_worldPollTimer >= WorldPollInterval)
                            {
                                _worldPollTimer = 0f;
                                try
                                {
                                    World? world = World.DefaultGameObjectInjectionWorld;
                                    if (world != null && world.IsCreated)
                                    {
                                        // Register KeyInputSystem immediately — ECS systems survive scene transitions.
                                        // This ensures the main-thread pump (DrainQueue) is active even during InitialGameLoader.
                                        TryRegisterKeyInputSystem(world);

                                        Scene activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                                        bool isLoaderScene = activeScene.name != null &&
                                            activeScene.name.IndexOf("InitialGameLoader", StringComparison.OrdinalIgnoreCase) >= 0;
                                        if (isLoaderScene)
                                        {
                                            _log?.LogDebug("[RuntimeDriver] ECS world found but at InitialGameLoader — waiting for scene transition.");
                                            continue; // Skip pack loading; NativeMenuInjector will trigger LoadScene(1)
                                        }

                                        _worldFound = true;
                                        OnWorldReady(world);
                                    }
                                }
                                catch
                                {
                                    // World not ready yet, will retry next poll
                                }
                            }
                        }
                        // World found — detect world CHANGES and re-trigger OnWorldReady
                        else
                        {
                            if (_destroyed) break;
                            try
                            {
                                World? w = World.DefaultGameObjectInjectionWorld;
                                if (w != null && w.IsCreated)
                                {
                                    // Detect world change: new world created after scene transition
                                    if (!ReferenceEquals(_registeredWorldInstance, w))
                                    {
                                        _log?.LogInfo($"[RuntimeDriver] World changed: '{w.Name}' (was {(_registeredWorldInstance != null ? _registeredWorldInstance.Name : "null")})");
                                        TryRegisterKeyInputSystem(w);

                                        // Re-trigger OnWorldReady for the new world
                                        _worldFound = false;
                                        _catalogRebuilt = false;
                                        _worldFound = true;
                                        DebugLog.Write("Plugin", $"[RuntimeDriver] World change detected — queueing OnWorldReady for '{w.Name}'");
                                        OnWorldReady(w);
                                    }

                                    // Deferred catalog rebuild: queue to main thread, don't call from BG thread
                                    if (!_catalogRebuilt)
                                    {
                                        int entityCount = w.EntityManager.UniversalQuery.CalculateEntityCount();
                                        if (entityCount > 1000)
                                        {
                                            _catalogRebuilt = true;
                                            _log?.LogInfo($"[RuntimeDriver] Catalog rebuild deferred to main thread ({entityCount} entities)");
                                            lock (_deferredWorkLock)
                                            {
                                                _pendingCatalogWorld = w;
                                            }
                                        }
                                    }
                                }
                                else if (w == null || !w.IsCreated)
                                {
                                    // World was destroyed (scene transition) — reset for next world
                                    if (_worldFound)
                                    {
                                        _worldFound = false;
                                        _catalogRebuilt = false;
                                        DebugLog.Write("Plugin", "[RuntimeDriver] World destroyed — reset worldFound, will re-detect");
                                    }
                                }
                            }
                            catch { } // safe-swallow: ECS world discovery best-effort
                        }
                        // (World-change detection + KeyInputSystem re-registration merged into the else block above)
                    }
                }
                catch (System.Exception ex)
                {
                    _log?.LogError($"[RuntimeDriver] Background polling thread exception: {ex}");
                }
            });
        }
    }
}
