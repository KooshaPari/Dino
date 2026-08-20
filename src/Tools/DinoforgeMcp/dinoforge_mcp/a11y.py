"""
DINOForge MCP Server — A11y (Accessibility) Stub
=================================================
Aria attribute helpers, role constants, label generation, and keyboard-
navigation patterns for any web-facing surface of the MCP server (admin
dashboards, terminal UI wrappers, rendered Markdown, etc.).

Usage
-----
    from _a11y_stub import aria, render_role, label_for, keyboard_trap

    attrs = aria("button", label="Save config", pressed=True)
    html  = f'<button {attrs}>Save</button>'

All helpers produce plain strings so they can be embedded in HTML, JSX, or
template literals without pulling in a DOM dependency.
"""

from __future__ import annotations

import html as _html_mod
import re
import uuid
from dataclasses import dataclass, field
from typing import Any, Dict, List, Optional, Sequence


# ═══════════════════════════════════════════════════════════════════════════
#  ARIA role constants
# ═══════════════════════════════════════════════════════════════════════════

class Role:
    """Common ARIA / HTML roles used across the MCP surface."""

    ALERT            = "alert"
    ALERTDIALOG      = "alertdialog"
    APPLICATION      = "application"
    ARTICLE          = "article"
    BANNER           = "banner"
    BUTTON           = "button"
    CELL             = "cell"
    CHECKBOX         = "checkbox"
    COLUMNHEADER     = "columnheader"
    COMBOBOX         = "combobox"
    COMMAND          = "command"
    COMPLEMENTARY    = "complementary"
    COMPOSITE        = "composite"
    CONTENTINFO      = "contentinfo"
    DEFINITION       = "definition"
    DIALOG           = "dialog"
    DIRECTORY        = "directory"
    DOCUMENT         = "document"
    FEED             = "feed"
    FIGURE           = "figure"
    FORM             = "form"
    GRID             = "grid"
    GROUP            = "group"
    HEADING          = "heading"
    IMG              = "img"
    INPUT            = "input"
    LANDMARK         = "landmark"
    LINK             = "link"
    LIST             = "list"
    LISTBOX          = "listbox"
    LISTITEM         = "listitem"
    LOG              = "log"
    MARQUEE          = "marquee"
    MATH             = "math"
    MENU             = "menu"
    MENUBAR          = "menubar"
    MENUITEM         = "menuitem"
    MENUITEMCHECKBOX = "menuitemcheckbox"
    MENUITEMRADIO    = "menuitemradio"
    NAVIGATION       = "navigation"
    NONE             = "none"
    NOTE             = "note"
    OPTION           = "option"
    PRESENTATION     = "presentation"
    PROGRESSBAR      = "progressbar"
    RADIO            = "radio"
    RADIOGROUP       = "radiogroup"
    REGION           = "region"
    ROW              = "row"
    ROWGROUP         = "rowgroup"
    ROWHEADER        = "rowheader"
    SCROLLBAR        = "scrollbar"
    SEARCH           = "search"
    SEARCHBOX        = "searchbox"
    SECTION          = "section"
    SELECT           = "select"
    SEPARATOR        = "separator"
    SLIDER           = "slider"
    SPINBUTTON       = "spinbutton"
    STATUS           = "status"
    STRUCTURE        = "structure"
    SWITCH           = "switch"
    TAB              = "tab"
    TABLE            = "table"
    TABLIST          = "tablist"
    TABPANEL         = "tabpanel"
    TERMINAL         = "terminal"
    TEXTBOX          = "textbox"
    TOOLBAR          = "toolbar"
    TOOLTIP          = "tooltip"
    TREE             = "tree"
    TREEGRID         = "treegrid"
    TREEITEM         = "treeitem"
    WIDGET           = "widget"
    WINDOW           = "window"


# ═══════════════════════════════════════════════════════════════════════════
#  Core ARIA attribute builder
# ═══════════════════════════════════════════════════════════════════════════

def _escape_attr(value: str) -> str:
    """Escape a value for safe insertion into an HTML attribute."""
    return _html_mod.escape(str(value), quote=True)


def aria(
    role: Optional[str] = None,
    *,
    label: Optional[str] = None,
    labelledby: Optional[str] = None,
    describedby: Optional[str] = None,
    controls: Optional[str] = None,
    expanded: Optional[bool] = None,
    pressed: Optional[bool] = None,
    checked: Optional[bool] = None,
    selected: Optional[bool] = None,
    disabled: Optional[bool] = None,
    hidden: Optional[bool] = None,
    live: Optional[str] = None,
    atomic: Optional[bool] = None,
    busy: Optional[bool] = None,
    current: Optional[str] = None,
    haspopup: Optional[str] = None,
    level: Optional[int] = None,
    modal: Optional[bool] = None,
    multiline: Optional[bool] = None,
    multiselectable: Optional[bool] = None,
    orientation: Optional[str] = None,
    placeholder: Optional[str] = None,
    readonly: Optional[bool] = None,
    required: Optional[bool] = None,
    sort: Optional[str] = None,
    valuemin: Optional[float] = None,
    valuemax: Optional[float] = None,
    valuenow: Optional[float] = None,
    valuetext: Optional[str] = None,
    dropeffect: Optional[str] = None,
    grabbed: Optional[bool] = None,
    relevant: Optional[str] = None,
    roledescription: Optional[str] = None,
) -> str:
    """Build an ARIA attribute string suitable for an HTML element.

    Returns something like::

        role="button" aria-label="Save" aria-pressed="true"

    Booleans become ``"true"`` / ``"false"`` (ARIA spec).  ``None`` values
    are omitted.
    """
    pairs: list[str] = []

    if role:
        pairs.append(f'role="{_escape_attr(role)}"')

    # Map keyword arguments → aria-* attribute names
    mapping = {
        "label":           "aria-label",
        "labelledby":      "aria-labelledby",
        "describedby":     "aria-describedby",
        "controls":        "aria-controls",
        "expanded":        "aria-expanded",
        "pressed":         "aria-pressed",
        "checked":         "aria-checked",
        "selected":        "aria-selected",
        "disabled":        "aria-disabled",
        "hidden":          "aria-hidden",
        "live":            "aria-live",
        "atomic":          "aria-atomic",
        "busy":            "aria-busy",
        "current":         "aria-current",
        "haspopup":        "aria-haspopup",
        "level":           "aria-level",
        "modal":           "aria-modal",
        "multiline":       "aria-multiline",
        "multiselectable": "aria-multiselectable",
        "orientation":     "aria-orientation",
        "placeholder":     "aria-placeholder",
        "readonly":        "aria-readonly",
        "required":        "aria-required",
        "sort":            "aria-sort",
        "valuemin":        "aria-valuemin",
        "valuemax":        "aria-valuemax",
        "valuenow":        "aria-valuenow",
        "valuetext":       "aria-valuetext",
        "dropeffect":      "aria-dropeffect",
        "grabbed":         "aria-grabbed",
        "relevant":        "aria-relevant",
        "roledescription": "aria-roledescription",
    }

    locals_map = {k: v for k, v in locals().items() if k in mapping}

    for py_name, attr_name in mapping.items():
        val = locals_map.get(py_name)
        if val is None:
            continue
        if isinstance(val, bool):
            pairs.append(f'{attr_name}="{"true" if val else "false"}"')
        else:
            pairs.append(f'{attr_name}="{_escape_attr(str(val))}"')

    return " ".join(pairs)


# ═══════════════════════════════════════════════════════════════════════════
#  Role rendering shorthand
# ═══════════════════════════════════════════════════════════════════════════

def render_role(tag: str, role: str, content: str, **aria_kwargs: Any) -> str:
    """Return a complete HTML element string with the given role and ARIA attrs.

    Example::

        render_role("div", Role.ALERT, "Server offline", live="assertive")
        # → '<div role="alert" aria-live="assertive">Server offline</div>'
    """
    attrs = aria(role, **aria_kwargs)
    return f"<{tag} {attrs}>{content}</{tag}>"


# ═══════════════════════════════════════════════════════════════════════════
#  Accessible label generation
# ═══════════════════════════════════════════════════════════════════════════

def _slugify(text: str) -> str:
    """Convert arbitrary text to a slug suitable for ``id`` attributes."""
    text = text.lower().strip()
    text = re.sub(r"[^\w\s-]", "", text)
    return re.sub(r"[\s_]+", "-", text)


def make_id(prefix: str = "a11y") -> str:
    """Generate a unique DOM id for labelling / describing."""
    return f"{prefix}-{uuid.uuid4().hex[:8]}"


def label_for(text: str, *, prefix: str = "") -> str:
    """Create a ``for``/``id`` label pair.

    Returns a dict::

        {"label_id": "a11y-abc12345", "label_text": "…", "for_attr": "for='…'"}

    Callers use ``label_id`` as the ``id`` on the target element and
    ``label_text`` as the visible ``<label>`` content.
    """
    label_id = make_id(prefix or _slugify(text)[:12])
    return {
        "label_id":   label_id,
        "label_text": text,
        "for_attr":   f'for="{label_id}"',
    }


def generate_accessible_name(
    *,
    text_content: Optional[str] = None,
    aria_label: Optional[str] = None,
    aria_labelledby: Optional[str] = None,
    title: Optional[str] = None,
    alt: Optional[str] = None,
    placeholder: Optional[str] = None,
) -> str:
    """Resolve the accessible name for a widget following the
    `Accessible Name Computation <https://www.w3.org/TR/accname-1.1/>`_
    algorithm (simplified).

    Returns the first non-empty value found in the standard priority order.
    """
    for candidate in (aria_label, aria_labelledby, text_content, title, alt, placeholder):
        if candidate and candidate.strip():
            return candidate.strip()
    return ""


# ═══════════════════════════════════════════════════════════════════════════
#  Keyboard navigation patterns
# ═══════════════════════════════════════════════════════════════════════════

@dataclass
class KeyboardShortcut:
    """Describes a keyboard shortcut."""
    key: str
    ctrl: bool = False
    shift: bool = False
    alt: bool = False
    meta: bool = False
    description: str = ""

    @property
    def combo(self) -> str:
        parts: list[str] = []
        if self.ctrl:
            parts.append("Ctrl")
        if self.shift:
            parts.append("Shift")
        if self.alt:
            parts.append("Alt")
        if self.meta:
            parts.append("Meta")
        parts.append(self.key.upper())
        return "+".join(parts)

    def to_onkeydown(self, handler: str) -> str:
        """Return an ``onkeydown`` attribute that calls *handler*."""
        mods: list[str] = []
        if self.ctrl:
            mods.append("event.ctrlKey")
        if self.shift:
            mods.append("event.shiftKey")
        if self.alt:
            mods.append("event.altKey")
        if self.meta:
            mods.append("event.metaKey")
        mods.append(f"event.key === '{_escape_attr(self.key)}'")

        condition = " && ".join(mods)
        return f"onkeydown=\"if ({condition}) {{ {handler}; event.preventDefault(); }}\""


# Common shortcuts for MCP surfaces
SHORTCUTS = {
    "save":       KeyboardShortcut("s",  ctrl=True,  description="Save current configuration"),
    "cancel":     KeyboardShortcut("Escape",           description="Cancel / close dialog"),
    "submit":     KeyboardShortcut("Enter",            description="Submit form"),
    "next_tab":   KeyboardShortcut("ArrowRight", alt=True, description="Move to next tab"),
    "prev_tab":   KeyboardShortcut("ArrowLeft",  alt=True, description="Move to previous tab"),
    "toggle_sidebar": KeyboardShortcut("b", ctrl=True, description="Toggle sidebar"),
    "focus_search":   KeyboardShortcut("/", ctrl=True, description="Focus search box"),
    "help":       KeyboardShortcut("?",  shift=True, ctrl=True, description="Open help"),
}


def keyboard_trap(container_id: str, focus_first: str = "first") -> str:
    """Return a JS snippet that traps keyboard focus inside *container_id*.

    Useful for modal dialogs rendered by the MCP server dashboard.
    """
    return (
        f"(function(){{"
        f"const c=document.getElementById('{container_id}');"
        f"if(!c)return;"
        f"c.addEventListener('keydown',function(e){{"
        f"if(e.key!=='Tab')return;"
        f"const f=c.querySelectorAll('a,button,input,textarea,select,[tabindex]:not([tabindex=\"-1\"])');"
        f"if(!f.length)return;"
        f"const first=f[0],last=f[f.length-1];"
        f"if(e.shiftKey&&document.activeElement===first){{e.preventDefault();last.focus();}}"
        f"else if(!e.shiftKey&&document.activeElement===last){{e.preventDefault();first.focus();}}"
        f"}});"
        f"const s=focus_first==='first'?c.querySelector('[tabindex]:not([tabindex=\"-1\"]),a,button,input'):document.getElementById(focus_first);"
        f"if(s)s.focus();"
        f"}})();"
    )


# ═══════════════════════════════════════════════════════════════════════════
#  Live-region helpers (for real-time MCP status updates)
# ═══════════════════════════════════════════════════════════════════════════

def live_region(
    message: str,
    *,
    polite: bool = True,
    atomic: bool = True,
    status_id: Optional[str] = None,
) -> str:
    """Return an ARIA live-region ``<div>`` announcing *message* to screen readers.

    * ``polite=True`` → ``aria-live="polite"`` (waits for user idle).
    * ``polite=False`` → ``aria-live="assertive"`` (interrupts).
    """
    level = "polite" if polite else "assertive"
    rid = status_id or make_id("live")
    return (
        f'<div id="{rid}" role="status" aria-live="{level}" '
        f'aria-atomic="{str(atomic).lower()}" class="sr-only">'
        f'{_html_mod.escape(message)}</div>'
    )


def update_live_region(element_id: str, message: str) -> str:
    """Return a JS snippet that updates the text of an existing live region."""
    escaped = message.replace("\\", "\\\\").replace("'", "\\'")
    return (
        f"document.getElementById('{element_id}').textContent = '{escaped}';"
    )


# ═══════════════════════════════════════════════════════════════════════════
#  Focus management utilities
# ═══════════════════════════════════════════════════════════════════════════

def focus_element(element_id: str) -> str:
    """Return a JS snippet that moves focus to *element_id*."""
    return (
        f"(function(){{"
        f"var e=document.getElementById('{element_id}');"
        f"if(e){{e.focus();e.scrollIntoView({{behavior:'smooth',block:'nearest'}});}}"
        f"}})();"
    )


def skip_link(target_id: str, label: str = "Skip to main content") -> str:
    """Return an HTML skip-link for keyboard-only users."""
    return (
        f'<a href="#{target_id}" class="skip-link sr-only-focusable">'
        f'{_html_mod.escape(label)}</a>'
    )


# ═══════════════════════════════════════════════════════════════════════════
#  Table accessibility helpers
# ═══════════════════════════════════════════════════════════════════════════

def accessible_table(
    headers: Sequence[str],
    rows: Sequence[Sequence[str]],
    *,
    caption: Optional[str] = None,
    sortable: Optional[Sequence[int]] = None,
    table_id: Optional[str] = None,
) -> str:
    """Render an accessible HTML table with ``<thead>`` / ``<tbody>``,
    ``scope`` attributes on ``<th>`` elements, and optional sort indicators.

    *sortable* is a sequence of column indices that support sorting.
    """
    tid = table_id or make_id("table")
    parts: list[str] = []

    parts.append(f'<table id="{tid}" role="table">')
    if caption:
        parts.append(f'  <caption>{_html_mod.escape(caption)}</caption>')

    # thead
    parts.append("  <thead><tr>")
    for idx, hdr in enumerate(headers):
        sort_attr = ""
        if sortable and idx in sortable:
            sort_attr = f' aria-sort="none" tabindex="0" role="columnheader button"'
            sort_attr += f' onclick="sortTable(\'{tid}\',{idx})"'
        else:
            sort_attr = ' scope="col"'
        parts.append(f'    <th{sort_attr}>{_html_mod.escape(hdr)}</th>')
    parts.append("  </tr></thead>")

    # tbody
    parts.append("  <tbody>")
    for row in rows:
        parts.append("    <tr>")
        for cell in row:
            parts.append(f"      <td>{_html_mod.escape(str(cell))}</td>")
        parts.append("    </tr>")
    parts.append("  </tbody>")
    parts.append("</table>")

    return "\n".join(parts)


# ═══════════════════════════════════════════════════════════════════════════
#  Error / status announcement helpers
# ═══════════════════════════════════════════════════════════════════════════

def announce_error(message: str, *, element_id: Optional[str] = None) -> str:
    """Return an assertive live-region announcing an error to screen readers."""
    return live_region(message, polite=False, status_id=element_id)


def announce_success(message: str, *, element_id: Optional[str] = None) -> str:
    """Return a polite live-region announcing a success message."""
    return live_region(message, polite=True, status_id=element_id)


def progress_announce(percent: int, label: str = "Loading") -> str:
    """Return an ARIA ``progressbar`` element with the current percentage."""
    pid = make_id("progress")
    return (
        f'<div id="{pid}" role="progressbar" '
        f'aria-valuenow="{percent}" aria-valuemin="0" aria-valuemax="100" '
        f'aria-label="{_html_mod.escape(label)}">'
        f'{percent}%</div>'
    )


# ═══════════════════════════════════════════════════════════════════════════
#  CLI quick-test
# ═══════════════════════════════════════════════════════════════════════════

if __name__ == "__main__":
    print("=== A11y Stub — Quick Test ===\n")

    # 1. ARIA attribute builder
    print("1. ARIA attribute builder:")
    print(f'   {aria(Role.BUTTON, label="Save config", pressed=True)}')
    print(f'   {aria(Role.DIALOG, modal=True, label="Settings")}')
    print(f'   {aria(Role.TREE, label="File tree", expanded=True, level=1)}')
    print()

    # 2. render_role
    print("2. render_role:")
    print(f'   {render_role("div", Role.ALERT, "Connection lost", live="assertive")}')
    print()

    # 3. Accessible labels
    print("3. Accessible label:")
    lbl = label_for("Email address")
    print(f'   {lbl}')
    print(f'   => <label {lbl["for_attr"]}>{lbl["label_text"]}</label>')
    print()

    # 4. Keyboard shortcuts
    print("4. Keyboard shortcuts:")
    for name, ks in SHORTCUTS.items():
        print(f'   {name:20s} => {ks.combo}  ({ks.description})')
    print()

    # 5. Live region
    print("5. Live region:")
    print(f'   {live_region("MCP server is ready.")}')
    print()

    # 6. Skip link
    print("6. Skip link:")
    print(f'   {skip_link("main-content")}')
    print()

    # 7. Table
    print("7. Accessible table:")
    print(accessible_table(
        ["Tool", "Status", "Latency"],
        [
            ["run_command", "healthy", "12ms"],
            ["read_file",   "healthy",  "3ms"],
            ["write_file",  "degraded", "45ms"],
        ],
        caption="MCP tool health",
    ))
