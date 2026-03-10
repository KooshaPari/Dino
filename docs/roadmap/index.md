# Roadmap

DINOForge follows a milestone-based development plan, building from low-level runtime to high-level content packs.

## Milestones

### M0 — Reverse-Engineering Harness :white_check_mark:

**Status**: Complete

Install BepInEx, confirm plugin loading, test plugin, dump entities.

- BepInEx 5.4.23.5 installed with modified Doorstop pre-loader
- Runtime builds to `BepInEx/ecs_plugins/DINOForge.Runtime.dll` (26KB)
- Entity Dumper, System Enumerator, Debug Overlay scaffolded
- 6/6 tests passing (xUnit + FluentAssertions)

---

### M1 — Runtime Scaffold :white_check_mark:

**Status**: Complete

Bootstrap plugin, version detection, logging, ECS introspection, debug overlay.

- BepInEx plugin bootstrap with version-aware initialization
- ECS system and component discovery
- Runtime logging infrastructure
- Debug overlay foundation

---

### M2 — Generic Mod SDK :construction:

**Status**: In Progress

Pack manifest format, registry system, schema validation, override model, dependency resolver.

- Pack manifest model with YAML loader (YamlDotNet)
- Registry system for extensible content types
- Schema validation pipeline (JsonSchema.Net / NJsonSchema)
- Content override model with priority layering
- Dependency resolver with cycle detection

---

### M3 — Dev Tooling :construction:

**Status**: In Progress

Pack compiler CLI, validator CLI, test harness, diff tools, diagnostics.

- PackCompiler CLI (validate, build, assets commands)
- DumpTools CLI with Spectre.Console
- Schema validation integration
- Pack diffing and compatibility checks

---

### M3.5 — QA Harness :calendar:

**Status**: Planned

BepInEx QA plugin with IPC (named pipe/WebSocket), external test driver, ECS state assertions, CI-runnable integration tests.

- In-game QA plugin that accepts commands via IPC
- External test driver for automated testing
- ECS state assertion framework
- CI integration for automated pack validation

---

### M4 — Warfare Domain Plugin :calendar:

**Status**: Planned

Factions, doctrines, unit classes, weapons, waves, defenses.

- Faction archetype system (Order, Industrial Swarm, Asymmetric)
- 5 factions: Republic, CIS, West, Classic West Enemy, Guerrilla
- Unit role matrix with 13 shared slots
- Weapon classes and defense tags
- Wave composition templates
- Doctrine modifiers

---

### M5 — First Example Packs :calendar:

**Status**: Planned

West vs Classic Enemy, then Republic vs CIS, then Guerrilla.

- Modern Warfare theme pack (West vs Classic West Enemy)
- Star Wars Clone Wars theme pack (Republic vs CIS)
- Asymmetric warfare pack (West vs Guerrilla)
- Level 1 content (zero-new-model build)

---

### M6 — Content Polish :calendar:

**Status**: Planned

Signature structures, better models, faction audio, campaign wrappers.

- Level 2 content (selective model swaps for key units)
- Faction-specific audio packs
- Campaign/scenario scripting
- Polish and balance iteration

---

## Success Criteria

> A new mod can be created mostly by editing validated pack files and running the toolchain, without new reverse engineering.

If every mod still needs runtime surgery, the framework failed.
