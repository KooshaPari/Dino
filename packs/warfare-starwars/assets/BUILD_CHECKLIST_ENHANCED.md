# Star Wars Building Assets — Complete Build Checklist (Enhanced)

**Purpose**: Master checklist for assembling 24 vanilla DINO building reskins from Kenney source models.

**Status**: Pipeline complete; assembly in progress (2/24 pilots done)

**Art Style**: Low-poly TABS aesthetic (300-700 tris per building, faction-colored)

---

## Quick Reference Tables

### All 24 Buildings at a Glance

| # | Vanilla Building | Rep Variant | CIS Variant | Kenney Source | Complexity | Est. Hours |
|---|---|---|---|---|---|---|
| 1 | house | Clone Quarters Pod | Droid Storage Pod | structure.fbx | Simple | 2.0 |
| 2 | farm | Hydroponic Farm Array | Fuel Cell Harvester | platform_large.fbx | Medium | 2.5 |
| 3 | granary | Nutrient Synthesizer | Power Cell Depot | structure.fbx | Simple | 2.0 |
| 4 | hospital | Clone Medical Bay | Droid Repair Station | structure.fbx | Medium | 2.5 |
| 5 | forester_house | Resource Extraction Post | Raw Material Extractor | structure.fbx | Medium | 2.5 |
| 6 | stone_cutter | Durasteel Refinery | Scrap Metal Works | structure_detailed.fbx | Hard | 3.5 |
| 7 | iron_mine | Tibanna Gas Extractor | Ore Processing Plant | tower-A.fbx | Hard | 3.5 |
| 8 | infinite_iron_mine | Deep-Core Tibanna Rig | Endless Ore Extractor | tower-A x2 (mirror) | Hard | 1.5 |
| 9 | soul_mine | Force Crystal Excavator | Dark Side Energy Tap | tower-B.fbx | Expert | 4.5 |
| 10 | builder_house | Republic Engineer Corps | Construction Droid Bay | structure.fbx | Simple | 2.0 |
| 11 | engineer_guild | Advanced Engineering Lab | Techno Union Workshop | structure_detailed.fbx | Expert | 4.5 |
| 12 | gate | Republic Security Gate | CIS Security Barrier | gate.fbx | Simple | 1.5 |

**Legend**:
- **Rep Variant**: Republic-themed name + white/blue palette
- **CIS Variant**: Confederacy-themed name + grey/orange palette
- **Complexity**: Simple (1.5-2 hrs) | Medium (2-3 hrs) | Hard (3-4 hrs) | Expert (4-5 hrs)
- **Est. Hours**: First-time assembly; 50% faster with practice

---

## Faction Color Palettes (Reference)

### Republic (Galactic Republic / Clone Wars Era)

Applied to ALL Republic buildings (rep_*):

| Element | Hex | RGB | Usage |
|---------|-----|-----|-------|
| **Primary** | `#F5F5F5` | 245, 245, 245 | Main body (bright white) |
| **Accent** | `#1A3A6B` | 26, 58, 107 | Stripes, trim, panels (navy blue) |
| **Light trim** | `#EEEEEE` | 238, 238, 238 | Highlights, edges (off-white) |
| **Detail gold** | `#FFD700` | 255, 215, 0 | Emblems, insignia only |
| **Medical red** | `#CC2222` | 204, 34, 34 | Danger/alert accents (rare) |

**Shader settings**:
- Metallic: 0.1 (subtle sheen)
- Roughness: 0.8 (matte, clean finish)
- Emission: None (unless specified for glow buildings)

---

### CIS (Confederacy of Independent Systems / Droid Army)

Applied to ALL CIS buildings (cis_*):

| Element | Hex | RGB | Usage |
|---------|-----|-----|-------|
| **Primary hull** | `#444444` | 68, 68, 68 | Main body (dark grey) |
| **Accent orange** | `#B35A00` | 179, 90, 0 | Stripes, rust, hazard (rust orange) |
| **Shadow dark** | `#2A2A2A` | 42, 42, 42 | Recesses, shadows |
| **Energy glow** | `#FF6600` | 255, 102, 0 | Emissive (orange heat/power) |
| **Alert red** | `#CC2222` | 204, 34, 34 | Danger states (droid/energy) |

**Shader settings**:
- Metallic: 0.2 (mechanical, more sheen)
- Roughness: 0.7 (industrial, less reflective)
- Emission: 0.5-2.0 (for energy-based buildings; see details below)

---

## Kenney Source Files Inventory

### Space-Kit (Primary: 9/12 buildings)

Location: `assets/source/kenney/space-kit/Models/FBX format/`

| File | Polygon Count | Buildings Using | Notes |
|---|---|---|---|
| `structure.fbx` | 1200-1500 | house, granary, hospital, forester, builder | Versatile base pod |
| `structure_detailed.fbx` | 2000-2500 | farm, stone_cutter, engineer_guild | Complex with details |
| `structure_closed.fbx` | 1000 | granary (variant) | Sealed container |
| `tower-A.fbx` | 1800-2000 | iron_mine, infinite_iron_mine (x2) | Derrick/drill tower |
| `tower-B.fbx` | 2200 | soul_mine | Tall mining rig |
| `gate.fbx` | 800-1000 | gate | Flat gate structure |
| `platform_large.fbx` | 1500 | farm (base) | Wide platform |
| Other pieces | Various | (Not yet used) | Spires, walls, pipes, chimneys |

### Modular-Space-Kit (Secondary: Fallback/Optional)

Location: `assets/source/kenney/modular-space-kit/Models/FBX format/`

- `gate.fbx` (alternative variant)
- Modular pieces for custom assembly (future use)

---

## Building-by-Building Assembly Guide

### GROUP 1: RESIDENTIAL & UTILITY (Hours: 6-8 total)

---

#### Building 1: HOUSE → Clone Quarters Pod / Droid Storage Pod

**Vanilla ID**: `house`

**Kenney Source**: `space-kit/structure.fbx` (base pod)

**Poly Budget**: 300-400 triangles

**Complexity**: ★☆☆ (Simple)

**Effort**: 2 hours (first time; 45 min with practice)

**Status**: ✓ **PILOTS COMPLETE** (rep + cis FBX built, textures applied)

##### Republic Variant: Clone Quarters Pod

| Aspect | Value |
|--------|-------|
| **Display Name** | Clone Quarters Pod |
| **ID** | rep_house_clone_quarters |
| **Texture** | rep_house_clone_quarters_albedo.png (white + blue) |
| **Details to Add** | Blue stripe on side; Clone emblem decal (white) on front |
| **FBX Output** | meshes/buildings/rep_house_clone_quarters.fbx ✓ DONE |

**Assembly Notes**:
- Keep dome roof visible
- Add white-painted panel on front (emblem location)
- Blue stripe wraps halfway around
- Simple geometry = quick iteration

##### CIS Variant: Droid Storage Pod

| Aspect | Value |
|--------|-------|
| **Display Name** | Droid Storage Pod |
| **ID** | cis_house_droid_pod |
| **Texture** | cis_house_droid_pod_albedo.png (grey + orange) |
| **Details to Add** | Rust stripes; droid rack outline (mechanical look) |
| **FBX Output** | meshes/buildings/cis_house_droid_pod.fbx ✓ DONE |

**Assembly Notes**:
- Add orange hazard stripes (diagonal pattern)
- Small mechanical details (box outline) suggesting droid storage
- Dark grey finish with weathered look

---

#### Building 2: GRANARY → Nutrient Synthesizer / Power Cell Depot

**Vanilla ID**: `granary`

**Kenney Source**: `space-kit/structure.fbx` OR `space-kit/structure_closed.fbx` (tall silo shape)

**Poly Budget**: 300-400 triangles

**Complexity**: ★☆☆ (Simple)

**Effort**: 2 hours

**Status**: ⏳ TODO

##### Republic Variant: Nutrient Synthesizer

| Aspect | Value |
|--------|-------|
| **Display Name** | Nutrient Synthesizer |
| **ID** | rep_granary_synthesizer |
| **Texture** | rep_granary_synthesizer_albedo.png |
| **Details to Add** | Side hatch (small square opening); blue panel accent; data display decal |
| **FBX Output** | meshes/buildings/rep_granary_synthesizer.fbx |

**Assembly Notes**:
- Tall cylinder shape (food → synthesized nutrients)
- Hatch on side (for loading)
- Blue accent panel (navigation/readout)
- White primary, clean finish
- Include small glow emitter point (for future VFX)

##### CIS Variant: Power Cell Depot

| Aspect | Value |
|--------|-------|
| **Display Name** | Power Cell Depot |
| **ID** | cis_granary_power_depot |
| **Texture** | cis_granary_power_depot_albedo.png |
| **Details to Add** | Stacked battery cell appearance; hazard stripes; red LED indicator points |
| **FBX Output** | meshes/buildings/cis_granary_power_depot.fbx |

**Assembly Notes**:
- Tall structure suggesting battery stack
- Orange hazard stripes (electrical warning)
- Red indicator points (energy readout)
- Mechanical, industrial finish (weathered grey)

---

#### Building 3: HOSPITAL → Clone Medical Bay / Droid Repair Station

**Vanilla ID**: `hospital`

**Kenney Source**: `space-kit/structure.fbx` + optional overlay for interior suggestion

**Poly Budget**: 300-400 triangles (rep) / 350-450 (cis)

**Complexity**: ★★☆ (Medium)

**Effort**: 2.5 hours

**Status**: ⏳ TODO

##### Republic Variant: Clone Medical Bay

| Aspect | Value |
|--------|-------|
| **Display Name** | Clone Medical Bay |
| **ID** | rep_hospital_medbay |
| **Texture** | rep_hospital_medbay_albedo.png |
| **Details to Add** | Interior window suggestion (transparent panel); blue medical trim; NO red cross (lore: uses blue trim only) |
| **FBX Output** | meshes/buildings/rep_hospital_medbay.fbx |

**Assembly Notes**:
- Compact prefab structure
- Window cutout (transparency for interior illusion)
- Blue trim = medical facility (replaces traditional red cross)
- Clean, sterile white finish
- Blender note: Add transparency shader for window

##### CIS Variant: Droid Repair Station

| Aspect | Value |
|--------|-------|
| **Display Name** | Droid Repair Station |
| **ID** | cis_hospital_repair_station |
| **Texture** | cis_hospital_repair_station_albedo.png |
| **Details to Add** | Open-frame structure with hanging articulated maintenance arm; tool racks visible; parts/droid visible |
| **FBX Output** | meshes/buildings/cis_hospital_repair_station.fbx |

**Assembly Notes**:
- More complex than Rep (adds custom arm)
- Open frame = industrial repair bay (no walls)
- Add small articulated arm (can be simple cylinder chain or Blender armature)
- Tool racks (simple box geometry)
- Droid parts visible (small boxes, mechanical look)
- Grey + orange palette, mechanical finish

---

### GROUP 2: RESOURCE EXTRACTION (Hours: 8-12 total)

---

#### Building 4: FORESTER HOUSE → Resource Extraction Post / Raw Material Extractor

**Vanilla ID**: `forester_house`

**Kenney Source**: `space-kit/structure.fbx` + antenna piece

**Poly Budget**: 250-300 triangles

**Complexity**: ★★☆ (Medium)

**Effort**: 2.5 hours (rep: 2 hrs; cis: 3 hrs due to spider legs)

**Status**: ⏳ TODO

##### Republic Variant: Resource Extraction Post

| Aspect | Value |
|--------|-------|
| **Display Name** | Resource Extraction Post |
| **ID** | rep_forester_extraction_post |
| **Texture** | rep_forester_extraction_post_albedo.png |
| **Details to Add** | Sensor array on top (small dome + antenna); resource extraction arm; Republic branding |
| **FBX Output** | meshes/buildings/rep_forester_extraction_post.fbx |

**Assembly Notes**:
- Small footprint (outpost, not factory)
- Sensor dome on top
- Extraction arm (articulated, can be simple or rigged)
- Antenna (optional, for flavor)
- White + blue, minimal branding

##### CIS Variant: Raw Material Extractor

| Aspect | Value |
|--------|-------|
| **Display Name** | Raw Material Extractor |
| **ID** | cis_forester_raw_extractor |
| **Texture** | cis_forester_raw_extractor_albedo.png |
| **Details to Add** | Spider-leg anchors (4 legs); rotating claw head attachment; mechanical design; CIS minimal branding |
| **FBX Output** | meshes/buildings/cis_forester_raw_extractor.fbx |

**Assembly Notes**:
- More complex (spider-leg attachment system)
- 4 anchor legs (can be simple tapered cylinders)
- Claw head (rotates for extraction; may need armature)
- Dark grey + orange, industrial
- **HIGHER EFFORT**: Custom spider-leg kit needs modeling

---

#### Building 5: STONE CUTTER → Durasteel Refinery / Scrap Metal Works

**Vanilla ID**: `stone_cutter`

**Kenney Source**: `space-kit/structure_detailed.fbx` (complex structure with detail)

**Poly Budget**: 400-500 triangles

**Complexity**: ★★★ (Hard)

**Effort**: 3-3.5 hours

**Status**: ⏳ TODO

##### Republic Variant: Durasteel Refinery

| Aspect | Value |
|--------|-------|
| **Display Name** | Durasteel Refinery |
| **ID** | rep_stone_durasteel_refinery |
| **Texture** | rep_stone_durasteel_refinery_albedo.png |
| **Details to Add** | Multiple chimneys; molten-glow windows (blue emission); conveyor input visualized |
| **FBX Output** | meshes/buildings/rep_stone_durasteel_refinery.fbx |

**Assembly Notes**:
- Multiple chimneys (3-4, stack on top)
- Windows with blue emission (molten durasteel glow)
- Conveyor visual (input tray on side)
- White/grey industrial, heavy-duty
- **Shader work**: Emission nodes for glowing windows

##### CIS Variant: Scrap Metal Works

| Aspect | Value |
|--------|-------|
| **Display Name** | Scrap Metal Works |
| **ID** | cis_stone_scrap_works |
| **Texture** | cis_stone_scrap_works_albedo.png |
| **Details to Add** | Exposed gears (mechanical look); jagged mesh panels; debris pile visual; orange/grey with rust |
| **FBX Output** | meshes/buildings/cis_stone_scrap_works.fbx |

**Assembly Notes**:
- Exposed machinery (gears visible on exterior)
- Jagged/angular panels (scrap aesthetic)
- Debris pile effect (stacked geometry near base)
- Orange hazard stripes
- VFX prep: note spark emitter location
- **HARDER**: More custom geometry needed

---

#### Building 6: IRON MINE → Tibanna Gas Extractor / Ore Processing Plant

**Vanilla ID**: `iron_mine`

**Kenney Source**: `space-kit/tower-A.fbx` (derrick tower)

**Poly Budget**: 400-500 triangles

**Complexity**: ★★★ (Hard)

**Effort**: 3-3.5 hours

**Status**: ⏳ TODO

##### Republic Variant: Tibanna Gas Extractor

| Aspect | Value |
|--------|-------|
| **Display Name** | Tibanna Gas Extractor |
| **ID** | rep_iron_tibanna_extractor |
| **Texture** | rep_iron_tibanna_extractor_albedo.png |
| **Details to Add** | Derrick frame (tall); pressurized tanks on side; gas conduit tubing; Republic insignia; white/grey finish |
| **FBX Output** | meshes/buildings/rep_iron_tibanna_extractor.fbx |

**Assembly Notes**:
- Tall derrick frame (tower-A base)
- Pressurized tanks (cylinders, blue accent)
- Conduit tubing (white pipes)
- Republic insignia (small decal, gold optional)
- Industrial, extraction rig aesthetic

##### CIS Variant: Ore Processing Plant

| Aspect | Value |
|--------|-------|
| **Display Name** | Ore Processing Plant |
| **ID** | cis_iron_ore_plant |
| **Texture** | cis_iron_ore_plant_albedo.png |
| **Details to Add** | Compact ore conveyor belt; grinder housing; rust-orange paint; Techno Union branding |
| **FBX Output** | meshes/buildings/cis_iron_ore_plant.fbx |

**Assembly Notes**:
- Compact tower with horizontal emphasis (vs. Rep's tall derrick)
- Conveyor belt (visual, may be static or rigged for animation)
- Grinder housing (mechanical, exposed)
- Rust/orange tones, industrial weathered
- Techno Union logo/insignia (small, mechanical style)

---

#### Building 7: INFINITE IRON MINE → Deep-Core Tibanna Rig / Endless Ore Extractor

**Vanilla ID**: `infinite_iron_mine`

**Kenney Source**: `space-kit/tower-A.fbx` x2 (duplicated + mirrored)

**Poly Budget**: 500-600 triangles

**Complexity**: ★★★ (Hard) — **BUT FASTER** (duplicate + mirror saves 1.5 hrs)

**Effort**: 1.5 hours (**OPTIMIZATION**: Share tower-A model with building 6)

**Status**: ⏳ TODO

##### Republic Variant: Deep-Core Tibanna Rig

| Aspect | Value |
|--------|-------|
| **Display Name** | Deep-Core Tibanna Rig |
| **ID** | rep_iron_deep_core_rig |
| **Texture** | rep_iron_deep_core_rig_albedo.png |
| **Details to Add** | Twin derricks (mirrored tower-A); flare exhaust stack between; white/grey; larger footprint |
| **FBX Output** | meshes/buildings/rep_iron_deep_core_rig.fbx |

**Assembly Notes**:
- **REUSE**: tower-A from building 6; duplicate + mirror
- Flare stack (tall cylinder between derricks)
- Twin rig aesthetic (industrial extraction scale)
- Larger footprint than single mine
- White/grey finish

##### CIS Variant: Endless Ore Extractor

| Aspect | Value |
|--------|-------|
| **Display Name** | Endless Ore Extractor |
| **ID** | cis_iron_endless_extractor |
| **Texture** | cis_iron_endless_extractor_albedo.png |
| **Details to Add** | Dual-bore setup; heavy orange/grey rust tones; Techno Union insignia |
| **FBX Output** | meshes/buildings/cis_iron_endless_extractor.fbx |

**Assembly Notes**:
- Duplicate tower-A geometry from building 6 CIS variant
- Add flare/exhaust between (simple cylinder)
- Dual-bore aesthetic
- Heavy rust overlay on textures
- **TIME SAVINGS**: 1.5 hrs vs 3.5 hrs for full build

---

#### Building 8: SOUL MINE → Force Crystal Excavator / Dark Side Energy Tap

**Vanilla ID**: `soul_mine`

**Kenney Source**: `space-kit/tower-B.fbx` (taller, more complex rig)

**Poly Budget**: 500-600 triangles

**Complexity**: ★★★★ (Expert) — **HIGHEST ARTISTIC DEMAND**

**Effort**: 4-4.5 hours (**CUSTOM SHADERS** required)

**Status**: ⏳ TODO

##### Republic Variant: Force Crystal Excavator

| Aspect | Value |
|--------|-------|
| **Display Name** | Force Crystal Excavator |
| **ID** | rep_soul_crystal_excavator |
| **Texture** | rep_soul_crystal_excavator_albedo.png |
| **Details to Add** | Crystal mining cage (angular frame); Kyber crystal glow (purple/blue emission); custom Blender shader for resonance |
| **FBX Output** | meshes/buildings/rep_soul_crystal_excavator.fbx |

**Assembly Notes**:
- Angular cage frame (tower-B base + custom geometry)
- **SHADER WORK**: Emission material for glowing crystal
  - Purple/blue emission (Kyber crystal resonance)
  - Strength: 1.5-2.0
  - Metallic: 0.3 (crystalline reflection)
- Mystical aesthetic (lore: Force crystals)
- **HIGHEST EFFORT**: Custom shader + custom geometry

##### CIS Variant: Dark Side Energy Tap

| Aspect | Value |
|--------|-------|
| **Display Name** | Dark Side Energy Tap |
| **ID** | cis_soul_dark_energy_tap |
| **Texture** | cis_soul_dark_energy_tap_albedo.png |
| **Details to Add** | Black lattice frame; red/purple resonance glow; alien rune engravings (decals); dark emission shader |
| **FBX Output** | meshes/buildings/cis_soul_dark_energy_tap.fbx |

**Assembly Notes**:
- Black lattice frame (tower-B base + custom details)
- **SHADER WORK**: Red/purple emission
  - Strength: 1.0-1.5
  - Metallic: 0.2
  - Roughness: 0.5 (menacing, less reflective than Rep)
- Alien rune engravings (decal texture or geometry)
- Menacing, dark Side aesthetic
- **VERY HIGH EFFORT**: Custom shader + detailed geometry + rune work

---

### GROUP 3: MILITARY & PRODUCTION (Hours: 8-10 total)

---

#### Building 9: BUILDER HOUSE → Republic Engineer Corps / Construction Droid Bay

**Vanilla ID**: `builder_house`

**Kenney Source**: `space-kit/structure.fbx` + crane arm

**Poly Budget**: 300-400 triangles

**Complexity**: ★☆☆ (Simple)

**Effort**: 2 hours

**Status**: ⏳ TODO

##### Republic Variant: Republic Engineer Corps

| Aspect | Value |
|--------|-------|
| **Display Name** | Republic Engineer Corps |
| **ID** | rep_builder_engineer_corps |
| **Texture** | rep_builder_engineer_corps_albedo.png |
| **Details to Add** | Mobile depot structure; crane arm; tool racks; supply crates; Republic emblem; white/blue |
| **FBX Output** | meshes/buildings/rep_builder_engineer_corps.fbx |

**Assembly Notes**:
- Mobile engineering depot (structure.fbx base)
- Crane arm (can be simple articulated cylinder or rigged)
- Tool racks (small boxes)
- Supply crates (geometric, stacked)
- Republic branding/emblem
- Clean, organized aesthetic

##### CIS Variant: Construction Droid Bay

| Aspect | Value |
|--------|-------|
| **Display Name** | Construction Droid Bay |
| **ID** | cis_builder_droid_bay |
| **Texture** | cis_builder_droid_bay_albedo.png |
| **Details to Add** | Lifting arm; storage racks with B1 droid silhouettes; CIS grey/orange finish |
| **FBX Output** | meshes/buildings/cis_builder_droid_bay.fbx |

**Assembly Notes**:
- Droid-operated assembly depot
- Lifting arm (automated, mechanical look)
- Racks with droid silhouettes (decals or small geometry)
- Grey + orange, industrial droid workshop
- Mechanical, less "tidy" than Republic

---

#### Building 10: ENGINEER GUILD → Advanced Engineering Lab / Techno Union Workshop

**Vanilla ID**: `engineer_guild`

**Kenney Source**: `space-kit/structure_detailed.fbx` OR custom modular assembly

**Poly Budget**: 500-600 triangles (rep) / 600-700 (cis)

**Complexity**: ★★★★ (Expert)

**Effort**: 3.5-4.5 hours (**CIS MUCH HARDER**: asymmetric layout)

**Status**: ⏳ TODO

##### Republic Variant: Advanced Engineering Lab

| Aspect | Value |
|--------|-------|
| **Display Name** | Advanced Engineering Lab |
| **ID** | rep_guild_engineering_lab |
| **Texture** | rep_guild_engineering_lab_albedo.png |
| **Details to Add** | Large footprint with symmetrical wings; holographic display dome (blue emission); white/blue finish |
| **FBX Output** | meshes/buildings/rep_guild_engineering_lab.fbx |

**Assembly Notes**:
- Large building (symmetrical layout)
- Two wing structures (mirror-symmetric)
- Holographic display dome on top
  - **SHADER**: Blue emission for hologram
  - Strength: 1.0
  - Transparency: 50% (suggestion of projection)
- Clean, organized (Republic aesthetic)
- Higher poly count acceptable (600 max)

##### CIS Variant: Techno Union Workshop

| Aspect | Value |
|--------|-------|
| **Display Name** | Techno Union Workshop |
| **ID** | cis_guild_techno_workshop |
| **Texture** | cis_guild_techno_workshop_albedo.png |
| **Details to Add** | Asymmetric organic-tech hybrid layout (MANUAL REARRANGEMENT); alien architecture sensibility; Techno Union logo; grey/orange |
| **FBX Output** | meshes/buildings/cis_guild_techno_workshop.fbx |

**Assembly Notes**:
- **MOST COMPLEX**: Asymmetric design (NOT mirror layout)
- Organic-tech hybrid (curved + angular mixed)
- Alien architecture language (non-standard proportions)
- Techno Union logo/insignia
- Grey + orange industrial aesthetic
- **EFFORT**: Requires manual Blender rearrangement; cannot reuse Rep layout
- **LONGER ASSEMBLY**: 4-4.5 hrs (heaviest single building)

---

#### Building 11: GATE → Republic Security Gate / CIS Security Barrier

**Vanilla ID**: `gate`

**Kenney Source**: `space-kit/gate.fbx` OR `modular-space-kit/gate.fbx`

**Poly Budget**: 250-350 triangles

**Complexity**: ★☆☆ (Simple)

**Effort**: 1.5-2 hours

**Status**: ⏳ TODO

##### Republic Variant: Republic Security Gate

| Aspect | Value |
|--------|-------|
| **Display Name** | Republic Security Gate |
| **ID** | rep_gate_security_gate |
| **Texture** | rep_gate_security_gate_albedo.png |
| **Details to Add** | Sliding gate frame; Republic insignia panels; white/blue; clean lines |
| **FBX Output** | meshes/buildings/rep_gate_security_gate.fbx |

**Assembly Notes**:
- Sliding gate mechanism (base gate.fbx)
- Insignia panels (small decals)
- Republic branding
- Clean, authoritative appearance
- Lowest poly count (< 300 tris possible)

##### CIS Variant: CIS Security Barrier

| Aspect | Value |
|--------|-------|
| **Display Name** | CIS Security Barrier |
| **ID** | cis_gate_security_barrier |
| **Texture** | cis_gate_security_barrier_albedo.png |
| **Details to Add** | Armored sliding gate; sentry droid alcoves on flanking pillars; dark grey barrier; red energy field indicator stripe |
| **FBX Output** | meshes/buildings/cis_gate_security_barrier.fbx |

**Assembly Notes**:
- Armored gate (heavier than Rep)
- Sentry droid alcoves (small box recesses on side pillars)
- Red energy field indicator stripe
- Dark grey, mechanical, threatening
- Droid sentry aesthetic

---

## Artifact Output Targets

### Republic Buildings (12 files)

```
FBX Output Directory: assets/meshes/buildings/

✓ rep_house_clone_quarters.fbx              (DONE - pilots)
⏳ rep_farm_hydroponic.fbx
⏳ rep_granary_synthesizer.fbx
⏳ rep_hospital_medbay.fbx
⏳ rep_forester_extraction_post.fbx
⏳ rep_stone_durasteel_refinery.fbx
⏳ rep_iron_tibanna_extractor.fbx
⏳ rep_iron_deep_core_rig.fbx
⏳ rep_soul_crystal_excavator.fbx                 (HIGH EFFORT - shaders)
⏳ rep_builder_engineer_corps.fbx
⏳ rep_guild_engineering_lab.fbx               (HIGH EFFORT - asymmetric)
⏳ rep_gate_security_gate.fbx

Textures: assets/textures/buildings/
✓ rep_house_clone_quarters_albedo.png           (DONE)
✓ rep_farm_hydroponic_albedo.png                (DONE)
✓ rep_granary_synthesizer_albedo.png            (DONE)
✓ rep_hospital_medbay_albedo.png                (DONE)
✓ rep_forester_extraction_post_albedo.png       (DONE)
✓ rep_stone_durasteel_refinery_albedo.png       (DONE)
✓ rep_iron_tibanna_extractor_albedo.png         (DONE)
✓ rep_iron_deep_core_rig_albedo.png             (DONE)
✓ rep_soul_crystal_excavator_albedo.png         (DONE)
✓ rep_builder_engineer_corps_albedo.png         (DONE)
✓ rep_guild_engineering_lab_albedo.png          (DONE)
✓ rep_gate_security_gate_albedo.png             (DONE)
```

### CIS Buildings (12 files)

```
FBX Output Directory: assets/meshes/buildings/

✓ cis_house_droid_pod.fbx                   (DONE - pilots)
⏳ cis_farm_fuel_harvester.fbx
⏳ cis_granary_power_depot.fbx
⏳ cis_hospital_repair_station.fbx
⏳ cis_forester_raw_extractor.fbx                 (HIGH EFFORT - spider legs)
⏳ cis_stone_scrap_works.fbx
⏳ cis_iron_ore_plant.fbx
⏳ cis_iron_endless_extractor.fbx             (FAST - shares iron_mine)
⏳ cis_soul_dark_energy_tap.fbx                  (HIGH EFFORT - dark shaders)
⏳ cis_builder_droid_bay.fbx
⏳ cis_guild_techno_workshop.fbx              (HIGHEST EFFORT - asymmetric)
⏳ cis_gate_security_barrier.fbx

Textures: assets/textures/buildings/
✓ cis_house_droid_pod_albedo.png                (DONE)
✓ cis_farm_fuel_harvester_albedo.png            (DONE)
✓ cis_granary_power_depot_albedo.png            (DONE)
✓ cis_hospital_repair_station_albedo.png        (DONE)
✓ cis_forester_raw_extractor_albedo.png         (DONE)
✓ cis_stone_scrap_works_albedo.png              (DONE)
✓ cis_iron_ore_plant_albedo.png                 (DONE)
✓ cis_iron_endless_extractor_albedo.png         (DONE)
✓ cis_soul_dark_energy_tap_albedo.png           (DONE)
✓ cis_builder_droid_bay_albedo.png              (DONE)
✓ cis_guild_techno_workshop_albedo.png          (DONE)
✓ cis_gate_security_barrier_albedo.png          (DONE)
```

---

## Complexity Ratings & Time Estimates

### SIMPLE (1.5-2 hours each)

- house (both)
- granary (both)
- gate (both)
- builder_house (both)

**Total**: 8 buildings, 16 hours

---

### MEDIUM (2-3 hours each)

- farm (both)
- hospital (both)
- forester_house (rep: 2 hrs; cis: 3 hrs due to spider legs)
- stone_cutter (both: 3-3.5 hrs each)

**Total**: 8 buildings, ~24 hours (note: cis_forester + both stone_cutters are higher)

---

### HARD (3-4 hours each)

- iron_mine (both: 3-3.5 hrs each)
- infinite_iron_mine (**1.5 hrs** - mirror of iron_mine, saves 2 hrs)
- engineer_guild (rep: 3.5 hrs; cis: 4-4.5 hrs)

**Total**: 7 buildings, ~22 hours (but infinite_iron is optimized)

---

### EXPERT (4-4.5 hours each)

- soul_mine (both: 4-4.5 hrs each) — **Custom shaders required**
- engineer_guild CIS (**4-4.5 hrs**) — **Asymmetric layout, highest single-building effort**

**Total**: 3 buildings (if counting soul_mine + guild as separate), ~13 hours

---

## Grand Total Estimate

| Tier | Buildings | Hours | Notes |
|------|-----------|-------|-------|
| Simple | 8 | 16 | Quick wins; Tier 1 priority |
| Medium | 8 | 24 | Moderate complexity; Tier 2 |
| Hard | 7 | 22 | Complex; includes infinite_iron optimization |
| Expert | 3+ | 13 | Highest effort; custom shaders/geometry |
| **TOTAL** | **24** | **60-72** | ~2.5 hrs/building average |

**With optimizations** (infinite_iron reuse, parallel work): **52-60 hours**

**With team of 4**: **2-3 weeks parallel**

---

## Quality Checkpoints

Every completed building must pass:

| Checkpoint | Criteria | Pass/Fail |
|---|---|---|
| **Poly Count** | < 700 tris (target 300-600) | MUST |
| **Texture** | Visible, faction-correct, no missing maps | MUST |
| **Scale** | Matches vanilla building footprint (visual comparison) | MUST |
| **Materials** | All assigned, no "magenta missing" errors | MUST |
| **Normals** | No dark patches (flipped faces) | MUST |
| **FBX Export** | Valid file, reimports cleanly | MUST |
| **Game Test** | Renders in game, no FPS drop, correct appearance | SHOULD |
| **Details** | Faction-specific features visible (stripes, emblems, glows) | SHOULD |
| **Documentation** | Status updated in checklist + notes | SHOULD |

---

## Kenney License Note

All Kenney assets used are **CC0 (Public Domain)**. No attribution required, full reuse permitted.

Source: https://kenney.nl

---

## References & Links

| Resource | Purpose | Location |
|---|---|---|
| **BLENDER_ASSEMBLY_TEMPLATE.md** | Step-by-step guide for one building | This directory |
| **BATCH_ASSEMBLY_PLAN.md** | Parallelization & scheduling | This directory |
| **Faction Palettes** | Hex colors & shader settings | Section 2 above |
| **Kenney Sources** | FBX models | `assets/source/kenney/` |
| **Generated Textures** | 24 PNG faction textures | `assets/textures/buildings/` |
| **Texture Generator** | Python script | `assets/tools/generate_faction_textures.py` |
| **Blender Script** | Batch automation template | `assets/tools/blender_assemble_buildings.py` |

---

## Success Criteria (Final Delivery)

- [ ] All 24 FBX files generated (rep + cis)
- [ ] All 24 textures applied (PNG loaded in shaders)
- [ ] Poly counts within budget (< 700 tris, target 300-600)
- [ ] Faction-specific details added (stripes, emblems, glows)
- [ ] All buildings tested in game (visual validation)
- [ ] Crosswalk manifests updated (`crosswalk_republic_vanilla.yaml`, `crosswalk_cis_vanilla.yaml`)
- [ ] Source `.blend` files archived (for future edits)
- [ ] This checklist marked 100% complete
- [ ] Team documentation finalized

---

**Created**: 2026-03-12
**Status**: Ready for batch assembly (pilots complete)
**Next**: Begin Tier 1 buildings (see BATCH_ASSEMBLY_PLAN.md)
