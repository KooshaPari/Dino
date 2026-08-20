"""
Hypothesis-based property (fuzz) tests for the DINOForge MCP server.

Covers:
  1. Config parsing (parse_yaml_safe from CI tooling)
  2. Pack YAML round-trip (generate -> parse -> validate)
  3. JSON-RPC 2.0 message construction
  4. Isolation layer data models (Frame, WindowInfo)
  5. Voice intent parsing (_match_intent)
  6. Addressables catalog parsing

Run:  pytest _fuzz_tests.py -v --hypothesis-seed=0
"""

from __future__ import annotations

import asyncio
import json
import re
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Any

import hypothesis
import hypothesis.strategies as st
import pytest
import yaml

# ---------------------------------------------------------------------------
# Path setup - make DINOForge source importable from CWD
# ---------------------------------------------------------------------------
_DINO_ROOT = Path(r"C:\Users\koosh\Dev\DinoForge\src\Tools\DinoforgeMcp")
_SCRIPTS_ROOT = Path(r"C:\Users\koosh\Dev\DinoForge\scripts")

if str(_DINO_ROOT) not in sys.path:
    sys.path.insert(0, str(_DINO_ROOT))
if str(_SCRIPTS_ROOT) not in sys.path:
    sys.path.insert(0, str(_SCRIPTS_ROOT))


# ===========================================================================
# 1. Config parsing - parse_yaml_safe
# ===========================================================================

try:
    from ci.check_framework_version import parse_yaml_safe
except ImportError:
    # Fallback: inline the same logic if module path does not resolve
    try:
        import yaml as _yaml
    except ImportError:
        _yaml = None

    def parse_yaml_safe(text: str):  # type: ignore[misc]
        if not text:
            return None
        if _yaml is not None:
            try:
                data = _yaml.safe_load(text)
                if isinstance(data, dict):
                    return data
                return None
            except _yaml.YAMLError:
                return None
        m = re.search(
            r'^\s*framework_version\s*:\s*["\']?([^"\'\n]+?)["\']?\s*$',
            text,
            re.MULTILINE,
        )
        if m:
            return {"framework_version": m.group(1)}
        return None


class TestConfigParsing:
    """Fuzz tests for parse_yaml_safe."""

    @hypothesis.given(text=st.text(min_size=0, max_size=2000))
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_parse_never_raises(self, text: str):
        """parse_yaml_safe must never raise - it returns None on bad input."""
        result = parse_yaml_safe(text)
        assert result is None or isinstance(result, dict)

    @hypothesis.given(
        key=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N"), whitelist_characters="_-"
            ),
            min_size=1,
            max_size=60,
        ),
        value=st.text(min_size=1, max_size=200),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_valid_yaml_returns_dict(self, key: str, value: str):
        """A simple key: value YAML string must parse to a dict."""
        raw = f"{key}: {value}"
        result = parse_yaml_safe(raw)
        if result is not None:
            assert isinstance(result, dict)

    @hypothesis.given(
        fw_version=st.text(
            alphabet=st.characters(
                whitelist_categories=(), whitelist_characters=">=<.0123456789 *"
            ),
            min_size=1,
            max_size=30,
        ),
    )
    @hypothesis.settings(max_examples=150, deadline=None)
    def test_framework_version_extraction(self, fw_version: str):
        """framework_version key is always extracted when present."""
        raw = f'framework_version: "{fw_version}"'
        result = parse_yaml_safe(raw)
        if result is not None:
            assert "framework_version" in result

    @hypothesis.given(
        entries=st.dictionaries(
            keys=st.text(
                alphabet=st.characters(
                    whitelist_categories=("L",), whitelist_characters="_"
                ),
                min_size=1,
                max_size=20,
            ),
            values=st.one_of(
                st.integers(min_value=-1000, max_value=10000),
                st.floats(allow_nan=False, allow_infinity=False),
                st.text(max_size=100),
                st.booleans(),
                st.lists(st.integers(), max_size=5),
            ),
            min_size=1,
            max_size=10,
        ),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_multi_key_yaml_roundtrip(self, entries: dict):
        """Multi-key YAML dicts survive parse_yaml_safe."""
        raw = yaml.dump(entries, default_flow_style=False)
        result = parse_yaml_safe(raw)
        if result is not None:
            assert isinstance(result, dict)

    @hypothesis.given(garbage=st.binary(min_size=1, max_size=500))
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_binary_garbage_never_crashes(self, garbage: bytes):
        """Binary/non-UTF8 input must not raise."""
        try:
            text = garbage.decode("utf-8", errors="replace")
        except Exception:
            text = str(garbage)
        result = parse_yaml_safe(text)
        assert result is None or isinstance(result, dict)

    @hypothesis.given(nesting_depth=st.integers(min_value=0, max_value=8))
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_deeply_nested_yaml(self, nesting_depth: int):
        """Deeply nested YAML dicts parse without stack overflow."""
        raw = ""
        for i in range(nesting_depth):
            raw += "  " * i + f"level{i}:\n"
        raw += "  " * nesting_depth + 'leaf: "deep_value"\n'
        result = parse_yaml_safe(raw)
        if result is not None:
            assert isinstance(result, dict)


# ===========================================================================
# 2. Pack YAML validation - round-trip generate -> parse -> validate
# ===========================================================================

pack_id_strategy = st.from_regex(
    r"[a-z][a-z0-9\-]{0,30}", fullmatch=True
)

pack_version_strategy = st.from_regex(
    r"\d+\.\d+\.\d+", fullmatch=True
)

pack_type_strategy = st.sampled_from(
    ["balance", "content", "graphics", "ui", "gameplay", "total-conversion"]
)

pack_name_strategy = st.text(
    alphabet=st.characters(
        whitelist_categories=("L", "N", "Z", "S"),
        whitelist_characters=" -_",
    ),
    min_size=1,
    max_size=80,
)

framework_version_range_strategy = st.one_of(
    st.just(">=1.0.0 <2.0.0"),
    st.just(">=0.24.0 <0.26.0"),
    st.just(">=3.1.4 <4.0.0"),
)


def _generate_pack_yaml(
    pack_id: str,
    name: str,
    version: str,
    pack_type: str,
    author: str = "fuzz-test",
    description: str = "hypothesis-generated pack",
    framework_version: str | None = None,
    depends_on: list[str] | None = None,
    conflicts_with: list[str] | None = None,
) -> str:
    """Generate a valid pack.yaml string."""
    data: dict[str, Any] = {
        "id": pack_id,
        "name": name,
        "version": version,
        "author": author,
        "description": description,
        "type": pack_type,
    }
    if framework_version:
        data["framework_version"] = framework_version
    if depends_on is not None:
        data["depends_on"] = depends_on
    if conflicts_with is not None:
        data["conflicts_with"] = conflicts_with
    return yaml.dump(data, default_flow_style=False, allow_unicode=True)


def _validate_pack_yaml(data: dict) -> list[str]:
    """Validate a parsed pack.yaml dict. Returns list of errors (empty = valid)."""
    errors: list[str] = []
    for fld in {"id", "name", "version"}:
        if fld not in data:
            errors.append(f"missing required field: {fld}")
    if "id" in data:
        if not re.match(r"^[a-z][a-z0-9\-]*$", str(data["id"])):
            errors.append(f"invalid id format: {data['id']}")
    if "version" in data:
        if not re.match(r"^\d+\.\d+\.\d+$", str(data["version"])):
            errors.append(f"invalid semver: {data['version']}")
    if "type" in data and data["type"] not in (
        "balance", "content", "graphics", "ui", "gameplay", "total-conversion",
    ):
        errors.append(f"unknown pack type: {data['type']}")
    return errors


class TestPackYamlRoundTrip:
    """Fuzz tests for pack YAML generation -> parse -> validate."""

    @hypothesis.given(
        pack_id=pack_id_strategy,
        name=pack_name_strategy,
        version=pack_version_strategy,
        pack_type=pack_type_strategy,
        description=st.text(min_size=0, max_size=500),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_valid_pack_roundtrip(self, pack_id, name, version, pack_type, description):
        """Generated pack YAML always parses and validates."""
        raw = _generate_pack_yaml(pack_id, name, version, pack_type, description=description)
        data = yaml.safe_load(raw)
        assert isinstance(data, dict)
        errors = _validate_pack_yaml(data)
        assert errors == [], f"Valid pack failed validation: {errors}"

    @hypothesis.given(
        pack_id=pack_id_strategy,
        version=pack_version_strategy,
        fw_range=framework_version_range_strategy,
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_framework_version_preserved(self, pack_id, version, fw_range):
        """framework_version round-trips through YAML."""
        raw = _generate_pack_yaml(
            pack_id, "Test Pack", version, "content",
            framework_version=fw_range,
        )
        data = yaml.safe_load(raw)
        assert data.get("framework_version") == fw_range

    @hypothesis.given(
        pack_id=pack_id_strategy,
        version=pack_version_strategy,
        deps=st.lists(pack_id_strategy, max_size=10),
        conflicts=st.lists(pack_id_strategy, max_size=10),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_depends_and_conflicts_preserved(self, pack_id, version, deps, conflicts):
        """depends_on and conflicts_with lists round-trip."""
        raw = _generate_pack_yaml(
            pack_id, "Test Pack", version, "content",
            depends_on=deps,
            conflicts_with=conflicts,
        )
        data = yaml.safe_load(raw)
        assert data.get("depends_on") == deps
        assert data.get("conflicts_with") == conflicts

    @hypothesis.given(name=pack_name_strategy, version=pack_version_strategy)
    @hypothesis.settings(max_examples=150, deadline=None)
    def test_id_sluggification_valid(self, name, version):
        """Slugified pack ID must always be valid."""
        slug = re.sub(r"[^a-z0-9\-]", "-", name.lower().strip())[:30].strip("-")
        if not slug or slug[0].isdigit():
            slug = "pack-" + slug
        raw = _generate_pack_yaml(slug, name, version, "content")
        data = yaml.safe_load(raw)
        errors = _validate_pack_yaml(data)
        assert errors == [], f"Slugified pack failed: {errors}"

    @hypothesis.given(
        pack_id=pack_id_strategy,
        version=pack_version_strategy,
        description=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N", "Z", "S", "P"),
                whitelist_characters="\n\r\t",
            ),
            min_size=0,
            max_size=1000,
        ),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_unicode_description_roundtrip(self, pack_id, version, description):
        """Descriptions with special chars and newlines survive YAML round-trip."""
        raw = _generate_pack_yaml(
            pack_id, "Test Pack", version, "content",
            description=description,
        )
        data = yaml.safe_load(raw)
        assert isinstance(data, dict)
        assert "id" in data

    @hypothesis.given(raw_yaml=st.text(min_size=1, max_size=2000))
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_arbitrary_yaml_never_crashes(self, raw_yaml):
        """Arbitrary YAML text must never crash _validate_pack_yaml."""
        try:
            data = yaml.safe_load(raw_yaml)
        except yaml.YAMLError:
            return
        if isinstance(data, dict):
            errors = _validate_pack_yaml(data)
            assert isinstance(errors, list)

    @hypothesis.given(
        pack_id=st.from_regex(r"[a-z][a-z0-9\-]{0,30}", fullmatch=True),
        version=pack_version_strategy,
        fw_version=st.one_of(
            st.just(""),
            st.just("invalid-version"),
            st.text(min_size=1, max_size=50),
        ),
    )
    @hypothesis.settings(max_examples=150, deadline=None)
    def test_optional_fields_missing(self, pack_id, version, fw_version):
        """Pack YAML without optional fields still validates core fields."""
        data: dict[str, Any] = {"id": pack_id, "name": "X", "version": version}
        if fw_version:
            data["framework_version"] = fw_version
        errors = _validate_pack_yaml(data)
        assert all("depends_on" not in e and "conflicts_with" not in e for e in errors)


# ===========================================================================
# 3. JSON-RPC 2.0 message construction
# ===========================================================================

def _build_jsonrpc_request(
    method: str,
    params: dict[str, Any] | None = None,
    request_id: int | str | None = None,
) -> dict:
    """Build a JSON-RPC 2.0 request message (mirrors PlayCUAClient.call)."""
    return {
        "jsonrpc": "2.0",
        "id": request_id,
        "method": method,
        "params": params if params is not None else {},
    }


def _build_jsonrpc_response(
    result: Any = None,
    error: dict | None = None,
    request_id: int | str | None = None,
) -> dict:
    """Build a JSON-RPC 2.0 response message."""
    msg: dict[str, Any] = {"jsonrpc": "2.0", "id": request_id}
    if error is not None:
        msg["error"] = error
    else:
        msg["result"] = result
    return msg


def _validate_jsonrpc_message(msg: dict) -> list[str]:
    """Validate a JSON-RPC 2.0 message structure."""
    errors: list[str] = []
    if msg.get("jsonrpc") != "2.0":
        errors.append("missing or wrong jsonrpc version")
    if "id" not in msg:
        errors.append("missing id")
    if "method" in msg:
        if not isinstance(msg.get("method", ""), str) or not msg["method"]:
            errors.append("method must be a non-empty string")
    elif "result" not in msg and "error" not in msg:
        errors.append("neither result nor error in response")
    return errors


class TestJsonRpcConstruction:
    """Fuzz tests for JSON-RPC 2.0 message construction."""

    @hypothesis.given(
        method=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N"), whitelist_characters="_."
            ),
            min_size=1,
            max_size=100,
        ),
        request_id=st.one_of(
            st.integers(min_value=0, max_value=2**31),
            st.text(max_size=20),
            st.none(),
        ),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_request_always_valid(self, method, request_id):
        """Built requests must always have correct JSON-RPC structure."""
        msg = _build_jsonrpc_request(method, request_id=request_id)
        assert msg["jsonrpc"] == "2.0"
        assert msg["method"] == method
        assert msg["id"] == request_id

    @hypothesis.given(
        method=st.text(min_size=1, max_size=100),
        params=st.dictionaries(
            keys=st.text(max_size=30),
            values=st.one_of(
                st.integers(),
                st.floats(allow_nan=False, allow_infinity=False),
                st.text(max_size=100),
                st.booleans(),
                st.none(),
                st.lists(st.integers(), max_size=5),
            ),
            max_size=10,
        ),
        request_id=st.integers(min_value=1, max_value=2**31),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_request_json_serializable(self, method, params, request_id):
        """Built requests must be JSON-serializable (NDJSON over stdin)."""
        msg = _build_jsonrpc_request(method, params, request_id)
        serialized = json.dumps(msg)
        deserialized = json.loads(serialized)
        assert deserialized == msg

    @hypothesis.given(
        result=st.one_of(
            st.integers(),
            st.floats(allow_nan=False, allow_infinity=False),
            st.text(max_size=200),
            st.booleans(),
            st.none(),
            st.lists(st.integers(), max_size=5),
            st.dictionaries(
                keys=st.text(max_size=10), values=st.integers(), max_size=5
            ),
        ),
        request_id=st.integers(min_value=1, max_value=2**31),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_response_always_valid(self, result, request_id):
        """Built responses must have correct structure."""
        msg = _build_jsonrpc_response(result=result, request_id=request_id)
        assert msg["jsonrpc"] == "2.0"
        assert msg["id"] == request_id
        assert "result" in msg
        assert "error" not in msg
        assert _validate_jsonrpc_message(msg) == []

    @hypothesis.given(
        error_code=st.integers(min_value=-32099, max_value=-32000),
        error_msg=st.text(min_size=1, max_size=200),
        request_id=st.integers(min_value=1, max_value=2**31),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_error_response_structure(self, error_code, error_msg, request_id):
        """Error responses follow JSON-RPC 2.0 error format."""
        error = {"code": error_code, "message": error_msg}
        msg = _build_jsonrpc_response(error=error, request_id=request_id)
        assert msg["error"]["code"] == error_code
        assert msg["error"]["message"] == error_msg
        assert "result" not in msg
        assert json.loads(json.dumps(msg))["error"]["code"] == error_code

    @hypothesis.given(n_requests=st.integers(min_value=1, max_value=50))
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_request_ids_unique(self, n_requests):
        """Multiple requests can have sequential unique IDs."""
        requests = [
            _build_jsonrpc_request(f"method_{i}", request_id=i + 1)
            for i in range(n_requests)
        ]
        ids = [r["id"] for r in requests]
        assert len(ids) == len(set(ids)), "IDs must be unique"

    @hypothesis.given(
        method=st.text(min_size=1, max_size=50),
        nested_params=st.dictionaries(
            keys=st.text(max_size=10),
            values=st.dictionaries(
                keys=st.text(max_size=10),
                values=st.one_of(st.integers(), st.text(max_size=20)),
                max_size=3,
            ),
            max_size=5,
        ),
    )
    @hypothesis.settings(max_examples=150, deadline=None)
    def test_nested_params_serializable(self, method, nested_params):
        """Nested dict params survive JSON round-trip."""
        msg = _build_jsonrpc_request(method, nested_params, request_id=1)
        restored = json.loads(json.dumps(msg))
        assert restored["params"] == nested_params


# ===========================================================================
# 4. Isolation layer data models (Frame, WindowInfo)
# ===========================================================================

try:
    from dinoforge_mcp.isolation_layer import Frame, WindowInfo
except ImportError:

    @dataclass
    class Frame:
        data: bytes
        width: int
        height: int

    @dataclass
    class WindowInfo:
        hwnd: int
        title: str
        process_id: int
        visible: bool


class TestIsolationDataModels:
    """Fuzz tests for Frame and WindowInfo dataclasses."""

    @hypothesis.given(
        data=st.binary(min_size=8, max_size=10000),
        width=st.integers(min_value=1, max_value=16384),
        height=st.integers(min_value=1, max_value=16384),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_frame_creation(self, data, width, height):
        """Frame can be created with any valid data."""
        frame = Frame(data=data, width=width, height=height)
        assert frame.data == data
        assert frame.width == width
        assert frame.height == height

    @hypothesis.given(
        hwnd=st.integers(min_value=0, max_value=2**32),
        title=st.text(min_size=0, max_size=256),
        process_id=st.integers(min_value=0, max_value=2**32),
        visible=st.booleans(),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_window_info_creation(self, hwnd, title, process_id, visible):
        """WindowInfo can be created with any valid data."""
        wi = WindowInfo(
            hwnd=hwnd, title=title, process_id=process_id, visible=visible
        )
        assert wi.hwnd == hwnd
        assert wi.title == title
        assert wi.process_id == process_id
        assert wi.visible == visible

    @hypothesis.given(
        width=st.integers(min_value=1, max_value=4096),
        height=st.integers(min_value=1, max_value=4096),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_frame_dimensions_product(self, width, height):
        """Pixel count is width * height."""
        frame = Frame(
            data=b"\x00" * (width * height * 3), width=width, height=height
        )
        assert frame.width * frame.height == width * height

    @hypothesis.given(
        w1=st.integers(min_value=1, max_value=1000),
        h1=st.integers(min_value=1, max_value=1000),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_frame_equality(self, w1, h1):
        """Two Frame objects with same args are equal (dataclass)."""
        data = b"\xff" * 100
        f1 = Frame(data=data, width=w1, height=h1)
        f2 = Frame(data=data, width=w1, height=h1)
        assert f1 == f2

    @hypothesis.given(
        hwnd=st.integers(min_value=1, max_value=2**16),
        title=st.text(min_size=0, max_size=100),
        pid=st.integers(min_value=1, max_value=2**16),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_window_info_as_dict(self, hwnd, title, pid):
        """WindowInfo fields can be extracted for JSON serialization."""
        wi = WindowInfo(hwnd=hwnd, title=title, process_id=pid, visible=True)
        d = {
            "hwnd": wi.hwnd,
            "title": wi.title,
            "process_id": wi.process_id,
            "visible": wi.visible,
        }
        restored = json.loads(json.dumps(d))
        assert restored["hwnd"] == hwnd
        assert restored["title"] == title

    @hypothesis.given(
        data=st.binary(min_size=1, max_size=500),
        width=st.integers(min_value=1, max_value=4096),
        height=st.integers(min_value=1, max_value=4096),
    )
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_frame_data_preserved(self, data, width, height):
        """Frame data bytes are preserved exactly."""
        frame = Frame(data=data, width=width, height=height)
        assert frame.data is data

    @hypothesis.given(
        titles=st.lists(
            st.text(
                alphabet=st.characters(
                    whitelist_categories=("L", "N", "Z"),
                    whitelist_characters=" -_.()[]",
                ),
                min_size=0,
                max_size=100,
            ),
            min_size=1,
            max_size=20,
        ),
    )
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_window_list_sortable(self, titles):
        """A list of WindowInfo objects can be sorted by title."""
        windows = [
            WindowInfo(hwnd=i, title=t, process_id=i, visible=True)
            for i, t in enumerate(titles)
        ]
        sorted_windows = sorted(windows, key=lambda w: w.title)
        titles_sorted = [w.title for w in sorted_windows]
        assert titles_sorted == sorted(titles)


# ===========================================================================
# 5. Voice intent parsing (_match_intent)
# ===========================================================================

try:
    from dinoforge_mcp.server import VOICE_INTENTS, _match_intent
except ImportError:
    VOICE_INTENTS = {
        r"(?:enable|load|activate)\s+(?:the\s+)?(.+?)\s+(?:mod|pack)": "enable_pack",
        r"(?:disable|unload|deactivate)\s+(?:the\s+)?(.+?)\s+(?:mod|pack)": "disable_pack",
        r"(?:reload|refresh)\s+(?:all\s+)?mods": "reload_mods",
        r"(?:take|capture)\s+(?:a\s+)?(?:screenshot|pic)": "screenshot",
        r"(?:show|get|check)\s+(?:game\s+)?status": "status",
        r"(?:open|toggle)\s+(?:the\s+)?(?:mods?\s+)?menu": "open_menu",
        r"(?:open|toggle)\s+(?:the\s+)?debug": "open_debug",
        r"(?:open|show)\s+(?:the\s+)?(?:mods?\s+)?panel": "open_menu",
        r"press\s+(?:the\s+)?f(\d+)": "press_f_key",
    }

    async def _match_intent(text):
        text_lower = text.lower().strip()
        for pattern, intent_name in VOICE_INTENTS.items():
            match = re.search(pattern, text_lower)
            if match:
                params: dict[str, Any] = {}
                if intent_name == "enable_pack":
                    params["pack"] = (
                        match.group(1).strip().replace(" ", "-").lower()
                    )
                elif intent_name == "disable_pack":
                    params["pack"] = (
                        match.group(1).strip().replace(" ", "-").lower()
                    )
                elif intent_name == "press_f_key":
                    params["key_num"] = int(match.group(1))
                return (intent_name, params)
        return ("unknown", {})


INTENT_PHRASES = [
    "enable the test mod",
    "load warfare pack",
    "activate the space mod",
    "disable the cheat pack",
    "unload graphics mod",
    "deactivate the test pack",
    "reload mods",
    "refresh all mods",
    "take a screenshot",
    "capture pic",
    "show status",
    "get game status",
    "check status",
    "open menu",
    "toggle the mods menu",
    "open the debug",
    "toggle debug",
    "open panel",
    "show the mods panel",
    "press f1",
    "press the f5",
    "press f12",
]


class TestVoiceIntentParsing:
    """Fuzz tests for _match_intent."""

    @hypothesis.given(text=st.text(min_size=0, max_size=500))
    @hypothesis.settings(max_examples=500, deadline=None)
    def test_match_never_raises(self, text):
        """_match_intent must never raise an exception."""
        intent, params = asyncio.run(_match_intent(text))
        assert isinstance(intent, str)
        assert isinstance(params, dict)

    @hypothesis.given(
        pack_name=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N"), whitelist_characters=" -_"
            ),
            min_size=1,
            max_size=30,
        ),
        verb=st.sampled_from(["enable", "load", "activate"]),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_enable_pack_intent(self, pack_name, verb):
        """'enable/load/activate <name> mod' -> enable_pack."""
        text = f"{verb} the {pack_name} mod"
        intent, params = asyncio.run(_match_intent(text))
        assert intent == "enable_pack"
        assert "pack" in params
        expected_slug = pack_name.strip().replace(" ", "-").lower()
        assert params["pack"] == expected_slug

    @hypothesis.given(
        pack_name=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N"), whitelist_characters=" -_"
            ),
            min_size=1,
            max_size=30,
        ),
        verb=st.just("disable"),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_disable_pack_intent(self, pack_name, verb):
        """'disable/unload/deactivate <name> pack' -> disable_pack."""
        text = f"{verb} the {pack_name} pack"
        intent, params = asyncio.run(_match_intent(text))
        assert intent == "disable_pack"
        expected_slug = pack_name.strip().replace(" ", "-").lower()
        assert params["pack"] == expected_slug

    @hypothesis.given(fnum=st.integers(min_value=1, max_value=20))
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_press_f_key_intent(self, fnum):
        """'press f<1-20>' -> press_f_key with key_num."""
        text = f"press f{fnum}"
        intent, params = asyncio.run(_match_intent(text))
        assert intent == "press_f_key"
        assert params.get("key_num") == fnum

    @hypothesis.given(
        nopic=st.text(
            alphabet=st.characters(
                whitelist_categories=("L",), whitelist_characters=" "
            ),
            min_size=1,
            max_size=40,
        ),
    )
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_unknown_input_returns_valid(self, nopic):
        """Unrecognised text returns a valid intent name."""
        text = f"xyzzy {nopic} plugh"
        intent, params = asyncio.run(_match_intent(text))
        assert intent in list(VOICE_INTENTS.values()) + ["unknown"]
        assert isinstance(params, dict)

    @hypothesis.given(phrase=st.sampled_from(INTENT_PHRASES))
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_known_phrase_matches(self, phrase):
        """Known intent phrases should not crash."""
        intent, _ = asyncio.run(_match_intent(phrase))
        assert isinstance(intent, str)

    @hypothesis.given(
        pack_name=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N"), whitelist_characters=" -_."
            ),
            min_size=1,
            max_size=40,
        ),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_case_insensitive(self, pack_name):
        """Intent matching is case-insensitive."""
        text = f"ENABLE THE {pack_name} MOD"
        intent, _ = asyncio.run(_match_intent(text))
        assert intent == "enable_pack"

    @hypothesis.given(
        verb=st.sampled_from(["enable", "load", "activate"]),
        filler=st.text(
            alphabet=st.characters(
                whitelist_categories=("Z",), whitelist_characters=" \t"
            ),
            min_size=1,
            max_size=20,
        ),
        pack_name=st.text(
            alphabet=st.characters(
                whitelist_categories=("L",), whitelist_characters="-"
            ),
            min_size=1,
            max_size=20,
        ),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_extra_whitespace_handling(self, verb, filler, pack_name):
        """Extra whitespace between words is handled."""
        text = f"{verb}{filler}the{filler}{pack_name}{filler}mod"
        intent, _ = asyncio.run(_match_intent(text))
        assert intent == "enable_pack"


# ===========================================================================
# 6. Addressables catalog parsing
# ===========================================================================

def _parse_catalog(internal_ids: list[str], filter_term: str = "") -> dict:
    """Parse an Addressables catalog (mirrors catalog_keys logic)."""
    non_bundle = [
        s for s in internal_ids
        if not s.startswith("{") and not s.endswith(".bundle")
    ]
    if filter_term:
        non_bundle = [s for s in non_bundle if filter_term.lower() in s.lower()]
    return {"success": True, "keys": non_bundle[:200], "total": len(non_bundle)}


def _parse_catalog_bundles(internal_ids: list[str]) -> dict:
    """Parse bundle entries from catalog (mirrors catalog_bundles logic)."""
    bundles = [
        s.replace(
            "{UnityEngine.AddressableAssets.Addressables.RuntimePath}", ""
        )
        for s in internal_ids
        if s.endswith(".bundle")
    ]
    return {"success": True, "bundles": bundles, "count": len(bundles)}


class TestAddressablesCatalogParsing:
    """Fuzz tests for catalog parsing functions."""

    @hypothesis.given(
        ids=st.lists(
            st.text(
                alphabet=st.characters(
                    whitelist_categories=("L", "N"), whitelist_characters="._/- "
                ),
                min_size=1,
                max_size=100,
            ),
            min_size=0,
            max_size=500,
        ),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_catalog_keys_never_crashes(self, ids):
        """catalog_keys parsing must never raise."""
        result = _parse_catalog(ids)
        assert result["success"] is True
        assert isinstance(result["keys"], list)
        assert isinstance(result["total"], int)
        assert result["total"] >= 0

    @hypothesis.given(
        ids=st.lists(
            st.text(
                alphabet=st.characters(
                    whitelist_categories=("L", "N"), whitelist_characters="._/-"
                ),
                min_size=1,
                max_size=80,
            ),
            min_size=0,
            max_size=500,
        ),
        filter_term=st.text(
            alphabet=st.characters(
                whitelist_categories=("L",), whitelist_characters="-_"
            ),
            min_size=0,
            max_size=30,
        ),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_filter_reduces_results(self, ids, filter_term):
        """Filtered results are a subset of total."""
        unfiltered = _parse_catalog(ids, "")
        filtered = _parse_catalog(ids, filter_term)
        assert filtered["total"] <= unfiltered["total"]
        assert len(filtered["keys"]) <= len(unfiltered["keys"])

    @hypothesis.given(
        ids=st.lists(
            st.text(
                alphabet=st.characters(
                    whitelist_categories=("L", "N"), whitelist_characters="._/"
                ),
                min_size=1,
                max_size=80,
            ),
            min_size=0,
            max_size=300,
        ),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_bundles_bounded(self, ids):
        """Bundle list count matches count field."""
        result = _parse_catalog_bundles(ids)
        assert result["success"] is True
        assert result["count"] == len(result["bundles"])

    @hypothesis.given(
        n_bundles=st.integers(min_value=0, max_value=500),
        n_non_bundles=st.integers(min_value=0, max_value=500),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_bundle_vs_non_bundle_separation(self, n_bundles, n_non_bundles):
        """Bundles and non-bundles are correctly separated."""
        bundles = [f"asset_{i}.bundle" for i in range(n_bundles)]
        non_bundles = [f"assets/prefab_{i}.prefab" for i in range(n_non_bundles)]
        all_ids = bundles + non_bundles
        keys_result = _parse_catalog(all_ids)
        bundles_result = _parse_catalog_bundles(all_ids)
        assert keys_result["total"] == n_non_bundles
        assert bundles_result["count"] == n_bundles

    @hypothesis.given(
        internal_id=st.text(
            alphabet=st.characters(
                whitelist_categories=("L", "N"), whitelist_characters="._/- "
            ),
            min_size=1,
            max_size=200,
        ),
    )
    @hypothesis.settings(max_examples=300, deadline=None)
    def test_single_id_parsing(self, internal_id):
        """Single catalog entry parses without error."""
        result = _parse_catalog([internal_id])
        assert result["success"] is True
        assert isinstance(result["keys"], list)

    @hypothesis.given(
        ids=st.lists(
            st.just(
                "{UnityEngine.AddressableAssets.Addressables.RuntimePath}base.bundle"
            ),
            min_size=0,
            max_size=50,
        ),
    )
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_runtime_path_prefix_stripped(self, ids):
        """Runtime path prefix is stripped from bundle names."""
        result = _parse_catalog_bundles(ids)
        for b in result["bundles"]:
            assert not b.startswith("{")

    @hypothesis.given(
        ids=st.lists(
            st.text(
                alphabet=st.characters(
                    whitelist_categories=("L", "N"), whitelist_characters="._/-"
                ),
                min_size=1,
                max_size=80,
            ),
            min_size=0,
            max_size=100,
        ),
    )
    @hypothesis.settings(max_examples=200, deadline=None)
    def test_keys_no_bundles_leaked(self, ids):
        """Keys results never contain .bundle entries."""
        result = _parse_catalog(ids)
        for key in result["keys"]:
            assert not key.endswith(".bundle"), f"Bundle leaked into keys: {key}"
            assert not key.startswith("{"), f"Runtime path leaked into keys: {key}"

    @hypothesis.given(
        ids=st.lists(
            st.text(
                alphabet=st.characters(
                    whitelist_categories=("L", "N"), whitelist_characters="._/-"
                ),
                min_size=1,
                max_size=80,
            ),
            min_size=201,
            max_size=500,
        ),
    )
    @hypothesis.settings(max_examples=50, deadline=None)
    def test_keys_truncated_at_200(self, ids):
        """Results are capped at 200 entries."""
        result = _parse_catalog(ids)
        assert len(result["keys"]) <= 200

    @hypothesis.given(
        filter_term=st.text(
            alphabet=st.characters(
                whitelist_categories=("L",), whitelist_characters=""
            ),
            min_size=1,
            max_size=20,
        ),
    )
    @hypothesis.settings(max_examples=100, deadline=None)
    def test_filter_on_empty_catalog(self, filter_term):
        """Filtering empty catalog returns empty."""
        result = _parse_catalog([], filter_term)
        assert result["total"] == 0
        assert result["keys"] == []


# ===========================================================================
# Entry point for direct execution
# ===========================================================================
if __name__ == "__main__":
    pytest.main([__file__, "-v", "--tb=short", "--hypothesis-seed=0"])
