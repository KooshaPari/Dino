# DINOForge

[![CI](https://github.com/KooshaPari/Dino/actions/workflows/ci.yml/badge.svg)](https://github.com/KooshaPari/Dino/actions/workflows/ci.yml)
[![codecov](https://codecov.io/gh/KooshaPari/Dino/branch/main/graph/badge.svg)](https://codecov.io/gh/KooshaPari/Dino)

**General-purpose mod platform for [Diplomacy is Not an Option](https://store.steampowered.com/app/1272320/Diplomacy_is_Not_an_Option/).**

DINOForge is a mod operating system, not a single mod. It provides the framework, registries, schemas, and tooling for building any type of mod — from simple balance tweaks to full total conversion packs.

## Features

- **Pack System** — YAML-first declarative content packs with dependency resolution, conflict detection, and schema validation
- **Typed Registries** — Units, buildings, factions, weapons, projectiles, doctrines, skills, waves, squads with layered override priority
- **ECS Bridge** — Maps mod content to DINO's actual Unity ECS components at runtime (30+ component mappings)
- **Asset Pipeline** — Full import → validate → optimize → LOD → prefab → Addressables pipeline; 38 catalog entries with 3-level LOD (100%/60%/30%)
- **Warfare Domain** — Faction archetypes (Order, Industrial Swarm, Asymmetric), doctrines, unit role validation, wave composition, balance calculation
- **Star Wars Clone Wars Pack** — 28 units (Republic + CIS) and 10 buildings with visual assets, prefabs, and Addressables entries
- **Dev Tooling** — PackCompiler CLI, DumpTools, in-game debug overlay, entity dumper
- **Schema Validation** — 10 JSON schemas catch errors before runtime

## Milestone Status

| Milestone | Description | Status |
|-----------|-------------|--------|
| M0 | Reverse-Engineering Harness (entity dumps, 45K entities) | Done |
| M1 | Runtime Scaffold (BepInEx plugin, ECS systems) | Done |
| M2 | Generic Mod SDK (registries, schemas, ContentLoader) | Done |
| M3 | Dev Tooling (PackCompiler, DumpTools, DebugOverlay) | Done |
| M4 | Warfare Domain (archetypes, doctrines, roles, waves, balance) | Done |
| M5 | Example Packs (warfare-starwars 28u/10b, warfare-guerrilla, warfare-modern) | In Progress — substantially complete |
| Docs | VitePress site (kooshapari.github.io/Dino) | In Progress |
| CI/QA | GitHub Actions, templates, contributing guides | In Progress |

**Current test count: 879 passing**

## Quick Start

### Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Diplomacy is Not an Option](https://store.steampowered.com/app/1272320/) (for Runtime deployment)
- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx/releases) (installed in game directory)

### Build

```bash
dotnet build src/DINOForge.sln
```

### Test

```bash
dotnet test src/DINOForge.sln
```

### Validate a Pack

```bash
dotnet run --project src/Tools/PackCompiler -- validate packs/example-balance
```

### Create a Mod Pack

Create a directory with a `pack.yaml` manifest:

```yaml
id: my-balance-mod
name: My Balance Mod
version: 0.1.0
author: YourName
type: balance
framework_version: ">=0.1.0"
loads:
  units:
    - units/
  buildings:
    - buildings/
```

Then add YAML content files in the referenced directories. See `packs/example-balance/` for a complete example.

## Architecture

```mermaid
graph TD
    subgraph "Content Layer"
        P1[warfare-modern]
        P2[warfare-starwars]
        P3[warfare-guerrilla]
        P4[example-balance]
    end

    subgraph "Domain Layer"
        W[Warfare Plugin]
        E[Economy Plugin]
        S[Scenario Plugin]
    end

    subgraph "SDK Layer"
        REG[Registries]
        SCH[Schema Validation]
        CL[ContentLoader]
        AST[Asset Pipeline]
        DEP[Dependency Resolver]
    end

    subgraph "Runtime Layer"
        PLG[BepInEx Plugin]
        BRG[ECS Bridge]
        DMP[Entity Dumper]
        OVL[Debug Overlay]
    end

    subgraph "Game"
        DINO[Diplomacy is Not an Option<br/>Unity ECS / DOTS]
    end

    P1 & P2 & P3 & P4 --> CL
    W & E & S --> REG
    CL --> REG & SCH & DEP
    REG & AST --> BRG
    BRG & PLG & DMP & OVL --> DINO
```

### Pack Loading Pipeline

```mermaid
sequenceDiagram
    participant PC as PackCompiler
    participant CL as ContentLoader
    participant SV as SchemaValidator
    participant DR as DependencyResolver
    participant RG as Registry

    PC->>CL: LoadPacks(packsDir)
    CL->>CL: Discover pack.yaml files
    CL->>DR: Resolve dependencies
    DR-->>CL: Topological load order
    loop Each pack in order
        CL->>SV: Validate content YAML
        SV-->>CL: ValidationResult
        CL->>RG: Register(id, data, source, packId)
    end
    CL-->>PC: ContentLoadResult
```

### Registry Priority Layers

```
┌─────────────────────────────────┐
│  Pack (priority 3000+)          │  ← Mod content overrides
├─────────────────────────────────┤
│  Domain Plugin (priority 2000+) │  ← Warfare/Economy defaults
├─────────────────────────────────┤
│  Framework (priority 1000+)     │  ← DINOForge defaults
├─────────────────────────────────┤
│  Base Game (priority 0+)        │  ← Vanilla DINO values
└─────────────────────────────────┘
  Higher priority wins. Same priority = conflict detected.
```

| Layer | Purpose | Target |
|-------|---------|--------|
| **Runtime** | BepInEx bootstrap, ECS system injection, component mapping | netstandard2.0 |
| **SDK** | Public mod API — registries, schemas, pack loading, asset tools | netstandard2.0 |
| **Domains** | Game logic — factions, doctrines, combat, economy | netstandard2.0 |
| **Tools** | CLI — pack compiler, dump analyzer, asset inspector | net8.0 |
| **Tests** | xUnit + FluentAssertions | net8.0 |

## Project Structure

```
DINOForge/
  src/
    Runtime/           # BepInEx plugin + ECS Bridge
    SDK/               # Public mod API
    Domains/Warfare/   # Warfare domain plugin
    Tools/PackCompiler/# CLI: validate, build, assets
    Tools/DumpTools/   # CLI: dump analysis
    Tests/             # Unit + integration tests
  packs/               # Content packs
  schemas/             # JSON Schema definitions
  docs/                # Documentation (VitePress)
```

## Documentation

Visit [kooshapari.github.io/Dino](https://kooshapari.github.io/Dino) for full documentation.

## Project Policies

- [SECURITY.md](SECURITY.md) describes private vulnerability reporting, supported versions, and response timelines.
- [SUPPORT.md](SUPPORT.md) describes support channels, self-service checks, and issue routing.
- [FUZZING.md](FUZZING.md) documents the current randomized testing posture and remaining fuzzing gaps.
- [CONTRIBUTING.md](CONTRIBUTING.md) defines contributor workflow, testing expectations, and release hygiene.

## Development Methodology

- **SDD** (Spec-Driven Development) — specifications drive the pipeline
- **BDD** (Behavior-Driven Development) — acceptance criteria before implementation
- **TDD** (Test-Driven Development) — unit tests for all public APIs
- **DDD** (Domain-Driven Design) — bounded contexts (Warfare, Economy, Scenario)
- **ADD** (Agent-Driven Development) — fully agent-authored codebase
- **CDD** (Contract-Driven Development) — schemas as contracts between packs and engine

## Formal Governance

- **Coverage** — Code coverage is published to Codecov from CI and governed by `codecov.yml`.
- **Versioning** — public releases use SemVer tags (`vX.Y.Z`) with `VERSION` tracking the latest released version.
- **Changelog** — `CHANGELOG.md` follows Keep a Changelog with a permanent `[Unreleased]` section.
- **Ownership** — `.github/CODEOWNERS` is the review-routing source of truth.
- **Release Process** — see [RELEASING.md](RELEASING.md).
- **Shared KooshaPari semantics** — see [docs/reference/kooshapari-project-semantics.md](docs/reference/kooshapari-project-semantics.md).

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

## License

MIT

## Acknowledgements

- [BepInEx](https://github.com/BepInEx/BepInEx) — Unity mod loader
- [AssetsTools.NET](https://github.com/nesrak1/AssetsTools.NET) — Unity asset bundle library
- [devopsdinosaur/dno-mods](https://github.com/devopsdinosaur/dno-mods) — Pioneering DINO modding patterns
# Trigger checks
