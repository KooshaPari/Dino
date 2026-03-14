#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Asserts UI state (visible, text, count, etc.).
/// </summary>
internal static class UiExpectCommand
{
    public static Command Create()
    {
        Argument<string> selectorArg = new("selector") { Description = "UI selector" };
        Argument<string> conditionArg = new("condition") { Description = "Condition: visible, hidden, interactable, text=..., text-exact=..., count=N, count>=N" };

        Command command = new("ui-expect", "Assert UI element state");
        command.Add(selectorArg);
        command.Add(conditionArg);

        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            string selector = parseResult.GetRequiredValue(selectorArg);
            string condition = parseResult.GetRequiredValue(conditionArg);

            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            AnsiConsole.MarkupLine($"[bold]Expecting:[/] {selector} -> {condition}");

            UiExpectationResult result = await client.ExpectUiAsync(selector, condition, ct);

            if (result.Success)
            {
                AnsiConsole.MarkupLine($"[green]✓ PASS[/] {result.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗ FAIL[/] {result.Message}");
            }

            AnsiConsole.MarkupLine($"[dim]Match count: {result.MatchCount}[/]");

            // Exit with error code on failure for CI/scripting
            if (!result.Success)
            {
                Environment.Exit(1);
            }
        });
        return command;
    }
}
