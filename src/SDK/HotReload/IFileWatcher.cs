#nullable enable
using System;
using System.IO;

namespace DINOForge.SDK.HotReload
{
    /// <summary>
    /// Abstraction over filesystem change notifications used by <see cref="PackFileWatcher"/>.
    /// </summary>
    internal interface IFileWatcher : IDisposable
    {
        /// <summary>Raised when a relevant file is created or changed.</summary>
        event FileSystemEventHandler? Changed;

        /// <summary>Raised when a relevant file is renamed.</summary>
        event RenamedEventHandler? Renamed;

        /// <summary>Gets or sets the watched file filter.</summary>
        string Filter { get; set; }

        /// <summary>Gets or sets whether notifications are enabled.</summary>
        bool EnableRaisingEvents { get; set; }
    }
}
