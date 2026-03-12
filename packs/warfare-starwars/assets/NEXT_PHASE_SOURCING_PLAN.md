# Next Phase Sourcing Plan
**DINOForge Star Wars Pack - Asset Integration Roadmap**

**Date**: 2026-03-12
**Status**: Ready for Execution
**Audience**: Technical leads, artists, build engineers

---

## Overview

This document outlines the **actionable roadmap** for completing all 24 vanilla DINO building asset skins using the Kenney.nl foundation. Three sequential phases, each with clear deliverables, effort estimates, and success criteria.

---

## Phase 1: Foundation Build (Weeks 1-3)

### Goal
Export all 24 Kenney FBX meshes with faction-specific materials. Establish baseline for texture application and game integration.

### Scope
- Blender batch FBX export (Kenney FBX → faction-textured FBX)
- Polygon count validation (< 400 tris per building)
- Faction color verification (Republic white/blue, CIS grey/orange)
- File organization & metadata logging

### Key Steps

#### Step 1.1: Blender Setup (2 hours, 1 artist)
- [ ] Install Blender 3.6+
- [ ] Download all Kenney FBX source files from `source/kenney/`
- [ ] Set up color palette references (from COLOR_PALETTE_GUIDE.md)
- [ ] Review BLENDER_ASSEMBLY_TEMPLATE.md for workflow

#### Step 1.2: Single-Building Pilot (4 hours, 1 artist)
- [ ] Select pilot: `rep_house_clone_quarters` (simplest building)
- [ ] Follow BLENDER_ASSEMBLY_TEMPLATE.md exactly
- [ ] Apply faction textures
- [ ] Export to FBX
- [ ] Verify polygon count (target: 280-320 tris)
- [ ] Document process in EXPORT_LOG.txt
- [ ] QA: Check faction colors match palette

**Output**: `rep_house_clone_quarters.fbx` + process notes

#### Step 1.3: Batch Export All 24 Buildings (40-60 hours, 1-2 artists)

**Option A: Serial Export** (1 artist, 6 weeks)
- Export all 24 buildings sequentially
- 2.5 hours per building (based on pilot)
- Quality gates at each step
- Safer, but slower

**Option B: Parallel Export** (2 artists, 3 weeks) ← RECOMMENDED
- Artist A: Republic buildings (10 buildings, 25 hours)
- Artist B: CIS buildings (10 buildings, 25 hours)
- Overlap on shared base meshes (e.g., tower-a used by both)
- Both finish ~Week 3

**Parallel Workflow**:
```
Week 1:
  Artist A: buildings 1-5 (Command, Barracks×3, Tower)
  Artist B: buildings 1-5 (same, CIS faction)
  → Both validate poly counts, materials match palette

Week 2:
  Artist A: buildings 6-10 (Defense, Economy×3, Research)
  Artist B: buildings 6-10 (same, CIS faction)
  → Batch test in game (5 buildings per faction)

Week 3:
  Artist A: buildings 11-24 (Residential, Specials, Pending)
  Artist B: buildings 11-24 (same, CIS faction)
  → Final validation, QA check all 24
```

#### Step 1.4: Quality Gates (Per-Building)

For each FBX export, verify:
- [ ] Polygon count < 400 tris (log actual count)
- [ ] Textures applied correctly (no missing/white materials)
- [ ] Faction colors visible (white/blue for Rep, grey/orange for CIS)
- [ ] Scale matches vanilla footprint (test in game)
- [ ] FBX exports cleanly (no errors in export log)
- [ ] Normals facing outward (no dark/inverted patches)
- [ ] Pivot point centered at ground level

**Template for EXPORT_LOG.txt**:
```
BUILDING: rep_command_center.fbx
Kenney Source: structure-c.fbx
Faction: Republic
Export Date: 2026-03-13
Artist: [Name]

Metrics:
  Polygon Count: 320 tris
  Texture: rep_command_center_albedo.png
  Colors: Primary #F5F5F5, Accent #1A3A6B
  Metallic: 0.1, Roughness: 0.8

QA Checks:
  ✓ Poly count < 400
  ✓ Texture applied
  ✓ Faction colors correct
  ✓ Scale verified
  ✓ Export clean
  ✓ Normals correct
  ✓ Game tested

Status: READY FOR DEPLOYMENT
```

#### Step 1.5: Asset Index Update (1 hour, build engineer)
- [ ] Read all FBX files and log poly counts
- [ ] Update `asset_index.json`:
  - Change all building status from "pending" → "complete"
  - Add `poly_count` field for each
  - Add `export_date` and `exported_by` metadata
- [ ] Verify manifest entries for all 24 buildings
- [ ] Generate checksums (MD5) for integrity tracking

#### Step 1.6: Game Integration & Testing (4-6 hours, QA)
- [ ] Load warfare-starwars pack into DINO game
- [ ] Verify all 24 building skins render
- [ ] Screenshot comparison: vanilla vs. faction reskins
- [ ] Performance test: frame rate stable with all 24 buildings on screen
- [ ] Edge case test: building placement, destruction animations, unit selection

### Deliverables

| Artifact | Status | Owner | Due |
|---|---|---|---|
| 24 FBX files in `assets/meshes/buildings/` | New | Artist(s) | Week 3 |
| EXPORT_LOG.txt with metadata | Updated | Artist(s) | Week 3 |
| asset_index.json updated (status + poly counts) | Updated | Build Eng | Week 3 |
| Game integration test report | New | QA | Week 3 |
| Screenshots (vanilla vs. faction) | New | QA | Week 3 |

### Effort & Timeline

| Role | Hours | Duration | Notes |
|---|---|---|---|
| Blender Artist(s) | 45-65 | 3 weeks | 2 artists @ 25h each (parallel) |
| Build Engineer | 2 | 1 day | Asset index & manifest updates |
| QA Engineer | 5 | 1 week | Integration testing & validation |
| **Total** | **52-72** | **3 weeks** | **With 2-artist team (parallel)** |

---

## Phase 2: Augmentation & Optimization (Weeks 4-6)

### Goal
Refine textures, optimize geometry, and augment buildings with faction-specific details (decals, glows, custom geometry).

### Scope

#### 2.1: Texture Refinement

**Current Status**: 20 textures generated (albedo maps only)

**Enhancements**:
- [ ] Normal maps (surface detail)
- [ ] Metallic maps (shine variation)
- [ ] Roughness maps (material finish)
- [ ] Emission maps (glowing elements, force fields)

**Effort**: 20 hours (automated script + manual tweaks)

**Process**:
1. Run `texture_generation.py` with normal/metallic/roughness flags
2. Review for faction-specific details:
   - Republic: Gold trim, clean lines, insignia
   - CIS: Orange accents, weathered surfaces, warning lights
3. Test in game (material blending)

#### 2.2: Faction-Specific Details

Add visual richness without exceeding poly budget (< 400 tris).

**Republic Buildings**:
- [ ] Republic insignia decals (empire crest, jedi symbol)
- [ ] Emission on control panels (blue glow)
- [ ] Gold trim detailing (chamfered edges)

**CIS Buildings**:
- [ ] CIS separatist markings (droid spider symbol)
- [ ] Rust/weathered appearance (darkened base, orange accents)
- [ ] Energy conduits (glowing orange lines)

**Effort**: 40 hours (decal design + material setup)

#### 2.3: High-Detail Prestige Buildings

Invest extra detail for landmark structures:

| Building | Detail | Effort | Faction |
|---|---|---|---|
| Jedi Temple | Spire geometry, blue force field emitter | 8h | Republic |
| Droid Factory | Conveyor belt decal, orange sparks emission | 6h | CIS |
| Command Center | Antenna tower, hologram emitter | 6h | Both |
| Reactor Core | Glowing core emission, venting pipes | 5h | Both |

**Total**: 25 hours (optional enhancement, Phase 2+ recommended)

### Deliverables

| Artifact | Notes |
|---|---|
| Normal maps (20 files) | Surface detail for all buildings |
| Metallic & roughness maps (20 files) | Material finish variation |
| Emission maps (8-10 files) | Prestige buildings with glow |
| Updated textures in `assets/textures/buildings/` | Regenerated with enhanced properties |
| TEXTURE_MANIFEST.json updated | New texture properties documented |

### Effort & Timeline

| Task | Hours | Duration | Notes |
|---|---|---|---|
| Normal/metallic/roughness generation | 20 | 1 week | Script-driven with QA |
| Faction decal design | 30 | 1 week | Artist-driven, material setup |
| Prestige building details | 25 | 1 week | Optional, highest visual ROI |
| **Total** | **75** | **3 weeks** | **Can overlap with Phase 1 final week** |

---

## Phase 3: Validation & Deployment (Weeks 7-8)

### Goal
Complete pack validation, documentation, and release to v1.0.

### Scope

#### 3.1: Pack-Level Validation

- [ ] Run `dotnet run --project src/Tools/PackCompiler -- validate packs/warfare-starwars`
  - Building definitions valid
  - All asset paths resolvable
  - Manifest syntax correct
  - No circular dependencies

- [ ] Schema validation
  - building.schema.json compliance
  - asset_replacement.schema.json compliance
  - pack-manifest.schema.json compliance

- [ ] Integration testing (full game playthrough)
  - All 24 building skins load without errors
  - No visual glitches (missing textures, corrupted geometry)
  - Performance acceptable (> 60 FPS on test rig)
  - Unit production from all barracks works
  - Unit behavior (pathfinding, combat) unaffected

#### 3.2: Documentation Updates

- [ ] CHANGELOG.md
  - Add "Phase 1: Asset sourcing & integration complete"
  - List all 24 buildings by faction
  - Note texture generation & FBX export details

- [ ] README.md (if applicable)
  - Building list with faction variants
  - Asset sourcing strategy (Kenney.nl)
  - License attribution (CC0)

- [ ] Assets/registry files
  - asset_index.json finalized (all 24 buildings status = "complete")
  - provenance_index.json complete (license, source, date)
  - VANILLA_BUILDINGS.json finalized with all mappings

#### 3.3: Release Prep

- [ ] Build pack: `dotnet run --project src/Tools/PackCompiler -- build packs/warfare-starwars`
  - Output: `.pack` archive file
  - Verify file size reasonable (< 5 MB)
  - Checksum generated

- [ ] Release notes: v1.0 summary
  - 24 vanilla buildings with faction skins
  - Republic (Galactic Republic) & CIS (Separatists) factions
  - Kenney.nl asset sources (CC0)
  - Known issues (if any)
  - Future enhancements (Sketchfab optional, v1.1+)

#### 3.4: Archive & Handoff

- [ ] Source .blend files archived (optional, for future updates)
- [ ] Texture generation pipeline documented & maintained
- [ ] FBX export pipeline tested for reproducibility
- [ ] Team knowledge transfer (asset sourcing playbook)

### Deliverables

| Artifact | Status | Owner |
|---|---|---|
| Pack validation report | Pass ✓ | QA |
| Build output (.pack file) | Generated | Build Eng |
| CHANGELOG.md updated | Updated | Maint |
| README.md updated | Updated | Maint |
| asset_index.json finalized | Complete | Build Eng |
| Release notes (v1.0) | New | PM |

### Effort & Timeline

| Task | Hours | Duration | Notes |
|---|---|---|---|
| Pack validation & schema checks | 4 | 1 day | Automated + manual review |
| Integration testing | 8 | 3 days | Full game playthrough |
| Documentation updates | 6 | 1 day | CHANGELOG, README, manifests |
| Build & release prep | 3 | 1 day | Generate .pack, checksums |
| **Total** | **21** | **1 week** | **Can run in parallel with Phase 2** |

---

## Dependency Graph & Critical Path

```
Phase 1 (Weeks 1-3): CRITICAL PATH
  ├── Step 1.1: Blender setup (2h)
  ├── Step 1.2: Pilot build (4h) ← Blocks Step 1.3
  ├── Step 1.3: Batch export (40-60h) ← Blocks Phase 2
  ├── Step 1.4: QA gates (per building)
  ├── Step 1.5: Asset index update (1h)
  └── Step 1.6: Game integration test (4-6h)

Phase 2 (Weeks 4-6): CAN OVERLAP
  ├── 2.1: Texture refinement (20h)
  ├── 2.2: Faction details (40h) ← DEPENDS ON 1.6 passing
  └── 2.3: Prestige details (25h, optional)

Phase 3 (Weeks 7-8): FINAL
  ├── 3.1: Pack validation (4h) ← DEPENDS ON 2.x complete
  ├── 3.2: Documentation (6h)
  ├── 3.3: Release prep (3h)
  └── 3.4: Archive & handoff (2h)

CRITICAL PATH: 1.1 → 1.2 → 1.3 → 1.6 → 2.2 → 3.1
TOTAL: 8 weeks (sequential) or 5-6 weeks (parallel where possible)
```

---

## Recommended Team Structure

### Option A: Minimal (1-2 people, 8 weeks)
- **Person A (Blender Artist)**: Phases 1-2 (60 hours over 6 weeks)
  - Batch export all 24 FBX, texture refinement, prestige details
  - Weekly deliverables (4-6 buildings/week)
- **Person B (Build/QA Engineer)**: Phases 1 & 3 (10 hours over 8 weeks)
  - Asset index updates, integration testing, release prep
  - Part-time, blocks only during validation gates

### Option B: Recommended (2-4 people, 5-6 weeks)
- **Artist A (Republic Specialist)**: Phase 1 (25 hours, weeks 1-3)
  - All 10 Republic buildings
  - Asset index updates (shared)

- **Artist B (CIS Specialist)**: Phase 1 (25 hours, weeks 1-3)
  - All 10 CIS buildings
  - Asset index updates (shared)

- **Artist C (Enhancement Lead)**: Phases 2-3 (40 hours, weeks 4-6)
  - Texture refinement, prestige details, final QA

- **Build/QA Engineer**: All phases (12 hours, ongoing)
  - Integration testing, validation, release

**Parallel gains**: 3 weeks to Phase 1 complete (vs. 6 weeks serial)

---

## Risk Mitigation

### Risk 1: Polygon Budget Exceeded
**Impact**: Game performance degradation
**Mitigation**:
- Batch test poly count after each building
- If > 400 tris, apply decimation (Blender Decimate modifier)
- Target: 280-320 tris (25% safety margin)

### Risk 2: Texture Misapplication
**Impact**: Wrong faction colors, visual inconsistency
**Mitigation**:
- Reference COLOR_PALETTE_GUIDE.md before each build
- Test colors in game (Material Preview mode)
- QA sign-off on each faction pair (Rep + CIS)

### Risk 3: Kenney FBX Unavailable
**Impact**: Blocked export
**Mitigation**:
- Download all Kenney assets upfront (Week 0)
- Verify checksums & file integrity
- Backup in team storage (OneDrive, Google Drive)

### Risk 4: Blender Version Incompatibility
**Impact**: Export failures
**Mitigation**:
- Standardize on Blender 3.6+ (stable LTS release)
- Test export on 2 systems before batch run
- Document Blender version in EXPORT_LOG.txt

### Risk 5: Schedule Slip
**Impact**: Delayed v1.0 release
**Mitigation**:
- Phase 1 is critical path; schedule buffer (1-2 weeks)
- Phase 2 is optional (can defer to v1.1)
- Phase 3 is fixed (1 week, minimal variability)

---

## Success Criteria

### Phase 1 Completion
- [ ] All 24 FBX files exist and are valid
- [ ] Polygon counts logged (< 400 tris per building)
- [ ] asset_index.json updated with metadata
- [ ] Game loads pack without errors
- [ ] All 24 building skins visible in game

### Phase 2 Completion (Optional)
- [ ] Normal/metallic/roughness maps generated & tested
- [ ] Prestige buildings enhanced (optional 8+ hour additions)
- [ ] Performance remains stable (> 60 FPS)

### Phase 3 Completion
- [ ] Pack validation passes (zero errors)
- [ ] Integration tests pass (all buildings functional)
- [ ] Documentation complete (CHANGELOG, README, manifests)
- [ ] v1.0 release artifacts generated (.pack file, checksums)

---

## Effort Summary Table

| Phase | Task | Hours | Duration | Team |
|---|---|---|---|---|
| **Phase 1** | Blender setup | 2 | 1 day | 1 artist |
| | Pilot build | 4 | 2 days | 1 artist |
| | Batch export (serial) | 60 | 3 weeks | 1 artist |
| | Batch export (parallel) | 50 | 3 weeks | 2 artists |
| | QA & integration | 6 | 1 week | QA/Build Eng |
| | Asset index update | 1 | 1 day | Build Eng |
| | **Phase 1 Total** | **52-72** | **3 weeks** | **2-3 people** |
| **Phase 2** | Texture refinement | 20 | 1 week | 1 artist |
| | Faction details | 40 | 1-2 weeks | 1 artist |
| | Prestige details (opt) | 25 | 1 week | 1 artist |
| | **Phase 2 Total** | **85** | **3 weeks** | **1-2 people** |
| **Phase 3** | Pack validation | 4 | 1 day | QA/Build Eng |
| | Integration testing | 8 | 3 days | QA |
| | Documentation | 6 | 1 day | Maint/Eng |
| | Release prep | 3 | 1 day | Build Eng |
| | **Phase 3 Total** | **21** | **1 week** | **2-3 people** |
| | **GRAND TOTAL** | **158** | **7-8 weeks** | **2-4 people** |

---

## Key Resources

### Documentation
- `BLENDER_ASSEMBLY_TEMPLATE.md` — Step-by-step assembly guide
- `BATCH_ASSEMBLY_PLAN.md` — Parallelization strategy
- `BUILD_CHECKLIST_ENHANCED.md` — Full building inventory
- `COLOR_PALETTE_GUIDE.md` — Faction colors & aesthetics
- `ASSET_SOURCE_HARMONIZATION.md` — Asset sourcing strategy

### Tools
- `texture_generation.py` — Automated texture pipeline
- `blender_batch_export.py` — Blender FBX export automation
- `PackCompiler` (dotnet CLI) — Pack validation & build

### Assets
- `source/kenney/` — Kenney FBX source files (CC0)
- `assets/textures/buildings/` — Generated faction textures (20 PNG)
- `assets/meshes/buildings/` — FBX outputs (stubs + production)
- `assets/registry/asset_index.json` — Master inventory

### External
- Kenney.nl: https://kenney.nl/assets/3d-models
- Blender: https://www.blender.org/

---

## Next Steps (Immediate)

1. **Confirm Team**: Assign 2-4 people for Phases 1-3
2. **Prepare Assets**: Download all Kenney FBX files to `source/kenney/`
3. **Setup Blender**: Install Blender 3.6+ on team systems
4. **Kickoff Meeting**: Review BLENDER_ASSEMBLY_TEMPLATE.md + timeline
5. **Week 1**: Begin Blender setup (Step 1.1) and pilot build (Step 1.2)

---

## Version History

| Version | Date | Status | Notes |
|---|---|---|---|
| 1.0 | 2026-03-12 | Complete | Initial sourcing plan with 3-phase roadmap |

---

**Document Status**: Ready for Execution
**Created**: 2026-03-12
**Version**: 1.0
**Audience**: Technical leads, artists, build engineers
**Maintained By**: DINOForge Subagent (Claude Haiku)

---

## Appendix: Quick Command Reference

### Validate Pack
```bash
cd packs/warfare-starwars
dotnet run --project ../../src/Tools/PackCompiler -- validate .
```

### Build Pack
```bash
cd packs/warfare-starwars
dotnet run --project ../../src/Tools/PackCompiler -- build .
```

### Generate Textures (if needed)
```bash
cd packs/warfare-starwars/assets
python3 texture_generation.py --source source/kenney --output textures/buildings/
```

### Export Single FBX (Template)
```bash
blender --background --python blender_batch_export.py -- \
  --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \
  --faction republic \
  --building-id command_center \
  --output assets/meshes/buildings/rep_command_center.fbx
```

---

**End of Document**
