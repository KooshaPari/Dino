"""Production integration layer for DINOForge MCP server.

Wires together: circuit_breaker, rate_limiter, agent_auth, agent_metrics,
audit_logger, health, prometheus, and tracing into a cohesive startup sequence.

Usage:
    from dinoforge_mcp.integration import init_production
    init_production(app=mcp_app, secret_key=os.environ["DINOFORGE_SECRET_KEY"])
"""
from __future__ import annotations

import logging
import os
import time
from typing import Any

logger = logging.getLogger("dinoforge_mcp.integration")


def init_production(
    app: Any = None,
    secret_key: str | None = None,
    prometheus_port: int = 8080,
    enable_tracing: bool = False,
    tracing_endpoint: str | None = None,
    rate_limit_per_agent: int = 100,
    rate_limit_capacity: int = 200,
    circuit_breaker_threshold: int = 5,
    circuit_breaker_timeout: float = 30.0,
) -> dict[str, Any]:
    """Initialize all production modules and return the wired context.

    This is the single entry point for production deployments.
    All modules are lazy-imported to avoid startup failures if optional
    dependencies (like opentelemetry) are not installed.
    """
    context: dict[str, Any] = {}
    start = time.monotonic()

    # 1. Agent Authentication
    try:
        from dinoforge_mcp.agent_auth import AgentAuth
        auth = AgentAuth(secret_key=secret_key or os.environ.get("DINOFORGE_SECRET_KEY", "dev"))
        context["auth"] = auth
        logger.info("Agent authentication initialized")
    except Exception as e:
        logger.warning("Agent auth init failed: %s", e)

    # 2. Rate Limiter
    try:
        from dinoforge_mcp.rate_limiter import TokenBucketLimiter
        limiter = TokenBucketLimiter(rate=rate_limit_per_agent, capacity=rate_limit_capacity)
        context["rate_limiter"] = limiter
        logger.info("Rate limiter initialized: %d/s per agent", rate_limit_per_agent)
    except Exception as e:
        logger.warning("Rate limiter init failed: %s", e)

    # 3. Circuit Breaker
    try:
        from dinoforge_mcp.circuit_breaker import CircuitBreaker
        cb = CircuitBreaker(
            failure_threshold=circuit_breaker_threshold,
            recovery_timeout=circuit_breaker_timeout,
        )
        context["circuit_breaker"] = cb
        logger.info("Circuit breaker initialized: threshold=%d, timeout=%.0fs",
                     circuit_breaker_threshold, circuit_breaker_timeout)
    except Exception as e:
        logger.warning("Circuit breaker init failed: %s", e)

    # 4. Audit Logger
    try:
        from dinoforge_mcp.audit_logger import audit
        context["audit"] = audit
        logger.info("Audit logger initialized")
    except Exception as e:
        logger.warning("Audit logger init failed: %s", e)

    # 5. Agent Metrics Collector
    try:
        from dinoforge_mcp.agent_metrics import get_metrics_collector
        metrics = get_metrics_collector()
        context["metrics"] = metrics
        logger.info("Agent metrics collector initialized")
    except Exception as e:
        logger.warning("Metrics collector init failed: %s", e)

    # 6. Tool Registry
    try:
        from dinoforge_mcp.tool_registry import get_tool_registry
        registry = get_tool_registry()
        context["tool_registry"] = registry
        logger.info("Tool registry initialized")
    except Exception as e:
        logger.warning("Tool registry init failed: %s", e)

    # 7. Health Checks
    try:
        from dinoforge_mcp.health import HealthChecker
        checker = HealthChecker()
        context["health"] = checker
        logger.info("Health checker initialized")
    except Exception as e:
        logger.warning("Health checker init failed: %s", e)

    # 8. Prometheus Metrics
    try:
        from dinoforge_mcp.prometheus import PrometheusMetrics
        prom = PrometheusMetrics()
        context["prometheus"] = prom
        logger.info("Prometheus metrics initialized")
    except Exception as e:
        logger.warning("Prometheus init failed: %s", e)

    # 9. Tracing (optional)
    if enable_tracing:
        try:
            from dinoforge_mcp.tracing import init_tracing
            init_tracing(
                service_name="dinoforge-mcp",
                endpoint=tracing_endpoint,
            )
            context["tracing"] = True
            logger.info("Tracing initialized: endpoint=%s", tracing_endpoint or "console")
        except Exception as e:
            logger.warning("Tracing init failed: %s", e)

    elapsed_ms = round((time.monotonic() - start) * 1000, 1)
    logger.info("Production init complete: %d modules in %.1fms", len(context), elapsed_ms)

    return context


def wrap_tool_call(
    tool_name: str,
    args: dict[str, Any],
    context: dict[str, Any],
    fn: Callable[..., Any],
    agent_id: str = "unknown",
) -> Any:
    """Wrap a tool invocation with auth, rate limiting, circuit breaker, metrics, and audit.

    This is the production-grade tool dispatch function.
    All checks are optional — if a module failed to init, the call proceeds.
    """
    start = time.monotonic()

    # 1. Rate limit check
    limiter = context.get("rate_limiter")
    if limiter and not limiter.allow(agent_id):
        raise RuntimeError(f"Rate limit exceeded for agent {agent_id}")

    # 2. Circuit breaker check
    cb = context.get("circuit_breaker")
    if cb and not cb.allow_request():
        raise RuntimeError("Circuit breaker open — bridge connection degraded")

    try:
        # 3. Execute the tool
        result = fn(**args)

        # 4. Record success
        latency_ms = round((time.monotonic() - start) * 1000, 1)
        metrics = context.get("metrics")
        if metrics:
            metrics.record_invocation(agent_id, tool_name, latency_ms)

        registry = context.get("tool_registry")
        if registry:
            registry.record_invocation(tool_name, latency_ms, error=False)

        audit = context.get("audit")
        if audit:
            audit.tool_invocation(tool_name, actor=agent_id, status="success")

        if cb:
            cb.record_success()

        return result

    except Exception as e:
        # 5. Record failure
        latency_ms = round((time.monotonic() - start) * 1000, 1)
        metrics = context.get("metrics")
        if metrics:
            metrics.record_error(agent_id, tool_name)

        registry = context.get("tool_registry")
        if registry:
            registry.record_invocation(tool_name, latency_ms, error=True)

        audit = context.get("audit")
        if audit:
            audit.tool_invocation(tool_name, actor=agent_id, status="error", error=str(e))

        if cb:
            cb.record_failure()

        raise
