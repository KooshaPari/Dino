# DINOForge MCP Server
# 
# FastMCP 3.0 server for DINOForge game automation
# Features: tools, resources, prompts, streaming, background tasks
# Supports: Claude Code, Codex, Factory Droid integration
#
# Architecture:
#   MCP Client → FastMCP Server → CLI → Named Pipe → GameBridgeServer

import os
import asyncio
import subprocess
import json
import logging
from pathlib import Path
from typing import Optional, Any, List, Dict
from enum import Enum

# FastMCP 3.0 imports
from fastmcp import FastMCP, Context
from pydantic import BaseModel, Field

# Configure logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger("dinoforge_mcp")

# ============== Configuration ==============

CLI_PATH = os.environ.get("DINOFORGE_CLI_PATH", "src/Tools/Cli")
PIPE_NAME = os.environ.get("DINOFORGE_PIPE_NAME", "dinoforge_game")
DEBUG = os.environ.get("DINOFORGE_MCP_DEBUG", "false").lower() == "true"

# ============== Models ==============

class GameStatus(BaseModel):
    """Game status response"""
    connected: bool
    game_running: bool = False
    version: str = ""
    mods_loaded: int = 0
    entities: int = 0

class EntityInfo(BaseModel):
    """ECS entity information"""
    index: int
    components: List[str] = []

class UiNode(BaseModel):
    """UI tree node"""
    id: str
    name: str
    role: str
    label: str = ""
    visible: bool = True
    interactable: bool = True
    children: List["UiNode"] = []

class PackInfo(BaseModel):
    """Mod pack information"""
    id: str
    name: str
    version: str
    loaded: bool = True

# ============== Client ==============

class DINOForgeClient:
    """Python client wrapping the .NET CLI"""
    
    def __init__(self, cli_path: str = None):
        self.cli_path = cli_path or CLI_PATH
        self.base_cmd = ["dotnet", "run", "--project", self.cli_path, "--no-restore"]
    
    def _run(self, *args, timeout: int = 30) -> dict:
        """Run CLI command synchronously"""
        cmd = list(self.base_cmd) + ["--"] + list(args)
        
        if DEBUG:
            logger.info(f"Running: {' '.join(cmd)}")
        
        try:
            result = subprocess.run(
                cmd,
                capture_output=True,
                text=True,
                timeout=timeout,
                cwd=Path(__file__).parent.parent.parent.parent
            )
            
            if result.returncode != 0:
                return {"success": False, "error": result.stderr}
            
            try:
                return json.loads(result.stdout) if result.stdout else {}
            except json.JSONDecodeError:
                return {"success": True, "raw": result.stdout}
                
        except subprocess.TimeoutExpired:
            return {"success": False, "error": "Command timed out"}
        except Exception as e:
            return {"success": False, "error": str(e)}
    
    # Game State
    def status(self) -> dict:
        """Get game status"""
        return self._run("status")
    
    def resources(self) -> dict:
        """Get game resources"""
        return self._run("resources")
    
    def packs(self) -> dict:
        """Get loaded packs"""
        return self._run("verify")
    
    # Entities
    def query(self, path: str) -> dict:
        """Query entities"""
        return self._run("query", path)
    
    # Spawning  
    def spawn(self, unit_id: str, x: float = 0, z: float = 0) -> dict:
        """Spawn unit"""
        return self._run("spawn", unit_id, str(x), str(z))
    
    # UI Automation
    def ui_tree(self, selector: str = None) -> dict:
        """Get UI tree"""
        if selector:
            return self._run("ui-tree", "--selector", selector)
        return self._run("ui-tree")
    
    def ui_query(self, selector: str) -> dict:
        """Query UI"""
        return self._run("ui-query", selector)
    
    def ui_click(self, selector: str) -> dict:
        """Click UI"""
        return self._run("ui-click", selector)
    
    def ui_wait(self, selector: str, state: str = "visible", timeout: int = 5000) -> dict:
        """Wait UI"""
        return self._run("ui-wait", selector, "--state", state, "--timeout", str(timeout))
    
    def ui_expect(self, selector: str, condition: str) -> dict:
        """Expect UI"""
        return self._run("ui-expect", selector, condition)
    
    # Mod Operations
    def override(self, path: str, value: float) -> dict:
        """Apply override"""
        return self._run("override", path, str(value))
    
    def reload(self) -> dict:
        """Reload packs"""
        return self._run("reload")
    
    # Screenshot
    def screenshot(self, output: str = None) -> dict:
        """Take screenshot"""
        if output:
            return self._run("screenshot", "--output", output)
        return self._run("screenshot")
    
    # Asset Pipeline (PackCompiler)
    def asset_list(self, pack: str) -> dict:
        """List pack assets"""
        return self._run("assets", "list", f"packs/{pack}")
    
    def asset_validate(self, pack: str) -> dict:
        """Validate pack assets"""
        return self._run("assets", "validate", f"packs/{pack}")
    
    def asset_import(self, pack: str) -> dict:
        """Import pack assets"""
        return self._run("assets", "import", f"packs/{pack}")
    
    def asset_optimize(self, pack: str) -> dict:
        """Optimize pack assets"""
        return self._run("assets", "optimize", f"packs/{pack}")
    
    def asset_build(self, pack: str) -> dict:
        """Build pack assets"""
        return self._run("assets", "build", f"packs/{pack}")
    
    # Pack Operations
    def pack_validate(self, pack: str) -> dict:
        """Validate pack"""
        return self._run("validate", f"packs/{pack}")
    
    def pack_build(self, pack: str) -> dict:
        """Build pack"""
        return self._run("build", f"packs/{pack}")
    
    # Debug
    def dump(self, what: str = "world") -> dict:
        """Dump game state"""
        return self._run("dump", what)
    
    def component_map(self) -> dict:
        """Get component map"""
        return self._run("component-map")


# Global client
_client: Optional[DINOForgeClient] = None

def get_client() -> DINOForgeClient:
    global _client
    if _client is None:
        _client = DINOForgeClient()
    return _client


# ============== FastMCP 3.0 Server ==============

# Create server with FastMCP 3.0 features
mcp = FastMCP("DINOForge")

# ============== TOOLS ==============

@mcp.tool()
async def game_status(ctx: Context) -> dict:
    """
    Check if game is running and get status.
    
    Returns: connected, game_running, version, mods_loaded, entities
    """
    return get_client().status()

@mcp.tool()
async def game_resources(ctx: Context) -> dict:
    """
    Get current game resources (gold, wood, food, etc)
    """
    return get_client().resources()

@mcp.tool()
async def game_packs(ctx: Context) -> dict:
    """
    Get loaded mod packs
    """
    return get_client().packs()

@mcp.tool()
async def query_entities(ctx: Context, component_type: str = None) -> dict:
    """
    Query ECS entities by component type (unit, building, faction, etc)
    
    Args:
        component_type: Component type to query (e.g., 'unit', 'building')
    """
    return get_client().query(component_type or "")

@mcp.tool()
async def spawn_unit(ctx: Context, unit_id: str, x: float = 0, z: float = 0) -> dict:
    """
    Spawn a unit at world position
    
    Args:
        unit_id: Unit definition ID
        x: X position
        z: Z position
    """
    return get_client().spawn(unit_id, x, z)

@mcp.tool()
async def ui_tree(ctx: Context, selector: str = None) -> dict:
    """
    Capture live Unity UI hierarchy snapshot
    
    Args:
        selector: Optional selector to filter tree
    """
    return get_client().ui_tree(selector)

@mcp.tool()
async def ui_query(ctx: Context, selector: str) -> dict:
    """
    Query UI elements by selector
    
    Selector syntax:
    - role=button, role=panel
    - text=Label, text-exact=Exact
    - name=ObjectName
    - path=root/panel/button
    - index=0, first, last
    """
    return get_client().ui_query(selector)

@mcp.tool()
async def ui_click(ctx: Context, selector: str) -> dict:
    """
    Click a UI element by selector
    """
    return get_client().ui_click(selector)

@mcp.tool()
async def ui_wait(ctx: Context, selector: str, state: str = "visible", timeout: int = 5000) -> dict:
    """
    Wait for UI element to reach a state
    
    Args:
        selector: Element selector
        state: visible, hidden, interactable, actionable
        timeout: Milliseconds to wait
    """
    return get_client().ui_wait(selector, state, timeout)

@mcp.tool()
async def ui_expect(ctx: Context, selector: str, condition: str) -> dict:
    """
    Assert UI element condition
    
    Args:
        selector: Element selector  
        condition: visible, hidden, text=..., text-exact=..., count>=1
    """
    return get_client().ui_expect(selector, condition)

@mcp.tool()
async def apply_override(ctx: Context, path: str, value: float) -> dict:
    """
    Apply a stat override
    
    Args:
        path: Stat path (e.g., 'units.b1_droid.health')
        value: New value
    """
    return get_client().override(path, value)

@mcp.tool()
async def reload_packs(ctx: Context) -> dict:
    """
    Hot reload all mod packs
    """
    return get_client().reload()

@mcp.tool()
async def take_screenshot(ctx: Context, output: str = None) -> dict:
    """
    Capture game screenshot
    
    Args:
        output: Output file path
    """
    return get_client().screenshot(output)

# ============== Asset Pipeline Tools ==============

@mcp.tool()
async def asset_list(ctx: Context, pack: str) -> dict:
    """
    List assets in a pack
    
    Args:
        pack: Pack name (e.g., 'warfare-starwars')
    """
    return get_client().asset_list(pack)

@mcp.tool()
async def asset_validate(ctx: Context, pack: str) -> dict:
    """
    Validate pack assets
    
    Args:
        pack: Pack name
    """
    return get_client().asset_validate(pack)

@mcp.tool()
async def asset_import(ctx: Context, pack: str) -> dict:
    """
    Import pack assets from sources
    
    Args:
        pack: Pack name
    """
    return get_client().asset_import(pack)

@mcp.tool()
async def asset_optimize(ctx: Context, pack: str) -> dict:
    """
    Optimize pack assets (generate LODs)
    
    Args:
        pack: Pack name
    """
    return get_client().asset_optimize(pack)

@mcp.tool()
async def asset_build(ctx: Context, pack: str) -> dict:
    """
    Full asset pipeline build
    
    Args:
        pack: Pack name
    """
    return get_client().asset_build(pack)

# ============== Pack Tools ==============

@mcp.tool()
async def pack_validate(ctx: Context, pack: str) -> dict:
    """
    Validate a mod pack
    
    Args:
        pack: Pack name or path
    """
    return get_client().pack_validate(pack)

@mcp.tool()
async def pack_build(ctx: Context, pack: str) -> dict:
    """
    Build a mod pack
    
    Args:
        pack: Pack name or path
    """
    return get_client().pack_build(pack)

# ============== Debug Tools ==============

@mcp.tool()
async def dump_world(ctx: Context) -> dict:
    """
    Dump full game world state (entities, components, systems)
    """
    return get_client().dump("world")

@mcp.tool()
async def dump_entities(ctx: Context) -> dict:
    """
    Dump all entities
    """
    return get_client().dump("entities")

@mcp.tool()
async def component_map(ctx: Context) -> dict:
    """
    Get ECS component mapping table
    """
    return get_client().component_map()

# ============== RESOURCES ==============

@mcp.resource("game://status")
async def status_resource() -> dict:
    """Current game status"""
    return get_client().status()

@mcp.resource("game://resources")
async def resources_resource() -> dict:
    """Game resources"""
    return get_client().resources()

@mcp.resource("game://packs")
async def packs_resource() -> dict:
    """Loaded mod packs"""
    return get_client().packs()

@mcp.resource("game://ui-tree")
async def ui_tree_resource() -> dict:
    """Full UI hierarchy"""
    return get_client().ui_tree()

@mcp.resource("game://entities")
async def entities_resource() -> dict:
    """All ECS entities"""
    return get_client().query("")

# ============== PROMPTS ==============

@mcp.prompt()
def debug_prompt(issue: str = "unknown") -> str:
    """Debug workflow for DINOForge issues"""
    return f"""You are debugging DINOForge in a running Unity game.

Issue: {issue}

Debug workflow:
1. game_status - verify game is running
2. query_entities - find relevant ECS data  
3. ui_tree - inspect current UI state
4. dump_world - get full state
5. apply_override - test fixes

Include entity counts and ECS query results in your analysis."""

@mcp.prompt()
def testing_prompt() -> str:
    """UI automation testing workflow"""
    return """You are testing DINOForge UI automation.

Test patterns:
1. ui_tree → baseline UI state
2. ui_wait → wait for panels
3. ui_click → interact
4. ui_expect → verify
5. take_screenshot → visual confirmation

Selectors: role=button, text=Label, name=Object, path=root/panel, index=0, first, last"""

@mcp.prompt()
def modding_prompt() -> str:
    """Mod development workflow"""
    return """You are helping develop DINOForge mods.

Workflow:
1. pack_validate - verify YAML
2. asset_import - import assets
3. asset_optimize - generate LOD
4. reload_packs - hot reload
5. query_entities - inspect game
6. apply_override - test balance

Packs in: packs/ directory"""

@mcp.prompt()
def asset_pipeline_prompt() -> str:
    """Asset pipeline workflow"""
    return """You are managing DINOForge asset pipelines.

Steps:
1. asset_list - see current assets
2. asset_import - download/import new assets
3. asset_validate - verify assets
4. asset_optimize - generate LOD variants
5. asset_build - full pipeline

Config: packs/*/asset_pipeline.yaml"""

# ============== Main ==============

def main():
    """Entry point"""
    import sys
    if "--help" in sys.argv or "-h" in sys.argv:
        print("""
DINOForge MCP Server
==================

Usage:
  python -m dinoforge_mcp.server [--host HOST] [--port PORT]

Environment:
  DINOFORGE_CLI_PATH    Path to CLI project (default: src/Tools/Cli)
  DINOFORGE_PIPE_NAME   Pipe name (default: dinoforge_game)  
  DINOFORGE_MCP_DEBUG   Enable debug logging

Tools: 21
Resources: 5
Prompts: 4
        """)
        return
    
    # Run with FastMCP
    mcp.run()


if __name__ == "__main__":
    main()
