# Changelog

All notable changes to DINOForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.5.0] - 2026-03-11

### Added

#### GUI Installer & Release Pipeline
- Avalonia-based cross-platform GUI installer with auto-update capability
- Player and Developer installation modes with separate workflows
- Interactive wizard UI for initial setup and pack selection
- GitHub Actions release pipeline for automated version publishing
- Release artifact generation and NuGet package distribution

#### Pack Registry System
- Pack registry for discovering and managing installed packs
- Registry metadata with version compatibility tracking
- Example pack templates with `dotnet new` template integration
- Pack discovery and enumeration APIs

#### NuGet Packaging & Distribution
- SDK NuGet package publication (`DINOForge.SDK` on nuget.org)
- Automated release pipeline for public package distribution
- Semantic versioning enforcement across package lifecycle
- Framework version compatibility constraints in package metadata

#### YAML Deserialization Fixes
- Fixed YAML array deserialization for list/collection fields
- Improved scalar type coercion in YamlSchemaConverter
- Better error messages for malformed YAML structures
- Backward compatibility with existing pack manifests

#### Stat Override Pipeline Enhancements
- Fixed stat modifier timing and application order
- Corrected damage calculation path for stat overrides
- YAML override integration with UI display
- Runtime stat modification system complete with validation

#### Debug Overlay Improvements
- Added error display panel to F9 debug overlay
- Improved ContentLoader error tracking and reporting
- Visual error indicators for pack loading failures
- Detailed diagnostic messages for troubleshooting

### Fixed

- Resolved all 20 pack loading errors from incomplete migrations
- Removed conflicting `conflicts_with` pack metadata for concurrent loading
- Fixed Plugin persistence across scene transitions
- Corrected stat pipeline timing relative to ECS system ordering
- Fixed YAML array handling in all domain models
- Added proper error display to debug overlay for visibility

### Changed

- Removed strict pack conflict checking to allow flexible pack combinations
- Updated all documentation to reflect v0.5.0 features
- Improved UI descriptions and help text across all overlays
- Enhanced error messages throughout ContentLoader pipeline

## [0.4.0] - 2026-03-10

### Added

#### M8: Runtime Integration & IPC Bridge
- `ModPlatform` orchestrator wiring SDK to Bridge to UI to HMR
- JSON-RPC IPC bridge protocol for out-of-process communication
- `GameClient` for external tool communication with game runtime
- Agent debug layer with MCP server integration (13 game tools)
- Runtime integration tests and end-to-end scenarios

#### Economy Domain Plugin
- `EconomyPlugin` with production, trade, balance, and validation subsystems
- `ResourceRate` model supporting 5 resource types with production/consumption rates
- `EconomyProfile` per-faction configuration with starting resources and trade modifiers
- `TradeRoute` system with exchange rates, cooldowns, and transaction limits
- `ProductionCalculator` for faction resource generation from buildings and workers
- `TradeEngine` for evaluating trade profitability and suggesting optimal trades
- `EconomyBalanceCalculator` + `EconomyBalanceReport` for per-faction analysis
- `EconomyValidator` for profile, route, and dependency validation
- `economy-profile.schema.json` schema for economy content validation
- Example pack: `packs/economy-balanced/` with economy profiles and trade routes

#### Scenario Domain Plugin
- `ScenarioPlugin` with runner, validator, and difficulty scaler subsystems
- `ScenarioDefinition` model supporting difficulty levels, objectives, waves, and conditions
- `VictoryCondition` system with 6 condition types (SurviveWaves, DestroyTarget, ReachPopulation, AccumulateResource, TimeSurvival, Custom)
- `DefeatCondition` system with 5 condition types (CommandCenterDestroyed, PopulationZero, TimeExpired, ResourceDepleted, Custom)
- `ScriptedEvent` + `EventAction` trigger-based system with 6 trigger types and 8 action types
- `ScenarioRunner` for evaluating game state and firing scripted events with deduplication
- `GameState` snapshot model for condition evaluation
- `DifficultyScaler` supporting Easy (1.5x) to Nightmare (0.5x) resource scaling
- `ScenarioValidator` for comprehensive scenario validation
- `scenario.schema.json` schema for scenario content validation
- Example pack: `packs/scenario-tutorial/` with defense tutorial, survival challenge, resource race

#### Infrastructure & Tooling
- Justfile with common development commands
- Global .NET SDK pinning via global.json for CI consistency
- Enhanced CI/CD pipeline with lint and test gates
- Policy gate upgrades for stricter validation

### Fixed

- Suppressed CA1416 (Windows API availability) warnings in Installer project
- Fixed lint issues across all projects for CI compliance
- Corrected dependency versions in csproj files
- Resolved CI build consistency issues with SDK version pinning

### Changed

- Reorganized project structure to accommodate domain plugins and IPC bridge
- Enhanced error handling in domain plugin loaders
- Improved validation messages in scenario and economy systems

## [0.3.0] - 2026-03-10

### Added

#### M4: Warfare Domain Plugin
- `ArchetypeRegistry` with 3 faction archetypes (Order, Industrial Swarm, Asymmetric)
- `DoctrineEngine` applying modifier chains with validation and stat bounds checking
- `UnitRoleValidator` validating faction rosters against 11 required role slots
- `WaveComposer` for generating wave sequences with tier-based unit selection and difficulty scaling
- `BalanceCalculator` with power rating formula and faction comparison
- `WarfarePlugin` entry point with full pack validation
- Warrior unit role archetypes with role distribution matrices
- Squad composition system with command authority tracking
- Skill definition system for unit and faction abilities
- 31 warfare domain unit tests

#### M5: Example Packs
- `warfare-modern` pack: 26 West units (West faction vs Classic Enemy), 16 weapons, 10 waves
- `warfare-starwars` pack: 26 Republic vs CIS units, 19 weapons, 10 waves
- `warfare-guerrilla` pack: 13 Guerrilla units, 13 weapons, 10 waves
- Pack manifests with proper version and dependency constraints
- Themed faction definitions with accurate stat distributions
- Complete unit rosters with role assignments

#### M6: In-Game Mod Menu & Hot Module Replacement
- `ModMenuOverlay`: F10-toggled IMGUI window with pack list, enable/disable toggles, status bar
- `ModSettingsPanel`: BepInEx ConfigEntry wrapper with auto-discovered settings UI
- `PackFileWatcher`: FileSystemWatcher-based HMR with 500ms debounce, thread-safe reload
- `HotReloadResult`: immutable result type (Success/Failure/Partial)
- `HotReloadBridge`: connects SDK HMR to BepInEx logger and ECS runtime
- UI Domain Plugin stubs: `UIPlugin`, `MenuManager`, `HUDInjectionSystem`
- F10 hotkey configuration with toggling support

#### M7: Installer & Universe Bible System
- `Install-DINOForge.ps1`: PowerShell installer with auto-detect Steam, BepInEx download, --Dev flag
- `install.sh`: Bash installer for Linux/Steam Deck
- `SteamLocator`: Windows registry + libraryfolders.vdf parsing for DINO install
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

#### VitePress Documentation Site
- Documentation source in `docs/` with VitePress configuration
- GitHub Pages deployment via Actions
- Navigation structure covering runtime, SDK, domains, tools, packs
- Mermaid diagram support for architecture visualization
- Dark theme configuration for readability
- Automated deployment pipeline

#### CI/QA Infrastructure
- GitHub Actions workflow for build + test + lint
- 200+ test cases covering SDK, domain plugins, and packs
- Test harness with bridge protocol integration tests
- Dependabot configuration for automated dependency updates
- Lint gates with code style enforcement

### Fixed

- Corrected `NoAllocReadOnlyCollection` IEnumerable cast error in SystemEnumerator
- Fixed DebugOverlay accessing `World.Systems` with proper index-only access
- Resolved MonoBehaviour lifecycle incompatibility (ECS-first architecture)
- Fixed PackCompiler CLI for System.CommandLine 2.0.3 API changes (SetAction, mutable collections)
- Updated YamlSchemaConverter for proper YAML-to-JSON scalar type coercion

### Changed

- Reorganized SDK to support domain-specific validation subsystems
- Enhanced error messages for pack loading and validation failures
- Improved schema validation error reporting with detailed context
- Updated all example packs with correct faction definitions

## [0.2.0] - 2025-Q1

### Added

#### M2: Generic Mod SDK
- `PackManifest` + `PackLoader`: YAML manifest parsing via YamlDotNet
- `PackDependencyResolver`: Kahn's algorithm for topological sort, conflict detection
- `NJsonSchemaValidator`: schema validation wrapping NJsonSchema library
- `Registry<T>`: generic typed registry with layered overrides (BaseGame → Framework → DomainPlugin → Pack)
- `RegistryManager`: typed registries for Units, Buildings, Factions, Weapons, Projectiles, Doctrines, Skills, Waves, Squads
- Content models: UnitDefinition, FactionDefinition, WeaponDefinition, ProjectileDefinition, DoctrineDefinition, BuildingDefinition, SkillDefinition, WaveDefinition, SquadDefinition
- `ContentLoader`: orchestrates pack loading from directory to registry
- 10 JSON schemas (pack-manifest, unit, faction, weapon, projectile, doctrine, building, skill, wave, squad)
- Example pack: `packs/example-balance/` with units, buildings, factions
- 46 SDK unit tests

#### M3: Dev Tooling
- `PackCompiler` CLI with commands: `validate`, `build`, `assets list/inspect/validate`
- `DumpTools` CLI with commands: `list`, `analyze`, `components`, `systems`, `namespaces`
- Offline dump analysis capabilities with detailed output
- Spectre.Console-based pretty printing for CLI tools

#### ECS Bridge Layer
- `ComponentMap`: 30+ mappings between DINO ECS components and SDK model fields
- `EntityQueries`: helper queries for player units, enemy units, buildings by class/type
- `StatModifierSystem`: ECS system for applying mod stat changes (Override/Add/Multiply)
- `VanillaCatalog`: runtime scanner classifying vanilla entities into registry IDs
- `AssetSwapSystem`: skeleton for total conversion asset replacement

#### Asset Pipeline
- AssetsTools.NET 3.0.4 integration for asset bundle reading/writing
- `AssetService`: ListBundles, ListAssets, ExtractAsset, ValidateModBundle
- `AddressablesCatalog`: parses DINO's Addressables catalog.json (492 entries)
- Asset validation against game bundle structure

### Fixed

- Corrected YamlSchemaConverter YAML-to-JSON conversion for proper scalar type coercion
- Fixed CLI dependency version upgrades for System.CommandLine 2.0.3

### Changed

- SDK now exports high-level APIs hiding ECS internals
- Registry system supports layered overrides instead of simple replacement
- Improved validation error messages with context information

## [0.1.0] - 2024-Q4

### Added

#### M0: Reverse-Engineering Harness
- BepInEx 5.4.23.5 runtime plugin targeting `netstandard2.0`
- ECS `DumpSystem` (SystemBase) that survives MonoBehaviour destruction
- `EntityDumper`: serializes worlds, archetypes, component types, entity samples to JSON
- `SystemEnumerator`: enumerates all registered ECS systems with metadata
- `DebugOverlay`: F9 IMGUI overlay showing live ECS world state
- First gameplay dump: 45,776 entities across 6 worlds, 500K lines of data
- 6 unit tests for dump infrastructure

#### M1: Runtime Scaffold
- Bootstrap plugin entry point with proper ECS system registration
- Version detection and compatibility checks
- Logging surfaces via BepInEx logger integration
- ECS introspection and system enumeration
- Debug overlay foundation for in-game diagnostics
- Component type discovery and introspection
- Runtime configuration via BepInEx ConfigFile

#### Project Foundation
- DINOForge.sln with organized project structure
- Directory.Build.props with shared MSBuild properties
- Game path configuration for automated deployment
- Initial csproj files for Runtime and SDK layers
- NuGet package references for dependencies (BepInEx, Unity.Entities, etc.)

### Documentation

- PRD defining DINOForge as a general-purpose DINO mod platform
- ADR-001 through ADR-008 (agent-driven dev, declarative arch, pack system, registry model, ECS integration, domain plugins, observability, wrap-don't-handroll)
- Warfare domain specification with faction archetypes and unit role matrix
- CLAUDE.md agent governance document
- Pack manifest, faction, and unit YAML schemas
- Module ownership map and extension point documentation

---

## Comparison & Release Links

[Unreleased]: https://github.com/KooshaPari/Dino/compare/v0.5.0...HEAD
[0.5.0]: https://github.com/KooshaPari/Dino/compare/v0.4.0...v0.5.0
[0.4.0]: https://github.com/KooshaPari/Dino/compare/v0.3.0...v0.4.0
[0.3.0]: https://github.com/KooshaPari/Dino/compare/v0.2.0...v0.3.0
[0.2.0]: https://github.com/KooshaPari/Dino/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/KooshaPari/Dino/releases/tag/v0.1.0
