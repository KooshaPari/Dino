"""
Module 6: Custom Routes, Resources, and Prompts
"""
from __future__ import annotations

import json
import logging
import os
from pathlib import Path

from fastmcp import FastMCP, Context
from starlette.responses import JSONResponse
from starlette.requests import Request

from .config import (
    _reload_event, _resolve_bepinex_root, _run_game_cli,
    DEBUG_LOG, CATALOG_JSON, logger
)
from ..ai_stack.preferences import DEFAULT_PREFERENCE_ORDER, PREF_ENV_VAR
from ..ai_stack.routing import get_provider_status, route_ai_request

def register(mcp: FastMCP):
    """Register resources, routes, and prompts with the MCP server."""

    # ===========================================================================
    # RESOURCES  (live data readable without tool calls)
    # ===========================================================================

    @mcp.resource("game://status")
    async def status_resource() -> str:
        return json.dumps(_run_game_cli("status"), indent=2)

    @mcp.resource("log://debug")
    async def debug_log_resource() -> str:
        # Importing here to avoid circular dependency or just using the registered tool name
        # We can also just implement the logic directly
        if not DEBUG_LOG.exists():
            return json.dumps({"success": False, "error": f"Debug log not found: {DEBUG_LOG}"})
        try:
            with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                all_lines = f.readlines()
            tail = all_lines[-200:]
            return "\n".join([l.rstrip() for l in tail])
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})

    @mcp.resource("catalog://bundles")
    async def catalog_resource() -> str:
        if not CATALOG_JSON.exists():
            return json.dumps({"success": False, "error": f"Catalog not found: {CATALOG_JSON}"})
        try:
            with open(CATALOG_JSON, encoding="utf-8") as f:
                cat = json.load(f)
            bundles = [
                s.replace("{UnityEngine.AddressableAssets.Addressables.RuntimePath}", "")
                for s in cat.get("m_InternalIds", [])
                if s.endswith(".bundle")
            ]
            return json.dumps({"success": True, "bundles": bundles, "count": len(bundles)}, indent=2)
        except Exception as e:
            return json.dumps({"success": False, "error": str(e)})

    # ===========================================================================
    # HEALTH CHECK ENDPOINT
    # ===========================================================================

    @mcp.custom_route("/health", methods=["GET"])
    async def health_check(request: Request) -> JSONResponse:
        """Health check endpoint for service monitoring and startup verification."""
        return JSONResponse({"status": "ok", "server": "dinoforge-mcp", "version": "0.13.0"})

    @mcp.custom_route("/game/navigate", methods=["POST"])
    async def game_navigate_route(request: Request) -> JSONResponse:
        """REST shim: POST /game/navigate  body: {"state":"gameplay"}"""
        body = await request.json()
        state = body.get("state", "gameplay")
        # Primary: check via pipe bridge
        result = _run_game_cli("status")
        running = result.get("Running") or ('"Running":true' in result.get("raw", ""))
        # Fallback: process-based check (pipe is unavailable on main menu)
        if not running:
            import subprocess as _sp
            try:
                ps_out = _sp.check_output(
                    ["powershell.exe", "-Command",
                     "Get-Process -Name 'Diplomacy is Not an Option' -ErrorAction SilentlyContinue | "
                     "Select-Object -First 1 -ExpandProperty Id"],
                    timeout=5, text=True
                ).strip()
                running = bool(ps_out and ps_out.isdigit())
            except Exception:
                pass
        if not running:
            return JSONResponse({"success": False, "error": "Game not running"})
        if state == "gameplay":
            save_result = _run_game_cli("load-save", "AUTOSAVE_1")
            _run_game_cli("dismiss")
            return JSONResponse({"success": True, "loadResult": save_result})
        return JSONResponse({"success": False, "error": f"Unsupported state: {state}"})

    @mcp.custom_route("/game/status", methods=["GET"])
    async def game_status_route(request: Request) -> JSONResponse:
        """REST shim: GET /game/status"""
        return JSONResponse(_run_game_cli("status"))

    @mcp.custom_route("/game/screenshot", methods=["POST"])
    async def game_screenshot_route(request: Request) -> JSONResponse:
        """REST shim: POST /game/screenshot"""
        result = _run_game_cli("screenshot")
        return JSONResponse(result)

    # ===========================================================================
    # HMR (HOT MODULE RELOAD) ENDPOINT
    # ===========================================================================

    @mcp.custom_route("/hmr", methods=["POST"])
    async def hmr_route(request: Request) -> JSONResponse:
        """
        HTTP endpoint for hot-module-reload notifications.
        Called by scripts/game/hot-reload.ps1 after deploying a new Runtime DLL.

        Writes the `DINOForge_HotReload` signal file to the BepInEx root (which the
        Runtime watcher polls by mtime) and clears MCP-side caches.
        """
        payload: dict[str, Any] = {"success": True}
        try:
            bepinex_root = _resolve_bepinex_root()
            signal_path = bepinex_root / "DINOForge_HotReload"
            signal_path.touch(exist_ok=True)
            payload["signal_path"] = str(signal_path)
            payload["message"] = "HMR signal written to BepInEx root; MCP caches cleared."
        except (FileNotFoundError, OSError) as ex:
            payload["success"] = False
            payload["error"] = str(ex)
            payload["message"] = "MCP caches cleared, but Runtime HMR signal NOT written."

        _reload_event.set()
        _reload_event.clear()
        status_code = 200 if payload["success"] else 500
        return JSONResponse(payload, status_code=status_code)

    @mcp.custom_route("/ai/v1/stack/preferences", methods=["GET"])
    @mcp.custom_route("/preferences/stack", methods=["GET"])
    async def ai_stack_preferences_route(_: Request) -> JSONResponse:
        preference = get_provider_status()
        return JSONResponse(
            {
                "preference_order": list(DEFAULT_PREFERENCE_ORDER),
                "configured_preference": os.getenv(PREF_ENV_VAR),
                "provider_status": preference,
            }
        )

    @mcp.custom_route("/ai/v1/router", methods=["POST"])
    @mcp.custom_route("/ai/v1/adapter", methods=["POST"])
    async def ai_stack_route(request: Request) -> JSONResponse:
        """Preference-aware shim routes for future Vercel AI SDK / Bifrost backends."""
        try:
            payload = await request.json()
        except Exception:
            return JSONResponse({"status": "error", "error": "Invalid JSON body"}, status_code=400)

        if not isinstance(payload, dict):
            return JSONResponse({"status": "error", "error": "JSON body must be an object"}, status_code=400)

        operation = str(payload.get("operation") or "chat")
        requested_provider_raw = payload.get("provider")
        preference_raw = payload.get("preferences")
        explicit_order = payload.get("preference_order")
        request_payload = payload.get("request")
        if request_payload is None:
            request_payload = payload.get("payload")
        if request_payload is None:
            request_payload = {}
        elif not isinstance(request_payload, dict):
            return JSONResponse({"status": "error", "error": "request/payload must be an object"}, status_code=400)

        if requested_provider_raw is not None and not isinstance(requested_provider_raw, str):
            return JSONResponse({"status": "error", "error": "provider must be a string"}, status_code=400)

        result = route_ai_request(
            request_payload,
            operation=operation,
            requested_provider=requested_provider_raw,
            preference_raw=preference_raw if isinstance(preference_raw, str) else None,
            explicit_preference_order=explicit_order if isinstance(explicit_order, list) else None,
        )

        status = result.get("status")
        if status == "unavailable":
            return JSONResponse(result, status_code=503)
        return JSONResponse(result)

    @mcp.tool()
    async def notify_hmr(ctx: Context) -> dict:
        """
        Trigger a Runtime hot-reload by writing the `DINOForge_HotReload` signal file
        to the BepInEx root directory. The Runtime's HMR watcher
        (RuntimeDriver.StartHmrWatcher) polls that file's mtime and performs a soft
        reload (pack refresh + ECS swap re-apply) when it changes.

        Also fires the MCP-side `_reload_event` so any in-process caches (pack
        listings, catalog reads) are invalidated.

        Resolves the BepInEx root via `$DINOFORGE_BEPINEX_ROOT` (preferred) or
        `$DINO_GAME_DIR`, defaulting to the canonical Steam install path.

        Typically called via POST http://127.0.0.1:8765/hmr after a Runtime rebuild
        or pack edit.
        """
        try:
            bepinex_root = _resolve_bepinex_root()
        except FileNotFoundError as ex:
            # Still flip the in-process event so MCP caches clear, but report the
            # failure to write the on-disk signal so the caller knows the Runtime
            # watcher will NOT pick this up.
            _reload_event.set()
            _reload_event.clear()
            return {
                "success": False,
                "error": str(ex),
                "message": "MCP caches cleared, but Runtime HMR signal NOT written.",
            }

        signal_path = bepinex_root / "DINOForge_HotReload"
        try:
            # touch() creates the file if missing, otherwise updates mtime — the
            # Runtime watcher keys off mtime, so either case triggers a reload.
            signal_path.touch(exist_ok=True)
        except OSError as ex:
            _reload_event.set()
            _reload_event.clear()
            return {
                "success": False,
                "error": f"Failed to write HMR signal at {signal_path}: {ex}",
                "message": "MCP caches cleared, but Runtime HMR signal NOT written.",
            }

        _reload_event.set()
        _reload_event.clear()
        return {
            "success": True,
            "signal_path": str(signal_path),
            "message": "HMR signal written to BepInEx root; MCP caches cleared.",
        }

    # ===========================================================================
    # PROMPTS
    # ===========================================================================

    @mcp.prompt()
    def debug_asset_swap(issue: str = "swaps not visible") -> str:
        return f"""Diagnose DINOForge asset swap issue: {issue}

Steps:
1. log_swap_status → check swaps_complete, render_mesh_entities_present
2. If render_mesh_entities_present=False → IncludePrefab fix not deployed, rebuild Runtime DLL
3. If swaps_complete=0 → check entity_swap_lines for "swapped 0/N entities"
4. game_query_entities("Unity.Rendering.RenderMesh") → verify entity count > 0
5. game_screenshot → visual confirmation
6. catalog_keys("") → verify asset addresses are NOT in catalog (normal for unit swaps)

Key facts:
- ALL DINO entities are ECS Prefab entities — EntityQueryOptions.IncludePrefab is mandatory
- Phase 1 (catalog disk patch) will always skip unit/building swaps — this is normal
- Phase 2 (live RenderMesh entity swap) is the primary mechanism
- 600-frame delay before swaps fire (~10s at 60fps)"""

    @mcp.prompt()
    def asset_pipeline_workflow(pack: str = "warfare-starwars") -> str:
        return f"""Asset pipeline workflow for pack: {pack}

1. pack_validate("{pack}") → verify YAML is valid
2. asset_validate("{pack}") → verify asset_pipeline.yaml
3. asset_import("{pack}") → download/convert source assets
4. asset_optimize("{pack}") → generate LOD variants
5. asset_build("{pack}") → full pipeline
6. game_launch → start test instance
7. game_wait_world → wait for ECS world
8. log_swap_status → verify swaps fired
9. game_screenshot → visual confirmation"""
