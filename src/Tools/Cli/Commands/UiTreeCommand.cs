#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Captures a live snapshot of the Unity UI hierarchy.
/// </summary>
internal static class UiTreeCommand
{
    public static Command Create()
    {
        Option<string?> selectorOpt = new("--selector") { Description = "Optional selector string echoed in result" };
        Command command = new("ui-tree", "Capture live Unity UI hierarchy snapshot");
        command.Add(selectorOpt);

        command.SetAction(async (ParseResult parseResult, CancellationToken ct) =>
        {
            string? selector = parseResult.GetValue(selectorOpt);
            using GameClient? client = await CommandHelper.ConnectAsync(ct);
            if (client is null) return;

            UiTreeResult result = await client.GetUiTreeAsync(selector, ct);

            AnsiConsole.MarkupLine($"[bold]UI Tree Snapshot[/]");
            AnsiConsole.MarkupLine($"[dim]Success: {result.Success}[/]");
            AnsiConsole.MarkupLine($"[dim]Node Count: {result.NodeCount}[/]");
            AnsiConsole.MarkupLine($"[dim]Message: {result.Message}[/]");

            if (result.Root != null)
            {
                RenderTree(result.Root, 0);
            }
        });
        return command;
    }

    private static void RenderTree(UiNode node, int depth)
    {
        string indent = new string(' ', depth * 2);
        string role = $"[{GetRoleColor(node.Role)}]{node.Role}[/]";
        string label = string.IsNullOrEmpty(node.Label) ? "" : $" \"{node.Label}\"";
        string state = node.Visible ? "[green]V[/]" : "[red]!V[/]";
        state += node.Interactable ? "[green]I[/]" : "[red]!I[/]";

        AnsiConsole.MarkupLine($"{indent}{node.Name} {role}{label} {state}");

        foreach (var child in node.Children)
        {
            RenderTree(child, depth + 1);
        }
    }

    private static string GetRoleColor(string role) => role switch
    {
        "button" => "yellow",
        "toggle" => "cyan",
        "panel" => "blue",
        "text" => "green",
        "input" => "magenta",
        "canvas" => "red",
        _ => "white"
    };
}
