# DINOForge v1.1 Roadmap

**Document Version**: 1.0
**Created**: 2026-03-12
**Status**: APPROVED
**Target Release**: 2026-05-12 (8 weeks)
**Estimated Effort**: 160-200 hours
**Team Size**: 1 Human PM + 3-4 Agents (specialized roles)

---

## Executive Summary

This document outlines the **v1.1 release roadmap** for DINOForge, the mod platform for Diplomacy is Not an Option. v1.1 focuses on **completing the asset pipeline**, **shipping production-ready warfare packs**, and **polishing end-user experience**.

**Current State (v0.3.0-dev)**:
- 383 unit tests passing (369 core + 14 integration)
- 3 warfare packs complete (Modern, Star Wars, Guerrilla) with full mechanics
- Asset infrastructure: 26 unit textures generated, 4/24 building FBX models ready, comprehensive sourcing plans
- Framework: Runtime, SDK, Registry, Domain plugins all stable
- Tooling: PackCompiler, DumpTools, Inspector, CLI all functional

**v1.1 Goals**:
1. **Ship production-ready assets** for all 3 warfare packs (buildings + VFX)
2. **Optimize asset loading** via Addressables integration (bundle streaming, memory management)
3. **Polish UX** with faction-specific UI theming and HUD customization
4. **Validate completeness** with comprehensive testing, balance audits, and performance profiling
5. **Release v1.0 candidates** (three themed warfare total conversions)

**Success Criterion**: Players can install any warfare pack and play a full campaign with all assets loading correctly, balanced gameplay, and faction-specific theming.

---

## Detailed Scope & Effort Estimates

### 1. Building FBX Completion (20-30 hours)

**Objective**: Export and integrate remaining 20 building FBX models from Kenney.nl source packs.

**Current State**:
- 4 FBX models complete: `rep_house_clone_quarters`, `cis_house_droid_pod`, `rep_farm_hydroponic`, `cis_farm_fuel_harvester`
- 20 buildings remaining (Tier 1: 8, Tier 2: 8, Tier 3: 7)
- Batch assembly plan documented in `BLENDER_ASSEMBLY_PLAN.md`
- Faction color schemes defined (Republic white/blue, CIS orange/dark)
- Polygon budgets verified (280-340 triangles per building)

**Deliverables**:
- [ ] Tier 1 buildings (8): simple structures, ~2.5 hrs each = 20 hrs
  - Houses, Farms, Granaries, Storage depots (2× faction variants = 16 models)
  - Pyramid: Unit model (4 models, shared geometry)
- [ ] Tier 2 buildings (8): medium complexity, ~3.5 hrs each = 28 hrs
  - Barracks, Stables, Towers, Walls (2× faction variants = 16 models)
  - Pyramid: Unit model (4 models, geometry variations)
- [ ] Tier 3 buildings (7): complex structures, ~4 hrs each = 28 hrs
  - Temple, Vault, Academy, Fort, Keep, Palace, Gate (2× faction variants = 14 models)
  - Pyramid: Shared geometry (7 models, custom details)
- [ ] Faction color variants: HSV-shift textures for orange/dark CIS theme
- [ ] Export validation: reimport checks, scale verification, shader preview
- [ ] Documentation: batch assembly completion record

**Breakdown**:
| Tier | Buildings | Count | Hrs/Model | Total Hours |
|------|-----------|-------|-----------|-------------|
| 1    | Basic     | 8 (×2 faction) | 2.5 | 20 |
| 2    | Medium    | 8 (×2 faction) | 3.5 | 28 |
| 3    | Complex   | 7 (×2 faction) | 4.0 | 28 |
| Validation & Polish | - | - | - | 5 |
| **Total** | 24 buildings | 48 models | - | **20-30 hrs** |

**Success Criteria**:
- All 24 buildings (48 faction variants) have production FBX models
- Models < 400 triangles (target 280-340)
- Textures applied and visible in Blender preview
- All models reimport correctly in Unity (scale, rotation, pivot)
- Documentation complete with timeline & troubleshooting

**Dependencies**:
- Blender 4.0+ installed
- Kenney.nl source FBX files downloaded
- Faction texture overlays ready (from unit texture pipeline)

**Risks**:
- **Texture seams/colors**: CIS faction variants may have color banding if HSV shift not properly tuned
  - *Mitigation*: Sample 2-3 buildings first, validate color palette before batch
- **Export quality**: Normals/tangents may break on complex models during FBX export
  - *Mitigation*: Reimport test cycle on each batch before proceeding
- **Time overrun**: Complex Tier 3 buildings may take 5-6 hours if custom geometry needed
  - *Mitigation*: Parallelize with 2-artist team (1 Rep specialist, 1 CIS specialist)

**Agent Assignment**: Asset-generation agent (high parallelization, low complexity review)

---

### 2. Addressables Integration (30-40 hours)

**Objective**: Integrate Unity Addressables system for efficient asset streaming and memory management.

**Current State**:
- DINO uses Unity Addressables v1.21.18 (NOT classic AssetBundles)
- Main game assets: `StreamingAssets/aa/StandaloneWindows64/` (4.2GB + 2.1GB bundles)
- No current bundling for custom DINOForge assets
- ContentLoader exists but does not leverage Addressables for streaming

**Deliverables**:
- [ ] **AddressablesManager wrapper** (~6 hrs)
  - Thin adapter around Addressables API
  - Lazy-load building FBX models on-demand
  - Lazy-load faction UI textures and HUD elements
  - Memory pooling for frequently-reused assets (projectiles, effects)
  - Async load support with progress reporting
  - Error handling with fallback/graceful degradation
- [ ] **Asset bundle configuration** (~8 hrs)
  - Group buildings by faction (Rep buildings, CIS buildings)
  - Group UI by faction (Rep HUD colors, CIS HUD colors)
  - Separate bundle for VFX particles (projectiles, impacts, abilities)
  - Compression: LZ4 (fastest) for streaming, LZMA (best) for downloads
  - Target bundle size: 50-150 MB per faction (balance load time vs memory)
- [ ] **Build pipeline updates** (~6 hrs)
  - Add `dotnet run -- build-addressables <pack>` command to PackCompiler
  - Validate bundle references before build
  - Generate addressables catalog at pack build time
  - Embed catalog in pack manifest
- [ ] **Memory profiling & optimization** (~10 hrs)
  - Profile memory usage with all assets loaded (target: < 500 MB overhead)
  - Implement unload on-demand (buildings not visible in viewport)
  - Cache shared geometry (faction variants) in memory
  - Benchmark loading times (target: 2-5 sec per building type on SSD)
- [ ] **Integration tests** (~6 hrs)
  - Asset availability assertions (all addressables resolvable)
  - Load/unload cycle tests (no memory leaks)
  - Fallback activation tests (graceful behavior when asset missing)
  - Performance benchmarks (load time, memory delta)
- [ ] **Documentation** (~4 hrs)
  - Asset pipeline guide for pack authors
  - Addressables configuration reference
  - Troubleshooting guide (missing assets, bundle errors)

**Breakdown**:
| Component | Hours |
|-----------|-------|
| AddressablesManager API | 6 |
| Bundle configuration & grouping | 8 |
| Build pipeline integration | 6 |
| Memory profiling & optimization | 10 |
| Integration tests | 6 |
| Documentation | 4 |
| **Total** | **30-40 hrs** |

**Success Criteria**:
- All faction building models load via Addressables on-demand
- Memory overhead < 500 MB with all packs loaded
- Load time < 2 seconds per building type (SSD, modern hardware)
- Bundle size targets met: 50-150 MB per faction
- Zero memory leaks over 1-hour gameplay session
- 95%+ asset availability (graceful fallback for missing assets)

**Dependencies**:
- Unity.Addressables NuGet package (MIT license, already available)
- DINO game with Addressables system enabled (stock configuration)
- Building FBX models complete (from Section 1)

**Risks**:
- **Platform differences**: DINO runs on Mono (not IL2CPP), Addressables behavior may differ
  - *Mitigation*: Early testing with actual DINO runtime via bridge IPC
- **Asset path conflicts**: Custom packs may clash with vanilla Addressables groups
  - *Mitigation*: Use namespaced catalog keys (e.g., `warfare-starwars/buildings/rep_house`)
- **Bundle versioning**: Game updates may invalidate old bundle catalogs
  - *Mitigation*: Version catalog along with pack version, add migration logic

**Agent Assignment**: SDK/integration agent (moderate complexity, requires ECS Bridge knowledge)

---

### 3. VFX System (40-60 hours)

**Objective**: Implement visual effects for projectiles, impacts, ability activation, and environmental effects.

**Current State**:
- Warfare pack mechanics complete (doctrines, units, weapons all data-driven)
- No custom VFX yet (using placeholder particle systems from DINO vanilla)
- 2 doctrine types with active effects: Area Denial (projectile trails), Overcharge (impact explosions)
- Faction themes defined (Republic: blue/glow, CIS: orange/dark energy)

**Deliverables**:

#### 3.1 Projectile VFX (~16 hrs)
- [ ] **Blaster bolt effects** (4 hrs)
  - Republic: blue energy trail with glow halo
  - CIS: orange energy trail with crackling edges
  - Guerrilla: simple green tracer (low-cost for asymmetry)
  - Particle prefab per faction (reusable, pooled)
  - Material with emission + glow for dark scenes
- [ ] **Laser/cannon effects** (4 hrs)
  - Heavy weapons: larger projectiles with persistent trails
  - Slow-moving mortars: arcing trajectories with smoke trail
  - Fast projectiles: short sharp bursts
- [ ] **Special projectiles** (4 hrs)
  - Grenades: spinning geometry with impact spark burst
  - Poison/gas: expanding cloud with fade
  - Healing/support: gentle glow aura (blue/green)
- [ ] **Particle pooling** (4 hrs)
  - ParticleEffectPool manager for projectile VFX
  - Reuse particles across frames (reduce GC pressure)
  - Async instantiation from Addressables (non-blocking)

#### 3.2 Impact Effects (~12 hrs)
- [ ] **Melee impact** (3 hrs)
  - Dust puff on ground hit
  - Weapon spark on armor hit
  - Faction-colored damage flash on unit health bar
- [ ] **Explosion effects** (4 hrs)
  - Area Denial doctrine activation (expanding ring of particles)
  - Building destruction (multi-phase collapse with debris)
  - Unit death (small explosion proportional to unit size)
- [ ] **Terrain/environmental** (3 hrs)
  - Scorched earth on fire damage
  - Frozen surface on cold effects
  - Glowing craters for energy weapons
- [ ] **Damage indicators** (2 hrs)
  - Floating damage numbers (optional, UI-driven)
  - Direction-of-damage arrows (pointing to attacker)

#### 3.3 Ability Activation Effects (~12 hrs)
- [ ] **Doctrine activation** (6 hrs)
  - Unit aura that pulses when ability active
  - Overcharge: crackling electrical fields, screen flash
  - Area Denial: expanding shockwave ring
  - Mobility Boost: speed trails, afterimage effect
- [ ] **Buff/debuff visuals** (3 hrs)
  - Positive buff: cyan/green glow on unit model
  - Negative debuff: red/orange glow or darkening
  - Status icons above unit head (tied to HUD system)
- [ ] **Building activation** (3 hrs)
  - Production queue activation: material emission pulse
  - Research completion: research-color glow
  - Defense activation: shield shimmer effect

#### 3.4 Environmental Effects (~8 hrs)
- [ ] **Weather/time of day** (4 hrs)
  - Adaptive particle density (fog, rain reduced in performance mode)
  - Lighting interaction (particle emission color + sun direction)
  - Seasonal color shifts (autumn leaves, winter snow tint)
- [ ] **Lighting FX** (2 hrs)
  - Bioluminescent plants (Guerrilla faction aesthetic)
  - Neon signage (Modern faction aesthetic)
  - Holy light rays (Republic aesthetic)
- [ ] **Audio/VFX sync** (2 hrs)
  - Particle birth matched to sound effect timing
  - Impact sound intensity scaled with projectile velocity
  - Ability activation sound + particle synchronized

#### 3.5 VFX Configuration System (~8 hrs)
- [ ] **Declarative VFX definitions** (~4 hrs)
  - YAML schema for particle effects (emitter settings, colors, lifetime)
  - Example: `projectile-blaster-republic.yaml`
  - Material overrides per faction
  - Performance cost metadata (particle count limits)
- [ ] **VFX registry** (~2 hrs)
  - Registr VFX by ID in Warfare domain
  - Associate VFX with unit weapons, doctrines, abilities
  - Lazy-load prefabs on first use via Addressables
- [ ] **Fallback system** (~2 hrs)
  - Graceful degradation if VFX assets missing
  - Performance reduction mode (disable expensive effects)
  - Debug visualization (show effect bounds, lifetimes)

#### 3.6 Testing & Optimization (~4 hrs)
- [ ] **Visual validation tests**
  - Screenshot-based regression tests for VFX appearance
  - Automated frame rate tests (target 60 FPS with VFX enabled)
  - GC profiling (particle pooling reduces allocations)
- [ ] **Performance benchmarks**
  - Profiler integration (Unity Frame Debugger compatible)
  - VFX cost reporting in debug overlay
  - Guidelines for pack authors (max particles per projectile)

**Breakdown**:
| Component | Hours |
|-----------|-------|
| Projectile VFX | 16 |
| Impact effects | 12 |
| Ability activation effects | 12 |
| Environmental effects | 8 |
| VFX configuration system | 8 |
| Testing & optimization | 4 |
| **Total** | **40-60 hrs** |

**Success Criteria**:
- All 3 factions have faction-specific projectile VFX (9 variants: 3 projectile types × 3 factions)
- Impact effects visible on unit/building damage
- Doctrines trigger visual feedback (Overcharge crackling, Area Denial pulse, Mobility glow)
- 60 FPS sustained with all VFX enabled (measured at 1080p on GTX 1070 equivalent)
- Addressables loading for all VFX prefabs (non-blocking)
- No memory leaks after 100+ particle spawns/despawns

**Dependencies**:
- Particle System expertise (Unity particles, not custom shaders)
- Shader knowledge for faction-color material variants (post-processing or material parameters)
- VFX assets (particle textures, sparkles, glow maps) - can source from Kenney.nl or generate procedurally
- Sound design coordination (SFX timing sync)

**Risks**:
- **Performance cliff**: Adding VFX can spike CPU/GPU usage, especially on lower-end hardware
  - *Mitigation*: Establish budget (500 particles max per frame), profile early, offer performance mode toggle
- **Asset compatibility**: DINO's particle system may have limited capabilities (Burst compiled, specific constraints)
  - *Mitigation*: Implement on mock ECS first, validate with bridge before shipping
- **Faction aesthetic inconsistency**: VFX colors may not match faction unit textures
  - *Mitigation*: Export faction color palettes as data, drive VFX material parameters from palette

**Agent Assignment**: VFX/graphics agent (specialized, requires particle system expertise and visual validation)

---

### 4. Faction UI Theming (20-30 hours)

**Objective**: Create faction-specific UI themes (HUD colors, menus, unit portraits, minimap).

**Current State**:
- All 3 factions have gameplay mechanics complete
- Vanilla DINO HUD used for all factions (no custom theming)
- Unit portraits are procedurally generated 2D sprites (no faction-specific art)
- Menu system exists but no faction-specific skinning

**Deliverables**:

#### 4.1 HUD Color Theming (~8 hrs)
- [ ] **Color palette system** (~2 hrs)
  - Define faction color scheme (primary, secondary, accent, warning, danger)
  - Republic: white/blue/gold, accent cyan
  - CIS: orange/dark brown/black, accent purple
  - Guerrilla: green/gray/tan, accent yellow
  - Serialized in YAML (reusable across UI elements)
- [ ] **HUD element updates** (~4 hrs)
  - Health bar: faction color gradient
  - Resource counter: accent color text
  - Status icons: faction-themed borders
  - Selection highlight: primary color border
  - Unit/building UI panel: faction color header
- [ ] **Minimap customization** (~2 hrs)
  - Unit markers: faction-specific icons
  - Building ownership: faction color fill
  - Fog of war: faction-specific desaturation
  - Important landmarks: faction-themed symbols

#### 4.2 Menu System Theming (~8 hrs)
- [ ] **Main menu reskin** (~2 hrs)
  - Faction selection background images
  - Logo/title in faction colors
  - Button styles (faction-themed borders/glow)
- [ ] **Pause menu theming** (~2 hrs)
  - Background music continuity (faction-specific score plays in menu)
  - Button highlights match active faction color
  - Stat panels styled with faction aesthetic
- [ ] **Settings panel** (~2 hrs)
  - Faction-specific toggle options (language, difficulty, VFX intensity)
  - Color preview for HUD theming
  - Audio volume per faction music track
- [ ] **Tooltips & info panels** (~2 hrs)
  - Faction background lore (shown on unit/building hover)
  - Stat comparison tables (styled with faction colors)
  - Doctrine description panels

#### 4.3 Unit Portrait Generation (~6 hrs)
- [ ] **Portrait generation pipeline** (~4 hrs)
  - Procedural portraiture from unit 3D model (sprite render)
  - Faction-specific pose & background (Rep formal, CIS mechanical)
  - Lighting matched to faction aesthetic (warm for Rep, cold for CIS)
  - Generate on pack load, cache to disk
- [ ] **Portrait art directives** (~2 hrs)
  - Rep units: bright, heroic lighting, golden backgrounds
  - CIS units: dramatic shadows, industrial backgrounds
  - Guerrilla: natural lighting, jungle/urban backgrounds

#### 4.4 Loading Screen & Cinematics (~4 hrs)
- [ ] **Loading screen** (~2 hrs)
  - Faction-specific background image
  - Loading bar color matches faction theme
  - Lore snippet displayed (rotated faction facts)
  - Music plays faction theme (looped)
- [ ] **Victory/Defeat cinematics** (~2 hrs)
  - Faction-specific victory animation (Republic flags, CIS symbols)
  - Camera pan over faction-colored landscape
  - Audio: faction theme with celebratory/tragic variation

#### 4.5 Accessibility & Polish (~4 hrs)
- [ ] **Colorblind mode** (~2 hrs)
  - High-contrast alternative palettes
  - Colorblind-safe unit marker shapes
  - Text labels for all color-coded elements
- [ ] **Performance optimization** (~1 hr)
  - Cache themed materials/UI prefabs
  - Lazy-load faction-specific graphics
- [ ] **QA & validation** (~1 hr)
  - UI screenshot tests per faction
  - Contrast ratio checks (WCAG AA compliance)
  - UX testing with sample users

**Breakdown**:
| Component | Hours |
|-----------|-------|
| HUD color theming | 8 |
| Menu system theming | 8 |
| Unit portrait generation | 6 |
| Loading screen & cinematics | 4 |
| Accessibility & polish | 4 |
| **Total** | **20-30 hrs** |

**Success Criteria**:
- All 3 factions have visually distinct HUD themes (color schemes, icons, UI elements)
- Unit portraits generated per faction at pack load time
- Menu system themed per faction
- Loading screens show faction-specific imagery
- Colorblind mode accessible (high contrast, text labels)
- UI responsive and performant (no jank on dialogue open/close)
- Screenshots pass automated visual regression tests

**Dependencies**:
- UI framework knowledge (DINO's menu system, HUD canvas)
- Color theory and accessibility guidelines
- Portrait rendering capability (sprite generation from 3D models)

**Risks**:
- **UI framework complexity**: DINO's menu system may have hardcoded colors/styles requiring deeper patching
  - *Mitigation*: Use Material parameter overrides + UI prefab variants to minimize patches
- **Portrait generation quality**: Sprite rendering from 3D models may look cartoonish or low-res
  - *Mitigation*: Pre-render portraits to 512×512 PNG and use illustration tool (Photoshop/Krita) for touch-up
- **Colorblind palette testing**: May require specialized tools and user feedback
  - *Mitigation*: Use established colorblind palettes (e.g., Okabe-Ito), test with online simulators, gather community feedback

**Agent Assignment**: UI/graphics agent (moderate complexity, requires design sensibility and accessibility knowledge)

---

### 5. Testing & Polish (20-30 hours)

**Objective**: Comprehensive testing, balance audits, bug fixes, performance profiling, and release readiness.

**Current State**:
- 383 unit tests passing
- 3 warfare packs feature-complete but not QA'd for release
- No integration tests with actual DINO runtime
- Performance profiling not done on full pack load

**Deliverables**:

#### 5.1 Pack Validation & Completeness (~8 hrs)
- [ ] **Schema validation** (~2 hrs)
  - All pack manifests pass schema validation
  - All referenced assets exist (buildings, units, VFX)
  - All texture files present and correctly formatted
  - All YAML files parseable (no syntax errors)
- [ ] **Reference integrity checks** (~2 hrs)
  - Unit equipment references valid (weapon IDs exist)
  - Building production references valid (unit/upgrade IDs exist)
  - Doctrine requirements satisfied (prerequisite units/buildings exist)
  - Research tree acyclic (no circular dependencies)
- [ ] **Content completeness** (~2 hrs)
  - All units have stats defined (HP, armor, DPS, cost, etc.)
  - All buildings have production/defense/economy stats
  - All doctrines have mechanical definitions
  - All factions have wave compositions defined
- [ ] **Test automation** (~2 hrs)
  - PackValidator integration test per pack
  - Automated schema checks in CI/CD
  - Asset existence assertions in test suite

#### 5.2 Balance Audit & Tuning (~10 hrs)
- [ ] **Faction win-rate analysis** (~4 hrs)
  - Simulate 100+ skirmishes per faction pairing
  - Measure win rates (target: 45-55% for each faction)
  - Identify overpowered units (> 60% usage rate in winning armies)
  - Identify underpowered units (< 10% usage rate)
- [ ] **Cost-effectiveness audit** (~3 hrs)
  - Calculate DPS/cost ratio per unit
  - Compare with vanilla DINO units (target parity ±20%)
  - Identify dominant strategies (if only unit type used, rebalance)
  - Adjust unit costs & HP to improve diversity
- [ ] **Doctrine balance** (~2 hrs)
  - Measure doctrine win-rate contribution (with vs without)
  - Check for dominant doctrine combos
  - Tune cooldowns/durations for perceived fairness
- [ ] **Economy balance** (~1 hr)
  - Verify resource gain rates match vanilla (or intentional variants documented)
  - Check building production times reasonable (not trivializing economy)

#### 5.3 Bug Discovery & Triage (~6 hrs)
- [ ] **Integration testing on real DINO** (~3 hrs)
  - Load each pack via bridge IPC
  - Verify all units spawn correctly
  - Verify all buildings construct correctly
  - Verify all doctrines activate correctly
  - Test edge cases (unit capacity constraints, resource limits, victory conditions)
- [ ] **Bug triage & fixing** (~3 hrs)
  - Log discovered bugs with reproduction steps
  - Prioritize by severity (game-breaking > balance > cosmetic)
  - Fix P0/P1 bugs before release
  - Document P2 bugs as known issues

#### 5.4 Performance Profiling (~4 hrs)
- [ ] **Memory profiling** (~2 hrs)
  - Measure memory footprint per pack (target: < 200 MB base + 100 MB per faction)
  - Check for memory leaks (repeated load/unload cycles)
  - Validate Addressables pooling reduces GC pressure
- [ ] **CPU profiling** (~1 hr)
  - Measure frame time with all VFX enabled
  - Target 60 FPS on GTX 1070 equivalent + modern CPU (6-core)
  - Profile heavy frames (battle with 50+ units)
- [ ] **Load time profiling** (~1 hr)
  - Measure pack initialization time (target: < 5 sec)
  - Measure asset loading time (Addressables bundles)
  - Validate no stalls in game loop during loading

#### 5.5 Documentation & Release Notes (~2 hrs)
- [ ] **Release notes** (~1 hr)
  - Features added in v1.1
  - Known issues and workarounds
  - Breaking changes (if any)
  - Updated build/test instructions
- [ ] **Troubleshooting guide** (~1 hr)
  - Common issues (missing assets, crashes, balance complaints)
  - Diagnostic steps
  - Links to support resources

**Breakdown**:
| Component | Hours |
|-----------|-------|
| Pack validation & completeness | 8 |
| Balance audit & tuning | 10 |
| Bug discovery & triage | 6 |
| Performance profiling | 4 |
| Documentation & release notes | 2 |
| **Total** | **20-30 hrs** |

**Success Criteria**:
- All 3 packs pass automated validation (schema, references, completeness)
- Zero P0 bugs (game-breaking) before release
- Faction win rates within 45-55% range (measured from simulated skirmishes)
- < 5 P1 bugs (balance exploits) documented as known issues
- Pack initialization < 5 seconds
- Memory footprint < 300 MB per pack
- 60 FPS sustained in full-scale battles (50+ units + VFX)
- Release notes comprehensive and accurate

**Dependencies**:
- All prior deliverables complete (buildings, Addressables, VFX, UI)
- Test infrastructure (mock ECS, bridge IPC for real DINO testing)
- Balance simulation harness (skirmish runner, win-rate calculator)

**Risks**:
- **Real DINO testing delays**: Bridge IPC may have stability issues or timing quirks
  - *Mitigation*: Start bridge testing early (week 4), allocate buffer time
- **Balance complexity**: Changing one unit stat may cascade (affects doctrine effectiveness, economy timing)
  - *Mitigation*: Use simulation-driven approach (many iterations), document balance rationale
- **Performance regressions**: Adding VFX/UI may introduce unexpected GC pressure
  - *Mitigation*: Profile early and often, establish budget caps, plan optimizations

**Agent Assignment**: QA/testing agent (requires attention to detail, systematic testing, and communication)

---

## Timeline & Milestones

### High-Level Schedule (8 weeks)

```
Week 1-2: Building FBX + Addressables Foundation
  ├─ FBX: Tier 1 completion (8 buildings, all variants)
  ├─ Addressables: API design, bundle config, build integration
  └─ Goal: Asset pipeline functional, 50% of models ready

Week 3: VFX System Development
  ├─ Projectile VFX: blaster, laser, special effects
  ├─ Impact effects: melee, explosion, environmental
  ├─ Ability activation effects
  └─ Goal: All faction-specific projectile VFX in game

Week 4: FBX Completion + UI Theming
  ├─ FBX: Tier 2 + 3 completion (remaining 16 buildings)
  ├─ UI: HUD colors, menu theming, portraits
  ├─ Addressables: Memory profiling & optimization
  └─ Goal: All assets integrated, UI visually distinct per faction

Week 5: Integration Testing & Tuning
  ├─ Test: All packs on real DINO instance
  ├─ Balance: Simulation-based win-rate analysis
  ├─ Bug: Triage & fix P0/P1 issues
  ├─ Performance: Full profiling (memory, CPU, load time)
  └─ Goal: Release candidates prepared

Week 6: Polish & Documentation
  ├─ VFX: Environmental effects, audio sync
  ├─ UI: Loading screens, cinematics, accessibility
  ├─ Docs: Release notes, troubleshooting guide
  ├─ Final balance passes
  └─ Goal: Feature-complete, release-ready

Week 7: Release Candidate Builds
  ├─ Build v1.1.0-rc1 for all 3 packs
  ├─ Community playtest signups
  ├─ Gather feedback on balance, performance, UX
  └─ Goal: RC builds stable, playable end-to-end

Week 8: Final Polish & v1.0 Release
  ├─ Address RC feedback
  ├─ Final balance tweaks based on playtest data
  ├─ Tag v1.0.0 (production release)
  ├─ GitHub release with install instructions
  └─ Goal: Shipping products

```

### Detailed Milestone Dates

| Milestone | Target Date | Status | Owner |
|-----------|-------------|--------|-------|
| M5.1: Building FBX Tier 1 complete | 2026-03-26 | Pending | Asset Agent |
| M5.2: Addressables API & integration | 2026-03-29 | Pending | SDK Agent |
| M5.3: Projectile VFX complete | 2026-04-09 | Pending | VFX Agent |
| M5.4: Building FBX Tier 2+3 complete | 2026-04-16 | Pending | Asset Agent |
| M5.5: UI theming complete | 2026-04-19 | Pending | UI Agent |
| M5.6: Integration testing on DINO | 2026-04-26 | Pending | QA Agent |
| M5.7: Balance audit complete | 2026-04-30 | Pending | QA Agent |
| M5.8: All bugs triaged, P0/P1 fixed | 2026-05-03 | Pending | QA Agent |
| M5.9: Release notes & docs | 2026-05-07 | Pending | PM |
| M5.10: v1.1.0-rc1 tagged | 2026-05-09 | Pending | PM |
| **M5.11: v1.0.0 released** | **2026-05-12** | **Pending** | **PM** |

---

## Effort Breakdown & Resource Planning

### Total Effort Estimate

| Phase | Hours | % of Total | Agent Type | Parallelizable |
|-------|-------|-----------|-----------|-----------------|
| Building FBX | 20-30 | 12-15% | Asset Gen | HIGH (2-3 artists) |
| Addressables | 30-40 | 15-20% | SDK/Integration | MEDIUM (1 agent, some blocking reviews) |
| VFX System | 40-60 | 20-30% | VFX/Graphics | MEDIUM (particle effects, testing) |
| UI Theming | 20-30 | 12-15% | UI/Design | MEDIUM (design → implementation) |
| Testing & Polish | 20-30 | 12-15% | QA/Testing | LOW (sequential: build → test → iterate) |
| **TOTAL** | **160-200** | **100%** | - | - |

### Agent Team Composition

```
1 Human PM (kooshapari)
├─ Roadmap oversight, decision-making, stakeholder communication
├─ Risk mitigation & escalation
└─ Release decision & shipping

+

3-4 Autonomous Agents (Claude Haiku subagents)
├─ Asset Generation Agent (FBX modeling, procedural generation)
│   └─ 20-30 hours, high parallelization
│
├─ SDK/Integration Agent (Addressables, ContentLoader, registries)
│   └─ 30-40 hours, requires code review checkpoints
│
├─ VFX/Graphics Agent (particle systems, shader work, visual effects)
│   └─ 40-60 hours, design-driven, visual validation needed
│
└─ QA/Testing Agent (pack validation, balance simulation, bug triage)
    └─ 20-30 hours, systematic & detail-oriented
```

### Skill Requirements

| Skill | Agent | Hours | Notes |
|-------|-------|-------|-------|
| Blender 3D modeling | Asset Gen | 20-30 | FBX export, material application, rigging (basic) |
| C# / .NET | SDK + VFX | 60-80 | Code architecture, optimization, integration |
| Unity particles | VFX | 30-40 | Particle system design, performance tuning |
| UI/UX design | UI | 15-20 | Color theory, accessibility, layout |
| QA/testing | QA | 20-30 | Test planning, automation, reproduction steps |
| YAML/JSON | All | 5-10 | Schema validation, configuration |
| Git workflow | All | 5-10 | Branching, commit discipline, code review |

### Tool Requirements

| Tool | Purpose | Cost | Notes |
|------|---------|------|-------|
| Blender 4.0+ | 3D modeling & FBX export | Free (Blender is GPL) | Required for FBX work |
| Unity 2021.3.45f2 | Game testing (match DINO) | Free (personal license) | For local VFX/UI testing |
| Rider / VS Code | C# development | Free (community) / $150/yr | IDE for SDK work |
| RenderDoc | GPU profiling | Free (MIT) | For VFX performance tuning |
| JetBrains Profiler | Memory/CPU profiling | Free (trial) / $200/yr | For performance analysis |
| GitHub Copilot | AI code assistance | $20/month | Accelerates development |

---

## Dependencies & Blockers

### Hard Dependencies (must complete first)

1. **Building FBX Completion** (Section 1)
   - Blocks: Addressables integration, UI portrait generation, VFX integration testing
   - Unblocked by: Nothing (can start immediately)

2. **Addressables Integration** (Section 2)
   - Blocks: VFX loading optimization, UI dynamic asset loading
   - Depends on: Building FBX models (for asset references)
   - Can start in parallel with Section 1 (API design first)

### Soft Dependencies (sequential but can overlap)

3. **VFX System** (Section 3)
   - Blocks: Visual polish, final balance tweaks
   - Depends on: Addressables (for prefab loading)
   - Can develop in parallel with Addressables (separate code paths)

4. **Faction UI Theming** (Section 4)
   - Blocks: Loading screen assets, portrait generation
   - Depends on: Building FBX (for scene context), VFX (for effect integration)
   - Can start design work immediately

5. **Testing & Polish** (Section 5)
   - Blocks: v1.0 release decision
   - Depends on: All prior sections complete
   - Highest priority for PM oversight (release-critical)

### External Dependencies

| Dependency | Criticality | Mitigation |
|-----------|-----------|-----------|
| DINO game updates (ECS changes) | HIGH | Lock DINO version to tested build, monitor GitHub releases |
| Unity.Addressables stability | HIGH | Use established v1.21.18, test early on real DINO |
| Kenney.nl asset availability | MEDIUM | Pre-download all source FBX files, maintain local archive |
| Community feedback (playtesting) | MEDIUM | Schedule Week 7 RC testing, allocate buffer for iteration |
| GPU/CPU hardware constraints | LOW | Profile on "minimum spec" (GTX 1070, 6-core CPU), document targets |

### Risk & Mitigation Matrix

| Risk | Impact | Probability | Mitigation |
|------|--------|-------------|-----------|
| FBX batch takes 40+ hours (not 20-30) | Timeline slip 1-2 weeks | MEDIUM | Start 2-3 artists in parallel, prioritize critical buildings |
| Addressables breaks on Mono runtime | Feature blocked | LOW | Test with bridge IPC by end of week 2, revert to fallback loader if needed |
| VFX perf ceiling (can't hit 60 FPS) | Feature cut or compromise | MEDIUM | Establish budget caps early, offer "performance mode" without VFX |
| Balance simulation inaccurate | Release with broken balance | LOW | Validate simulation against human playtesting, iterate both |
| UI framework too tightly coupled to vanilla | Major patching required | MEDIUM | Use Material parameter overrides, UI prefab variants first before Harmony patches |
| DINO game breaks mid-development | Large rework | LOW | Lock to known-good version, maintain compatibility matrix |

---

## Success Criteria & Exit Conditions

### Release Readiness Checklist

- [ ] **Building Assets**
  - [ ] All 24 buildings (48 faction variants) have production FBX models
  - [ ] All models < 400 triangles, average 300-340
  - [ ] Faction textures applied, colors visually distinct
  - [ ] All models reimport correctly in Unity, scale verified

- [ ] **Addressables Integration**
  - [ ] All assets load via Addressables on-demand
  - [ ] Memory overhead < 500 MB with all packs loaded
  - [ ] Load time < 2 seconds per building type
  - [ ] Bundle size 50-150 MB per faction
  - [ ] Zero memory leaks (100+ spawn/despawn cycles tested)

- [ ] **VFX System**
  - [ ] Projectile VFX complete for all 3 factions (9 variants)
  - [ ] Impact effects visible on all unit/building damage
  - [ ] Doctrines trigger visual feedback (Overcharge, Area Denial, Mobility)
  - [ ] 60 FPS sustained in full-scale battles (50+ units + VFX)
  - [ ] All VFX prefabs load via Addressables

- [ ] **Faction UI Theming**
  - [ ] All 3 factions have visually distinct HUD color schemes
  - [ ] Unit portraits generated and cached per faction
  - [ ] Menu system themed per faction (main, pause, settings)
  - [ ] Loading screens show faction-specific imagery
  - [ ] Colorblind mode accessibility tested & validated

- [ ] **Testing & Validation**
  - [ ] All 3 packs pass automated schema validation
  - [ ] Zero P0 bugs (game-breaking)
  - [ ] < 5 P1 bugs documented as known issues
  - [ ] Faction win rates within 45-55% (simulated & human tested)
  - [ ] Integration tests pass on real DINO instance
  - [ ] Memory footprint < 300 MB per pack
  - [ ] Load time < 5 seconds per pack

- [ ] **Documentation & Release**
  - [ ] Comprehensive release notes (features, known issues, breaking changes)
  - [ ] Troubleshooting guide with FAQs
  - [ ] Updated build/test/install instructions
  - [ ] GitHub release with installer packages
  - [ ] v1.0.0 tag on main branch with signed commit

### Definition of Done

A feature is "done" when:
1. Code complete and tested (unit tests passing)
2. Integration tested on real DINO instance (no crashes, expected behavior)
3. Documented (README, schema examples, troubleshooting)
4. Performance validated (profiled, targets met)
5. Code reviewed by PM (design oversight, no regrets)
6. Merged to main branch and tagged

---

## Communication & Reporting

### Weekly Status Updates

Every Friday, provide status report to PM:
- **Completed this week**: Bulleted features/deliverables
- **In progress**: Current blockers, estimated completion
- **Blocked**: Dependencies, escalation needed
- **Metrics**: Test pass rate, code coverage, performance metrics
- **Risks**: New blockers, timeline impact

### Escalation Triggers

Escalate to PM immediately if:
- Timeline slip > 3 days
- P0 bug discovered (game-breaking)
- Tool/library compatibility issue
- Resource constraint (need additional agents)
- Architecture decision needed

### Documentation Checkpoints

Update CHANGELOG.md after each major feature:
- Add entry under `[Unreleased]` with feature description
- Tag with milestone (M5.X), effort estimate, owner
- Link to PR/commits in GitHub

Example:
```markdown
#### M5.1: Building FBX Completion (COMPLETE)
- **All 24 buildings exported**: 48 faction variants with production-quality FBX models
- **Asset sources**: Kenney.nl sourced, polygon budgets verified (280-340 tri/building)
- **Effort**: 25 hours, Asset Agent + Parallelization
- **PR**: #123
```

---

## Known Issues & Open Questions

### Known Limitations

1. **Addressables Mono compatibility**: DINO runs Mono, not IL2CPP. Addressables API may behave differently. **Mitigation**: Early bridge testing.

2. **VFX particle budget**: Adding too many simultaneous particles will impact frame rate. **Mitigation**: Establish 500-particle budget per frame, offer performance mode.

3. **FBX batch modeling timeline**: 20-30 hours estimate is aggressive if complex tier 3 buildings take longer. **Mitigation**: Parallelize with 2-3 artists, prioritize critical buildings.

4. **Balance simulation accuracy**: AI simulation may not reflect human playstyle preferences. **Mitigation**: Combine simulation with community playtesting Week 7.

5. **UI framework coupling**: DINO's menu system may require deeper patching than expected. **Mitigation**: Use UI prefab variants + Material parameter overrides first.

### Open Questions for PM

1. **Asset art style**: Kenney.nl assets are more "cartoon" style. Is this acceptable, or should we pursue hand-drawn/realistic art for higher fidelity? (Trade-off: effort/timeline)

2. **VFX realism level**: Should projectiles have realistic physics (arcing, wind resistance) or arcade simplicity (instant hit)? (Impact: gameplay feel, player expectations)

3. **Balance philosophy**: Exact parity with vanilla DINO, or intentional mechanical diversity (some factions stronger in certain matchups)? (Impact: competitive fairness)

4. **Colorblind accessibility**: WCAG AA compliance required for v1.0, or acceptable for v1.1+?

5. **Community playtest scale**: How many external testers for Week 7 RC? (Small: 5-10 friends, Medium: 50+ Discord, Large: 200+ community)

---

## Appendix A: Test Strategy

### Unit Test Coverage Targets

| Component | Current | Target | Notes |
|-----------|---------|--------|-------|
| SDK (registries, schemas) | 150+ | 180+ | Add Addressables integration tests |
| Warfare domain | 31 | 50+ | Add VFX registry, UI theming tests |
| Integration tests | 14 | 30+ | Add bridge IPC tests, pack loading tests |
| **Total** | **383** | **450+** | 10% improvement from v0.3 |

### Test Categories

1. **Unit Tests** (xUnit, in-process)
   - Registry operations (add, remove, query)
   - Schema validation (pass/fail cases)
   - ContentLoader pipeline
   - StatModifier calculations

2. **Integration Tests** (bridge IPC, actual DINO)
   - Pack initialization on real runtime
   - Asset loading via Addressables
   - Unit spawn and behavior verification
   - Building production chain validation
   - Doctrine activation and cooldown reset

3. **Performance Tests** (profiled, benchmarked)
   - Asset load time per building (target < 2 sec)
   - Memory footprint per pack (target < 300 MB)
   - Frame time with VFX enabled (target 60 FPS)
   - GC allocation spike on particle spawn (target < 1 MB per frame)

4. **Visual Regression Tests** (screenshot-based)
   - HUD color themes render correctly per faction
   - Unit portraits generated without artifacts
   - Loading screen imagery displays
   - VFX particle effects appear as expected

5. **Balance Tests** (simulation-driven)
   - Win rates per faction pairing (target 45-55%)
   - Unit usage diversity (no single unit > 60% in winning armies)
   - Cost-effectiveness ratios (within ±20% of vanilla)
   - Doctrine effectiveness (positive impact on win rate)

### CI/CD Pipeline

```
Trigger: git push to main

1. Build all projects
   └─ Target: zero warnings/errors

2. Run unit tests (xUnit)
   └─ Target: 100% pass rate

3. Run integration tests (bridge IPC)
   └─ Target: 100% pass rate (on DINO-enabled runner)

4. Run performance tests
   └─ Target: Load time < 2 sec, Memory < 300 MB

5. Run visual regression tests
   └─ Target: 100% screenshot match

6. Build release artifacts
   ├─ war fate-starwars-v1.0.0.zip
   ├─ warfare-modern-v1.0.0.zip
   └─ warfare-guerrilla-v1.0.0.zip

7. Publish to GitHub Releases
   └─ Attach artifacts, auto-draft release notes
```

---

## Appendix B: Asset Inventory

### Building Assets Status

| Building ID | Vanilla ID | Description | FBX Status | Texture Status | Faction Variants |
|-------------|-----------|-------------|-----------|----------------|-----------------|
| rep_house | 0 | Basic housing | COMPLETE | COMPLETE | 2 (Rep + CIS) |
| cis_house | - | Droid pod variant | COMPLETE | COMPLETE | 2 |
| rep_farm | 1 | Food production | COMPLETE | COMPLETE | 2 |
| cis_farm | - | Fuel harvester variant | COMPLETE | COMPLETE | 2 |
| rep_barracks | 2 | Unit training | PENDING | COMPLETE | 2 |
| cis_barracks | - | Droid factory variant | PENDING | COMPLETE | 2 |
| rep_stable | 3 | Cavalry production | PENDING | COMPLETE | 2 |
| cis_stable | - | Walker production variant | PENDING | COMPLETE | 2 |
| rep_tower | 4 | Defense tower | PENDING | COMPLETE | 2 |
| cis_tower | - | Droid turret variant | PENDING | COMPLETE | 2 |
| guerrilla_hideout | - | Mobile unit training | PENDING | PENDING | 1 |
| *... remaining 13 buildings* | ... | ... | PENDING | PENDING | ... |

**Summary**:
- FBX Complete: 4 buildings (8%)
- Texture Complete: 20 buildings (83%)
- Faction variants: 48 total (24 unique buildings × 2 factions)
- Effort to complete: 20-30 hours (Sections 1 + 5)

### Unit Texture Assets Status

| Faction | Unit Type | Count | Status | Quality |
|---------|-----------|-------|--------|---------|
| Republic | Infantry, vehicles, specialists | 13 | COMPLETE | 512×512 PNG, sRGB |
| CIS | Droids, walkers, specialists | 13 | COMPLETE | 512×512 PNG, sRGB |
| Guerrilla | Light units, mobile, asymmetric | 13 | PENDING | Planned for M5.2 |
| **Total** | | **39** | **26 COMPLETE** | |

### VFX Assets Planned

| VFX Type | Subtypes | Count | Faction Variants | Effort (hrs) |
|----------|----------|-------|------------------|-------------|
| Projectile | Blaster, Laser, Grenade, Special | 4 | 3 (Rep, CIS, Guerrilla) | 16 |
| Impact | Melee, Explosion, Environmental | 3 | 3 | 12 |
| Ability | Doctrine, Buff/Debuff, Building | 3 | 3 | 12 |
| Environmental | Weather, Lighting, Audio sync | 3 | Shared | 8 |
| **Total** | | **13 VFX systems** | | **48 hrs** |

---

## Appendix C: Reference Architecture

### Asset Pipeline Data Flow

```
v1.1 Asset Pipeline Overview

┌─────────────────────────────────────────────────────────┐
│                     Pack Content                         │
│  (YAML manifests: units, buildings, factions, doctrines)│
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│              Asset Sourcing & Generation                 │
│ ┌────────────────────────────────────────────────────┐  │
│ │ 1. Building FBX: Kenney.nl → Blender → Export     │  │
│ │ 2. Unit Textures: Procedural PIL gen. → PNG       │  │
│ │ 3. VFX Prefabs: Particle systems + materials      │  │
│ │ 4. UI Elements: Faction colors + portrait gen.    │  │
│ └────────────────────────────────────────────────────┘  │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│                  ContentLoader Pipeline                  │
│ ┌────────────────────────────────────────────────────┐  │
│ │ 1. Load YAML manifests (parse, validate schema)   │  │
│ │ 2. Initialize registries (units, buildings, etc)  │  │
│ │ 3. Load Addressables catalog (bundle metadata)    │  │
│ │ 4. Request asset load (buildings on-demand)       │  │
│ │ 5. Apply stat modifiers (balance overrides)       │  │
│ │ 6. Initialize faction UI (colors, portraits)      │  │
│ │ 7. Register ECS systems (doctrine, waves)         │  │
│ └────────────────────────────────────────────────────┘  │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────┐
│                    ECS Runtime Bridge                    │
│ ┌────────────────────────────────────────────────────┐  │
│ │ ComponentMap: Vanilla → Mod unit/building mapping │  │
│ │ EntityQueries: Find units, buildings by faction   │  │
│ │ VFX Spawner: Projectiles, impacts, abilities      │  │
│ │ StatModifierSystem: Apply balance overrides       │  │
│ │ DoctrineSystems: Cooldown, aura, activation      │  │
│ └────────────────────────────────────────────────────┘  │
└────────────┬────────────────────────────────────────────┘
             │
             ▼
         GAME LOOP
      (60 FPS target)
```

### Addressables Bundle Structure

```
packs/
  warfare-starwars/
    bundles/
      catalog.json                    # Addressables catalog
      buildings-republic.bundle       # Rep buildings FBX + materials
      buildings-cis.bundle            # CIS buildings FBX + materials
      vfx-projectiles.bundle          # All projectile particle prefabs
      vfx-impacts.bundle              # Impact effect prefabs
      ui-republic.bundle              # Rep HUD colors, portraits, icons
      ui-cis.bundle                   # CIS HUD colors, portraits, icons
      ui-common.bundle                # Loading screens, shared UI assets
    manifest.yaml                      # Pack definition
    ...
```

### Testing Architecture

```
Test Pyramid (v1.1 target: 450+ total tests)

                    ▲
                   ╱│╲
                  ╱ │ ╲  Integration Tests (30+)
                 ╱  │  ╲  - Bridge IPC
                ╱   │   ╲ - Pack loading
               ╱    │    ╲ - Asset availability
              ╱───────────╲
             ╱      │      ╲ Unit Tests (400+)
            ╱       │       ╲ - Registries, schemas, loaders
           ╱        │        ╲ - Stat modifiers, validators
          ╱─────────┼─────────╲
         ╱          │          ╲ Manual/Visual Tests
        ╱           │           ╲ - Playtest balance
       ╱────────────┼────────────╲ - Screenshot regression
      ▬─────────────┴─────────────▬
```

---

## Appendix D: Budget & Resource Requirements

### Personnel Costs (Estimated)

Assuming agent-driven development with minimal human oversight:

| Role | Hours/Week | Duration (weeks) | Total Hours | Cost Model |
|------|-----------|-----------------|-------------|-----------|
| PM (kooshapari) | 8-10 | 8 | 64-80 | Salaried (5-10% allocation) |
| Asset Gen Agent | 15-20 | 8 | 120-160 | API credits (~$40/week × 8) |
| SDK Agent | 15-20 | 8 | 120-160 | API credits |
| VFX Agent | 15-20 | 8 | 120-160 | API credits |
| QA Agent | 10-15 | 8 | 80-120 | API credits |
| **TOTAL** | **65-85** | **8** | **480-680** | **~$400-500 in API costs** |

### Software & Hardware Budget

| Item | Cost | Notes |
|------|------|-------|
| Blender 4.0+ | Free | Open-source, already required for modeling |
| Unity 2021.3.45f2 | Free | Personal license sufficient |
| RenderDoc | Free | GPU profiling tool |
| JetBrains Rider | $150/yr | Optional, VS Code sufficient for agent dev |
| GitHub Copilot | $20/month | Accelerates code generation |
| Cloud build machine | $50-100 | (Optional) Faster CI/CD builds |
| **TOTAL** | **$0-400** | | Can all use free tools |

### Timeline Risk & Buffer

- **Planned duration**: 8 weeks
- **Buffer**: 1-2 weeks for unforeseen delays
- **Realistic commitment**: 9-10 weeks from start to stable v1.0 release
- **Contingency**: If major blockers (e.g., bridge IPC instability), extend to 12 weeks

---

## Appendix E: Version Compatibility Matrix

### Supported DINO Versions

| DINO Version | Framework | ECS | BepInEx | Support Level |
|--------------|-----------|-----|---------|---------------|
| 1.2.0 | .NET 4.7 | Enabled | 5.4.23 | PRIMARY (tested) |
| 1.1.x | .NET 4.7 | Enabled | 5.4.23 | SECONDARY (assumed compat) |
| 1.0.x | .NET 4.7 | Disabled | 5.4.20 | NOT SUPPORTED (ECS req) |

### Dependency Versions

| Dependency | Version | License | Status |
|-----------|---------|---------|--------|
| Unity.Addressables | 1.21.18 | MIT | STABLE |
| AssetsTools.NET | Latest | MIT | STABLE |
| YamlDotNet | 13.0+ | MIT | STABLE |
| JsonSchema.Net | 1.0+ | MIT | STABLE |
| xUnit | 2.6+ | MIT | STABLE |
| FluentAssertions | 6.0+ | MIT | STABLE |

---

## Appendix F: Glossary

| Term | Definition |
|------|-----------|
| **Addressables** | Unity asset management system enabling on-demand loading and bundling |
| **Bundle** | Compressed asset container (FBX, textures, prefabs) streamed from disk to memory |
| **FBX** | 3D model file format (Autodesk), used for buildings/units |
| **VFX** | Visual effects (particle systems, shaders, animations) |
| **Doctrine** | Warfare domain concept: reusable active ability (Overcharge, Area Denial, Mobility) |
| **Faction** | Game concept: playable side with unique units, buildings, doctrines (Republic, CIS, Guerrilla) |
| **ContentLoader** | DINOForge SDK component: loads pack YAML, initializes registries, applies overrides |
| **PackCompiler** | CLI tool: validates schema, builds bundles, outputs release artifacts |
| **Bridge IPC** | Inter-Process Communication between DINOForge SDK and DINO ECS runtime |
| **ECS** | Entity Component System: DINO's low-level architecture for game objects |
| **HUD** | Heads-Up Display: on-screen UI (health bars, resources, minimap) |
| **rc (Release Candidate)** | Pre-release version for final testing before production launch |

---

## Conclusion

v1.1 is the **completion and polish pass** for the DINOForge platform and warfare packs. Success means:

✓ **Production-ready assets**: All buildings, VFX, UI complete and visually distinct per faction
✓ **Optimized loading**: Addressables streaming, memory < 500 MB, load time < 5 sec
✓ **Balanced gameplay**: Win rates 45-55%, diverse unit usage, fair faction matchups
✓ **Polished UX**: Faction theming, portraits, loading screens, accessibility
✓ **Comprehensive testing**: 450+ tests, zero P0 bugs, integration-tested on DINO
✓ **Shipping products**: Three complete warfare total conversions ready for v1.0 release

**Target ship date: 2026-05-12** (8 weeks)
**Estimated effort: 160-200 hours** (human PM + 4 agent team)
**Expected outcome: Stable, feature-complete v1.0 release** for community adoption

---

*Document created: 2026-03-12*
*Last updated: 2026-03-12*
*Status: APPROVED*
