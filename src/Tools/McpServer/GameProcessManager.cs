#nullable enable
using System.Diagnostics;
using System.Text;

namespace DINOForge.Tools.McpServer;

/// <summary>
/// Manages the Diplomacy is Not an Option game process lifecycle.
/// Supports launching from the main install path or the TEST (isolated) instance path.
/// Uses hidden window style for TEST launches to avoid cluttering the taskbar.
/// </summary>
public sealed class GameProcessManager : IDisposable
{
    private Process? _process;
    private readonly string _exeName = "Diplomacy is Not an Option.exe";
    private readonly string? _testInstancePath;

    /// <summary>Returns true if a game process is currently running.</summary>
    public bool IsRunning => _process != null && !_process.HasExited;

    /// <summary>
    /// Creates a new GameProcessManager.
    /// Reads the TEST instance path from .dino_test_instance_path if present.
    /// </summary>
    public GameProcessManager()
    {
        string? testPath = TryReadTestInstancePath();
        if (!string.IsNullOrEmpty(testPath) && Directory.Exists(testPath))
        {
            string testExe = Path.Combine(testPath, _exeName);
            if (File.Exists(testExe))
                _testInstancePath = testPath;
        }
    }

    /// <summary>
    /// Launches the game from the TEST instance directory (hidden window).
    /// The TEST path bypasses Unity's native single-instance mutex.
    /// </summary>
    /// <param name="hidden">If true, launch with hidden window style.</param>
    /// <returns>True if the process was launched successfully.</returns>
    public bool LaunchTestInstance(bool hidden = true)
    {
        if (_testInstancePath == null)
            return false;

        if (IsRunning)
            return true; // already running

        string exePath = Path.Combine(_testInstancePath, _exeName);
        if (!File.Exists(exePath))
            return false;

        EnsureSteamRunning();

        var psi = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = _testInstancePath,
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };

        if (hidden)
            psi.WindowStyle = ProcessWindowStyle.Hidden;

        Process? proc = null;
        try
        {
            proc = Process.Start(psi);
            if (proc is null)
                return false;
            _process = proc;
            return true;
        }
        catch
        {
            proc?.Dispose();
            return false;
        }
    }

    /// <summary>
    /// Launches the game from the default/install path (visible window).
    /// </summary>
    /// <param name="gamePath">Optional explicit path to the game executable.</param>
    /// <returns>True if the process was launched successfully.</returns>
    public Task<bool> LaunchAsync(string? gamePath = null)
    {
        return Task.FromResult(LaunchSync(gamePath));
    }

    private bool LaunchSync(string? gamePath)
    {
        if (IsRunning)
            return true;

        string? exePath = gamePath;
        if (string.IsNullOrEmpty(exePath))
        {
            // Probe common install locations
            exePath = ProbeGamePath();
        }

        if (string.IsNullOrEmpty(exePath) || !File.Exists(exePath))
            return false;

        EnsureSteamRunning();

        var psi = new ProcessStartInfo(exePath)
        {
            WorkingDirectory = Path.GetDirectoryName(exePath) ?? "",
            UseShellExecute = false,
            RedirectStandardOutput = false,
            RedirectStandardError = false,
        };

        Process? proc = null;
        try
        {
            proc = Process.Start(psi);
            if (proc is null)
                return false;
            _process = proc;
            return true;
        }
        catch
        {
            proc?.Dispose();
            return false;
        }
    }

    /// <summary>Returns the path to the TEST instance directory, or null if not found.</summary>
    public string? TestInstancePath => _testInstancePath;

    /// <summary>Returns true if a TEST instance path is configured.</summary>
    public bool HasTestInstance => _testInstancePath != null;

    private static string? TryReadTestInstancePath()
    {
        // Walk up from the executing assembly looking for .dino_test_instance_path
        string? dir = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            string markerPath = Path.Combine(dir, ".dino_test_instance_path");
            if (File.Exists(markerPath))
            {
                string? path = File.ReadAllText(markerPath, Encoding.UTF8).Trim();
                return Directory.Exists(path) ? path : null;
            }

            dir = Path.GetDirectoryName(dir);
            if (string.IsNullOrEmpty(dir))
                break;
        }

        // Also check current working directory
        string cwdMarker = Path.Combine(Environment.CurrentDirectory, ".dino_test_instance_path");
        if (File.Exists(cwdMarker))
        {
            string? path = File.ReadAllText(cwdMarker, Encoding.UTF8).Trim();
            return Directory.Exists(path) ? path : null;
        }

        return null;
    }

    private static string? ProbeGamePath()
    {
        string exeName = "Diplomacy is Not an Option.exe";

        // Check Steam library locations
        string[] steamRoots =
        [
            @"G:\SteamLibrary\steamapps\common\Diplomacy is Not an Option",
            @"C:\Program Files (x86)\Steam\steamapps\common\Diplomacy is Not an Option",
        ];

        foreach (string root in steamRoots)
        {
            string candidate = Path.Combine(root, exeName);
            if (File.Exists(candidate))
                return candidate;
        }

        return null;
    }

    /// <summary>
    /// Ensures the Steam client is running before launching the game.
    /// DINO's main-menu news/profile/leaderboard panels depend on a successful
    /// SteamAPI_Init; if the Steam client is not running those panels hang as
    /// loading skeletons. Launching the Steam client first (steam_appid.txt still
    /// keeps DINO from self-relaunching) lets SteamAPI_Init succeed so the panels
    /// populate. Windows-only; a no-op on other platforms.
    /// </summary>
    public static void EnsureSteamRunning()
    {
        if (!OperatingSystem.IsWindows())
            return;

        try
        {
            if (Process.GetProcessesByName("steam").Length > 0)
                return; // already running

            string[] steamExeCandidates =
            [
                @"C:\Program Files (x86)\Steam\steam.exe",
                @"C:\Program Files\Steam\steam.exe",
            ];

            string? steamExe = Array.Find(steamExeCandidates, File.Exists);
            if (steamExe is null)
                return; // Steam not installed where we expect; let launch proceed

            var steamPsi = new ProcessStartInfo(steamExe)
            {
                Arguments = "-silent",
                UseShellExecute = true,
            };
            using Process? steam = Process.Start(steamPsi);

            // Give the Steam client time to come up so SteamAPI_Init can connect.
            for (int i = 0; i < 30; i++)
            {
                if (Process.GetProcessesByName("steam").Length > 0)
                    break;
                Thread.Sleep(1000);
            }
        }
        catch
        {
            // safe-swallow: best-effort Steam launch; still attempt the game launch.
        }
    }

    public void Dispose()
    {
        if (_process is null)
            return;

        try
        {
            if (!_process.HasExited)
                _process.Kill();
        }
        catch (InvalidOperationException)
        {
            /* process already exited */
        }
        catch (System.ComponentModel.Win32Exception)
        {
            /* best-effort kill */
        }

        try { _process.Dispose(); } catch (ObjectDisposedException) { /* ignore */ }
        _process = null;
    }
}
