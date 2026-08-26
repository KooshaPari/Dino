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
        private void OnDestroy()
        {
            // Iter-144 #543 fix: set resurrection flags AT THE VERY TOP, before any teardown work.
            // The s_rootJustDestroyed companion flag is the source of truth for "RuntimeDriver died
            // and has not been replaced yet" — the fallback loop checks it via OR with
            // NeedsResurrection to avoid the Unity fake-null trap where PersistentRoot reports
            // == null via operator overload but ReferenceEquals(_, null) is false.
            // s_skipBundleUnload is checked by AssetSwapSystem.OnDestroy to preserve bundles
            // across scene transitions (otherwise chicken-sprite swaps orphan mid-flight).
            Plugin.NeedsResurrection = true;
            Plugin.NeedsDeferredResurrection = true;
            Plugin.s_rootJustDestroyed = true;
            Plugin.s_skipBundleUnload = true;

            // Iter-144 #543 gray-freeze fix: signal all subsystems IMMEDIATELY, before any other
            // teardown work runs. VanillaCatalog.Build + ContentLoader pack registration check
            // this static flag and short-circuit cleanly to avoid racing world teardown.
            s_isBeingDestroyed = true;
            _destroyed = true; // Signal background polling thread to stop
            _backgroundPollStopEvent.Set();  // Wake up the polling loop

            // Pair the activeSceneChanged subscription added in Step 7 (Pattern #105). This
            // instance is being destroyed on the scene transition; the next RuntimeDriver
            // resubscribes its own handler during its Initialize Step 7.
            if (_sceneChangeSubscribed)
            {
                try { SceneManager.activeSceneChanged -= OnRuntimeDriverSceneChanged; }
                catch { /* safe-swallow: unsubscribe is best-effort during teardown */ }
                _sceneChangeSubscribed = false;
            }
            // iter-145 #882 ROOT CAUSE: Removed _resurrectionFallbackStopEvent.Set() — that was killing
            // the fallback thread on every RuntimeDriver.OnDestroy (scene transition), preventing the
            // post-OnDestroy resurrection that's the whole point of the fallback. The "wake without
            // exit" intent at L433 used `if (Wait(...)) break;` which exits unconditionally on signal
            // regardless of _resurrectionFallbackStop. Fallback thread now only exits when Plugin
            // itself unloads (via _resurrectionFallbackStop=true set elsewhere). 500ms poll latency
            // for resurrection is fine; was never real-time-critical.

            // Iter-144 #547 gray-freeze ROOT CAUSE fix: WinDbg analysis revealed the main thread
            // was parked in mono_jit_cleanup → mono_threads_set_shutting_down waiting on the
            // bridge thread stuck in synchronous ConnectNamedPipe. Force-cancel the bridge accept
            // loop NOW (synchronously) before any other teardown work, so the kernel I/O unblocks
            // and mono_jit_cleanup can complete cleanly at process exit. The bridge's
            // RequestShutdown() disposes the current pipe handle, which yields ObjectDisposedException
            // on the BeginWaitForConnection IAsyncResult and lets the accept loop exit.
            // (docs/sessions/iter144-windbg-wedge-stack.md)
            try
            {
                Plugin.SharedBridgeServer?.RequestShutdown();
                DebugLog.Write("Plugin", "[RuntimeDriver] OnDestroy: GameBridgeServer.RequestShutdown() invoked (sync pipe unwedge).");
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[RuntimeDriver] OnDestroy: RequestShutdown failed (non-fatal): {ex.GetType().Name}: {ex.Message}");
            }

            // Iter-144 #547 H5: belt-and-suspenders — the resurrection flags were already set above,
            // but null the field reference explicitly so the next check sees a true managed null
            // (not a Unity fake-null) on subsequent activeSceneChanged callbacks.
            Plugin.PersistentRoot = null;

            // Honest reporting (iter-144 #535 re-fix): the previous "Bridge kept alive" claim was
            // misleading. What actually happens at this point:
            //   - The background polling thread (which runs OnWorldReady, catalog rebuild, world
            //     change detection) STOPS — this RuntimeDriver instance is dead.
            //   - The MainThread pump anchored on this driver's KeyInputSystem also stops servicing
            //     dispatches until TryResurrect attaches a new driver + KeyInputSystem to the new
            //     ECS world.
            //   - The GameBridgeServer thread (IsBackground=false, owned by Plugin.SharedBridgeServer)
            //     DOES survive. Verified at log time below. New requests sit in the pipe queue and
            //     will be serviced once TryResurrect installs a new pump.
            // Resurrection is initiated by SceneManager.activeSceneChanged (iter-144 #546 fix) +
            // a Win32 fallback thread (Plugin.ResurrectionFallbackLoop) + the background-poll deferred path.
            bool bridgeThreadAlive = false;
            try
            {
                Bridge.GameBridgeServer? srv = Plugin.SharedBridgeServer;
                bridgeThreadAlive = srv != null && srv.IsServerThreadAlive;
            }
            catch { } // safe-swallow: diagnostic-only liveness probe must not throw from OnDestroy
            DebugLog.Write("Plugin",
                "[RuntimeDriver] OnDestroy: background poll stopped, main-thread pump idle until resurrection. " +
                $"BridgeServerThreadAlive={bridgeThreadAlive}. NeedsResurrection set; awaiting scene transition.");
            // IMPORTANT: Do NOT call _modPlatform.Shutdown() here.
            // The bridge server runs on its own thread and must survive RuntimeDriver destruction.
            // It will be reattached when TryResurrect creates a new RuntimeDriver.
            // Iter-144 #547 H5: dispatch ShutdownNonBridge to a worker thread so this OnDestroy
            // returns immediately. The dispose work (file watcher + HMR cleanup) is non-essential
            // for resurrection and was previously the suspect for native deadlock. Running it on
            // a background thread releases Unity's destruction pump even if dispose work wedges.
            try
            {
                ModPlatform? mp = _modPlatform;
                if (mp != null)
                {
                    DebugLog.Write("Plugin", "[RuntimeDriver] OnDestroy: dispatching ShutdownNonBridge to worker thread.");
                    Thread shutdownWorker = new Thread(() =>
                    {
                        try
                        {
                            mp.ShutdownNonBridge();
                            DebugLog.Write("Plugin", "[RuntimeDriver] OnDestroy.worker: ShutdownNonBridge completed.");
                        }
                        catch (Exception ex)
                        {
                            DebugLog.Write("Plugin", $"[RuntimeDriver] OnDestroy.worker: ShutdownNonBridge threw {ex.GetType().Name}: {ex.Message}");
                        }
                    })
                    {
                        Name = "DINOForge.ShutdownNonBridge",
                        IsBackground = true,
                    };
                    shutdownWorker.Start();
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[RuntimeDriver] OnDestroy: ShutdownNonBridge dispatch failed: {ex.Message}");
            }
            // #923: Persist metrics snapshot on shutdown (best-effort).
            try
            {
                string snapshotPath = Path.Combine(BepInEx.Paths.BepInExRootPath, "dinoforge-metrics-snapshot.json");
                string metricsJson = MetricsCollector.Instance.DumpJson();
                File.WriteAllText(snapshotPath, metricsJson, System.Text.Encoding.UTF8);
                DebugLog.Write("Plugin", $"[RuntimeDriver] OnDestroy: metrics snapshot written to '{snapshotPath}'.");
            }
            catch (Exception ex)
            {
                // Best-effort: metrics persistence must never throw from OnDestroy
                DebugLog.Write("Plugin", $"[RuntimeDriver] OnDestroy: metrics snapshot failed (non-fatal): {ex.Message}");
            }

            DebugLog.Write("Plugin", "[RuntimeDriver] OnDestroy: returning to Unity (resurrection flags set, fallback thread will revive).");
        }

    }
}
