#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using DINOForge.Bridge.Protocol;
using DINOForge.Runtime.Diagnostics;
using DINOForge.Runtime.Telemetry;
using DINOForge.Runtime.UI;
using RuntimeDriver = DINOForge.Runtime.RuntimeDriver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace DINOForge.Runtime.Bridge
{
    /// <summary>
    /// Named pipe server implementing JSON-RPC 2.0 over NDJSON for IPC communication.
    /// Runs on a background thread and dispatches Unity-thread-required operations
    /// through <see cref="MainThreadDispatcher"/>.
    /// </summary>
    public sealed partial class GameBridgeServer : IDisposable
    {
        /// <summary>The well-known pipe name used by the DINOForge bridge.</summary>
        public const string PipeName = "dinoforge-game-bridge";

        // CLR's COR_E_THREADABORT HRESULT — Thread.Abort on .NET Core wraps as IOException.
        // See: https://github.com/dotnet/runtime/blob/main/src/libraries/System.Private.CoreLib/src/System/Threading/Thread.cs
        private const int COR_E_THREADABORT = unchecked((int)0x80131623);

        // Bounded waits for MainThreadDispatcher work items (task #535).
        // Background threads MUST NOT call .Result or .Wait() unbounded — the dispatcher
        // pump (KeyInputSystem.OnUpdate) can be torn down at any scene transition, after
        // which queued work never drains and the bridge thread parks indefinitely
        // (IsHungAppWindow=True). Use these constants as the timeout argument.
        // threshold-ok: bounded main-thread wait for ECS handlers
        private const int MainThreadWaitTimeoutMs = 5000;
        // threshold-ok: mid-tier bound for input-injection handlers that scan scene objects
        private const int MainThreadInputWaitTimeoutMs = 8000;
        // threshold-ok: longer bound for heavier reflection-driven UI handlers
        private const int MainThreadHeavyWaitTimeoutMs = 10000;
        // threshold-ok: poll interval for shutdown signal check; short enough to react quickly, long enough not to spin
        private const int ShutdownPollIntervalMs = 200;

        /// <summary>
        /// Resets the current thread abort state on legacy Unity/Mono runtimes.
        /// The runtime still relies on ThreadAbortException handling for scene-transition teardown,
        /// so the call remains, but the SYSLIB0006 warning is intentionally suppressed here rather
        /// than changing the abort recovery behavior.
        /// </summary>
#pragma warning disable SYSLIB0006
        private static void ResetThreadAbort()
        {
            Thread.ResetAbort();
        }
#pragma warning restore SYSLIB0006

        private ModPlatform _platform;
        private readonly DateTimeOffset _startTime;
        private readonly TimeProvider _timeProvider;
        private Thread? _serverThread;
        private volatile bool _running;

        /// <summary>
        /// Diagnostic: true if the server background thread is alive. Used by RuntimeDriver.OnDestroy
        /// to log accurate state instead of asserting "Bridge kept alive" without verification.
        /// (iter-144 #535 re-fix.)
        /// </summary>
        public bool IsServerThreadAlive => _running && _serverThread != null && _serverThread.IsAlive;
        private volatile NamedPipeServerStream? _currentPipe;
        private readonly ManualResetEventSlim _shutdownEvent = new(false);
        private SessionHmac? _session;

        /// <summary>
        /// True while the ModPlatform is alive (not destroyed during a scene transition).
        /// </summary>
        private bool IsPlatformAlive
        {
            get
            {
                try { return _platform != null && _platform.IsInitialized; }
                catch (Exception) { /* safe-swallow: bridge health probe must return false instead of surfacing transient teardown races */ return false; }
            }
        }

        /// <summary>
        /// Creates a new game bridge server.
        /// </summary>
        /// <param name="platform">The ModPlatform instance for accessing subsystems.</param>
        /// <param name="timeProvider">Optional TimeProvider for testing. Defaults to <see cref="TimeProvider.System"/>.</param>
        public GameBridgeServer(ModPlatform platform, TimeProvider? timeProvider = null)
        {
            _platform = platform ?? throw new ArgumentNullException(nameof(platform));
            _timeProvider = timeProvider ?? TimeProvider.System;
            _startTime = _timeProvider.GetUtcNow();
        }

        /// <summary>
        /// Starts the named pipe server on a background thread.
        /// </summary>
        public void Start()
        {
            if (_running) return;
            _running = true;
            StartThread();
        }

        /// <summary>
        /// Starts (or restarts) the server thread. If the thread was aborted by
        /// DINO's scene transitions, this creates a new one.
        /// </summary>
        private void StartThread()
        {
            _serverThread = new Thread(ServerLoopWithAutoRestart)
            {
                Name = "DINOForge-Bridge-Server",
                IsBackground = false
            };
            _serverThread.Start();
            DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Started on pipe: " + PipeName);
        }

        /// <summary>
        /// Wrapper around ServerLoop that catches ThreadAbortException and restarts.
        /// </summary>
        private void ServerLoopWithAutoRestart()
        {
            try
            {
                ServerLoop();
            }
            catch
            {
                // ServerLoop exited — either stopped normally or thread was aborted.
                // Dispose any lingering pipe to free the pipe name for restart.
                try { _currentPipe?.Dispose(); } catch { /* safe-swallow: pipe already disposed or invalid during restart */ }
                _currentPipe = null;

                if (_running)
                {
                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Server loop exited — restarting in 2s...");
                    try
                    {
                        new Thread(() =>
                        {
                            Thread.Sleep(2000);
                            if (_running) StartThread();
                        })
                        { IsBackground = false, Name = "DINOForge-Bridge-Restart" }.Start();
                    }
                    catch (Exception ex)
                    {
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Restart failed: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Iter-144 #547 gray-freeze ROOT CAUSE fix: signals shutdown and disposes the current
        /// pipe handle synchronously to unblock the kernel-mode ConnectNamedPipe wait that was
        /// parking the bridge thread in mono_jit_cleanup. Safe to call from Unity OnDestroy at
        /// the TOP of teardown, BEFORE mono_threads_set_shutting_down tries to interrupt the
        /// blocked thread. WinDbg analysis (docs/sessions/iter144-windbg-wedge-stack.md)
        /// identified the synchronous ConnectNamedPipe syscall on thread 82 as the wedge.
        /// </summary>
        public void RequestShutdown()
        {
            _running = false;
            try { _shutdownEvent.Set(); } catch { /* safe-swallow: event disposed during shutdown race */ }

            // Dispose the pipe handle synchronously — this unblocks the kernel I/O
            // (BeginWaitForConnection / WaitForConnectionAsync) with ObjectDisposedException.
            try
            {
                NamedPipeServerStream? pipe = _currentPipe;
                _currentPipe = null;
                pipe?.Dispose();
            }
            catch { /* safe-swallow: pipe dispose during shutdown — kernel I/O will unblock */ }

            DebugLog.Write("GameBridgeServer", "[GameBridgeServer] shutdown requested — pipe disposed, accept loop will exit.");
        }

        /// <summary>
        /// Stops the server and releases all resources.
        /// </summary>
        public void Stop()
        {
            RequestShutdown();

            try
            {
                _session?.Dispose();
            }
            catch { /* safe-swallow: session disposal during shutdown can race */ }

            _session = null;
            DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Stopped.");
        }

        /// <summary>
        /// Disposes the server, stopping it if running.
        /// </summary>
        public void Dispose()
        {
            Stop();
        }

        /// <summary>
        /// Updates the ModPlatform reference after resurrection.
        /// Called when a new RuntimeDriver is created and re-initializes ModPlatform.
        /// Also ensures the server thread is alive — restarts it if it died.
        /// </summary>
        public void UpdatePlatform(ModPlatform platform)
        {
            _platform = platform;
            DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Platform reference updated (post-resurrection).");
            EnsureServerAlive();
        }

        /// <summary>
        /// Checks if the server thread is alive and restarts it if dead.
        /// Also triggers RuntimeDriver resurrection if PersistentRoot is null.
        /// Called from KeyInputSystem.OnUpdate() every ~50ms to ensure the bridge
        /// and UI systems survive Unity's scene transitions which may abort threads
        /// and destroy the RuntimeDriver.
        /// </summary>
        public void EnsureServerAlive()
        {
            // FailureMode B fix (iter-149, 2026-05-29): DO NOT call TryResurrect here.
            // EnsureServerAlive runs on the ResurrectionFallback BACKGROUND thread every poll
            // tick. Calling TryResurrect when PersistentRoot==null reaches Unity ECalls
            // (Camera.main / AddComponent / RuntimeDriver.Initialize, which touch
            // Resources/asset APIs) on a background thread DURING the InitialGameLoader→MainMenu
            // asset load — that DEADLOCKS the fallback thread (memory: "Resources.* from a bg
            // thread DEADLOCKS during asset loading"). The wedged thread stops emitting
            // heartbeats and never reaches the grace-windowed revive, so the driver stays
            // dormant and no engine UI (MODS/F9/F10) appears.
            //
            // Resurrection is OWNED by ResurrectionFallbackLoop's grace-windowed path, which
            // exists precisely to defer the revive until the scene has settled. This method now
            // only restarts a dead bridge SERVER THREAD (pipe-only work, no Unity ECalls), and
            // marks the deferred-resurrection flag so the grace path picks it up safely.
            if (Plugin.PersistentRoot == null)
            {
                DebugLog.Write("GameBridgeServer", "[GameBridgeServer] PersistentRoot is null — deferring resurrection to grace-windowed fallback path (no bg-thread Unity ECalls).");
                Plugin.MarkNeedsDeferredResurrection("EnsureServerAlive");
            }

            if (_running && (_serverThread == null || !_serverThread.IsAlive))
            {
                DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Server thread is dead — restarting...");
                Stop();
                // Create fresh thread — the old thread object is abandoned after abort.
                Start();
            }
        }

        /// <summary>
        /// Main server loop: accepts pipe connections and processes NDJSON messages.
        /// Reconnects automatically after each client disconnects.
        /// </summary>
        private void ServerLoop()
        {
            while (_running)
            {
                NamedPipeServerStream? pipe = null;
                try
                {
                    // Use None (synchronous mode) — this server runs on a dedicated background
                    // thread so async pipe mode is not needed and causes ReadLine() to block
                    // indefinitely on Windows when no data is available.
                    // Allow multiple server instances so that after a ThreadAbortException
                    // + ResetAbort cycle, a new pipe can be created even if the old one
                    // hasn't been fully disposed yet.
                    // Iter-144 #547 gray-freeze ROOT CAUSE fix: use PipeOptions.Asynchronous so
                    // BeginWaitForConnection returns an IAsyncResult whose WaitHandle can be
                    // multiplexed with _shutdownEvent. The previous synchronous WaitForConnection
                    // parked this thread in kernel-mode ConnectNamedPipe; Mono could not interrupt
                    // it, causing mono_jit_cleanup to wait forever on the bridge thread at the
                    // MainMenu transition (WinDbg dump: docs/sessions/iter144-windbg-wedge-stack.md).
                    pipe = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NamedPipeServerStream.MaxAllowedServerInstances,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    _currentPipe = pipe;
                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Waiting for connection (async-cancellable)...");

                    IAsyncResult connectAr = pipe.BeginWaitForConnection(null, null);
                    WaitHandle[] handles = { connectAr.AsyncWaitHandle, _shutdownEvent.WaitHandle };
                    int sigIdx = WaitHandle.WaitAny(handles);
                    if (sigIdx == 1 || !_running)
                    {
                        // Shutdown signaled — dispose pipe to unblock the pending I/O cleanly.
                        DebugLog.Write("GameBridgeServer", "[GameBridgeServer] pipe disposed, accept loop exiting (shutdown).");
                        try { pipe.Dispose(); } catch { /* safe-swallow: pipe dispose during shutdown */ }
                        _currentPipe = null;
                        return;
                    }

                    // Connection completed — call EndWaitForConnection to observe any exception.
                    pipe.EndWaitForConnection(connectAr);
                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Client connected.");

                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Setting up line reader");
                    // Read lines manually byte-by-byte to avoid StreamReader buffering issues
                    // on Mono with synchronous named pipes.
                    while (_running && pipe.IsConnected)
                    {
                        string? line = null;
                        bool responseWritten = false;
                        string? processError = null;
                        try
                        {
                            // Read line from client
                            try
                            {
                                line = ReadLineFromPipe(pipe);
                            }
                            catch (IOException ex)
                            {
                                if (ex.HResult == unchecked((int)0x80131623))
                                    ResetThreadAbort();
                            }
                            catch (ThreadAbortException)
                            {
                                ResetThreadAbort();
                            }
                            catch { /* safe-swallow: read errors on client pipe — loop continues, line remains null */ }

                            if (line == null) continue;
                            if (string.IsNullOrWhiteSpace(line)) continue;

                            // Process message
                            string? response = null;
                            processError = null;
                            try
                            {
                                response = ProcessMessage(line);
                            }
                            catch (ThreadAbortException)
                            {
                                ResetThreadAbort();
                            }
                            catch (Exception ex)
                            {
                                processError = ex.ToString();
                            }

                            if (response == null) continue;

                            // Write response to client
                            try
                            {
                                byte[] bytes = Encoding.UTF8.GetBytes(response + "\n");
                                pipe.Write(bytes, 0, bytes.Length);
                                pipe.Flush();
                                responseWritten = true;
                            }
                            catch (IOException ex)
                            {
                                if (ex.HResult == unchecked((int)0x80131623))
                                    ResetThreadAbort();
                            }
                            catch (ThreadAbortException)
                            {
                                ResetThreadAbort();
                            }
                            catch { /* safe-swallow: write errors trigger fallback writer in finally block */ }
                        }
                        finally
                        {
                            // GUARANTEED fallback: if no response was written (exception occurred),
                            // send a minimal error response so the client unblocks and does not hang.
                            if (!responseWritten)
                            {
                                string fallbackJson = BuildBridgeErrorResponse(processError) + "\n";
                                bool pipeWriteFailed = false;
                                try
                                {
                                    byte[] fallback = Encoding.UTF8.GetBytes(fallbackJson);
                                    pipe.Write(fallback, 0, fallback.Length);
                                    pipe.Flush();
                                }
                                catch
                                {
                                    pipeWriteFailed = true;
                                }

                                if (pipeWriteFailed)
                                {
                                    // Pipe is broken (e.g., Thread.Abort during write).
                                    // Write fallback to a file as a backup. The CLI will check this file.
                                    try
                                    {
                                        string fallbackDir = Path.Combine(Path.GetTempPath(), "DINOForge");
                                        Directory.CreateDirectory(fallbackDir);
                                        string fallbackFile = Path.Combine(fallbackDir, "dinoforge_bridge_fallback.txt");
                                        File.WriteAllText(fallbackFile, fallbackJson.TrimEnd(), Encoding.UTF8);
                                    }
                                    catch { /* safe-swallow: fallback file is best-effort diagnostic; pipe error already handled */ }
                                }
                            }
                        }
                    }

                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Exited read loop");

                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] Client disconnected.");
                }
                catch (ObjectDisposedException)
                {
                    // safe-swallow: server is shutting down — pipe disposal is expected
                }
                catch (System.Threading.ThreadAbortException)
                {
                    // DINO/Unity may abort threads during scene transitions.
                    // Reset the abort and continue the loop — the bridge must survive.
                    ResetThreadAbort();
                    DebugLog.Write("GameBridgeServer", "[GameBridgeServer] [OUTER] ThreadAbortException caught — closing pipe to unblock client.");
                    try { pipe?.Dispose(); } catch { /* safe-swallow: pipe dispose after ThreadAbort recovery */ }
                }
                catch (IOException ex)
                {
                    // Thread.Abort may manifest as IOException with COR_E_THREADABORT HResult
                    // (0x80131623) when the abort interrupts a blocking synchronous I/O call.
                    // See: https://github.com/dotnet/runtime/issues/30675
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] [OUTER-IO] IOException: {ex.Message} (HResult=0x{ex.HResult:X8})");
                    if (ex.HResult == COR_E_THREADABORT)
                    {
                        ResetThreadAbort();
                        DebugLog.Write("GameBridgeServer", "[GameBridgeServer] [OUTER-IO] COR_E_THREADABORT — resetting and restarting.");
                    }
                    else
                    {
                        DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] [OUTER-IO] Non-abort IOException.");
                    }
                    try { pipe?.Dispose(); } catch { /* safe-swallow: pipe dispose after IO error */ }
                }
                catch (Exception ex)
                {
                    DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] [OUTER] Error in server loop: {ex.Message}");
                    try { pipe?.Dispose(); } catch { /* safe-swallow: pipe dispose after outer exception */ }
                }
                finally
                {
                    // Close the pipe handle. Unlike Dispose(), Close() on Windows named pipes
                    // sends ERROR_OPIPE_NOT_CONNECTED to waiting clients without blocking.
                    // This unblocks any client blocked in Read() on this pipe.
                    try { pipe?.Dispose(); } catch { /* safe-swallow: pipe dispose in finally — guaranteed cleanup */ }
                    _currentPipe = null;
                }

                // Pause before re-listening. Longer delay after errors to avoid log spam.
                if (_running)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private static string BuildBridgeErrorResponse(string? detail)
        {
            string message = string.IsNullOrWhiteSpace(detail) ? "Bridge error" : $"Bridge error: {detail}";
            return JsonConvert.SerializeObject(new
            {
                jsonrpc = "2.0",
                id = (object?)null,
                error = new { code = -32603, message }
            });
        }

        /// <summary>
        /// Parses a single NDJSON line as a JSON-RPC request, dispatches to the
        /// appropriate handler, and returns the serialized response.
        /// </summary>
        private string ProcessMessage(string json)
        {
            JsonRpcRequest? request;
            try
            {
                request = JsonConvert.DeserializeObject<JsonRpcRequest>(json);
            }
            catch (Exception ex)
            {
                return SerializeError(null, -32700, "Parse error: " + ex.Message);
            }

            if (request == null || string.IsNullOrEmpty(request.Method))
            {
                return SerializeError(request?.Id, -32600, "Invalid request");
            }

            try
            {
                JToken result = DispatchMethod(request.Method, request.Params);
                return SerializeSuccess(request.Id, result);
            }
            catch (ThreadAbortException tae)
            {
                // Thread.Abort() was called on the bridge thread (e.g., by Unity/Mono runtime cleanup).
                // Reset the abort so the thread can continue. Return a valid response so the client
                // unblocks — otherwise the pipe breaks without a response and the client hangs forever.
                ResetThreadAbort();
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] ThreadAbortException during '{request.Method}': {tae.Message}");
                return SerializeError(request.Id, -32603, "Bridge thread abort — retry later");
            }
            catch (Exception ex)
            {
                DebugLog.Write("GameBridgeServer", $"[GameBridgeServer] Handler error for '{request.Method}': {ex}");
                return SerializeError(request.Id, -32603, "Internal error: " + ex.Message);
            }
        }

        /// <summary>
        /// Routes a method name to the appropriate handler and returns the result as a JToken.
        /// </summary>
        private JToken DispatchMethod(string method, JObject? parameters)
        {
            // Normalize: accept both "game.status" and "status" formats
            string m = method.StartsWith("game.") ? method.Substring(5) : method;
            switch (m)
            {
                case "connect":
                    return HandleConnect();
                case "ping":
                    return HandlePing();
                case "status":
                    return HandleStatus();
                case "getCatalog":
                    return HandleGetCatalog();
                case "getComponentMap":
                    return HandleGetComponentMap(parameters);
                case "discoverTypes":
                    return HandleDiscoverTypes(parameters);
                case "getUiTree":
                    return HandleGetUiTree(parameters);
                case "queryUi":
                    return HandleQueryUi(parameters);
                case "clickUi":
                    return HandleClickUi(parameters);
                case "uiPointer":
                case "ui_pointer":
                    return HandleUiPointer(parameters);
                case "waitForUi":
                    return HandleWaitForUi(parameters);
                case "expectUi":
                    return HandleExpectUi(parameters);
                case "getStat":
                    return HandleGetStat(parameters);
                case "applyOverride":
                    return HandleApplyOverride(parameters);
                case "queryEntities":
                    return HandleQueryEntities(parameters);
                case "reloadPacks":
                    return HandleReloadPacks(parameters);
                case "getResources":
                    return HandleGetResources();
                case "screenshot":
                    return HandleScreenshot(parameters);
                case "dumpState":
                    return HandleDumpState(parameters);
                case "verifyMod":
                    return HandleVerifyMod(parameters);
                case "waitForWorld":
                    return HandleWaitForWorld(parameters);
                case "loadScene":
                    return HandleLoadScene(parameters);
                case "startGame":
                    return HandleStartGame(parameters);
                case "loadSave":
                    return HandleLoadSave(parameters);
                case "listSaves":
                    return HandleListSaves();
                case "pressKey":
                    return HandlePressKey(parameters);
                case "simulateKey":
                    return HandleSimulateKey(parameters);
                case "pressEscape":
                    return HandleSimulateKey(new JObject { ["key"] = "Escape" });
                case "togglePauseMenu":
                    return HandleTogglePauseMenu(parameters);
                case "dismissLoadScreen":
                    return HandleDismissLoadScreen();
                case "clickButton":
                    return HandleClickButton(parameters);
                case "toggleUi":
                    return HandleToggleUi(parameters);
                case "invokeMethod":
                    return HandleInvokeMethod(parameters);
                case "getMetrics":
                    return HandleGetMetrics();
                case "navigateToGameplay":
                case "navigate_to_gameplay":
                    return HandleNavigateToGameplay(parameters);
                default:
                    throw new InvalidOperationException($"Method not found: {method}");
            }
        }

        /// <summary>
        /// Reads a single UTF-8 line from the pipe byte-by-byte.
        /// Returns null if the pipe is closed. This avoids StreamReader buffering
        /// issues on Mono where a large buffer causes blocking on partial reads.
        /// </summary>
        private static string? ReadLineFromPipe(Stream pipe)
        {
            var sb = new System.Text.StringBuilder();
            int b;
            while ((b = pipe.ReadByte()) != -1)
            {
                char c = (char)b;
                if (c == '\n') return sb.ToString();
                if (c != '\r') sb.Append(c);
            }
            return sb.Length > 0 ? sb.ToString() : null;
        }

        /// <summary>
        /// Serializes a successful JSON-RPC response.
        /// </summary>
        private static string SerializeSuccess(string? id, JToken result)
        {
            JsonRpcResponse response = new JsonRpcResponse
            {
                Id = id,
                Result = result
            };
            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        /// <summary>
        /// Serializes a JSON-RPC error response.
        /// </summary>
        private static string SerializeError(string? id, int code, string message)
        {
            JsonRpcResponse response = new JsonRpcResponse
            {
                Id = id,
                Error = new JsonRpcError
                {
                    Code = code,
                    Message = message
                }
            };
            return JsonConvert.SerializeObject(response, Formatting.None);
        }

        /// <summary>
        /// Returns the ECS world to use for entity queries.
        ///
        /// After scene transitions, KeyInputSystem may live in a different world than
        /// GetActiveWorld() (because OnCreate fires before the default
        /// world is set). We query DefaultGameObjectInjectionWorld first (has all game entities).
        /// If that's null (startup edge case), we scan all worlds to find one with entities.
        /// </summary>
        private static World? GetActiveWorld()
        {
            World? preferred = World.DefaultGameObjectInjectionWorld;
            if (preferred != null && preferred.IsCreated)
                return preferred;
            return null;
        }
    }
}
