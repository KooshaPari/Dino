"""Unity-specific accessibility helpers for DINOForge mod UI."""
from __future__ import annotations
from typing import Any

SCREEN_READER_MESSAGES = {
    "pack_loaded": "Pack {name} loaded with {count} units",
    "pack_error": "Error loading pack {name}: {error}",
    "menu_opened": "{menu} menu opened",
    "menu_closed": "{menu} menu closed",
    "setting_changed": "{setting} changed from {old} to {new}",
    "hotkey_pressed": "{key} hotkey pressed: {action}",
    "screenshot_taken": "Screenshot saved to {path}",
    "game_paused": "Game paused",
    "game_resumed": "Game resumed",
}

def get_screen_reader_message(key, **kwargs):
    template = SCREEN_READER_MESSAGES.get(key, key)
    try:
        return template.format(**kwargs)
    except (KeyError, IndexError):
        return template

def generate_aria_label(element_type, **attrs):
    parts = []
    if "text" in attrs: parts.append(str(attrs["text"]))
    if "state" in attrs: parts.append(f"state: {attrs['state']}")
    if "count" in attrs: parts.append(f"{attrs['count']} items")
    if "hotkey" in attrs: parts.append(f"press {attrs['hotkey']} to activate")
    return ", ".join(parts) if parts else element_type

def keyboard_shortcuts():
    return {
        "F8": "Toggle debug overlay",
        "F9": "Toggle debug panel",
        "F10": "Toggle mods panel",
        "F11": "Toggle session recorder",
        "Escape": "Close current panel",
        "Tab": "Cycle through UI elements",
        "Enter": "Activate selected element",
        "Space": "Toggle checkbox/slider",
        "ArrowUp/Down": "Navigate list",
        "Ctrl+S": "Save settings",
    }
