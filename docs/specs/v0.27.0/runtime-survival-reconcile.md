# SW-014: Runtime Survival & v0.27.0 Reconcile

**Status**: Shipped
**AgilePlus WP State**: done
**Sequence**: 14
**Date**: 2026-05-30
**Author**: DINOForge Agents
**Epic**: [EPIC-027 — True Full-Conversion Experience](../v0.27.0-full-conversion-epic.md)
**AgilePlus Feature Slug**: epic-027-full-conversion
**Sprint**: 1 — Foundation (survival hardening) + cross-sprint reconcile
**Story Points**: 13
**Priority**: P0 — Release blocker (without process survival, nothing downstream is observable)
**File Scope**:
  - `scripts/game/write-dump.ps1`
  - `src/Tools/Cli/Commands/DeployCommand.cs`
  - `src/Runtime/UI/NativeMainMenuModMenu.cs`
  - `src/Runtime/UI/NativeMenuInjector.cs`
  - `src/Domains/Warfare/Aviation/`
  - `src/Runtime/UpdateCheck/UpdateChecker.cs`
  - `packs/warfare-modern/pack.yaml`
  - `packs/warfare-naval/`
**Depends On**: [SW-001, SW-003, SW-011]
**Requirements**: EPIC-027-FR-021, EPIC-027-FR-022, EPIC-027-FR-023, EPIC-027-NFR-002, EPIC-027-NFR-005, EPIC-027-NFR-006, EPIC-027-NFR-013, EPIC-027-NFR-023, EPIC-027-NFR-024, EPIC-027-NFR-025

---

## User Story

As a **mod developer**, I want the injected DINOForge process to survive DINO's startup
transitions and the runtime to load without codegen/serialization failures, so that every
other EPIC-027 feature (Mods page, reskin, aerial content) is actually reachable and
observable in-game instead of dying silently before the first frame.

## Background

Six iterations (iter-144 through iter-149) chased a "no BepInEx / no MODS button / dead
F9-F10" symptom that looked like a dormant plugin or a native wedge. The definitive root
cause (`project_dino_steam_selfrelaunch_root_cause.md`, iter-149) is that DINO **self-relaunches
via Steam** when launched directly without `steam_appid.txt`, killing the injected process.
The fix is to auto-provision `steam_appid.txt` = `1272320` beside the exe on every deploy/launch
(Steam Verify deletes it, so it must be re-created on deploy).

Once the process survived, two more runtime load-failures surfaced:
- Aviation/aerial ECS systems used `Entities.ForEach`, which depends on Burst/Roslyn codegen
  that is unavailable under BepInEx Mono — raising TypeLoadException at load.
- The update-check path called an instance `JsonConvert.SerializeObject` overload that does
  not exist on the BepInEx-bundled Newtonsoft.Json (MethodNotFound).

This story also reconciles the shipped roster and UX deltas: the native Mods button now opens
a **quick MODS panel** by default (with Browse-all escalation), and the pack roster was cut to
a deterministic focus set (non-focus packs `.disabled`, `warfare-naval` added).

## Acceptance Criteria

### Scenario 1 — Process survives launch (steam_appid)

**Given** the game is deployed and launched directly (not via the Steam client),
**When** DINO completes its startup transition,
**Then** `steam_appid.txt` (=`1272320`) exists beside the exe, the process is NOT replaced by a
Steam-spawned child, and BepInEx + the MODS button + F9/F10 are all live.

### Scenario 2 — Runtime loads with no codegen/serialization failure

**Given** the runtime DLL deployed under BepInEx Mono,
**When** the plugin loads and the aerial domain + update-check run,
**Then** `LogOutput.log` contains no `TypeLoadException` and no `MethodNotFound` for
`JsonConvert.SerializeObject`; aerial systems iterate via manual `EntityQuery` loops.

### Scenario 3 — Quick MODS panel is the default click

**Given** the native Mods button is visible,
**When** the player clicks it,
**Then** a native-styled quick MODS panel opens listing installed packs, with a visible
"Browse all" affordance that opens the full mod browser.

### Scenario 4 — Deterministic roster

**Given** the shipped pack set,
**When** the runtime selects a total-conversion pack,
**Then** exactly one TC pack is chosen deterministically, non-focus packs are present as
`.disabled`, and `warfare-naval` is loadable.

## Functional Requirements

| ID | Requirement |
|----|-------------|
| F-01 | Deploy/launch step writes `steam_appid.txt`=`1272320` beside the exe if absent. |
| F-02 | Quick MODS panel renders as default Mods-button action; Browse-all opens full browser. |
| F-03 | Deterministic single-TC selection; non-focus packs shipped `.disabled`. |

## Non-Functional Requirements

| ID | Requirement |
|----|-------------|
| N-01 | Aerial ECS systems use manual `EntityQuery` loops, not `Entities.ForEach`. |
| N-02 | Update-check uses static `JsonConvert.SerializeObject` overload. |
| N-03 | No `TypeLoadException` / `MethodNotFound` in `LogOutput.log` after clean launch. |

## Engine Quirks / Dependencies

- DINO self-relaunches via Steam without `steam_appid.txt`; check process parentage across
  startup transitions, not just `MainWindowTitle` (iter-149 lesson).
- `Entities.ForEach` requires source-generators absent under BepInEx Mono — manual
  `EntityManager` / `EntityQuery` iteration only (CLAUDE.md ECS facts).
- BepInEx ships its own Newtonsoft.Json; prefer the static serialization overloads.

## Definition of Done

- [x] `steam_appid.txt` auto-provisioned; injected process survives launch (live proof).
- [x] Aviation systems converted to manual `EntityQuery` loops; no TypeLoadException.
- [x] Update-check Newtonsoft MethodNotFound fixed (static overload).
- [x] Quick MODS panel is the default Mods-button click; Browse-all escalates.
- [x] Roster cut to deterministic focus set; `warfare-naval` added.
- [x] CHANGELOG.md updated for v0.27.0.

## Evidence Requirements

| Requirement ID | Evidence Type | Artifact Path Pattern | Transition Gate |
|----------------|---------------|-----------------------|-----------------|
| EPIC-027-FR-021 | ManualAttestation | `docs/screenshots/mods-button-FIXED-steamappid-20260529.png` + `docs/sessions/dino-steam-selfrelaunch-fix-20260529.md` (process survives; BepInEx/MODS/F9-F10 live) | Implementing -> Validated |
| EPIC-027-FR-022 | ManualAttestation | `docs/screenshots/fixbranch-ab8c6074-live.png` (quick MODS panel opens on default click; Browse-all visible) | Implementing -> Validated |
| EPIC-027-FR-023 | ReviewApproval | `packs/*.disabled/` present + single deterministic TC selection + `packs/warfare-naval/` (commit d77040d2 / PackCut #4) | Implementing -> Validated |
| EPIC-027-NFR-023 | ReviewApproval | Aviation systems use manual `EntityQuery` loops, not `Entities.ForEach` (commit 7a9dea82) | Implementing -> Validated |
| EPIC-027-NFR-024 | ReviewApproval | Update-check uses static `JsonConvert.SerializeObject` (commit 72da648d) | Implementing -> Validated |
| EPIC-027-NFR-025 | TestResult | `convert_real_models.py` output; >=14/36 SW bundles wire real meshes (commit 884908b2 / RealMesh #5) | Implementing -> Validated |
| EPIC-027-NFR-013 | ManualAttestation | `LogOutput.log` shows no TypeLoadException after clean launch (iter-149 session log) | Implementing -> Validated |
| SW-014 | ReviewApproval | PR URL (auto-detected from WorkPackage.pr_url) | Validated -> Shipped |
| SW-014 | CiOutput | GitHub Actions run URL (dotnet build/test green) | Implementing -> Validated |

## Related

- `docs/sessions/dino-steam-selfrelaunch-fix-20260529.md`
- `project_dino_steam_selfrelaunch_root_cause.md` (memory)
- SW-001 (Native Mods Page), SW-003 (Real Asset Bundles), SW-011 (Aerial Combat)
- Pattern #530 (MSBuild deploy silent no-op — verify deploy by hash/process, not exit 0)
