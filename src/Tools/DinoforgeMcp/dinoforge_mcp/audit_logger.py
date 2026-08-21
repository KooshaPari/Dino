"""Structured audit logging for DINOForge MCP."""
from __future__ import annotations

import json
import logging
import os
import time
from dataclasses import dataclass, field, asdict
from typing import Any

logger = logging.getLogger("dinoforge_mcp.audit")


@dataclass(frozen=True)
class AuditEvent:
    """Immutable audit event."""
    timestamp: float = field(default_factory=time.time)
    event_type: str = ""
    action: str = ""
    actor: str = "system"
    resource: str = ""
    status: str = "success"
    details: dict[str, Any] = field(default_factory=dict)
    session_id: str | None = None
    ip_address: str | None = None
    user_agent: str | None = None


    def to_dict(self) -> dict[str, Any]:
        return {k: v for k, v in asdict(self).items() if v is not None}

    def to_json(self) -> str:
        return json.dumps(self.to_dict(), default=str)


class AuditLogger:
    """Structured audit logger with configurable backends."""

    def __init__(self, service: str = "dinoforge-mcp") -> None:
        self.service = service
        self._logger = logging.getLogger(f"dinoforge_mcp.audit.{service}")
        self._events: list[AuditEvent] = []
        self._max_buffer = int(os.environ.get("AUDIT_BUFFER_SIZE", "1000"))

    def log(self, event: AuditEvent) -> None:
        """Log an audit event."""
        self._events.append(event)
        if len(self._events) > self._max_buffer:
            self._events = self._events[-self._max_buffer:]
        self._logger.info(event.to_json())

    def tool_invocation(
        self,
        tool_name: str,
        actor: str = "agent",
        status: str = "success",
        args: dict[str, Any] | None = None,
        error: str | None = None,
    ) -> None:
        details: dict[str, Any] = {"tool": tool_name}
        if args:
            details["args"] = {k: str(v)[:200] for k, v in args.items()}
        if error:
            details["error"] = error[:500]
        self.log(AuditEvent(
            event_type="tool_invocation",
            action=f"invoke.{tool_name}",
            actor=actor,
            resource=f"tool:{tool_name}",
            status=status,
            details=details,
        ))

    def authentication(
        self, actor: str, method: str, status: str = "success") -> None:
        self.log(AuditEvent(
            event_type="authentication",
            action=f"auth.{method}",
            actor=actor,
            resource="auth",
            status=status,
        ))

    def config_change(self, actor: str, key: str, old_value: str = "", new_value: str = "") -> None:
        self.log(AuditEvent(
            event_type="config_change",
            action=f"config.update.{key}",
            actor=actor,
            resource=f"config:{key}",
            status="success",
            details={"old": old_value[:200], "new": new_value[:200]},
        ))

    def get_recent(self, count: int = 100) -> list[dict[str, Any]]:
        """Get recent audit events."""
        return [e.to_dict() for e in self._events[-count:]]

    def get_stats(self) -> dict[str, int]:
        """Get event type counts."""
        counts: dict[str, int] = {}
        for e in self._events:
            counts[e.event_type] = counts.get(e.event_type, 0) + 1
        return counts


# Global audit logger
audit = AuditLogger()
