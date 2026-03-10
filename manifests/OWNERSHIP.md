# Module Ownership Map

This document defines ownership boundaries for agent-driven development.
Each module has a single owner type and clear dependency rules.

---

## Runtime Layer (HIGH SENSITIVITY)

**Owner**: Runtime agents only
**Files**: `src/Runtime/`
**Touches**: Raw ECS, BepInEx bootstrap, game version detection, hook points
**Dependencies**: Unity.Entities, BepInEx, HarmonyX (sparingly)
**Rule**: Fewest agents should touch this. Changes require version-gate testing.

---

## SDK Layer (MEDIUM SENSITIVITY)

**Owner**: Architect agents + SDK agents
**Files**: `src/SDK/`, `src/Registries/`, `src/Schemas/`
**Touches**: Public mod API, registries, schema validation, pack loading
**Dependencies**: Runtime (abstracted), System.Text.Json/YamlDotNet
**Rule**: All public API changes require docs + tests + schema updates.

---

## Domain Plugins (MEDIUM SENSITIVITY)

**Owner**: Domain agents (per-domain)
**Files**: `src/Domains/<DomainName>/`
**Touches**: Domain-specific registries, schemas, validation
**Dependencies**: SDK only (never other domains, never Runtime directly)
**Rule**: Each domain is independent. No cross-domain imports.

---

## Packs (LOW SENSITIVITY)

**Owner**: Pack agents / content agents
**Files**: `packs/<pack-name>/`
**Touches**: YAML/JSON content files, asset references
**Dependencies**: Schemas only
**Rule**: Must pass pack compiler validation. Mostly declarative.

---

## Tools (MEDIUM SENSITIVITY)

**Owner**: Tooling agents
**Files**: `src/Tools/`
**Touches**: CLI tools, pack compiler, validators, dump tools
**Dependencies**: SDK, Schemas
**Rule**: Tools must be deterministic and produce machine-parseable output.

---

## Debug (LOW SENSITIVITY)

**Owner**: Debug/diagnosis agents
**Files**: `src/Debug/`
**Touches**: Overlays, inspectors, hot reload, logging
**Dependencies**: Runtime (read-only), SDK
**Rule**: Debug code must never affect gameplay behavior.

---

## Tests (REQUIRED FOR ALL)

**Owner**: All agents (each maintains tests for their module)
**Files**: `src/Tests/`
**Rule**: Every public API change requires corresponding test update.

---

## Docs (ALL AGENTS)

**Owner**: All agents update docs for their changes
**Files**: `docs/`
**Rule**: ADRs for architectural changes. WORKLOG for session summaries. CHANGELOG for releases.
