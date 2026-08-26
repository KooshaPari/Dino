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
    public partial class Plugin
    {
        /// <summary>
        /// iter-149e: main-thread revive entry usable by DINO-driven callbacks (KeyInputSystem.OnCreate,
        /// PlayerLoop.SetPlayerLoop postfix) that fire after our own bg threads/scene events have gone
        /// silent. Delegates to <see cref="MainThreadReviveIfNeeded"/>. Caller MUST be on the Unity
        /// main thread (these callbacks are). Never throws (Pattern #104/#111).
        /// </summary>
        internal static void ReviveFromMainThreadCallback(string trigger)
        {
            try { MainThreadReviveIfNeeded(LastSceneNameForResurrection ?? "world-create", trigger); }
            catch (Exception ex)
            {
                try { DebugLog.Write("Plugin", $"[Plugin] ReviveFromMainThreadCallback ({trigger}) threw: {ex.GetType().Name}: {ex.Message}"); }
                catch { /* diagnostic only */ }
            }
        }

        /// <summary>
        /// Iter-144 #543/#546 gray-freeze fix: subscribe to <c>SceneManager.activeSceneChanged</c>
        /// rather than <c>sceneLoaded</c>. Per project_dino_runtime_execution_model.md (confirmed
        /// 2026-03-21), DINO replaces Unity's PlayerLoop entirely; <c>sceneLoaded</c> fires
        /// inconsistently (in-game probe iter-144 confirmed NO sceneLoaded post-OnDestroy) while
        /// <c>activeSceneChanged</c> reliably fires on the main thread for each scene transition.
        ///
        /// Also starts a Win32 background polling thread that calls TryResurrect on a grace-window
        /// timer in case NO scene-change event fires within the window (defense-in-depth, survives
        /// RuntimeDriver destruction since it lives on the Plugin class, not the MonoBehaviour).
        /// </summary>
        private static void StartResurrectionWatcher()
        {
            SceneManager.activeSceneChanged += OnActiveSceneChanged;
            // Blocker 2 keystone fix (iter-149b, 2026-05-29): also subscribe to sceneLoaded.
            // Live log evidence (dinoforge_debug.log, all relaunches) shows activeSceneChanged
            // fires ONLY for '' and 'InitialGameLoader' — it NEVER fired for MainMenu. DINO loads
            // MainMenu ADDITIVELY (LoadSceneMode.Additive) or via an async path that does NOT change
            // the ACTIVE scene, so activeSceneChanged is silent for it while sceneLoaded DOES fire
            // for additive loads. With both the bg fallback wedged (Blocker 1) and no MainMenu
            // activeSceneChanged, resurrection never ran on a main thread. sceneLoaded is the missing
            // main-thread hook for the MainMenu activation. Both events run on the Unity main thread,
            // so resurrection (Unity ECalls) is safe in either handler.
            SceneManager.sceneLoaded += OnSceneLoaded;
            DebugLog.Write("Plugin", "[Plugin] activeSceneChanged + sceneLoaded watchers registered (iter-149b Blocker 2 fix).");
            StartResurrectionFallbackThread();
            // iter-149d BISECT (2026-05-29): PipeKeepAlive is the suspected NEW un-interruptible
            // waiter behind the recurring gray-freeze. PipeKeepAliveLoop polls EnsureServerAlive()
            // every 1s on a BACKGROUND thread; EnsureServerAlive does a pipe Stop()->Start() whenever
            // the bridge server thread is dead — which is ALWAYS the case immediately after
            // RuntimeDriver.OnDestroy calls RequestShutdown(). So during teardown OnDestroy disposes
            // the pipe handle (iter-144 fix) to unwedge the accept thread, but PipeKeepAlive instantly
            // re-creates a NamedPipeServerStream + re-arms BeginWaitForConnection — re-establishing
            // exactly the kernel ConnectNamedPipe wait that RequestShutdown just tore down. That
            // re-armed wait becomes the un-interruptible waiter that wedges mono_jit_cleanup during
            // World.Dispose. Gated OFF to isolate. The pipe must stay DOWN through teardown and only
            // be rebuilt by a clean main-thread resurrection (PlayerLoop/sceneLoaded path).
            const bool EnablePipeKeepAlive = false;
#pragma warning disable CS0162 // Unreachable code — intentional bisect gate (iter-149d).
            if (EnablePipeKeepAlive)
            {
                StartPipeKeepAliveThread();
            }
            else
            {
                DebugLog.Write("Plugin", "[Plugin] PipeKeepAlive thread DISABLED (iter-149d bisect: suspected re-arm of ConnectNamedPipe wedge during World.Dispose).");
            }
#pragma warning restore CS0162
        }

        // Blocker 2 keystone fix (iter-149b): sceneLoaded fires for additive scene loads where
        // activeSceneChanged stays silent (confirmed: MainMenu emitted NO activeSceneChanged).
        // Runs on the Unity main thread, so it is a safe place to perform resurrection. We log the
        // scene name + buildIndex + load mode on EVERY scene event so DINO's actual MainMenu emission
        // is observable, then drive the same main-thread revive path as OnActiveSceneChanged.
        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            DebugLog.Write("Plugin", $"[Plugin] OnSceneLoaded: name='{scene.name}' buildIndex={scene.buildIndex} mode={mode} isLoaded={scene.isLoaded}");

            // Always remember the latest scene name so a fallback revive has a meaningful label.
            if (!string.IsNullOrEmpty(scene.name))
            {
                LastSceneNameForResurrection = scene.name;
            }

            // iter-149e ROOT-CAUSE fix: REVIVE FIRST. Previously EnsureEventSystemAlive() (a heavy
            // FindObjectsOfType ECall) + RecreateInCurrentWorld() ran BEFORE the revive; if either
            // wedged or threw during the MainMenu additive asset load, the revive never executed and
            // the plugin stayed dormant (the MDMP symptom). The revive is the load-bearing action —
            // run it on the main thread first, then do the (now best-effort) EventSystem/world fixups.
            MainThreadReviveIfNeeded(scene.name, "sceneLoaded(main-thread)");

            try { EnsureEventSystemAlive(); }
            catch (Exception ex) { DebugLog.Write("Plugin", $"[Plugin] OnSceneLoaded EnsureEventSystemAlive failed (non-fatal): {ex.Message}"); }
            try { Bridge.KeyInputSystem.RecreateInCurrentWorld(); }
            catch (Exception ex) { DebugLog.Write("Plugin", $"[Plugin] OnSceneLoaded RecreateInCurrentWorld failed (non-fatal): {ex.Message}"); }

            // EPIC-027: DINO loads MainMenu ADDITIVELY — activeSceneChanged stays silent for it, so
            // the MainMenu fade-out must also fire from sceneLoaded (the missing main-thread hook).
            try
            {
                if (scene.name == "MainMenu")
                    UI.LoadingScreenController.Instance?.BeginFadeOut();
            }
            catch (Exception ex) { DebugLog.Write("Plugin", $"[Plugin] OnSceneLoaded LoadingScreen fade failed (non-fatal): {ex.Message}"); }
        }

        /// <summary>
        /// Blocker 2 fix (iter-149b): shared main-thread revive entry point used by BOTH
        /// activeSceneChanged and sceneLoaded. Performs the actual resurrection on the Unity main
        /// thread (where Camera.main / AddComponent / Initialize ECalls are safe), then clears the
        /// need flags only on confirmed success. The bg fallback thread only MARKS the need; this is
        /// where the revive actually executes. Never throws to the Unity caller (Pattern #104/#111).
        /// </summary>
        private static void MainThreadReviveIfNeeded(string sceneName, string trigger)
        {
            // iter-149e: engine-driven heartbeat — this runs on the Unity main thread for EVERY
            // scene event, so it is a reliable liveness pulse that survives plugin-log freezes.
            BumpEngineHeartbeat(trigger);

            bool rootIsRefNull = ReferenceEquals(PersistentRoot, null);
            bool needsRevive = NeedsResurrection || NeedsDeferredResurrection || s_rootJustDestroyed || rootIsRefNull || PersistentRoot == null;
            if (!needsRevive)
            {
                return;
            }

            // iter-149e ROOT-CAUSE fix: a NEW scene event is a fresh opportunity to revive. During
            // InitialGameLoader (no Camera, no MainMenu) the create-root path can burn through
            // MaxResurrectionAttempts (3) and PERMANENTLY halt (IsResurrectionCapExhausted latches
            // true forever). When MainMenu later loads with a valid Camera, resurrection would stay
            // capped-out and never fire — the dormant-plugin symptom. Reset the consecutive-attempt
            // counter on each main-thread scene event so a loader-phase exhaustion never poisons the
            // MainMenu revive. The cap still bounds churn WITHIN a single scene's tick window.
            _resurrectionAttempts = 0;

            LastSceneNameForResurrection = string.IsNullOrEmpty(sceneName) ? LastSceneNameForResurrection : sceneName;
            NeedsDeferredResurrection = true;
            DebugLog.Write("Plugin", $"[Plugin] MainThreadReviveIfNeeded ({trigger}): revive needed (NeedsRes={NeedsResurrection} NeedsDefRes={NeedsDeferredResurrection} rootJustDestroyed={s_rootJustDestroyed} refNull={rootIsRefNull}) — invoking TryResurrect.");
            try
            {
                TryResurrect(LastSceneNameForResurrection ?? sceneName ?? "main-thread-unknown", trigger);
                if (ResurrectionSucceeded())
                {
                    NeedsResurrection = false;
                    NeedsDeferredResurrection = false;
                    s_rootJustDestroyed = false;
                    s_skipBundleUnload = false;
                    ResetGraceDeadline();
                    DebugLog.Write("Plugin", $"[Plugin] Resurrection complete via {trigger} (driver live; flags cleared).");
                }
                else
                {
                    DebugLog.Write("Plugin", $"[Plugin] {trigger} TryResurrect did not bring driver live — retained for next main-thread tick.");
                }
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[Plugin] {trigger} TryResurrect threw: {ex.GetType().Name}: {ex.Message} — retained for next main-thread tick.");
            }
        }

        // Blocker 1 fix (iter-149b): dedicated pipe-keepalive thread. Pipe Stop()/Start() may block
        // (kernel-mode pipe teardown during asset loads) — running it here keeps that blocking work
        // OFF the resurrection fallback thread, so resurrection heartbeats keep ticking regardless of
        // pipe I/O latency. Polls every 1s; restart is idempotent (EnsureServerAlive no-ops when alive).
        private static Thread? _pipeKeepAliveThread;
        internal static readonly ManualResetEventSlim _pipeKeepAliveStopEvent = new(false);

        private static void StartPipeKeepAliveThread()
        {
            if (_pipeKeepAliveThread != null) return;
            _pipeKeepAliveThread = new Thread(PipeKeepAliveLoop)
            {
                Name = "DINOForge.PipeKeepAlive",
                IsBackground = true,
            };
            _pipeKeepAliveThread.Start();
            DebugLog.Write("Plugin", "[Plugin] Pipe-keepalive thread started (Blocker 1: pipe I/O off the resurrection heartbeat).");
        }

        private static void PipeKeepAliveLoop()
        {
            const int PipePollIntervalMs = 1000;
            DebugLog.Write("Plugin", "[Plugin] PipeKeepAlive: loop entered.");
            while (!_resurrectionFallbackStop)
            {
                try
                {
#pragma warning disable DF0116 // Intentional cooperative-stop blocking wait on the pipe-keepalive thread.
                    if (_pipeKeepAliveStopEvent.Wait(PipePollIntervalMs)) break;
#pragma warning restore DF0116
                    // This MAY block on a dead-pipe Stop()->Start(); that is acceptable here because
                    // it does not run on the resurrection heartbeat thread or the Unity main thread.
                    SharedBridgeServer?.EnsureServerAlive();
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DebugLog.Write("Plugin", $"[Plugin] PipeKeepAlive EnsureServerAlive: {ex.Message}");
                }
            }
            DebugLog.Write("Plugin", "[Plugin] PipeKeepAlive thread exiting.");
        }

        private static void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            DebugLog.Write("Plugin", $"[Plugin] OnActiveSceneChanged: old='{oldScene.name}' new='{newScene.name}'");

            // iter-149e ROOT-CAUSE fix: REVIVE FIRST (mirrors OnSceneLoaded). The revive is the
            // load-bearing action and must not be gated behind the heavy EventSystem/world fixups
            // that can wedge during an asset load. activeSceneChanged fires on the Unity main thread.
            if (!string.IsNullOrEmpty(newScene.name))
            {
                LastSceneNameForResurrection = newScene.name;
            }
            MainThreadReviveIfNeeded(newScene.name, "activeSceneChanged(main-thread)");

            // Iter-144 menu-unclickable fix: DINO's MainMenu scene EventSystem is destroyed on
            // scene transitions, leaving EventSystem.current = null even though our
            // DontDestroyOnLoad'd EventSystem (DFCanvas) still exists. Re-promote (or recreate)
            // on every scene change so NativeMenuInjector clicks route correctly.
            try { EnsureEventSystemAlive(); }
            catch (Exception ex) { DebugLog.Write("Plugin", $"[Plugin] OnActiveSceneChanged EnsureEventSystemAlive failed (non-fatal): {ex.Message}"); }
            try { Bridge.KeyInputSystem.RecreateInCurrentWorld(); }
            catch (Exception ex) { DebugLog.Write("Plugin", $"[Plugin] OnActiveSceneChanged RecreateInCurrentWorld failed (non-fatal): {ex.Message}"); }

            // Re-arm the MainMenuThemer retry loop when the MainMenu scene becomes active.
            // The canvas takes ~37s to appear, far beyond the original 30-frame window.
            // Resetting here ensures the pump loop retries from frame 0 for each MainMenu entry.
            if (newScene.name != null && newScene.name.IndexOf("MainMenu", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                RuntimeDriver? driver = PersistentRoot?.GetComponent<RuntimeDriver>();
                if (driver != null)
                {
                    driver.RearmThemer();
                }
                DebugLog.Write("Plugin", "[Plugin] OnActiveSceneChanged: MainMenu detected — MainMenuThemer re-armed");
            }

            // EPIC-027 loading-screen takeover: show during the game's own asset-load window
            // (InitialGameLoader / first scene), hide once the MainMenu is active.
            try
            {
                var ls = UI.LoadingScreenController.Instance;
                if (ls != null)
                {
                    // EPIC-027 fix: check MainMenu FIRST so a cold-start transition (oldScene=""→MainMenu)
                    // triggers BeginFadeOut, not EnsureVisible. The old order hit the "empty oldScene"
                    // branch first, calling EnsureVisible on the very transition where we need to fade out.
                    if (newScene.name == "MainMenu")
                        ls.BeginFadeOut();
                    else if (newScene.name == "InitialGameLoader" || string.IsNullOrEmpty(oldScene.name))
                        ls.EnsureVisible();
                }
            }
            catch (Exception ex) { DebugLog.Write("Plugin", $"[Plugin] OnActiveSceneChanged LoadingScreen toggle failed (non-fatal): {ex.Message}"); }
            // Revive already executed at the TOP of this handler (iter-149e reorder).
        }

        // Iter-144 #546 fallback: Win32 background thread independent of any MonoBehaviour.
        // Survives RuntimeDriver destruction (the MB-owned background poll thread dies with its host).
        // Polls NeedsResurrection every 500ms; if set and no scene event has cleared it within the
        // grace window, attempts TryResurrect directly. Plugin class is referenced as long as the
        // BepInEx assembly is loaded, so this thread persists across scene transitions.
        private static Thread? _resurrectionFallbackThread;
#pragma warning disable CS0649 // Intentional shared shutdown flag for the fallback thread; set via runtime teardown path.
        private static volatile bool _resurrectionFallbackStop;
#pragma warning restore CS0649
        // P2 #879 Pattern #113 fix: ManualResetEventSlim allows the fallback loop to wake
        // immediately on shutdown instead of waiting out a full 500ms Thread.Sleep tick.
        // Mirrors _backgroundPollStopEvent pattern (#873).
        internal static readonly ManualResetEventSlim _resurrectionFallbackStopEvent = new(false);

        // FailureMode B fix (iter-149, 2026-05-29): the grace deadline MUST persist across loop
        // restarts. Previously `lastNeedsObservedUtc` was a LOCAL inside ResurrectionFallbackLoop;
        // when the loop re-entered (a new thread start, or a fresh "loop entered" after the prior
        // instance exited), the timer reset to MinValue and the 4000ms grace window NEVER elapsed,
        // so TryResurrect was detected every cycle but never executed. Latching the deadline as a
        // STATIC field means any loop iteration — even a brand-new one — honors the in-progress
        // grace window set by a prior iteration. DateTime.MinValue = "not armed".
        // Sentinel: DateTime.MinValue means no grace window is currently armed.
        private static DateTime _graceDeadlineUtc = DateTime.MinValue;
        private static readonly object _graceDeadlineLock = new();

        private static void StartResurrectionFallbackThread()
        {
            if (_resurrectionFallbackThread != null) return;
            _resurrectionFallbackThread = new Thread(ResurrectionFallbackLoop)
            {
                Name = "DINOForge.ResurrectionFallback",
                IsBackground = true,
            };
            _resurrectionFallbackThread.Start();
            DebugLog.Write("Plugin", "[Plugin] Resurrection fallback thread started.");
        }

        private static void ResurrectionFallbackLoop()
        {
            long iterationCount = 0;
            const int PollIntervalMs = 500;
            const int GraceWindowMs = 4000; // 4s after NeedsResurrection observed, attempt direct revive
            // Iter-144 #547 H6: 4-iter (2s) heartbeat — frequent enough to distinguish "Mono wedged"
            // from "no scene events firing yet" in the post-OnDestroy gray-freeze window. Previous 10s
            // cadence left ambiguous gaps where probe timing missed the window entirely.
            const int HeartbeatEveryNIterations = 4;
            DebugLog.Write("Plugin", "[Plugin] ResurrectionFallback: loop entered.");
            while (!_resurrectionFallbackStop)
            {
                try
                {
                    // P2 #879 Pattern #113 fix: cancellation-aware wait instead of Thread.Sleep.
#pragma warning disable DF0116 // Intentional blocking wait on the fallback thread's cooperative stop event.
                    if (_resurrectionFallbackStopEvent.Wait(PollIntervalMs)) break;
#pragma warning restore DF0116
                    iterationCount++;

                    // iter-149e: bump the engine heartbeat file from the FALLBACK thread too. This is
                    // a separate file from the debug log, so if the heartbeat counter keeps advancing
                    // with source "fallback" while the plugin LOG is frozen, the bg thread is ALIVE and
                    // the freeze is purely a log-write contention — vs the counter freezing too, which
                    // proves the bg thread itself is suspended/dead (the WinDbg dormant-plugin case).
                    BumpEngineHeartbeat("fallback#" + iterationCount);

                    // Blocker 1 fix (iter-149b, 2026-05-29): DO NOT call EnsureServerAlive() here.
                    // EnsureServerAlive performs a pipe Stop()->Start() (NamedPipeServerStream dispose +
                    // fresh server thread) whenever BridgeServerThreadAlive=False — which is ALWAYS the
                    // case right after RuntimeDriver.OnDestroy's RequestShutdown(). That pipe
                    // teardown/recreate BLOCKS this background thread during the
                    // InitialGameLoader->MainMenu asset-load window. Confirmed in dinoforge_debug.log:
                    // heartbeats #4..#20 tick cleanly until OnDestroy, then heartbeat #24 NEVER appears
                    // (the loop wedged on the pipe restart), so the grace-windowed revive is never
                    // reached. The deadlock did not disappear when TryResurrect was removed from this
                    // loop in 6be0f5e3 — it MOVED to the pipe restart on this same background thread.
                    //
                    // The fallback loop's PRIMARY job is the grace-windowed revive heartbeat; pipe I/O
                    // must NEVER starve it. Pipe keepalive is now owned by:
                    //   (a) DINOForgePlayerLoopUpdate (main thread, %60 gate) -> EnsureServerAlive(), and
                    //   (b) a dedicated background pipe-keepalive thread (PipeKeepAliveLoop) which may
                    //       block freely on Stop()/Start() without affecting resurrection heartbeats.
                    // This loop now performs pure managed work only (flag checks + grace timer + MARK).

                    // Iter-144 #547 H5: emit periodic heartbeat to prove Mono runtime + this thread are alive.
                    // If the gray-freeze is a native deadlock at runtime level, heartbeats stop appearing
                    // immediately after OnDestroy. If they keep appearing, the hang is elsewhere.
                    if (iterationCount % HeartbeatEveryNIterations == 0)
                    {
                        DebugLog.Write("Plugin", $"[Plugin] ResurrectionFallback heartbeat #{iterationCount} NeedsRes={NeedsResurrection} NeedsDefRes={NeedsDeferredResurrection} rootNull={PersistentRoot == null}");
                    }
                    // Iter-144 #543 fix: OR in s_rootJustDestroyed flag — when RuntimeDriver.OnDestroy
                    // fires, PersistentRoot may hold a destroyed-but-not-nulled Unity fake-null reference,
                    // and NeedsResurrection could be cleared by a stale scene event before the new
                    // driver attaches. The companion flag is the source of truth for "RuntimeDriver
                    // died and has not been replaced yet."
                    bool needsRevive = NeedsResurrection || NeedsDeferredResurrection || s_rootJustDestroyed;
                    if (!needsRevive)
                    {
                        // No need observed: disarm the (static) grace window so a future need starts fresh.
                        ResetGraceDeadline();
                        continue;
                    }

                    // FailureMode B fix: latch the grace DEADLINE in a STATIC field so a loop restart
                    // RESUMES the in-progress window instead of resetting it. Returns true only once
                    // the latched deadline has elapsed; until then we keep polling.
                    if (!IsGraceWindowElapsed(GraceWindowMs, out bool justArmed))
                    {
                        if (justArmed)
                        {
                            DebugLog.Write("Plugin", $"[Plugin] ResurrectionFallback: NeedsResurrection observed, grace deadline armed ({GraceWindowMs}ms).");
                        }
                        continue;
                    }

                    // Blocker 2 fix (iter-149b, 2026-05-29): the grace window has elapsed without a
                    // main-thread scene event resolving the revive. This BACKGROUND thread MUST NOT
                    // call TryResurrect directly — TryResurrect reaches Unity ECalls (Camera.main /
                    // AddComponent / RuntimeDriver.Initialize -> coroutine touching Resources/asset
                    // APIs) which DEADLOCK on a bg thread during the InitialGameLoader->MainMenu asset
                    // load (memory: "Resources.* from a bg thread DEADLOCKS during asset loading").
                    // The bg path's ONLY job is to keep the need MARKED so a main-thread consumer
                    // (DINOForgePlayerLoopUpdate or a scene event) performs the actual revive on a
                    // thread where Unity ECalls are safe. We re-arm and keep heart-beating so the need
                    // never silently drops, and so the heartbeat proves the loop is no longer wedged.
                    // iter-149e ROOT-CAUSE fix (WinDbg MDMP): the previous code called
                    // ResurrectionSucceeded() HERE — which performs a Unity ECall
                    // (PersistentRoot.GetComponent<RuntimeDriver>()) on THIS BACKGROUND THREAD.
                    // During the InitialGameLoader->MainMenu asset load, Unity ECalls from a bg
                    // thread wedge/tear the calling thread (memory: "Resources.* from a bg thread
                    // DEADLOCKS during asset loading"; GetComponent is in the same ECall family).
                    // The MDMP showed this fallback thread GONE post-OnDestroy with NO managed frame
                    // and NO stop-flag ever set — i.e. it was torn inside the ECall, never reaching
                    // heartbeat #12. The bg loop MUST do PURE managed work only: mark the need and
                    // re-arm. The actual revive (and the GetComponent liveness probe) happens ONLY on
                    // the Unity main thread via OnSceneLoaded/OnActiveSceneChanged -> MainThreadReviveIfNeeded.
                    NeedsDeferredResurrection = true;
                    RearmGraceDeadline(GraceWindowMs);
                    DebugLog.Write("Plugin", $"[Plugin] ResurrectionFallback: grace window {GraceWindowMs}ms elapsed — MARKED NeedsDeferredResurrection for main-thread revive (NO bg-thread Unity ECalls — iter-149e). scene='{LastSceneNameForResurrection ?? "fallback-unknown"}'.");
                }
                catch (ThreadAbortException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    DebugLog.Write("Plugin", $"[Plugin] ResurrectionFallback loop error: {ex.Message}");
                }
            }
            DebugLog.Write("Plugin", "[Plugin] Resurrection fallback thread exiting.");
        }

        /// <summary>
        /// FailureMode B helper: returns true once the latched (static) grace deadline has elapsed.
        /// If no deadline is armed, arms one (now + graceWindowMs) and returns false with
        /// <paramref name="justArmed"/>=true. Survives loop restarts because the deadline is static.
        /// </summary>
        private static bool IsGraceWindowElapsed(int graceWindowMs, out bool justArmed)
        {
            justArmed = false;
            lock (_graceDeadlineLock)
            {
                if (_graceDeadlineUtc == DateTime.MinValue)
                {
                    _graceDeadlineUtc = DateTime.UtcNow.AddMilliseconds(graceWindowMs);
                    justArmed = true;
                    return false;
                }
                return DateTime.UtcNow >= _graceDeadlineUtc;
            }
        }

        /// <summary>Re-arms the static grace deadline to now + graceWindowMs (back-off after a failed/partial revive).</summary>
        private static void RearmGraceDeadline(int graceWindowMs)
        {
            lock (_graceDeadlineLock)
            {
                _graceDeadlineUtc = DateTime.UtcNow.AddMilliseconds(graceWindowMs);
            }
        }

        /// <summary>Disarms the static grace deadline (no resurrection currently needed, or revive succeeded).</summary>
        private static void ResetGraceDeadline()
        {
            lock (_graceDeadlineLock)
            {
                _graceDeadlineUtc = DateTime.MinValue;
            }
        }

        /// <summary>
        /// FailureMode B helper: true only when the resurrection actually brought a live, initialized
        /// RuntimeDriver online. Used to decide whether to clear NeedsResurrection (only on success)
        /// versus retain the need + re-arm for another attempt. Never throws to the caller.
        /// </summary>
        private static bool ResurrectionSucceeded()
        {
            try
            {
                if (ReferenceEquals(PersistentRoot, null)) return false;
                RuntimeDriver? driver = PersistentRoot!.GetComponent<RuntimeDriver>();
                return driver != null && driver.IsInitialized;
            }
            catch (Exception ex)
            {
                // Pattern #104/#111: surface, do not silently swallow.
                try { DebugLog.Write("Plugin", $"[Plugin] ResurrectionSucceeded check threw: {ex.GetType().Name}: {ex.Message}"); } catch { /* diagnostic only */ }
                return false;
            }
        }

        /// <summary>
        /// Marks that TryResurrect should be called from the background polling thread.
        /// Called by KeyInputSystem.OnCreate when the ECS world is created during scene transition.
        /// The background thread will call TryResurrect after Plugin.Awake() has completed,
        /// ensuring resurrection parameters are available.
        /// </summary>
        internal static void MarkNeedsDeferredResurrection(string trigger)
        {
            if (NeedsDeferredResurrection) return; // Already set
            DebugLog.Write("Plugin", $"[Plugin] MarkNeedsDeferredResurrection via {trigger}");
            NeedsDeferredResurrection = true;
        }

        internal static void TryResurrect(string sceneName, string trigger)
        {
            if (ReferenceEquals(PersistentRoot, null))
            {
                if (IsResurrectionCapExhausted())
                    return;

                _resurrectionAttempts++;
                try
                {
                    TryResurrectCreateRoot(sceneName, trigger);
                }
                catch (Exception)
                {
                    PersistentRoot = null;
                }
                return;
            }

            TryResurrectWhenRootAlive(trigger);
        }

        /// <summary>SPEC-004 KIS-NF4: pure C# cap gate — no Unity ECalls (unit-test safe).</summary>
        private static bool IsResurrectionCapExhausted()
        {
            if (_resurrectionAttempts < MaxResurrectionAttempts)
                return false;

            if (_resurrectionAttempts == MaxResurrectionAttempts)
            {
                try
                {
                    DebugLog.Write("Plugin", $"[Plugin] TryResurrect: giving up after {MaxResurrectionAttempts} consecutive failures — resurrection loop halted.");
                }
                catch { } // safe-swallow: diagnostic only; must not escape outside Unity player
                _resurrectionAttempts++;
            }

            return true;
        }

        private static void TryResurrectWhenRootAlive(string trigger)
        {
            try
            {
                _resurrectionAttempts = 0;
                NeedsResurrection = false;
                // Check if RuntimeDriver component exists and is initialized
                RuntimeDriver? existing = PersistentRoot!.GetComponent<RuntimeDriver>();
                if (existing != null && existing.IsInitialized)
                {
                    DebugLog.Write("Plugin", $"[Plugin] TryResurrect ({trigger}): RuntimeDriver already running, ensuring KeyInputSystem is registered...");
                    // CRITICAL: Always ensure KeyInputSystem is registered in the current world,
                    // even if RuntimeDriver is already initialized. Scene transitions may have
                    // created a new world that KeyInputSystem needs to be registered in.
                    Bridge.KeyInputSystem.RecreateInCurrentWorld();
                    return;
                }
                // RuntimeDriver exists but wasn't initialized — initialize it
                if (existing != null)
                {
                    DebugLog.Write("Plugin", $"[Plugin] TryResurrect ({trigger}): RuntimeDriver exists but not initialized, initializing...");
                    existing.Initialize(_resurrectionLog!, _resurrectionConfig!, _resurrectionDump, _resurrectionDumpPath);
                    return;
                }
                // No RuntimeDriver component — create one
                DebugLog.Write("Plugin", $"[Plugin] TryResurrect ({trigger}): PersistentRoot exists but no RuntimeDriver, adding component...");
                RuntimeDriver driver = PersistentRoot!.AddComponent<RuntimeDriver>();
                driver.Initialize(_resurrectionLog!, _resurrectionConfig!, _resurrectionDump, _resurrectionDumpPath);
            }
            catch (Exception ex)
            {
                try
                {
                    DebugLog.Write("Plugin", $"[Plugin] TryResurrectWhenRootAlive FAILED ({trigger}): {ex.Message}");
                }
                catch { } // safe-swallow: diagnostic only
            }
        }

        private static void TryResurrectCreateRoot(string sceneName, string trigger)
        {
            try
            {
                DebugLog.Write("Plugin", $"[Plugin] TryResurrect attempt {_resurrectionAttempts}/{MaxResurrectionAttempts} via {trigger} on '{sceneName}' — resurrecting...");
                // Try to attach RuntimeDriver to DINO's main camera — DINO never destroys its own camera
                Camera? cam = Camera.main ?? (Camera.allCameras.Length > 0 ? Camera.allCameras[0] : null);
                GameObject host;
                if (cam != null)
                {
                    host = cam.gameObject;
                    DebugLog.Write("Plugin", $"[Plugin] Attaching to existing camera '{host.name}'");
                }
                else
                {
                    // Fallback: create our own object
                    host = new GameObject("DINOForge_Root");
                    host.hideFlags = HideFlags.HideAndDontSave;
                    UnityEngine.Object.DontDestroyOnLoad(host);
                    DebugLog.Write("Plugin", $"[Plugin] No camera found, using new GameObject");
                }
                PersistentRoot = host;

                RuntimeDriver driver = host.AddComponent<RuntimeDriver>();
                driver.Initialize(_resurrectionLog!, _resurrectionConfig!, _resurrectionDump, _resurrectionDumpPath);

                // Immediately register KeyInputSystem in the current ECS world.
                // The polling thread will also do this, but scene transitions may have already
                // created a new DefaultGameObjectInjectionWorld that the thread hasn't caught yet.
                // This call bridges the gap so the pump is active without waiting for a poll cycle.
                Bridge.KeyInputSystem.RecreateInCurrentWorld();
                _resurrectionAttempts = 0;
                NeedsResurrection = false;
                DebugLog.Write("Plugin", $"[Plugin] Resurrection complete via {trigger} on '{sceneName}' host='{host.name}'.");
            }
            catch (Exception ex)
            {
                PersistentRoot = null;
                try
                {
                    DebugLog.Write("Plugin", $"[Plugin] Resurrection FAILED via {trigger}: {ex.Message}");
                }
                catch { } // safe-swallow: diagnostic only; must not escape and break KIS-NF4 cap semantics
            }
        }


        private static bool _playerLoopHarmonyPatched;

        /// <summary>SPEC-004 Path 2: append <see cref="Bridge.PlayerLoopKeyInputInjection.DINOForgeUpdateMarker"/> to PlayerLoop.Update.</summary>
        private static bool InjectPlayerLoopUpdate()
        {
            bool injected = Bridge.PlayerLoopKeyInputInjection.InjectIntoCurrentPlayerLoop(
                typeof(Bridge.PlayerLoopKeyInputInjection.DINOForgeUpdateMarker),
                DINOForgePlayerLoopUpdate);
            if (injected)
            {
                PatchPlayerLoopRejection();
                Log?.LogInfo("[Plugin] PlayerLoop DINOForgeUpdate injected (SPEC-004 Path 2).");
            }
            else
            {
                Log?.LogWarning("[Plugin] PlayerLoop DINOForgeUpdate injection failed (Update subsystem missing?).");
            }

            return injected;
        }

        private static int _playerLoopEventSystemTick;
        private static string? _lastEventSystemReconcileKey;
        private static bool _prevF9;
        private static bool _prevF10;

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
        private static extern short PluginGetAsyncKeyState(int vKey);

        private static void DINOForgePlayerLoopUpdate()
        {
            _playerLoopEventSystemTick++;
            if (_playerLoopEventSystemTick % 60 == 1)
            {
                // iter-149e: heartbeat from the PlayerLoop too. The WinDbg MDMP showed NO Harmony/
                // DINOForge frame on the idle main thread — i.e. this injected PlayerLoop callback
                // may NOT actually tick under DINO's replaced PlayerLoop. If dinoforge_heartbeat.txt
                // only ever advances with scene-event sources (never "playerloop"), that confirms
                // the PlayerLoop revive path is dead and scene events are the sole reliable hook.
                BumpEngineHeartbeat("playerloop");
                EnsureEventSystemAlive();
                try { SharedBridgeServer?.EnsureServerAlive(); }
                catch (Exception ex) { DebugLog.Write("Plugin", $"[PlayerLoop] EnsureServerAlive: {ex.Message}"); }

                // FailureMode B definitive fix (iter-149, 2026-05-29): MAIN-THREAD resurrection
                // consumer. The PlayerLoop Update injection runs on the Unity MAIN THREAD every
                // frame and SURVIVES RuntimeDriver teardown (it is static + Harmony-injected), so
                // it is the correct place to perform resurrection — TryResurrect's Unity ECalls
                // (Camera.main / AddComponent / Initialize) are main-thread-safe here, whereas the
                // ResurrectionFallback BACKGROUND thread deadlocks on the same calls (and even on
                // the Unity `==`/GetComponent ECalls) during the InitialGameLoader→MainMenu asset
                // load — which is why its heartbeat went silent and the driver never revived.
                // Throttled to once/sec (the %60 gate) so we don't thrash; idempotent + cap-guarded
                // inside TryResurrect. Need flags are cleared only on confirmed success.
                ConsumeResurrectionOnMainThread();
            }

            if (!System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return;
            }

            const int VK_F9 = 0x78;
            const int VK_F10 = 0x79;
            const int KEY_PRESSED = unchecked((int)0x8000);

            bool f9Now = (PluginGetAsyncKeyState(VK_F9) & KEY_PRESSED) != 0;
            bool f10Now = (PluginGetAsyncKeyState(VK_F10) & KEY_PRESSED) != 0;

            if (f9Now && !_prevF9)
            {
                try { Bridge.KeyInputSystem.OnF9Pressed?.Invoke(); }
                catch (System.Exception ex) { DebugLog.Write("Plugin", $"[PlayerLoop] F9 handler threw: {ex.Message}"); }
            }
            if (f10Now && !_prevF10)
            {
                try { Bridge.KeyInputSystem.OnF10Pressed?.Invoke(); }
                catch (System.Exception ex) { DebugLog.Write("Plugin", $"[PlayerLoop] F10 handler threw: {ex.Message}"); }
            }

            _prevF9 = f9Now;
            _prevF10 = f10Now;
        }

        /// <summary>
        /// FailureMode B definitive fix (iter-149, 2026-05-29): MAIN-THREAD resurrection consumer.
        /// Invoked from <see cref="DINOForgePlayerLoopUpdate"/> (throttled by the caller's %60 gate),
        /// this runs on the Unity main thread and SURVIVES RuntimeDriver teardown (it is a static
        /// method reached via the Harmony-injected PlayerLoop entry). Unlike the ResurrectionFallback
        /// BACKGROUND thread — which deadlocks on Unity ECalls (Camera.main / AddComponent / Initialize
        /// touching Resources/asset APIs) during the InitialGameLoader→MainMenu asset load — every call
        /// made here is main-thread-safe.
        ///
        /// Idempotent and cap-guarded inside <see cref="TryResurrect"/>. Need flags are cleared only on
        /// confirmed success (<see cref="ResurrectionSucceeded"/>). Never throws to Unity (Pattern #104/#111).
        /// </summary>
        private static void ConsumeResurrectionOnMainThread()
        {
            try
            {
                bool needsRevive = NeedsResurrection || NeedsDeferredResurrection || s_rootJustDestroyed;
                if (!needsRevive)
                {
                    return;
                }

                // Cap gate: when PersistentRoot is gone, TryResurrect's create-root path is bounded by
                // MaxResurrectionAttempts. Checking here too avoids logging churn once the cap is hit.
                if (ReferenceEquals(PersistentRoot, null) && IsResurrectionCapExhausted())
                {
                    return;
                }

                string sceneName;
                try { sceneName = LastSceneNameForResurrection ?? SceneManager.GetActiveScene().name; }
                catch { sceneName = LastSceneNameForResurrection ?? "main-thread-unknown"; }

                DebugLog.Write("Plugin", $"[Plugin] ConsumeResurrectionOnMainThread: revive needed (NeedsRes={NeedsResurrection} NeedsDefRes={NeedsDeferredResurrection} rootJustDestroyed={s_rootJustDestroyed}) — invoking TryResurrect (scene='{sceneName}').");
                TryResurrect(sceneName, "main-thread-playerloop");

                if (ResurrectionSucceeded())
                {
                    NeedsResurrection = false;
                    NeedsDeferredResurrection = false;
                    s_rootJustDestroyed = false;
                    s_skipBundleUnload = false;
                    ResetGraceDeadline();
                    DebugLog.Write("Plugin", "[Plugin] Resurrection complete via main-thread-playerloop (driver live; flags cleared).");
                }
            }
            catch (Exception ex)
            {
                // Pattern #104/#111: surface, never throw into the PlayerLoop.
                try { DebugLog.Write("Plugin", $"[Plugin] ConsumeResurrectionOnMainThread threw: {ex.GetType().Name}: {ex.Message}"); } catch { /* diagnostic only */ }
            }
        }

        private static void PatchPlayerLoopRejection()
        {
            if (_playerLoopHarmonyPatched)
            {
                return;
            }

            try
            {
                var harmony = new Harmony("dinoforge.plugin.playerloop");
                System.Reflection.MethodInfo? original = typeof(PlayerLoop).GetMethod(
                    nameof(PlayerLoop.SetPlayerLoop),
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (original == null)
                {
                    Log?.LogWarning("[Plugin] PatchPlayerLoopRejection: SetPlayerLoop not found.");
                    return;
                }

                System.Reflection.MethodInfo? postfix = typeof(Plugin).GetMethod(
                    nameof(OnPlayerLoopSet),
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
                _playerLoopHarmonyPatched = true;
                DebugLog.Write("Plugin", "[Plugin] Harmony postfix on PlayerLoop.SetPlayerLoop applied.");
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"[Plugin] PatchPlayerLoopRejection failed: {ex.Message}");
            }
        }

        private static void OnPlayerLoopSet()
        {
            // Blocker 2 diagnostic (iter-149b): log every PlayerLoop rebuild + whether re-injection
            // re-added our DINOForgeUpdateMarker. If DINO rebuilds the loop entering MainMenu and our
            // marker is dropped, this surfaces it. Even if re-injection fails, the sceneLoaded /
            // activeSceneChanged main-thread revive path (Blocker 2) covers resurrection, so the
            // engine UI no longer depends solely on the PlayerLoop marker surviving.
            Bridge.PlayerLoopKeyInputInjection.OnAfterSetPlayerLoop(() =>
                Bridge.PlayerLoopKeyInputInjection.InjectIntoCurrentPlayerLoop(
                    typeof(Bridge.PlayerLoopKeyInputInjection.DINOForgeUpdateMarker),
                    DINOForgePlayerLoopUpdate));

            // iter-149e DECISIVE fix (WinDbg MDMP + live repro): after RuntimeDriver.OnDestroy on the
            // InitialGameLoader->MainMenu transition, ALL our managed activity halts — the
            // ResurrectionFallback bg thread stops heart-beating (it armed the grace window then went
            // silent), no MainMenu sceneLoaded/activeSceneChanged ever reaches our static handlers, and
            // the injected PlayerLoop callback never ticks. The engine stays healthy (process alive,
            // Responding=True, MainMenu rendered, engine heartbeat advances) — a DORMANT-PLUGIN bug,
            // not a native wedge. The ONE callback DINO itself drives on the main thread post-teardown
            // is THIS Harmony postfix on PlayerLoop.SetPlayerLoop — DINO calls SetPlayerLoop while
            // bringing up MainMenu systems. So drive the revive directly from HERE, on the main thread,
            // where TryResurrect's Unity ECalls (Camera.main / AddComponent / Initialize) are safe.
            // This does not depend on a post-teardown scene event or on our suspended bg threads.
            try { MainThreadReviveIfNeeded(LastSceneNameForResurrection ?? "playerloop-set", "playerloop-set(main-thread)"); }
            catch (Exception ex) { try { DebugLog.Write("Plugin", $"[Plugin] OnPlayerLoopSet revive threw: {ex.GetType().Name}: {ex.Message}"); } catch { /* diagnostic only */ } }

            try
            {
                bool markerPresent = Bridge.PlayerLoopKeyInputInjection.ContainsMarkerInUpdate(
                    UnityEngine.LowLevel.PlayerLoop.GetCurrentPlayerLoop(),
                    typeof(Bridge.PlayerLoopKeyInputInjection.DINOForgeUpdateMarker));
                DebugLog.Write("Plugin", $"[Plugin] OnPlayerLoopSet postfix fired — DINOForgeUpdateMarker re-injected={markerPresent}.");
            }
            catch (Exception ex)
            {
                DebugLog.Write("Plugin", $"[Plugin] OnPlayerLoopSet marker-check threw: {ex.Message}");
            }
        }
    }
}
