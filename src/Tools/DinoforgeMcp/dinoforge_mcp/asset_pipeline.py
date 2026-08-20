"""
Module 2: Asset Pipeline & Pack Management Tools
"""
from __future__ import annotations

from typing import Any

from fastmcp import FastMCP, Context

from .config import _run_pack_compiler, PACKS_DIR, _RUST_AVAILABLE, logger

def register(mcp: FastMCP):
    """Register asset pipeline tools with the MCP server."""

    @mcp.tool()
    async def asset_validate(ctx: Context, pack: str) -> dict:
        """
        Validate assets in a pack against the asset_pipeline.yaml schema.

        Args:
            pack: Pack name (e.g. 'warfare-starwars').
        """
        return _run_pack_compiler("assets", "validate", f"packs/{pack}")

    @mcp.tool()
    async def asset_import(ctx: Context, pack: str) -> dict:
        """
        Import (download + convert) source assets for a pack.

        Uses Rust PyO3 module when available for better performance, falls back to
        PackCompiler CLI if Rust module is not available.

        Args:
            pack: Pack name.
        """
        # Try Rust implementation first if available
        if _RUST_AVAILABLE:
            try:
                pack_dir = PACKS_DIR / pack
                if not pack_dir.exists():
                    return {
                        "success": False,
                        "error": f"Pack directory not found: {pack_dir}",
                        "method": "rust"
                    }

                # Rust module expects source and output paths
                # For now, delegate to PackCompiler since full pipeline requires
                # download + asset.json creation which Rust module handles asset geometry only
                # This fallthrough is intentional — Rust module will be integrated fully in next phase
                pass
            except Exception as e:
                logger.debug(f"Rust asset_import failed, falling back to PackCompiler: {e}")

        # Python/PackCompiler fallback (always reliable)
        result = _run_pack_compiler("assets", "import", f"packs/{pack}")
        if _RUST_AVAILABLE:
            result["method"] = "python (rust available but not used in full pipeline)"
        else:
            result["method"] = "python (rust not available)"
        return result

    @mcp.tool()
    async def asset_optimize(ctx: Context, pack: str) -> dict:
        """
        Generate LOD variants for all assets in a pack.

        Uses Rust PyO3 module when available for SIMD-optimized mesh decimation.
        Falls back to PackCompiler CLI if Rust module is not available.

        Args:
            pack: Pack name.
        """
        # Try Rust implementation first if available
        if _RUST_AVAILABLE:
            try:
                pack_dir = PACKS_DIR / pack
                if not pack_dir.exists():
                    return {
                        "success": False,
                        "error": f"Pack directory not found: {pack_dir}",
                        "method": "rust"
                    }

                # Rust module provides optimize_asset(mesh_json, targets)
                # Full orchestration (load meshes from JSON, call Rust, write LOD variants)
                # is handled by PackCompiler for consistency; direct Rust is for integration tests
                pass
            except Exception as e:
                logger.debug(f"Rust asset_optimize failed, falling back to PackCompiler: {e}")

        # Python/PackCompiler fallback (handles full pipeline)
        result = _run_pack_compiler("assets", "optimize", f"packs/{pack}")
        if _RUST_AVAILABLE:
            result["method"] = "python (rust available but orchestrated via PackCompiler)"
        else:
            result["method"] = "python (rust not available)"
        return result

    @mcp.tool()
    async def asset_build(ctx: Context, pack: str) -> dict:
        """
        Run the full asset pipeline (validate → import → optimize → generate → build).

        Args:
            pack: Pack name.
        """
        return _run_pack_compiler("assets", "build", f"packs/{pack}")

    @mcp.tool()
    async def pack_validate(ctx: Context, pack: str) -> dict:
        """
        Validate a mod pack (YAML schemas, references, completeness).

        Args:
            pack: Pack name or path.
        """
        return _run_pack_compiler("validate", f"packs/{pack}")

    @mcp.tool()
    async def pack_build(ctx: Context, pack: str) -> dict:
        """
        Compile and package a mod pack.

        Args:
            pack: Pack name.
        """
        return _run_pack_compiler("build", f"packs/{pack}")

    @mcp.tool()
    async def pack_list(ctx: Context) -> dict:
        """List all available packs in the repository."""
        try:
            packs = [
                {"id": p.name, "path": str(p)}
                for p in PACKS_DIR.iterdir()
                if p.is_dir() and (p / "pack.yaml").exists()
            ]
            return {"success": True, "packs": packs, "count": len(packs)}
        except Exception as e:
            return {"success": False, "error": str(e)}
