#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Shows game and mod platform status.
/// </summary>
internal static class StatusCommand
{
    /// <summary>
    /// Creates the <c>status</c> command.
    /// </summary>
    public static Command Create()
    {
        Command command = new("status", "Show game and mod platform status");
        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            Bridge.Protocol.GameStatus status = await client.StatusAsync(ct);

            Table table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]DINOForge Status[/]")
                .AddColumn("Property")
                .AddColumn("Value");

            table.AddRow("Running", status.Running ? "[green]Yes[/]" : "[red]No[/]");
            table.AddRow("World Ready", status.WorldReady ? "[green]Yes[/]" : "[yellow]No[/]");
            table.AddRow("World Name", Markup.Escape(status.WorldName));
            table.AddRow("Entity Count", status.EntityCount.ToString("N0"));
            table.AddRow("Mod Platform Ready", status.ModPlatformReady ? "[green]Yes[/]" : "[yellow]No[/]");
            table.AddRow("Version", Markup.Escape(status.Version));
            table.AddRow("Loaded Packs", status.LoadedPacks.Count > 0
                ? Markup.Escape(string.Join(", ", status.LoadedPacks))
                : "[dim]None[/]");

            AnsiConsole.Write(table);
        });
        return command;
    }
}
