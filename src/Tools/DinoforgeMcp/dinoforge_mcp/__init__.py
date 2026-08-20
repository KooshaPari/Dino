"""
DINOForge MCP Modules
"""
from . import (
    game_control,
    asset_pipeline,
    catalog_inspection,
    log_analysis,
    voice_commands,
    routes_prompts,
)

ALL_MODULES = [
    game_control,
    asset_pipeline,
    catalog_inspection,
    log_analysis,
    voice_commands,
    routes_prompts,
]

def register_all(mcp):
    """Register all modules with the MCP server."""
    for mod in ALL_MODULES:
        mod.register(mcp)
