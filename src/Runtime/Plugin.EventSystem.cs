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
    }
}
