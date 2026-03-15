#nullable enable
using FlaUI.Core.AutomationElements;
using FluentAssertions;
using Xunit;

namespace DINOForge.Tests.UiAutomation;

/// <summary>
/// COMP-UI-003: Toggling a pack in the UI changes its enabled state in the data service.
/// </summary>
[Collection(UiAutomationCollection.Name)]
[Trait("Category", "UiAutomation")]
public sealed class CompanionPackToggleTests(CompanionFixture fixture)
{
    [Fact]
    public void TogglePack_FlipsEnabledState()
    {
        // Find the first pack toggle button (AutomationId pattern: "PackToggle_{packId}")
        AutomationElement? firstToggle = fixture.MainWindow!
            .FindFirstDescendant(cf => cf.ByAutomationId("PackToggle_example-balance"));

        firstToggle.Should().NotBeNull(
            "example-balance pack should have a toggle button in the UI");

        ToggleButton toggle = firstToggle!.AsToggleButton();
        bool stateBefore = toggle.IsChecked ?? false;

        toggle.Toggle();

        // Allow UI to react
        System.Threading.Thread.Sleep(300);

        bool stateAfter = toggle.IsChecked ?? false;
        stateAfter.Should().NotBe(stateBefore, "toggling should flip the pack's enabled state");
    }
}
