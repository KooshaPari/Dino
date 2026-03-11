# Changelog

All notable changes to DINOForge will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- **UGUI medieval redesign** — replaced all legacy IMGUI windows with a proper UGUI Canvas-based overlay stack aligned to the "Diplomacy is Not an Option" medieval RTS aesthetic. New files: `DFCanvas.cs` (root Canvas manager, F9/F10/Escape wiring, slide-in animation), `ModMenuPanel.cs` (full mod menu with card list, detail pane, amber left-border enabled indicator, ERR/CONF badges, fade+slide animation), `DebugPanel.cs` (collapsible sections: Platform Status / ECS Worlds / Systems / Archetypes / Errors; Copy Errors to clipboard), `HudStrip.cs` (always-visible 200×32px top-right strip with pack count, green/red status dot, click-to-open, 3s auto-dismiss toasts), `UiBuilder.cs` (static factory: MakePanel, MakeText, MakeButton, MakeScrollView, MakeInputField, MakeToggle, MakeHorizontalSeparator), `UiAssets.cs` (optional sprite registry for 9-sliced backgrounds; flat-colour fallback always active). Palette: `#0d1a0f` background · `#1c2b1e` surface · `#e8d5b0` parchment text · `#c9a84c` amber gold accent · `#4caf7d` success · `#e05252` error.
- **`DinoForgeStyle`** — static IMGUI style kit (dark navy theme, gold accent, lazy-initialized `GUIStyle` instances, `StatusBadge` helper) used by the IMGUI fallback path and legacy overlays
- **`ModMenuOverlayProxy`** — thin `ModMenuOverlay` subclass that forwards `SetPacks`/`SetStatus` to the UGUI `ModMenuPanel` without modifying `ModPlatform`
- **IMGUI fallback** — old `ModMenuOverlay` and `DebugOverlayBehaviour` kept intact; `RuntimeDriver` falls back to them if the UGUI canvas setup throws an exception
- **`HudIndicator`** — IMGUI companion HUD strip (always visible, top-right) showing pack/error count and toast queue; used in IMGUI fallback mode
- **AssetSwapSystem write path** — `AssetService.ReplaceAsset(bundlePath, assetName, newData, outputPath)` patches vanilla Addressables bundles at runtime using AssetsTools.NET 3.0.4 write APIs (`SetNewData` + `AssetsFileWriter` + bundle `Write()`); `AssetService.FindBundlesWithType(typeName)` filters bundles by Unity class name. `AssetSwapRegistry` (SDK/Assets/) provides a thread-safe static registry for mod packs to register `AssetSwapRequest` entries; `AssetSwapSystem` (Runtime/Bridge/) drains pending swaps each ECS update cycle after a 600-frame warmup, writes patched bundles to `BepInEx/dinoforge_patched_bundles/`, and falls back to in-memory RenderMesh entity swaps for live visual changes without scene reload.
- **Kenney CC0 UI sprites + `UiAssets` loader** — `src/Runtime/UI/Assets/UiAssets.cs` loads Kenney CC0 UI Pack PNG sprites from disk at runtime; `UiBuilder.MakePanel()` and `MakeButton()` use 9-sliced sprites when available, falling back silently to flat colours. `src/Runtime/UI/Assets/README.md` documents four CC0 packs with direct download URLs and PowerShell/Bash setup scripts. MSBuild `DeployUiAssets` target copies sprites to `BepInEx/plugins/dinoforge-ui-assets/` when `GameInstalled=true`. `UiAssets.Initialize()` called from `RuntimeDriver` at startup; missing files logged via `UiAssets.MissingFiles`.
- **Native DINO menu injection** — `NativeMenuInjector` MonoBehaviour scans active UGUI canvases on scene load and injects a "Mods" button adjacent to the Settings button, wired to toggle the DINOForge mod menu overlay
- **`NativeUiHelper`** — static UGUI utility class with `FindButtonByText`, `CloneButton`, `PositionAfterSibling`, and `SetButtonText`; handles both legacy `UnityEngine.UI.Text` and TMPro via reflection
- `RuntimeDriver` wires `NativeMenuInjector` after the other UI components; `SetLogger` + `SetModMenuOverlay` wiring points

### Fixed
- **CI: remove `./local-packages` from nuget.config** — caused NU1301 failures on every GitHub Actions build
- **Installer: silent crash after UAC** — added `AppDomain.UnhandledException`, task exception handler, try/catch around Avalonia startup, and native Win32 `MessageBox` crash dialog; crash log written to `%LOCALAPPDATA%\DINOForge\installer-crash.log`
- **PackCompiler: `DefaultValue` API** — updated to `DefaultValueFactory` for System.CommandLine 2.0 compatibility

### Infrastructure & Quality
- `.gitattributes` — normalize all source files to LF (fixes `dotnet format` ENDOFLINE errors on Linux CI)
- `packages.lock.json` generated for all 17 projects (reproducible NuGet restore in CI)
- PRD updated to v0.5.0 reflecting current state (M9-M11 complete)
- ROADMAP updated: M9/M10/M11 complete, M12/M13 in progress, M14/M15 scoped out
- Current test coverage: 416+ tests (402 unit + 14 integration) with 60%+ enforcement
- CI/QA infrastructure: MinVer versioning, NetArchTest validation, CycloneDX SBOM, Scorecard security analysis
- Thunderstore distribution support integrated

### Added

#### M12: Polyrepo + Submodule Support
- `dinoforge pack add` — Add pack repositories as git submodules
- `dinoforge pack list` — List installed pack submodules from .gitmodules
- `dinoforge pack update` — Update all pack submodules to latest remote versions
- `dinoforge pack lock` — Generate packs.lock file for reproducible builds
- `packs.lock` file format: path + commit SHA pairs for exact pack versions
- PackSubmoduleTests: 5 unit tests for repo name extraction, .gitmodules parsing, lock file format
- `packs/README.md` — Guide for managing official and community packs

#### M13: Total Conversion Framework
- `TotalConversionManifest` model for total conversion pack definitions
- `TotalConversionValidator` with completeness and consistency checks
- `AssetReplacementEngine` for vanilla → mod asset mapping and fallback resolution
- `total-conversion.schema.json` JSON Schema for pack validation
- `PackCompiler validate-tc` command for manifest validation with detailed reporting
- Example `warfare-starwars` pack (Star Wars: The Clone Wars total conversion)
- 24+ unit and integration tests for total conversion subsystem

#### Versionize & Release Automation
- Versionize conventional-commits based version automation workflow
- .versionize config for GitHub URL formats in changelog (commits, tags, issues, users)
- SHA256SUMS.txt generated automatically for all release artifacts
- Enhanced version-bump.yml workflow with dry-run support and automatic tagging

#### Thunderstore Distribution Support
- PackCompiler `thunderstore` command: generates Thunderstore-compatible manifest.json for r2modman/TMM compatibility
- Automatic Thunderstore manifest generation during `build` command
- Manifest includes Thunderstore package naming (Author-PackId format), BepInEx dependency, and description truncation to 250 chars

#### CI/Build Optimization & Reproducibility
- NuGet package lock files for reproducible builds (RestorePackagesWithLockFile)
- CI NuGet caching via setup-dotnet built-in cache (cache-dependency-path: packages.lock.json)
- RestoreLockedMode enabled in CI to enforce lock file consistency
- Parallel xunit test execution in CI (xunit.parallelizeAssembly=true)
- TRX test results upload as CI artifacts for visibility

#### Testing & Architecture Validation
- NetArchTest architecture enforcement tests (SDK layer isolation from Runtime and Domains)
- AutoFixture test data generation package for improved test fixtures
- Code coverage collection (Cobertura format) with 60% line threshold in CI
- Coverage report artifacts uploaded to GitHub Actions with 14-day retention

#### Versioning & Security Infrastructure
- MinVer git-tag-based versioning for all .NET projects (automatic version detection from git tags with `v` prefix)
- NuGet security audit (moderate threshold) via Directory.Build.props to fail CI on vulnerable packages
- Dependabot weekly updates for NuGet packages and GitHub Actions with package grouping (Microsoft/System, Testing, Avalonia, Stryker)
- Automated dependency PR labeling and scheduling (Mondays at default time)
- Unity package exclusion from major version updates to maintain game compatibility
- OpenSSF Scorecard security analysis workflow (weekly + push to main)
- CycloneDX SBOM generation for SDK and Runtime projects
- SLSA L2 build provenance attestations on release artifacts

#### M9: Unit Spawning & Wave Injection System
- M9: **PackUnitSpawner** - clone-and-override ECS system for spawning pack-defined units with full ECS archetype support
- M9: **PackUnitSpawner** ECS SystemBase for cloning vanilla entity archetypes from pack definitions
- M9: **VanillaArchetypeMapper** maps pack unit class strings to ECS component types
- M9: **UnitSpawnRequest** queue system with faction tagging and stat override support
- M9: **FactionSystem** - runtime faction registry and entity tagging via Enemy component marker
- M9: **WaveInjector** - translates pack wave definitions to timed unit spawn sequences with stagger support
- M9: **IUnitFactory**, **IFactionSystem**, **IWaveInjector** SDK interfaces for mod extensibility
- M9: Version compatibility matrix (compat.json, CompatibilityChecker) for pack dependency resolution
- Pack registry metadata field: `requires_spawner` flag for UI compatibility warnings
- ModPlatform system registration for all M9 systems with error isolation
- PackCompiler `--format json` flag for machine-parseable output (agent-first tooling)
- `GetUnitsByComponentType()` query helper in EntityQueries

### Changed

- Pack registry schema now includes optional `requires_spawner` boolean field
- Updated warfare pack entries (modern, starwars, guerrilla) to flag M9 dependency
- Documentation updated to clarify M9+ requirements for total conversion packs

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

## [0.4.0] - 2026-03-11

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

#### Economy Domain Plugin (Early Preview)
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

#### Scenario Domain Plugin (Early Preview)
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

### Fixed

- Corrected damage calculation paths for stat modifiers
- Fixed unit role validation against faction rosters
- Resolved scenario condition evaluation edge cases

### Changed

- Added early preview tags to Economy and Scenario domain plugins
- Enhanced wave composition algorithm for difficulty scaling

## [0.3.0] - 2026-03-10

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

#### M6: In-Game Mod Menu & Hot Module Replacement
- `ModMenuOverlay`: F10-toggled IMGUI window with pack list, enable/disable toggles, status bar
- `ModSettingsPanel`: BepInEx ConfigEntry wrapper with auto-discovered settings UI
- `PackFileWatcher`: FileSystemWatcher-based HMR with 500ms debounce, thread-safe reload
- `HotReloadResult`: immutable result type (Success/Failure/Partial)
- `HotReloadBridge`: connects SDK HMR to BepInEx logger and ECS runtime
- UI Domain Plugin stubs: `UIPlugin`, `MenuManager`, `HUDInjectionSystem`
- F10 hotkey configuration with toggling support

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

- Corrected YamlSchemaConverter YAML-to-JSON conversion for proper scalar type coercion
- Fixed CLI dependency version upgrades for System.CommandLine 2.0.3
- Corrected `NoAllocReadOnlyCollection` IEnumerable cast error in SystemEnumerator
- Fixed DebugOverlay accessing `World.Systems` with proper index-only access
- Resolved MonoBehaviour lifecycle incompatibility (ECS-first architecture)
- Fixed PackCompiler CLI for System.CommandLine 2.0.3 API changes (SetAction, mutable collections)
- Updated YamlSchemaConverter for proper YAML-to-JSON scalar type coercion

### Changed

- SDK now exports high-level APIs hiding ECS internals
- Registry system supports layered overrides instead of simple replacement
- Improved validation error messages with context information
- Reorganized SDK to support domain-specific validation subsystems
- Enhanced error messages for pack loading and validation failures
- Improved schema validation error reporting with detailed context
- Updated all example packs with correct faction definitions

## [0.2.0] - 2026-03-10

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

### Fixed

- Resolved initial ECS introspection challenges with proper system enumeration

### Changed

- Established foundation for polyrepo-hexagonal architecture

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
