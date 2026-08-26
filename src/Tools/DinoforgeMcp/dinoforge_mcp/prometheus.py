"""Prometheus-compatible metrics for DINOForge MCP."""
from __future__ import annotations

import time
import logging
from typing import Any

logger = logging.getLogger("dinoforge_mcp.prometheus")

_metrics: dict[str, Any] = {
    "requests_total": 0,
    "requests_by_tool": {},
    "errors_total": 0,
    "latency_sum": 0.0,
    "latency_count": 0,
    "uptime_start": time.time(),
    "bridge_connects": 0,
    "bridge_disconnects": 0,
}


def record_request(tool: str, latency_ms: float, success: bool) -> None:
    _metrics["requests_total"] += 1
    _metrics["requests_by_tool"][tool] = _metrics["requests_by_tool"].get(tool, 0) + 1
    _metrics["latency_sum"] += latency_ms / 1000.0
    _metrics["latency_count"] += 1
    if not success:
        _metrics["errors_total"] += 1


def record_bridge_event(event: str) -> None:
    if event == "connect":
        _metrics["bridge_connects"] += 1
    elif event == "disconnect":
        _metrics["bridge_disconnects"] += 1


def render_prometheus() -> str:
    lines = []
    lines.append("# HELP dinoforge_requests_total Total requests")
    lines.append("# TYPE dinoforge_requests_total counter")
    lines.append(f'dinoforge_requests_total {_metrics["requests_total"]}')
    lines.append("")
    lines.append("# HELP dinoforge_requests_by_tool Requests by tool")
    lines.append("# TYPE dinoforge_requests_by_tool counter")
    for tool, count in _metrics["requests_by_tool"].items():
        lines.append(f'dinoforge_requests_by_tool{{tool="{tool}"}} {count}')
    lines.append("")
    lines.append("# HELP dinoforge_errors_total Total errors")
    lines.append("# TYPE dinoforge_errors_total counter")
    lines.append(f'dinoforge_errors_total {_metrics["errors_total"]}')
    lines.append("")
    lines.append("# HELP dinoforge_latency_seconds Request latency")
    lines.append("# TYPE dinoforge_latency_seconds summary")
    if _metrics["latency_count"] > 0:
        avg = _metrics["latency_sum"] / _metrics["latency_count"]
        lines.append(f'dinoforge_latency_seconds_sum {_metrics["latency_sum"]:.3f}')
        lines.append(f'dinoforge_latency_seconds_count {_metrics["latency_count"]}')
        lines.append(f'dinoforge_latency_seconds_avg {avg:.3f}')
    lines.append("")
    uptime = time.time() - _metrics["uptime_start"]
    lines.append("# HELP dinoforge_uptime_seconds Server uptime")
    lines.append("# TYPE dinoforge_uptime_seconds gauge")
    lines.append(f'dinoforge_uptime_seconds {uptime:.1f}')
    lines.append("")
    lines.append("# HELP dinoforge_bridge_connects_total Bridge connections")
    lines.append("# TYPE dinoforge_bridge_connects_total counter")
    lines.append(f'dinoforge_bridge_connects_total {_metrics["bridge_connects"]}')
    lines.append(f'dinoforge_bridge_disconnects_total {_metrics["bridge_disconnects"]}')
    return chr(10).join(lines)
