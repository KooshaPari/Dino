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
        private Profiles.ProfileManager? _profileManager;

        // ── Step 8: Update checker (#899) ─────────────────────────────────────────
        // The Task is fired on the thread pool after pack-load and polled in the
        // deferred-work coroutine loop. Results are pushed to the UI panel when ready.
        private System.Threading.Tasks.Task<System.Collections.Generic.IReadOnlyList<Updates.UpdateInfo>>? _updateCheckTask;
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

        private IEnumerator InitializeRoutine()
        {
            yield return null;

            RunPhaseWithAbortGuard("L10n.Initialize", () =>
            {
                try
                {
                    Localization.L10n.Initialize();
                    _log.LogInfo($"[RuntimeDriver] L10n initialized with locale: {Localization.L10n.CurrentLocale}");
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] L10n initialization failed: {ex}");
                }
            });
            yield return null;

            RunPhaseWithAbortGuard("CleanupUiInterceptors", CleanupUiInterceptors);
            yield return null;

            RunPhaseWithAbortGuard("UiAssets.Initialize", () =>
            {
                // Initialize Kenney CC0 UI asset loader.
                // Sprites are expected at BepInEx/plugins/dinoforge-ui-assets/ (deployed by MSBuild target).
                // If the directory or files are absent UiAssets falls back silently — all properties return null.
                try
                {
                    UiAssets.Initialize(BepInEx.Paths.PluginPath);
                    if (UiAssets.MissingFiles.Count > 0)
                    {
                        _log.LogInfo($"[RuntimeDriver] UiAssets: {UiAssets.MissingFiles.Count} sprite(s) not found " +
                            $"— flat-colour fallback active. See src/Runtime/UI/Assets/README.md for download instructions.");
                    }
                    else
                    {
                        _log.LogInfo("[RuntimeDriver] UiAssets: sprites loaded from disk.");
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] UiAssets initialization failed: {ex}");
                }
            });
            yield return null;

            RunPhaseWithAbortGuard("ModPlatform.Initialize", () =>
            {
                try
                {
                    _modPlatform = new ModPlatform();
                    _modPlatform.Initialize(_log, _config, gameObject);
                    _log.LogInfo("[RuntimeDriver] ModPlatform initialized.");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] ModPlatform initialization failed: {ex}");
                    _modPlatform = null;
                }
            });
            yield return null;

            RunPhaseWithAbortGuard("ProfileManager.Initialize", () =>
            {
                try
                {
                    string profilesDir = System.IO.Path.Combine(
                        BepInEx.Paths.BepInExRootPath, "dinoforge-profiles");
                    _profileManager = new Profiles.ProfileManager(profilesDir, _log);
                    _log.LogInfo($"[RuntimeDriver] ProfileManager initialised at '{profilesDir}'.");
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] ProfileManager initialisation failed: {ex.Message}");
                }
            });
            yield return null;

            RunPhaseWithAbortGuard("PackSettingsStore.Initialize", () =>
            {
                try
                {
                    // Fix(iter-148): use BepInEx root path so settings land under BepInEx/,
                    // not next to the game executable (AppDomain.CurrentDomain.BaseDirectory bug).
                    var store = Settings.PackSettingsStore.GetOrCreate(BepInEx.Paths.BepInExRootPath);
                    store.SetLogger(_log);
                    _log.LogInfo($"[RuntimeDriver] PackSettingsStore initialised at '{BepInEx.Paths.BepInExRootPath}'.");
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] PackSettingsStore initialisation failed: {ex.Message}");
                }
            });
            yield return null;

            RunPhaseWithAbortGuard("MainThreadDispatcher/DebugOverlay", () =>
            {
                // Add MainThreadDispatcher for IPC bridge support.
                try
                {
                    gameObject.AddComponent<Bridge.MainThreadDispatcher>();
                    _log.LogInfo("[RuntimeDriver] Added MainThreadDispatcher.");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] MainThreadDispatcher setup failed: {ex}");
                }

                // ── Step 1: Always add DebugOverlayBehaviour ────────────────────────────
                // This component owns the IMGUI F9 debug panel and must always be present
                // so F9 works even when UGUI is active or fails.  DFCanvas also shows a
                // UGUI debug panel (DebugPanel) when healthy, but DebugOverlayBehaviour
                // is the guaranteed fallback.
                try
                {
                    _debugOverlay = gameObject.AddComponent<DebugOverlayBehaviour>();
                    _log.LogInfo("[RuntimeDriver] Added DebugOverlayBehaviour (guaranteed F9 handler).");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] DebugOverlayBehaviour setup failed: {ex}");
                }

                // ── KeyInputSystem ECS callbacks (DISABLED) ────────────────────────────────
                // ECS callbacks are the reliable toggle path — KeyInputSystem.OnUpdate runs
                // in the ECS loop and correctly sees both physical and synthetic key presses.
                // The background thread's GetAsyncKeyState DOES NOT reliably see synthetic
                // keybd_event input from external processes, so ECS callbacks are preferred.
                // Background thread F9/F10 polling is disabled to prevent double-toggles.
                // Key mapping: F9=Debug panel, F10=Mods menu (#944 fix: correct swap from ff1455b2)
                DebugLog.Write("Plugin", "[RuntimeDriver] Key mapping: F9=Debug, F10=Mods");
                Bridge.KeyInputSystem.OnF9Pressed = () =>
                {
                    try
                    {
                        DebugLog.Write("Plugin", "[RuntimeDriver] F9 pressed → DEBUG panel (via KeyInputSystem)");
                        if (_uguiReady && _dfCanvas != null)
                        {
                            _dfCanvas.ToggleDebug();
                            // ForceRefresh after toggle so the panel always shows current data
                            // (Update() never fires in DINO — periodic refresh is dead code).
                            if (_dfCanvas.DebugPanel != null && _dfCanvas.DebugPanel.IsVisible)
                            {
                                _dfCanvas.DebugPanel.ForceRefresh();
                            }
                        }
                        else _debugOverlay?.Toggle();
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write("Plugin", $"[RuntimeDriver] F9 toggle failed: {ex.GetType().Name} - {ex.Message}");
                    }
                };
                Bridge.KeyInputSystem.OnF10Pressed = () =>
                {
                    try
                    {
                        DebugLog.Write("Plugin", "[RuntimeDriver] F10 pressed → MODS menu (via KeyInputSystem)");
                        if (_uguiReady && _dfCanvas != null) _dfCanvas.ToggleModMenu();
                        else _modMenuHost?.Toggle();
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write("Plugin", $"[RuntimeDriver] F10 toggle failed: {ex.GetType().Name} - {ex.Message}");
                    }
                };

                // ── Wire HMR pack reload callback (can be invoked from background thread) ──
                Bridge.KeyInputSystem.OnPackReloadRequested = () =>
                {
                    try
                    {
                        DebugLog.Write("Plugin", "[RuntimeDriver] Pack reload requested (via OnPackReloadRequested)");
                        RequestPackReload("OnPackReloadRequested");
                    }
                    catch (Exception ex)
                    {
                        _log?.LogWarning($"[RuntimeDriver] Pack reload request failed: {ex}");
                    }
                };
            });
            yield return null;

            // ── Step 2: Attempt UGUI canvas setup ───────────────────────────────────
            // DFCanvas.Initialize() builds the canvas hierarchy synchronously and calls
            // OnInitSuccess immediately if successful, or OnInitFailed if it throws.
            // We register both callbacks so that _uguiReady is set on the main thread,
            // not from the background polling thread (which would cause UnityException).
            RunPhaseWithAbortGuard("DFCanvas.Initialize", () =>
            {
                bool uguiAddedOk = false;
                try
                {
                    _dfCanvas = gameObject.AddComponent<DFCanvas>();

                    // Register callbacks BEFORE Initialize() — Initialize() calls them synchronously.
                    _dfCanvas.OnInitSuccess = () =>
                    {
                        _uguiReady = true;
                        _uguiChecked = true;
                        _log.LogInfo("[RuntimeDriver] DFCanvas.OnInitSuccess — UGUI canvas ready on main thread.");
                        DebugLog.Write("Plugin", "[RuntimeDriver] DFCanvas.OnInitSuccess: UGUI is ready.");
                        WireUguiToModPlatform();
                    };
                    _dfCanvas.OnInitFailed = () =>
                    {
                        _log.LogWarning("[RuntimeDriver] DFCanvas.OnInitFailed — activating IMGUI fallback.");
                        _uguiReady = false;
                        _uguiChecked = true;
                        ActivateImguiFallback();
                    };

                    _dfCanvas.Initialize(_log);

                    uguiAddedOk = true;
                    _log.LogInfo("[RuntimeDriver] Added DFCanvas — UGUI canvas built in Initialize().");
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] DFCanvas AddComponent failed, falling back to IMGUI immediately: {ex}");

                    if (_dfCanvas != null)
                    {
                        Destroy(_dfCanvas);
                        _dfCanvas = null;
                    }
                }

                if (!uguiAddedOk)
                {
                    // UGUI component could not even be added — activate IMGUI now.
                    _uguiChecked = true;
                    ActivateImguiFallback();
                }
            });
            yield return null;

            RunPhaseWithAbortGuard("NativeMenuInjector/HMR/startup", () =>
            {
                // ── Step 3: Add NativeMenuInjector for main menu button injection ──────
                // This component monitors scene changes and injects a "Mods" button into
                // the native game menus (main menu, pause menu) next to Settings/Options.
                try
                {
                    _nativeMenuInjector = gameObject.AddComponent<NativeMenuInjector>();
                    _nativeMenuInjector.SetLogger(_log);
                    // Fix (iter-149): wire the pack-data provider so the native MODS page
                    // (TryShowNativeModsPage) can populate its INSTALLED PACKS list. Without
                    // this, PackDataProvider stays null → SetPacks() is never called → the
                    // left pack list renders empty even though packs are loaded.
                    _nativeMenuInjector.PackDataProvider = () =>
                        _modPlatform?.GetLoadedPackDisplayInfos()
                        ?? (System.Collections.Generic.IReadOnlyList<PackDisplayInfo>)System.Array.Empty<PackDisplayInfo>();
                    // Quick panel reads the active total_conversion ui_theme from disk.
                    _nativeMenuInjector.PacksDirectory = _modPlatform?.PacksDirectory;
                    // Route quick-panel / native-page pack toggles + reloads through the same
                    // queued path the UGUI menu uses (SetPackEnabled persists disabled_packs.json).
                    _nativeMenuInjector.OnNativePackToggled = (packId, enabled) => RequestPackToggle(packId, enabled);
                    _nativeMenuInjector.OnNativeReloadRequested = () => RequestPackReload("native mods menu reload");
                    TryWireNativeMenuInjectorHost();
                    // SPEC-002 F-07: main-thread re-scan hook for tests/tooling (not background thread — ADR-015).
                    NativeMenuInjector.OnScanNeeded = () =>
                    {
                        try { _nativeMenuInjector?.TryInjectMenuButton(); }
                        catch { /* safe-swallow: TryInjectMenuButton already logs; external trigger must not throw */ }
                    };
                    _log.LogInfo("[RuntimeDriver] Added NativeMenuInjector — will inject Mods button into native menus.");
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] NativeMenuInjector setup failed: {ex}");
                }

                // ── Step 3b: UiEventInterceptor intentionally disabled ──
                // Interceptor diagnostics mutate button object names and can interfere with
                // NativeMenuInjector idempotency and click routing in production runtime.
                _log.LogInfo("[RuntimeDriver] UiEventInterceptor disabled for native menu stability.");

                // ── Step 4: Start HMR (Hot Module Reload) signal watcher ─────────────
                // Watches for DINOForge_HotReload signal file in BepInEx root
                // When detected, triggers soft UI + pack reload without full game restart
                if (Plugin._enableHotReload?.Value != false)
                {
                    // Create the tiered reloader so the watcher can classify signals.
                    // The reloader captures the loaded-DLL hash at construction time.
                    try
                    {
                        string runtimeDllPath = System.IO.Path.Combine(
                            BepInEx.Paths.PluginPath, "DINOForge.Runtime.dll");
                        _hmrTieredReloader = new HotReload.HmrTieredReloader(
                            _log,
                            packActions: new HmrPackActionsAdapter(this),
                            uiActions: new HmrUiActionsAdapter(this),
                            runtimeDllPath: runtimeDllPath);
                        _log.LogInfo("[RuntimeDriver] HmrTieredReloader created.");
                    }
                    catch (Exception ex)
                    {
                        _log.LogWarning($"[RuntimeDriver] HmrTieredReloader creation failed (will use flat reload): {ex}");
                    }

                    StartHmrWatcher();
                }
                else
                {
                    _log.LogInfo("[RuntimeDriver] HMR disabled via config (General.EnableHotReload=false).");
                }

                // ── Step 5: Start background polling (ECS world, catalog rebuild, heartbeats) ──
                // MonoBehaviour.Update() NEVER fires in DINO — background thread polling is required.
                StartBackgroundPollingThread();
            });

            // ── Step 6: Log key handler registration ────────────────────────────────
            DebugLog.Write("Plugin", $"[RuntimeDriver.Initialize] ENTRY — Initialize starting on {gameObject.name}");
            _log.LogInfo($"[RuntimeDriver] F9/F10 key handlers registered on {gameObject.name}.");

            // ── Step 6.5: Create themed loading screen (EPIC-027) ───────────────────
            // Full-screen branded loading takeover during the ~30-45s mod-init phase.
            // For an active total_conversion pack with a declared loading_screen, this
            // paints the pack's themed background + logo + tips. Hidden when the
            // MainMenu scene + engine UI are ready.
            RunPhaseWithAbortGuard("LoadingScreenController.Create", () =>
            {
                try
                {
                    // Reuse the early instance created in Plugin.Awake if it is still alive;
                    // only create a new one if it was never built or already faded out.
                    _loadingScreen = LoadingScreenController.Instance;
                    if (_loadingScreen == null)
                    {
                        string packsDir = _modPlatform?.PacksDirectory
                            ?? System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "dinoforge_packs");
                        _loadingScreen = LoadingScreenController.Create(gameObject, packsDir, _log);
                    }
                    if (_loadingScreen != null)
                    {
                        _loadingScreen.EnsureVisible();
                        _log.LogInfo("[RuntimeDriver] LoadingScreenController ready.");
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] LoadingScreenController creation failed: {ex}");
                }
            });
            yield return null;

            // ── Step 7: MainMenu-mode pack-load (no ECS world needed) ────────────────
            // Pack loading is YAML parsing — it does NOT require an ECS World.
            // OnWorldReadyCoroutine only fires when gameplay starts (ECS world created).
            // At main menu there is no ECS world, so packs would never load without this path.
            DebugLog.Write("Plugin", $"[RuntimeDriver] Step 7 ENTERING MainMenu-mode PackLoad — _modPlatform={((_modPlatform != null) ? "present" : "NULL")}");
            RunPhaseWithAbortGuard("MainMenu-mode PackLoad", () =>
            {
                RunMainMenuInit("initialize");

                // Subscribe to scene changes ONCE so re-entering a menu scene (e.g. returning
                // from gameplay to the main menu) re-runs the idempotent menu-mode init. This is
                // the self-healing path that recovers the engine UI after scene transitions.
                if (!_sceneChangeSubscribed)
                {
                    SceneManager.activeSceneChanged += OnRuntimeDriverSceneChanged;
                    _sceneChangeSubscribed = true;
                    _log.LogInfo("[RuntimeDriver] Subscribed activeSceneChanged for engine-UI self-heal.");
                }
            });

            // ── Step 8: Fire update check on the thread pool (best-effort, never blocks) ──
            RunPhaseWithAbortGuard("UpdateChecker.Launch", () =>
            {
                if (_modPlatform != null && !_updateCheckPushed)
                {
                    try
                    {
                        string bepInExRoot = BepInEx.Paths.BepInExRootPath;
                        string dinoForgeVersion = PluginInfo.VERSION;
                        IReadOnlyList<Updates.PackUpdateTarget> packTargets =
                            _modPlatform.GetPackUpdateTargets();
                        Updates.UpdateChecker checker = new Updates.UpdateChecker(bepInExRoot);
                        System.Threading.CancellationToken ct =
                            new System.Threading.CancellationToken(false);
                        _updateCheckTask = checker.RunAllChecksAsync(packTargets, dinoForgeVersion, ct);
                        _log.LogInfo($"[RuntimeDriver] Update check launched for DINOForge + {packTargets.Count} pack(s).");
                    }
                    catch (Exception updateEx)
                    {
                        _log.LogWarning($"[RuntimeDriver] Update check launch failed: {updateEx.Message}");
                    }
                }
            });

            if (Plugin._showOverlayOnStart?.Value == true && _dfCanvas != null)
            {
                _dfCanvas.ToggleDebug();
                _log.LogInfo("[RuntimeDriver] F9 overlay shown on start (General.ShowDebugOverlayOnStart=true).");
            }

            _log.LogInfo("[RuntimeDriver] Waiting for ECS World (Update polling)...");
            _log.LogInfo("[DINOForge] RuntimeDriver.Initialize() EXIT");

            // Pump deferred work on the main thread until destruction.
            int _themeAuxFrame = 0;
            while (!_destroyed)
            {
                // ── Engine-UI self-healing bounded retry ─────────────────────────
                // Re-attempt MODS-button injection until it succeeds or the retry budget
                // is spent. The native menu canvas / custom Selectable buttons may not be
                // present on the exact frame Step 7 ran, so a single missed window would
                // otherwise leave "no Mods button" until the next scene change. Re-running
                // each pump frame on the main thread closes that race deterministically.
                if (_nativeMenuInjector != null
                    && !_nativeMenuInjector.IsModsButtonInjected
                    && _menuInitRetryFrames < MenuInitMaxRetryFrames)
                {
                    _menuInitRetryFrames++;
                    if (_menuInitRetryFrames % 30 == 0) // ~twice/sec at 60fps; cheap canvas scan
                    {
                        try { _nativeMenuInjector.TryInjectMenuButton(); }
                        catch (Exception injEx)
                        {
                            // Surface, don't swallow (Pattern #104/#111).
                            _log?.LogWarning($"[RuntimeDriver] Engine-UI retry injection failed: {injEx.Message}");
                        }
                        // Emit the heartbeat once injection succeeds (or once the budget is
                        // exhausted) so the log shows the final engine-UI state at a glance.
                        if (_nativeMenuInjector.IsModsButtonInjected
                            || _menuInitRetryFrames >= MenuInitMaxRetryFrames)
                        {
                            LogEngineUiHeartbeat("self-heal retry");
                        }
                    }
                }

                // Retry MainMenuThemer if canvas wasn't ready during Step 7
                if (_mainMenuThemer != null && !_mainMenuThemer.IsApplied && _modPlatform != null && _themeRetryCount < 600)
                {
                    _themeRetryCount++;
                    if (_themeRetryCount % 5 == 0) // every ~5 frames
                    {
                        try
                        {
                            var packInfos = _modPlatform.GetLoadedPackDisplayInfos();
                            if (packInfos.Count > 0)
                                _mainMenuThemer.TryApplyTheme(packInfos);
                        }
                        catch { /* safe-swallow: theme retry is best-effort */ }
                    }
                }

                // Re-skin non-MainMenu pages on a steady cadence. Settings sub-tabs and the
                // game create/select screens are instantiated lazily when the user navigates,
                // so a one-shot apply misses them. The reskinner is idempotent (per-object
                // marker) — repeated passes only touch newly-appeared elements. (#970b)
                if (_canvasReskinner != null && _modPlatform != null)
                {
                    _reskinRetryCount++;
                    if (_reskinRetryCount % 15 == 0) // every ~15 frames
                    {
                        try
                        {
                            _canvasReskinner.ReskinAllPages(_modPlatform.GetLoadedPackDisplayInfos());
                        }
                        catch { /* safe-swallow: page reskin retry is best-effort */ }
                    }
                }

                // ── Subpage FULL TAKEOVER (#974): Options + settings tabs + in-game panels ──
                // Subpages are separate canvases the user opens AFTER the main menu and
                // re-opens repeatedly. The packs-overload performs the SAME full takeover the
                // main menu got (supersedes the #970a color-only aux-skin); it self-guards on
                // the live canvas count so this per-frame call is cheap until a subpage opens.
                if (_mainMenuThemer != null && _modPlatform != null && (_themeAuxFrame++ % 10) == 0)
                {
                    try
                    {
                        var auxPacks = _modPlatform.GetLoadedPackDisplayInfos();
                        if (auxPacks.Count > 0)
                            _mainMenuThemer.ApplyToAuxiliaryMenus(auxPacks);
                    }
                    catch { /* safe-swallow: aux takeover is best-effort */ }
                }

                // ── Step 8 deferred: push update-check results to UI once the Task completes ──
                if (!_updateCheckPushed && _updateCheckTask != null
                    && _updateCheckTask.IsCompleted && _dfCanvas?.ModMenuPanel != null)
                {
                    _updateCheckPushed = true;
                    try
                    {
                        System.Collections.Generic.IReadOnlyList<Updates.UpdateInfo> updates =
                            _updateCheckTask.Result;
                        if (updates.Count > 0)
                        {
                            _dfCanvas.ModMenuPanel.SetUpdatesAvailable(updates);
                            _log?.LogInfo($"[RuntimeDriver] Update check: {updates.Count} update(s) pushed to UI.");
                        }
                        else
                        {
                            _log?.LogInfo("[RuntimeDriver] Update check: up to date.");
                        }
                    }
                    catch (Exception updateEx)
                    {
                        // safe-swallow: update-check result delivery is best-effort
                        _log?.LogWarning($"[RuntimeDriver] Update check result delivery failed: {updateEx.Message}");
                    }
                    _updateCheckTask = null;
                }

                if (TryDequeuePendingWorldReady(out World? pendingWorld))
                {
                    yield return ProcessWorldReadyCoroutine(pendingWorld!);
                    continue;
                }

                if (TryDequeuePendingPackReload(out string? packReloadReason))
                {
                    yield return ProcessPackReloadCoroutine(packReloadReason!);
                    continue;
                }

                if (TryDequeuePendingPackToggle(out string? packId, out bool enabled))
                {
                    yield return ProcessPackToggleCoroutine(packId!, enabled);
                    continue;
                }

                // Deferred catalog rebuild (queued from background thread to avoid EntityManager race)
                World? catalogWorld = null;
                lock (_deferredWorkLock)
                {
                    if (_pendingCatalogWorld != null)
                    {
                        catalogWorld = _pendingCatalogWorld;
                        _pendingCatalogWorld = null;
                    }
                }
                if (catalogWorld != null && catalogWorld.IsCreated)
                {
                    try
                    {
                        _log?.LogInfo($"[RuntimeDriver] Catalog rebuild executing on main thread for world '{catalogWorld.Name}'");
                        _modPlatform?.RebuildCatalogAndApplyStats(catalogWorld);
                    }
                    catch (Exception ex)
                    {
                        _log?.LogWarning($"[RuntimeDriver] Catalog rebuild failed: {ex.Message}");
                    }
                }

                yield return null;
            }
        }

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


        /// <summary>
        /// Win32 API: GetAsyncKeyState - polls keyboard state without blocking.
        /// Returns a short where bit 15 (0x8000) indicates key is currently pressed.
        /// </summary>
        [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)]
        private static extern short GetAsyncKeyState(int vKey);

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
    }
}
