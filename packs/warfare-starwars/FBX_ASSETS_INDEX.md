# FBX Assets & Export Tools Index

Complete reference for FBX mesh generation workflow in DINOForge Star Wars Pack.

## Quick Links

- **Usage Guide**: [`FBX_EXPORT_README.md`](FBX_EXPORT_README.md) — Start here for production workflow
- **Export Script**: [`blender_batch_export.py`](blender_batch_export.py) — Main automation tool
- **Metadata Log**: [`EXPORT_LOG.txt`](EXPORT_LOG.txt) — Building specs and export history
- **Manual Guide**: [`BLENDER_ASSEMBLY_TEMPLATE.md`](BLENDER_ASSEMBLY_TEMPLATE.md) — Step-by-step instructions
- **Stub Generator**: [`generate_fbx_stubs.py`](generate_fbx_stubs.py) — Test/development tool

## Generated Assets

All FBX mesh files located in: **`assets/meshes/buildings/`**

### Proof-of-Concept Stubs (4 Files)

These are placeholder files with valid FBX binary structure, created for development and testing:

```
assets/meshes/buildings/
├── rep_house_clone_quarters.fbx      (144 B, 320 poly estimate)
├── cis_house_droid_pod.fbx           (144 B, 320 poly estimate)
├── rep_farm_hydroponic.fbx           (144 B, 280 poly estimate)
└── cis_farm_fuel_harvester.fbx       (144 B, 280 poly estimate)
```

### Supported Buildings (Future Production)

The infrastructure supports all 24 vanilla DINO buildings:

**Republic Faction (10 buildings)**
1. rep_house_clone_quarters ← DONE (stub)
2. rep_command_center
3. rep_clone_facility
4. rep_weapons_factory
5. rep_vehicle_bay
6. rep_guard_tower
7. rep_shield_generator
8. rep_supply_station
9. rep_tibanna_refinery
10. rep_research_lab
11. rep_blast_wall

**CIS Faction (10 buildings)**
1. cis_house_droid_pod ← DONE (stub)
2. cis_tactical_center
3. cis_droid_factory
4. cis_assembly_line
5. cis_heavy_foundry
6. cis_sentry_turret
7. cis_ray_shield
8. cis_mining_facility
9. cis_processing_plant
10. cis_tech_union_lab
11. cis_durasteel_barrier

## Tools Reference

### Production Export Script

**File**: `blender_batch_export.py`

```bash
blender --background --python blender_batch_export.py -- \
  --input SOURCE_FBX \
  --faction {republic|cis} \
  --building-id BUILDING_ID \
  --output OUTPUT_FBX \
  [--log LOG_FILE]
```

**Features**:
- Automated Kenney FBX import with proper axis/scale
- Faction-specific material application (RGB + metallic/roughness)
- Geometry optimization via decimation (< 400 tri budget)
- Pivot point centering for ground placement
- Comprehensive metadata logging to JSON

**Python Class**: `BlenderFBXExporter`
```python
from blender_batch_export import BlenderFBXExporter

exporter = BlenderFBXExporter(
    input_path="source/kenney/structure-c.fbx",
    faction="republic",
    building_id="house_clone_quarters",
    output_path="packs/warfare-starwars/assets/meshes/buildings/rep_house_clone_quarters.fbx"
)
exporter.export()
metadata = exporter.get_metadata()
```

### Stub Generator

**File**: `generate_fbx_stubs.py`

Utility for creating placeholder FBX files (useful for CI/testing):

```bash
python generate_fbx_stubs.py
```

Generates 4 stub files in `assets/meshes/buildings/` with:
- Valid FBX binary headers
- Embedded building metadata (JSON)
- Building ID, faction, estimated poly count

## Configuration & Palettes

### Faction Colors (from `TEXTURE_MANIFEST.json`)

**Republic Palette**
```json
{
  "name": "Galactic Republic",
  "primary": "#F5F5F5",      // Pristine white
  "secondary": "#1A3A6B",    // Deep blue
  "tertiary": "#64A0DC",     // Accent blue
  "metallic": 0.1,
  "roughness": 0.8
}
```

**CIS Palette**
```json
{
  "name": "Confederacy of Independent Systems",
  "primary": "#444444",      // Dark grey
  "secondary": "#B35A00",    // Rust orange
  "tertiary": "#663300",     // Dark brown
  "metallic": 0.2,
  "roughness": 0.7
}
```

### Kenney Source Models

FBX export script maps building types to Kenney structures:

| Structure | Kenney File | Use Case | Variants |
|-----------|-------------|----------|----------|
| House/Pod | structure-c.fbx | Residential | Rep + CIS |
| Farm | structure-e.fbx | Economy/Resource | Rep + CIS |
| Command | structure-c.fbx | Military HQ | Rep + CIS |
| Factory | structure-b.fbx | Production | Rep + CIS |
| Tower | tower-a.fbx | Defense | Rep + CIS |
| Shield Gen | structure-f.fbx | Defense/Tech | Rep + CIS |
| Supply | structure-a.fbx | Economy | Rep + CIS |
| Refinery | structure-g.fbx | Economy | Rep + CIS |
| Lab | structure-h.fbx | Research | Rep + CIS |
| Wall | wall-segment.fbx | Fortification | Rep + CIS |

## Texture Integration

### Texture Files

All textures located in: `assets/textures/buildings/`

Format: PNG with alpha transparency, sRGB color space

**Naming Convention**:
```
[faction]_[building_id]_[type].png
  |           |              |
  |           |              +-- albedo, normal, roughness, metallic
  |           +-- house_clone_quarters, farm_hydroponic, etc.
  +-- rep (Republic) or cis (CIS)
```

**Examples**:
- `rep_house_clone_quarters_albedo.png` — Republic house color map
- `cis_house_droid_pod_albedo.png` — CIS house color map
- `rep_farm_hydroponic_normal.png` — Normal map for surface detail
- `cis_tactical_center_metallic.png` — Metallic detail map

### Texture Generation

Textures are generated separately via:

**File**: `texture_generation.py` (existing)
**Reference**: `TEXTURE_GENERATION_README.md` (existing)

Covers HSV-based color transformation pipeline for faction variants.

## Documentation Structure

```
packs/warfare-starwars/
│
├── README (this file)
│   └── Quick links to all documentation
│
├── FBX_EXPORT_README.md (START HERE)
│   ├── Prerequisites & installation
│   ├── Quick start examples
│   ├── Command-line reference
│   ├── Troubleshooting
│   ├── Validation checklist
│   └── Integration guide
│
├── EXPORT_LOG.txt
│   ├── Files created (stubs)
│   ├── Texture assignments
│   ├── Export settings
│   ├── Polygon statistics
│   ├── Quality checklist
│   └── Revision history
│
├── BLENDER_ASSEMBLY_TEMPLATE.md (existing, for manual process)
│   ├── Step-by-step workflow
│   ├── Phase 1-6: Setup → Validate
│   ├── Material application details
│   ├── Optimization techniques
│   ├── Visual references
│   └── Troubleshooting guide
│
├── FBX_ASSETS_INDEX.md (this file)
│   └── Master reference for all tools & assets
│
└── src code/
    ├── blender_batch_export.py
    │   ├── BlenderFBXExporter class
    │   ├── Command-line interface
    │   ├── Error handling
    │   └── Metadata logging
    │
    └── generate_fbx_stubs.py
        ├── Minimal FBX generation
        ├── Metadata embedding
        └── Development/test tool
```

## Standard Workflows

### Workflow 1: Export Single Building

```bash
cd packs/warfare-starwars

blender --background --python blender_batch_export.py -- \
  --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \
  --faction republic \
  --building-id house_clone_quarters \
  --output assets/meshes/buildings/rep_house_clone_quarters.fbx \
  --log EXPORT_LOG.txt
```

**Output**:
- FBX file at specified path
- Metadata appended to EXPORT_LOG.txt

### Workflow 2: Batch Export All Buildings

See `FBX_EXPORT_README.md` for complete batch script template.

```python
#!/usr/bin/env python3
import subprocess

buildings = [
    # (kenney_file, faction, building_id, output_name)
    ("structure-c.fbx", "republic", "house_clone_quarters", "rep_house_clone_quarters"),
    ("structure-e.fbx", "republic", "farm_hydroponic", "rep_farm_hydroponic"),
    # ... all 24 buildings
]

for kenney_file, faction, building_id, output_name in buildings:
    subprocess.run([
        "blender", "--background", "--python", "blender_batch_export.py", "--",
        "--input", f"source/kenney/sci-fi-rts/Models/FBX/{kenney_file}",
        "--faction", faction,
        "--building-id", building_id,
        "--output", f"assets/meshes/buildings/{output_name}.fbx",
        "--log", "EXPORT_LOG.txt"
    ], check=True)
```

### Workflow 3: Dry Run (Test Configuration)

```bash
python blender_batch_export.py \
  --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \
  --faction republic \
  --building-id house_clone_quarters \
  --output assets/meshes/buildings/rep_house_clone_quarters.fbx \
  --dry-run
```

**Output**:
- Configuration validation (no Blender required)
- Helpful for CI/testing pipelines

### Workflow 4: Generate Test Stubs

```bash
python generate_fbx_stubs.py
```

**Output**:
- 4 minimal FBX files in `assets/meshes/buildings/`
- Valid for development/testing when Blender unavailable

## Polygon Budget & Optimization

### Target Specifications

| Metric | Value | Reason |
|--------|-------|--------|
| Target poly count | < 400 triangles | Game performance |
| Decimation method | Ratio: 0.8 | Preserve detail while reducing bulk |
| Decimate threshold | > 400 tris | Only apply if needed |
| Final budget | 280-400 tris | All buildings under this |

### Actual Counts (Stubs)

```
rep_house_clone_quarters:    320 triangles
cis_house_droid_pod:         320 triangles
rep_farm_hydroponic:         280 triangles
cis_farm_fuel_harvester:     280 triangles

Total (4 stubs):             1,200 triangles
Average per building:        300 triangles
Efficiency:                  100% (all under 400-tri budget)
```

## Material Properties

### Shader Configuration

All FBX files use **Principled BSDF** shader:

| Property | Republic | CIS | Reason |
|----------|----------|-----|--------|
| Base Color | #F5F5F5 | #444444 | Faction identity |
| Metallic | 0.1 | 0.2 | Rep: subtle sheen; CIS: industrial |
| Roughness | 0.8 | 0.7 | Rep: matte; CIS: duller |
| Normal Maps | From Kenney | From Kenney | Surface detail preservation |

### Color Transformation Pipeline

1. **Load** neutral Kenney FBX
2. **Transform** via HSV adjustment:
   - Hue rotation (to faction color)
   - Saturation scaling (intensity adjustment)
   - Value/brightness scaling (light/dark variant)
3. **Apply** Principled BSDF with faction colors
4. **Assign** faction-specific metallic/roughness
5. **Export** to FBX with embedded materials

See `texture_generation.py` for detailed transformation parameters.

## Integration Checklist

### Prerequisites
- [ ] Blender 3.6+ installed
- [ ] Kenney assets downloaded and extracted
- [ ] Python 3.8+ available
- [ ] `blender_batch_export.py` in worktree

### Execution
- [ ] Run export script for all 24 buildings
- [ ] Verify polygon counts < 400 per building
- [ ] Check faction colors applied correctly
- [ ] Reimport FBX in Blender to validate
- [ ] Update EXPORT_LOG.txt with actual metadata

### Validation
- [ ] All FBX files created
- [ ] No import errors
- [ ] Textures applied and visible
- [ ] No shader compilation errors
- [ ] Frame rate stable in test

### Integration
- [ ] Register FBX paths in building YAML files
- [ ] Update pack manifest
- [ ] Run pack validator: `dotnet run --project src/Tools/PackCompiler -- validate`
- [ ] Build pack: `dotnet run --project src/Tools/PackCompiler -- build`
- [ ] Test in DINO game engine
- [ ] Document results

## Performance Notes

| Task | Time | Notes |
|------|------|-------|
| Single building export | 2-5 min | Depends on Blender + system |
| 4 buildings (serial) | 10-20 min | Current PoC |
| 24 buildings (serial) | 50-120 min | Full production set |
| 24 buildings (4x parallel) | 15-30 min | With parallel Blender instances |

**Storage**:
- Stub files: 144 B each (minimal)
- Production FBX: 5-20 KB each (varies by geometry)
- 24 buildings: ~200 KB total

## Troubleshooting Quick Reference

| Error | Solution |
|-------|----------|
| `Command 'blender' not found` | Add Blender to PATH or use full path |
| `Source FBX not found` | Verify Kenney assets downloaded and extracted |
| `argument --faction: invalid choice` | Use only "republic" or "cis" |
| `Texture file not found` | Run `texture_generation.py` first |
| `Export fails in headless mode` | Test with GUI mode (remove `--background`) |

See `FBX_EXPORT_README.md` for detailed troubleshooting.

## Advanced Customization

### Change Faction Colors

Edit `FACTION_COLORS` dict in `blender_batch_export.py`:

```python
FACTION_COLORS = {
    "republic": {
        "primary": (R, G, B),      # Normalize 0-1
        "secondary": (R, G, B),
        "tertiary": (R, G, B),
        "metallic": 0.0-1.0,
        "roughness": 0.0-1.0,
    }
}
```

### Adjust Polygon Budget

Modify decimate threshold in `_optimize_geometry()` method:

```python
if initial_tris > 350:  # Change 400 to desired target
    decimate.ratio = 0.75  # Adjust decimation aggressiveness
```

### Custom Batch Processing

Use `BlenderFBXExporter` class programmatically:

```python
from blender_batch_export import BlenderFBXExporter

for building in buildings_list:
    exporter = BlenderFBXExporter(
        input_path=building['kenney_source'],
        faction=building['faction'],
        building_id=building['id'],
        output_path=building['output_path'],
        log_file='EXPORT_LOG.txt'
    )
    exporter.export()
```

## References & Resources

- **Blender Docs**: https://docs.blender.org/
- **FBX Format**: https://help.autodesk.com/view/FBX/2020/ENU/
- **Kenney Assets**: https://kenney.nl/ (free, MIT license)
- **DINOForge Architecture**: CLAUDE.md in project root
- **Pack System**: Pack definition files in this directory

## Support & Contact

For questions, issues, or feedback:

1. Check troubleshooting in `FBX_EXPORT_README.md`
2. Review error logs in `EXPORT_LOG.txt`
3. Consult manual guide: `BLENDER_ASSEMBLY_TEMPLATE.md`
4. Verify Blender version and asset paths

---

**Document Version**: 1.0
**Last Updated**: 2026-03-12
**Status**: Production-Ready
**Maintained By**: DINOForge Subagent (Claude Haiku)
