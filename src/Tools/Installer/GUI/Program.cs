using Avalonia;
using System;

namespace DINOForge.Installer;

/// <summary>
/// Entry point for the DINOForge GUI installer application.
/// </summary>
internal static class Program
{
    /// <summary>
    /// Application entry point. Initializes Avalonia with desktop extensions.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Builds and configures the Avalonia application instance.
    /// </summary>
    /// <returns>Configured AppBuilder.</returns>
    public static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
