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
        // ── Engine-driven heartbeat (iter-149e, 2026-05-29) ───────────────────────────
        // WinDbg MDMP (docs/sessions/engine-ui-windbg-mdmp-20260529.md) proved the wedge is a
        // DORMANT-PLUGIN lifecycle bug, NOT a native deadlock: the engine main thread is in the
        // normal Unity idle wait while the plugin's worker threads are gone. The old wedge
        // classifier ("log mtime frozen + process alive + Responding") could not distinguish a
        // benign-engine/dormant-plugin from a true native wedge. This heartbeat is incremented and
        // flushed to BepInEx/dinoforge_heartbeat.txt from EVERY reliable main-thread tick
        // (scene events + PlayerLoop). If the heartbeat keeps advancing while the plugin LOG is
        // frozen, it is a dormant-plugin lifecycle bug (this class). If both are frozen, it is a
        // native wedge (iter-144 class). Never misclassify again.
        private static long _engineHeartbeat;
        private static readonly object _engineHeartbeatLock = new object();
        private const string EngineHeartbeatFileName = "dinoforge_heartbeat.txt";

        /// <summary>
        /// Increments the engine heartbeat and best-effort flushes it to
        /// <c>BepInEx/dinoforge_heartbeat.txt</c>. Safe to call from any reliable main-thread tick
        /// (scene events, PlayerLoop). Never throws (Pattern #104/#111).
        /// </summary>
        internal static void BumpEngineHeartbeat(string source)
        {
            try
            {
                long n;
                lock (_engineHeartbeatLock)
                {
                    n = ++_engineHeartbeat;
                }
                // Throttle disk writes to ~once per N bumps to avoid I/O churn; callers gate cadence.
                string root = BepInEx.Paths.BepInExRootPath;
                if (string.IsNullOrEmpty(root)) return;
                string path = Path.Combine(root, EngineHeartbeatFileName);
                string body = n.ToString() + " " + DateTime.UtcNow.ToString("o") + " " + (source ?? "") + "\n";
                File.WriteAllText(path, body, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                try { DebugLog.Write("Plugin", $"[Heartbeat] write failed (non-fatal): {ex.GetType().Name}: {ex.Message}"); }
                catch { /* diagnostic only */ }
            }
        }
    }
}
