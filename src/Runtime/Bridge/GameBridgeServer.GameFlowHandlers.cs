#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using DINOForge.Bridge.Protocol;
using DINOForge.Runtime.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using RuntimeDriver = DINOForge.Runtime.RuntimeDriver;

namespace DINOForge.Runtime.Bridge
{
    public sealed partial class GameBridgeServer
    {
        private JToken HandleLoadScene(JObject? parameters)
        {
            string sceneName = parameters?.Value<string>("scene") ?? "level0";
            // JToken.Value<int> returns 0 when the key is absent — treat missing buildIndex as unset.
            int buildIndex = -1;
            if (parameters?["buildIndex"] is JToken buildIndexToken
                && buildIndexToken.Type != JTokenType.Null)
            {
                buildIndex = buildIndexToken.Value<int>();
            }

            // If scene is purely numeric, treat as build index
            if (buildIndex < 0 && int.TryParse(sceneName, out int parsed))
                buildIndex = parsed;

            var loadResult = MainThreadDispatcher.RunOnMainThread(() =>
            {
                int count = SceneManager.sceneCountInBuildSettings;
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] LoadScene: buildIndex={buildIndex} sceneName={sceneName} totalScenes={count}");
                try
                {
                    if (buildIndex >= 0)
                        SceneManager.LoadScene(buildIndex);
                    else
                        SceneManager.LoadScene(sceneName);
                    return new { success = true, sceneCount = count };
                }
                catch (Exception ex)
                {
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] LoadScene failed: {ex.Message}");
                    return new { success = false, sceneCount = count };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool timedOut = !loadResult.Wait(MainThreadWaitTimeoutMs);
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool success = !timedOut && loadResult.Result.success;
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            int sceneCount = timedOut ? -1 : loadResult.Result.sceneCount;

            return JToken.FromObject(new { success, scene = sceneName, buildIndex, sceneCount });
        }

        /// <summary>
        /// Drives the scripted main-menu → skirmish/gameplay UI sequence in-process via
        /// <see cref="Capture.NavigationScripter"/>, capturing a verification frame at each step.
        /// This composes the EventSystem pointer driver (#972) + reliable FrameCapture (#980) into
        /// the missing multi-step flow that actually reaches the gameplay camera. Params:
        /// { plan?: "skirmish" (default), screenshotDir?: string, finalShot?: string }.
        /// </summary>
        private JToken HandleNavigateToGameplay(JObject? parameters)
        {
            string planName = parameters?.Value<string>("plan") ?? "skirmish";
            string screenshotDir = parameters?.Value<string>("screenshotDir")
                ?? Path.Combine(BepInEx.Paths.BepInExRootPath, "screenshots", "nav");
            string? finalShot = parameters?.Value<string>("finalShot");

            Capture.NavigationScripter.Plan plan = Capture.NavigationScripter.SkirmishPlan();
            plan.Name = planName;

            // The scripter blocks on per-step waits/captures; allow a long ceiling for the
            // gameplay-world load (DINO can take tens of seconds to spin up a level).
            const int NavigationWaitTimeoutMs = 120000;
            var navTask = MainThreadDispatcher.RunOnMainThread(() =>
                Capture.NavigationScripter.Run(plan, screenshotDir, finalShot));

            // sync-over-async-unavoidable: ECS/EventSystem-bound, main-thread-required
            if (!navTask.Wait(NavigationWaitTimeoutMs))
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleNavigateToGameplay timed out ({NavigationWaitTimeoutMs}ms)");
                return JToken.FromObject(new NavigationResult
                {
                    Success = false,
                    Plan = planName,
                    Message = $"Navigation timed out after {NavigationWaitTimeoutMs}ms.",
                    FinalState = "timeout",
                });
            }

            // sync-over-async-unavoidable: ECS/EventSystem-bound, main-thread-required
            return JToken.FromObject(navTask.Result);
        }

        private JToken HandleWaitForWorld(JObject? parameters)
        {
            int timeoutMs = parameters?.Value<int?>("timeoutMs") ?? 30000;

            DateTimeOffset deadline = _timeProvider.GetUtcNow().AddMilliseconds(timeoutMs);
            while (_timeProvider.GetUtcNow() < deadline && _running)
            {
                if (IsPlatformAlive && _platform.IsWorldReady)
                {
                    string worldName = "";
                    try
                    {
                        // Task #535: bounded wait. The world is "ready" but the dispatcher pump
                        // can still be dead during scene transition — fall through to empty name.
                        // sync-over-async-unavoidable: ECS-bound, main-thread-required
                        var wnTask = MainThreadDispatcher.RunOnMainThread(() =>
                        {
                            World? world = GetActiveWorld();
                            return world?.Name ?? "";
                        });
                        // sync-over-async-unavoidable: ECS-bound, main-thread-required
                        if (wnTask.Wait(MainThreadWaitTimeoutMs))
                        {
                            // sync-over-async-unavoidable: ECS-bound, main-thread-required
                            worldName = wnTask.Result;
                        }
                        else
                        {
                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleWaitForWorld world-name read timed out ({MainThreadWaitTimeoutMs}ms) — dispatcher pump may be dead");
                        }
                    }
                    catch (Exception ex) { DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleWaitForWorld world-name read failed: {ex}"); }

                    WaitResult readyResult = new WaitResult
                    {
                        Ready = true,
                        WorldName = worldName
                    };
                    return JToken.FromObject(readyResult);
                }

                // sync-over-async-unavoidable: ManualResetEventSlim signal wait with ShutdownPollIntervalMs bounded timeout (not a Task)
                if (_shutdownEvent.Wait(ShutdownPollIntervalMs))  // Signaled = shutdown
                    break;
            }

            WaitResult timeoutResult = new WaitResult
            {
                Ready = false,
                WorldName = ""
            };
            return JToken.FromObject(timeoutResult);
        }

        private JToken HandleStartGame(JObject? parameters)
        {
            string saveName = parameters?.Value<string>("saveName") ?? "";

            // GATED OFF (regression fix, reconcile3 2026-05-31):
            // Previously this RPC created a bare `Components.SingletonComponents.BeginGameWorldLoadingSingleton`
            // ECS entity with EMPTY/unpopulated fields to programmatically trigger world-load. DINO's native
            // Systems.GameWorldLoaderSystem.OnUpdate then calls GetSingleton<that type>() expecting its managed
            // component (save path / map id / load params) populated, gets null/empty, and throws a
            // NullReferenceException EVERY FRAME in InitializationSystemGroup — flooding the log and breaking
            // world-load. The bare-entity creation MUST NOT happen. This is an experimental nav-scripter trigger,
            // NOT core functionality; the nav/record-session paths do not depend on it. Normal menu-driven
            // world-load (player clicking through the menu) populates the singleton correctly and is unaffected.
            //
            // To re-enable programmatic world-load, populate ALL required fields of the singleton with valid
            // values BEFORE creating the entity (see Option B in the regression ticket). Until then this is a
            // no-op so we never corrupt DINO's native loader.
            DebugLog.Write("GameBridgeServer",
                $"[GameBridgeServer] startGame RPC is GATED OFF (no-op) to avoid corrupting native GameWorldLoaderSystem; " +
                $"saveName='{saveName}'. Load a world through the in-game menu instead.");

            return JToken.FromObject(new
            {
                success = false,
                disabled = true,
                message = "startGame RPC is disabled: programmatic BeginGameWorldLoadingSingleton creation broke "
                          + "DINO's native GameWorldLoaderSystem (NRE/frame). Use the in-game menu to load a world."
            });
        }

        private JToken HandleDismissLoadScreen()
        {
            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // DINO's loading screen uses UI.LoadingProgressBar which has a _startAction field
                    // (a UnityAction) that gets invoked when the player presses any key.
                    var allMBs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                    string found = "";
                    foreach (var mb in allMBs)
                    {
                        if (mb == null) continue;
                        string tName = mb.GetType().Name;
                        // event-lifecycle-ok: local string accumulator for diagnostic dump, not an event subscription
                        found += $"[{tName}]";

                        // Target: UI.LoadingProgressBar
                        if (tName == "LoadingProgressBar")
                        {
                            // Try _startAction field (UnityAction)
                            FieldInfo? startField = mb.GetType().GetField("_startAction",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (startField != null)
                            {
                                object? action = startField.GetValue(mb);
                                if (action is UnityEngine.Events.UnityAction ua)
                                {
                                    ua.Invoke();
                                    return new { success = true, message = $"Invoked _startAction on LoadingProgressBar" };
                                }
                                // Try invoking as delegate
                                if (action is System.Delegate del)
                                {
                                    del.DynamicInvoke();
                                    return new { success = true, message = $"DynamicInvoked _startAction on LoadingProgressBar" };
                                }
                                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] _startAction type: {(action?.GetType().Name ?? "null")}");
                            }

                            // Fallback: call Update() to simulate time passing with anyKeyDown
                            // Actually try GetComponent on the progress GameObject
                            FieldInfo? progressField = mb.GetType().GetField("_progress",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            if (progressField != null)
                            {
                                UnityEngine.GameObject? progressGO = progressField.GetValue(mb) as UnityEngine.GameObject;
                                if (progressGO != null)
                                    progressGO.SetActive(false); // hide progress bar panel
                            }

                            // Try destroying the component to let the scene proceed
                            return new { success = false, message = $"LoadingProgressBar found but _startAction invoke failed. Action type: {startField?.GetValue(mb)?.GetType().Name ?? "null"}" };
                        }
                    }

                    return new { success = false, message = $"No dismiss handler found. MBs: {found}" };
                }
                catch (Exception ex)
                {
                    // Pattern #104 (Task #302): preserve type info in wire message + full stack in log.
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] DismissDialog handler failed: {ex}");
                    return new { success = false, message = $"{ex.GetType().Name}: {ex.Message}" };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            return JToken.FromObject(result.Result);
        }

        private JToken HandleListSaves()
        {
            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Find save manager via reflection
                    Type? saveManagerType = null;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        foreach (string typeName in new[] {
                            "Systems.SaveLoadSystem", "Systems.GameWorldLoaderSystem",
                            "Systems.Save.SaveSystem", "Systems.SaveSystem",
                            "SaveManager", "SaveSystem"
                        })
                        {
                            saveManagerType = asm.GetType(typeName);
                            if (saveManagerType != null) break;
                        }
                        if (saveManagerType != null) break;
                    }

                    // Search for saves in DNO's actual paths
                    string persistPath = Application.persistentDataPath;
                    var saves = new List<string>();

                    // DINO saves: persistentDataPath/DNOPersistentData/<branch>/
                    string dnoDataDir = System.IO.Path.Combine(persistPath, "DNOPersistentData");
                    string saveDir = dnoDataDir;
                    if (System.IO.Directory.Exists(dnoDataDir))
                    {
                        foreach (string branchDir in System.IO.Directory.GetDirectories(dnoDataDir))
                        {
                            string branchName = System.IO.Path.GetFileName(branchDir);
                            foreach (var f in System.IO.Directory.GetFiles(branchDir, "*.dat"))
                                saves.Add($"{branchName}/{System.IO.Path.GetFileNameWithoutExtension(f)}");
                        }
                    }
                    else
                    {
                        // Fallback to standard Saves dir
                        saveDir = System.IO.Path.Combine(persistPath, "Saves");
                        if (System.IO.Directory.Exists(saveDir))
                        {
                            foreach (var f in System.IO.Directory.GetFiles(saveDir, "*.sav"))
                                saves.Add(System.IO.Path.GetFileNameWithoutExtension(f));
                            foreach (var f in System.IO.Directory.GetFiles(saveDir, "*.dat"))
                                saves.Add(System.IO.Path.GetFileNameWithoutExtension(f));
                        }
                    }

                    return new
                    {
                        saveManagerType = saveManagerType?.FullName ?? "not found",
                        persistentDataPath = persistPath,
                        saveDir = saveDir,
                        saveDirExists = System.IO.Directory.Exists(saveDir),
                        saves = saves,
                        dataPath = Application.dataPath
                    };
                }
                catch (Exception ex)
                {
                    return new
                    {
                        saveManagerType = "error",
                        persistentDataPath = "",
                        saveDir = "",
                        saveDirExists = false,
                        saves = new List<string>(),
                        dataPath = ex.Message
                    };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            if (!completed) return JToken.FromObject(new { error = "Timed out" });
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            return JToken.FromObject(result.Result);
        }

        private JToken HandleLoadSave(JObject? parameters)
        {
            string saveName = parameters?.Value<string>("saveName") ?? "AutoSave_1";

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleLoadSave: '{saveName}'");

                    // Strategy 0: Create a LoadRequest ECS entity — the game's SaveLoadSystem
                    // reads Components.RawComponents.LoadRequest singletons and triggers a load.
                    // Fields: NameToLoad (FixedString128Bytes), FromMenu (Boolean)
                    World? world = GetActiveWorld();
                    if (world != null && world.IsCreated)
                    {
                        Type? loadRequestType = null;
                        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                        {
                            loadRequestType = asm.GetType("Components.RawComponents.LoadRequest");
                            if (loadRequestType != null) break;
                        }

                        if (loadRequestType != null)
                        {
                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Found LoadRequest type: {loadRequestType.FullName}");

                            // Create the component value
                            object loadRequest = System.Activator.CreateInstance(loadRequestType)
                                ?? throw new InvalidOperationException($"Could not create instance of {loadRequestType.FullName}.");

                            // Set NameToLoad — it's a Unity.Collections.FixedString128Bytes
                            FieldInfo? nameField = loadRequestType.GetField("NameToLoad",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                            FieldInfo? fromMenuField = loadRequestType.GetField("FromMenu",
                                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

                            if (nameField != null)
                            {
                                // FixedString128Bytes can be set from a regular string via implicit conversion
                                // We need to box/unbox correctly
                                Type fsType = nameField.FieldType; // Unity.Collections.FixedString128Bytes
                                // Try to create FixedString128Bytes from string
                                try
                                {
                                    // FixedString128Bytes has implicit operator from string in Unity
                                    MethodInfo? op = fsType.GetMethod("op_Implicit",
                                        BindingFlags.Public | BindingFlags.Static,
                                        null, new[] { typeof(string) }, null);
                                    if (op != null)
                                    {
                                        object? fs = op.Invoke(null, new object[] { saveName });
                                        nameField.SetValue(loadRequest, fs);
                                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Set NameToLoad = '{saveName}' via op_Implicit");
                                    }
                                    else
                                    {
                                        // Try ctor with string
                                        System.Reflection.ConstructorInfo? ctor = fsType.GetConstructor(new[] { typeof(string) });
                                        if (ctor != null)
                                        {
                                            object? fs = ctor.Invoke(new object[] { saveName });
                                            nameField.SetValue(loadRequest, fs);
                                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Set NameToLoad via ctor");
                                        }
                                        else
                                        {
                                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] No string ctor or op_Implicit for {fsType.Name}");
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] NameToLoad set failed: {ex.Message}");
                                }
                            }

                            if (fromMenuField != null)
                                fromMenuField.SetValue(loadRequest, true);

                            // Create entity and add LoadRequest component
                            try
                            {
                                ComponentType ct = ComponentType.ReadWrite(loadRequestType);
                                Entity e = world.EntityManager.CreateEntity(ct);

                                // Set the component data via reflection
                                MethodInfo? setComp = typeof(EntityManager).GetMethod("SetComponentData",
                                    BindingFlags.Public | BindingFlags.Instance);
                                if (setComp != null)
                                {
                                    MethodInfo genSet = setComp.MakeGenericMethod(loadRequestType);
                                    genSet.Invoke(world.EntityManager, new object?[] { e, loadRequest });
                                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Created LoadRequest entity {e.Index} with NameToLoad='{saveName}'");
                                    return new { success = true, message = $"Created LoadRequest entity {e.Index} NameToLoad='{saveName}'", foundPath = "" };
                                }
                            }
                            catch (Exception ex)
                            {
                                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] LoadRequest entity creation failed: {ex.Message}");
                            }
                        }
                        else
                        {
                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] LoadRequest type NOT found");
                        }
                    }

                    // Find the save file in DINO's DNOPersistentData structure
                    string persistPath = Application.persistentDataPath;
                    string dnoDataDir = System.IO.Path.Combine(persistPath, "DNOPersistentData");

                    string foundPath = "";
                    if (System.IO.Directory.Exists(dnoDataDir))
                    {
                        foreach (string branchDir in System.IO.Directory.GetDirectories(dnoDataDir))
                        {
                            foreach (string f in System.IO.Directory.GetFiles(branchDir, "*.dat"))
                            {
                                string fn = System.IO.Path.GetFileNameWithoutExtension(f).ToUpperInvariant();
                                string sn = saveName.ToUpperInvariant();
                                if (fn.Contains(sn) || sn.Contains(fn))
                                {
                                    foundPath = f;
                                    break;
                                }
                            }
                            if (!string.IsNullOrEmpty(foundPath)) break;
                        }
                    }

                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Save file found: '{foundPath}'");
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] PersistentDataPath: {persistPath}");

                    // Strategy 3: Find the game's native UI buttons via Unity's UI system
                    // Use Resources.FindObjectsOfTypeAll to find ALL button instances including inactive
                    var allButtons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Found {allButtons.Length} buttons (Resources.FindObjectsOfTypeAll)");

                    // Also try FindObjectsOfType (scene-only)
                    var sceneButtons = UnityEngine.Object.FindObjectsOfType<UnityEngine.UI.Button>();
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Found {sceneButtons.Length} buttons (FindObjectsOfType scene-only)");

                    // Dump ALL GameObjects to find what the menu uses
                    if (allButtons.Length == 0 && sceneButtons.Length == 0)
                    {
                        // Search for any MonoBehaviour with "Click" or "Button" in name
                        var allMBs = UnityEngine.Object.FindObjectsOfType<MonoBehaviour>();
                        var interesting = new System.Text.StringBuilder();
                        foreach (var mb in allMBs)
                        {
                            if (mb == null) continue;
                            string tName = mb.GetType().Name;
                            if (tName.Contains("Button") || tName.Contains("Click") || tName.Contains("Menu") || tName.Contains("Interactable"))
                                interesting.Append($"[{tName}:{mb.gameObject.name}] ");
                        }
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Button-like MonoBehaviours: {interesting}");
                    }

                    var saveNameUpper = saveName.ToUpperInvariant();
                    UnityEngine.UI.Button? targetButton = null;
                    UnityEngine.UI.Button? continueButton = null;
                    UnityEngine.UI.Button? okButton = null;
                    var buttonSummary = new System.Text.StringBuilder();

                    foreach (var btn in allButtons)
                    {
                        if (btn == null) continue;
                        // Skip the DINOForge mods button only
                        if (btn.name == "DINOForge_ModsButton") continue;
                        // Skip inactive
                        if (!btn.gameObject.activeInHierarchy) continue;

                        var txt = btn.GetComponentInChildren<UnityEngine.UI.Text>();
                        var tmptxt = btn.GetComponentInChildren<TMPro.TMP_Text>();
                        string label = (txt?.text ?? tmptxt?.text ?? "").Trim();
                        string btnName = btn.name;
                        buttonSummary.Append($"[{btnName}:'{label}'] ");

                        string labelUpper = label.ToUpperInvariant();
                        string nameUpper = btnName.ToUpperInvariant();

                        if (labelUpper == "OK" && nameUpper == "BUTTON_INTERCEPTED")
                        {
                            // Only capture unnamed "Button" as OK — not named buttons like Continue
                            if (okButton == null) okButton = btn;
                        }
                        string nameBase = btnName.Replace("_intercepted", "").ToUpperInvariant();
                        if (nameBase == "CONTINUE" || labelUpper == "CONTINUE")
                        {
                            continueButton = btn;
                        }
                        if (!string.IsNullOrEmpty(saveNameUpper))
                        {
                            // Match save name against button label or name
                            if (labelUpper.Contains(saveNameUpper) || nameBase.Contains(saveNameUpper))
                            {
                                targetButton = btn;
                            }
                            // Special: if searching for CONTINUE, match the Continue button
                            if (saveNameUpper == "CONTINUE" && (nameBase == "CONTINUE" || labelUpper == "CONTINUE"))
                                targetButton = btn;
                            // Special: if searching for OK or CONFIRM, match the ok button
                            if ((saveNameUpper == "OK" || saveNameUpper == "CONFIRM") && labelUpper == "OK")
                                targetButton = btn;
                            // Match Load buttons: LOAD_1, LOAD buttons by date position
                            if (saveNameUpper.StartsWith("LOAD") && nameBase == "LOAD")
                            {
                                if (targetButton == null) targetButton = btn; // first Load button
                            }
                        }
                    }

                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Active buttons: {buttonSummary}");
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] okButton={okButton?.name ?? "null"} continueButton={continueButton?.name ?? "null"} targetButton={targetButton?.name ?? "null"}");

                    // Priority: explicit name match > CONTINUE > OK fallback
                    UnityEngine.UI.Button? toInvoke = targetButton ?? continueButton ?? okButton;
                    if (toInvoke != null)
                    {
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Invoking button: {toInvoke.name}");
                        // Try ExecuteEvents for proper UI simulation, fall back to onClick.Invoke
                        try
                        {
                            UnityEngine.EventSystems.ExecuteEvents.Execute(
                                toInvoke.gameObject,
                                new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current),
                                UnityEngine.EventSystems.ExecuteEvents.pointerClickHandler);
                        }
                        catch
                        {
                            toInvoke.onClick.Invoke();
                        }
                        return new { success = true, message = $"Invoked button '{toInvoke.name}' (label search: '{saveName}')", foundPath };
                    }

                    return new { success = false, message = $"No suitable button found for '{saveName}'. Active buttons: {buttonSummary}. Save path: '{foundPath}'", foundPath };
                }
                catch (Exception ex)
                {
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleLoadSave failed: {ex.Message}");
                    return new { success = false, message = ex.Message, foundPath = "" };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadHeavyWaitTimeoutMs);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            return JToken.FromObject(result.Result);
        }
    }
}
