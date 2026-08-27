#nullable enable
using System;
using System.Collections.Generic;
using DINOForge.Bridge.Protocol;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.Telemetry;
using Newtonsoft.Json.Linq;

namespace DINOForge.Runtime.Bridge
{
    public sealed partial class GameBridgeServer
    {
        /// <summary>
        /// Handles the <c>connect</c> handshake request. Generates a fresh session
        /// with a unique session_id and 32-byte ephemeral key for Phase 4a SessionHmac.
        /// Returns a JSON object with snake_case fields: session_id, session_key_b64.
        /// Per the mock server contract, replaces any prior session (reconnect semantics).
        /// </summary>
        private JToken HandleConnect()
        {
            // Dispose previous session if one exists (reconnect semantics)
            _session?.Dispose();

            // Mint a fresh session
            _session = new SessionHmac();

            // Return the handshake envelope with snake_case fields
            var envelope = new JObject
            {
                ["session_id"] = _session.SessionId,
                ["session_key_b64"] = _session.KeyMaterialB64(),
            };

            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleConnect: minted session_id={_session.SessionId}");
            return envelope;
        }

        private JToken HandlePing()
        {
            PingResult result = new PingResult
            {
                Pong = true,
                Version = PluginInfo.VERSION,
                UptimeSeconds = (_timeProvider.GetUtcNow() - _startTime).TotalSeconds
            };
            return JToken.FromObject(result);
        }

        private JToken HandleStatus()
        {
            DebugLog.Write("GameBridgeServer", "[GameBridgeServer] HandleStatus ENTER");
            GameStatus status = new GameStatus
            {
                Running = _running && IsPlatformAlive,
                WorldReady = IsPlatformAlive && _platform.IsWorldReady,
                ModPlatformReady = IsPlatformAlive && _platform.IsInitialized,
                Version = PluginInfo.VERSION,
                EntityCount = -1,
                LoadedPacks = new List<string>()
            };

            // Access KeyInputSystem cached values for world name.
            // KeyInputSystem.OnUpdate caches these from the main ECS thread each frame.
            // Reading static strings from a background thread is safe.
            try
            {
                string? cachedName = KeyInputSystem.CachedWorldName;
                if (!string.IsNullOrEmpty(cachedName))
                    status.WorldName = cachedName;
            }
            catch (Exception worldEx)
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] KeyInputSystem.CachedWorldName failed: {worldEx.Message}");
            }

            // Try entity count from KeyInputSystem cached value (updated each OnUpdate frame).
            // This is a static int read — thread-safe and never triggers ECS main-thread checks.
            try
            {
                int cachedCount = KeyInputSystem.LastEntityCount;
                status.EntityCount = cachedCount;
            }
            catch (Exception ex)
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] KeyInputSystem.LastEntityCount failed: {ex.Message}");
            }

            // Populate loaded pack names from platform
            if (IsPlatformAlive && _platform.IsInitialized)
            {
                try
                {
                    System.Collections.Generic.IReadOnlyList<string>? packs = _platform.GetLoadedPackIds();
                    if (packs != null)
                    {
                        foreach (string id in packs)
                            status.LoadedPacks.Add(id);
                    }
                }
                catch { /* safe-swallow: pack-status enumeration is best-effort diagnostic */ }
            }

            DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleStatus EXIT: worldName='{status.WorldName}' entityCount={status.EntityCount}");
            try { return JToken.FromObject(status); }
            catch { return JToken.FromObject(new { EntityCount = -1, Running = _running && IsPlatformAlive }); /* safe-swallow: JToken serialization fallback */ }
        }

        // #923: Telemetry — return current MetricsCollector snapshot as JSON.
        private JToken HandleGetMetrics()
        {
            try
            {
                string json = MetricsCollector.Instance.DumpJson();
                return JToken.Parse(json);
            }
            catch (Exception ex)
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] HandleGetMetrics failed: {ex.Message}");
                return JToken.FromObject(new { error = ex.Message });
            }
        }
    }
}
