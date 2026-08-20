# DINOForge MCP Modules
# Lazy imports to avoid pulling heavy deps when running proof_policy standalone
from __future__ import annotations
from typing import TYPE_CHECKING
if TYPE_CHECKING:
    from fastmcp import FastMCP
_MODULE_NAMES = [
    "game_control",
    "asset_pipeline",
    "catalog_inspection",
    "log_analysis",
    "voice_commands",
    "routes_prompts",
]

def register_all(mcp: "FastMCP") -> None:
    """Register all tool modules with the MCP server."""
    import importlib
    for name in _MODULE_NAMES:
        mod = importlib.import_module(f".{name}", package=__name__)
        mod.register(mcp)
