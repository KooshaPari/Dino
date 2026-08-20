"""REST API for DINOForge MCP - wires HTTP endpoints to actual MCP tools."""
from __future__ import annotations
import importlib
import logging
from typing import Any, Callable, Coroutine
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

logger = logging.getLogger("dinoforge_mcp.api")
app = FastAPI(title="DINOForge API", version="1.0.0", description="REST API bridging HTTP clients to DINOForge MCP tools")

_TOOL_REGISTRY: dict[str, Callable[..., Coroutine[Any, Any, Any]]] = {}

def _build_registry() -> None:
    for mod_name in ["dinoforge_mcp.game_control", "dinoforge_mcp.asset_pipeline", "dinoforge_mcp.catalog_inspection", "dinoforge_mcp.log_analysis", "dinoforge_mcp.voice_commands"]:
        try:
            mod = importlib.import_module(mod_name)
            for attr_name in dir(mod):
                obj = getattr(mod, attr_name)
                if callable(obj) and attr_name.startswith(("game_", "asset_", "pack_", "catalog_", "log_", "voice_")):
                    _TOOL_REGISTRY[attr_name] = obj
        except Exception as e:
            logger.warning(f"Could not load {mod_name}: {e}")

_build_registry()

class ToolRequest(BaseModel):
    args: dict[str, Any] = Field(default_factory=dict)

class ToolResponse(BaseModel):
    tool: str
    result: Any = None
    status: str = "ok"
    error: str | None = None

class ToolInfo(BaseModel):
    name: str
    module: str
    doc: str | None = None

@app.get("/health")
async def health() -> dict[str, str]:
    return {"status": "healthy", "service": "dinoforge-mcp", "tools": str(len(_TOOL_REGISTRY))}

@app.get("/tools", response_model=list[ToolInfo])
async def list_tools() -> list[ToolInfo]:
    return [ToolInfo(name=name, module=fn.__module__ or "unknown", doc=(fn.__doc__ or "").strip()[:200] if fn.__doc__ else None) for name, fn in sorted(_TOOL_REGISTRY.items())]

@app.post("/tools/{tool_name}", response_model=ToolResponse)
async def run_tool(tool_name: str, req: ToolRequest) -> ToolResponse:
    if tool_name not in _TOOL_REGISTRY:
        raise HTTPException(status_code=404, detail=f"Tool not found. Available: {sorted(_TOOL_REGISTRY.keys())}")
    try:
        result = await _TOOL_REGISTRY[tool_name](**req.args)
        return ToolResponse(tool=tool_name, result=result, status="ok")
    except TypeError as e:
        return ToolResponse(tool=tool_name, status="error", error=f"Argument error: {e}")
    except Exception as e:
        logger.exception(f"Tool {tool_name} failed")
        return ToolResponse(tool=tool_name, status="error", error=str(e))

@app.get("/tools/{tool_name}")
async def get_tool_info(tool_name: str) -> ToolInfo:
    if tool_name not in _TOOL_REGISTRY:
        raise HTTPException(status_code=404, detail=f"Tool not found")
    fn = _TOOL_REGISTRY[tool_name]
    return ToolInfo(name=tool_name, module=fn.__module__ or "unknown", doc=(fn.__doc__ or "").strip()[:200] if fn.__doc__ else None)

@app.get("/metrics")
async def metrics() -> dict[str, Any]:
    return {"dinoforge_tools_registered": len(_TOOL_REGISTRY), "dinoforge_api_version": "1.0.0"}
