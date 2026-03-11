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
    /// </summary>
    internal class RuntimeDriver : MonoBehaviour
    {
        private ManualLogSource _log = null!;
        private ConfigFile _config = null!;
        private bool _dumpOnStartup;
        private string _dumpOutputPath = "";

        private ModPlatform? _modPlatform;

        // UGUI system (preferred)
        private DFCanvas? _dfCanvas;

        // IMGUI fallback (kept alive in case UGUI setup fails)
        private ModMenuOverlay? _modMenuOverlay;
        private ModSettingsPanel? _modSettingsPanel;
        private DebugOverlayBehaviour? _debugOverlay;
        private HudIndicator? _hudIndicator;

        private bool _worldFound;
        private bool _initialized;
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
                _log.LogInfo("[RuntimeDriver] MainThreadDispatcher added.");
            }
            catch (Exception ex)
            {
                _log.LogError($"[RuntimeDriver] MainThreadDispatcher setup failed: {ex.Message}");
            }

            // Attempt to set up the UGUI canvas system (preferred).
            // Falls back to legacy IMGUI components if setup fails.
            bool uguiReady = false;
            try
            {
                _dfCanvas = gameObject.AddComponent<DFCanvas>();
                _dfCanvas.Initialize(_log);

                // ModMenuPanel exposes the same API as ModMenuOverlay.
                // ModPlatform.SetUI still expects ModMenuOverlay + ModSettingsPanel.
                // We wire the UGUI panel callbacks manually to keep ModPlatform untouched.
                if (_dfCanvas.ModMenuPanel != null && _modPlatform != null)
                {
                    // Wire reload callback — triggers pack reload and shows HUD strip toast.
                    _dfCanvas.ModMenuPanel.OnReloadRequested = () => _modPlatform?.LoadPacks();

                    // Use a legacy ModMenuOverlay shim for ModPlatform.SetUI typing.
                    // We create a disabled IMGUI overlay purely as a typed carrier;
                    // it will never render (no F10 key handler in it once DFCanvas owns F10).
                    _modMenuOverlay = gameObject.AddComponent<ModMenuOverlay>();
                    _modMenuOverlay.enabled = false; // disable Update/OnGUI

                    _modSettingsPanel = gameObject.AddComponent<ModSettingsPanel>();
                    _modSettingsPanel.enabled = false;

                    // Route ModPlatform events back to the UGUI panel
                    _modMenuOverlay.OnReloadRequested = () =>
                        _dfCanvas?.ModMenuPanel?.OnReloadRequested?.Invoke();
                    _modMenuOverlay.OnPackToggled = (id, enabled) =>
                        _dfCanvas?.ModMenuPanel?.OnPackToggled?.Invoke(id, enabled);

                    // ModPlatform calls SetPacks/SetStatus on _modMenuOverlay.
                    // We subclass those by forwarding via an override in the shim.
                    // Since ModMenuOverlay is not abstract, we use a thin proxy wrapper.
                    ModMenuOverlayProxy proxy = gameObject.AddComponent<ModMenuOverlayProxy>();
                    proxy.enabled = false;
                    proxy.SetTarget(_dfCanvas.ModMenuPanel);

                    // Replace the shim with the proxy for ModPlatform wiring
                    Destroy(_modMenuOverlay); // remove the duplicate
                    _modMenuOverlay = proxy;

                    _modPlatform.SetUI(_modMenuOverlay, _modSettingsPanel);
                }

                uguiReady = true;
                _log.LogInfo("[RuntimeDriver] UGUI DFCanvas initialized (F9 debug, F10 mod menu).");
            }
            catch (Exception ex)
            {
                _log.LogWarning($"[RuntimeDriver] UGUI setup failed, falling back to IMGUI: {ex.Message}");
                uguiReady = false;

                if (_dfCanvas != null)
                {
                    Destroy(_dfCanvas);
                    _dfCanvas = null;
                }
            }

            // IMGUI fallback — only activated if UGUI failed
            if (!uguiReady)
            {
                try
                {
                    _modMenuOverlay   = gameObject.AddComponent<ModMenuOverlay>();
                    _modSettingsPanel = gameObject.AddComponent<ModSettingsPanel>();
                    _debugOverlay     = gameObject.AddComponent<DebugOverlayBehaviour>();

                    // Wire settings panel into mod menu so its inline Settings button works
                    _modMenuOverlay.SetSettingsPanel(_modSettingsPanel);

                    if (_modPlatform != null)
                    {
                        _modPlatform.SetUI(_modMenuOverlay, _modSettingsPanel);
                    }

                    _log.LogInfo("[RuntimeDriver] IMGUI fallback UI added (F9 debug, F10 mod menu).");
                }
                catch (Exception ex)
                {
                    _log.LogError($"[RuntimeDriver] IMGUI fallback UI setup failed: {ex.Message}");
                }
            }

            // HUD indicator — only used in IMGUI fallback mode.
            // When UGUI is active, DFCanvas.HudStrip provides equivalent functionality.
            if (!uguiReady)
            {
                try
                {
                    _hudIndicator = gameObject.AddComponent<HudIndicator>();
                    _hudIndicator.SetModMenu(_modMenuOverlay);

                    if (_modMenuOverlay != null)
                    {
                        _modMenuOverlay.OnReloadRequested += () => _hudIndicator?.ShowToast("Packs reloaded");
                    }

                    _log.LogInfo("[RuntimeDriver] HudIndicator added (IMGUI mode).");
                }
                catch (Exception ex)
                {
                    _log.LogWarning($"[RuntimeDriver] HudIndicator setup failed: {ex.Message}");
                }
            }

            _log.LogInfo("[RuntimeDriver] Waiting for ECS World (Update polling)...");
        }

        private void Update()
        {
            if (!_initialized || _worldFound) return;

            // Poll for ECS world on a timer instead of every frame
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
