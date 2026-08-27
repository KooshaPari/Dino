#nullable enable
using System;
using System.IO;
using System.Reflection;
using DINOForge.Bridge.Protocol;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.UI;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DINOForge.Runtime.Bridge
{
    public sealed partial class GameBridgeServer
    {
        private JToken HandleGetUiTree(JObject? parameters)
        {
            string? selector = parameters?.Value<string>("selector");

            var result = MainThreadDispatcher.RunOnMainThread(() => UiTreeSnapshotBuilder.Capture(selector));
            // sync-over-async-unavoidable: ECS-bound, main-thread-required. MainThreadDispatcher.RunOnMainThread
            // returns a Task that completes on main thread; RPC handler must wait synchronously to return response.
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            UiTreeResult treeResult;
            if (!completed)
            {
                treeResult = new UiTreeResult
                {
                    Success = false,
                    Message = "Timed out while capturing UI tree.",
                    Selector = selector,
                    GeneratedAtUtc = DateTime.UtcNow.ToString("O"),
                    NodeCount = 0,
                    Root = new UiNode
                    {
                        Id = "root",
                        Path = "root",
                        Name = "root",
                        Label = "Unity UI",
                        Role = "root",
                        ComponentType = "Root",
                        Active = true,
                        Visible = true,
                        Interactable = false,
                        RaycastTarget = false
                    }
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                treeResult = result.Result;
            }

            UiActionTrace.Record("tree", selector ?? "", treeResult);
            return JToken.FromObject(treeResult);
        }

        private JToken HandleQueryUi(JObject? parameters)
        {
            string selector = parameters?.Value<string>("selector") ?? string.Empty;
            var result = MainThreadDispatcher.RunOnMainThread(() => UiSelectorEngine.Query(selector));
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            UiActionResult queryResult;
            if (!completed)
            {
                queryResult = new UiActionResult
                {
                    Success = false,
                    Selector = selector,
                    Message = "Timed out while querying UI."
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                queryResult = result.Result;
            }

            UiActionTrace.Record("query", selector, queryResult, queryResult.MatchedNode);
            return JToken.FromObject(queryResult);
        }

        private JToken HandleClickUi(JObject? parameters)
        {
            string selector = parameters?.Value<string>("selector") ?? string.Empty;
            var result = MainThreadDispatcher.RunOnMainThread(() => UiSelectorEngine.Click(selector));
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            UiActionResult clickResult;
            if (!completed)
            {
                clickResult = new UiActionResult
                {
                    Success = false,
                    Selector = selector,
                    Message = "Timed out while clicking UI."
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                clickResult = result.Result;
            }

            UiActionTrace.Record("click", selector, clickResult, clickResult.MatchedNode);
            return JToken.FromObject(clickResult);
        }

        /// <summary>
        /// Drives Unity's EventSystem pointer lifecycle in-process (hover/press/click), bypassing
        /// OS input which DINO's EventSystem does not receive. Params:
        /// { target?: selector, x?, y?: screen coords, event: enter|exit|down|up|click|hover|press }.
        /// Either <c>target</c> (selector) or <c>x</c>+<c>y</c> (screen coords) must be supplied.
        /// </summary>
        private JToken HandleUiPointer(JObject? parameters)
        {
            string evt = parameters?.Value<string>("event") ?? "click";
            string? target = parameters?.Value<string>("target");
            float? x = parameters?.Value<float?>("x");
            float? y = parameters?.Value<float?>("y");

            System.Threading.Tasks.Task<UiActionResult> task;
            string selectorLabel;
            if (!string.IsNullOrWhiteSpace(target))
            {
                selectorLabel = target!;
                task = MainThreadDispatcher.RunOnMainThread(() => EventSystemDriver.Drive(target!, evt));
            }
            else if (x.HasValue && y.HasValue)
            {
                selectorLabel = $"({x.Value},{y.Value})";
                task = MainThreadDispatcher.RunOnMainThread(() => EventSystemDriver.DriveAt(x.Value, y.Value, evt));
            }
            else
            {
                var bad = new UiActionResult
                {
                    Success = false,
                    Selector = string.Empty,
                    Message = "uiPointer requires either 'target' (selector) or 'x'+'y' (screen coords).",
                    ActionabilityReason = "missing-target",
                };
                UiActionTrace.Record("pointer", string.Empty, bad);
                return JToken.FromObject(bad);
            }

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = task.Wait(MainThreadWaitTimeoutMs); // sync-over-async-unavoidable: ECS-bound, main-thread-required
            UiActionResult result = completed
                ? task.Result // sync-over-async-unavoidable: ECS-bound, main-thread-required
                : new UiActionResult
                {
                    Success = false,
                    Selector = selectorLabel,
                    Message = "Timed out while driving EventSystem pointer.",
                    ActionabilityReason = "timeout",
                };

            UiActionTrace.Record($"pointer:{evt}", selectorLabel, result, result.MatchedNode);
            return JToken.FromObject(result);
        }

        private JToken HandleWaitForUi(JObject? parameters)
        {
            string selector = parameters?.Value<string>("selector") ?? string.Empty;
            string? state = parameters?.Value<string>("state");
            int timeoutMs = parameters?.Value<int?>("timeoutMs") ?? 5000;
            DateTimeOffset deadline = _timeProvider.GetUtcNow().AddMilliseconds(Math.Max(1, timeoutMs));
            UiWaitResult? lastResult = null;

            while (_timeProvider.GetUtcNow() <= deadline)
            {
                var evalTask = MainThreadDispatcher.RunOnMainThread(() => UiSelectorEngine.EvaluateState(selector, state));
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                bool completed = evalTask.Wait(MainThreadWaitTimeoutMs);
                if (!completed)
                {
                    var timeoutResult = new UiWaitResult
                    {
                        Ready = false,
                        Selector = selector,
                        State = string.IsNullOrWhiteSpace(state) ? "visible" : state!,
                        Message = "Timed out while evaluating UI state on the main thread."
                    };
                    UiActionTrace.Record("wait", selector, timeoutResult, timeoutResult.MatchedNode);
                    return JToken.FromObject(timeoutResult);
                }

                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                lastResult = evalTask.Result;
                if (lastResult.Ready)
                {
                    UiActionTrace.Record("wait", selector, lastResult, lastResult.MatchedNode);
                    return JToken.FromObject(lastResult);
                }

                Thread.Sleep(100);
            }

            var finalResult = lastResult ?? new UiWaitResult
            {
                Ready = false,
                Selector = selector,
                State = string.IsNullOrWhiteSpace(state) ? "visible" : state!,
                Message = $"Timed out waiting for selector '{selector}'."
            };
            UiActionTrace.Record("wait", selector, finalResult, finalResult.MatchedNode);
            return JToken.FromObject(finalResult);
        }

        private JToken HandleExpectUi(JObject? parameters)
        {
            string selector = parameters?.Value<string>("selector") ?? string.Empty;
            string condition = parameters?.Value<string>("condition") ?? "visible";

            var result = MainThreadDispatcher.RunOnMainThread(() => UiSelectorEngine.Expect(selector, condition));
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            UiExpectationResult expectResult;
            if (!completed)
            {
                expectResult = new UiExpectationResult
                {
                    Success = false,
                    Selector = selector,
                    Condition = condition,
                    Message = "Timed out while evaluating UI expectation."
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                expectResult = result.Result;
            }

            UiActionTrace.Record("expect", selector, expectResult, expectResult.MatchedNode);
            return JToken.FromObject(expectResult);
        }

        private JToken HandleScreenshot(JObject? parameters)
        {
            string path = parameters?.Value<string>("path") ?? "";
            // Guard: if the path arg looks like a CLI flag (e.g. "--format=json" leaked from
            // the MCP Python wrapper), discard it and fall back to the default path.
            if (string.IsNullOrEmpty(path) || path.StartsWith("--", StringComparison.Ordinal))
            {
                path = Path.Combine(
                    BepInEx.Paths.BepInExRootPath,
                    "screenshots",
                    $"dinoforge_{DateTime.UtcNow:yyyyMMddTHHmmssZ}.png");
            }

            // Task #535: bounded wait. Screenshot can be a couple of frames long on first call.
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            var ssTask = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // #972 fix: use FrameCapture (synchronous RenderTexture readback) instead of
                    // the asynchronous UnityEngine.ScreenCapture.CaptureScreenshot. The legacy call
                    // only QUEUED a deferred end-of-frame capture and returned Success=true before
                    // any file was written; DINO's custom PlayerLoop never reliably flushed that
                    // capture during active gameplay, so "saved" was reported with no PNG on disk.
                    // FrameCapture renders the active camera into a temp RT, reads it back, encodes
                    // PNG, and File.WriteAllBytes — the file exists before this returns, in ALL states.
                    FrameCapture.Result fc = FrameCapture.Capture(path);
                    if (!fc.Success)
                    {
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleScreenshot FrameCapture failed for '{path}': {fc.Error}");
                    }

                    return new ScreenshotResult
                    {
                        Success = fc.Success,
                        Path = path,
                        Width = fc.Width,
                        Height = fc.Height
                    };
                }
                catch (Exception ex)
                {
                    // Pattern #104 (Task #302): structured logging instead of catch-swallow-default.
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleScreenshot failed for '{path}' ({ex.GetType().Name}): {ex}");
                    return new ScreenshotResult
                    {
                        Success = false,
                        Path = path
                    };
                }
            });
            ScreenshotResult ssResult;
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            if (!ssTask.Wait(MainThreadWaitTimeoutMs))
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleScreenshot timed out ({MainThreadWaitTimeoutMs}ms) — dispatcher pump may be dead");
                ssResult = new ScreenshotResult
                {
                    Success = false,
                    Path = path
                };
            }
            else
            {
                // sync-over-async-unavoidable: ECS-bound, main-thread-required
                ssResult = ssTask.Result;
            }

            return JToken.FromObject(ssResult);
        }

        /// <summary>
        /// Clicks a named Unity UI button. Pass buttonName="DINOForge_ModsButton" to test
        /// the injected Mods button, or any other button name visible in the active scene.
        /// </summary>
        private JToken HandleClickButton(JObject? parameters)
        {
            string buttonName = parameters?.Value<string>("buttonName") ?? "";

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    var allButtons = Resources.FindObjectsOfTypeAll<UnityEngine.UI.Button>();
                    var summary = new System.Text.StringBuilder();
                    UnityEngine.UI.Button? target = null;

                    foreach (var btn in allButtons)
                    {
                        if (btn == null) continue;
                        if (!btn.gameObject.activeInHierarchy) continue;
                        string name = btn.name;
                        var txt = btn.GetComponentInChildren<UnityEngine.UI.Text>();
                        var tmptxt = btn.GetComponentInChildren<TMPro.TMP_Text>();
                        string label = (txt?.text ?? tmptxt?.text ?? "").Trim();
                        summary.Append($"[{name}:'{label}'] ");

                        if (string.IsNullOrEmpty(buttonName))
                            continue; // just listing

                        if (name == buttonName ||
                            name.Equals(buttonName, StringComparison.OrdinalIgnoreCase) ||
                            label.Equals(buttonName, StringComparison.OrdinalIgnoreCase))
                        {
                            target = btn;
                        }
                    }

                    if (string.IsNullOrEmpty(buttonName))
                        return new { success = true, message = $"Buttons: {summary.ToString().Substring(0, Math.Min(800, summary.Length))}" };

                    if (target == null)
                        return new { success = false, message = $"Button '{buttonName}' not found. Active buttons: {summary.ToString().Substring(0, Math.Min(600, summary.Length))}" };

                    // Primary: onClick.Invoke() fires the UnityEvent directly (works for modal dialogs)
                    // Guard against NRE inside button listeners that call EventSystem.current internally
                    try
                    {
                        target.onClick.Invoke();
                    }
                    catch (Exception onClickEx)
                    {
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] onClick.Invoke NRE on '{target.name}', falling back to pointer sim: {onClickEx.Message}");
                    }

                    // Secondary: also fire pointer click for components that listen to IPointerClickHandler
                    // Guard EventSystem.current null — absent on main menu scenes with only TMP buttons (#NRE-fix)
                    var es = EventSystem.current;
                    if (es != null)
                    {
                        try
                        {
                            ExecuteEvents.Execute(
                                target.gameObject,
                                new PointerEventData(es),
                                ExecuteEvents.pointerClickHandler);
                        }
                        catch (Exception execEx)
                        {
                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ExecuteEvents NRE on '{target.name}': {execEx.Message}");
                        }
                    }

                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Clicked button: {target.name}");
                    return new { success = true, message = $"Clicked '{target.name}'" };
                }
                catch (Exception ex)
                {
                    // Pattern #104 (Task #302): preserve type info in wire message + full stack in log.
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ClickButton '{buttonName}' failed: {ex}");
                    return new { success = false, message = $"{ex.GetType().Name}: {ex.Message}" };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            // Use heavy timeout (30s) — scene-loading buttons like "continue" block main thread for 5-20s
            bool completed = result.Wait(30000);
            if (!completed)
            {
                // Fire-and-forget succeeded: scene load is in progress, return dispatched status
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ClickButton '{buttonName}': scene load in progress (fire-and-forget)");
                return JToken.FromObject(new { success = true, message = $"Dispatched '{buttonName}' (scene loading)" });
            }
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            return JToken.FromObject(result.Result);
        }

        /// <summary>
        /// Toggles DINOForge UI panels. target="modmenu" (F10 equivalent) or "debug" (F9 equivalent).
        /// Finds DFCanvas via MonoBehaviour reflection and calls ToggleModMenu()/ToggleDebug().
        /// Falls back to ModMenuOverlay.Toggle() if DFCanvas is not available.
        /// </summary>
        private JToken HandleToggleUi(JObject? parameters)
        {
            string target = (parameters?.Value<string>("target") ?? "modmenu").ToLowerInvariant();

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    // Prefer the live DFCanvas owned by RuntimeDriver (avoids stale DontDestroyOnLoad copies).
                    var allMBs = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                    DFCanvas? dfCanvas = null;
                    MonoBehaviour? debugOverlay = null;
                    MonoBehaviour? modMenuOverlay = null;

                    foreach (var mb in allMBs)
                    {
                        if (mb == null) continue;
                        if (mb is RuntimeDriver driver
                            && driver._dfCanvas != null
                            && driver._dfCanvas.gameObject.activeInHierarchy)
                        {
                            dfCanvas = driver._dfCanvas;
                            break;
                        }
                    }

                    foreach (var mb in allMBs)
                    {
                        if (mb == null) continue;
                        string tName = mb.GetType().Name;
                        if (dfCanvas == null && tName == "DFCanvas" && mb.gameObject.activeInHierarchy)
                        {
                            dfCanvas = mb as DFCanvas;
                        }
                        else if (tName == "DebugOverlayBehaviour") debugOverlay = mb;
                        else if (tName == "ModMenuOverlay") modMenuOverlay = mb;
                    }

                    // Try DFCanvas first (UGUI path)
                    if (dfCanvas != null)
                    {
                        if (target == "debug")
                        {
                            dfCanvas.ToggleDebug();
                        }
                        else
                        {
                            dfCanvas.ToggleModMenu();
                        }

                        string methodName = target == "debug" ? "ToggleDebug" : "ToggleModMenu";
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ToggleUi: called DFCanvas.{methodName}");
                        return new { success = true, message = $"DFCanvas.{methodName}() invoked" };
                    }

                    // Fallback: ModMenuOverlay.Toggle() / DebugOverlayBehaviour.Toggle()
                    MonoBehaviour? fallback = target == "debug" ? debugOverlay : modMenuOverlay;
                    if (fallback != null)
                    {
                        MethodInfo? toggleMethod = fallback.GetType().GetMethod("Toggle",
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        if (toggleMethod != null)
                        {
                            toggleMethod.Invoke(fallback, null);
                            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ToggleUi: called {fallback.GetType().Name}.Toggle()");
                            return new { success = true, message = $"{fallback.GetType().Name}.Toggle() invoked" };
                        }
                    }

                    // Last resort: find any active component whose name contains the target
                    string sbAll = string.Join(", ", Array.ConvertAll(allMBs, mb => mb?.GetType().Name ?? "null"));
                    return new { success = false, message = $"No UI handler found for '{target}'. MBs: {sbAll.Substring(0, Math.Min(400, sbAll.Length))}" };
                }
                catch (Exception ex)
                {
                    // Pattern #104 (Task #302): preserve type info in wire message + full stack in log.
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ToggleUi target='{target}' failed: {ex}");
                    return new { success = false, message = $"{ex.GetType().Name}: {ex.Message}" };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadWaitTimeoutMs);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            return JToken.FromObject(result.Result);
        }
    }
}
