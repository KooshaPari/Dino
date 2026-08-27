#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DINOForge.Bridge.Protocol;
using DINOForge.Runtime.Diagnostics;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    public sealed partial class GameBridgeServer
    {
        /// <summary>
        /// Injects a key press via Win32 SendInput (same path as MCP game input tools).
        /// Parameter <c>key</c> defaults to Escape for pause-menu tests.
        /// For Escape, also runs <see cref="PauseMenuBridgeHelper"/> on the main thread when Win32 fails
        /// or the pause UI is still hidden.
        /// </summary>
        private JToken HandleSimulateKey(JObject? parameters)
        {
            string key = parameters?.Value<string>("key") ?? "Escape";
            bool win32Ok = Win32KeyInput.TrySendKey(key, out string win32Message);

            if (!IsEscapeKey(key))
            {
                DebugLog.Write("GameBridgeServer",
                    $"[GameBridgeServer] HandleSimulateKey key='{key}' ok={win32Ok} msg={win32Message}");
                return JToken.FromObject(new { success = win32Ok, message = win32Message });
            }

            return HandleEscapePauseOpen(win32Ok, win32Message);
        }

        private JToken HandleTogglePauseMenu(JObject? _)
        {
            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                (bool opened, string message) = PauseMenuBridgeHelper.TryOpenPauseMenu();
                bool visible = PauseMenuBridgeHelper.IsPauseMenuVisible();
                return new
                {
                    success = visible,
                    message,
                    pauseVisible = visible,
                    opened,
                };
            });

            bool completed = result.Wait(MainThreadInputWaitTimeoutMs);
            if (!completed)
            {
                return JToken.FromObject(new { success = false, message = "Timed out" });
            }

            DebugLog.Write("GameBridgeServer",
                $"[GameBridgeServer] HandleTogglePauseMenu result={result.Result}");
            return JToken.FromObject(result.Result);
        }

        private JToken HandleEscapePauseOpen(bool win32Ok, string win32Message)
        {
            var pauseResult = MainThreadDispatcher.RunOnMainThread(() =>
            {
                (bool _, string openMessage) = PauseMenuBridgeHelper.TryOpenPauseMenu();
                bool visible = PauseMenuBridgeHelper.IsPauseMenuVisible();
                return (visible, openMessage);
            });

            bool pauseCompleted = pauseResult.Wait(MainThreadInputWaitTimeoutMs);
            bool pauseVisible = false;
            string pauseMessage = "main-thread pause open timed out";
            if (pauseCompleted)
            {
                (bool visible, string openMessage) = pauseResult.Result;
                pauseVisible = visible;
                pauseMessage = openMessage;
            }

            bool success = pauseVisible || win32Ok;
            string message = pauseVisible
                ? $"pause menu visible; win32={win32Ok} ({win32Message}); {pauseMessage}"
                : $"win32={win32Ok} ({win32Message}); {pauseMessage}";

            DebugLog.Write("GameBridgeServer",
                $"[GameBridgeServer] HandleSimulateKey Escape success={success} pauseVisible={pauseVisible} msg={message}");
            return JToken.FromObject(new { success, message, pauseVisible });
        }

        private static bool IsEscapeKey(string key) =>
            string.Equals(key, "Escape", StringComparison.OrdinalIgnoreCase)
            || string.Equals(key, "Esc", StringComparison.OrdinalIgnoreCase);

        private JToken HandlePressKey(JObject? parameters)
        {
            // scanScene: dump all active MonoBehaviours + their public/private void methods
            // filter: optional substring filter on type name
            string filter = parameters?.Value<string>("filter") ?? "";

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    var allMBs = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                    var sb = new System.Text.StringBuilder();
                    foreach (var mb in allMBs)
                    {
                        if (mb == null || !mb.gameObject.activeInHierarchy) continue;
                        string tName = mb.GetType().Name;
                        if (!string.IsNullOrEmpty(filter) &&
                            tName.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0 &&
                            mb.gameObject.name.IndexOf(filter, StringComparison.OrdinalIgnoreCase) < 0)
                            continue;

                        // List void methods with 0 params
                        var methods = mb.GetType().GetMethods(
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                            .Where(m => m.ReturnType == typeof(void) && m.GetParameters().Length == 0)
                            .Select(m => m.Name)
                            .Where(n => !n.StartsWith("get_") && !n.StartsWith("set_") && n != "Finalize")
                            .Take(8);
                        sb.AppendLine($"[{mb.gameObject.name}] {tName}: {string.Join(", ", methods)}");
                    }
                    string output = sb.Length > 0 ? sb.ToString().Substring(0, Math.Min(2000, sb.Length)) : "No matches";
                    return new { success = true, message = output };
                }
                catch (Exception ex)
                {
                    // Pattern #104 (Task #302): preserve type info in wire message + full stack in log.
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] PressKey/scanScene failed: {ex}");
                    return new { success = false, message = $"{ex.GetType().Name}: {ex.Message}" };
                }
            });

            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            bool completed = result.Wait(MainThreadInputWaitTimeoutMs);
            if (!completed) return JToken.FromObject(new { success = false, message = "Timed out" });
            // sync-over-async-unavoidable: ECS-bound, main-thread-required
            return JToken.FromObject(result.Result);
        }

        /// <summary>
        /// Invokes a named void(0-param) method on any MonoBehaviour whose type name or
        /// gameObject name contains <c>target</c>. Use to call dialog confirm handlers, etc.
        /// </summary>
        private JToken HandleInvokeMethod(JObject? parameters)
        {
            string target = parameters?.Value<string>("target") ?? "";
            string method = parameters?.Value<string>("method") ?? "";

            var result = MainThreadDispatcher.RunOnMainThread(() =>
            {
                try
                {
                    if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(method))
                        return new { success = false, message = "Provide target (type/go name) and method" };

                    var allMBs = Resources.FindObjectsOfTypeAll<MonoBehaviour>();
                    var invoked = new List<string>();
                    foreach (var mb in allMBs)
                    {
                        if (mb == null) continue;
                        string tName = mb.GetType().Name;
                        string goName = mb.gameObject.name;
                        bool matches = tName.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0 ||
                                       goName.IndexOf(target, StringComparison.OrdinalIgnoreCase) >= 0;
                        if (!matches) continue;

                        if (!mb.gameObject.activeInHierarchy)
                        {
                            Transform? current = mb.transform;
                            while (current != null)
                            {
                                if (!current.gameObject.activeSelf)
                                {
                                    current.gameObject.SetActive(true);
                                }

                                current = current.parent;
                            }
                        }

                        MethodInfo? mi = mb.GetType().GetMethod(method,
                            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                            null, Type.EmptyTypes, null);
                        if (mi == null) continue;

                        mi.Invoke(mb, null);
                        invoked.Add($"{tName}.{method}()");
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] InvokeMethod: {tName}.{method}()");
                    }

                    if (invoked.Count == 0)
                        return new { success = false, message = $"No active MonoBehaviour matching '{target}' with method '{method}' found" };

                    return new { success = true, message = $"Invoked: {string.Join(", ", invoked)}" };
                }
                catch (Exception ex)
                {
                    // Pattern #104 (Task #302): preserve type info in wire message + full stack in log.
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] InvokeMethod target='{target}' method='{method}' failed: {ex}");
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
