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
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.BEPINEX_VERSION)]
    public partial class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource Log = null!;
        private Harmony? _harmony;

        internal static ConfigEntry<bool>? _showOverlayOnStart;
        internal static ConfigEntry<string>? _graphicsTier;
        internal static Graphics.GraphicsMode? _graphicsMode;
        internal static ConfigEntry<bool>? _enableHotReload;
        internal static ConfigEntry<int>? _hmrDebounceMs;

        // Static constructor fires BEFORE Awake — probe entry point
        static Plugin()
        {
            try
            {
                string debugLog = Path.Combine(Paths.BepInExRootPath, "dinoforge_debug.log");
                File.AppendAllText(debugLog, $"[{DateTime.UtcNow:o}] [STATIC] Plugin class referenced\n"); // unbounded-log-ok: static ctor fires once per AppDomain load; one-shot probe before DebugLog is initialized — Pattern #232
            }
            catch { } // safe-swallow: diagnostic only
        }

        /// <summary>
        /// The persistent GameObject that survives scene changes.
        /// All UI and runtime components live here, NOT on the BepInEx-managed gameObject.
        /// </summary>
        internal static GameObject? PersistentRoot;

        // Captured at Awake for SceneManager resurrection callback
        private static ManualLogSource? _resurrectionLog;
        private static ConfigFile? _resurrectionConfig;
        private static bool _resurrectionDump;
        private static string _resurrectionDumpPath = "";

        /// <summary>
        /// iter-149e: true once Plugin.Awake captured the resurrection parameters (_resurrectionLog /
        /// _resurrectionConfig). Until then, a direct main-thread TryResurrect would NPE on those.
        /// KeyInputSystem.OnCreate (a DINO-driven main-thread callback that fires when the MainMenu
        /// ECS world is created — proven to fire post-teardown) checks this before reviving directly.
        /// </summary>
        internal static bool ResurrectionParamsReady => _resurrectionLog != null && _resurrectionConfig != null;


        /// <summary>Flag set by KeyInputSystem when F9 is pressed during ECS tick.</summary>
        internal static volatile bool PendingF9Toggle;

        /// <summary>Flag set by KeyInputSystem when F10 is pressed during ECS tick.</summary>
        internal static volatile bool PendingF10Toggle;

        /// <summary>Flag indicating PersistentRoot needs resurrection.</summary>
        internal static volatile bool NeedsResurrection;

        /// <summary>Number of consecutive resurrection attempts since last successful resurrection.</summary>
        private static int _resurrectionAttempts;

        /// <summary>Maximum consecutive resurrection attempts before giving up (SPEC-004 KIS-NF4).</summary>
        private const int MaxResurrectionAttempts = 3;

        /// <summary>
        /// Iter-144 #543 fix: Companion flag set by RuntimeDriver.OnDestroy BEFORE any teardown work.
        /// Resurrection check OR's this with NeedsResurrection to avoid the Unity fake-null trap
        /// (PersistentRoot field may hold a destroyed-but-not-nulled reference where `== null`
        /// returns true via Unity's operator overload but ReferenceEquals(_, null) returns false,
        /// causing the resurrection loop to silently skip).
        /// </summary>
        internal static volatile bool s_rootJustDestroyed;

        /// <summary>
        /// Iter-144 #543 fix: Set when RuntimeDriver.OnDestroy fires during a scene transition so
        /// AssetSwapSystem.OnDestroy can skip its bundle-unload (bundles must survive scene
        /// transitions; unloading mid-swap orphans chicken-sprite placeholders).
        /// </summary>
        internal static volatile bool s_skipBundleUnload;

        // Deferred TryResurrect: set by OnSceneLoaded or KeyInputSystem.OnCreate when a scene
        // transition or ECS world creation is detected. Checked by the background polling thread
        // which runs AFTER Plugin.Awake() completes. This prevents TryResurrect from racing
        // with Plugin.Awake() on a new RuntimeDriver.
        internal static volatile bool NeedsDeferredResurrection;
        internal static string? LastSceneNameForResurrection;

        /// <summary>
        /// Static singleton bridge server that survives RuntimeDriver destruction.
        /// Created once, thread owned by Plugin class (not by any MonoBehaviour).
        /// </summary>
        internal static Bridge.GameBridgeServer? SharedBridgeServer;


        private void Awake()
        {
            Log = Logger;
            Log.LogInfo("[DINOForge] Plugin.Awake() ENTRY");
            Log.LogInfo($"DINOForge Runtime v{PluginInfo.VERSION} loading...");

            // Config for debug features
            ConfigEntry<bool> dumpOnStartup = Config.Bind("Debug", "DumpOnStartup", true,
                "Automatically dump entity/component data when the game loads");
            ConfigEntry<string> dumpOutputPath = Config.Bind("Debug", "DumpOutputPath",
                Path.Combine(Paths.BepInExRootPath, "dinoforge_dumps"),
                "Directory to write entity/component dump files");

            // DINOForge platform settings (exposed in BepInEx ConfigurationManager)
            ConfigEntry<bool> showOverlayOnStart = Config.Bind("General", "ShowDebugOverlayOnStart", false,
                "Show F9 debug overlay automatically when the game starts");
            ConfigEntry<bool> enableHotReload = Config.Bind("General", "EnableHotReload", true,
                "Watch pack files for changes and reload automatically (15s debounce)");
            ConfigEntry<int> hmrDebounceMs = Config.Bind("General", "HotReloadDebounceMs", 15000,
                new ConfigDescription("Milliseconds to wait after a file change before triggering reload",
                    new AcceptableValueRange<int>(500, 60000)));
            ConfigEntry<string> logLevel = Config.Bind("General", "LogLevel", "Info",
                new ConfigDescription("Logging verbosity for DINOForge runtime",
                    new AcceptableValueList<string>("Debug", "Info", "Warning", "Error")));

            // Realistic-GFX mode (Tier-B Phase-1 PoC). OFF by default. When "High", DINOForge injects a
            // URP post-processing Volume (ACES tonemap + bloom + color grading + vignette) onto the
            // active camera for a more cinematic, less TABS-flat look. See Graphics/GraphicsMode.cs and
            // docs/sessions/realistic-gfx-mode-rnd-20260530.md.
            ConfigEntry<string> graphicsTier = Config.Bind("Graphics", "Tier", "Vanilla",
                new ConfigDescription("Visual fidelity tier: Vanilla (no change) or High (cinematic post-processing).",
                    new AcceptableValueList<string>("Vanilla", "High")));
            _graphicsTier = graphicsTier;

            _showOverlayOnStart = showOverlayOnStart;
            _enableHotReload = enableHotReload;
            _hmrDebounceMs = hmrDebounceMs;

            // Session recorder (#971): record a REAL user playthrough (pointer + key + EventSystem
            // widget + screen frames) for in-process replay (#972) and journey embeds (#966).
            ConfigEntry<bool> recorderEnabled = Config.Bind("SessionRecorder", "Enabled", true,
                "Enable the F11 session recorder (records real user input + frames for replay/vision-verify)");
            ConfigEntry<int> recorderFrameMs = Config.Bind("SessionRecorder", "FrameIntervalMs", 500,
                new ConfigDescription("Periodic screen-frame cadence while recording (ms)",
                    new AcceptableValueRange<int>(100, 5000)));
            ConfigEntry<bool> recorderPerEvent = Config.Bind("SessionRecorder", "CaptureFramePerEvent", true,
                "Also capture a screen frame on every pointer down/up event");

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
                Log.LogWarning($"Version detection failed: {ex}");
            }

            // Harmony — apply patches from this assembly
            // ModsButtonTextPatch (UI/UiGridHarmonyPatch.cs) intercepts Text/TMP_Text setters
            // to prevent DINO's UiGrid from overwriting our repurposed Mods button label.
            try
            {
                _harmony = new Harmony(PluginInfo.GUID);
                Bridge.DestroyGuardPatch.Apply(_harmony);

                // iter-149c: The iter-144 H7/H8/H9 DIAGNOSTIC probes (Resources.UnloadUnusedAssets,
                // AssetBundle.Unload/LoadFromFile, SceneManager.UnloadSceneAsync, World.Dispose) are
                // Harmony Prefix/Postfix patches on dispose/unload/teardown hot paths. Each prefix calls
                // `new StackTrace()` + synchronous BepInEx logging INSIDE those native calls. During the
                // InitialGameLoader->MainMenu transition, Unity.Entities.World.Dispose() tears down the
                // 45K-entity Default World while Mono is in teardown; a synchronous StackTrace+log there
                // contends the BepInEx log lock / blocks the managed plugin thread mid-dispose — exactly
                // matching the observed wedge (BepInEx's own LogOutput.log freezes at the same instant,
                // recurrence of the iter-144 mono_jit_cleanup gray-freeze). These probes are
                // diagnostics ONLY (no load-bearing functionality) — gate them OFF to test whether the
                // diagnostic probes are themselves causing the World.Dispose wedge. Files are kept intact
                // so the probes can be re-enabled for future native diagnosis. DestroyGuardPatch
                // (protects DINOForge_Root) and ModsButtonTextPatch (engine-UI label) stay ACTIVE.
                const bool EnableDisposeProbes = false;
#pragma warning disable CS0162 // unreachable code (intentional compile-time probe gate)
                if (EnableDisposeProbes)
                {
                    Bridge.ResourcesUnloadGuardPatch.Apply(_harmony);
                    Bridge.AssetBundleUnloadGuardPatch.Apply(_harmony);
                    Bridge.AssetBundleLoadGuardPatch.Apply(_harmony);
                    Bridge.SceneUnloadGuardPatch.Apply(_harmony);
                    Bridge.WorldDisposeGuardPatch.Apply(_harmony);
                }
#pragma warning restore CS0162

                UI.ModsButtonTextPatch.Apply(_harmony);
                Log.LogInfo($"Harmony initialized and patches applied (disposeProbes={EnableDisposeProbes}).");
            }
            catch (Exception ex)
            {
                Log.LogError($"Harmony init/patch failed: {ex}");
            }

            StartCoroutine(DeferredAwake());

            // Create a dedicated persistent GameObject that won't be destroyed.
            // The BepInEx-managed gameObject gets cleaned up during DINO's scene
            // transitions. A separate object with HideAndDontSave survives.
            try
            {
                PersistentRoot = new GameObject("DINOForge_Root");
                PersistentRoot.hideFlags = HideFlags.HideAndDontSave;
                UnityEngine.Object.DontDestroyOnLoad(PersistentRoot);
                Log.LogInfo("[Plugin] Persistent root GameObject created.");

                // EPIC-027: create the themed loading screen as EARLY as possible so it is
                // visible across the game's own InitialGameLoader asset-load window (before
                // RuntimeDriver finishes pack loading). It is faded out on pack-load complete /
                // world-ready / MainMenu. Created here (Awake, main thread) rather than waiting
                // for RuntimeDriver.Initialize's coroutine.
                try
                {
                    string lsPacksDir = System.IO.Path.Combine(BepInEx.Paths.BepInExRootPath, "dinoforge_packs");
                    UI.LoadingScreenController.Create(PersistentRoot, lsPacksDir, Logger);
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"[Plugin] Early LoadingScreenController.Create failed (non-fatal): {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] Failed to create persistent root: {ex}");
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
                Log.LogError($"[Plugin] RuntimeDriver setup failed: {ex}");
            }

            // Realistic-GFX mode (Tier-B Phase-1 PoC). Attach the GraphicsMode component to the
            // persistent root, seed it from config, and re-apply on every scene change (Camera.main is
            // not available until a gameplay/menu scene loads, and DINO's PlayerLoop means we can't rely
            // on Update()). Inert unless Graphics.Tier == "High".
            try
            {
                Graphics.GraphicsMode gfx = PersistentRoot.AddComponent<Graphics.GraphicsMode>();
                gfx.ConfiguredTier = string.Equals(_graphicsTier?.Value, "High", StringComparison.OrdinalIgnoreCase)
                    ? Graphics.GraphicsTier.High
                    : Graphics.GraphicsTier.Vanilla;
                _graphicsMode = gfx;
                SceneManager.activeSceneChanged += (_, __) => _graphicsMode?.Apply();
                gfx.Apply();
                Log.LogInfo($"[Plugin] GraphicsMode attached (tier={gfx.ConfiguredTier}).");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Plugin] GraphicsMode setup failed (non-fatal): {ex.Message}");
            }

            // Capture state for static resurrection callback (kept for emergency use)
            _resurrectionLog = Logger;
            _resurrectionConfig = Config;
            _resurrectionDump = dumpOnStartup.Value;
            _resurrectionDumpPath = dumpOutputPath.Value;

            StartResurrectionWatcher();

            // SPEC-004 Path 2: PlayerLoop.Update injection (preferred F9/F10 path at main menu).
            bool playerLoopInjected = false;
            try
            {
                playerLoopInjected = InjectPlayerLoopUpdate();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Plugin] InjectPlayerLoopUpdate failed: {ex}");
            }

            // Win32 background poll only when PlayerLoop injection failed — both paths use
            // independent edge detection and would double-toggle F9/F10 if both run.
            if (!playerLoopInjected)
            {
                try
                {
                    Bridge.KeyInputSystem.StartKeyPollThread();
                    Log.LogInfo("[Plugin] PlayerLoop injection failed; using background key poll for F9/F10.");
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"[Plugin] StartKeyPollThread failed: {ex}");
                }
            }

            // Session recorder (#971): F11 toggles recording of the real user playthrough.
            // Uses its own PlayerLoop sampler + Win32 F11 bg thread (independent of F9/F10).
            try
            {
                Capture.SessionRecorder.Configure(recorderEnabled.Value, recorderFrameMs.Value, recorderPerEvent.Value);
                Capture.SessionRecorder.Initialize();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Plugin] SessionRecorder init failed: {ex}");
            }

            DebugLog.Write("Plugin", "Awake completed");
            Log.LogInfo("DINOForge Runtime loaded successfully.");
            Log.LogInfo("[DINOForge] Plugin.Awake() EXIT");
        }

        /// <summary>
        /// Defers ECS type discovery until after the first Unity frame so the loading
        /// screen can dismiss before the diagnostic walk starts.
        /// </summary>
        private IEnumerator DeferredAwake()
        {
            yield return null;

            try
            {
                Bridge.EcsTypeDiscovery.DiscoverAndLog();
                Log.LogInfo("[Plugin] ECS type discovery complete - check dinoforge_debug.log for details");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[Plugin] ECS type discovery failed: {ex}");
            }
        }

        private static void LogInstallDiagnostics()
        {
            string loadedAssemblyPath = typeof(Plugin).Assembly.Location;
            string primaryRuntimePath = Path.Combine(Paths.PluginPath, "DINOForge.Runtime.dll");
            string legacyRuntimePath = Path.Combine(Paths.BepInExRootPath, "ecs_plugins", "DINOForge.Runtime.dll");
            string backupRuntimePath = Path.Combine(Paths.PluginPath, "DINOForge.Runtime.dll.bak");

            Log.LogInfo($"[Plugin] Loaded runtime assembly from: {loadedAssemblyPath}");
            DebugLog.Write("Plugin", $"[Plugin] Loaded runtime assembly from: {loadedAssemblyPath}");

            if (File.Exists(legacyRuntimePath))
            {
                string message = $"[Plugin] Legacy runtime copy detected at deprecated path: {legacyRuntimePath}";
                Log.LogWarning(message);
                DebugLog.Write("Plugin", message);
            }

            if (File.Exists(primaryRuntimePath) && File.Exists(legacyRuntimePath))
            {
                string message = $"[Plugin] Duplicate runtime assemblies detected. Primary='{primaryRuntimePath}', Legacy='{legacyRuntimePath}'";
                Log.LogWarning(message);
                DebugLog.Write("Plugin", message);
            }

            if (File.Exists(backupRuntimePath))
            {
                string message = $"[Plugin] Stale runtime backup file detected: {backupRuntimePath}";
                Log.LogWarning(message);
                DebugLog.Write("Plugin", message);
            }

            if (!string.Equals(loadedAssemblyPath, primaryRuntimePath, StringComparison.OrdinalIgnoreCase))
            {
                string message = $"[Plugin] Runtime loaded from non-canonical location. Expected '{primaryRuntimePath}', actual '{loadedAssemblyPath}'";
                Log.LogWarning(message);
                DebugLog.Write("Plugin", message);
            }
        }

        private void OnDestroy()
        {
            // The BepInEx-managed object is being destroyed (expected in DINO).
            // The persistent root and RuntimeDriver continue running independently.
            Log?.LogInfo("[Plugin] BepInEx plugin object OnDestroy (persistent root still alive).");
            try { _harmony?.UnpatchSelf(); } catch (Exception ex) { DebugLog.Write("Plugin", $"OnDestroy Harmony.UnpatchSelf failed: {ex.Message}"); }
            // P0 fix: stop the Win32 F9/F10 polling thread on plugin teardown.
            try { Bridge.KeyInputSystem.StopKeyPollThread(); } catch (Exception ex) { DebugLog.Write("Plugin", $"OnDestroy StopKeyPollThread failed: {ex.Message}"); }
            try { Capture.SessionRecorder.Shutdown(); } catch (Exception ex) { DebugLog.Write("Plugin", $"OnDestroy SessionRecorder.Shutdown failed: {ex.Message}"); }
            // Iter-144 #547 H5 gray-freeze fix: do NOT unsubscribe activeSceneChanged here.
            // The handler is a static method on the Plugin class; the static delegate survives
            // BepInEx Plugin instance destruction. Previously we unsubscribed here, breaking
            // resurrection on second-and-later scene transitions (only the Win32 fallback thread
            // could revive). Keeping the subscription live is the correct behavior — there's
            // no leak because the target is a static method.
            // Harmony unpatch is also deliberately skipped — runtime patches must persist across
            // BepInEx Plugin object death since the actual functionality lives on RuntimeDriver/
            // ModPlatform which outlive this BepInEx wrapper.
            DebugLog.Write("Plugin", "OnDestroy called (BepInEx object only); activeSceneChanged + fallback thread persist by design (iter-144 #547).");
        }
    }

}
