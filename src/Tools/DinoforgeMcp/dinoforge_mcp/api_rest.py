"""REST API for DINOForge MCP - wires HTTP endpoints to actual MCP tools.

Production-hardened: health probes, Prometheus metrics, circuit breaker.
"""
from __future__ import annotations
import importlib
import logging
import time
from typing import Any, Callable, Coroutine
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel, Field

from dinoforge_mcp.health import health_liveness, health_readiness, health_startup
from dinoforge_mcp.prometheus import render_prometheus, record_request
from dinoforge_mcp.circuit_breaker import get_circuit_breaker, State

logger = logging.getLogger("dinoforge_mcp.api")

app = FastAPI(
    title="DINOForge API",
    version="1.0.0",
    description="REST API bridging HTTP clients to DINOForge MCP tools",
)

_TOOL_REGISTRY: dict[str, Callable[..., Coroutine[Any, Any, Any]]] = {}


def _build_registry() -> None:
    for mod_name in [
        "dinoforge_mcp.game_control",
        "dinoforge_mcp.asset_pipeline",
        "dinoforge_mcp.catalog_inspection",
        "dinoforge_mcp.log_analysis",
        "dinoforge_mcp.voice_commands",
    ]:
        try:
            mod = importlib.import_module(mod_name)
            for attr_name in dir(mod):
                obj = getattr(mod, attr_name)
                if callable(obj) and attr_name.startswith(
                    ("game_", "asset_", "pack_", "catalog_", "log_", "voice_")
                ):
                    _TOOL_REGISTRY[attr_name] = obj
        except Exception as e:
            logger.warning(f"Could not load {mod_name}: {e}")


_build_registry()


# ---------------------------------------------------------------------------
# Request / response models
# ---------------------------------------------------------------------------

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


# ---------------------------------------------------------------------------
# Health endpoints  (K8s liveness / readiness / startup probes)
# ---------------------------------------------------------------------------

@app.get("/health")
async def health() -> dict[str, Any]:
    """Aggregate health - returns readiness status for simple clients."""
    return health_readiness()


@app.get("/health/live")
async def health_live() -> dict[str, Any]:
    """K8s liveness probe."""
    return health_liveness()


@app.get("/health/ready")
async def health_ready() -> dict[str, Any]:
    """K8s readiness probe."""
    return health_readiness()


@app.get("/health/startup")
async def health_startup_endpoint() -> dict[str, Any]:
    """K8s startup probe."""
    return health_startup()


# ---------------------------------------------------------------------------
# Prometheus metrics endpoint
# ---------------------------------------------------------------------------

@app.get("/metrics")
async def metrics() -> str:
    """Prometheus-compatible /metrics endpoint."""
    return render_prometheus()


# ---------------------------------------------------------------------------
# Tool listing
# ---------------------------------------------------------------------------

@app.get("/tools", response_model=list[ToolInfo])
async def list_tools() -> list[ToolInfo]:
    return [
        ToolInfo(
            name=name,
            module=fn.__module__ or "unknown",
            doc=(fn.__doc__ or "").strip()[:200] if fn.__doc__ else None,
        )
        for name, fn in sorted(_TOOL_REGISTRY.items())
    ]


# ---------------------------------------------------------------------------
# Tool execution  (circuit-breaker protected)
# ---------------------------------------------------------------------------

@app.post("/tools/{tool_name}", response_model=ToolResponse)
async def run_tool(tool_name: str, req: ToolRequest) -> ToolResponse:
    # --- circuit breaker gate ------------------------------------------------
    cb = get_circuit_breaker()
    if not cb.allow_request():
        logger.warning(f"Circuit breaker OPEN - rejecting tool '{tool_name}'")
        return ToolResponse(
            tool=tool_name,
            status="error",
            error="Circuit breaker is OPEN - service degraded, retry later",
        )

    if tool_name not in _TOOL_REGISTRY:
        raise HTTPException(
            status_code=404,
            detail=f"Tool not found. Available: {sorted(_TOOL_REGISTRY.keys())}",
        )

    # --- execute with latency tracking --------------------------------------
    t0 = time.monotonic()
    try:
        result = await _TOOL_REGISTRY[tool_name](**req.args)
        latency_ms = (time.monotonic() - t0) * 1000
        record_request(tool_name, latency_ms, success=True)
        cb.record_success()
        return ToolResponse(tool=tool_name, result=result, status="ok")
    except TypeError as e:
        latency_ms = (time.monotonic() - t0) * 1000
        record_request(tool_name, latency_ms, success=False)
        cb.record_failure()
        return ToolResponse(tool=tool_name, status="error", error=f"Argument error: {e}")
    except Exception as e:
        latency_ms = (time.monotonic() - t0) * 1000
        logger.exception(f"Tool {tool_name} failed")
        record_request(tool_name, latency_ms, success=False)
        cb.record_failure()
        return ToolResponse(tool=tool_name, status="error", error=str(e))


# ---------------------------------------------------------------------------
# Tool metadata
# ---------------------------------------------------------------------------

@app.get("/tools/{tool_name}")
async def get_tool_info(tool_name: str) -> ToolInfo:
    if tool_name not in _TOOL_REGISTRY:
        raise HTTPException(status_code=404, detail="Tool not found")
    fn = _TOOL_REGISTRY[tool_name]
    return ToolInfo(
        name=tool_name,
        module=fn.__module__ or "unknown",
        doc=(fn.__doc__ or "").strip()[:200] if fn.__doc__ else None,
    )
