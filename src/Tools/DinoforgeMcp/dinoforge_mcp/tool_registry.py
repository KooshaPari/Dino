"""Enhanced tool registry with versioning, discovery, and metadata."""
from __future__ import annotations
import time, logging
from dataclasses import dataclass, field
from typing import Any

logger = logging.getLogger("dinoforge_mcp.tool_registry")

@dataclass
class ToolMetadata:
    name: str
    version: str = "1.0.0"
    module: str = ""
    description: str = ""
    category: str = "general"
    tags: list[str] = field(default_factory=list)
    registered_at: float = field(default_factory=time.time)
    invocation_count: int = 0
    error_count: int = 0
    avg_latency_ms: float = 0.0

class ToolRegistry:
    def __init__(self) -> None:
        self._tools: dict[str, ToolMetadata] = {}
        self._categories: dict[str, list[str]] = {}

    def register(self, name: str, version: str = "1.0.0", module: str = "", description: str = "", category: str = "general", tags: list[str] | None = None) -> ToolMetadata:
        meta = ToolMetadata(name=name, version=version, module=module, description=description, category=category, tags=tags or [])
        self._tools[name] = meta
        self._categories.setdefault(category, []).append(name)
        return meta

    def get(self, name: str) -> ToolMetadata | None:
        return self._tools.get(name)

    def list_all(self) -> list[ToolMetadata]:
        return sorted(self._tools.values(), key=lambda t: t.name)

    def list_by_category(self, category: str) -> list[ToolMetadata]:
        return [self._tools[n] for n in self._categories.get(category, []) if n in self._tools]

    def record_invocation(self, name: str, latency_ms: float, error: bool = False) -> None:
        meta = self._tools.get(name)
        if meta:
            meta.invocation_count += 1
            if error:
                meta.error_count += 1
            meta.avg_latency_ms = (meta.avg_latency_ms * (meta.invocation_count - 1) + latency_ms) / meta.invocation_count

    def get_stats(self) -> dict[str, Any]:
        return {
            "total_tools": len(self._tools),
            "categories": {k: len(v) for k, v in self._categories.items()},
            "total_invocations": sum(t.invocation_count for t in self._tools.values()),
            "total_errors": sum(t.error_count for t in self._tools.values()),
        }

_registry: ToolRegistry | None = None

def get_tool_registry() -> ToolRegistry:
    global _registry
    if _registry is None:
        _registry = ToolRegistry()
    return _registry
