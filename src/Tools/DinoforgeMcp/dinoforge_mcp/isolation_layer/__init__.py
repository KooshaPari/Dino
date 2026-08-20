"""
Isolation Layer — Abstraction over playCUA and Win32 for game automation

Provides a unified interface to:
- Screenshot capture (GPU-accelerated, hidden desktop, fallback)
- Input injection (keyboard, mouse, scroll — focus-agnostic)
- Window enumeration and focus management
- Process launch/kill/status
- Image analysis (perceptual diff, hashing)

Architecture:
  3-tier fallback strategy:
    Tier 1: DINOForge Virtual Display Driver (WDDM/IDD) — future, best performance
    Tier 2: playCUA stdio JSON-RPC (bare-cua-native binary) — GPU WGC on Windows
    Tier 3: Win32 CreateDesktop + direct ctypes calls — compatibility fallback

  Each IsolationContext selects a backend; tools transparently use it.

Implementation notes:
  - playCUA binary: C:\Users\koosh\playcua_ci_test\target\release\bare-cua-native.exe
  - JSON-RPC protocol: stdin/stdout NDJSON
  - All methods are async-ready (return dicts compatible with FastMCP)

Backward-compatible re-exports:
  All public symbols are importable from this package directly, e.g.:
    from _iso_modules import Frame, HiddenDesktopBackend, get_isolation_context
"""

from _iso_modules.models import Frame, WindowInfo, IsolationBackend
from _iso_modules.hidden_desktop import HiddenDesktopBackend
from _iso_modules.playcua_client import PlayCUAClient
from _iso_modules.playcua_backend import PlayCUABackend
from _iso_modules.context import (
    IsolationContextManager,
    get_isolation_context,
    set_isolation_context,
)

__all__ = [
    "Frame",
    "WindowInfo",
    "IsolationBackend",
    "HiddenDesktopBackend",
    "PlayCUAClient",
    "PlayCUABackend",
    "IsolationContextManager",
    "get_isolation_context",
    "set_isolation_context",
]
