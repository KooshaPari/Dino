#nullable enable
using System.ComponentModel;
using DINOForge.Bridge.Client;
using DINOForge.Bridge.Protocol;
using ModelContextProtocol.Server;

namespace DINOForge.Tools.McpServer.Tools;

/// <summary>
/// MCP tool that captures a screenshot of the game window.
/// </summary>
[McpServerToolType]
public sealed class GameScreenshotTool
{
    /// <summary>
    /// Captures a screenshot of the game window and saves it to disk.
    /// </summary>
    /// <param name="client">The game client (injected).</param>
    /// <param name="path">Optional file path to save the screenshot. If omitted, uses a default temp location.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>JSON with the screenshot file path and dimensions.</returns>
    [McpServerTool(Name = "game_screenshot"), Description("Capture a screenshot of the game window. Returns the file path and image dimensions.")]
    public static async Task<string> ScreenshotAsync(
        GameClient client,
        [Description("Optional file path to save the screenshot")] string? path = null,
        CancellationToken ct = default)
    {
        if (!await GameClientHelper.EnsureConnectedAsync(client, ct).ConfigureAwait(false))
        {
            return GameClientHelper.ToJson(new { success = false, error = GameClientHelper.NotConnectedMessage });
        }

        try
        {
            ScreenshotResult result = await client.ScreenshotAsync(path, ct).ConfigureAwait(false);
            return GameClientHelper.ToJson(new
            {
                success = result.Success,
                path = result.Path,
                width = result.Width,
                height = result.Height
            });
        }
        catch (GameClientException ex)
        {
            return GameClientHelper.ToJson(new { success = false, error = ex.Message });
        }
    }
}
