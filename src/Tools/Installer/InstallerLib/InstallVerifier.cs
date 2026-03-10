using System;
using System.Collections.Generic;
using System.IO;

namespace DINOForge.Tools.Installer
{
    /// <summary>
    /// Verifies that DINOForge and its dependencies are properly installed
    /// in a DINO game directory.
    /// </summary>
    public static class InstallVerifier
    {
        /// <summary>
        /// Performs a full verification of the DINOForge installation.
        /// </summary>
        /// <param name="gamePath">Path to the DINO game directory.</param>
        /// <returns>Install status with any issues found.</returns>
        public static InstallStatus Verify(string gamePath)
        {
            List<string> issues = new List<string>();

            if (string.IsNullOrWhiteSpace(gamePath))
            {
                issues.Add("Game path is null or empty.");
                return new InstallStatus(false, false, false, false, issues);
            }

            if (!Directory.Exists(gamePath))
            {
                issues.Add($"Game directory does not exist: {gamePath}");
                return new InstallStatus(false, false, false, false, issues);
            }

            bool gameExists = VerifyGameFiles(gamePath, issues);
            bool bepInExInstalled = VerifyBepInEx(gamePath, issues);
            bool runtimeInstalled = VerifyRuntime(gamePath, issues);
            bool packsReady = VerifyPacksDirectory(gamePath, issues);

            return new InstallStatus(gameExists, bepInExInstalled, runtimeInstalled, packsReady, issues);
        }

        /// <summary>
        /// Checks that the game executable exists.
        /// </summary>
        public static bool VerifyGameFiles(string gamePath, List<string> issues)
        {
            // Check for the game executable (Windows or Linux)
            string windowsExe = Path.Combine(gamePath, "Diplomacy is Not an Option.exe");
            string linuxExe = Path.Combine(gamePath, "Diplomacy is Not an Option.x86_64");

            if (!File.Exists(windowsExe) && !File.Exists(linuxExe))
            {
                issues.Add("Game executable not found. Is this the correct game directory?");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies BepInEx 5 is properly installed (winhttp.dll, doorstop_config.ini, BepInEx/).
        /// </summary>
        public static bool VerifyBepInEx(string gamePath, List<string> issues)
        {
            bool valid = true;

            // Check winhttp.dll (Windows proxy DLL for Doorstop)
            string winhttpDll = Path.Combine(gamePath, "winhttp.dll");
            if (!File.Exists(winhttpDll))
            {
                issues.Add("BepInEx: winhttp.dll not found (Doorstop proxy).");
                valid = false;
            }

            // Check doorstop_config.ini
            string doorstopConfig = Path.Combine(gamePath, "doorstop_config.ini");
            if (!File.Exists(doorstopConfig))
            {
                issues.Add("BepInEx: doorstop_config.ini not found.");
                valid = false;
            }

            // Check BepInEx directory
            string bepInExDir = Path.Combine(gamePath, "BepInEx");
            if (!Directory.Exists(bepInExDir))
            {
                issues.Add("BepInEx: BepInEx/ directory not found.");
                valid = false;
            }
            else
            {
                // Check core directory
                string coreDir = Path.Combine(bepInExDir, "core");
                if (!Directory.Exists(coreDir))
                {
                    issues.Add("BepInEx: core/ directory not found.");
                    valid = false;
                }

                // Check plugins directory
                string pluginsDir = Path.Combine(bepInExDir, "plugins");
                if (!Directory.Exists(pluginsDir))
                {
                    issues.Add("BepInEx: plugins/ directory not found.");
                    valid = false;
                }
            }

            return valid;
        }

        /// <summary>
        /// Verifies the DINOForge Runtime DLL exists in BepInEx/plugins/.
        /// </summary>
        public static bool VerifyRuntime(string gamePath, List<string> issues)
        {
            string runtimeDll = Path.Combine(gamePath, "BepInEx", "plugins", "DINOForge.Runtime.dll");
            if (!File.Exists(runtimeDll))
            {
                issues.Add("DINOForge: Runtime DLL not found at BepInEx/plugins/DINOForge.Runtime.dll.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Verifies the packs directory exists.
        /// </summary>
        public static bool VerifyPacksDirectory(string gamePath, List<string> issues)
        {
            string packsDir = Path.Combine(gamePath, "packs");
            if (!Directory.Exists(packsDir))
            {
                issues.Add("DINOForge: packs/ directory not found.");
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Represents the installation status of DINOForge and its dependencies.
    /// </summary>
    public class InstallStatus
    {
        /// <summary>
        /// Whether the DINO game files were found.
        /// </summary>
        public bool GameExists { get; }

        /// <summary>
        /// Whether BepInEx is properly installed.
        /// </summary>
        public bool BepInExInstalled { get; }

        /// <summary>
        /// Whether the DINOForge Runtime DLL is in place.
        /// </summary>
        public bool RuntimeInstalled { get; }

        /// <summary>
        /// Whether the packs directory exists.
        /// </summary>
        public bool PacksReady { get; }

        /// <summary>
        /// List of issues found during verification.
        /// </summary>
        public IReadOnlyList<string> Issues { get; }

        /// <summary>
        /// True when all components are properly installed.
        /// </summary>
        public bool IsFullyInstalled => GameExists && BepInExInstalled && RuntimeInstalled && PacksReady;

        public InstallStatus(
            bool gameExists,
            bool bepInExInstalled,
            bool runtimeInstalled,
            bool packsReady,
            IReadOnlyList<string> issues)
        {
            GameExists = gameExists;
            BepInExInstalled = bepInExInstalled;
            RuntimeInstalled = runtimeInstalled;
            PacksReady = packsReady;
            Issues = issues;
        }
    }
}
