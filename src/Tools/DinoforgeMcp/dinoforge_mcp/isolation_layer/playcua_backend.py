"""
PlayCUA backend — IsolationBackend implementation via JSON-RPC over stdio.

Wraps PlayCUAClient to provide the IsolationBackend interface for
screenshot capture, input injection, window management, and process launch.
"""

import asyncio
import base64
import logging
from typing import List, Optional

from _iso_modules.models import Frame, IsolationBackend, WindowInfo
from _iso_modules.playcua_client import PlayCUAClient

logger = logging.getLogger(__name__)


class PlayCUABackend(IsolationBackend):
    """playCUA JSON-RPC backend via stdio NDJSON."""

    def __init__(self, binary_path: Optional[str] = None) -> None:
        if binary_path is None:
            binary_path = r"C:\Users\koosh\playcua_ci_test\target\release\bare-cua-native.exe"
        self.binary_path = binary_path
        self.client: Optional[PlayCUAClient] = None

    async def _ensure_client(self) -> PlayCUAClient:
        """Ensure client is started."""
        if self.client is None:
            self.client = PlayCUAClient(self.binary_path)
            await self.client.start()
        return self.client

    async def capture_window(self, title: str) -> Frame:
        """Capture window screenshot via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("screenshot", {"window_title": title})

            if "error" in response:
                raise RuntimeError(response["error"].get("message", "Unknown error"))

            result = response.get("result", {})
            data_b64 = result.get("data")
            width = result.get("width", 0)
            height = result.get("height", 0)

            if not data_b64:
                raise RuntimeError("No image data in response")

            data = base64.b64decode(data_b64)
            return Frame(data=data, width=width, height=height)
        except Exception as e:
            logger.error(f"PlayCUA capture_window failed: {e}")
            raise

    async def capture_display(self, monitor: int = 0) -> Frame:
        """Capture display screenshot via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("screenshot", {"monitor": monitor})

            if "error" in response:
                raise RuntimeError(response["error"].get("message", "Unknown error"))

            result = response.get("result", {})
            data_b64 = result.get("data")
            width = result.get("width", 0)
            height = result.get("height", 0)

            if not data_b64:
                raise RuntimeError("No image data in response")

            data = base64.b64decode(data_b64)
            return Frame(data=data, width=width, height=height)
        except Exception as e:
            logger.error(f"PlayCUA capture_display failed: {e}")
            raise

    async def inject_key(self, key: str, duration: float = 0.05) -> bool:
        """Inject key via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("input.key", {"key": key.lower(), "action": "press"})

            if "error" in response:
                logger.error(f"Key injection failed: {response['error']}")
                return False

            return True
        except Exception as e:
            logger.error(f"PlayCUA inject_key failed: {e}")
            return False

    async def type_text(self, text: str) -> bool:
        """Type text via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("input.type", {"text": text})

            if "error" in response:
                logger.error(f"Text typing failed: {response['error']}")
                return False

            return True
        except Exception as e:
            logger.error(f"PlayCUA type_text failed: {e}")
            return False

    async def mouse_click(self, x: int, y: int, button: str = "left") -> bool:
        """Click mouse via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("input.click", {
                "x": x, "y": y, "button": button, "action": "click"
            })

            if "error" in response:
                logger.error(f"Mouse click failed: {response['error']}")
                return False

            return True
        except Exception as e:
            logger.error(f"PlayCUA mouse_click failed: {e}")
            return False

    async def mouse_scroll(self, x: int, y: int, delta: int) -> bool:
        """Scroll mouse via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("input.scroll", {
                "x": x, "y": y, "delta": delta
            })

            if "error" in response:
                logger.error(f"Mouse scroll failed: {response['error']}")
                return False

            return True
        except Exception as e:
            logger.error(f"PlayCUA mouse_scroll failed: {e}")
            return False

    async def list_windows(self) -> List[WindowInfo]:
        """List windows via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("windows.list", {})

            if "error" in response:
                logger.error(f"List windows failed: {response['error']}")
                return []

            result = response.get("result", [])
            windows = []
            for w in result:
                windows.append(WindowInfo(
                    hwnd=w.get("hwnd", 0),
                    title=w.get("title", ""),
                    process_id=w.get("process_id", 0),
                    visible=w.get("visible", True)
                ))
            return windows
        except Exception as e:
            logger.error(f"PlayCUA list_windows failed: {e}")
            return []

    async def focus_window(self, title: str) -> bool:
        """Focus window via playCUA."""
        try:
            client = await self._ensure_client()
            response = await client.call("windows.focus", {"window_title": title})

            if "error" in response:
                logger.error(f"Focus window failed: {response['error']}")
                return False

            return True
        except Exception as e:
            logger.error(f"PlayCUA focus_window failed: {e}")
            return False

    async def launch_process(self, exe: str, args: Optional[List[str]] = None, cwd: Optional[str] = None) -> int:
        """Launch process via playCUA."""
        try:
            client = await self._ensure_client()
            params: dict = {"exe": exe}
            if args:
                params["args"] = args
            if cwd:
                params["cwd"] = cwd

            response = await client.call("process.launch", params)

            if "error" in response:
                raise RuntimeError(response["error"].get("message", "Unknown error"))

            result = response.get("result", {})
            pid = result.get("pid")
            if pid is None:
                raise RuntimeError("No PID in response")

            return pid
        except Exception as e:
            logger.error(f"PlayCUA launch_process failed: {e}")
            raise
