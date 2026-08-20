"""
DINOForge MCP Server — I18n Stub
=================================
Lightweight internationalisation / locale-detection helpers for the MCP
server surface.

Usage
-----
    from _i18n_stub import get_text, detect_locale, set_locale

    greeting = get_text("greeting", locale="fr")
    locale   = detect_locale()            # from env / Accept-Language
    set_locale("de")                      # force a locale

The translation dictionaries below contain every public-facing string that
the MCP server emits (tool descriptions, error messages, status banners,
notification payloads).  Only the *en* dictionary is populated by default;
other locales are placeholders that callers can extend at runtime via
``register_translations``.
"""

from __future__ import annotations

import os
from typing import Any, Dict, Optional

# ---------------------------------------------------------------------------
# Core data structures
# ---------------------------------------------------------------------------

#: Default locale used when no explicit locale is supplied.
DEFAULT_LOCALE: str = "en"

#: Current session-level locale override (``None`` means "use detect_locale").
_current_locale: Optional[str] = None

# ---------------------------------------------------------------------------
# Translation registry
# ---------------------------------------------------------------------------

_translations: Dict[str, Dict[str, str]] = {
    "en": {
        # --- Tool / resource descriptions ---------------------------------
        "tool.description.run_command": "Execute a shell command and return stdout/stderr.",
        "tool.description.read_file": "Read the contents of a file at the given path.",
        "tool.description.write_file": "Write content to a file, creating it if necessary.",
        "tool.description.list_directory": "List entries in a directory.",
        "tool.description.search_files": "Search for files matching a glob pattern.",
        # --- Error messages ------------------------------------------------
        "error.file_not_found": "File not found: {path}",
        "error.permission_denied": "Permission denied: {path}",
        "error.command_failed": "Command failed with exit code {code}: {cmd}",
        "error.invalid_locale": "Unsupported locale: {locale}",
        "error.mcp_not_ready": "MCP server is not ready to accept requests.",
        # --- Status / informational ----------------------------------------
        "status.ready": "MCP server is ready.",
        "status.shutting_down": "MCP server is shutting down.",
        "status.locale_changed": "Locale changed to {locale}.",
        # --- Notifications -------------------------------------------------
        "notification.task_started": "Task '{task}' started.",
        "notification.task_completed": "Task '{task}' completed successfully.",
        "notification.task_failed": "Task '{task}' failed: {reason}",
        # --- A11y live-region strings --------------------------------------
        "a11y.sr_command_result": "Command result: {summary}",
        "a11y.sr_error": "Error: {message}",
        "a11y.sr_progress": "Progress: {percent}% complete.",
    },
    # Placeholder locales – callers can extend with register_translations().
    "es": {},
    "fr": {},
    "de": {},
    "ja": {},
    "zh-CN": {},
    "pt-BR": {},
    "ar": {},
    "hi": {},
}

# ---------------------------------------------------------------------------
# Public API
# ---------------------------------------------------------------------------

def register_translations(locale: str, mapping: Dict[str, str]) -> None:
    """Merge *mapping* into the translation dictionary for *locale*.

    This lets callers (e.g. plugin authors) contribute additional keys at
    runtime without mutating the built-in dictionaries directly.
    """
    if locale not in _translations:
        _translations[locale] = {}
    _translations[locale].update(mapping)


def set_locale(locale: str) -> None:
    """Force the session locale.

    Raises ``ValueError`` when the locale is not registered.
    """
    global _current_locale
    if locale not in _translations:
        raise ValueError(f"Unsupported locale: {locale}")
    _current_locale = locale


def get_locale() -> str:
    """Return the currently active locale string."""
    return _current_locale or detect_locale()


def detect_locale() -> str:
    """Heuristic locale detection (in priority order):

    1. ``FORGE_LOCALE`` environment variable.
    2. ``DINO_LOCALE`` environment variable.
    3. ``current_locale`` session override.
    4. ``LANG`` / ``LC_ALL`` / ``LANGUAGE`` POSIX env vars.
    5. Fallback to ``DEFAULT_LOCALE`` (``en``).
    """
    # 1-2. Explicit env overrides
    for env_var in ("FORGE_LOCALE", "DINO_LOCALE"):
        val = os.environ.get(env_var, "").strip()
        if val:
            return _normalise(val)

    # 3. Session override
    if _current_locale:
        return _current_locale

    # 4. POSIX variables
    for env_var in ("LANG", "LC_ALL", "LANGUAGE"):
        val = os.environ.get(env_var, "").strip()
        if val:
            return _normalise(val)

    # 5. Fallback
    return DEFAULT_LOCALE


def get_text(key: str, *, locale: Optional[str] = None, **kwargs: Any) -> str:
    """Look up a translation by *key* and interpolate *kwargs*.

    Resolution order for the locale:
      1. Explicit *locale* parameter.
      2. ``get_locale()`` (session / detected).

    If the key is missing in the target locale the *en* fallback is tried.
    If it is still missing the raw key itself is returned so callers never
    get a ``KeyError``.
    """
    target = locale or get_locale()
    dictionary = _translations.get(target, {})

    template = dictionary.get(key) or _translations.get(DEFAULT_LOCALE, {}).get(key, key)

    try:
        return template.format(**kwargs) if kwargs else template
    except KeyError:
        # Missing interpolation variable – return the un-interpolated template.
        return template


def available_locales() -> list[str]:
    """Return sorted list of registered locale codes."""
    return sorted(_translations.keys())


def has_translation(key: str, locale: Optional[str] = None) -> bool:
    """Check whether *key* exists for the given (or current) locale."""
    target = locale or get_locale()
    return key in _translations.get(target, {}) or key in _translations.get(DEFAULT_LOCALE, {})


# ---------------------------------------------------------------------------
# Accept-Language parsing helper (for HTTP-level locale negotiation)
# ---------------------------------------------------------------------------

def parse_accept_language(header: str) -> list[str]:
    """Parse an ``Accept-Language`` header value into an ordered list of
    locale codes (without quality factors).

    Example::

        parse_accept_language("fr-CA,fr;q=0.9,en;q=0.8")
        # → ["fr-CA", "fr", "en"]
    """
    if not header or not header.strip():
        return []

    locales: list[str] = []
    for part in header.split(","):
        part = part.strip()
        if not part:
            continue
        # Strip quality factor (e.g. ";q=0.8")
        locale_part = part.split(";")[0].strip()
        if locale_part:
            locales.append(locale_part)
    return locales


def negotiate_locale(accept_language: str, supported: Optional[list[str]] = None) -> str:
    """Pick the best locale from *accept_language* given *supported* codes.

    Uses simple first-match semantics (highest q wins because we preserve
    header order).
    """
    requested = parse_accept_language(accept_language)
    supported_set = set(supported or available_locales())

    for loc in requested:
        # Exact match
        if loc in supported_set:
            return loc
        # Language-only match (e.g. "fr" matches "fr-CA" – fall through)
        base = loc.split("-")[0]
        for s in supported_set:
            if s == base or s.startswith(base + "-"):
                return s

    return DEFAULT_LOCALE


# ---------------------------------------------------------------------------
# CLI quick-test
# ---------------------------------------------------------------------------

if __name__ == "__main__":
    print("=== I18n Stub — Quick Test ===")
    print(f"Default locale : {DEFAULT_LOCALE}")
    print(f"Detected locale: {detect_locale()}")
    print(f"Available      : {available_locales()}")
    print()
    for loc in ("en", "fr", "de"):
        msg = get_text("status.ready", locale=loc)
        print(f"  [{loc}] status.ready => {msg}")
    print()
    print("Accept-Language parsing:")
    header = "ja, en-US;q=0.9, fr;q=0.7"
    print(f"  Header : {header!r}")
    print(f"  Parsed : {parse_accept_language(header)}")
    print(f"  Best   : {negotiate_locale(header)}")
