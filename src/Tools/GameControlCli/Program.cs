#nullable enable
using System.CommandLine;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using Spectre.Console;

namespace DINOForge.Tools.GameControlCli;

/// <summary>
/// DINOForge Game Control CLI - Standalone command-line interface for checking game state.
/// Communicates directly with the running game via named pipes (GameClient).
/// Does NOT interact with the screen or other windows.
/// </summary>
public static class Program
{
    public static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            ShowHelp();
            return 0;
        }

        string command = args[0];
        return command switch
        {
            "status" => await HandleStatusCommand(),
            "wait-world" => await HandleWaitWorldCommand(),
            "resources" => await HandleResourcesCommand(),
            "screenshot" => await HandleScreenshotCommand(args.Skip(1).FirstOrDefault()),
            "catalog" => await HandleCatalogCommand(args.Skip(1).FirstOrDefault()),
            "entities" => await HandleEntitiesCommand(args.Skip(1).FirstOrDefault()),
            "load-scene" => await HandleLoadSceneCommand(args.Skip(1).FirstOrDefault()),
            "--help" or "-h" => ShowHelpAndReturn(0),
            _ => ShowHelpAndReturn(1)
        };
    }

    private static void ShowHelp()
    {
        AnsiConsole.MarkupLine("[cyan bold]DINOForge Game Control CLI[/]");
        AnsiConsole.MarkupLine("[yellow]Direct game process communication via named pipes[/]");
        AnsiConsole.MarkupLine("");
        AnsiConsole.MarkupLine("[green]Commands:[/]");
        AnsiConsole.MarkupLine("  status           - Check game connection and status");
        AnsiConsole.MarkupLine("  wait-world       - Wait for ECS world to be ready");
        AnsiConsole.MarkupLine("  resources        - Show current resource values");
        AnsiConsole.MarkupLine("  screenshot       - Capture in-game screenshot");
        AnsiConsole.MarkupLine("  catalog [cat]    - Dump game catalog (units/buildings/projectiles)");
        AnsiConsole.MarkupLine("  entities [comp]  - Query entities by component type");
        AnsiConsole.MarkupLine("  load-scene [name]- Load a game scene by name (default: Sandbox)");
        AnsiConsole.MarkupLine("  --help, -h       - Show this help");
    }

    private static int ShowHelpAndReturn(int code)
    {
        if (code != 0) AnsiConsole.MarkupLine("[red]Invalid command[/]");
        ShowHelp();
        return code;
    }

    private static async Task<int> HandleStatusCommand()
    {
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();
            AnsiConsole.MarkupLine("[green]✓[/] Connected to game bridge");

            var status = await client.StatusAsync();
            AnsiConsole.MarkupLine($"[cyan]Running:[/] {status.Running}");
            AnsiConsole.MarkupLine($"[cyan]World ready:[/] {status.WorldReady}");
            AnsiConsole.MarkupLine($"[cyan]World name:[/] {status.WorldName}");
            AnsiConsole.MarkupLine($"[cyan]Entity count:[/] {status.EntityCount}");
            AnsiConsole.MarkupLine($"[cyan]Mod platform ready:[/] {status.ModPlatformReady}");
            AnsiConsole.MarkupLine($"[cyan]Loaded packs:[/] {status.LoadedPacks.Count}");
            foreach (var pack in status.LoadedPacks)
            {
                AnsiConsole.MarkupLine($"  - {pack}");
            }
            AnsiConsole.MarkupLine($"[cyan]Version:[/] {status.Version}");

            client.Disconnect();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleWaitWorldCommand()
    {
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();

            await AnsiConsole.Progress()
                .StartAsync(async progress =>
                {
                    var task = progress.AddTask("[cyan]Waiting for ECS world...[/]");
                    await client.WaitForWorldAsync(30000);
                    task.Increment(100);
                });

            AnsiConsole.MarkupLine("[green]✓[/] ECS World is ready");
            client.Disconnect();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleResourcesCommand()
    {
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();
            var result = await client.GetResourcesAsync();

            AnsiConsole.MarkupLine("[cyan]Game Resources:[/]");
            AnsiConsole.MarkupLine($"  Food:   [yellow]{result.Food}[/]");
            AnsiConsole.MarkupLine($"  Wood:   [yellow]{result.Wood}[/]");
            AnsiConsole.MarkupLine($"  Stone:  [yellow]{result.Stone}[/]");
            AnsiConsole.MarkupLine($"  Iron:   [yellow]{result.Iron}[/]");
            AnsiConsole.MarkupLine($"  Money:  [yellow]{result.Money}[/]");
            AnsiConsole.MarkupLine($"  Souls:  [yellow]{result.Souls}[/]");
            AnsiConsole.MarkupLine($"  Bones:  [yellow]{result.Bones}[/]");
            AnsiConsole.MarkupLine($"  Spirit: [yellow]{result.Spirit}[/]");

            client.Disconnect();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleScreenshotCommand(string? outputPath)
    {
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();

            await AnsiConsole.Progress()
                .StartAsync(async progress =>
                {
                    var task = progress.AddTask("[cyan]Taking screenshot...[/]");
                    await client.ScreenshotAsync(outputPath);
                    task.Increment(100);
                });

            AnsiConsole.MarkupLine($"[green]✓[/] Screenshot saved");
            client.Disconnect();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleCatalogCommand(string? category)
    {
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();
            var result = await client.DumpStateAsync(category);

            AnsiConsole.MarkupLine($"[cyan]Catalog dump ({category ?? "all"}):[/]");

            if (result.Units.Count > 0)
            {
                AnsiConsole.MarkupLine("[cyan]Units:[/]");
                foreach (var entry in result.Units.Take(15))
                {
                    AnsiConsole.MarkupLine($"  {entry.InferredId}: {entry.EntityCount} entities");
                }
            }

            if (result.Buildings.Count > 0)
            {
                AnsiConsole.MarkupLine("[cyan]Buildings:[/]");
                foreach (var entry in result.Buildings.Take(15))
                {
                    AnsiConsole.MarkupLine($"  {entry.InferredId}: {entry.EntityCount} entities");
                }
            }

            if (result.Projectiles.Count > 0)
            {
                AnsiConsole.MarkupLine($"[cyan]Projectiles: {result.Projectiles.Count}[/]");
            }

            client.Disconnect();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleEntitiesCommand(string? componentType)
    {
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();
            var result = await client.QueryEntitiesAsync(componentType);

            AnsiConsole.MarkupLine("[green]✓[/] Query complete");

            client.Disconnect();
            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> HandleLoadSceneCommand(string? sceneName)
    {
        sceneName ??= "Sandbox";
        using var client = new GameClient();
        try
        {
            await client.ConnectAsync();
            AnsiConsole.MarkupLine($"[cyan]Loading scene:[/] {sceneName}");
            var result = await client.LoadSceneAsync(sceneName);
            if (result.Success)
                AnsiConsole.MarkupLine($"[green]✓[/] Scene load dispatched: {result.Scene}");
            else
                AnsiConsole.MarkupLine($"[red]✗[/] Scene load failed");
            client.Disconnect();
            return result.Success ? 0 : 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]✗ Error:[/] {ex.Message}");
            return 1;
        }
    }
}
