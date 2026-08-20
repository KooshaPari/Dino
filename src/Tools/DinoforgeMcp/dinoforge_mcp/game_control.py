"""
Module 1: Game Control & Bridge Tools
"""
from __future__ import annotations

import ctypes
import logging
import os
import subprocess
from pathlib import Path
from typing import Any

from fastmcp import FastMCP, Context

from .config import (
    _ensure_steam_appid, _get_test_instance_path, _get_vdd_index,
    _launch_hidden, _launch_on_vdd, _run_game_cli, GAME_DIR, GAME_EXE,
    _visual_validator, REPO_ROOT, _RUST_AVAILABLE, logger, _TEST_INSTANCE_PATH_FILE
)

def register(mcp: FastMCP):
    """Register game control tools with the MCP server."""

    @mcp.tool()
    async def game_status(ctx: Context, pipe_name: str | None = None) -> dict:
        """
        Get game connection status, world readiness, entity count, and loaded packs.

        Args:
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("status", pipe_name=pipe_name)

    @mcp.tool()
    async def game_wait_world(ctx: Context, timeout_seconds: int = 60, pipe_name: str | None = None) -> dict:
        """
        Wait until the ECS game world is ready (up to timeout_seconds).

        Args:
            timeout_seconds: Maximum seconds to wait.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("wait-world", timeout=timeout_seconds + 5, pipe_name=pipe_name)

    @mcp.tool()
    async def game_wait_for_world(ctx: Context, timeout_seconds: int = 60, pipe_name: str | None = None) -> dict:
        """
        Wait until the ECS game world is ready (up to timeout_seconds). Alias for game_wait_world.

        Args:
            timeout_seconds: Maximum seconds to wait.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("wait-world", timeout=timeout_seconds + 5, pipe_name=pipe_name)

    @mcp.tool()
    async def game_resources(ctx: Context, pipe_name: str | None = None) -> dict:
        """
        Get current in-game resources (gold, wood, food, etc.).

        Args:
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("resources", pipe_name=pipe_name)

    @mcp.tool()
    async def game_get_resources(ctx: Context, pipe_name: str | None = None) -> dict:
        """
        Get current in-game resources (gold, wood, food, etc.). Alias for game_resources.

        Args:
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("resources", pipe_name=pipe_name)

    @mcp.tool()
    async def game_screenshot(ctx: Context, output_path: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Capture a screenshot of the game window using a multi-tier fallback chain.

        Tier order (per docs/proposals/wgc-capture-backend-design.md, task #536):
          1. WGC (Windows.Graphics.Capture, via bare-cua-native) — foreground-independent,
             survives hung Unity / DXGI exclusive fullscreen / non-focused windows.
             5s timeout; falls through silently on failure.
          2. GameControlCli "screenshot" — named pipe → in-process FrameCapture
             (synchronous camera RenderTexture readback; #972). Highest-fidelity path,
             works in menu + in-game + loading, but BLOCKS when the game hangs.
          3. (Future) Last-resort PrintWindow / GDI via HiddenDesktopBackend.

        The returned dict includes a `backend` field ("wgc" | "game_control_cli")
        indicating which tier succeeded, in addition to the original keys produced
        by GameControlCli (success, path, error, etc.).

        Args:
            output_path: Optional file path to save the PNG. Defaults to a temp path.
            pipe_name: Optional named pipe name for multi-instance support
                       (defaults to "dinoforge-game-bridge"). Only used by the
                       GameControlCli fallback tier.
        """
        # --- Tier 1: WGC (foreground-independent, hung-game safe) ---
        try:
            from ..capture_wgc import (
                DEFAULT_DINO_WINDOW_TITLE,
                capture_window_via_wgc,
                check_wgc_available,
            )
            if check_wgc_available():
                try:
                    wgc_out = Path(output_path) if output_path else None
                    wgc_result = await capture_window_via_wgc(
                        window_title=DEFAULT_DINO_WINDOW_TITLE,
                        output_path=wgc_out,
                        timeout_seconds=5.0,
                    )
                    if wgc_result.success:
                        d = wgc_result.to_dict()
                        d["backend"] = "wgc"
                        return d
                    try:
                        await ctx.info(f"WGC capture failed (will fall back): {wgc_result.error}")
                    except Exception:
                        logger.info("WGC capture failed (will fall back): %s", wgc_result.error)
                except Exception as e:
                    try:
                        await ctx.info(f"WGC capture exception (will fall back): {e}")
                    except Exception:
                        logger.info("WGC capture exception (will fall back): %s", e)
            else:
                logger.debug("WGC unavailable (bare-cua-native not found); using GameControlCli")
        except ImportError as e:
            logger.debug("capture_wgc unavailable: %s", e)

        # --- Tier 2: GameControlCli → named pipe → Unity ScreenCapture ---
        args = ["screenshot"]
        if output_path:
            args += ["--output", output_path]
        result = _run_game_cli(*args, pipe_name=pipe_name)
        if isinstance(result, dict):
            result.setdefault("backend", "game_control_cli")
        return result

    @mcp.tool()
    async def game_screenshot_wgc(
        ctx: Context,
        window_title: str = "Diplomacy is Not an Option",
        output_path: str | None = None,
        timeout_seconds: float = 8.0,
    ) -> dict:
        """
        Capture the game window directly via Windows.Graphics.Capture (WGC).

        Bypasses the GameBridge named pipe entirely — works on hung/unresponsive
        Unity processes and on DXGI exclusive fullscreen surfaces where GDI /
        PrintWindow / BitBlt return solid-black frames. Routes through
        bare-cua-native's WGC adapter (#537).

        Args:
            window_title: Exact window title to capture (FindWindowW). Defaults to DINO.
            output_path: PNG output path. Defaults to %TEMP%/DINOForge/wgc/wgc_<ts>.png.
            timeout_seconds: Hard cap on capture round-trip. Default 8s.

        Returns:
            dict with keys: success, path, width, height, backend ("wgc"),
            elapsed_ms, error.
        """
        from ..capture_wgc import capture_window_via_wgc

        out = Path(output_path) if output_path else None
        result = await capture_window_via_wgc(
            window_title=window_title,
            output_path=out,
            timeout_seconds=timeout_seconds,
        )
        return result.to_dict()

    @mcp.tool()
    async def game_query_entities(ctx: Context, component_type: str = "", pipe_name: str | None = None) -> dict:
        """
        Query ECS entities by component type.

        Args:
            component_type: Full ECS component type name, e.g. 'Components.Unit',
                            'Components.BuildingBase', 'Unity.Rendering.RenderMesh'.
                            Empty string returns all entities.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("entities", component_type, pipe_name=pipe_name)

    @mcp.tool()
    async def game_ui_tree(ctx: Context, selector: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Snapshot the live Unity UI hierarchy (Playwright-style DOM).

        Args:
            selector: Optional CSS-like selector to filter results.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return await game_dump_ui_tree(ctx, selector=selector, pipe_name=pipe_name)

    @mcp.tool()
    async def game_dump_ui_tree(
        ctx: Context,
        selector: str | None = None,
        include_cursor: bool = True,
        pipe_name: str | None = None,
    ) -> dict:
        """
        Dump a full runtime UI census snapshot with visual metadata and themed/native flags.

        Args:
            selector: Optional selector string to filter results.
            include_cursor: Whether to include hardware cursor metadata.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["ui-tree"]
        if selector:
            args.append(selector)
        args.append(f"includeCursor={str(include_cursor).lower()}")
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_click_button(ctx: Context, button_name: str, pipe_name: str | None = None) -> dict:
        """
        Click a named Unity UI button.

        Args:
            button_name: Unity UI button name (e.g. 'DINOForge_ModsButton', 'PlayButton').
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("click-button", button_name, pipe_name=pipe_name)

    @mcp.tool()
    async def game_load_scene(ctx: Context, scene_name: str, pipe_name: str | None = None) -> dict:
        """
        Load a game scene by name. Available: level0–level9 and others.

        Args:
            scene_name: Scene name or index.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("load-scene", scene_name, pipe_name=pipe_name)

    @mcp.tool()
    async def game_start(ctx: Context, pipe_name: str | None = None) -> dict:
        """
        Trigger game world load via ECS singleton (bypasses the main menu).

        Args:
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("start-game", pipe_name=pipe_name)

    @mcp.tool()
    async def game_dismiss(ctx: Context, pipe_name: str | None = None) -> dict:
        """
        Dismiss a 'Press Any Key to Continue' loading screen.

        Args:
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("dismiss", pipe_name=pipe_name)

    @mcp.tool()
    async def game_catalog(ctx: Context, category: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Dump the game's content catalog (units, buildings, projectiles).

        Args:
            category: Optional filter: 'units', 'buildings', 'projectiles'.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["catalog"]
        if category:
            args.append(category)
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_launch(ctx: Context, hidden: bool = False) -> dict:
        """
        Launch Diplomacy is Not an Option directly (bypasses Steam — safe to run
        alongside an existing session for testing).

        Args:
            hidden: If True, launch on the dedicated DINOForge VDD first, fallback to CreateDesktop.
        """
        if not GAME_EXE.exists():
            return {"success": False, "error": f"Game exe not found: {GAME_EXE}"}
        _ensure_steam_appid(GAME_DIR)
        try:
            if hidden:
                # Try VDD first (dedicated DINOForge virtual display, not user's Parsec VDD)
                vdd_result = await _launch_on_vdd(str(GAME_EXE))
                if vdd_result["success"]:
                    return vdd_result
                # Fall back to CreateDesktop if VDD not configured
                return await _launch_hidden(str(GAME_EXE), "DINOForge_Agent")
            subprocess.Popen([str(GAME_EXE)], cwd=str(GAME_DIR))
            return {"success": True, "message": f"Launched: {GAME_EXE}. Use game_wait_world to wait for ECS world."}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def game_launch_test(ctx: Context, hidden: bool = True) -> dict:
        """
        Launch the TEST instance of DINO (second concurrent instance for testing).
        Reads path from .dino_test_instance_path config file (if present) or uses default.
        Kill existing test instances first if needed.

        Args:
            hidden: If True (default), launch on an invisible Win32 desktop (CreateDesktop). Set to False for visible window.
        """
        test_dir = _get_test_instance_path()
        test_exe = Path(test_dir) / "Diplomacy is Not an Option.exe"
        if not test_exe.exists():
            error_msg = (
                f"Test game exe not found: {test_exe}. "
                f"Check that the test instance is installed at the path specified in {_TEST_INSTANCE_PATH_FILE} "
                f"or at the default location."
            )
            return {"success": False, "error": error_msg}
        _ensure_steam_appid(Path(test_dir))
        try:
            if hidden:
                return await _launch_hidden(str(test_exe), "DINOForge_Agent_Test")
            subprocess.Popen([str(test_exe)], cwd=test_dir)
            return {"success": True, "message": f"Launched TEST instance: {test_exe}. Use game_wait_world to wait for ECS world."}
        except Exception as e:
            error_msg = (
                f"Failed to launch test instance: {str(e)}. "
                f"If the path is wrong, update {_TEST_INSTANCE_PATH_FILE} or reinstall the test instance."
            )
            return {"success": False, "error": error_msg}

    @mcp.tool()
    async def game_launch_vdd(ctx: Context, width: int = 1920, height: int = 1080) -> dict:
        """
        Launch game on dedicated DINOForge virtual display (not user's personal VDD).
        Requires .dinoforge_vdd_index to be configured in repo root.

        Args:
            width: Virtual display width (default 1920).
            height: Virtual display height (default 1080).
        """
        if not GAME_EXE.exists():
            return {"success": False, "error": f"Game exe not found: {GAME_EXE}"}
        return await _launch_on_vdd(str(GAME_EXE), width, height)

    @mcp.tool()
    async def game_get_stat(ctx: Context, sdk_path: str, entity_index: int | None = None, pipe_name: str | None = None) -> dict:
        """
        Read a stat value from ECS entities by SDK model path.

        Args:
            sdk_path: Dot-separated SDK path (e.g. 'unit.stats.hp').
            entity_index: Optional specific entity index.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["get-stat", sdk_path]
        if entity_index is not None:
            args.append(str(entity_index))
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_apply_override(
        ctx: Context,
        sdk_path: str,
        value: float,
        mode: str | None = None,
        filter_component: str | None = None,
        pipe_name: str | None = None,
    ) -> dict:
        """
        Apply a stat override to matching ECS entities.

        Args:
            sdk_path: SDK model path (e.g. 'unit.stats.hp').
            value: The numeric value to apply.
            mode: 'override' (default), 'add', or 'multiply'.
            filter_component: Optional ECS component type to narrow affected entities.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["apply-override", sdk_path, str(value)]
        if mode:
            args.append(mode)
        if filter_component:
            args.append(filter_component)
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_get_component_map(ctx: Context, sdk_path: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Return SDK-to-ECS component type mappings.

        Args:
            sdk_path: Optional filter — omit to return all 30+ mappings.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["get-component-map"]
        if sdk_path:
            args.append(sdk_path)
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_reload_packs(ctx: Context, path: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Hot-reload content packs from disk without restarting the game.

        Args:
            path: Optional packs directory path override.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["reload-packs"]
        if path:
            args.append(path)
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_verify_mod(ctx: Context, pack_path: str, pipe_name: str | None = None) -> dict:
        """
        End-to-end mod verification: load a pack into the running game, verify entity changes.

        Args:
            pack_path: Path to the pack directory or manifest file.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        return _run_game_cli("verify-mod", pack_path, pipe_name=pipe_name)

    @mcp.tool()
    async def game_dump_state(ctx: Context, category: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Dump ECS game state as structured JSON.

        Args:
            category: 'unit', 'building', 'projectile', or omit for all.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["dump-state"]
        if category:
            args.append(category)
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_input(
        ctx: Context,
        key: str | None = None,
        mouse_x: int | None = None,
        mouse_y: int | None = None,
        click: bool = False,
    ) -> dict:
        """
        Inject keyboard or mouse input to the game without requiring window focus (Win32 SendInput).

        Args:
            key: Virtual key name (e.g. 'F1', 'Space', 'Escape').
            mouse_x: Mouse X coordinate (screen absolute).
            mouse_y: Mouse Y coordinate (screen absolute).
            click: If True, send a left mouse click at (mouse_x, mouse_y).
        """
        try:
            import ctypes
        except ImportError:
            return {"success": False, "error": "ctypes not available."}

        SendInput = ctypes.windll.user32.SendInput
        KEYEVENTF_KEYUP = 0x0002
        MOUSEEVENTF_LEFTDOWN = 0x0002
        MOUSEEVENTF_LEFTUP = 0x0004
        MOUSEEVENTF_MOVE = 0x0001

        VK_CODES = {
            "escape": 0x1B, "esc": 0x1B,
            "space": 0x20, "enter": 0x0D, "return": 0x0D,
            "tab": 0x09, "left": 0x25, "up": 0x26, "right": 0x27, "down": 0x28,
            "f1": 0x70, "f2": 0x71, "f3": 0x72, "f4": 0x73,
            "f5": 0x74, "f6": 0x75, "f7": 0x76, "f8": 0x77,
            "f9": 0x78, "f10": 0x79, "f11": 0x7A, "f12": 0x7B,
            "a": 0x41, "b": 0x42, "c": 0x43, "d": 0x44, "e": 0x45,
            "f": 0x46, "g": 0x47, "h": 0x48, "i": 0x49, "j": 0x4A,
            "k": 0x4B, "l": 0x4C, "m": 0x4D, "n": 0x4E, "o": 0x4F,
            "p": 0x50, "q": 0x51, "r": 0x52, "s": 0x53, "t": 0x54,
            "u": 0x55, "v": 0x56, "w": 0x57, "x": 0x58, "y": 0x59, "z": 0x5A,
            "0": 0x30, "1": 0x31, "2": 0x32, "3": 0x33, "4": 0x34,
            "5": 0x35, "6": 0x36, "7": 0x37, "8": 0x38, "9": 0x39,
        }

        class _KEYBDINPUT(ctypes.Structure):
            _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                         ("dwFlags", ctypes.c_uint), ("time", ctypes.c_uint),
                         ("dwExtraInfo", ctypes.c_void_p)]

        class _MOUSEINPUT(ctypes.Structure):
            _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long),
                         ("mouseData", ctypes.c_uint), ("dwFlags", ctypes.c_uint),
                         ("time", ctypes.c_uint), ("dwExtraInfo", ctypes.c_void_p)]

        class _INPUT_UNION(ctypes.Union):
            _fields_ = [("ki", _KEYBDINPUT), ("mi", _MOUSEINPUT)]

        class _INPUT(ctypes.Structure):
            _fields_ = [("type", ctypes.c_uint), ("data", _INPUT_UNION)]

        def _make_input(type_val: int, union_val) -> _INPUT:
            inp = _INPUT()
            inp.type = type_val
            ctypes.memmove(ctypes.addressof(inp.data), ctypes.addressof(union_val), ctypes.sizeof(union_val))
            return inp

        def _send_key(vk: int) -> None:
            ki_down = _KEYBDINPUT(vk, 0, 0, 0, None)
            ki_up = _KEYBDINPUT(vk, 0, KEYEVENTF_KEYUP, 0, None)
            arr = (_INPUT * 2)(*[_make_input(1, ki_down), _make_input(1, ki_up)])
            SendInput(2, arr, ctypes.sizeof(_INPUT))

        def _send_mouse_move(x: int, y: int) -> None:
            mi = _MOUSEINPUT(x, y, 0, MOUSEEVENTF_MOVE, 0, None)
            arr = (_INPUT * 1)(*[_make_input(0, mi)])
            SendInput(1, arr, ctypes.sizeof(_INPUT))

        def _send_click(x: int, y: int) -> None:
            _send_mouse_move(x, y)
            for flags in (MOUSEEVENTF_LEFTDOWN, MOUSEEVENTF_LEFTUP):
                mi = _MOUSEINPUT(0, 0, 0, flags, 0, None)
                arr = (_INPUT * 1)(*[_make_input(0, mi)])
                SendInput(1, arr, ctypes.sizeof(_INPUT))

        try:
            if key:
                vk = VK_CODES.get(key.lower())
                if vk is None:
                    return {"success": False, "error": f"Unknown key: {key}"}
                _send_key(vk)
            if mouse_x is not None and mouse_y is not None and click:
                _send_click(mouse_x, mouse_y)
            elif mouse_x is not None and mouse_y is not None:
                _send_mouse_move(mouse_x, mouse_y)
            return {"success": True, "message": "Input injected."}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def game_ui_automation(ctx: Context, action: str, target: str | None = None, pipe_name: str | None = None) -> dict:
        """
        Automate game UI interactions (click, hover, type, screenshot).

        Args:
            action: Action to perform: 'click', 'hover', 'type', 'snapshot'.
            target: Target button/element name or selector.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = ["ui-automation", action]
        if target:
            args.append(target)
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_navigate_to_gameplay(
        ctx: Context,
        plan: str = "skirmish",
        screenshot_dir: str | None = None,
        final_shot: str | None = None,
        pipe_name: str | None = None,
    ) -> dict:
        """
        Autonomously drive DINO from the main menu INTO an active gameplay/skirmish state.

        This scripts the full multi-step native UI sequence (PLAY/SANDBOX/SKIRMISH → optional
        map/scenario select → START) in-process via the EventSystem pointer driver (#972), waiting
        for next-screen / world-ready conditions between steps (no fixed sleeps), and captures a
        reliable FrameCapture PNG (#980) at every step plus a final gameplay-camera frame.

        Each step resolves its target from an ordered list of candidate selectors and clicks the
        first actionable match, so it tolerates DINO's label variance across builds. The returned
        `steps` trace shows exactly where a flow stalled (blockedAtStep).

        Use this once to reach gameplay, then game_screenshot / game_query_entities to verify
        in-game state (swaps/buildings/blasters visible).

        Args:
            plan: Navigation plan name (default "skirmish").
            screenshot_dir: Directory for per-step PNGs (server default BepInEx/screenshots/nav when omitted).
            final_shot: Optional path for the final gameplay-camera capture PNG.
            pipe_name: Optional named pipe name for multi-instance support.

        Returns:
            {success, message, plan, finalState, entityCount, worldName, blockedAtStep, steps[]}
        """
        args = ["navigate-to-gameplay", plan]
        if screenshot_dir:
            args.append(f"screenshotDir={screenshot_dir}")
        if final_shot:
            args.append(f"finalShot={final_shot}")
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_ui_pointer(
        ctx: Context,
        event: str,
        target: str | None = None,
        x: float | None = None,
        y: float | None = None,
        pipe_name: str | None = None,
    ) -> dict:
        """
        Drive DINO's Unity EventSystem pointer lifecycle IN-PROCESS (hover/press/click),
        bypassing OS input which DINO's EventSystem does not receive.

        This is the in-process path that actually paints hover/press visuals and fires
        onClick — synthetic OS input (SetCursorPos/SendInput/game_input) is NOT delivered
        to DINO's EventSystem. Use this to pixel-verify interactive UI.

        Args:
            event: Pointer event — 'enter'|'exit'|'down'|'up'|'click'|'hover'|'press'.
                   'hover' = enter only (leaves cursor hovering); 'press' = full
                   enter->down->up->click lifecycle.
            target: Selector for the target UI node (e.g. 'label=MODS', 'name=ModsButton').
                    Either target OR x+y must be supplied.
            x: Optional screen X coordinate (resolves target via GraphicRaycaster).
            y: Optional screen Y coordinate.
            pipe_name: Optional named pipe name for multi-instance support.
        """
        args = ["ui-pointer", event]
        if target:
            args.append(target)
        if x is not None:
            args.append(f"x={x}")
        if y is not None:
            args.append(f"y={y}")
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_analyze_screen(
        ctx: Context,
        screenshot_path: str | None = None,
        golden_key: str | None = None,
        prompts: list[str] | None = None,
        pipe_name: str | None = None
    ) -> dict:
        """
        Analyze a screenshot using two-tier visual validation (pHash + CLIP).

        Implements graceful degradation:
        - Tier 1: pHash golden reference matching (1ms, requires imagehash)
        - Tier 2: CLIP zero-shot classification (200ms, requires transformers)
        - Tier 3: OpenCV contour analysis (100ms, requires cv2)

        Args:
            screenshot_path: Optional path to existing screenshot (captures new one if omitted).
            golden_key: Optional golden reference key (e.g., "cp2_f9_overlay") for pHash comparison.
            prompts: Optional list of text prompts for CLIP classification (e.g., ["overlay visible", "menu open"]).
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").

        Returns:
            {
                "method": "phash" | "clip" | "opencv" | "none",
                "passed": bool (for pHash),
                "distance": float (for pHash),
                "confidence": float (for CLIP),
                "prompts": dict (for CLIP),
                "regions": list (for OpenCV),
                "error": str (if analysis failed)
            }
        """
        from ..vision import VisualValidator
        global _visual_validator

        # Initialize validator on first use
        if _visual_validator is None:
            _visual_validator = VisualValidator(fallback_to_opencv=True)
            # Register golden references
            golden_dir = REPO_ROOT / "docs" / "proof" / "golden"
            if golden_dir.exists():
                for img_file in golden_dir.glob("*.png"):
                    key = img_file.stem
                    _visual_validator.register_golden(key, str(img_file))
                    logger.info(f"Registered golden: {key}")

        # Capture screenshot if not provided
        if screenshot_path is None:
            temp_dir = Path(os.getenv("TEMP", "/tmp")) / "DINOForge"
            temp_dir.mkdir(parents=True, exist_ok=True)
            screenshot_path = str(temp_dir / "screenshot.png")
            cap_result = _run_game_cli("screenshot", screenshot_path, pipe_name=pipe_name)
            if not cap_result.get("success"):
                return {"success": False, "error": f"Failed to capture screenshot: {cap_result}"}

        # Perform two-tier analysis
        result = _visual_validator.analyze_screenshot(
            screenshot_path,
            golden_key=golden_key,
            prompts=prompts
        )

        return {"success": True, **result}

    def _analyze_screenshot_cv(screenshot_path: str | None) -> dict:
        """
        Analyze a screenshot using OpenCV to detect UI regions.
        Returns health bars, buttons, portraits, and faction color patches.
        """
        try:
            import cv2
            import numpy as np
        except ImportError:
            return {"success": False, "error": "opencv-python-headless not installed"}

        temp_dir = Path(os.getenv("TEMP", "/tmp")) / "DINOForge"
        if screenshot_path is None:
            screenshot_path = str(temp_dir / "screenshot.png")
            cap_result = _run_game_cli("screenshot", screenshot_path)
            if not cap_result.get("success"):
                return {"success": False, "error": f"Failed to capture screenshot: {cap_result}"}

        if not Path(screenshot_path).exists():
            return {"success": False, "error": f"Screenshot not found: {screenshot_path}"}

        img = cv2.imread(screenshot_path)
        if img is None:
            return {"success": False, "error": f"Failed to read image: {screenshot_path}"}

        h, w = img.shape[:2]
        elements: list[dict] = []

        # Convert to different color spaces for analysis
        gray = cv2.cvtColor(img, cv2.COLOR_BGR2GRAY)
        hsv = cv2.cvtColor(img, cv2.COLOR_BGR2HSV)

        # Detect health bars: narrow horizontal rectangles, typically green/red/yellow
        # Green health bar range in HSV
        green_lower = np.array([35, 50, 50])
        green_upper = np.array([85, 255, 255])
        green_mask = cv2.inRange(hsv, green_lower, green_upper)
        # Find contours (horizontal bar-shaped)
        contours, _ = cv2.findContours(green_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        for cnt in contours:
            x, y, bw, bh = cv2.boundingRect(cnt)
            aspect = bw / max(bh, 1)
            if aspect > 3 and 5 < bw < w * 0.4 and 2 < bh < 20:  # Long thin bar = health bar
                elements.append({
                    "type": "health_bar",
                    "x": int(x), "y": int(y),
                    "width": int(bw), "height": int(bh),
                    "color": "green",
                    "confidence": min(1.0, bw / 200),
                })

        # Red/amber bar for enemy health
        red_lower1 = np.array([0, 70, 50]); red_upper1 = np.array([10, 255, 255])
        red_lower2 = np.array([170, 70, 50]); red_upper2 = np.array([180, 255, 255])
        red_mask = cv2.inRange(hsv, red_lower1, red_upper1) | cv2.inRange(hsv, red_lower2, red_upper2)
        contours2, _ = cv2.findContours(red_mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        for cnt in contours2:
            x, y, bw, bh = cv2.boundingRect(cnt)
            aspect = bw / max(bh, 1)
            if aspect > 3 and 5 < bw < w * 0.4 and 2 < bh < 20:
                elements.append({
                    "type": "health_bar",
                    "x": int(x), "y": int(y),
                    "width": int(bw), "height": int(bh),
                    "color": "red",
                    "confidence": min(1.0, bw / 200),
                })

        # Detect faction color patches: large uniform color regions (top corners = faction banners)
        faction_colors = [
            ("republic_blue",   ( 70,  80, 180), (110, 160, 255), 0.8),
            ("cis_gold",        ( 15, 100, 150), ( 35, 200, 255), 0.8),
            ("enemy_red",       (  0, 100, 100), ( 10, 255, 255), 0.8),
            ("neutral_gray",    ( 80,   0,  80), (120,  50, 180), 0.5),
        ]
        for name, bgr_low, bgr_high, conf in faction_colors:
            low = np.array(bgr_low); upper = np.array(bgr_high)
            mask = cv2.inRange(img, low, upper)
            cnts, _ = cv2.findContours(mask, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
            for cnt in cnts:
                x, y, bw, bh = cv2.boundingRect(cnt)
                if bw > 30 and bh > 30:  # Faction banner minimum size
                    elements.append({
                        "type": "faction_patch",
                        "faction": name,
                        "x": int(x), "y": int(y),
                        "width": int(bw), "height": int(bh),
                        "confidence": conf,
                    })

        # Detect portrait-like regions: square-ish shapes in the bottom-left area (unit portrait zone)
        portrait_zone = img[int(h * 0.7):, :int(w * 0.3)]
        gray_portrait = cv2.cvtColor(portrait_zone, cv2.COLOR_BGR2GRAY)
        _, thresh = cv2.threshold(gray_portrait, 0, 255, cv2.THRESH_BINARY + cv2.THRESH_OTSU)
        cnts3, _ = cv2.findContours(thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        offset_y = int(h * 0.7)
        for cnt in cnts3:
            x, y, bw, bh = cv2.boundingRect(cnt)
            aspect = min(bw, bh) / max(bw, bh, 1)
            area = bw * bh
            if aspect > 0.7 and 400 < area < 50000:  # Square-ish, portrait-sized
                elements.append({
                    "type": "unit_portrait",
                    "x": int(x), "y": int(y + offset_y),
                    "width": int(bw), "height": int(bh),
                    "confidence": 0.6,
                })

        # Button-like regions: light/white rectangles near bottom of screen
        button_zone = img[int(h * 0.85):, :]
        button_gray = cv2.cvtColor(button_zone, cv2.COLOR_BGR2GRAY)
        _, button_thresh = cv2.threshold(button_gray, 200, 255, cv2.THRESH_BINARY)
        cnts4, _ = cv2.findContours(button_thresh, cv2.RETR_EXTERNAL, cv2.CHAIN_APPROX_SIMPLE)
        button_offset_y = int(h * 0.85)
        for cnt in cnts4:
            x, y, bw, bh = cv2.boundingRect(cnt)
            if bw > 60 and 10 < bh < 60 and bw / max(bh, 1) > 2:
                elements.append({
                    "type": "button",
                    "x": int(x), "y": int(y + button_offset_y),
                    "width": int(bw), "height": int(bh),
                    "confidence": 0.7,
                })

        return {
            "success": True,
            "screenshot": screenshot_path,
            "resolution": {"width": w, "height": h},
            "elements_detected": len(elements),
            "elements": elements,
            "method": "opencv_color_contour",
        }

    @mcp.tool()
    async def game_wait_and_screenshot(
        ctx: Context,
        timeout_seconds: int = 30,
        interval_seconds: float = 1.0,
        change_threshold: float = 0.05,
        pipe_name: str | None = None,
    ) -> dict:
        """
        Poll for a visual change in the game window, then capture a screenshot.

        Args:
            timeout_seconds: Max time to wait for visual change.
            interval_seconds: How often to check for change (seconds).
            change_threshold: Minimum pixel-change fraction to count as a change (0.0–1.0).
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        args = [
            "wait-and-screenshot",
            "--timeout", str(timeout_seconds),
            "--interval", str(interval_seconds),
            "--threshold", str(change_threshold),
        ]
        return _run_game_cli(*args, pipe_name=pipe_name)

    @mcp.tool()
    async def game_navigate_to(ctx: Context, state: str, pipe_name: str | None = None) -> dict:
        """
        Navigate to a game state via input sequences.

        Args:
            state: Target state — 'main_menu', 'gameplay', or 'pause_menu'.
            pipe_name: Optional named pipe name for multi-instance support (defaults to "dinoforge-game-bridge").
        """
        # Validate state
        if state not in ("main_menu", "gameplay", "pause_menu"):
            return {
                "success": False,
                "error": f"Unknown state '{state}'. Valid values: 'main_menu', 'gameplay', 'pause_menu'."
            }

        # Check current state via status
        status_result = _run_game_cli("status", pipe_name=pipe_name)
        if not status_result.get("Running"):
            return {"success": False, "error": "Game is not running."}

        # Parse entity count from the raw/text response
        raw = status_result.get("raw", "")
        entity_count = 0
        for line in raw.split("\n"):
            if "Entity count:" in line:
                try:
                    entity_count = int(line.split("Entity count:")[1].strip())
                except (ValueError, IndexError):
                    pass

        if state == "main_menu":
            # Can't programmatically return to main menu without keyboard input.
            return {
                "success": False,
                "error": (
                    "Returning to main_menu requires keyboard input (ESC or menu navigation) "
                    "which is not yet implemented in the bridge. "
                    "Use game_launch() to restart the game."
                )
            }

        if state == "pause_menu":
            # From gameplay: send ESC twice — requires keyboard input.
            return {
                "success": False,
                "error": (
                    "pause_menu requires keyboard input (ESC key) which is not yet available. "
                    "Use game_input(key='Escape') once keyboard input is implemented."
                )
            }

        # state == "gameplay"
        if entity_count > 50000:
            return {
                "success": True,
                "message": "Already at gameplay state.",
                "entityCount": entity_count
            }

        # Load AUTOSAVE_1 to enter gameplay
        save_result = _run_game_cli("load-save", "AUTOSAVE_1", pipe_name=pipe_name)
        if not save_result.get("success") and "already" not in save_result.get("error", "").lower():
            return save_result

        # Dismiss loading screen if present
        dismiss_result = _run_game_cli("dismiss", pipe_name=pipe_name)
        return {
            "success": True,
            "message": "Navigated to gameplay.",
            "loadResult": save_result,
            "dismissResult": dismiss_result,
            "entityCount": entity_count
        }
