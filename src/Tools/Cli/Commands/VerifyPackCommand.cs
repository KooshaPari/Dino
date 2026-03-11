#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// End-to-end pack verification against the running game.
/// </summary>
internal static class VerifyPackCommand
{
    /// <summary>
    /// Creates the <c>verify</c> command.
    /// </summary>
    public static Command Create()
    {
        Argument<string> packPathArg = new("packPath") { Description = "Path to the pack directory to verify" };
        Command command = new("verify", "End-to-end pack verification");
        command.Add(packPathArg);

        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            string packPath = parseResult.GetRequiredValue(packPathArg);
            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            VerifyResult result = await client.VerifyModAsync(packPath, ct);

            Table table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Pack Verification Report[/]")
                .AddColumn("Property")
                .AddColumn("Value");

            table.AddRow("Pack ID", Markup.Escape(result.PackId));
            table.AddRow("Loaded", result.Loaded ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("Entity Count", result.EntityCount.ToString("N0"));

            AnsiConsole.Write(table);

            if (result.StatChanges.Count > 0)
            {
                AnsiConsole.MarkupLine("\n[bold]Stat Changes:[/]");
                foreach (string change in result.StatChanges)
                {
                    AnsiConsole.MarkupLine($"  [green]+[/] {Markup.Escape(change)}");
                }
            }

            if (result.Errors.Count > 0)
            {
                AnsiConsole.MarkupLine("\n[bold red]Errors:[/]");
                foreach (string error in result.Errors)
                {
                    AnsiConsole.MarkupLine($"  [red]x[/] {Markup.Escape(error)}");
                }
            }

            if (result.Errors.Count == 0 && result.Loaded)
            {
                AnsiConsole.MarkupLine("\n[green bold]PASS[/]");
            }
            else
            {
                AnsiConsole.MarkupLine("\n[red bold]FAIL[/]");
            }
        });
        return command;
    }
}
