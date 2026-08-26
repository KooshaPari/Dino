"""Circuit breaker for game bridge connections."""
from __future__ import annotations

import time
import logging
from enum import Enum

logger = logging.getLogger("dinoforge_mcp.circuit_breaker")


class State(Enum):
    CLOSED = "closed"      # Normal operation
    OPEN = "open"          # Failing, reject calls
    HALF_OPEN = "half_open" # Testing recovery


class CircuitBreaker:
    def __init__(self, failure_threshold: int = 5, recovery_timeout: float = 30.0) -> None:
        self._failure_threshold = failure_threshold
        self._recovery_timeout = recovery_timeout
        self._state = State.CLOSED
        self._failure_count = 0
        self._last_failure_time = 0.0
        self._success_count = 0

    @property
    def state(self) -> State:
        if self._state == State.OPEN:
            if time.time() - self._last_failure_time >= self._recovery_timeout:
                self._state = State.HALF_OPEN
                logger.info("Circuit breaker: OPEN -> HALF_OPEN")
        return self._state

    def record_success(self) -> None:
        self._failure_count = 0
        if self._state == State.HALF_OPEN:
            self._success_count += 1
            if self._success_count >= 3:
                self._state = State.CLOSED
                self._success_count = 0
                logger.info("Circuit breaker: HALF_OPEN -> CLOSED")

    def record_failure(self) -> None:
        self._failure_count += 1
        self._last_failure_time = time.time()
        self._success_count = 0
        if self._failure_count >= self._failure_threshold:
            self._state = State.OPEN
            logger.warning(f"Circuit breaker: OPEN after {self._failure_count} failures")

    def allow_request(self) -> bool:
        s = self.state
        if s == State.CLOSED:
            return True
        if s == State.HALF_OPEN:
            return True
        return False


_breaker: CircuitBreaker | None = None

def get_circuit_breaker() -> CircuitBreaker:
    global _breaker
    if _breaker is None:
        _breaker = CircuitBreaker()
    return _breaker
