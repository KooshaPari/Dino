"""
playCUA JSON-RPC 2.0 client for bare-cua-native binary.

Spawns the binary as a subprocess and communicates via stdin/stdout NDJSON.
"""

import asyncio
import json
import logging
import subprocess
from typing import Any, Optional

logger = logging.getLogger(__name__)


class PlayCUAClient:
    """
    JSON-RPC 2.0 client for bare-cua-native binary.
    Spawns the binary as a subprocess and communicates via stdin/stdout NDJSON.
    """

    def __init__(self, binary_path: str) -> None:
        self.binary_path = binary_path
        self.process: Optional[subprocess.Popen] = None
        self.pending_responses: dict[int, asyncio.Future] = {}
        self._reader_task: Optional[asyncio.Task] = None
        self._lock = asyncio.Lock()
        self._request_id_counter = 0

    async def start(self) -> None:
        """Start the bare-cua-native binary and reader loop."""
        if self.process is not None:
            return

        logger.info(f"Starting playCUA binary: {self.binary_path}")
        try:
            self.process = await asyncio.to_thread(
                subprocess.Popen,
                [self.binary_path],
                stdin=subprocess.PIPE,
                stdout=subprocess.PIPE,
                stderr=subprocess.PIPE,
                text=True,
                bufsize=1,
            )
        except Exception as e:
            logger.error(f"Failed to start playCUA binary: {e}")
            raise

        self._reader_task = asyncio.create_task(self._read_responses())

    async def stop(self) -> None:
        """Stop the binary and clean up."""
        if self.process is None:
            return

        logger.info("Stopping playCUA binary")
        try:
            self.process.stdin.close()
            self.process.wait(timeout=5)
        except Exception as e:
            logger.warning(f"Error stopping playCUA: {e}")
            if self.process.poll() is None:
                self.process.terminate()
                self.process.wait(timeout=2)

        self.process = None
        if self._reader_task:
            self._reader_task.cancel()
            try:
                await self._reader_task
            except asyncio.CancelledError:
                pass

    async def _read_responses(self) -> None:
        """Background task: read NDJSON responses from stdout."""
        try:
            loop = asyncio.get_event_loop()
            while self.process and self.process.stdout and not self.process.stdout.closed:
                line = await asyncio.to_thread(self.process.stdout.readline)
                if not line:
                    break

                try:
                    response = json.loads(line)
                    request_id = response.get("id")
                    if request_id in self.pending_responses:
                        future = self.pending_responses.pop(request_id)
                        loop.call_soon_threadsafe(future.set_result, response)
                except json.JSONDecodeError as e:
                    logger.error(f"Invalid JSON from playCUA: {line} — {e}")
        except Exception as e:
            logger.error(f"Reader loop error: {e}")

    async def call(self, method: str, params: dict[str, Any]) -> dict[str, Any]:
        """Call a playCUA JSON-RPC method and wait for response."""
        if self.process is None:
            raise RuntimeError("playCUA not running — call await client.start() first")

        async with self._lock:
            self._request_id_counter += 1
            request_id = self._request_id_counter

            request = {
                "jsonrpc": "2.0",
                "id": request_id,
                "method": method,
                "params": params,
            }

            future: asyncio.Future = asyncio.Future()
            self.pending_responses[request_id] = future

            try:
                await asyncio.to_thread(
                    lambda: self.process.stdin.write(json.dumps(request) + "\n")
                )
                await asyncio.to_thread(lambda: self.process.stdin.flush())
            except Exception as e:
                self.pending_responses.pop(request_id, None)
                raise RuntimeError(f"Failed to send playCUA request: {e}")

        try:
            response = await asyncio.wait_for(future, timeout=30.0)
        except asyncio.TimeoutError:
            self.pending_responses.pop(request_id, None)
            raise asyncio.TimeoutError(f"playCUA did not respond to {method} within 30s")

        return response
