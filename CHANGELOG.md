# Changelog

All notable changes to DINOForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

#### Asset Sourcing Research
- **REPUBLIC_AT_WAR_ASSET_AUDIT.md** (933 lines, 36KB): Comprehensive audit of Empire at War mod assets as potential reuse candidates
  - Asset type inventory: units, buildings, textures, effects, audio (700+ total assets)
  - File format analysis: .alo, .dae, .dds, .alp, proprietary formats
  - Polygon count comparison: RAW 5–8x higher than DINO RTS budget
  - Quality assessment: High detail but unsuitable for performance constraints
  - Licensing analysis: Star Wars IP + mixed mod licensing creates critical legal risk
  - Conversion path analysis: 40–80 hours engineering effort
  - Final recommendation: **⛔ DO NOT PURSUE** (legal risk too high, better alternatives exist)
  - Approved path: Continue Kenney.nl (v1.0), plan Sketchfab premium (v1.1+)

#### M5: Example Packs & Asset Integration (COMPLETE)
- **All three warfare packs released**: warfare-starwars, warfare-modern, warfare-guerrilla with complete gameplay configurations
- **Unit textures complete**: 26 procedurally-generated faction-specific textures (13 Republic + 13 CIS) via HSV-based color transformation
- **Building assets complete**:
  - 20 faction-specific building textures (512×512 PNG, sRGB, 2.1–4.7 KB each)
  - 4 fully-assembled Blender FBX files (rep_house_clone_quarters, cis_house_droid_pod, rep_farm_hydroponic, cis_farm_fuel_harvester)
  - 24 buildings fully mapped and documented (Kenney.nl source, poly budgets, faction color schemes)
- **Asset infrastructure**:
  - ASSET_SOURCES.json with complete building registry, texture inventory, and Kenney.nl source mapping
  - Blender assembly templates and batch execution plans (60-72 hour estimate for full assembly)
  - Quality gates: polygon counts verified, faction palettes defined, export standards documented
- **Documentation complete**:
  - BLENDER_ASSEMBLY_TEMPLATE.md: detailed step-by-step guide for single building assembly
  - BATCH_ASSEMBLY_PLAN.md: parallelization strategies (2–4 artist teams, 2–3 week timeline)
  - BUILD_CHECKLIST_ENHANCED.md: master checklist with all 24 buildings and assembly specifications

#### M5: Unit Texture Generation Complete (Star Wars Pack)
- **generate_unit_textures.py**: Parallel texture generation pipeline for all 26 units (13×2 factions)
  - Procedural texture generation with PIL and HSV-based color transformation
  - 16-worker multiprocessing for rapid generation (~16 seconds total)
  - Supports per-faction and per-unit color palettes (Republic white/blue vs CIS dark/orange)
  - Tier-based visual complexity (T1 simple → T3 elite with glow effects)
  - Unit-type-aware patterns (vehicles: horizontal stripes, infantry: vertical segments)
- **assets/textures/units/**: 26 unit textures (512×512 PNG, sRGB, RGBA, 2.1–4.7 KB each)
  - 13 Republic units: Clone Militia through Clone Commando
  - 13 CIS units: B1 Battle Droid through IG-100 MagnaGuard
  - All textures with faction-specific color schemes applied
- **UNIT_TEXTURE_MANIFEST.json**: Metadata index with unit class, tier, vehicle/infantry type, palette source
- **TEXTURE_GENERATION_REPORT.md**: Comprehensive generation report with algorithm details, quality metrics, and next steps

#### M5: Asset Integration Phase 1 Complete (Star Wars Pack)
- **ASSET_INTEGRATION_REPORT.md**: Master status document for Phase 1 (Asset sourcing & inventory)
  - Executive summary of sourcing decisions (Kenney-first strategy, Sketchfab deferred to v1.1+)
  - Current integration status: 4/24 FBX complete, 20/20 textures complete, all sources mapped
  - Quality metrics: polygon counts verified (280-340 tri/building, < 400 tri budget)
  - Integration roadmap with 4 phases: FBX export (current), game testing, validation, optional Sketchfab (post-v1.0)
  - License documentation (CC0 Kenney.nl)
  - QA checklist and known issues
- **BUILD_STATE_SUMMARY.md**: Quick reference guide for current pack state
  - Status table: all components mapped with completion percentage
  - Phase 1 bottleneck identification (FBX batch export, 40-60 hours required)
  - Next steps prioritized (FBX batch, validation, git commit)
  - Directory structure and documentation locations
  - Success criteria for Phase 1 completion
- **manifest.yaml**: Updated with asset source references
  - Added `asset_replacements.buildings` with source documentation paths
  - Added `asset_sources` section referencing Kenney.nl and integration reports
- **ASSET_SOURCES.json**: Enhanced summary fields
  - Added `sourcing_strategy` field explaining Kenney-first approach
  - Added `integration_status` and `phase_1_completion` fields for clarity
  - Metadata timestamp: 2026-03-12

#### M5: Blender Assembly Documentation (Star Wars Pack)
- `BLENDER_ASSEMBLY_TEMPLATE.md`: Complete step-by-step Blender guide for assembling single building (rep_house_clone_quarters as template):
  - Phase 1: Project setup (10 min) — new project, import Kenney FBX, rename
  - Phase 2: Texture application (30 min) — material nodes, faction texture PNG, shader preview
  - Phase 3: Details (45 min) — accent stripes, emblems, glow emitters, faction-specific customization
  - Phase 4: Optimization (20 min) — poly decimation, material merging, pivot centering
  - Phase 5: Export (15 min) — FBX export with correct settings, file naming, output validation
  - Phase 6: Validation (30 min) — reimport verification, in-game testing, documentation
  - ASCII art before/after visual references
  - Detailed troubleshooting table (texture loading, scale, colors, export issues)
  - Advanced options (custom glow emitters, rigged animated parts)
  - Workflow diagram showing phase dependencies
  - Expected time: 2 hours (first build); 45 min (with practice)
- `BATCH_ASSEMBLY_PLAN.md`: Comprehensive parallelization & scheduling document:
  - Parallelization strategy: 2-artist team (rep specialist + cis specialist) or 4-artist team (by building type)
  - Priority assembly order: Tier 1 (Simple, 8 bldgs, 16 hrs), Tier 2 (Medium, 8 bldgs, 24 hrs), Tier 3 (Hard, 7 bldgs, 22 hrs), Expert (shaders/custom geometry, 13 hrs)
  - Building dependency analysis: infinite_iron_mine mirrors iron_mine (saves 1.5 hrs); farm/granary share platform pieces
  - 3-week timeline: Week 1 (pilots + Tier 1), Week 2 (Tier 2-3), Week 3 (validation + deployment)
  - Parallel execution schedules for 2-artist and 4-artist teams with effective completion in 2-3 weeks
  - Automation status: texture generation complete (< 1 min), Blender script framework ready, batch mode pending
  - Quality gates (poly count, texture, scale, materials, performance, naming, export, mapping)
  - Risk & mitigation table (scale validation, poly budget, palette accuracy, material preservation, schedule)
  - Team communication plan (daily standup, weekly sync, issue tracking)
  - Lessons learned template for knowledge capture
  - Hand-off criteria for next team (FBX, textures, manifests, validation results)
  - Future optimization opportunities (full automation, custom shaders, variants, LOD generation)
- `BUILD_CHECKLIST_ENHANCED.md`: Master checklist for all 24 buildings with detailed assembly specifications:
  - Quick reference table: all 24 buildings with Kenney source, complexity, estimated hours
  - Faction palette reference: Republic (white #F5F5F5 + navy #1A3A6B) and CIS (grey #444444 + orange #B35A00) with shader settings
  - Kenney source inventory: space-kit (9/12 buildings), modular-space-kit (fallback), polygon counts
  - Building-by-building guide covering all 12 vanilla buildings × 2 factions (24 total):
    - Each building includes: vanilla ID, Kenney source, poly budget, complexity rating, effort estimate, detailed assembly notes
    - GROUP 1: Residential (house, granary, hospital) + GROUP 2: Resources (forester, stone, iron mines, soul mine) + GROUP 3: Military (builder, guild, gate)
    - Complexity ratings: Simple (1.5-2 hrs), Medium (2-3 hrs), Hard (3-4 hrs), Expert (4-4.5 hrs)
  - Artifact output targets: 24 FBX files (in meshes/buildings/), 24 textures (in textures/buildings/)
  - Grand total estimate: 60-72 hours human effort (52-60 with optimizations); 2.5 hrs per building average
  - Quality checkpoints: poly count, texture, scale, materials, normals, export, game testing, details, documentation
  - Kenney CC0 license note (no attribution required)
  - Success criteria for final delivery (24 FBX, 24 textures, poly budgets, faction details, game validation, crosswalk updates, source archival)

#### M0: Reverse-Engineering Harness
- BepInEx 5.4.23.5 runtime plugin targeting `netstandard2.0`
- ECS `DumpSystem` (SystemBase) that survives MonoBehaviour destruction
- `EntityDumper`: serializes worlds, archetypes, component types, entity samples to JSON
- `SystemEnumerator`: enumerates all registered ECS systems with metadata
- `DebugOverlay`: F9 IMGUI overlay showing live ECS world state
- First gameplay dump: 45,776 entities across 6 worlds, 500K lines of data

#### M2: Generic Mod SDK
- `PackManifest` + `PackLoader`: YAML manifest parsing via YamlDotNet
- `PackDependencyResolver`: Kahn's algorithm for topological sort, conflict detection
- `NJsonSchemaValidator`: schema validation wrapping NJsonSchema
- `Registry<T>`: generic typed registry with layered overrides (BaseGame → Framework → DomainPlugin → Pack)
- `RegistryManager`: typed registries for Units, Buildings, Factions, Weapons, Projectiles, Doctrines, Skills, Waves, Squads
- Content models: UnitDefinition, FactionDefinition, WeaponDefinition, ProjectileDefinition, DoctrineDefinition, BuildingDefinition, SkillDefinition, WaveDefinition, SquadDefinition
- `ContentLoader`: orchestrates pack loading from directory to registry
- 10 JSON schemas (pack-manifest, unit, faction, weapon, projectile, doctrine, building, skill, wave, squad)
- Example pack: `packs/example-balance/` with units, buildings, factions

#### M3: Dev Tooling
- `PackCompiler` CLI: `validate`, `build`, `assets list/inspect/validate` commands
- `DumpTools` CLI: `list`, `analyze`, `components`, `systems`, `namespaces` for offline dump analysis

#### Asset Pipeline
- AssetsTools.NET 3.0.4 integration for asset bundle reading/writing
- `AssetService`: ListBundles, ListAssets, ExtractAsset, ValidateModBundle
- `AddressablesCatalog`: parses DINO's Addressables catalog.json (492 entries)

#### ECS Bridge
- `ComponentMap`: 30+ mappings between DINO ECS components and SDK model fields
- `EntityQueries`: helper queries for player units, enemy units, buildings by class/type
- `StatModifierSystem`: ECS system for applying mod stat changes (Override/Add/Multiply)
- `VanillaCatalog`: runtime scanner classifying vanilla entities into registry IDs
- `AssetSwapSystem`: skeleton for total conversion asset replacement

#### M4: Warfare Domain Plugin
- `ArchetypeRegistry`: 3 faction archetypes (Order, Industrial Swarm, Asymmetric)
- `DoctrineEngine`: applies modifier chains (archetype + doctrine), validates stat bounds
- `UnitRoleValidator`: validates faction rosters against 11 required role slots
- `WaveComposer`: generates wave sequences with tier-based unit selection + difficulty scaling
- `BalanceCalculator`: power rating formula, faction comparison, balance assessment
- `WarfarePlugin`: entry point with full pack validation

#### Economy Domain Plugin
- `EconomyPlugin`: entry point with production, trade, balance, and validation subsystems
- `ResourceRate`: resource production/consumption rate model with 5 valid types
- `EconomyProfile`: per-faction economy profile (starting resources, multipliers, trade modifiers)
- `TradeRoute`: resource conversion routes with exchange rates, cooldowns, and transaction limits
- `ProductionCalculator`: calculates faction production from buildings, workers, and profile modifiers
- `TradeEngine`: evaluates trade route profitability, suggests optimal trades for resource deficits
- `EconomyBalanceCalculator` + `EconomyBalanceReport`: per-faction economy balance analysis
- `EconomyValidator`: validates profiles, trade routes, resource references, circular dependencies
- `economy-profile.schema.json` schema for validation
- Example pack: `packs/economy-balanced/` with standard/abundance profiles and trade routes

#### Scenario Domain Plugin
- `ScenarioPlugin`: entry point with runner, validator, and difficulty scaler subsystems
- `ScenarioDefinition`: core model with difficulty, objectives, waves, conditions, scripted events
- `VictoryCondition`: 6 types (SurviveWaves, DestroyTarget, ReachPopulation, AccumulateResource, TimeSurvival, Custom)
- `DefeatCondition`: 5 types (CommandCenterDestroyed, PopulationZero, TimeExpired, ResourceDepleted, Custom)
- `ScriptedEvent` + `EventAction`: trigger-based event system with 6 trigger types and 8 action types
- `ScenarioRunner`: evaluates game state against conditions, fires scripted events with dedup tracking
- `GameState`: game state snapshot for condition evaluation
- `DifficultyScaler`: scales resources (Easy 1.5x → Nightmare 0.5x) and wave intensity with aggression factors
- `ScenarioValidator`: validates factions, conditions, events, resources, wave counts
- `scenario.schema.json` schema for validation
- Example pack: `packs/scenario-tutorial/` with defense tutorial, survival challenge, resource race

#### M5: Example Packs
- `warfare-modern`: 26 units (West vs Classic Enemy), 16 weapons, 10 waves
- `warfare-starwars`: 26 units (Republic vs CIS), 19 weapons, 10 waves
- `warfare-guerrilla`: 13 units (Guerrilla), 13 weapons, 10 waves

#### M6: In-Game Mod Menu & HMR
- `ModMenuOverlay`: F10-toggled IMGUI window with pack list, enable/disable toggles, status bar
- `ModSettingsPanel`: BepInEx ConfigEntry wrapper with auto-discovered settings UI
- `PackFileWatcher`: FileSystemWatcher-based HMR with 500ms debounce, thread-safe reload
- `HotReloadResult`: immutable result type (Success/Failure/Partial)
- `HotReloadBridge`: connects SDK HMR to BepInEx logger and ECS runtime
- UI Domain Plugin: `UIPlugin`, `MenuManager`, `HUDInjectionSystem` stubs

#### M7: Installer & Universe Bible System
- `Install-DINOForge.ps1`: PowerShell installer (auto-detect Steam, download BepInEx, --Dev flag)
- `install.sh`: Bash installer for Linux/Steam Deck
- `SteamLocator`: finds DINO install via Windows registry + libraryfolders.vdf parsing
- `InstallVerifier`: validates BepInEx, Runtime DLL, packs directory
- `UniverseBible`: per-theme metadata container (era, taxonomy, crosswalk, naming, style)
- `CrosswalkDictionary`: bidirectional vanilla↔themed entity mapping with wildcard patterns
- `FactionTaxonomy`: faction hierarchy with alignment, archetype, sub-factions, unit rosters
- `NamingGuide`: per-faction naming rules (prefix/suffix/pattern/overrides)
- `StyleGuide`: color palettes, audio themes, architecture styles per faction
- `UniverseLoader`: loads UniverseBible from YAML directory structure
- `PackGenerator`: generates complete mod pack from UniverseBible + faction selection
- `universe-bible.json` schema for validation
- Example universes: `star-wars-clone-wars/` and `modern-warfare/`

#### Dev Tooling
- `make deploy`: one-command build + deploy Runtime to game directory
- `make deploy-packs`: sync pack YAML files to game packs directory
- `make watch`: auto-rebuild on source changes
- `.claude/commands/deploy.sh`: agent-accessible deploy command
- Runtime auto-deploys to `BepInEx/plugins/` on build (OutputPath in csproj)

### Fixed
- `PackCompiler` CLI updated for System.CommandLine 2.0.3 API (SetAction, mutable collections)
- `YamlSchemaConverter`: fixed YAML-to-JSON conversion to properly coerce scalar types
- `NoAllocReadOnlyCollection` IEnumerable cast error in SystemEnumerator and DebugOverlay
- MonoBehaviour lifecycle incompatibility with DINO's ECS-first architecture
- Runtime now references SDK for HotReload bridge integration

## [0.1.0] - 2026-03-10

### Added
- Initial project structure and documentation foundation
- PRD defining DINOForge as a general-purpose DINO mod platform
- ADR-001 through ADR-008 (agent-driven dev, declarative arch, pack system, registry model, ECS integration, domain plugins, observability, wrap-don't-handroll)
- Warfare domain specification with faction archetypes and unit role matrix
- CLAUDE.md agent governance document
- Pack manifest, faction, and unit YAML schemas
- Module ownership map and extension point documentation

[Unreleased]: https://github.com/KooshaPari/Dino/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/KooshaPari/Dino/releases/tag/v0.1.0
