"""REST API scaffold for DINOForge MCP"""
from __future__ import annotations
from typing import Any
from fastapi import FastAPI, HTTPException
from pydantic import BaseModel

app = FastAPI(title="DINOForge API", version="0.1.0")

class ToolRequest(BaseModel):
    tool: str
    args: dict[str, Any] = {}

class ToolResponse(BaseModel):
    result: Any
    status: str = "ok"

@app.get("/health")
async def health():
    return {"status": "healthy"}

@app.post("/tools/{tool_name}")
async def run_tool(tool_name: str, req: ToolRequest):
    raise HTTPException(status_code=501, detail=f"Tool {tool_name} not yet wired")

@app.get("/tools")
async def list_tools():
    return {"tools": ["game_screenshot", "pack_validate", "catalog_list"]}
