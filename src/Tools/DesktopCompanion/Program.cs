using System;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.WindowsAppRuntime.Bootstrap;

namespace DINOForge.DesktopCompanion
{
    /// <summary>
    /// Application entry point for the unpackaged WinUI 3 companion app.
    /// Must initialize the Windows App Runtime bootstrap before any WinUI code.
    /// </summary>
    public static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Bootstrap Windows App Runtime for unpackaged apps.
            // This must happen before any WinUI/XAML type is touched.
            try
            {
                Bootstrap.Initialize(0x00010006); // WindowsAppSDK 1.6
            }
            catch (Exception ex)
            {
                string logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "DINOForge", "companion-crash.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.WriteAllText(logPath,
                    $"[{DateTime.Now:O}] Bootstrap.Initialize failed:\n{ex}\n");
                throw;
            }

            Application.Start(_ =>
            {
                global::WinRT.ComWrappersSupport.InitializeComWrappers();
                App app = new App();
            });
        }
    }
}
