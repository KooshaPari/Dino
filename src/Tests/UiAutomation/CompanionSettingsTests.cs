#nullable enable
using FlaUI.Core.AutomationElements;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests.UiAutomation;

/// <summary>
/// COMP-UI-004: Settings page — game path text box accepts and persists a value.
/// COMP-UI-005: Status bar reflects bridge connection state.
/// </summary>
[Collection(UiAutomationCollection.Name)]
[Trait("Category", "UiAutomation")]
public sealed class CompanionSettingsTests(CompanionFixture fixture)
{
    [Fact]
    public void Settings_GamePathTextBox_AcceptsInput()
    {
        // Navigate to settings (SettingsButton in nav bar)
        AutomationElement? settingsNav = fixture.MainWindow!
            .FindFirstDescendant(cf => cf.ByAutomationId("NavSettings"));

        settingsNav.Should().NotBeNull("navigation must have a Settings item");
        settingsNav!.AsButton().Invoke();

        System.Threading.Thread.Sleep(300);

        AutomationElement? gamePathBox = fixture.MainWindow
            .FindFirstDescendant(cf => cf.ByAutomationId("GamePathInput"));

        gamePathBox.Should().NotBeNull("settings page must show GamePathInput text box");

        TextBox tb = gamePathBox!.AsTextBox();
        tb.Enter("C:\\FakeGamePath\\DINO.exe");

        tb.Text.Should().Be("C:\\FakeGamePath\\DINO.exe",
            "entered path should appear in the text box");
    }

    [Fact]
    public void StatusBar_ShowsDisconnected_WhenBridgeAbsent()
    {
        // When launched without a running game, status bar should indicate no bridge
        AutomationElement? statusBar = fixture.MainWindow!
            .FindFirstDescendant(cf => cf.ByAutomationId("BridgeStatusText"));

        statusBar.Should().NotBeNull("a bridge status indicator must be present");

        string text = statusBar!.AsLabel().Text;
        text.Should().MatchRegex("(Not connected|Disconnected|No game)",
            "status should clearly indicate the bridge is not available");
    }
}
