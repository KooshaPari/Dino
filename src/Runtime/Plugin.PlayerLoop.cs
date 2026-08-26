#nullable enable
using System;
using BepInEx.Logging;
using DINOForge.Runtime.Diagnostics;
using HarmonyLib;
using UnityEngine;
using UnityEngine.LowLevel;
using UnityEngine.SceneManagement;

namespace DINOForge.Runtime
{
    public partial class Plugin
    {
        /// <summary>
        /// Iter-144 menu-unclickable fix. DINO's MainMenu-scene EventSystem is destroyed during
        /// scene transitions, resetting <c>EventSystem.current</c> to null even when our
        /// DontDestroyOnLoad EventSystem (created by DFCanvas) is still alive in the hierarchy.
        /// Idempotent: re-promotes an existing one if found, otherwise creates a new
        /// DontDestroyOnLoad EventSystem with StandaloneInputModule.
        /// </summary>
        internal static void EnsureEventSystemAlive()
        {
            try
            {
                UnityEngine.EventSystems.EventSystem[] existing = UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
                UnityEngine.EventSystems.EventSystem? preferred = null;
                int activeCount = 0;
                string[] names = new string[existing.Length];

                for (int i = 0; i < existing.Length; i++)
                {
                    UnityEngine.EventSystems.EventSystem? system = existing[i];
                    if (system == null)
                    {
                        names[i] = "NULL";
                        continue;
                    }

                    names[i] = system.gameObject.name;
                    if (system.enabled) activeCount++;
                    if (preferred == null && IsDinoForgeEventSystem(system))
                    {
                        preferred = system;
                    }
                }

                if (preferred == null)
                {
                    if (UnityEngine.EventSystems.EventSystem.current != null &&
                        IsDinoForgeEventSystem(UnityEngine.EventSystems.EventSystem.current))
                    {
                        preferred = UnityEngine.EventSystems.EventSystem.current;
                    }
                }

                if (preferred == null)
                {
                    // None at all — create the authoritative DINOForge EventSystem.
                    var go = new GameObject("DINOForge_EventSystem_Restored");
                    UnityEngine.Object.DontDestroyOnLoad(go);
                    preferred = go.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    go.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    DebugLog.Write("Plugin", "[EventSystem] no scene EventSystem found — created DINOForge_EventSystem_Restored.");
                    existing = UnityEngine.Object.FindObjectsOfType<UnityEngine.EventSystems.EventSystem>();
                    names = new string[existing.Length];
                    for (int i = 0; i < existing.Length; i++)
                    {
                        names[i] = existing[i] != null ? existing[i].gameObject.name : "NULL";
                    }
                }
                else if (preferred.GetComponent<UnityEngine.EventSystems.StandaloneInputModule>() == null)
                {
                    preferred.gameObject.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                }

                if (!preferred.enabled)
                {
                    preferred.enabled = true;
                }

                for (int i = 0; i < existing.Length; i++)
                {
                    UnityEngine.EventSystems.EventSystem? system = existing[i];
                    if (system == null || ReferenceEquals(system, preferred))
                    {
                        continue;
                    }

                    if (system.enabled)
                    {
                        system.enabled = false;
                    }
                }

                if (!ReferenceEquals(UnityEngine.EventSystems.EventSystem.current, preferred))
                {
                    UnityEngine.EventSystems.EventSystem.current = preferred;
                }

                activeCount = 0;
                for (int i = 0; i < existing.Length; i++)
                {
                    UnityEngine.EventSystems.EventSystem? system = existing[i];
                    if (system != null && system.enabled)
                    {
                        activeCount++;
                    }
                }

                string currentName = UnityEngine.EventSystems.EventSystem.current != null
                    ? UnityEngine.EventSystems.EventSystem.current.gameObject.name
                    : "NULL";
                string key = $"{preferred.gameObject.name}|{currentName}|{existing.Length}|{activeCount}";
                if (key != _lastEventSystemReconcileKey)
                {
                    _lastEventSystemReconcileKey = key;
                    DebugLog.Write("Plugin", $"[EventSystem] reconcile: preferred={preferred.gameObject.name}, current={currentName}, total={existing.Length}, enabled={activeCount}, systems=[{string.Join(", ", names)}]");
                }
            }
            catch (Exception ex)
            {
                try { DebugLog.Write("Plugin", $"[EventSystem] ensure failed: {ex.GetType().Name}: {ex.Message}"); } catch { /* safe-swallow */ }
            }
        }

        private static bool IsDinoForgeEventSystem(UnityEngine.EventSystems.EventSystem system)
        {
            return system != null &&
                system.gameObject.name.StartsWith("DINOForge_", StringComparison.Ordinal);
        }

        private static bool _playerLoopHarmonyPatched;
        private static int _playerLoopEventSystemTick;
        private static string? _lastEventSystemReconcileKey;
        private static bool _prevF9;
        private static bool _prevF10;

        [System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint = "GetAsyncKeyState")]
        private static extern short PluginGetAsyncKeyState(int vKey);

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
