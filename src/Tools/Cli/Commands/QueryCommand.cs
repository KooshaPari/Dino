#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Queries entities by SDK path or component type.
/// </summary>
internal static class QueryCommand
{
    /// <summary>
    /// Creates the <c>query</c> command.
    /// </summary>
    public static Command Create()
    {
        Argument<string> pathArg = new("path") { Description = "SDK path or component type to query" };
        Command command = new("query", "Query entities by SDK path or component type");
        command.Add(pathArg);

        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            string path = parseResult.GetRequiredValue(pathArg);
            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            QueryResult result = await client.QueryEntitiesAsync(componentType: path, ct: ct);

            AnsiConsole.MarkupLine($"[bold]Found {result.Count} entities[/]");

            if (result.Entities.Count == 0)
            {
                AnsiConsole.MarkupLine("[dim]No entities matched the query.[/]");
                return;
            }

            Table table = new Table()
                .Border(TableBorder.Rounded)
                .AddColumn("Index")
                .AddColumn("Components");

            foreach (EntityInfo entity in result.Entities)
            {
                table.AddRow(
                    entity.Index.ToString(),
                    Markup.Escape(string.Join(", ", entity.Components)));
            }

            AnsiConsole.Write(table);
        });
        return command;
    }
}
