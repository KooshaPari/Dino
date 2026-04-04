# PLAN: DINOForge — DINO Mod Platform

## Purpose

DINOForge provides state-of-the-art mod infrastructure for Unity ECS-based real-time strategy games.

---

## Phases

| Phase | Duration | Key Deliverables | Resource Estimate |
|-------|----------|------------------|-------------------|
| 1: Runtime Scaffold | 3 weeks | BepInEx plugin, ECS Bridge, entity dumper | 2 developers |
| 2: SDK | 3 weeks | Registries, schemas, ContentLoader | 2 developers |
| 3: Dev Tooling | 2 weeks | PackCompiler, DumpTools, DebugOverlay | 1 developer |
| 4: Warfare Domain | 2 weeks | Archetypes, doctrines, roles, waves | 2 developers |
| 5: Example Packs | 2 weeks | Star Wars, modern, guerrilla packs | 2 developers |
| 6: Desktop Companion | 3 weeks | WinUI 3 pack manager, asset browser | 2 developers |
| 7: Testing | 2 weeks | 1017+ tests, fuzzing, CI/CD | 1 developer |

---

## Phase Details

### Phase 1: Runtime Scaffold
- BepInEx plugin structure
- ECS Bridge (component mapping)
- Entity dumper (45K entities)
- Vanilla catalog mirror

### Phase 2: SDK
- TypedRegistry<T> generic base
- Unit, Building, Faction registries
- JSON Schema validation
- DependencyResolver
- ContentLoader orchestration

### Phase 3: Dev Tooling
- PackCompiler CLI (validate, build)
- DumpTools (entity analysis)
- DebugOverlay (F9/F10 in-game)
- Hot module replacement

### Phase 4: Warfare Domain
- Unit archetypes (infantry, ranged, etc.)
- Combat doctrines
- Unit role system
- Wave scripting
- Balance calculation

### Phase 5: Example Packs
- warfare-starwars (28 units, 10 buildings)
- warfare-modern (contemporary military)
- warfare-guerrilla (asymmetric warfare)
- example-balance (simple tweaks)

### Phase 6: Desktop Companion
- WinUI 3 application
- Pack manager (add/list/update/lock)
- Asset browser
- Mod conflict detection
- F9/F10 mirror from game

### Phase 7: Testing
- Unit tests (xUnit)
- Property tests (FsCheck)
- Fuzzing (SharpFuzz)
- CI/CD pipeline
- 1017+ total tests

---

## Resource Summary

| Resource | Estimate |
|----------|----------|
| **Total Duration** | 17 weeks |
| **Developers** | 2-3 |
| **Complexity** | High |
| **Priority** | High |

---

## Status

Production — M0-M14 complete, 1017+ tests passing.

---

## Traceability

`/// @trace DINOFORGE-PLAN-001`
