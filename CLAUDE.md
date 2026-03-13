# DINOForge - CLAUDE.md

## Project Overview

**DINOForge** is a general-purpose mod platform and agent-oriented development scaffold for **Diplomacy is Not an Option (DINO)**. It is a **mod operating system**, not a single mod.

- **Game**: Diplomacy is Not an Option (Unity ECS, BepInEx-compatible)
- **Architecture**: Polyrepo-hexagonal, declarative-first, agent-driven
- **Language**: C# (.NET), YAML/JSON schemas, CLI tooling
- **Mod Loader**: BepInEx + custom ECS plugin loader (`BepInEx/ecs_plugins/`)

## Build Commands

```bash
# Build
dotnet build src/DINOForge.sln

# Test
dotnet test src/DINOForge.sln --verbosity normal

# Lint
dotnet format src/DINOForge.sln --verify-no-changes

# Validate packs
dotnet run --project src/Tools/PackCompiler -- validate packs/

# Package a mod pack
dotnet run --project src/Tools/PackCompiler -- build packs/<pack-name>
```

## Repository Structure

```
DINOForge/
  src/
    Runtime/             # BepInEx plugin: bootstrap, ECS detection, debug overlay
      Bridge/            #   ECS bridge: component mapping, stat modifiers, entity queries
      HotReload/         #   Hot module replacement bridge
      UI/                #   In-game mod menu overlay (F10) and settings panel
    SDK/                 # Public mod API: registries, schemas, pack loaders
      Assets/            #   Asset service, addressables catalog, bundle info
      Dependencies/      #   Pack dependency resolver with cycle detection
      HotReload/         #   Pack file watcher for live reload
      Models/            #   Data models (units, factions, buildings, weapons, etc.)
      Registry/          #   Generic registry system with conflict detection
      Universe/          #   Universe Bible system for total conversions
      Validation/        #   Schema validation (NJsonSchema)
    Bridge/
      Protocol/          #   JSON-RPC message types and IGameBridge interface
      Client/            #   GameClient for out-of-process bridge communication
    Domains/
      Warfare/           #   Warfare domain plugin (factions, doctrines, combat, waves)
      Economy/           #   Economy domain plugin (rates, trade, balance)
      Scenario/          #   Scenario domain plugin (scripting, conditions, validation)
      UI/                #   UI/UX domain plugin (HUD injection, menu management)
    Tools/
      Cli/               #   dinoforge CLI (status, query, override, reload, watch, etc.)
      McpServer/         #   MCP server for Claude Code integration (13 game tools)
      PackCompiler/      #   Pack compiler: validate, build, package packs
      DumpTools/         #   Entity/component dump analysis (Spectre.Console)
      Installer/         #   PowerShell/Bash installer for BepInEx + DINOForge
    Tests/               #   Unit tests (xUnit + FluentAssertions)
      Integration/       #   Integration tests (Bridge, ContentLoader, end-to-end)
  packs/                 # Content packs and example mods
    example-balance/     #   Simple stat override example
    warfare-modern/      #   Modern warfare theme (West vs Classic Enemy)
    warfare-starwars/    #   Star Wars Clone Wars theme (Republic vs CIS)
    warfare-guerrilla/   #   Asymmetric warfare (Guerrilla faction)
    economy-balanced/    #   Economy balance pack
    scenario-tutorial/   #   Tutorial scenario pack
  schemas/               # Canonical JSON/YAML schema definitions (17 schemas)
  docs/                  # All project documentation
  manifests/             # System contracts, ownership maps, extension points
```

## Agent Governance

### Agents MUST:
- Work through manifests and registries
- Use generators/templates for new content
- Update docs/contracts when changing public surfaces
- Add tests for new public APIs
- Log failure modes explicitly
- Keep features pack-based when possible
- Run `dotnet test` before considering work complete

### Agents MUST NOT:
- **Handroll what a library already solves** - always search for existing packages first
- Patch runtime internals unless assigned runtime-layer work
- Invent new registry patterns casually
- Duplicate schemas
- Bypass validators
- Hardcode content IDs in engine glue
- Add undocumented extension points
- Skip tests
- Merge without compatibility checks

## Legal Move Classes (Agent Operations)

Agents should reduce all work to one of these forms:
- `create schema` - new data shape definition
- `extend registry` - add entries to existing registry
- `add content pack` - new pack with manifest
- `patch mapping` - update vanilla-to-mod mapping
- `write validator` - new validation rule
- `add test fixture` - new test case
- `add debug view` - new diagnostic surface
- `add migration` - version compatibility migration
- `add compatibility rule` - cross-pack conflict rule
- `add documentation manifest` - update docs

## Code Style

- C# 12+ with nullable reference types enabled
- `async/await` over raw Tasks
- XML doc comments on all public APIs
- Immutable data models preferred
- Registry pattern for all extensible domains
- No `var` for non-obvious types
- Meaningful names over comments

## Key Design Principles

1. **Wrap, don't handroll** - Use established libraries/tools and wrap them. Never build from scratch what a proven package already solves. Prefer thin wrappers and adapters over custom implementations. This is a vibecoding-only environment: maximize feature coverage and minimize risk by standing on existing shoulders.
2. **Framework before content** - platform first, themed mods second
3. **Declarative before imperative** - YAML/JSON manifests over C# patches
4. **Stable abstraction over unstable internals** - isolate ECS glue
5. **Agent-first repo design** - optimize for autonomous agent dev
6. **Observability is first-class** - logs, overlays, reports, validators
7. **Domain extensibility** - warfare is first plugin, not the only one
8. **Compatibility-aware packaging** - explicit deps, conflicts, versions
9. **Graceful degradation** - fail loudly with fallbacks

## Build vs Wrap Decision Rule

**ALWAYS prefer** (in order):
1. Direct use of an existing library/tool as-is
2. Thin wrapper / adapter around an existing library
3. Composition of multiple existing libraries
4. Modified fork of an existing library (last resort before handroll)

**ONLY handroll when**:
- No existing solution covers the need (e.g. DINO-specific ECS glue)
- Wrapping would be more complex than a simple implementation
- The scope is tiny and self-contained (< 50 lines)

**Rationale**: This is a fully agent-driven (vibecoding) environment. Agents produce more reliable output when integrating proven code than when generating novel implementations. Handrolled code has higher defect rates, lacks community testing, and creates maintenance burden that agents handle poorly without human review. Every handrolled component is a liability; every wrapped dependency is borrowed reliability.

### Concrete Examples

| Need | DO | DON'T |
|------|----|-------|
| YAML/JSON schema validation | Use JsonSchema.Net or NJsonSchema | Write custom validator |
| Pack dependency resolution | Use NuGet's resolver or Semver.NET | Write custom semver solver |
| Logging | Use Serilog or NLog via BepInEx | Write custom logger |
| CLI tooling | Use System.CommandLine or Spectre.Console | Write custom arg parser |
| Config management | Use BepInEx ConfigurationManager | Write custom config system |
| ECS introspection | Wrap Unity.Entities reflection APIs | Write custom reflection |
| File watching / hot reload | Use FileSystemWatcher | Write custom polling loop |
| Serialization | Use YamlDotNet + System.Text.Json | Write custom parsers |
| Diffing | Use DiffPlex or similar | Write custom diff engine |
| Testing | Use xUnit + FluentAssertions + Moq | Write custom test framework |

## Pack System

Every mod is a pack with a `pack.yaml` manifest and explicit metadata:
```yaml
id: example-pack
name: Example Pack
version: 0.1.0
framework_version: ">=0.1.0 <1.0.0"
author: DINOForge Agents
type: content  # content | balance | ruleset | total_conversion | utility
depends_on: []
conflicts_with: []
loads:
  factions: []
  units: []
  buildings: []
```

## Testing Philosophy

- **BDD-first**: Behavior specs define acceptance criteria before implementation
- **SDD**: Schema-driven development - schemas validate before runtime
- **TDD**: Unit tests for all public API surfaces
- Property-based tests for balance/combat model validation
- Pack validation tests (schema, references, completeness)
- Integration tests against mock ECS runtime

## Asset Pipeline Governance (v0.7.0+)

### Unified Asset Workflows in PackCompiler

All asset operations (3D models, textures, VFX, etc.) MUST go through **PackCompiler commands**, never fragmented tools:

```bash
# Asset import pipeline (unified, declarative)
dotnet run --project src/Tools/PackCompiler -- assets import <pack>
dotnet run --project src/Tools/PackCompiler -- assets validate <pack>
dotnet run --project src/Tools/PackCompiler -- assets optimize <pack>
dotnet run --project src/Tools/PackCompiler -- assets generate <pack>
dotnet run --project src/Tools/PackCompiler -- assets build <pack>

# Content sync
dotnet run --project src/Tools/PackCompiler -- sync download <pack> --phase <version>

# VFX generation (wraps VFXPrefabGenerator)
dotnet run --project src/Tools/PackCompiler -- vfx generate <pack>
```

### Asset Configuration: asset_pipeline.yaml

Every pack with assets MUST define `asset_pipeline.yaml` with:
- Model sources (GLB/FBX file paths)
- LOD targets (polycount percentages, screen thresholds)
- Material definitions (faction colors, emission)
- Addressables keys (for runtime loading)
- Definition updates (inject visual_asset references)

**Schema**: `schemas/asset_pipeline.schema.json` (validates all configs)

### Mandatory Asset Workflow Steps

Agents importing assets MUST follow this sequence **in order**:

1. **Define** — Create/update `asset_pipeline.yaml` in pack root
2. **Download** — `dotnet run -- sync download <pack>`
3. **Import** — `dotnet run -- assets import <pack>`
4. **Validate** — `dotnet run -- assets validate <pack>`
5. **Optimize** — `dotnet run -- assets optimize <pack>` (generates LOD)
6. **Generate** — `dotnet run -- assets generate <pack>` (creates prefabs)
7. **Verify** — `dotnet run -- assets build <pack>` (full pipeline + tests)
8. **Commit** — Git commit all artifacts + updated definitions

**Agents MUST NOT**:
- Manually edit game definitions when assets change
- Skip validation/optimization steps
- Create ad-hoc asset directories outside `packs/<pack>/assets/`
- Hardcode polycount targets or LOD percentages in C#
- Use separate/legacy tools (old download scripts, etc.)

### Asset Services (PackCompiler)

Core services in `src/Tools/PackCompiler/Services/`:

| Service | Responsibility | Tests |
|---------|-----------------|-------|
| `AssetImportService` | GLB/FBX → JSON (via AssimpNet) | 4+ tests |
| `AssetOptimizationService` | Mesh decimation → LOD variants | 4+ tests |
| `PrefabGenerationService` | JSON → .prefab (serialized) | 4+ tests |
| `AddressablesService` | YAML → catalog entries | 2+ tests |
| `DefinitionUpdateService` | Inject visual_asset into YAML | 2+ tests |

### Extension Pattern

Custom asset processors/validators can be registered:

```csharp
// In PackCompiler/Program.cs DI setup
public static IServiceCollection AddCustomAssetProcessors(this IServiceCollection services)
{
    services.AddAssetProcessor<CustomLightsaberGlowProcessor>();
    services.AddAssetValidator<StarWarsColorValidator>();
    services.AddAssetExporter<AlternativeFormatExporter>();
    return services;
}
```

Implementations MUST inherit from `IAssetProcessor`, `IAssetValidator`, `IAssetExporter` interfaces defined in PackCompiler.

### Testing Requirements for Assets

New asset features MUST include:

- **Unit tests** for each service (import, optimize, generate)
- **Integration tests** for full pipeline (download → build)
- **Regression tests** for known assets (v0.6.0 models, v0.7.0 critical)
- **Performance tests** (import < 5s/model, full pipeline < 5min for 9 models)
- **Schema validation tests** (asset_pipeline.yaml)

All asset tests live in `src/Tests/AssetPipelineTests.cs`

### Documentation Requirements

Agents changing asset workflows MUST update:

1. `ASSET_PIPELINE_CLI.md` — Command reference
2. `asset_pipeline.schema.json` — Config schema
3. `CLAUDE.md` (this section) — Governance changes
4. Inline XML docs in PackCompiler services
5. Test cases documenting new behavior
