"""
Shared configuration, paths, and helper functions for the DINOForge MCP modules.
"""
from __future__ import annotations

import asyncio
import base64
import json
import logging
import os
import re
import subprocess
import tempfile
import threading
from pathlib import Path
from typing import Any

from dotenv import load_dotenv
from pydantic import BaseModel, Field

from ..vision import VisualValidator
from ..ai_stack.preferences import DEFAULT_PREFERENCE_ORDER, PREF_ENV_VAR
from ..ai_stack.routing import get_provider_status, route_ai_request

# Rust PyO3 asset pipeline module (optional — graceful fallback if not available)
try:
    import dinoforge_asset_pipeline as _rust_pipeline
    _RUST_AVAILABLE = True
except ImportError:
    _RUST_AVAILABLE = False

load_dotenv()
logging.basicConfig(level=logging.DEBUG if os.getenv("DINOFORGE_MCP_DEBUG") else logging.WARNING)
logger = logging.getLogger("dinoforge_mcp")

# Log polyglot availability on startup
if _RUST_AVAILABLE:
    logger.info("✓ Rust asset pipeline (PyO3) available — using for SIMD-optimized imports/optimization")

# HMR event — set when game reloads to clear cached pack state
_reload_event = threading.Event()

# Global VisualValidator instance (lazy-loaded on first use)
_visual_validator: VisualValidator | None = None

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------

_HERE = Path(__file__).resolve().parent.parent
REPO_ROOT = (_HERE / "../../../../").resolve()

GAME_DIR = Path(os.getenv(
    "DINO_GAME_DIR",
    r"G:\SteamLibrary\steamapps\common\Diplomacy is Not an Option"
))
GAME_EXE = GAME_DIR / "Diplomacy is Not an Option.exe"
BEPINEX_DIR = GAME_DIR / "BepInEx"
DEBUG_LOG = BEPINEX_DIR / "dinoforge_debug.log"
CATALOG_JSON = GAME_DIR / r"Diplomacy is Not an Option_Data\StreamingAssets\aa\catalog.json"

# DINO AppID — steam_appid.txt beside the exe stops DINO from self-relaunching
# through Steam (which kills the BepInEx-injected process: no MODS/F9/F10).
DINO_STEAM_APPID = "1272320"


def _ensure_steam_appid(game_dir: Path) -> None:
    """Ensure steam_appid.txt (AppID, UTF-8 no BOM, no trailing newline) sits beside
    the exe before launch. Steam 'Verify Integrity' can delete it; recreate if missing."""
    appid_file = game_dir / "steam_appid.txt"
    try:
        if appid_file.read_bytes() == DINO_STEAM_APPID.encode("utf-8"):
            return
    except OSError:
        pass
    appid_file.write_bytes(DINO_STEAM_APPID.encode("utf-8"))


GAME_CONTROL_PROJ = REPO_ROOT / "src/Tools/GameControlCli/GameControlCli.csproj"
PACK_COMPILER_PROJ = REPO_ROOT / "src/Tools/PackCompiler/DINOForge.Tools.PackCompiler.csproj"
ASSET_CLI_PROJ = REPO_ROOT / "src/Tools/Cli/DINOForge.Tools.Cli.csproj"
PACKS_DIR = REPO_ROOT / "packs"
DEFAULT_GAME_PIPE_NAME = "dinoforge-game-bridge"

# Dedicated DINOForge Virtual Display Driver (Nefarius/MTT VDD)
_VDD_INDEX_FILE = REPO_ROOT / ".dinoforge_vdd_index"

# Test instance path config file
_TEST_INSTANCE_PATH_FILE = REPO_ROOT / ".dino_test_instance_path"

# ---------------------------------------------------------------------------
# GameControlCli client (thin wrapper — avoids dotnet run cold-start overhead
# by using --no-build; caller should run `dotnet build` once before first use)
# ---------------------------------------------------------------------------

def _pipe_exists(pipe_name: str) -> bool:
    """Return True when the given named pipe is visible on the local machine."""
    if not pipe_name:
        return False

    try:
        return Path(r"\\.\pipe" + f"\\{pipe_name}").exists()
    except Exception:
        return False


def _select_pipe_name(pipe_name: str | None = None, allow_default_fallback: bool = True) -> tuple[str | None, bool]:
    """
    Resolve the pipe name to use for GameControlCli.

    Returns:
        (pipe_name, used_default_fallback)
    """
    preferred = pipe_name or os.getenv("DINOFORGE_PIPE_NAME")
    if not preferred:
        return DEFAULT_GAME_PIPE_NAME, False

    if preferred == DEFAULT_GAME_PIPE_NAME:
        return preferred, False

    if allow_default_fallback and not _pipe_exists(preferred) and _pipe_exists(DEFAULT_GAME_PIPE_NAME):
        logger.warning(
            "Configured pipe '%s' is unavailable; falling back to default pipe '%s'.",
            preferred,
            DEFAULT_GAME_PIPE_NAME,
        )
        return DEFAULT_GAME_PIPE_NAME, True

    return preferred, False


def _run_game_cli(*args: str, timeout: int = 20, json_output: bool = True, pipe_name: str | None = None) -> dict[str, Any]:
    """
    Invoke GameControlCli synchronously and return parsed JSON.

    Args:
        *args: Command and arguments to pass to GameControlCli
        timeout: Command timeout in seconds
        json_output: Whether to parse output as JSON
        pipe_name: Optional named pipe name (defaults to "dinoforge-game-bridge" in GameControlCli)
    """
    cmd = [
        "dotnet", "run",
        "--project", str(GAME_CONTROL_PROJ),
        "--no-build",
        "-c", "Release",
        "--",
    ]

    # Add pipe_name as global option if specified
    pipe_name, _used_default_fallback = _select_pipe_name(pipe_name)
    if pipe_name:
        cmd.extend(["--pipe-name", pipe_name])

    cmd.extend(["--format=json", *args])

    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout, cwd=REPO_ROOT)
        if r.returncode != 0:
            return {"success": False, "error": r.stderr.strip() or r.stdout.strip()}
        if not json_output:
            return {"success": True, "raw": r.stdout.strip()}
        try:
            parsed = json.loads(r.stdout) if r.stdout.strip() else {"success": True}
            if isinstance(parsed, dict):
                raw_error = parsed.get("error")
                if parsed.get("success") is False or raw_error:
                    error_message = raw_error if isinstance(raw_error, str) else json.dumps(raw_error)
                    return {
                        **parsed,
                        "success": False,
                        "error": error_message or "GameControlCli returned a bridge error.",
                    }
            return parsed
        except json.JSONDecodeError:
            return {"success": True, "raw": r.stdout.strip()}
    except subprocess.TimeoutExpired:
        return {"success": False, "error": f"GameControlCli timed out after {timeout}s"}
    except Exception as e:
        return {"success": False, "error": str(e)}


def _run_pack_compiler(*args: str, timeout: int = 60) -> dict[str, Any]:
    """Invoke PackCompiler CLI."""
    cmd = ["dotnet", "run", "--project", str(PACK_COMPILER_PROJ), "--no-build", "--", *args]
    try:
        r = subprocess.run(cmd, capture_output=True, text=True, timeout=timeout, cwd=REPO_ROOT)
        return {"success": r.returncode == 0, "output": r.stdout.strip(), "error": r.stderr.strip()}
    except subprocess.TimeoutExpired:
        return {"success": False, "error": f"PackCompiler timed out after {timeout}s"}
    except Exception as e:
        return {"success": False, "error": str(e)}


# ---------------------------------------------------------------------------
# VDD (Virtual Display Driver) Support
# ---------------------------------------------------------------------------

def _get_vdd_index() -> int | None:
    """Read the dedicated DINOForge VDD monitor index from config file."""
    try:
        return int(_VDD_INDEX_FILE.read_text().strip())
    except Exception:
        return None


def _get_test_instance_path() -> str:
    """
    Read the test instance path from .dino_test_instance_path config file.
    Falls back to default path if file doesn't exist or is invalid.
    Validates that the directory exists.
    """
    default_path = r"G:\SteamLibrary\steamapps\common\Diplomacy is Not an Option_TEST"

    # Try to read from config file
    if _TEST_INSTANCE_PATH_FILE.exists():
        try:
            config_path = _TEST_INSTANCE_PATH_FILE.read_text().strip()
            if config_path:
                path_obj = Path(config_path)
                if path_obj.exists() and path_obj.is_dir():
                    logger.info(f"Using test instance path from {_TEST_INSTANCE_PATH_FILE}: {config_path}")
                    return config_path
                else:
                    logger.warning(f"Config path doesn't exist or is not a directory: {config_path}. Using default.")
        except Exception as e:
            logger.warning(f"Error reading {_TEST_INSTANCE_PATH_FILE}: {e}. Using default.")
    else:
        logger.info(f"Config file not found at {_TEST_INSTANCE_PATH_FILE}. Using default path.")

    return default_path


async def _launch_on_vdd(exe_path: str, width: int = 1920, height: int = 1080) -> dict:
    """Launch game on dedicated DINOForge Virtual Display Driver (not user's Parsec VDD)."""
    idx = _get_vdd_index()
    if idx is None:
        return {"success": False, "error": "DINOForge VDD not configured — run scripts/setup-vdd.ps1"}
    try:
        await asyncio.to_thread(
            subprocess.Popen,
            [exe_path, "-monitor", str(idx), "-screen-width", str(width),
             "-screen-height", str(height), "-popupwindow"],
            cwd=str(Path(exe_path).parent)
        )
        return {"success": True, "message": f"Launched on VDD monitor {idx} ({width}x{height})"}
    except Exception as e:
        return {"success": False, "error": str(e)}


async def _launch_hidden(exe_path: str, desktop_name: str = "DINOForge_Agent") -> dict:
    """Launch game on a hidden Win32 desktop using CreateDesktop with -popupwindow flag."""
    # Write script to a temp file to avoid -Command here-string parsing issues
    # (here-strings via -Command can fail with "Invalid argument" on some PowerShell versions)
    script_path = Path(tempfile.gettempdir()) / f"dinoforge_launch_{os.getpid()}.ps1"
    script_content = f'''\
param($ExePath, $DesktopName)
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
    try:
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
            return {"success": True, "pid": pid, "desktop": desktop_name, "hidden": True}
        return {"success": False, "error": stdout or result.stderr}
    finally:
        try:
            script_path.unlink(missing_ok=True)
        except Exception:
            pass


def _resolve_bepinex_root() -> Path:
    """
    Resolve the BepInEx root directory used by the Runtime HMR watcher.

    Resolution order:
      1. $DINOFORGE_BEPINEX_ROOT (explicit override)
      2. Module-level BEPINEX_DIR (derived from $DINO_GAME_DIR or default game path)

    Raises FileNotFoundError if the resolved directory does not exist, so callers
    can surface a clear error to the MCP client instead of silently no-oping.
    """
    override = os.getenv("DINOFORGE_BEPINEX_ROOT")
    root = Path(override) if override else BEPINEX_DIR
    if not root.is_dir():
        raise FileNotFoundError(
            f"BepInEx root not found at {root}. "
            f"Set DINOFORGE_BEPINEX_ROOT or DINO_GAME_DIR to a valid path."
        )
    return root
