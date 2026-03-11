#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Shows current in-game resource values.
/// </summary>
internal static class ResourcesCommand
{
    /// <summary>
    /// Creates the <c>resources</c> command.
    /// </summary>
    public static Command Create()
    {
        Command command = new("resources", "Show current resource values");
        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            ResourceSnapshot resources = await client.GetResourcesAsync(ct);

            Table table = new Table()
                .Border(TableBorder.Rounded)
                .Title("[bold]Resources[/]")
                .AddColumn("Resource")
                .AddColumn(new TableColumn("Amount").RightAligned());

            table.AddRow("Food", resources.Food.ToString("N0"));
            table.AddRow("Wood", resources.Wood.ToString("N0"));
            table.AddRow("Stone", resources.Stone.ToString("N0"));
            table.AddRow("Iron", resources.Iron.ToString("N0"));
            table.AddRow("Money (Gold)", resources.Money.ToString("N0"));
            table.AddRow("Souls", resources.Souls.ToString("N0"));
            table.AddRow("Bones", resources.Bones.ToString("N0"));
            table.AddRow("Spirit", resources.Spirit.ToString("N0"));

            AnsiConsole.Write(table);
        });
        return command;
    }
}
