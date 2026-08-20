"""
Module 4: Debug Log Analysis Tools
"""
from __future__ import annotations

import asyncio
import logging
import os
import re
from pathlib import Path

from fastmcp import FastMCP, Context

from .config import DEBUG_LOG, BEPINEX_DIR, logger

def register(mcp: FastMCP):
    """Register log analysis tools with the MCP server."""

    @mcp.tool()
    async def log_tail(ctx: Context, lines: int = 100) -> dict:
        """
        Read the last N lines from the DINOForge debug log.

        Args:
            lines: Number of lines to return (default 100).
        """
        if not DEBUG_LOG.exists():
            return {"success": False, "error": f"Debug log not found: {DEBUG_LOG}"}
        try:
            with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                all_lines = f.readlines()
            tail = all_lines[-lines:]
            return {"success": True, "lines": [l.rstrip() for l in tail], "total_lines": len(all_lines)}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def log_swap_status(ctx: Context) -> dict:
        """
        Parse the debug log and summarise asset swap results for the latest game session.
        Returns swap success count, pending count, entity counts, and any exceptions.
        """
        if not DEBUG_LOG.exists():
            return {"success": False, "error": f"Debug log not found: {DEBUG_LOG}"}
        try:
            with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                content = f.read()

            lines = content.splitlines()
            # Find the last OnCreate (start of latest session)
            session_start = 0
            for i, line in enumerate(lines):
                if "AssetSwapSystem.OnCreate" in line:
                    session_start = i

            session_lines = lines[session_start:]
            completed = sum(1 for l in session_lines if "swap complete" in l)
            pending = sum(1 for l in session_lines if "live swap pending" in l)
            exceptions = [l for l in session_lines if "swap exception" in l]
            entity_lines = [l for l in session_lines if "swapped " in l and "/"]
            render_line = next((l for l in session_lines if "RenderMesh entities present" in l), None)
            probe_line = next((l for l in session_lines if "probe query created" in l), None)

            return {
                "success": True,
                "session_start_line": session_start,
                "swaps_complete": completed,
                "swaps_pending": pending,
                "exceptions": exceptions,
                "entity_swap_lines": entity_lines,
                "render_mesh_entities_present": render_line is not None,
                "probe_query_line": probe_line,
            }
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def log_bepinex(ctx: Context, lines: int = 50) -> dict:
        """
        Read the last N lines from the BepInEx LogOutput.log.

        Args:
            lines: Number of lines to return.
        """
        bepinex_log = BEPINEX_DIR / "LogOutput.log"
        if not bepinex_log.exists():
            return {"success": False, "error": f"BepInEx log not found: {bepinex_log}"}
        try:
            with open(bepinex_log, encoding="utf-8", errors="replace") as f:
                all_lines = f.readlines()
            tail = all_lines[-lines:]
            return {"success": True, "lines": [l.rstrip() for l in tail]}
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def log_debug_log(ctx: Context, lines: int = 500) -> dict:
        """
        Read the full DINOForge debug log (all entries, not just tail).
        Use this for deep analysis of swap exceptions, ECS world state, and pack loading.

        Args:
            lines: Maximum lines to return (default 500, use 0 for all).
        """
        if not DEBUG_LOG.exists():
            return {"success": False, "error": f"Debug log not found: {DEBUG_LOG}"}
        try:
            with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                all_lines = f.readlines()
            tail = all_lines[-lines:] if lines > 0 else all_lines
            return {
                "success": True,
                "path": str(DEBUG_LOG),
                "total_lines": len(all_lines),
                "returned_lines": len(tail),
                "lines": [l.rstrip() for l in tail],
            }
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def log_packs_loaded(ctx: Context) -> dict:
        """
        Extract pack loading summary from the debug log (PacksLoader.OnAfterDeserialize output).
        Returns a list of loaded packs with their versions and any load errors.
        """
        if not DEBUG_LOG.exists():
            return {"success": False, "error": f"Debug log not found: {DEBUG_LOG}"}
        try:
            with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                content = f.read()

            packs: list[dict] = []
            for line in content.splitlines():
                if any(tag in line for tag in ("PacksLoader", "Pack loaded", "Pack load error",
                                                "warfare-starwars", "warfare-modern", "warfare-guerrilla",
                                                "economy-balanced", "example-balance")):
                    ts = line[:23] if len(line) >= 23 else ""
                    msg = line[24:].strip() if len(line) > 24 else line
                    packs.append({"timestamp": ts, "line": msg.strip()})

            return {
                "success": True,
                "total_entries": len(packs),
                "entries": packs,
            }
        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def game_log_search(ctx: Context, pattern: str, tail: int = 1000) -> dict:
        """
        Search the last N lines of the DINOForge debug log for a regex pattern.
        Case-insensitive search by default.

        Args:
            pattern: Regular expression pattern to search for (case-insensitive).
            tail: Number of lines to search in (default 1000, use 0 for all).

        Returns:
            Dictionary with matching lines and match count.
        """
        if not DEBUG_LOG.exists():
            return {"success": False, "error": f"Debug log not found: {DEBUG_LOG}"}

        try:
            def search_log() -> dict:
                try:
                    with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                        all_lines = f.readlines()

                    # Get tail lines
                    lines_to_search = all_lines[-tail:] if tail > 0 else all_lines
                    total_lines = len(all_lines)

                    # Compile regex (case-insensitive)
                    try:
                        regex = re.compile(pattern, re.IGNORECASE)
                    except re.error as e:
                        return {
                            "success": False,
                            "error": f"Invalid regex pattern: {e}"
                        }

                    # Search
                    matches = []
                    for i, line in enumerate(lines_to_search):
                        if regex.search(line):
                            matches.append({
                                "line_number": total_lines - len(lines_to_search) + i + 1,
                                "text": line.rstrip()
                            })

                    return {
                        "success": True,
                        "pattern": pattern,
                        "matches": matches,
                        "match_count": len(matches),
                        "lines_searched": len(lines_to_search),
                        "total_lines": total_lines,
                    }
                except Exception as e:
                    return {"success": False, "error": str(e)}

            # Run synchronously in thread pool to avoid blocking event loop
            return await asyncio.to_thread(search_log)

        except Exception as e:
            return {"success": False, "error": str(e)}

    @mcp.tool()
    async def game_log_stream(
        ctx: Context,
        lines: int = 100,
        follow: bool = False,
        filter: str | None = None,
    ) -> dict:
        """
        Stream or tail the DINOForge debug log with optional regex filtering.
        When follow=True, returns initial lines and streams new entries (best-effort).
        Agents can poll this tool periodically to monitor log updates in real-time.

        Args:
            lines: Initial number of lines to return (default 100).
            follow: If True, poll for new lines and yield them progressively.
            filter: Optional regex pattern to filter lines (case-insensitive).

        Returns:
            Dictionary with initial lines and metadata. For follow=True, subsequent
            calls with higher line counts reveal new entries.
        """
        if not DEBUG_LOG.exists():
            return {"success": False, "error": f"Debug log not found: {DEBUG_LOG}"}

        try:
            def stream_log() -> dict:
                try:
                    with open(DEBUG_LOG, encoding="utf-8", errors="replace") as f:
                        all_lines = f.readlines()

                    # Get tail lines
                    tail_lines = all_lines[-lines:] if lines > 0 else all_lines
                    total_lines = len(all_lines)

                    # Compile filter regex if provided
                    regex = None
                    if filter:
                        try:
                            regex = re.compile(filter, re.IGNORECASE)
                        except re.error as e:
                            return {
                                "success": False,
                                "error": f"Invalid filter pattern: {e}"
                            }

                    # Apply filter
                    filtered_lines = []
                    if regex:
                        filtered_lines = [
                            l.rstrip()
                            for l in tail_lines
                            if regex.search(l)
                        ]
                    else:
                        filtered_lines = [l.rstrip() for l in tail_lines]

                    return {
                        "success": True,
                        "lines": filtered_lines,
                        "line_count": len(filtered_lines),
                        "total_lines": total_lines,
                        "follow": follow,
                        "filter": filter,
                        "note": "When follow=True, call again to check for new lines" if follow else None,
                    }
                except Exception as e:
                    return {"success": False, "error": str(e)}

            # Run in thread pool to avoid blocking
            return await asyncio.to_thread(stream_log)

        except Exception as e:
            return {"success": False, "error": str(e)}
