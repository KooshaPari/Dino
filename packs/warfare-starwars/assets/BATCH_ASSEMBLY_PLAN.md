# Batch Assembly Plan: 24 Star Wars Building Reskins

**Objective**: Assemble all 24 vanilla DINO buildings (Republic + CIS) from Kenney source models.

**Total Effort**: 60-72 human hours + automated build time
**Timeline**: 2-3 weeks (parallel work recommended)
**Team Size**: 2-4 artists (recommended for parallelization)

---

## Executive Summary

| Metric | Value |
|--------|-------|
| **Total buildings** | 24 (12 Republic + 12 CIS) |
| **Kenney source kits** | 2 (space-kit, modular-space-kit) |
| **Average assembly time** | 2.5 hours per building |
| **Texture generation** | ✓ Complete (24/24) |
| **Pilot FBX builds** | ✓ Complete (2/24 - proof of concept) |
| **Critical path** | Manual Blender assembly (can parallelize) |
| **Automation support** | Blender Python script (template provided) |
| **Quality gates** | Poly count < 700 tris; texture visible; game-testable |

---

## Parallelization Strategy

### Key Insight: Buildings Can Be Done Simultaneously

Since each building is **independent** (no dependencies on other buildings), you can assign different buildings to different artists.

**Recommended Team Structure**:

```
Artist A: Buildings 1-6 (Residential + Stone/Iron)
Artist B: Buildings 7-12 (Extraction + Military)
Artist C: Buildings 13-18 (Iron variants + Guild)
Artist D: Buildings 19-24 (Specialty + Gates)
```

Or, if you have 2 artists:

```
Artist A (Rep): Buildings 1-12 (all Republic variants)
Artist B (CIS): Buildings 13-24 (all CIS variants)
```

**Advantage**: Republic and CIS have consistent faction palettes, so each artist learns their palette once.

---

## Which Buildings Can Share Base Meshes

Some buildings are **duplicates or mirrors** of others. These can be optimized:

| Building Pair | Strategy | Effort Saved |
|---|---|---|
| **iron_mine** ↔ **infinite_iron_mine** | Duplicate + mirror tower-A | 1.5 hours (don't rebuild tower) |
| **farm** ↔ **granary** | Both use platform pieces; share base model | ~0.5 hours |
| **house** (Rep) ↔ **house** (CIS) | Same Kenney source, different colors | 0.5 hours (1 assembly, 2 textures) |
| **stone_cutter** duplicates in variants | Reuse frame, change details | ~0.5 hours per variant |

**Total time savings**: ~3-4 hours via shared assets

---

## Priority-Based Assembly Order

### Tier 1: Simple Buildings (Easiest, ~1.5-2 hrs each)

Start with these to build confidence and validate process:

1. **House → Clone Quarters Pod (Rep) / Droid Storage Pod (CIS)**
   - Single Kenney source (structure.fbx)
   - Simple texture application
   - Few details (stripe + emblem)
   - Proof-of-concept ✓ (pilots already built)

2. **Granary → Nutrient Synthesizer (Rep) / Power Cell Depot (CIS)**
   - Tall cylinder, straightforward geometry
   - One accent stripe
   - Easy poly budget

3. **Gate → Security Gate (Rep) / Security Barrier (CIS)**
   - Flat gate structure (simple proportions)
   - Panel details optional
   - Lowest poly count (< 250 tris possible)

4. **Builder House → Engineer Corps (Rep) / Droid Bay (CIS)**
   - Small depot structure
   - Crane arm (simple modifier)
   - Faction color + mechanical details

**Subtotal**: 4 buildings × 2 hours = **8 hours**

---

### Tier 2: Medium Complexity (2-3 hrs each)

Build these after Tier 1; leverage Blender skills from pilots:

5. **Farm → Hydroponic Array (Rep) / Fuel Cell Harvester (CIS)**
   - Platform base + transparent overlays
   - Emissive glow (blue/orange)
   - Moderate detail count

6. **Hospital → Medical Bay (Rep) / Repair Station (CIS)**
   - Prefab walls with interior suggestion
   - Open maintenance frame (CIS)
   - 2-3 material slots

7. **Forester House → Extraction Post (Rep) / Raw Extractor (CIS)**
   - Outpost structure with antenna
   - Spider-leg attachment (CIS only; custom geometry)
   - Sector-specific details

8. **Stone Cutter → Durasteel Refinery (Rep) / Scrap Works (CIS)**
   - Industrial smelter with chimneys
   - Chimney glow (emit shader)
   - Multiple detail layers (conveyor, gears)

**Subtotal**: 4 buildings × 2.5 hours = **10 hours**

---

### Tier 3: High Complexity (3-4.5 hrs each)

Save these for experienced artists; leverage learnings from Tier 1 & 2:

9. **Iron Mine → Tibanna Extractor (Rep) / Ore Plant (CIS)**
   - Tower derrick structure
   - Pressurized tanks + conduits (Rep)
   - Conveyor + grinder (CIS)

10. **Infinite Iron Mine → Deep-Core Rig (Rep) / Endless Extractor (CIS)**
    - **Duplicate + Mirror** iron_mine (saves time)
    - Twin-derrick layout
    - Flare stack between towers
    - Mirrors tower geometry; mostly new assembly

11. **Engineer Guild → Engineering Lab (Rep) / Techno Workshop (CIS)**
    - Large symmetrical wings (Rep)
    - Asymmetric organic-tech hybrid (CIS) ⚠️ **HIGH COMPLEXITY**
    - Holodisplay dome with emission
    - Most complex CIS variant

12. **Soul Mine → Crystal Excavator (Rep) / Dark Energy Tap (CIS)**
    - Custom mining cage frame
    - Emission shaders (purple/blue for Rep, red/purple for CIS)
    - Lore-specific visual language
    - Highest artistic bar

**Subtotal**: 4 buildings × 3.5 hours = **14 hours**

---

## Kenney Source Files Required

### Space-Kit (Primary)

```
assets/source/kenney/space-kit/
├── Models/FBX format/
│   ├── structure.fbx              ✓ (House, Hospital, Builder, Granary)
│   ├── structure_detailed.fbx     ✓ (Farm, Stone, Guild)
│   ├── structure_closed.fbx       ✓ (Granary variant)
│   ├── tower-A.fbx                ✓ (Iron Mine, Infinite Iron)
│   ├── tower-B.fbx                ✓ (Soul Mine)
│   ├── gate.fbx                   ✓ (Gate)
│   ├── platform_large.fbx         ✓ (Farm base)
│   └── ... (other pieces)
│
└── Textures/
    ├── structure_albedo.png       (source for colorization)
    └── ... (generic Kenney textures)
```

### Modular-Space-Kit (Secondary)

```
assets/source/kenney/modular-space-kit/
├── Models/FBX format/
│   ├── gate.fbx                   (alternative gate variant)
│   └── ... (modular pieces for custom assembly)
```

---

## Estimated Timeline

### Week 1: Pilots + Framework Validation

```
Mon  - Validate pilot buildings (rep_house, cis_house) in Blender
       Fix any material/texture issues
       Document lessons learned

Tue  - Run texture generation (verify all 24 PNG files)
       Check generated colors match faction palettes

Wed  - Build Tier 1 buildings (House, Granary, Gate, Builder)
       → 4 buildings × 2 hours = 8 hours (1 artist, full day)

Thu  - Validate Tier 1 in game (scale, poly count, performance)
       Refine assembly process based on validation

Fri  - Build Tier 2 buildings (Farm, Hospital, Forester, Stone)
       → 4 buildings × 2.5 hours = 10 hours (2 artists, parallel)
       Start on Blender script refinement
```

**Week 1 Total**: 18 hours of assembly work

---

### Week 2: Tier 2-3 Build-Out

```
Mon  - Continue Tier 2 (if not finished)
       Start Tier 3 with senior artist (Iron Mine lead)

Tue  - Tier 3 construction (Iron, Infinite Iron, Guild, Soul)
       Parallel: Tier 1 validation in game
       Parallel: Texture refinement if needed

Wed  - Batch-run Blender script on completed buildings (if available)
       Test automated poly optimization

Thu  - Game validation pass (all 20+ buildings tested)
       Document any issues (scale, materials, performance)

Fri  - Fix issues, refine outliers, complete remaining builds
       Prepare for final validation
```

**Week 2 Total**: 24 hours of assembly work

---

### Week 3: Validation + Deployment

```
Mon  - Final 4 buildings (if any stragglers)
       Full suite game testing (all 24)

Tue  - Document final pipeline
       Generate before/after screenshots
       Update BUILD_CHECKLIST.md with completion status

Wed  - Pack integration (crosswalk manifests)
       Test in mod loader

Thu-Fri - Buffer time for fixes, documentation, team handoff
```

**Week 3 Total**: 12 hours (mostly testing/docs)

---

## Assembly Workflow per Building

Each building follows this **standard process**:

```
1. SETUP (10 min)
   ├─ Create new Blender project
   ├─ Import Kenney FBX source
   └─ Rename object for clarity

2. TEXTURE (30 min)
   ├─ Create material nodes
   ├─ Load faction texture PNG
   └─ Enable Material Preview

3. DETAILS (45 min)
   ├─ Add accent stripe (faction color)
   ├─ Add emblem or decal
   ├─ Optional: glow emitter or mechanical details
   └─ Polish faction-specific visuals

4. OPTIMIZE (20 min)
   ├─ Decimate poly count (target < 600 tris)
   ├─ Merge material slots
   └─ Center pivot

5. EXPORT (15 min)
   ├─ Configure FBX export settings
   ├─ Save to correct path
   └─ Verify export succeeded

6. VALIDATE (30 min)
   ├─ Reimport FBX in Blender
   ├─ Verify texture + colors
   ├─ Test in game (optional)
   └─ Document status in checklist

TOTAL: ~2.5 hours average
FAST: ~1.5 hours (simple buildings)
SLOW: ~4.5 hours (complex buildings + custom shaders)
```

---

## Parallel Execution Schedule

### Two-Artist Team (Recommended Minimum)

**Setup**:
- Artist A (Republic specialist): Buildings 1-12 (all Rep)
- Artist B (CIS specialist): Buildings 13-24 (all CIS)

**Advantage**: Each artist becomes expert in their faction palette and details.

**Timeline**:
```
Week 1:
  Artist A: Tier 1 Rep buildings (4 buildings, 8 hours)
  Artist B: Tier 1 CIS buildings (4 buildings, 8 hours)

Week 2:
  Artist A: Tier 2-3 Rep buildings (8 buildings, 22 hours, distributed)
  Artist B: Tier 2-3 CIS buildings (8 buildings, 22 hours, distributed)

Week 3: Both → Validation + Game Testing
```

**Effective timeline**: 3 weeks (parallel) vs 6 weeks (serial)

---

### Four-Artist Team (Full Parallelization)

**Setup**:
- Artist A: Buildings 1-6 (Residential, Simple)
- Artist B: Buildings 7-12 (Extraction, Medium)
- Artist C: Buildings 13-18 (Rep Continued, Medium-Hard)
- Artist D: Buildings 19-24 (CIS Advanced, Hard)

**Timeline**:
```
Week 1: All 4 artists build simultaneously
  A: 6 buildings × 2 hrs = 12 hours (spread over week)
  B: 6 buildings × 2.5 hrs = 15 hours
  C: 6 buildings × 3 hrs = 18 hours
  D: 6 buildings × 3.5 hrs = 21 hours

Week 2: Leverage learnings, batch optimize
  All artists: Continue + Begin game validation

Week 3: Final validation + deployment
```

**Effective timeline**: 2-3 weeks (overlapped work)

---

## Automation & Scripting

### Python Texture Generation

**Status**: ✓ Complete

**Script**: `assets/tools/generate_faction_textures.py`

**Usage**:
```bash
python assets/tools/generate_faction_textures.py
# Output: All 24 PNG files in assets/textures/buildings/
# Time: < 1 minute
```

**Regenerate if needed**:
```bash
python assets/tools/generate_faction_textures.py --force
```

---

### Blender Batch Assembly Script

**Status**: Framework ready (template provided)

**Script**: `assets/tools/blender_assemble_buildings.py`

**Usage**:
```bash
# Single-building mode (good for learning)
blender --python assets/tools/blender_assemble_buildings.py -- \
  --building rep_house_clone_quarters \
  --output assets/meshes/buildings/

# Batch mode (after validating pilots)
blender --python assets/tools/blender_assemble_buildings.py -- \
  --all \
  --output assets/meshes/buildings/ \
  --threads 4
```

**Current state**:
- ✓ Material node creation working
- ✓ Texture loading working
- ✓ FBX export working
- ⏳ Custom detail automation (stripes, emblems) needs refinement
- ⏳ Game validation integration needed

**Effort to full automation**: 4-6 hours of scripting + testing

---

## Quality Gates (Before Marking "Done")

Each building must pass these checks:

| Gate | Criteria | Test |
|------|----------|------|
| **Poly Count** | < 700 triangles | Check Blender stats panel |
| **Texture** | Visible, faction-correct colors | Material Preview mode |
| **Scale** | Matches vanilla footprint | Game visual comparison |
| **Materials** | All assigned, no missing | Import FBX, check shader nodes |
| **Performance** | No FPS drop in game | Run on target hardware |
| **Naming** | Follows convention (rep_/cis_) | Check FBX filename |
| **Export** | Valid FBX, no errors | Reimport and verify |
| **Mapping** | Entries in crosswalk manifest | Check YAML file |

---

## Risk & Mitigation

| Risk | Impact | Mitigation | Owner |
|------|--------|-----------|-------|
| **Kenney models don't scale** | All builds fail; process halts | Test Tier 1 pilots ASAP | Senior Artist |
| **Polygon budget exceeded** | Game performance drops | Use Decimate; budget 300-400 not 700 | Tech Artist |
| **Texture colors wrong** | Art style breaks | Validate palette RGB values; compare to reference | Lead Artist |
| **Material nodes lost on export** | Fallback to solid colors | Save .blend source; document workflow | All |
| **Schedule slip** | Miss deadline | Parallelize; reduce polish scope if needed | PM |
| **Skill gap** | Some artists struggle | Pair less-experienced with senior; pair programming | Lead |

---

## Deliverables Checklist

### By End of Week 1
- [ ] 4 Tier 1 buildings complete (House, Granary, Gate, Builder)
- [ ] Pilots validated in game (scale, textures, poly count)
- [ ] Process documented in BLENDER_ASSEMBLY_TEMPLATE.md
- [ ] Lessons learned captured

### By End of Week 2
- [ ] All 24 buildings have FBX files
- [ ] Poly counts within budget (< 600 tris average)
- [ ] Textures applied to all buildings
- [ ] Faction-specific details added (stripes, emblems, glows)

### By End of Week 3
- [ ] All 24 buildings tested in game
- [ ] Crosswalk manifests updated
- [ ] Asset pipeline documented for future use
- [ ] Team handoff complete

---

## Success Metrics

| Metric | Target | Current |
|--------|--------|---------|
| Buildings assembled | 24/24 | 2/24 (pilots) ✓ |
| Average build time | 2.5 hrs | TBD (in progress) |
| Poly count avg | < 600 tris | TBD |
| Texture quality | Faction-distinct | ✓ (PNG generated) |
| Game-ready FBX | 100% | 2/24 ✓ |
| Team velocity | 6-8 bldgs/week | TBD (week 1) |

---

## Tools & Resources

| Tool | Version | Purpose | Status |
|------|---------|---------|--------|
| Blender | 3.6+ | 3D assembly, texturing, export | Required |
| Python | 3.9+ | Automation scripts | For batch |
| Pillow | Latest | Texture generation | Installed |
| Git | Latest | Version control (.blend files) | Recommended |
| Unity Editor | 2021.3 | Game validation | For testing |
| Notepad++ / VS Code | Any | Documentation | Helpful |

---

## Team Communication Plan

### Daily Standup (10 min)

```
Each artist:
- What I built yesterday (buildings completed)
- What I'm building today (priority buildings)
- Blockers (Blender crashes, texture issues, etc.)
```

### Weekly Sync (30 min)

```
Mon 9 AM:
- Review completed buildings
- Address blockers
- Adjust priorities for the week
- Validate pilots in game
```

### Issue Tracking

```
Track in BUILD_CHECKLIST.md:
- Building ID
- Artist assigned
- Status (todo → in-progress → done)
- Hours spent
- Notes (issues, learnings)
```

---

## Lessons Learned Template

**After each building (or every 3-4), capture**:

```
Building: [ID]
Artist: [Name]
Time spent: [X hours]

What went well:
- ...

What was hard:
- ...

Tips for next building:
- ...

Texture/material changes made:
- ...
```

---

## Hand-Off Criteria

When handing off to next team (e.g., game implementation):

- [ ] All 24 FBX files in `assets/meshes/buildings/`
- [ ] All 24 textures in `assets/textures/buildings/`
- [ ] Crosswalk manifests updated and validated
- [ ] BUILD_CHECKLIST.md fully marked done
- [ ] BLENDER_ASSEMBLY_TEMPLATE.md documented
- [ ] Source `.blend` files archived (for future edits)
- [ ] Test results documented (game validation pass/fail)
- [ ] Known issues logged (any buildings needing polish)

---

## Future Optimization Opportunities

**After V1 delivery**:

1. **Full Blender automation** (4-6 hrs scripting)
   - Auto-apply textures
   - Auto-add stripe details
   - Auto-export with validation

2. **Custom shader library** (8-12 hrs)
   - Reusable emission shaders for glows
   - Weathering/rust overlays
   - Faction-specific material presets

3. **Asset variant generation** (6-8 hrs)
   - Script-driven color palette swaps
   - Damage/worn state variants
   - Season/weather variants

4. **Geometry simplification** (4-6 hrs)
   - ML-based poly optimization
   - Automatic LOD generation
   - Texture baking for detail preservation

---

## References

- **Build Checklist**: `BUILD_CHECKLIST.md` (all 24 buildings with sources)
- **Assembly Template**: `BLENDER_ASSEMBLY_TEMPLATE.md` (step-by-step guide)
- **Texture Generator**: `assets/tools/generate_faction_textures.py`
- **Blender Script**: `assets/tools/blender_assemble_buildings.py`
- **Kenney Assets**: `assets/source/kenney/` (CC0 licensed)
- **Faction Palettes**: See BUILD_CHECKLIST.md section 5

---

**Plan created**: 2026-03-12
**Status**: Ready for execution
**Next step**: Begin Week 1 Tier 1 builds
**Questions?** See BLENDER_ASSEMBLY_TEMPLATE.md for detailed step-by-step guide
