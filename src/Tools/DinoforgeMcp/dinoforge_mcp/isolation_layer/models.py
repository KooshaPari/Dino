"""
Data models and backend abstraction for the isolation layer.

Contains:
  - Frame: Screenshot frame data dataclass
  - WindowInfo: Window information dataclass
  - IsolationBackend: Abstract base class for isolation backends
"""

import logging
from abc import ABC, abstractmethod
from dataclasses import dataclass
from typing import List, Optional

logger = logging.getLogger(__name__)


# ---------------------------------------------------------------------------
# Data models
# ---------------------------------------------------------------------------

@dataclass
class Frame:
    """Screenshot frame data."""
    data: bytes       # Raw PNG/JPEG bytes
    width: int        # Image width in pixels
    height: int       # Image height in pixels


@dataclass
class WindowInfo:
    """Window information."""
    hwnd: int         # Windows handle (or identifier on other platforms)
    title: str        # Window title
    process_id: int   # Process ID
    visible: bool     # Whether window is visible


# ---------------------------------------------------------------------------
# Backend abstraction
# ---------------------------------------------------------------------------

class IsolationBackend(ABC):
    """Abstract base class for isolation backends."""

    @abstractmethod
    async def capture_window(self, title: str) -> Frame:
        """Capture a screenshot of a window by title."""
        pass

    @abstractmethod
    async def capture_display(self, monitor: int = 0) -> Frame:
        """Capture a screenshot of a display/monitor."""
        pass

    @abstractmethod
    async def inject_key(self, key: str, duration: float = 0.05) -> bool:
        """Inject a keyboard key press."""
        pass

    @abstractmethod
    async def type_text(self, text: str) -> bool:
        """Type text character-by-character."""
        pass

    @abstractmethod
    async def mouse_click(self, x: int, y: int, button: str = "left") -> bool:
        """Click mouse at screen coordinates."""
        pass

    @abstractmethod
    async def mouse_scroll(self, x: int, y: int, delta: int) -> bool:
        """Scroll mouse wheel at screen coordinates."""
        pass

    @abstractmethod
    async def list_windows(self) -> List[WindowInfo]:
        """List all visible windows."""
        pass

    @abstractmethod
    async def focus_window(self, title: str) -> bool:
        """Focus a window by title."""
        pass

    @abstractmethod
    async def launch_process(self, exe: str, args: Optional[List[str]] = None, cwd: Optional[str] = None) -> int:
        """Launch a process and return PID."""
        pass
