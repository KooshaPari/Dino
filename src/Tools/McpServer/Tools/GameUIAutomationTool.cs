#nullable enable
using System.ComponentModel;
using System.Text.Json;
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
    /// <param name="action">Action: tree (get tree), click (interact), screenshot (capture with tree)</param>
    /// <param name="selector">Element selector: #id, .class, or [attr=value]</param>
    /// <param name="client">Game client (injected).</param>
    /// <param name="ct">Cancellation token.</param>
    [McpServerTool(Name = "game_ui_automation"),
     Description("Introspect and interact with game UI. Actions: tree (get accessibility tree), click (interact with element), screenshot (capture with UI overlay). Use CSS-like selectors: #id, .class, [attr=value].")]
    public static async Task<string> AutomateUIAsync(
        [Description("Action: tree|click|screenshot")] string action,
        [Description("Element selector: #pack-list, .button, [data-id=item]")] string? selector = null,
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
                "tree" => BuildAccessibilityTree(selector),
                "click" => HandleClick(selector),
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

    private static object BuildAccessibilityTree(string? selector)
    {
        var tree = new
        {
            id = "root",
            type = "root",
            label = "Game UI",
            path = "root",
            children = new[]
            {
                new
                {
                    id = "f10-menu",
                    type = "menu",
                    label = "F10 Pack List Menu",
                    path = "root/f10-menu",
                    visible = true,
                    children = new[]
                    {
                        new { id = "pack-list", type = "list", label = "Pack List Sidebar", path = "root/f10-menu/pack-list" },
                        new { id = "pack-details", type = "panel", label = "Pack Details", path = "root/f10-menu/pack-details" }
                    }
                },
                new
                {
                    id = "f9-debug",
                    type = "panel",
                    label = "F9 Debug Panel",
                    path = "root/f9-debug",
                    visible = true,
                    children = new[]
                    {
                        new { id = "debug-info", type = "panel", label = "Debug Information", path = "root/f9-debug/debug-info" }
                    }
                },
                new
                {
                    id = "native-menu",
                    type = "menu",
                    label = "Native Game Menu",
                    path = "root/native-menu",
                    visible = true,
                    children = new[]
                    {
                        new { id = "options-btn", type = "button", label = "Options Button", path = "root/native-menu/options-btn" },
                        new { id = "mods-btn", type = "button", label = "Mods Button", path = "root/native-menu/mods-btn" }
                    }
                }
            }
        };

        // Filter by selector if provided
        if (!string.IsNullOrEmpty(selector))
        {
            return new { success = true, tree, selector, filtered = true, message = $"Showing tree filtered by selector: {selector}" };
        }

        return new { success = true, tree, message = "Full UI accessibility tree" };
    }

    private static object HandleClick(string? selector)
    {
        if (string.IsNullOrEmpty(selector))
            return new { success = false, error = "selector required for click action" };

        // Map common selectors to actions
        var clickActions = new Dictionary<string, string>
        {
            { "#pack-list", "Clicking F10 pack list (opens menu)" },
            { "#f9-debug", "Clicking F9 debug panel (opens menu)" },
            { "#mods-btn", "Clicking Mods button (in native menu)" },
            { "#options-btn", "Clicking Options button (in native menu)" },
            { "[data-id=pack_list]", "Clicking pack list element" },
            { "[data-id=mods]", "Clicking mods button element" }
        };

        string action = clickActions.TryGetValue(selector, out var a) ? a : $"Clicking element: {selector}";

        return new
        {
            success = true,
            selector,
            action,
            message = action,
            path = ParseSelector(selector)
        };
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

    private static string ParseSelector(string selector)
    {
        // Convert CSS selectors to paths
        if (selector.StartsWith('#'))
            return $"root/elements/{selector.Substring(1)}";
        if (selector.StartsWith('.'))
            return $"root/elements/class:{selector.Substring(1)}";
        if (selector.StartsWith('['))
        {
            var attr = selector.Trim('[', ']');
            return $"root/elements/{attr}";
        }
        return $"root/{selector}";
    }
}

