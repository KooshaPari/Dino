#nullable enable
using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using DINOForge.Runtime.UI;
using DINOForge.SDK;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime
{
    /// <summary>
    /// BepInEx entry point for the DINOForge mod platform.
    /// Bootstraps the <see cref="ModPlatform"/> orchestrator, registers ECS systems,
    /// and wires up UI overlays and hot reload.
    ///
    /// IMPORTANT: The BepInEx-managed GameObject (this.gameObject) gets destroyed
    /// during DINO's scene transitions, even with DontDestroyOnLoad. To survive,
    /// we create a separate "DINOForge_Root" GameObject with HideAndDontSave flags
    /// and attach all persistent MonoBehaviours to it. This matches the pattern
    /// used by devopsdinosaur/dno-mods where ECS systems outlive MonoBehaviours.
    /// </summary>
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource Log = null!;
        private Harmony? _harmony;

        /// <summary>
        /// The persistent GameObject that survives scene changes.
        /// All UI and runtime components live here, NOT on the BepInEx-managed gameObject.
        /// </summary>
        internal static GameObject? PersistentRoot { get; private set; }

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"DINOForge Runtime v{PluginInfo.VERSION} loading...");

            // Config for debug features
            ConfigEntry<bool> dumpOnStartup = Config.Bind("Debug", "DumpOnStartup", true,
                "Automatically dump entity/component data when the game loads");
            ConfigEntry<string> dumpOutputPath = Config.Bind("Debug", "DumpOutputPath",
                Path.Combine(Paths.BepInExRootPath, "dinoforge_dumps"),
                "Directory to write entity/component dump files");

            // Detect game and log version compatibility info
            try
            {
                var bepinexVersion = typeof(BaseUnityPlugin).Assembly.GetName().Version?.ToString() ?? "unknown";
                Log.LogInfo($"DINOForge v{PluginInfo.VERSION} | BepInEx {bepinexVersion} | Unity {Application.unityVersion}");
                Log.LogInfo($"Platform: {Application.platform}");
                LogInstallDiagnostics();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Version detection failed: {ex.Message}");
            }

            // Harmony (available but unused per ADR-005)
            try
            {
                _harmony = new Harmony(PluginInfo.GUID);
                Log.LogInfo("Harmony initialized (no patches applied).");
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony init failed: {ex.Message}");
            }

            // Create a dedicated persistent GameObject that won't be destroyed.
            // The BepInEx-managed gameObject gets cleaned up during DINO's scene
            // transitions. A separate object with HideAndDontSave survives.
            try
            {
                PersistentRoot = new GameObject("DINOForge_Root");
                PersistentRoot.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(PersistentRoot);
                Log.LogInfo("[Plugin] Persistent root GameObject created.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] Failed to create persistent root: {ex.Message}");
                return;
            }

            // Add the runtime driver to the persistent root.
            // RuntimeDriver is a MonoBehaviour that handles Update()-based polling
            // for the ECS world and hosts all UI components.
            try
            {
                RuntimeDriver driver = PersistentRoot.AddComponent<RuntimeDriver>();
                driver.Initialize(Logger, Config, dumpOnStartup.Value, dumpOutputPath.Value);
                Log.LogInfo("[Plugin] RuntimeDriver initialized on persistent root.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] RuntimeDriver setup failed: {ex.Message}");
            }

            WriteDebug("Awake completed");
            Log.LogInfo("DINOForge Runtime loaded successfully.");
        }

        private static void WriteDebug(string msg)
        {
            try
            {
                string debugLog = Path.Combine(Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.Now}] {msg}\n");
            }
            catch { }
        }

        private static void LogInstallDiagnostics()
        {
            string loadedAssemblyPath = typeof(Plugin).Assembly.Location;
            string primaryRuntimePath = Path.Combine(Paths.PluginPath, "DINOForge.Runtime.dll");
            string legacyRuntimePath = Path.Combine(Paths.BepInExRootPath, "ecs_plugins", "DINOForge.Runtime.dll");
            string backupRuntimePath = Path.Combine(Paths.PluginPath, "DINOForge.Runtime.dll.bak");

            Log.LogInfo($"[Plugin] Loaded runtime assembly from: {loadedAssemblyPath}");
            WriteDebug($"[Plugin] Loaded runtime assembly from: {loadedAssemblyPath}");

            if (File.Exists(legacyRuntimePath))
            {
                string message = $"[Plugin] Legacy runtime copy detected at deprecated path: {legacyRuntimePath}";
                Log.LogWarning(message);
                WriteDebug(message);
            }

            if (File.Exists(primaryRuntimePath) && File.Exists(legacyRuntimePath))
            {
                string message = $"[Plugin] Duplicate runtime assemblies detected. Primary='{primaryRuntimePath}', Legacy='{legacyRuntimePath}'";
                Log.LogWarning(message);
                WriteDebug(message);
            }

            if (File.Exists(backupRuntimePath))
            {
                string message = $"[Plugin] Stale runtime backup file detected: {backupRuntimePath}";
                Log.LogWarning(message);
                WriteDebug(message);
            }

            if (!string.Equals(loadedAssemblyPath, primaryRuntimePath, StringComparison.OrdinalIgnoreCase))
            {
                string message = $"[Plugin] Runtime loaded from non-canonical location. Expected '{primaryRuntimePath}', actual '{loadedAssemblyPath}'";
                Log.LogWarning(message);
                WriteDebug(message);
            }
        }

        private void OnDestroy()
        {
            // The BepInEx-managed object is being destroyed (expected in DINO).
            // The persistent root and RuntimeDriver continue running independently.
            Log?.LogInfo("[Plugin] BepInEx plugin object OnDestroy (persistent root still alive).");
            _harmony?.UnpatchSelf();
            WriteDebug("OnDestroy called (BepInEx object only)");
        }
    }

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
    internal class RuntimeDriver : MonoBehaviour
    {
        private ManualLogSource _log = null!;
        private ConfigFile _config = null!;
        private bool _dumpOnStartup;
        private string _dumpOutputPath = "";

        private ModPlatform? _modPlatform;

        // UGUI system (preferred). Null if UGUI setup failed.
        private DFCanvas? _dfCanvas;

        // IMGUI overlay components.
        // _modMenuOverlay is always set to the active menu (either UGUI proxy or IMGUI).
        // _debugOverlay is ALWAYS added (it owns the IMGUI F9 debug panel).
        private ModMenuOverlay? _modMenuOverlay;
        private ModSettingsPanel? _modSettingsPanel;
        private DebugOverlayBehaviour? _debugOverlay;
        private HudIndicator? _hudIndicator;
        private NativeMenuInjector? _nativeMenuInjector;

        // _uguiReady: true once DFCanvas.Start() reports success via IsReady.
        // We check this each Update() because DFCanvas.Start() runs after Initialize().
        private bool _uguiReady;
        // _uguiChecked: we only need to check DFCanvas readiness once after it has
        // had at least one frame to run its Start().
        private bool _uguiChecked;

        private bool _worldFound;
        private bool _initialized;
        private bool _catalogRebuilt;
        private float _worldPollTimer;

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
                _log.LogWarning($"[RuntimeDriver] UiAssets initialization failed: {ex.Message}");
            }

            // Initialize ModPlatform orchestrator
            try
            {
                _modPlatform = new ModPlatform();
                _modPlatform.Initialize(_log, _config, gameObject);
                _log.LogInfo("[RuntimeDriver] ModPlatform initialized.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[RuntimeDriver] ModPlatform initialization failed: {ex.Message}");
                _modPlatform = null;
            }

            // Add MainThreadDispatcher for IPC bridge support
            try
            {
                gameObject.AddComponent<Bridge.MainThreadDispatcher>();
                _log.LogInfo("[RuntimeDriver] Added MainThreadDispatcher.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[RuntimeDriver] MainThreadDispatcher setup failed: {ex.Message}");
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
                _log.LogError($"[RuntimeDriver] DebugOverlayBehaviour setup failed: {ex.Message}");
            }

            // ── Step 2: Attempt UGUI canvas setup ───────────────────────────────────
            // DFCanvas.Initialize() only stores the logger; the actual canvas hierarchy
            // is built in DFCanvas.Start() which Unity calls on the NEXT frame.
            // We register an OnInitFailed callback so that if Start() throws, we can
            // activate the IMGUI fallback from Update() on the same frame it fails.
            bool uguiAddedOk = false;
            try
            {
                _dfCanvas = gameObject.AddComponent<DFCanvas>();
                _dfCanvas.Initialize(_log);

                // Register failure callback — called by DFCanvas.Start() if BuildCanvas throws.
                _dfCanvas.OnInitFailed = () =>
                {
                    _log.LogWarning("[RuntimeDriver] DFCanvas.OnInitFailed — activating IMGUI fallback.");
                    _uguiReady = false;
                    _uguiChecked = true;
                    ActivateImguiFallback();
                };

                uguiAddedOk = true;
                _log.LogInfo("[RuntimeDriver] Added DFCanvas — canvas hierarchy deferred to Start().");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] DFCanvas AddComponent failed, falling back to IMGUI immediately: {ex.Message}");

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

            // ── Step 3: Add NativeMenuInjector for main menu button injection ──────
            // This component monitors scene changes and injects a "Mods" button into
            // the native game menus (main menu, pause menu) next to Settings/Options.
            try
            {
                _nativeMenuInjector = gameObject.AddComponent<NativeMenuInjector>();
                _nativeMenuInjector.SetLogger(_log);
                // We'll wire the overlay reference later once it's created
                _log.LogInfo("[RuntimeDriver] Added NativeMenuInjector — will inject Mods button into native menus.");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] NativeMenuInjector setup failed: {ex.Message}");
            }

            // ── Step 3b: Add UiEventInterceptor for comprehensive UI event logging ──
            // This component logs ALL button clicks and pointer events to diagnose UI issues.
            try
            {
                UiEventInterceptor interceptor = gameObject.AddComponent<UiEventInterceptor>();
                interceptor.SetLogger(_log);
                _log.LogInfo("[RuntimeDriver] Added UiEventInterceptor — logging all button clicks for diagnostics.");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] UiEventInterceptor setup failed: {ex.Message}");
            }

            // ── Step 4: Log key handler registration ────────────────────────────────
            _log.LogInfo($"[RuntimeDriver] F9/F10 key handlers registered on {gameObject.name}.");
            _log.LogInfo("[RuntimeDriver] Waiting for ECS World (Update polling)...");
        }

        /// <summary>
        /// Activates the IMGUI fallback UI (ModMenuOverlay + ModSettingsPanel + HudIndicator).
        /// Safe to call from Update() as well as Initialize().
        /// No-ops if already activated.
        /// </summary>
        private void ActivateImguiFallback()
        {
            // Guard: only activate once
            if (_modMenuOverlay != null) return;

            try
            {
                ModMenuOverlay overlay = gameObject.AddComponent<ModMenuOverlay>();
                ModSettingsPanel settingsPanel = gameObject.AddComponent<ModSettingsPanel>();

                // Wire settings panel into mod menu for its inline Settings button
                overlay.SetSettingsPanel(settingsPanel);

                if (_modPlatform != null)
                {
                    _modPlatform.SetUI(overlay, settingsPanel);
                }

                // Wire ModMenuOverlay into NativeMenuInjector for the native Mods button
                if (_nativeMenuInjector != null)
                {
                    _nativeMenuInjector.SetModMenuOverlay(overlay);
                }

                _modMenuOverlay   = overlay;
                _modSettingsPanel = settingsPanel;

                _log.LogInfo("[RuntimeDriver] IMGUI fallback — Added ModMenuOverlay + ModSettingsPanel.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[RuntimeDriver] IMGUI fallback ModMenuOverlay setup failed: {ex.Message}");
            }

            try
            {
                _hudIndicator = gameObject.AddComponent<HudIndicator>();
                _hudIndicator.SetModMenu(_modMenuOverlay);

                if (_modMenuOverlay != null)
                {
                    _modMenuOverlay.OnReloadRequested += () => _hudIndicator?.ShowToast("Packs reloaded");
                }

                _log.LogInfo("[RuntimeDriver] IMGUI fallback — Added HudIndicator.");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] HudIndicator setup failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Wires UGUI DFCanvas to ModPlatform once DFCanvas.Start() has succeeded.
        /// Called the first frame that DFCanvas.IsReady becomes true.
        /// </summary>
        private void WireUguiToModPlatform()
        {
            if (_dfCanvas == null || _modPlatform == null) return;

            try
            {
                // ModMenuPanel exposes the same API as ModMenuOverlay.
                // ModPlatform.SetUI still expects ModMenuOverlay + ModSettingsPanel.
                // We use a thin proxy wrapper (ModMenuOverlayProxy) to bridge them.
                ModMenuOverlayProxy proxy = gameObject.AddComponent<ModMenuOverlayProxy>();
                proxy.enabled = false; // disable IMGUI Update/OnGUI — UGUI owns rendering

                if (_dfCanvas.ModMenuPanel != null)
                {
                    proxy.SetTarget(_dfCanvas.ModMenuPanel);
                    _dfCanvas.ModMenuPanel.OnReloadRequested = () => _modPlatform?.LoadPacks();
                }

                ModSettingsPanel settingsPanel = gameObject.AddComponent<ModSettingsPanel>();
                settingsPanel.enabled = false;

                _modPlatform.SetUI(proxy, settingsPanel);

                // Wire ModMenuOverlayProxy into NativeMenuInjector for the native Mods button
                if (_nativeMenuInjector != null)
                {
                    _nativeMenuInjector.SetModMenuOverlay(proxy);
                }

                // Wire UGUI DebugPanel to ModPlatform so it displays platform status
                if (_dfCanvas.DebugPanel != null && _modPlatform != null)
                {
                    _dfCanvas.DebugPanel.SetModPlatform(_modPlatform);
                    _log.LogInfo("[RuntimeDriver] UGUI DebugPanel wired to ModPlatform.");
                }

                _modMenuOverlay   = proxy;
                _modSettingsPanel = settingsPanel;

                _log.LogInfo("[RuntimeDriver] UGUI wired to ModPlatform via ModMenuOverlayProxy.");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] UGUI→ModPlatform wiring failed, activating IMGUI fallback: {ex.Message}");
                _uguiReady = false;
                ActivateImguiFallback();
            }
        }

        private void Update()
        {
            if (!_initialized) return;

            // ── F9/F10 key handling — always runs regardless of UI layer ─────────
            // These handlers are intentionally on RuntimeDriver so they work even if
            // DFCanvas or ModMenuOverlay fails to initialise.
            if (Input.GetKeyDown(KeyCode.F9))
            {
                // Prefer UGUI DebugPanel when available; fall back to IMGUI overlay
                if (_uguiReady && _dfCanvas != null)
                    _dfCanvas.ToggleDebug();
                else
                    _debugOverlay?.Toggle();
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                // Prefer UGUI ModMenuPanel when available; fall back to IMGUI overlay
                if (_uguiReady && _dfCanvas != null)
                    _dfCanvas.ToggleModMenu();
                else
                    _modMenuOverlay?.Toggle();
            }

            // ── Check whether DFCanvas.Start() has run yet ───────────────────────
            // DFCanvas.Start() is deferred by Unity so we can't know in Initialize()
            // whether UGUI succeeded.  We check IsReady each frame until confirmed.
            if (!_uguiChecked && _dfCanvas != null)
            {
                if (_dfCanvas.IsReady)
                {
                    _uguiReady    = true;
                    _uguiChecked  = true;
                    _log.LogInfo("[RuntimeDriver] DFCanvas confirmed ready — UGUI active.");
                    WireUguiToModPlatform();
                }
                // If not yet ready, keep waiting (OnInitFailed callback handles failure)
            }

            // ── Deferred VanillaCatalog rebuild once the game scene is loaded ────
            // The world exists at startup with only 24 entities (loading screen).
            // We wait until entities > 1000 (full scene loaded ~45K entities) to rebuild.
            if (_worldFound && !_catalogRebuilt)
            {
                try
                {
                    World? w = World.DefaultGameObjectInjectionWorld;
                    if (w != null && w.IsCreated)
                    {
                        int entityCount = w.EntityManager.UniversalQuery.CalculateEntityCount();
                        if (entityCount > 1000)
                        {
                            _catalogRebuilt = true;
                            _modPlatform?.RebuildCatalogAndApplyStats(w);
                        }
                    }
                }
                catch { }
            }

            // ── ECS world poll ───────────────────────────────────────────────────
            if (_worldFound) return;

            _worldPollTimer += Time.deltaTime;
            if (_worldPollTimer < WorldPollInterval) return;
            _worldPollTimer = 0f;

            try
            {
                World? world = World.DefaultGameObjectInjectionWorld;
                if (world != null && world.IsCreated)
                {
                    _worldFound = true;
                    OnWorldReady(world);
                }
            }
            catch
            {
                // World not ready yet, will retry next poll
            }
        }

        /// <summary>
        /// Called once when the ECS World becomes available.
        /// Registers systems, loads packs, starts hot reload.
        /// </summary>
        private void OnWorldReady(World ecsWorld)
        {
            _log.LogInfo($"[RuntimeDriver] ECS World available: {ecsWorld.Name}");

            // Register DumpSystem if configured
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
                    _log.LogWarning($"[RuntimeDriver] DumpSystem registration failed: {ex.Message}");
                }
            }

            // Notify ModPlatform that the world is ready
            if (_modPlatform != null)
            {
                try
                {
                    _modPlatform.OnWorldReady(ecsWorld);
                    _log.LogInfo("[RuntimeDriver] ModPlatform notified of world readiness.");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] ModPlatform.OnWorldReady failed: {ex.Message}");
                }

                // Load packs
                try
                {
                    ContentLoadResult result = _modPlatform.LoadPacks();
                    _log.LogInfo($"[RuntimeDriver] Pack loading complete: success={result.IsSuccess}, " +
                        $"loaded={result.LoadedPacks.Count}, errors={result.Errors.Count}");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] Pack loading failed: {ex.Message}");
                }

                // Start hot reload
                try
                {
                    _modPlatform.StartHotReload();
                    _log.LogInfo("[RuntimeDriver] Hot reload started.");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] Hot reload startup failed: {ex.Message}");
                }

                // Discover settings for the settings panel
                try
                {
                    if (_modSettingsPanel != null)
                    {
                        _modSettingsPanel.DiscoverSettings();
                        _log.LogInfo("[RuntimeDriver] Mod settings discovered.");
                    }
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] Settings discovery failed: {ex.Message}");
                }
            }

            // Give the debug overlay a reference to ModPlatform for status display
            if (_debugOverlay != null)
            {
                _debugOverlay.SetModPlatform(_modPlatform);
            }
        }

        private void OnDestroy()
        {
            // This should NOT normally fire since we use HideAndDontSave.
            // If it does, log it clearly.
            _log?.LogWarning("[RuntimeDriver] OnDestroy called - persistent root was destroyed!");

            try
            {
                _modPlatform?.Shutdown();
            }
            catch (Exception ex)
            {
                _log?.LogWarning($"[RuntimeDriver] ModPlatform shutdown error: {ex.Message}");
            }
        }
    }
}
