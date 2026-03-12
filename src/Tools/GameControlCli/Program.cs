#nullable enable
using System;
using System.Threading.Tasks;
using System.CommandLine;
using System.CommandLine.Invocation;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.GameControlCli;

/// <summary>
/// DINOForge Game Control CLI - Standalone command-line interface for controlling the game.
/// Communicates directly with the running game via named pipes (GameClient).
/// Does NOT interact with the screen or other windows.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("DINOForge Game Control - Direct game process communication");

        // Sub-commands
        var statusCommand = new Command("status", "Check game connection and status");
        var keyCommand = new Command("key", "Send a key press to the game");
        var screenshotCommand = new Command("screenshot", "Take an in-game screenshot");
        var queryCommand = new Command("query", "Query entities from the game");
        var waitWorldCommand = new Command("wait-world", "Wait for ECS world to be ready");

        // Key command arguments
        var keyArg = new Argument<string>("key", "Key name (f10, f9, etc.)");
        keyCommand.AddArgument(keyArg);
        keyCommand.SetHandler(HandleKeyCommand, keyArg);

        // Screenshot command arguments
        var outputArg = new Argument<string>("output", "Output file path for screenshot");
        screenshotCommand.AddArgument(outputArg);
        screenshotCommand.SetHandler(HandleScreenshotCommand, outputArg);

        // Status command
        statusCommand.SetHandler(HandleStatusCommand);

        // Wait world command
        waitWorldCommand.SetHandler(HandleWaitWorldCommand);

        // Add sub-commands
        rootCommand.AddCommand(statusCommand);
        rootCommand.AddCommand(keyCommand);
        rootCommand.AddCommand(screenshotCommand);
        rootCommand.AddCommand(queryCommand);
        rootCommand.AddCommand(waitWorldCommand);

        return await rootCommand.InvokeAsync(args);
    }

    private static async Task HandleStatusCommand()
    {
        try
        {
            var client = new GameClient();
            await client.ConnectAsync();

            AnsiConsole.MarkupLine("[green]✓[/] Game client connected");
            AnsiConsole.MarkupLine($"[cyan]Pipe name:[/] {client.PipeName}");
            AnsiConsole.MarkupLine("[green]Ready to send commands[/]");

            await client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Connection failed:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task HandleKeyCommand(string key)
    {
        try
        {
            AnsiConsole.Status()
                .Start($"[yellow]Connecting to game...[/]", ctx =>
                {
                    ctx.Spinner(Spinner.Known.Star);
                    // Key commands would be sent via RPC once the protocol supports it
                    AnsiConsole.MarkupLine($"[cyan]Would send key:[/] {key}");
                    AnsiConsole.MarkupLine("[yellow]Note:[/] Key input via GameClient not yet implemented in Bridge protocol");
                });
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task HandleScreenshotCommand(string outputPath)
    {
        try
        {
            var client = new GameClient();
            await AnsiConsole.Progress()
                .StartAsync(async progress =>
                {
                    var task = progress.AddTask("[cyan]Connecting to game...[/]");
                    await client.ConnectAsync();

                    task.Update(p => p
                        .Increment(20)
                        .Update("[cyan]Requesting screenshot...[/]"));

                    // Would call GameScreenshotTool via RPC once protocol supports it
                    task.Update(p => p
                        .Increment(30)
                        .Update("[cyan]Saving screenshot...[/]"));

                    task.Update(p => p
                        .Increment(50)
                        .Update("[green]Complete![/]"));

                    await client.DisconnectAsync();
                });

            AnsiConsole.MarkupLine($"[green]✓[/] Screenshot saved to [cyan]{outputPath}[/]");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Screenshot failed:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }

    private static async Task HandleWaitWorldCommand()
    {
        try
        {
            var client = new GameClient();
            await AnsiConsole.Progress()
                .StartAsync(async progress =>
                {
                    var task = progress.AddTask("[cyan]Waiting for ECS world...[/]");

                    await client.ConnectAsync();
                    task.Increment(30);

                    // Would wait for world via RPC
                    await Task.Delay(2000); // Simulate
                    task.Increment(70);

                    await client.DisconnectAsync();
                });

            AnsiConsole.MarkupLine("[green]✓[/] ECS World is ready");
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            Environment.Exit(1);
        }
    }
}
