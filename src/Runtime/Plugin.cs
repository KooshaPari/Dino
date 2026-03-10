using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime
{
    /// <summary>
    /// DINOForge Runtime Plugin - the bootstrap entry point for the mod platform.
    /// Loads into DINO via BepInEx ecs_plugins and provides:
    /// - Game version detection
    /// - ECS World introspection
    /// - Entity/component dumping
    /// - System enumeration
    /// - Debug overlay toggle
    /// </summary>
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource Log = null!;
        private Harmony? _harmony;

        // Config entries
        private ConfigEntry<bool> _enableDebugOverlay = null!;
        private ConfigEntry<bool> _dumpOnStartup = null!;
        private ConfigEntry<string> _dumpOutputPath = null!;
        private ConfigEntry<KeyCode> _dumpHotkey = null!;
        private ConfigEntry<KeyCode> _overlayHotkey = null!;

        private EntityDumper? _entityDumper;
        private SystemEnumerator? _systemEnumerator;
        private DebugOverlay? _debugOverlay;
        private bool _showOverlay;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"DINOForge Runtime v{PluginInfo.VERSION} loading...");

            InitConfig();
            DetectGameVersion();
            InitHarmony();

            _entityDumper = new EntityDumper(Log, _dumpOutputPath.Value);
            _systemEnumerator = new SystemEnumerator(Log);
            _debugOverlay = new DebugOverlay();

            if (_dumpOnStartup.Value)
            {
                // Delay dump to let the game finish loading
                Invoke(nameof(RunStartupDump), 5f);
            }

            Log.LogInfo("DINOForge Runtime loaded successfully.");
        }

        private void InitConfig()
        {
            _enableDebugOverlay = Config.Bind(
                "Debug", "EnableOverlay", false,
                "Show the DINOForge debug overlay in-game");

            _dumpOnStartup = Config.Bind(
                "Debug", "DumpOnStartup", true,
                "Automatically dump entity/component data when the game loads");

            _dumpOutputPath = Config.Bind(
                "Debug", "DumpOutputPath",
                Path.Combine(Paths.BepInExRootPath, "dinoforge_dumps"),
                "Directory to write entity/component dump files");

            _dumpHotkey = Config.Bind(
                "Hotkeys", "DumpKey", KeyCode.F8,
                "Press to dump all entities and components to disk");

            _overlayHotkey = Config.Bind(
                "Hotkeys", "OverlayKey", KeyCode.F9,
                "Press to toggle the debug overlay");
        }

        private void DetectGameVersion()
        {
            try
            {
                string gameExePath = Path.Combine(
                    Application.dataPath, "..",
                    "Diplomacy is Not an Option.exe");

                if (File.Exists(gameExePath))
                {
                    Log.LogInfo($"Game executable found: {gameExePath}");
                }

                Log.LogInfo($"Unity version: {Application.unityVersion}");
                Log.LogInfo($"Platform: {Application.platform}");
                Log.LogInfo($"Data path: {Application.dataPath}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to detect game version: {ex.Message}");
            }
        }

        private void InitHarmony()
        {
            try
            {
                _harmony = new Harmony(PluginInfo.GUID);
                // We don't patch anything yet - Harmony is available for future use
                // Prefer ECS-native approaches over Harmony patches (ADR-005)
                Log.LogInfo("Harmony initialized (no patches applied - ECS-native preferred).");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to initialize Harmony: {ex.Message}");
            }
        }

        private void RunStartupDump()
        {
            Log.LogInfo("Running startup entity dump...");
            _entityDumper?.DumpAll();
            _systemEnumerator?.EnumerateAll();
        }

        private void Update()
        {
            // Hotkey: dump entities
            if (Input.GetKeyDown(_dumpHotkey.Value))
            {
                Log.LogInfo("Manual dump triggered via hotkey.");
                _entityDumper?.DumpAll();
                _systemEnumerator?.EnumerateAll();
            }

            // Hotkey: toggle overlay
            if (Input.GetKeyDown(_overlayHotkey.Value))
            {
                _showOverlay = !_showOverlay;
                Log.LogInfo($"Debug overlay: {(_showOverlay ? "ON" : "OFF")}");
            }
        }

        private void OnGUI()
        {
            if (_showOverlay && _debugOverlay != null)
            {
                _debugOverlay.Draw();
            }
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();
            Log.LogInfo("DINOForge Runtime unloaded.");
        }
    }
}
