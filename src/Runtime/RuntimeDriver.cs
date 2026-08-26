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
using DINOForge.Runtime.Profiles;
using DINOForge.Runtime.Telemetry;
using DINOForge.Runtime.UI;
using DINOForge.Runtime.Updates;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DINOForge.Runtime
{
    /// <summary>
    /// Persistent MonoBehaviour that runs on the DINOForge_Root GameObject.
    /// Uses Update()-based polling instead of coroutines to detect the ECS world,
    /// since coroutines die with their host MonoBehaviour and the BepInEx object
    /// gets destroyed before the ECS world is ready.
    ///
    /// Hosts all UI components (debug overlay on F9, mod menu on F10).
    ///
    /// Key design: F9/F10 handling lives HERE, not in DFCanvas or ModMenuOverlay,
    /// so the shortcuts always work regardless of which UI layer is active.
    /// </summary>
    internal partial class RuntimeDriver : MonoBehaviour
    {
        private ManualLogSource _log = null!;
        private ConfigFile _config = null!;
        private bool _dumpOnStartup;
        private string _dumpOutputPath = "";
        private ModPlatform? _modPlatform;

        // UGUI system (preferred). Null if UGUI setup failed.
        internal DFCanvas? _dfCanvas;

        // EPIC-027: themed loading-screen takeover (shown during mod init / scene loads,
        // faded out when the target scene + engine UI are ready).
        private LoadingScreenController? _loadingScreen;

        // Active UI hosts.
        // _modMenuHost is always set to the active menu (UGUI when healthy, IMGUI fallback otherwise).
        // _debugOverlay is ALWAYS added (it owns the IMGUI F9 debug panel).
        private IModMenuHost? _modMenuHost;
        private IModSettingsHost? _modSettingsHost;
        private DebugOverlayBehaviour? _debugOverlay;
        private HudIndicator? _hudIndicator;
        private NativeMenuInjector? _nativeMenuInjector;
        private MainMenuThemer? _mainMenuThemer;
        private UI.CanvasReskinner? _canvasReskinner;
        private int _reskinRetryCount;

        // Theme retry counter — promoted from local so RearmThemer() can reset it.
        private int _themeRetryCount = 0;

        /// <summary>
        /// Resets the MainMenuThemer retry counter so the pump loop retries from frame 0.
        /// Called on every MainMenu scene activation so the themer has a fresh window to apply.
        /// </summary>
        internal void RearmThemer()
        {
            _themeRetryCount = 0;
            DebugLog.Write("Plugin", "[RuntimeDriver] RearmThemer: theme retry counter reset");
        }

        // ── Engine-UI self-healing (fix/engine-ui-injection-race) ────────────────
        // RunMainMenuInit() is idempotent and re-runnable; these track its state so the
        // main-thread pump can bounded-retry injection until the MODS button exists, and so
        // the scene-change handler can re-run the menu-mode init when re-entering a menu scene.
        // This kills the intermittent "no Mods button / no engine UI" race: a single missed
        // timing window (ECS-world gate, late canvas, custom Selectable button) auto-recovers.
        private bool _engineUiHeartbeatLogged;
        private int _menuInitRetryFrames;
        // Bounded retry budget — re-attempt MODS injection for up to N pump frames after the
        // initial menu-mode init. At ~once-per-frame this covers several seconds of menu fade-in.
        private const int MenuInitMaxRetryFrames = 600;
        // Subscribed once; reset menu-mode init state when a menu scene becomes active again.
        private bool _sceneChangeSubscribed;

        // _uguiReady: true once DFCanvas.Start() reports success via IsReady.
        // We check this each Update() because DFCanvas.Start() runs after Initialize().
        internal bool _uguiReady;
        // _uguiChecked: we only need to check DFCanvas readiness once after it has
        // had at least one frame to run its Start().
        internal bool _uguiChecked;

        /// <summary>
        /// Registers KeyInputSystem in the given ECS world if not already registered.
        /// Called every poll cycle to ensure the pump survives scene transitions.
        /// Safe to call multiple times (GetOrCreateSystem is idempotent).
        /// </summary>
        private void TryRegisterKeyInputSystem(World world)
        {
            if (_registeredWorldInstance != null && ReferenceEquals(_registeredWorldInstance, world)) return;
            try
            {
                world.GetOrCreateSystem<Bridge.KeyInputSystem>();
                _log.LogInfo($"[RuntimeDriver] KeyInputSystem registered in world '{world.Name}'.");
                _registeredWorldInstance = world;
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] TryRegisterKeyInputSystem failed: {ex}");
            }
        }

        private bool _worldFound;
        private bool _initialized;

        /// <summary>Public accessor for TryResurrect to check if RuntimeDriver is initialized.</summary>
        internal bool IsInitialized => _initialized;
        private bool _catalogRebuilt;
        private float _worldPollTimer;
        // Tracks the ECS world instance that KeyInputSystem was registered in.
        // When DINO transitions scenes, it destroys the old world and creates a new one.
        // We detect this by comparing the current DefaultGameObjectInjectionWorld against
        // _registeredWorldInstance and re-registering KeyInputSystem in the new world.
        private World? _registeredWorldInstance;
        // Cross-thread flag: true once OnDestroy is called. The background polling thread
        // checks this to avoid calling OnWorldReady after the RuntimeDriver is destroyed.
        private volatile bool _destroyed;
        private readonly ManualResetEventSlim _backgroundPollStopEvent = new(false);
        private readonly object _deferredWorkLock = new();
        private bool _bootSequenceStarted;
        private bool _worldReadyProcessing;
        private World? _pendingWorldReady;
        private bool _hasPendingWorldReady;
        private bool _pendingPackReload;
        private string? _pendingPackReloadReason;
        private bool _pendingPackToggle;
        private string? _pendingPackToggleId;
        private bool _pendingPackToggleEnabled;
        private World? _pendingCatalogWorld;
        private bool _sceneDumpQueued;

        // HMR tiered reloader — created once ModPlatform is available.
        private HotReload.HmrTieredReloader? _hmrTieredReloader;

        // Profiles manager (#918) — created once BepInEx root path is known.
        private ProfileManager? _profileManager;

        // ── Step 8: Update checker (#899) ─────────────────────────────────────────
        // The Task is fired on the thread pool after pack-load and polled in the
        // deferred-work coroutine loop. Results are pushed to the UI panel when ready.
        private System.Threading.Tasks.Task<IReadOnlyList<UpdateInfo>>? _updateCheckTask;
        private bool _updateCheckPushed;

        // Iter-144 #543 gray-freeze fix: cross-thread static flag observable by any subsystem
        // (e.g. VanillaCatalog.Build, ContentLoader pack registration) so they can short-circuit
        // cleanly when DINO is tearing down the ECS world. Set true at the TOP of OnDestroy
        // before any other shutdown work, so the window between scene-transition begin and our
        // OnDestroy completion is observable to callers running on the main thread.
        private static volatile bool s_isBeingDestroyed;
        public static bool IsBeingDestroyed => s_isBeingDestroyed;

        /// <summary>Polling interval in seconds for ECS world detection.</summary>
        private const float WorldPollInterval = 0.5f;

        private void RunPhaseWithAbortGuard(string phaseName, Action phase)
        {
            try
            {
                phase();
            }
            catch (ThreadAbortException)
            {
                try
                {
#pragma warning disable SYSLIB0006 // Required here to clear Unity's abort and preserve the rest of the teardown path.
                    Thread.ResetAbort();
#pragma warning restore SYSLIB0006
                }
                catch (Exception resetEx)
                {
                    _log?.LogWarning($"[RuntimeDriver] {phaseName} abort reset failed: {resetEx}");
                }

                _log?.LogWarning($"[RuntimeDriver] {phaseName} aborted by Unity thread abort.");
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[RuntimeDriver] {phaseName} failed: {ex}");
            }
        }

        /// <summary>
        /// Initializes the driver with config and logger references.
        /// Called immediately after AddComponent by Plugin.Awake().
        /// </summary>
        public void Initialize(ManualLogSource log, ConfigFile config, bool dumpOnStartup, string dumpOutputPath)
        {
            _log = log;
            _config = config;
            _dumpOnStartup = dumpOnStartup;
            _dumpOutputPath = dumpOutputPath;
            _initialized = true;
            _log.LogInfo("[DINOForge] RuntimeDriver.Initialize() ENTRY");
            if (_bootSequenceStarted)
            {
                _log.LogWarning("[RuntimeDriver] Initialize() called after boot sequence already started.");
                return;
            }

            _bootSequenceStarted = true;
            StartCoroutine(InitializeRoutine());
        }

        /// <summary>
        /// Called once when the ECS World becomes available (non-InitialGameLoader scenes only).
        /// Loads packs, starts hot reload. KeyInputSystem is registered every poll cycle
        /// via <see cref="TryRegisterKeyInputSystem"/> so it survives scene transitions.
        /// </summary>
        private void OnWorldReady(World ecsWorld)
        {
            _log.LogInfo($"[RuntimeDriver] ECS World available: {ecsWorld.Name}");
            _registeredWorldInstance = ecsWorld;
            lock (_deferredWorkLock)
            {
                _pendingWorldReady = ecsWorld;
                _hasPendingWorldReady = true;
            }
        }

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

        /// <summary>
        /// Win32 API: GetAsyncKeyState - polls keyboard state without blocking.
        /// Returns a short where bit 15 (0x8000) indicates key is currently pressed.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern short GetAsyncKeyState(int vKey);
    }
}
