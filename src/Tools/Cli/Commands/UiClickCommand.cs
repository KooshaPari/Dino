#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Clicks a UI element by selector.
/// </summary>
internal static class UiClickCommand
{
    public static Command Create()
    {
        Argument<string> selectorArg = new("selector") { Description = "UI selector (e.g., role=button&&text=Mods)" };
        Command command = new("ui-click", "Click UI element by selector");
        command.Add(selectorArg);

        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            string selector = parseResult.GetRequiredValue(selectorArg);
            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            AnsiConsole.MarkupLine($"[bold]Clicking:[/] {selector}");

            UiActionResult result = await client.ClickUiAsync(selector, ct);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {result.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {result.Message}");
                if (!string.IsNullOrEmpty(result.ActionabilityReason))
                {
                    AnsiConsole.MarkupLine($"[red]Reason:[/] {result.ActionabilityReason}");
                }
            }

            if (result.MatchedNode != null)
            {
                AnsiConsole.MarkupLine($"[dim]Target: {result.MatchedNode.Name} ({result.MatchedNode.Role})[/]");
            }
        });
        return command;
    }
}
