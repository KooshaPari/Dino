# Changelog

All notable changes to DINOForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added

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
