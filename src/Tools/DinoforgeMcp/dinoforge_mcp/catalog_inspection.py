"""
Module 3: Addressables Catalog Inspection Tools
"""
from __future__ import annotations

import json
from pathlib import Path

from fastmcp import FastMCP, Context

from .config import CATALOG_JSON

def register(mcp: FastMCP):
    """Register catalog inspection tools with the MCP server."""

    @mcp.tool()
    async def catalog_keys(ctx: Context, filter_term: str = "") -> dict:
        """
        List Addressables catalog keys (asset addresses used in the game).

        Args:
            filter_term: Optional substring filter on keys.
        """
        if not CATALOG_JSON.exists():
            return {"success": False, "error": f"Catalog not found: {CATALOG_JSON}"}
        try:
            with open(CATALOG_JSON, encoding="utf-8") as f:
                cat = json.load(f)
            ids: list[str] = cat.get("m_InternalIds", [])
            non_bundle = [s for s in ids if not s.startswith("{") and not s.endswith(".bundle")]
            if filter_term:
                non_bundle = [s for s in non_bundle if filter_term.lower() in s.lower()]
            return {"success": True, "keys": non_bundle[:200], "total": len(non_bundle)}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def catalog_bundles(ctx: Context) -> dict:
        """List all AssetBundle files registered in the Addressables catalog."""
        if not CATALOG_JSON.exists():
            return {"success": False, "error": f"Catalog not found: {CATALOG_JSON}"}
        try:
            with open(CATALOG_JSON, encoding="utf-8") as f:
                cat = json.load(f)
            bundles = [
                s.replace("{UnityEngine.AddressableAssets.Addressables.RuntimePath}", "")
                for s in cat.get("m_InternalIds", [])
                if s.endswith(".bundle")
            ]
            return {"success": True, "bundles": bundles, "count": len(bundles)}
        except Exception as e:
            return {"success": False, "error": str(e)}
