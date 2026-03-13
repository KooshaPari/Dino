#nullable enable
using System.ComponentModel;
using DINOForge.Bridge.Client;
using ModelContextProtocol.Server;

namespace DINOForge.Tools.McpServer.Tools;

/// <summary>
/// UI Automation tool - introspect and interact with game UI elements.
/// Provides accessibility tree for UI navigation and interaction via selectors.
/// </summary>
[McpServerToolType]
public sealed class GameUIAutomationTool
{
    /// <summary>
    /// Get UI accessibility tree or interact with UI elements.
    /// </summary>
    /// <param name="action">Action: tree, query, click, or screenshot.</param>
    /// <param name="selector">Selector such as "role=button&&text=Mods" or "name=DINOForge_ModsButton".</param>
    /// <param name="client">Game client (injected).</param>
    /// <param name="ct">Cancellation token.</param>
    [McpServerTool(Name = "game_ui_automation"),
     Description("Introspect and interact with game UI. Actions: tree, query, click, screenshot. Use selectors such as role=button&&text=Mods, name=DINOForge_ModsButton, id=root_dfcanvas_root_0_.")]
    public static async Task<string> AutomateUIAsync(
        [Description("Action: tree|query|click|screenshot")] string action,
        [Description("Selector such as role=button&&text=Mods")] string? selector = null,
        GameClient? client = null,
        CancellationToken ct = default)
    {
        try
        {
            if (client == null)
                return GameClientHelper.ToJson(new { success = false, error = "GameClient not available" });

            await client.ConnectAsync(ct);

            var response = action switch
            {
                "tree" => await HandleTree(client, selector, ct),
                "query" => await HandleQuery(client, selector, ct),
                "click" => await HandleClick(client, selector, ct),
                "screenshot" => await HandleScreenshot(client, selector, ct),
                _ => new { success = false, error = $"Unknown action: {action}" }
            };

            client.Disconnect();
            return GameClientHelper.ToJson(response);
        }
        catch (Exception ex)
        {
            return GameClientHelper.ToJson(new { success = false, error = ex.Message });
        }
    }

    private static async Task<object> HandleTree(GameClient client, string? selector, CancellationToken ct)
    {
        try
        {
            return await client.GetUiTreeAsync(selector, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"UI tree capture failed: {ex.Message}" };
        }
    }

    private static async Task<object> HandleQuery(GameClient client, string? selector, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(selector))
            return new { success = false, error = "selector required for query action" };

        try
        {
            return await client.QueryUiAsync(selector, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"UI query failed: {ex.Message}" };
        }
    }

    private static async Task<object> HandleClick(GameClient client, string? selector, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(selector))
            return new { success = false, error = "selector required for click action" };

        try
        {
            return await client.ClickUiAsync(selector, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"UI click failed: {ex.Message}" };
        }
    }

    private static async Task<object> HandleScreenshot(GameClient client, string? selector, CancellationToken ct)
    {
        try
        {
            var result = await client.ScreenshotAsync();

            return new
            {
                success = true,
                path = result.Path,
                selector,
                message = "Screenshot captured with UI automation",
                tree = "Use selector parameter to show UI elements overlay on screenshot"
            };
        }
        catch (Exception ex)
        {
            return new { success = false, error = $"Screenshot failed: {ex.Message}" };
        }
    }

}
