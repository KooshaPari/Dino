#nullable enable
using DINOForge.Bridge.Client;
using Spectre.Console;

namespace DINOForge.Tools.Cli.Commands;

/// <summary>
/// Shared helper for CLI commands that need a <see cref="GameClient"/> connection.
/// </summary>
internal static class CommandHelper
{
    /// <summary>
    /// Creates and connects a <see cref="GameClient"/> to the in-game bridge.
    /// Displays a friendly error message and returns null if the game is not running.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A connected <see cref="GameClient"/>, or null if the connection failed.</returns>
    public static async Task<GameClient?> ConnectAsync(CancellationToken ct = default, bool writeErrors = true)
    {
        GameClient client = new();
        try
        {
            await client.ConnectAsync(ct).ConfigureAwait(false);
            return client;
        }
        catch (GameClientException)
        {
            if (writeErrors)
            {
                AnsiConsole.MarkupLine("[red]Error:[/] Game not running. Start DINO first.");
            }

            client.Dispose();
            return null;
        }
        catch (OperationCanceledException)
        {
            client.Dispose();
            throw;
        }
    }
}
