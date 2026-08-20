"""
HiddenDesktop backend — Win32 CreateDesktop for hidden game launches.

Uses ctypes for direct Win32 API calls to:
- Capture screenshots (GDI PrintWindow / BitBlt)
- Inject input (SendInput for keyboard and mouse)
- Enumerate and focus windows on the hidden desktop
- Launch processes on the hidden desktop via PowerShell
"""

import asyncio
import ctypes
import logging
import os
import subprocess
import tempfile
from pathlib import Path
from typing import List, Optional

from _iso_modules.models import Frame, IsolationBackend, WindowInfo

logger = logging.getLogger(__name__)


class HiddenDesktopBackend(IsolationBackend):
    """
    Win32 CreateDesktop backend for hidden game launches.
    Uses ctypes for direct Win32 API calls.
    """

    # Hidden desktop name (must match _launch_hidden below).
    HIDDEN_DESKTOP_NAME = "DINOForge_Agent"

    # Cached desktop handle for SetThreadDesktop (P0 fix b).
    # Opened lazily on first SendInput call from the hidden-desktop-scoped thread.
    _hidden_desktop_handle: Optional[int] = None

    def __init__(self) -> None:
        self.game_cli_proj = None  # Will be set during initialization
        self.game_dir = None

    async def capture_window(self, title: str) -> Frame:
        """Capture window screenshot via GDI PrintWindow (P0 fix a).

        Uses the desktop-scoped window-find helper so it can target windows on
        the hidden desktop. Falls back to bare-cua-native subprocess if PrintWindow
        returns an empty buffer (common with hardware-accelerated surfaces).
        """
        try:
            hwnd = await asyncio.to_thread(self._find_window_on_desktop, title)
            if not hwnd:
                logger.warning(f"capture_window: window '{title}' not found on hidden desktop")
                return Frame(data=b"", width=0, height=0)

            frame = await asyncio.to_thread(self._printwindow_capture, hwnd)
            if frame.data:
                return frame

            # Fallback: delegate to bare-cua-native if available
            logger.info("PrintWindow returned empty; delegating to bare-cua-native subprocess")
            return await asyncio.to_thread(self._barecua_capture, title)
        except Exception as e:
            logger.error(f"Capture window failed: {e}")
            raise

    async def capture_display(self, monitor: int = 0) -> Frame:
        """Capture display screenshot via BitBlt of the virtual screen."""
        try:
            return await asyncio.to_thread(self._bitblt_capture_screen, monitor)
        except Exception as e:
            logger.error(f"Capture display failed: {e}")
            raise

    # -----------------------------------------------------------------------
    # GDI capture helpers (P0 fix a)
    # -----------------------------------------------------------------------

    def _printwindow_capture(self, hwnd: int) -> Frame:
        """Capture a single window via GDI PrintWindow into a PNG buffer."""
        try:
            user32 = ctypes.windll.user32
            gdi32 = ctypes.windll.gdi32

            # GetWindowRect
            class RECT(ctypes.Structure):
                _fields_ = [("left", ctypes.c_long), ("top", ctypes.c_long),
                            ("right", ctypes.c_long), ("bottom", ctypes.c_long)]

            rect = RECT()
            if not user32.GetWindowRect(hwnd, ctypes.byref(rect)):
                return Frame(data=b"", width=0, height=0)

            width = rect.right - rect.left
            height = rect.bottom - rect.top
            if width <= 0 or height <= 0:
                return Frame(data=b"", width=0, height=0)

            hdc_window = user32.GetWindowDC(hwnd)
            hdc_mem = gdi32.CreateCompatibleDC(hdc_window)
            hbitmap = gdi32.CreateCompatibleBitmap(hdc_window, width, height)
            gdi32.SelectObject(hdc_mem, hbitmap)

            # PrintWindow flag PW_RENDERFULLCONTENT = 0x00000002 (works for some hw surfaces)
            PW_RENDERFULLCONTENT = 0x00000002
            ok = user32.PrintWindow(hwnd, hdc_mem, PW_RENDERFULLCONTENT)

            # Encode bitmap to PNG bytes via Pillow if available; otherwise fall back to raw BMP.
            data = b""
            try:
                from PIL import Image, ImageWin  # type: ignore
                import io as _io
                # Copy bitmap into a Pillow Image
                img = Image.new("RGB", (width, height))
                # We can't directly grab via ImageWin without ctypes-level pixel copy; use BitBlt to DIB instead.
                BITMAPINFOHEADER = ctypes.c_byte * 40
                bmi = (ctypes.c_byte * 40)()
                # Fill BITMAPINFOHEADER manually
                ctypes.memset(bmi, 0, 40)
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_uint32))[0] = 40  # biSize
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_int32))[1] = width
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_int32))[2] = -height  # top-down
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_uint16))[6] = 1  # biPlanes
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_uint16))[7] = 32  # biBitCount
                # biCompression = BI_RGB = 0 (already zeroed)

                buf_size = width * height * 4
                buf = (ctypes.c_ubyte * buf_size)()
                gdi32.GetDIBits(hdc_mem, hbitmap, 0, height, buf, bmi, 0)

                img = Image.frombuffer("RGBA", (width, height), bytes(buf), "raw", "BGRA", 0, 1).convert("RGB")
                out = _io.BytesIO()
                img.save(out, format="PNG")
                data = out.getvalue()
            except ImportError:
                logger.warning("Pillow not available; capture returns empty Frame")
            except Exception as enc_e:
                logger.warning(f"PrintWindow encode failed: {enc_e}")

            # Cleanup GDI
            gdi32.DeleteObject(hbitmap)
            gdi32.DeleteDC(hdc_mem)
            user32.ReleaseDC(hwnd, hdc_window)

            if not ok:
                logger.warning("PrintWindow returned 0; result may be incomplete")

            return Frame(data=data, width=width, height=height)
        except Exception as e:
            logger.error(f"_printwindow_capture failed: {e}")
            return Frame(data=b"", width=0, height=0)

    def _bitblt_capture_screen(self, monitor: int) -> Frame:
        """Capture full virtual-screen via BitBlt of the desktop DC."""
        try:
            user32 = ctypes.windll.user32
            gdi32 = ctypes.windll.gdi32

            SM_XVIRTUALSCREEN = 76
            SM_YVIRTUALSCREEN = 77
            SM_CXVIRTUALSCREEN = 78
            SM_CYVIRTUALSCREEN = 79

            x = user32.GetSystemMetrics(SM_XVIRTUALSCREEN)
            y = user32.GetSystemMetrics(SM_YVIRTUALSCREEN)
            width = user32.GetSystemMetrics(SM_CXVIRTUALSCREEN)
            height = user32.GetSystemMetrics(SM_CYVIRTUALSCREEN)

            hdc_screen = user32.GetDC(0)
            hdc_mem = gdi32.CreateCompatibleDC(hdc_screen)
            hbitmap = gdi32.CreateCompatibleBitmap(hdc_screen, width, height)
            gdi32.SelectObject(hdc_mem, hbitmap)
            SRCCOPY = 0x00CC0020
            gdi32.BitBlt(hdc_mem, 0, 0, width, height, hdc_screen, x, y, SRCCOPY)

            data = b""
            try:
                from PIL import Image  # type: ignore
                import io as _io
                bmi = (ctypes.c_byte * 40)()
                ctypes.memset(bmi, 0, 40)
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_uint32))[0] = 40
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_int32))[1] = width
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_int32))[2] = -height
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_uint16))[6] = 1
                ctypes.cast(bmi, ctypes.POINTER(ctypes.c_uint16))[7] = 32
                buf_size = width * height * 4
                buf = (ctypes.c_ubyte * buf_size)()
                gdi32.GetDIBits(hdc_mem, hbitmap, 0, height, buf, bmi, 0)
                img = Image.frombuffer("RGBA", (width, height), bytes(buf), "raw", "BGRA", 0, 1).convert("RGB")
                out = _io.BytesIO()
                img.save(out, format="PNG")
                data = out.getvalue()
            except ImportError:
                logger.warning("Pillow not available; display capture returns empty Frame")

            gdi32.DeleteObject(hbitmap)
            gdi32.DeleteDC(hdc_mem)
            user32.ReleaseDC(0, hdc_screen)

            return Frame(data=data, width=width, height=height)
        except Exception as e:
            logger.error(f"_bitblt_capture_screen failed: {e}")
            return Frame(data=b"", width=0, height=0)

    def _barecua_capture(self, title: str) -> Frame:
        """Fallback: delegate to bare-cua-native subprocess for capture."""
        try:
            barecua_path = r"C:\Users\koosh\playcua_ci_test\target\release\bare-cua-native.exe"
            if not Path(barecua_path).exists():
                logger.warning("bare-cua-native binary not present; capture fallback unavailable")
                return Frame(data=b"", width=0, height=0)

            result = subprocess.run(
                [barecua_path, "capture-window", "--title", title, "--format", "png"],
                capture_output=True, timeout=10
            )
            if result.returncode == 0 and result.stdout:
                # Convention: stdout is raw PNG bytes
                return Frame(data=result.stdout, width=0, height=0)
            logger.warning(f"bare-cua-native fallback returned {result.returncode}: {result.stderr[:200]!r}")
            return Frame(data=b"", width=0, height=0)
        except Exception as e:
            logger.error(f"_barecua_capture failed: {e}")
            return Frame(data=b"", width=0, height=0)

    async def inject_key(self, key: str, duration: float = 0.05) -> bool:
        """Inject a keyboard key via Win32 SendInput."""
        try:
            return await self._send_key(key.lower(), duration)
        except Exception as e:
            logger.error(f"Key injection failed: {e}")
            return False

    async def type_text(self, text: str) -> bool:
        """Type text character by character via Win32 SendInput."""
        try:
            for char in text:
                await self._send_char(char)
                await asyncio.sleep(0.05)
            return True
        except Exception as e:
            logger.error(f"Text typing failed: {e}")
            return False

    async def mouse_click(self, x: int, y: int, button: str = "left") -> bool:
        """Click mouse at coordinates via Win32 SendInput."""
        try:
            return await self._send_click(x, y, button)
        except Exception as e:
            logger.error(f"Mouse click failed: {e}")
            return False

    async def mouse_scroll(self, x: int, y: int, delta: int) -> bool:
        """Scroll mouse wheel via Win32 SendInput."""
        try:
            return await self._send_scroll(x, y, delta)
        except Exception as e:
            logger.error(f"Mouse scroll failed: {e}")
            return False

    async def list_windows(self) -> List[WindowInfo]:
        """List windows on the hidden desktop via EnumWindows + GetThreadDesktop filter."""
        try:
            return await asyncio.to_thread(self._enum_hidden_desktop_windows)
        except Exception as e:
            logger.error(f"List windows failed: {e}")
            return []

    def _enum_hidden_desktop_windows(self) -> List[WindowInfo]:
        """Enumerate all top-level windows and filter to those owned by the hidden desktop."""
        try:
            user32 = ctypes.windll.user32
            kernel32 = ctypes.windll.kernel32

            self._ensure_hidden_desktop_attached()
            target_desktop = HiddenDesktopBackend._hidden_desktop_handle

            results: List[WindowInfo] = []
            EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_int, ctypes.c_void_p, ctypes.c_void_p)

            def _cb(hwnd, _lparam) -> int:
                if not user32.IsWindow(hwnd):
                    return 1
                length = user32.GetWindowTextLengthW(hwnd)
                buf = ctypes.create_unicode_buffer(length + 1 if length else 1)
                if length:
                    user32.GetWindowTextW(hwnd, buf, length + 1)
                pid = ctypes.c_uint(0)
                tid = user32.GetWindowThreadProcessId(hwnd, ctypes.byref(pid))
                if tid == 0:
                    return 1

                GetThreadDesktop = user32.GetThreadDesktop
                GetThreadDesktop.restype = ctypes.c_void_p
                GetThreadDesktop.argtypes = [ctypes.c_uint]
                wnd_desktop = GetThreadDesktop(tid)

                if target_desktop is None or wnd_desktop == target_desktop:
                    results.append(WindowInfo(
                        hwnd=int(hwnd),
                        title=buf.value,
                        process_id=int(pid.value),
                        visible=bool(user32.IsWindowVisible(hwnd)),
                    ))
                return 1

            cb = EnumWindowsProc(_cb)
            user32.EnumWindows(cb, 0)
            return results
        except Exception as e:
            logger.error(f"_enum_hidden_desktop_windows failed: {e}")
            return []

    async def focus_window(self, title: str) -> bool:
        """Focus window by title via Win32 SetForegroundWindow."""
        try:
            return await self._focus_window(title)
        except Exception as e:
            logger.error(f"Focus window failed: {e}")
            return False

    async def launch_process(self, exe: str, args: Optional[List[str]] = None, cwd: Optional[str] = None) -> int:
        """Launch process on hidden desktop via Win32 CreateDesktop."""
        try:
            return await self._launch_hidden(exe, args, cwd)
        except Exception as e:
            logger.error(f"Launch process failed: {e}")
            raise

    # -----------------------------------------------------------------------
    # Win32 helpers (extracted from server.py)
    # -----------------------------------------------------------------------

    def _ensure_hidden_desktop_attached(self) -> bool:
        """P0 fix b: Attach calling thread to the hidden desktop so SendInput
        targets the hidden desktop's input queue, not the interactive desktop.

        OpenDesktopW with DESKTOP_SWITCHDESKTOP|DESKTOP_WRITEOBJECTS, then
        SetThreadDesktop. Caches the desktop handle in a class-level field so
        subsequent calls are O(1).

        Returns True on success, False on failure (in which case SendInput will
        still execute but may route to the wrong desktop — the caller logs).
        """
        try:
            user32 = ctypes.windll.user32

            if HiddenDesktopBackend._hidden_desktop_handle is None:
                # DESKTOP_SWITCHDESKTOP = 0x0100, DESKTOP_WRITEOBJECTS = 0x0080,
                # DESKTOP_READOBJECTS = 0x0001, GENERIC_READ|GENERIC_WRITE for safety
                DESKTOP_ALL = 0x000F01FF
                OpenDesktopW = user32.OpenDesktopW
                OpenDesktopW.restype = ctypes.c_void_p
                OpenDesktopW.argtypes = [ctypes.c_wchar_p, ctypes.c_uint,
                                        ctypes.c_int, ctypes.c_uint]

                handle = OpenDesktopW(self.HIDDEN_DESKTOP_NAME, 0, 0, DESKTOP_ALL)
                if not handle:
                    err = ctypes.windll.kernel32.GetLastError()
                    logger.warning(f"OpenDesktopW('{self.HIDDEN_DESKTOP_NAME}') failed (err={err}); "
                                   "input may not reach hidden desktop")
                    return False
                HiddenDesktopBackend._hidden_desktop_handle = handle

            SetThreadDesktop = user32.SetThreadDesktop
            SetThreadDesktop.argtypes = [ctypes.c_void_p]
            SetThreadDesktop.restype = ctypes.c_int
            ok = SetThreadDesktop(HiddenDesktopBackend._hidden_desktop_handle)
            if not ok:
                err = ctypes.windll.kernel32.GetLastError()
                # ERROR_BUSY (170) is common when thread already owns a different desktop;
                # input will still go to the interactive desktop in that case.
                logger.debug(f"SetThreadDesktop returned 0 (err={err}); thread may already be attached elsewhere")
                return False
            return True
        except Exception as e:
            logger.warning(f"_ensure_hidden_desktop_attached failed: {e}")
            return False

    def _find_window_on_desktop(self, title: str) -> int:
        """P0 fix c: Desktop-scoped EnumWindows that filters windows owned by
        threads attached to the hidden desktop.

        FindWindowW only searches the calling thread's current desktop. To find
        windows on the hidden desktop, we enumerate top-level windows and check
        each window's owning thread/desktop via GetThreadDesktop.

        Returns HWND (int) or 0 if not found.
        """
        try:
            user32 = ctypes.windll.user32
            kernel32 = ctypes.windll.kernel32

            # Get the hidden desktop handle so we can compare per-window desktops.
            if HiddenDesktopBackend._hidden_desktop_handle is None:
                self._ensure_hidden_desktop_attached()
            target_desktop = HiddenDesktopBackend._hidden_desktop_handle

            found_hwnd = [0]

            EnumWindowsProc = ctypes.WINFUNCTYPE(ctypes.c_int, ctypes.c_void_p, ctypes.c_void_p)

            def _callback(hwnd, _lparam) -> int:
                if not user32.IsWindow(hwnd):
                    return 1  # continue
                # Get window title
                length = user32.GetWindowTextLengthW(hwnd)
                if length == 0:
                    return 1
                buf = ctypes.create_unicode_buffer(length + 1)
                user32.GetWindowTextW(hwnd, buf, length + 1)
                if title.lower() not in buf.value.lower():
                    return 1

                # Check the thread desktop ownership
                tid = user32.GetWindowThreadProcessId(hwnd, None)
                if tid == 0:
                    return 1

                # Open thread to retrieve its desktop. THREAD_QUERY_INFORMATION = 0x0040.
                OpenThread = kernel32.OpenThread
                OpenThread.restype = ctypes.c_void_p
                OpenThread.argtypes = [ctypes.c_uint, ctypes.c_int, ctypes.c_uint]
                th = OpenThread(0x0040, False, tid)
                if not th:
                    # If we can't query, accept the match as a best-effort
                    found_hwnd[0] = hwnd
                    return 0

                try:
                    GetThreadDesktop = user32.GetThreadDesktop
                    GetThreadDesktop.restype = ctypes.c_void_p
                    GetThreadDesktop.argtypes = [ctypes.c_uint]
                    wnd_desktop = GetThreadDesktop(tid)

                    if target_desktop is None or wnd_desktop == target_desktop:
                        found_hwnd[0] = hwnd
                        return 0  # stop
                finally:
                    kernel32.CloseHandle(th)
                return 1

            cb = EnumWindowsProc(_callback)
            user32.EnumWindows(cb, 0)

            if found_hwnd[0] == 0:
                # Fallback: try plain FindWindowW (interactive desktop) as last resort
                FindWindowW = user32.FindWindowW
                FindWindowW.argtypes = [ctypes.c_wchar_p, ctypes.c_wchar_p]
                FindWindowW.restype = ctypes.c_void_p
                hwnd_fallback = FindWindowW(None, title)
                if hwnd_fallback:
                    return int(hwnd_fallback)
            return int(found_hwnd[0])
        except Exception as e:
            logger.error(f"_find_window_on_desktop failed: {e}")
            return 0

    async def _send_key(self, key: str, duration: float) -> bool:
        """Send key press via Win32 SendInput."""
        try:
            vk_codes = {
                "escape": 0x1B, "esc": 0x1B,
                "space": 0x20, "enter": 0x0D, "return": 0x0D,
                "tab": 0x09, "left": 0x25, "up": 0x26, "right": 0x27, "down": 0x28,
                "f1": 0x70, "f2": 0x71, "f3": 0x72, "f4": 0x73,
                "f5": 0x74, "f6": 0x75, "f7": 0x76, "f8": 0x77,
                "f9": 0x78, "f10": 0x79, "f11": 0x7A, "f12": 0x7B,
            }

            vk = vk_codes.get(key.lower())
            if vk is None:
                logger.warning(f"Unknown key: {key}")
                return False

            # P0 fix b: attach calling thread to hidden desktop before SendInput
            self._ensure_hidden_desktop_attached()

            SendInput = ctypes.windll.user32.SendInput
            KEYEVENTF_KEYUP = 0x0002

            class KEYBDINPUT(ctypes.Structure):
                _fields_ = [("wVk", ctypes.c_ushort), ("wScan", ctypes.c_ushort),
                             ("dwFlags", ctypes.c_uint), ("time", ctypes.c_uint),
                             ("dwExtraInfo", ctypes.c_void_p)]

            class INPUT_UNION(ctypes.Union):
                _fields_ = [("ki", KEYBDINPUT)]

            class INPUT(ctypes.Structure):
                _fields_ = [("type", ctypes.c_uint), ("data", INPUT_UNION)]

            # Key down
            ki_down = KEYBDINPUT(vk, 0, 0, 0, None)
            inp_down = INPUT()
            inp_down.type = 1
            inp_down.data.ki = ki_down

            # Key up
            ki_up = KEYBDINPUT(vk, 0, KEYEVENTF_KEYUP, 0, None)
            inp_up = INPUT()
            inp_up.type = 1
            inp_up.data.ki = ki_up

            arr = (INPUT * 2)(inp_down, inp_up)
            SendInput(2, arr, ctypes.sizeof(INPUT))

            await asyncio.sleep(duration)
            return True
        except Exception as e:
            logger.error(f"_send_key failed: {e}")
            return False

    async def _send_char(self, char: str) -> bool:
        """Send a single character via Win32 (simplified)."""
        # For now, just try to send as key if it's alphanumeric
        if char.isalnum():
            return await self._send_key(char.lower(), 0.05)
        return True

    async def _send_click(self, x: int, y: int, button: str) -> bool:
        """Send mouse click via Win32 SendInput."""
        try:
            # P0 fix b: attach calling thread to hidden desktop before SendInput
            self._ensure_hidden_desktop_attached()

            SendInput = ctypes.windll.user32.SendInput
            MOUSEEVENTF_LEFTDOWN = 0x0002
            MOUSEEVENTF_LEFTUP = 0x0004
            MOUSEEVENTF_MOVE = 0x0001

            class MOUSEINPUT(ctypes.Structure):
                _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long),
                             ("mouseData", ctypes.c_uint), ("dwFlags", ctypes.c_uint),
                             ("time", ctypes.c_uint), ("dwExtraInfo", ctypes.c_void_p)]

            class INPUT_UNION(ctypes.Union):
                _fields_ = [("mi", MOUSEINPUT)]

            class INPUT(ctypes.Structure):
                _fields_ = [("type", ctypes.c_uint), ("data", INPUT_UNION)]

            # Move to coordinates
            mi_move = MOUSEINPUT(x, y, 0, MOUSEEVENTF_MOVE, 0, None)
            inp_move = INPUT()
            inp_move.type = 0
            inp_move.data.mi = mi_move
            SendInput(1, ctypes.byref(inp_move), ctypes.sizeof(INPUT))

            # Click down
            mi_down = MOUSEINPUT(0, 0, 0, MOUSEEVENTF_LEFTDOWN, 0, None)
            inp_down = INPUT()
            inp_down.type = 0
            inp_down.data.mi = mi_down
            SendInput(1, ctypes.byref(inp_down), ctypes.sizeof(INPUT))

            # Click up
            mi_up = MOUSEINPUT(0, 0, 0, MOUSEEVENTF_LEFTUP, 0, None)
            inp_up = INPUT()
            inp_up.type = 0
            inp_up.data.mi = mi_up
            SendInput(1, ctypes.byref(inp_up), ctypes.sizeof(INPUT))

            return True
        except Exception as e:
            logger.error(f"_send_click failed: {e}")
            return False

    async def _send_scroll(self, x: int, y: int, delta: int) -> bool:
        """Send mouse scroll via Win32 SendInput."""
        try:
            # P0 fix b: attach calling thread to hidden desktop before SendInput
            self._ensure_hidden_desktop_attached()

            SendInput = ctypes.windll.user32.SendInput
            MOUSEEVENTF_WHEEL = 0x0800

            class MOUSEINPUT(ctypes.Structure):
                _fields_ = [("dx", ctypes.c_long), ("dy", ctypes.c_long),
                             ("mouseData", ctypes.c_uint), ("dwFlags", ctypes.c_uint),
                             ("time", ctypes.c_uint), ("dwExtraInfo", ctypes.c_void_p)]

            class INPUT_UNION(ctypes.Union):
                _fields_ = [("mi", MOUSEINPUT)]

            class INPUT(ctypes.Structure):
                _fields_ = [("type", ctypes.c_uint), ("data", INPUT_UNION)]

            mi = MOUSEINPUT(x, y, delta, MOUSEEVENTF_WHEEL, 0, None)
            inp = INPUT()
            inp.type = 0
            inp.data.mi = mi
            SendInput(1, ctypes.byref(inp), ctypes.sizeof(INPUT))

            return True
        except Exception as e:
            logger.error(f"_send_scroll failed: {e}")
            return False

    async def _focus_window(self, title: str) -> bool:
        """Focus window by title.

        P0 fix c: Uses desktop-scoped EnumWindows filter (_find_window_on_desktop)
        instead of plain FindWindowW, which only searches the calling thread's
        current desktop and therefore misses windows on the hidden desktop.
        """
        try:
            # Ensure we're attached to the hidden desktop before SetForegroundWindow
            self._ensure_hidden_desktop_attached()

            hwnd = await asyncio.to_thread(self._find_window_on_desktop, title)
            if not hwnd:
                logger.warning(f"Window not found on hidden desktop: {title}")
                return False

            SetForegroundWindow = ctypes.windll.user32.SetForegroundWindow
            SetForegroundWindow.argtypes = [ctypes.c_void_p]
            SetForegroundWindow.restype = ctypes.c_int
            SetForegroundWindow(hwnd)
            return True
        except Exception as e:
            logger.error(f"_focus_window failed: {e}")
            return False

    async def _launch_hidden(self, exe_path: str, args: Optional[List[str]] = None, cwd: Optional[str] = None) -> int:
        """Launch process on hidden Win32 desktop (via PowerShell script)."""
        try:
            desktop_name = "DINOForge_Agent"
            script_path = Path(tempfile.gettempdir()) / f"dinoforge_launch_{os.getpid()}.ps1"

            script_content = f'''\
:param($ExePath, $DesktopName)
Add-Type @"
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
public class Win32Desktop {{
    [DllImport("user32.dll")] public static extern IntPtr CreateDesktop(string lpszDesktop, IntPtr lpszDevice, IntPtr pDevmode, int dwFlags, uint dwDesiredAccess, IntPtr lpsa);
    [DllImport("user32.dll")] public static extern bool CloseDesktop(IntPtr hDesktop);
    [DllImport("kernel32.dll")] public static extern bool CreateProcess(string lpAppName, string lpCmdLine, IntPtr lpPA, IntPtr lpTA, bool bInherit, uint dwCreationFlags, IntPtr lpEnv, string lpCurDir, ref STARTUPINFO lpSI, out PROCESS_INFORMATION lpPI);
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Auto)] public struct STARTUPINFO {{ public int cb; public string lpReserved; public string lpDesktop; public string lpTitle; public int dwX, dwY, dwXSize, dwYSize, dwXCountChars, dwYCountChars, dwFillAttribute, dwFlags; public short wShowWindow, cbReserved2; public IntPtr lpReserved2, hStdInput, hStdOutput, hStdError; }}
    [StructLayout(LayoutKind.Sequential)] public struct PROCESS_INFORMATION {{ public IntPtr hProcess, hThread; public int dwProcessId, dwThreadId; }}
}}
"@
$DESKTOP_ALL = [uint32]0x000F01FF
$CREATE_NO_WINDOW = [uint32]0x08000000
$CREATE_DEFAULT_ERROR_MODE = [uint32]0x04000000
$desktop = [Win32Desktop]::CreateDesktop($DesktopName, [IntPtr]::Zero, [IntPtr]::Zero, 0, $DESKTOP_ALL, [IntPtr]::Zero)
if ($desktop -eq [IntPtr]::Zero) {{ Write-Output "ERROR:CreateDesktop"; exit 1 }}
$si = New-Object Win32Desktop+STARTUPINFO
$si.cb = [System.Runtime.InteropServices.Marshal]::SizeOf($si)
$si.lpDesktop = $DesktopName
$si.dwFlags = 0x00000001
$si.wShowWindow = 0
$pi = New-Object Win32Desktop+PROCESS_INFORMATION
$exeDir = Split-Path $ExePath -Parent
$cmdLine = $ExePath + " -popupwindow"
$creationFlags = $CREATE_NO_WINDOW -bor $CREATE_DEFAULT_ERROR_MODE -bor [uint32]0x00000010
$ok = [Win32Desktop]::CreateProcess($ExePath, $cmdLine, [IntPtr]::Zero, [IntPtr]::Zero, $false, $creationFlags, [IntPtr]::Zero, $exeDir, [ref]$si, [ref]$pi)
if (!$ok) {{ Write-Output "ERROR:CreateProcess"; exit 1 }}
$scriptBlock = {{
    param($desktopHandle, $processId)
    try {{
        $proc = [System.Diagnostics.Process]::GetProcessById($processId)
        $proc.WaitForExit()
    }} finally {{
        [Win32Desktop]::CloseDesktop($desktopHandle) | Out-Null
    }}
}}
Start-Job -ScriptBlock $scriptBlock -ArgumentList $desktop, $pi.dwProcessId | Out-Null
Write-Output "PID:$($pi.dwProcessId)"
'''

            script_path.write_text(script_content, encoding="utf-8-sig")
            result = await asyncio.to_thread(
                subprocess.run,
                ["powershell", "-ExecutionPolicy", "Bypass", "-File", str(script_path),
                 "-ExePath", exe_path, "-DesktopName", desktop_name],
                capture_output=True, text=True, timeout=30
            )

            stdout = result.stdout.strip()
            if stdout.startswith("PID:"):
                pid = int(stdout[4:])
                logger.info(f"Launched hidden process PID={pid}")
                return pid

            logger.error(f"Launch failed: {stdout or result.stderr}")
            raise RuntimeError(stdout or result.stderr)
        except Exception as e:
            logger.error(f"_launch_hidden failed: {e}")
            raise
        finally:
            try:
                script_path.unlink(missing_ok=True)
            except Exception:
                pass
