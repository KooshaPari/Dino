#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using Xunit;

namespace DINOForge.Tests.UiAutomation;

/// <summary>
/// xUnit collection fixture: launches the DesktopCompanion process and finds its main window
/// via Windows UI Automation (FlaUI + UIA3).
///
/// Required environment variables:
///   COMPANION_EXE  — path to DINOForge.DesktopCompanion.exe (built Release artifact)
///
/// All UI automation tests are tagged [Trait("Category","UiAutomation")] and run via
/// ui-automation.yml on a windows-latest GitHub Actions runner.
/// </summary>
public sealed class CompanionFixture : IAsyncLifetime
{
    private const string MainWindowAutomationId = "MainWindow";
    private const int WindowTimeoutMs = 15_000;

    private Application? _app;
    private UIA3Automation? _automation;

    public Window? MainWindow { get; private set; }

    public Task InitializeAsync()
    {
        string exePath = Environment.GetEnvironmentVariable("COMPANION_EXE")
            ?? throw new InvalidOperationException(
                "COMPANION_EXE environment variable is required for UI automation tests.");

        if (!File.Exists(exePath))
            throw new FileNotFoundException($"DesktopCompanion executable not found: {exePath}");

        _automation = new UIA3Automation();
        _app = Application.Launch(exePath);

        MainWindow = _app.GetMainWindow(_automation, TimeSpan.FromMilliseconds(WindowTimeoutMs));
        MainWindow.Should().NotBeNull("the companion main window must appear within the timeout");

        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        try { _app?.Close(); } catch { /* best-effort */ }
        _app?.Dispose();
        _automation?.Dispose();
        return Task.CompletedTask;
    }

    // FluentAssertions extension shim (FlaUI objects aren't null-checked by FA by default)
    private static void Should() { }
}

[CollectionDefinition(UiAutomationCollection.Name)]
public sealed class UiAutomationCollection : ICollectionFixture<CompanionFixture>
{
    public const string Name = "UiAutomation";
}
