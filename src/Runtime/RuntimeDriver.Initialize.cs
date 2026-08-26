#nullable enable
using System;
using System.Collections;
using System.IO;
using BepInEx;
using BepInEx.Logging;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.UI;
using Unity.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace DINOForge.Runtime
{
    internal partial class RuntimeDriver
    {
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
                        System.Collections.Generic.IReadOnlyList<Updates.PackUpdateTarget> packTargets =
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
    }
}
