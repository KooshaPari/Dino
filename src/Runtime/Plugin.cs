using System;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime
{
    [BepInPlugin(PluginInfo.GUID, PluginInfo.NAME, PluginInfo.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private static ManualLogSource Log = null!;
        private Harmony? _harmony;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"DINOForge Runtime v{PluginInfo.VERSION} loading...");

            DontDestroyOnLoad(gameObject);

            // Config
            var dumpOnStartup = Config.Bind("Debug", "DumpOnStartup", true,
                "Automatically dump entity/component data when the game loads");
            var dumpOutputPath = Config.Bind("Debug", "DumpOutputPath",
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

            // Register ECS DumpSystem - survives MonoBehaviour destruction
            if (dumpOnStartup.Value)
            {
                DumpSystem.Configure(Log, dumpOutputPath.Value);
                try
                {
                    // Try to register in default world
                    World? world = World.DefaultGameObjectInjectionWorld;
                    if (world != null && world.IsCreated)
                    {
                        world.GetOrCreateSystem<DumpSystem>();
                        Log.LogInfo("DumpSystem registered in default world.");
                    }
                    else
                    {
                        Log.LogWarning("Default world not ready yet - DumpSystem will try all worlds.");
                        // Try any existing world
                        foreach (World w in World.All)
                        {
                            if (w.IsCreated)
                            {
                                w.GetOrCreateSystem<DumpSystem>();
                                Log.LogInfo($"DumpSystem registered in world: {w.Name}");
                                break;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Could not register DumpSystem in ECS: {ex.Message}");
                    // Fallback: thread-based dump
                    string outputDir = dumpOutputPath.Value;
                    var thread = new System.Threading.Thread(() =>
                    {
                        try
                        {
                            System.Threading.Thread.Sleep(15000); // Wait 15s
                            WriteDebug("Thread-based dump firing");
                            var dumper = new EntityDumper(Log, outputDir);
                            dumper.DumpAll();
                            var sysEnum = new SystemEnumerator(Log);
                            sysEnum.EnumerateAll();
                        }
                        catch (Exception tex)
                        {
                            WriteDebug($"Thread dump failed: {tex}");
                        }
                    });
                    thread.IsBackground = true;
                    thread.Start();
                    Log.LogInfo("Fallback thread-based dump scheduled (15s delay).");
                }
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
            _harmony?.UnpatchSelf();
            Log.LogInfo("DINOForge Runtime unloaded.");
            WriteDebug("OnDestroy called");
        }
    }
}
