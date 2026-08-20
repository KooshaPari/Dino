"""Per-agent rate limiting for DINOForge MCP."""
from __future__ import annotations

import time
from collections import defaultdict
from dataclasses import dataclass, field
from typing import Any


@dataclass
class RateLimitConfig:
    """Configuration for rate limiting."""
    max_requests: int = 100
    window_seconds: int = 60
    burst_limit: int = 20


@dataclass
class AgentRateLimiter:
    """Token bucket rate limiter per agent ID."""
    config: RateLimitConfig = field(default_factory=RateLimitConfig)
    _buckets: dict[str, list[float]] = field(default_factory=lambda: defaultdict(list))

    def check(self, agent_id: str) -> bool:
        """Check if agent is within rate limit. Returns True if allowed."""
        now = time.time()
        window_start = now - self.config.window_seconds
        self._buckets[agent_id] = [t for t in self._buckets[agent_id] if t > window_start]
        if len(self._buckets[agent_id]) >= self.config.max_requests:
            return False
        self._buckets[agent_id].append(now)
        return True

    def remaining(self, agent_id: str) -> int:
        """Return remaining requests in current window."""
        now = time.time()
        window_start = now - self.config.window_seconds
        self._buckets[agent_id] = [t for t in self._buckets[agent_id] if t > window_start]
        return max(0, self.config.max_requests - len(self._buckets[agent_id]))

    def reset(self, agent_id: str) -> None:
        """Reset rate limit for an agent."""
        self._buckets.pop(agent_id, None)


# Global singleton
_rate_limiter: AgentRateLimiter | None = None


def get_rate_limiter() -> AgentRateLimiter:
    global _rate_limiter
    if _rate_limiter is None:
        _rate_limiter = AgentRateLimiter()
    return _rate_limiter
