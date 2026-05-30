# SW-001: Native Mods Page Visible

**Status**: Proposed
**AgilePlus WP State**: planned
**Sequence**: 1
**Date**: 2026-05-28
**Author**: DINOForge Agents
**Epic**: [EPIC-027 — True Full-Conversion Experience](../v0.27.0-full-conversion-epic.md)
**AgilePlus Feature Slug**: epic-027-full-conversion
**Sprint**: 1 — Foundation
**Story Points**: 8
**Priority**: P0 — Sprint blocker
**File Scope**:
  - `src/Runtime/UI/NativeMenuInjector.cs`
  - `src/Runtime/UI/NativeMainMenuModMenu.cs`
  - `src/Runtime/UI/ContextualModMenuHost.cs`
  - `src/Runtime/UI/NativeModsPage.cs`
  - `src/Tests/ModsPageControllerTests.cs`
**Depends On**: []
**Requirements**: EPIC-027-FR-001, EPIC-027-FR-002, EPIC-027-FR-003, EPIC-027-FR-004, EPIC-027-FR-020, EPIC-027-NFR-001, EPIC-027-NFR-004, EPIC-027-NFR-007, EPIC-027-NFR-008, EPIC-027-NFR-009, EPIC-027-NFR-015, EPIC-027-NFR-016, EPIC-027-NFR-017, EPIC-027-NFR-018

---

## User Story

As a **mod player**, I want clicking the "Mods" button in DINO's main menu to open a visible,
native-styled mods page — not a blank overlay — so that I can see my installed packs, their
status, and toggle them without memorizing the F10 hotkey.

## Background

The MODS button (injected by `NativeMenuInjector`, confirmed working in iter-146 commit
`cafd2b70`) currently opens `ModMenuPanel` — a floating DFCanvas overlay. Two problems
documented in `SPEC-fix-mods-button-ux.md`:

1. The overlay is visually distinct from DINO's native settings-page style (jarring context switch).
2. Under certain pack states the panel renders blank (labels missing, scroll area empty).

This story makes the Mods page feel like a first-class DINO screen by fixing both issues.
It is a Sprint 1 blocker because the Mods page is the primary mod-management surface for all
v0.27.0 UX goals.

## Acceptance Criteria

### Scenario 1 — Mods button opens a styled, populated page

**Given** `warfare-starwars` and `warfare-modern` packs are installed and `dinoforge status`
reports both Active,
**When** the player clicks the "Mods" button in DINO's main menu,
**Then** a mods page opens that:
- fills the same screen region as DINO's native Settings/Options page (not a floating overlay),
- lists every loaded pack with display name, version, and active/inactive status badge,
- matches the active total-conversion palette or DINOForge default theme,
- is navigable by mouse click.

### Scenario 2 — No blank state under any pack configuration

**Given** any combination of packs (including zero packs beyond the built-in default),
**When** the Mods page opens,
**Then** the page is never blank — it shows at minimum the DINOForge header and a
"No additional packs loaded" message when no user packs are present.

### Scenario 3 — Page closes cleanly and returns input to menu

**Given** the Mods page is open,
**When** the player clicks "Back" or presses Escape,
**Then** the Mods page closes and all native main menu buttons are clickable
(no input swallowed, no EventSystem focus lost).

### Scenario 4 — Hover effects on the Mods button match native buttons

**Given** the Mods button is visible in the main menu,
**When** the player moves the cursor over the Mods button,
**Then** the hover visual effect matches adjacent native DINO buttons
(confirmed against live `dinoforge ui-tree` dump — do not hardcode without verifying).

## Functional Requirements

| ID | Requirement |
|----|-------------|
| F-01 | Mods page renders within 500 ms of button click on a 60 FPS host. |
| F-02 | Pack list entries include: display name, version, type badge, active/disabled badge. |
| F-03 | Page layout uses `ResolvedTheme` palette (primary / secondary / text colors). |
| F-04 | Page is scrollable when pack count exceeds visible area. |
| F-05 | Mods button hover state must not block raycasts to sibling buttons (Pattern #235). |
| F-06 | Mods page does not duplicate entries if `NativeMenuInjector` re-scans while page is open. |

## Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| N-01 | All UGUI construction and canvas walks execute on the Unity main thread. |
| N-02 | Page `GameObject` named `DINOForge_ModsPage` so `CanvasWalker` skips it. |
| N-03 | Page is destroyed (not hidden) on close to avoid memory accumulation. |
| N-04 | No Harmony patches on DINO's UI systems — `EventTrigger` / `Button.onClick` only. |

## Engine Quirks / Dependencies

- `MonoBehaviour.Update()` never fires — page construction is one-shot inside the button
  click handler (main thread, `EventTrigger.PointerClick`).
- Confirm DINO's Settings page `RectTransform` anchor via `dinoforge ui-tree` before sizing
  the Mods page.
- `NativeMenuInjector` hover fix (`SPEC-fix-mods-button-ux.md`) should land in the same
  sprint to avoid regressing iter-146 behavior.
- Graceful fallback to DINOForge default palette if ThemeEngine (SW-006) not yet active.
- Pattern #235: any transparent Image over buttons with `raycastTarget = true` kills all
  button clicks silently — guard every injected element.

## Definition of Done

- [ ] Clicking Mods button opens `DINOForge_ModsPage` (no blank state under any pack config).
- [ ] Pack list shows all loaded packs with correct name, version, and status.
- [ ] Hover effects match native DINO buttons (external screenshot proof).
- [ ] Closing page returns full input control to main menu.
- [ ] `dotnet test` green — unit tests for `ModsPageController` construction + pack entry population.
- [ ] In-game screenshot with Mods page open deposited in `docs/proof/`
  + external judge receipt in `docs/proof/judge-receipts/`.

## Evidence Requirements

| Requirement ID | Evidence Type | Artifact Path Pattern | Transition Gate |
|----------------|---------------|-----------------------|-----------------|
| EPIC-027-FR-001 | ManualAttestation | `docs/proof/judge-receipts/SW-001-mods-page.md` (clicking "Mods" opens `DINOForge_ModsPage`; screenshot shows full-region page, not blank overlay) | Implementing → Validated |
| EPIC-027-FR-002 | ManualAttestation | Mods page lists both packs with name+version+status; zero-pack "No additional packs loaded" message visible (screenshot per scenario) | Implementing → Validated |
| EPIC-027-FR-003 | ManualAttestation | After close, all native buttons are clickable; re-scan while open produces no duplicate rows (log confirmation + screenshot) | Implementing → Validated |
| EPIC-027-FR-004 | ManualAttestation | F10 toggles Mods surface; F9 toggles Debug overlay; verified via Win32 background-thread key path (in-game screenshot) | Implementing → Validated |
| EPIC-027-FR-020 | ManualAttestation | Toggling a pack updates its active badge; `dinoforge status` reflects the toggle after relaunch (log + status output) | Implementing → Validated |
| EPIC-027-NFR-001 | ManualAttestation | Timed Mods page open ≤ 500 ms on 60 FPS host (F9 overlay timestamp or log timing) | Implementing → Validated |
| EPIC-027-NFR-004 | TestResult | Open/close cycles show no monotonic GameObject/memory growth in snapshot (docs/test-results/SW-001/MemorySnapshot.txt) | Implementing → Validated |
| EPIC-027-NFR-007 | ReviewApproval | No `[HarmonyPatch` attribute targets DINO UI type in SW-001 scope (grep in NativeMenuInjector + NativeModsPage) | Implementing → Validated |
| EPIC-027-NFR-008 | ReviewApproval | Page GameObject named `DINOForge_ModsPage`; no unnamed injected objects (grep `new GameObject` in NativeModsPage.BuildUI) | Implementing → Validated |
| EPIC-027-NFR-009 | ReviewApproval | No `Process.Start` / URL-open path consumes unvalidated pack data (grep in NativeModsPage + ContextualModMenuHost) | Implementing → Validated |
| EPIC-027-NFR-015 | ReviewApproval | Mods button injection has `raycastTarget = false` on any overlay Image; EventSystem guard present (Pattern #235) | Implementing → Validated |
| EPIC-027-NFR-016 | ManualAttestation | Mods page hover/layout matches adjacent native buttons (verified against live `dinoforge ui-tree` dump) | Implementing → Validated |
| EPIC-027-NFR-017 | ManualAttestation | Escape closes Mods page; keyboard navigation moves focus across entries (in-game confirmation) | Implementing → Validated |
| EPIC-027-NFR-018 | CiOutput | New UI strings resolve through locale layer; non-English locale shows translated labels (i18n CI check) | Implementing → Validated |
| SW-001 | TestResult | `docs/test-results/SW-001/ModsPageControllerTests.xml` (page construction + pack-entry population + no-blank-state path) | Implementing → Validated |
| SW-001 | ReviewApproval | PR URL (auto-detected from WorkPackage.pr_url) | Validated → Shipped |
| SW-001 | CiOutput | GitHub Actions run URL (dotnet test green) | Implementing → Validated |

## Related

- `SPEC-002-native-menu-injector.md`
- `SPEC-fix-mods-button-ux.md`
- `docs/design/main-menu-takeover.md §1.2`
- Pattern #235 (GraphicRaycaster without EventSystem guard)
- SW-006 (ThemeEngine — palette provider)
