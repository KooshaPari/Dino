"""
Isolation context management — singleton with auto-detection.

Provides:
  - IsolationContextManager: Singleton that selects and caches the active backend
  - get_isolation_context(): Module-level convenience function
  - set_isolation_context(): Module-level setter for manual override
"""

import logging
import threading
from typing import Optional

from _iso_modules.models import IsolationBackend
from _iso_modules.hidden_desktop import HiddenDesktopBackend
from _iso_modules.playcua_backend import PlayCUABackend

logger = logging.getLogger(__name__)


class IsolationContextManager:
    """Singleton context manager for isolation backends."""

    def __init__(self) -> None:
        self._backend: Optional[IsolationBackend] = None
        self._lock = threading.Lock()

    def get(self, backend_name: str = "auto") -> IsolationBackend:
        """
        Get or create isolation backend.

        Args:
            backend_name: "auto" (try playCUA, fallback to HiddenDesktop),
                         "playcua", or "hidden_desktop"

        Returns:
            IsolationBackend instance
        """
        if backend_name == "auto":
            return self._auto_select()
        elif backend_name == "playcua":
            return PlayCUABackend()
        elif backend_name == "hidden_desktop":
            return HiddenDesktopBackend()
        else:
            logger.warning(f"Unknown backend: {backend_name}, using auto-detection")
            return self._auto_select()

    def _auto_select(self) -> IsolationBackend:
        """Auto-detect: try playCUA, fallback to HiddenDesktop."""
        with self._lock:
            if self._backend is None:
                # Try playCUA first
                try:
                    backend = PlayCUABackend()
                    logger.info("Using PlayCUABackend (auto-detected)")
                    self._backend = backend
                except Exception as e:
                    logger.warning(f"playCUA not available: {e}, falling back to HiddenDesktop")
                    backend = HiddenDesktopBackend()
                    logger.info("Using HiddenDesktopBackend (fallback)")
                    self._backend = backend
            return self._backend


# Global singleton instance
_isolation_context_manager = IsolationContextManager()


def get_isolation_context(backend: str = "auto") -> IsolationBackend:
    """Get isolation backend (singleton with auto-detection)."""
    return _isolation_context_manager.get(backend)


def set_isolation_context(backend: IsolationBackend) -> None:
    """Manually set isolation backend."""
    with _isolation_context_manager._lock:
        _isolation_context_manager._backend = backend
