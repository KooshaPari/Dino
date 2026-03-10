using System;
using System.Collections.Generic;
using BepInEx.Logging;
using DINOForge.SDK;
using DINOForge.SDK.HotReload;
using DINOForge.SDK.Registry;

namespace DINOForge.Runtime.HotReload
{
    /// <summary>
    /// Connects the SDK <see cref="PackFileWatcher"/> to the ECS runtime.
    /// Listens for hot-reload events, logs changes, and coordinates
    /// registry updates with the in-game entity state.
    /// </summary>
    public class HotReloadBridge : IDisposable
    {
        private readonly PackFileWatcher _watcher;
        private readonly RegistryManager _registryManager;
        private readonly ManualLogSource _log;
        private bool _disposed;

        /// <summary>Raised after a successful hot reload cycle updates runtime state.</summary>
        public event EventHandler<HotReloadResult>? OnRuntimeUpdated;

        /// <summary>
        /// Initializes the bridge between SDK file watcher and ECS runtime.
        /// </summary>
        /// <param name="watcher">The SDK pack file watcher.</param>
        /// <param name="registryManager">The registry manager to inspect after reload.</param>
        /// <param name="log">BepInEx logger for output.</param>
        public HotReloadBridge(PackFileWatcher watcher, RegistryManager registryManager, ManualLogSource log)
        {
            _watcher = watcher ?? throw new ArgumentNullException(nameof(watcher));
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
            _log = log ?? throw new ArgumentNullException(nameof(log));

            // Subscribe to watcher events
            _watcher.OnPackContentChanged += HandlePackContentChanged;
            _watcher.OnPackReloaded += HandlePackReloaded;
            _watcher.OnPackReloadFailed += HandlePackReloadFailed;
        }

        /// <summary>
        /// Starts the underlying file watcher.
        /// </summary>
        public void Start()
        {
            _watcher.Start();
            _log.LogInfo("[HotReloadBridge] Hot reload watching started.");
        }

        /// <summary>
        /// Stops the underlying file watcher.
        /// </summary>
        public void Stop()
        {
            _watcher.Stop();
            _log.LogInfo("[HotReloadBridge] Hot reload watching stopped.");
        }

        /// <summary>
        /// Triggers a full manual reload of all packs.
        /// </summary>
        /// <returns>Result of the reload operation.</returns>
        public HotReloadResult TriggerReload()
        {
            _log.LogInfo("[HotReloadBridge] Manual reload triggered.");
            HotReloadResult result = _watcher.ReloadAll();

            if (result.IsSuccess)
            {
                ApplyRuntimeUpdates(result);
            }
            else
            {
                foreach (string error in result.Errors)
                {
                    _log.LogError($"[HotReloadBridge] Reload error: {error}");
                }
            }

            return result;
        }

        private void HandlePackContentChanged(object? sender, PackContentChangedEventArgs e)
        {
            _log.LogInfo($"[HotReloadBridge] File changed: {e.FilePath}");
        }

        private void HandlePackReloaded(object? sender, HotReloadResult result)
        {
            _log.LogInfo($"[HotReloadBridge] Pack reload succeeded. " +
                $"Changed files: {result.ChangedFiles.Count}, Updated entries: {result.UpdatedEntries.Count}");

            ApplyRuntimeUpdates(result);
        }

        private void HandlePackReloadFailed(object? sender, HotReloadResult result)
        {
            _log.LogWarning($"[HotReloadBridge] Pack reload had errors:");
            foreach (string error in result.Errors)
            {
                _log.LogError($"  {error}");
            }

            // Partial updates may still have been applied
            if (result.UpdatedEntries.Count > 0)
            {
                _log.LogInfo($"[HotReloadBridge] Partial update applied: {result.UpdatedEntries.Count} entries.");
                ApplyRuntimeUpdates(result);
            }
        }

        /// <summary>
        /// Applies registry changes to the ECS runtime.
        /// In the current implementation, this logs the changes and notifies listeners.
        /// Future versions will find affected entities and update component data.
        /// </summary>
        private void ApplyRuntimeUpdates(HotReloadResult result)
        {
            foreach (string entry in result.UpdatedEntries)
            {
                _log.LogInfo($"[HotReloadBridge] Registry entry updated: {entry}");
            }

            // TODO: Find affected entities in the ECS world and update their component data.
            // This requires knowledge of which entities correspond to which registry entries,
            // which will be implemented as part of the runtime entity tracking system.

            OnRuntimeUpdated?.Invoke(this, result);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _watcher.OnPackContentChanged -= HandlePackContentChanged;
            _watcher.OnPackReloaded -= HandlePackReloaded;
            _watcher.OnPackReloadFailed -= HandlePackReloadFailed;

            Stop();
        }
    }
}
