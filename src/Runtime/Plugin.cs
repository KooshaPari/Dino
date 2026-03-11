#nullable enable
using System;
using System.Collections;
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
    /// </summary>
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource Log = null!;
        private Harmony? _harmony;
        private ModPlatform? _modPlatform;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"DINOForge Runtime v{PluginInfo.VERSION} loading...");

            DontDestroyOnLoad(gameObject);

            // Config for debug features (kept from original)
            ConfigEntry<bool> dumpOnStartup = Config.Bind("Debug", "DumpOnStartup", true,
                "Automatically dump entity/component data when the game loads");
            ConfigEntry<string> dumpOutputPath = Config.Bind("Debug", "DumpOutputPath",
                Path.Combine(Paths.BepInExRootPath, "dinoforge_dumps"),
                "Directory to write entity/component dump files");

            // Detect game
            try
            {
                Log.LogInfo($"Unity version: {Application.unityVersion}");
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

            // Initialize the ModPlatform orchestrator
            try
            {
                _modPlatform = new ModPlatform();
                _modPlatform.Initialize(Logger, Config, gameObject);
                Log.LogInfo("[Plugin] ModPlatform initialized.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] ModPlatform initialization failed: {ex.Message}");
                _modPlatform = null;
            }

            // Add MainThreadDispatcher for IPC bridge support
            try
            {
                gameObject.AddComponent<Bridge.MainThreadDispatcher>();
                Log.LogInfo("[Plugin] MainThreadDispatcher added.");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] MainThreadDispatcher setup failed: {ex.Message}");
            }

            // Add UI components to the plugin GameObject
            try
            {
                ModMenuOverlay menuOverlay = gameObject.AddComponent<ModMenuOverlay>();
                ModSettingsPanel settingsPanel = gameObject.AddComponent<ModSettingsPanel>();

                if (_modPlatform != null)
                {
                    _modPlatform.SetUI(menuOverlay, settingsPanel);
                }

                Log.LogInfo("[Plugin] UI components added (F10 for mod menu).");
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] UI component setup failed: {ex.Message}");
            }

            // Start coroutine to wait for ECS world, then load packs and start hot reload
            try
            {
                StartCoroutine(WaitForWorldAndLoad(dumpOnStartup.Value, dumpOutputPath.Value));
            }
            catch (Exception ex)
            {
                Log.LogError($"[Plugin] Failed to start world-wait coroutine: {ex.Message}");
            }

            WriteDebug("Awake completed");
            Log.LogInfo("DINOForge Runtime loaded successfully.");
        }

        /// <summary>
        /// Coroutine that waits for the ECS World to become available, then
        /// triggers pack loading, ECS system registration, and hot reload.
        /// </summary>
        private IEnumerator WaitForWorldAndLoad(bool dumpOnStartup, string dumpOutputPath)
        {
            Log.LogInfo("[Plugin] Waiting for ECS World...");

            // Wait until the default world is created and available
            yield return new WaitUntil(() =>
            {
                try
                {
                    World? world = World.DefaultGameObjectInjectionWorld;
                    return world != null && world.IsCreated;
                }
                catch
                {
                    return false;
                }
            });

            World ecsWorld = World.DefaultGameObjectInjectionWorld;
            Log.LogInfo($"[Plugin] ECS World available: {ecsWorld.Name}");

            // Register DumpSystem if configured
            if (dumpOnStartup)
            {
                try
                {
                    DumpSystem.Configure(Log, dumpOutputPath);
                    ecsWorld.GetOrCreateSystem<DumpSystem>();
                    Log.LogInfo("[Plugin] DumpSystem registered in default world.");
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"[Plugin] DumpSystem registration failed: {ex.Message}");
                }
            }

            // Notify ModPlatform that the world is ready
            if (_modPlatform != null)
            {
                try
                {
                    _modPlatform.OnWorldReady(ecsWorld);
                    Log.LogInfo("[Plugin] ModPlatform notified of world readiness.");
                }
                catch (Exception ex)
                {
                    Log.LogError($"[Plugin] ModPlatform.OnWorldReady failed: {ex.Message}");
                }

                // Load packs
                try
                {
                    ContentLoadResult result = _modPlatform.LoadPacks();
                    Log.LogInfo($"[Plugin] Pack loading complete: success={result.IsSuccess}, " +
                        $"loaded={result.LoadedPacks.Count}, errors={result.Errors.Count}");
                }
                catch (Exception ex)
                {
                    Log.LogError($"[Plugin] Pack loading failed: {ex.Message}");
                }

                // Start hot reload
                try
                {
                    _modPlatform.StartHotReload();
                    Log.LogInfo("[Plugin] Hot reload started.");
                }
                catch (Exception ex)
                {
                    Log.LogError($"[Plugin] Hot reload startup failed: {ex.Message}");
                }

                // Discover settings for the settings panel
                try
                {
                    ModSettingsPanel? settingsPanel = gameObject.GetComponent<ModSettingsPanel>();
                    if (settingsPanel != null)
                    {
                        settingsPanel.DiscoverSettings();
                        Log.LogInfo("[Plugin] Mod settings discovered.");
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"[Plugin] Settings discovery failed: {ex.Message}");
                }
            }
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
            // Shutdown the mod platform
            try
            {
                _modPlatform?.Shutdown();
            }
            catch (Exception ex)
            {
                Log?.LogWarning($"[Plugin] ModPlatform shutdown error: {ex.Message}");
            }

            _harmony?.UnpatchSelf();
            Log?.LogInfo("DINOForge Runtime unloaded.");
            WriteDebug("OnDestroy called");
        }
    }
}
