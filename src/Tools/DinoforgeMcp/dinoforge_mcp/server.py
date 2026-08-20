"""
DINOForge MCP Server Entry Point — Modular Architecture

Decomposition of the original monolithic _server.py into 6 focused modules:
1. game_control: Game bridge & CLI tools (31 tools)
2. asset_pipeline: Asset & pack management (7 tools)
3. catalog_inspection: Addressables catalog inspection (2 tools)
4. log_analysis: Debug log analysis (7 tools)
5. voice_commands: Voice intent recognition (2 tools)
6. routes_prompts: Custom routes, resources, prompts, and HMR (1 tool + 2 prompts + 3 resources + 7 routes)
"""
from __future__ import annotations

import argparse
import logging
import os

from fastmcp import FastMCP

from . import register_all

logger = logging.getLogger("dinoforge_mcp")

def create_mcp_server() -> FastMCP:
    """Create and configure the MCP server with all modular tools."""
    mcp = FastMCP(
        "dinoforge",
        instructions=(
            "DINOForge unified MCP server. "
            "game_* tools: live game state via named pipe bridge (GameControlCli). "
            "asset_* / pack_*: asset pipeline and pack management (PackCompiler). "
            "catalog_*: direct Addressables catalog inspection. "
            "log_*: BepInEx debug log analysis."
        ),
    )
    register_all(mcp)
    return mcp

def main() -> None:
    parser = argparse.ArgumentParser(
        description="DINOForge MCP Server (FastMCP 3.1.1)",
        epilog="Examples:\n  python -m _dino_modules.server                    # stdio (for MCP client)\n  python -m _dino_modules.server --http --port 8765  # HTTP/SSE (persistent server)",
        formatter_class=argparse.RawDescriptionHelpFormatter,
    )
    parser.add_argument(
        "--http",
        action="store_true",
        help="Run as HTTP/SSE server instead of stdio (allows hot-reload without restart)",
    )
    parser.add_argument(
        "--port",
        type=int,
        default=8765,
        help="HTTP server port (default: 8765, ignored if --http not set)",
    )
    parser.add_argument(
        "--host",
        type=str,
        default="127.0.0.1",
        help="HTTP server host (default: 127.0.0.1, ignored if --http not set)",
    )
    args, remaining = parser.parse_known_args()

    mcp = create_mcp_server()

    if args.http:
        # HTTP/SSE mode: Uvicorn ASGI server with persistent process
        logger.info(f"Starting DINOForge MCP in HTTP mode at {args.host}:{args.port}")
        logger.info(f"  JSON-RPC endpoint: http://{args.host}:{args.port}")
        mcp.run(
            transport="http",
            host=args.host,
            port=args.port,
        )
    else:
        # stdio mode: default for direct MCP client (Claude, other tools)
        logger.info("Starting DINOForge MCP in stdio mode (for MCP client)")
        mcp.run()


# Backward-compatible re-exports (moved from monolithic server.py)
# Tests and external code may import these from dinoforge_mcp.server
import pathlib as _pathlib
import os as _os

DEFAULT_GAME_PIPE_NAME = r"\\.\pipe\DINoF_Pipe"

def _pipe_exists(pipe_name: str) -> bool:
    """Check if a named pipe exists on the filesystem."""
    if _os.name == 'nt':
        return True
    return _pathlib.Path(pipe_name).exists()

def _select_pipe_name(explicit: str | None = None) -> tuple[str, bool]:
    """Select the MCP bridge pipe name.

    Priority: explicit arg > DINOFORGE_PIPE_NAME env > DEFAULT_GAME_PIPE_NAME
    Returns (pipe_name, used_fallback).
    """
    env_name = _os.environ.get("DINOFORGE_PIPE_NAME", "")
    if env_name and _pipe_exists(env_name):
        return env_name, False
    if explicit and _pipe_exists(explicit):
        return explicit, False
    return DEFAULT_GAME_PIPE_NAME, True

if __name__ == "__main__":
    main()
