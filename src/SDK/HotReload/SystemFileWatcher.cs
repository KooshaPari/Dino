#nullable enable
using System;
using System.IO;

namespace DINOForge.SDK.HotReload
{
    /// <summary>
    /// Adapts <see cref="FileSystemWatcher"/> to the internal <see cref="IFileWatcher"/> port.
    /// </summary>
    internal sealed class SystemFileWatcher : IFileWatcher
    {
        private readonly FileSystemWatcher _watcher;

        public SystemFileWatcher(string rootDirectory)
        {
            _watcher = new FileSystemWatcher(rootDirectory)
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size
            };
        }

        public event FileSystemEventHandler? Changed
        {
            add
            {
                _watcher.Changed += value;
                _watcher.Created += value;
            }
            remove
            {
                _watcher.Changed -= value;
                _watcher.Created -= value;
            }
        }

        public event RenamedEventHandler? Renamed
        {
            add => _watcher.Renamed += value;
            remove => _watcher.Renamed -= value;
        }

        public string Filter
        {
            get => _watcher.Filter;
            set => _watcher.Filter = value;
        }

        public bool EnableRaisingEvents
        {
            get => _watcher.EnableRaisingEvents;
            set => _watcher.EnableRaisingEvents = value;
        }

        public void Dispose()
        {
            _watcher.Dispose();
        }
    }
}
