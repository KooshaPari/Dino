"""Health check endpoints for K8s liveness and readiness probes."""
from __future__ import annotations

import time
from typing import Any

from dinoforge_mcp.circuit_breaker import get_circuit_breaker, State
from dinoforge_mcp.agent_metrics import get_metrics_collector
from dinoforge_mcp.audit_logger import audit


_start_time = time.time()


def health_liveness() -> dict[str, Any]:
    return {"status": "alive", "uptime_seconds": round(time.time() - _start_time, 1)}


def health_readiness() -> dict[str, Any]:
    cb = get_circuit_breaker()
    collector = get_metrics_collector()
    bridge_ok = cb.state != State.OPEN
    metrics_ok = collector is not None
    return {
        "status": "ready" if bridge_ok and metrics_ok else "not_ready",
        "bridge_circuit": cb.state.value,
        "metrics_active": metrics_ok,
        "audit_events": len(audit.get_recent(1)),
    }


def health_startup() -> dict[str, Any]:
    return {"status": "started", "version": "0.25.0-dev"}
