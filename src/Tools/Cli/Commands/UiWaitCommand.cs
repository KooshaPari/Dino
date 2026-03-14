#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Waits for UI state (visible, hidden, interactable).
/// </summary>
internal static class UiWaitCommand
{
    public static Command Create()
    {
        Argument<string> selectorArg = new("selector") { Description = "UI selector" };
        Option<string> stateOpt = new("--state") { Description = "State to wait for: visible, hidden, interactable, actionable" };
        Option<int> timeoutOpt = new("--timeout") { Description = "Timeout in milliseconds" };

        Command command = new("ui-wait", "Wait for UI element state");
        command.Add(selectorArg);
        command.Add(stateOpt);
        command.Add(timeoutOpt);

        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            string selector = parseResult.GetRequiredValue(selectorArg);
            string? state = parseResult.GetValue(stateOpt) ?? "visible";
            int timeout = parseResult.GetValue(timeoutOpt);
            if (timeout == 0) timeout = 5000;

            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            AnsiConsole.MarkupLine($"[bold]Waiting:[/] {selector} -> {state} (timeout: {timeout}ms)");

            UiWaitResult result = await client.WaitForUiAsync(selector, state, timeout, ct);

            if (result.Ready)
            {
                AnsiConsole.MarkupLine($"[green]✓[/] {result.Message}");
            }
            else
            {
                AnsiConsole.MarkupLine($"[red]✗[/] {result.Message}");
            }

            AnsiConsole.MarkupLine($"[dim]Match count: {result.MatchCount}[/]");
        });
        return command;
    }
}
