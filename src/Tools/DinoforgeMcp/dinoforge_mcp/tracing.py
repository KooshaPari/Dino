"""OpenTelemetry tracing setup for DINOForge MCP."""
from __future__ import annotations

import logging
import os
from functools import wraps
from typing import Any, Callable, TypeVar

logger = logging.getLogger("dinoforge_mcp.tracing")
F = TypeVar("F", bound=Callable[..., Any])

# Lazy init - only creates tracer if OTEL is available
def _get_tracer():
    try:
        from opentelemetry import trace
        from opentelemetry.sdk.trace import TracerProvider
        from opentelemetry.sdk.trace.export import BatchSpanProcessor
        from opentelemetry.exporter.otlp.proto.grpc.trace_exporter import OTLPSpanExporter
        from opentelemetry.sdk.resources import Resource

        resource = Resource.create({
            "service.name": "dinoforge-mcp",
            "service.version": os.environ.get("DINOFORGE_VERSION", "0.1.0"),
            "deployment.environment": os.environ.get("DINOFORGE_ENV", "development"),
        })

        provider = TracerProvider(resource=resource)
        endpoint = os.environ.get("OTEL_EXPORTER_OTLP_ENDPOINT", "localhost:4317")
        exporter = OTLPSpanExporter(endpoint=endpoint, insecure=True)
        provider.add_span_processor(BatchSpanProcessor(exporter))
        trace.set_tracer_provider(provider)
        return trace.get_tracer("dinoforge-mcp")
    except ImportError:
        logger.debug("opentelemetry not installed, tracing disabled")
        return None
    except Exception as e:
        logger.warning(f"Failed to init OTEL: {e}")
        return None


_tracer = None


def get_tracer():
    global _tracer
    if _tracer is None:
        _tracer = _get_tracer()
    return _tracer


def trace_tool(func: F) -> F:
    """Decorator to trace MCP tool invocations."""
    @wraps(func)
    async def wrapper(*args: Any, **kwargs: Any) -> Any:
        tracer = get_tracer()
        if tracer is None:
            return await func(*args, **kwargs)
        with tracer.start_as_current_span(
            f"tool.{func.__name__}",
            attributes={
                "tool.name": func.__name__,
                "tool.module": func.__module__ or "unknown",
            }
        ) as span:
            try:
                result = await func(*args, **kwargs)
                span.set_status(trace.StatusCode.OK)
                return result
            except Exception as e:
                span.set_status(trace.StatusCode.ERROR, str(e))
                span.record_exception(e)
                raise
    return wrapper  # type: ignore


def trace_span(name: str, attributes: dict[str, str] | None = None):
    """Context manager for manual tracing."""
    tracer = get_tracer()
    if tracer is None:
        from contextlib import nullcontext
        return nullcontext()
    return tracer.start_as_current_span(name, attributes=attributes or {})
