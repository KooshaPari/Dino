# Asset Registry System

This directory contains the asset tracking and validation layer for vanilla DINO building reskins in the warfare-starwars pack.

## Files

### `asset_index.json`
**Purpose**: Master index tracking all 24 vanilla buildings with faction reskin mappings

**Structure**:
```json
{
  "buildings": [
    {
      "vanilla_id": 1,
      "vanilla_name": "Command Center",
      "republic_id": "rep_command_center",
      "cis_id": "cis_tactical_center",
      "status": "complete|in_progress|pending",
      "texture_status": "complete|in_progress|not_started",
      "texture_file_republic": "rep_command_center_albedo.png",
      "texture_file_cis": "cis_tactical_center_albedo.png",
      "kenney_source": "kenney_structure_c",
      "license": "MIT (Kenney.nl)"
    }
  ],
  "summary": {
    "total_complete": 10,
    "completion_percentage": 41.67
  }
}
```

**Key Fields**:
- `vanilla_id`: Numeric identifier (1-24) for vanilla DINO building
- `status`: Overall completion state (complete, in_progress, pending)
- `texture_status`: State of texture assets (complete, in_progress, not_started)
- `manifest_status`: State of YAML definition (complete, pending)
- `kenney_source`: Source FBX file from Kenney.nl packs

**Usage**:
- Query to find buildings needing texture work
- Track progress metrics across the pack
- Cross-reference with manifest files
- Identify missing assets by faction or type

---

### `VANILLA_BUILDINGS.json`
**Purpose**: Structured metadata for scripting and batch processing

**Structure**:
```json
{
  "buildings": [
    {
      "vanilla_type": "command",
      "vanilla_index": 1,
      "republic_id": "rep_command_center",
      "republic_name": "Republic Command Center",
      "cis_id": "cis_tactical_center",
      "cis_name": "Tactical Droid Center",
      "kenney_source": "kenney_structure_c.fbx",
      "kenney_pack": "structure",
      "category": "Strategic",
      "effort_estimate": "Low|Medium|High",
      "status": "complete|in_progress|pending"
    }
  ],
  "texture_palettes": {
    "republic_white_blue": { ... },
    "cis_grey_orange": { ... }
  }
}
```

**Key Fields**:
- `vanilla_type`: Building category (command, barracks, defense, economy, etc.)
- `category`: Gameplay role classification
- `effort_estimate`: Estimated work hours/complexity
- `kenney_pack`: Which Kenney.nl pack the model comes from
- `texture_palette_*`: Color palette keys for texture generation

**Usage**:
- Script texture generation for multiple buildings
- Plan batch processing tasks
- Estimate remaining work by effort level
- Generate reports by building type or category
- Cross-reference with texture generation pipeline

---

### `provenance_index.json`
**Purpose**: Complete attribution and licensing information

**Structure**:
```json
{
  "primary_sources": [
    {
      "source_name": "Kenney.nl Asset Packs",
      "license": "CC0 1.0 Universal (Public Domain)",
      "packs_used": [
        {
          "pack_name": "3D Structure Pack",
          "models": ["kenney_structure_a.fbx", ...],
          "notes": "Primary building models"
        }
      ]
    }
  ],
  "derivative_works": [
    {
      "work_type": "Texture Modifications",
      "description": "HSV-based color transformation",
      "palettes": [...]
    }
  ],
  "contributors": [...]
}
```

**Key Sections**:
- `primary_sources`: Original Kenney.nl assets with links to download pages
- `derivative_works`: Transformations (textures, naming, lore)
- `contributors`: Attribution to Kenney and DINOForge team
- `compliance`: License preservation and CC0 compliance status
- `buildings_by_kenney_pack`: Reverse index mapping packs to buildings

**Usage**:
- Generate attribution statements for pack distribution
- Ensure CC0 compliance during redistribution
- Track which Kenney packs are in use
- Document derivative work transformations
- Reference original sources when requesting updates/improvements

---

## Validation

### `validate_vanilla_assets.py`

**Purpose**: Automated validation of asset completeness and correctness

**Checks**:
1. ✓ All expected texture files exist (albedo + normal maps)
2. ✓ Manifest references match actual building definitions
3. ✓ Kenney source references are documented
4. ✓ License and attribution information is complete

**Usage**:
```bash
# Standard run
python validate_vanilla_assets.py

# Verbose output (show all found files)
python validate_vanilla_assets.py --verbose

# Custom pack root (if run from different directory)
python validate_vanilla_assets.py --pack-root /path/to/warfare-starwars
```

**Output**:
```
[CHECK 1] Validating texture files...
  Texture check: 20/20 found
[CHECK 2] Validating manifest references...
  Found 2 building file(s) in manifest
[CHECK 3] Validating Kenney source references...
  Found 14 unique Kenney source(s)

VALIDATION REPORT
Asset Index Statistics:
  Total buildings: 24
  Complete: 10 (41.7%)
  In Progress: 2
  Pending: 12

[EXIT CODE] 0 = success, 1 = failure
```

---

## Coverage Status

See `../VANILLA_BUILDING_COVERAGE.md` for detailed per-building completion status.

**Current Progress**:
- **Complete**: 10/24 buildings (41.7%)
  - All command, barracks, core defense buildings
  - All economic production buildings
  - Research and basic wall structures

- **In Progress**: 2/24 buildings (8.3%)
  - Residential/support structures (meshes compiling)

- **Pending**: 12/24 buildings (50.0%)
  - Advanced structures (harbor, temple, power plant)
  - Additional defensive structures
  - Specialized utility buildings

---

## Building Definitions Reference

### Republic Faction
All building IDs follow the prefix `rep_` (e.g., `rep_command_center`)

Naming convention: `rep_<building_function>` (e.g., `rep_clone_facility`, `rep_tibanna_refinery`)

### CIS Faction
All building IDs follow the prefix `cis_` (e.g., `cis_tactical_center`)

Naming convention: `cis_<building_variant>` (e.g., `cis_droid_factory`, `cis_processing_plant`)

---

## Texture File Naming

### Albedo (Diffuse) Maps
- **Format**: `<faction>_<building_name>_albedo.png`
- **Example**: `rep_command_center_albedo.png`, `cis_tactical_center_albedo.png`
- **Location**: `../textures/buildings/`

### Normal Maps
- **Format**: `<faction>_<building_name>_normal.png`
- **Example**: `rep_command_center_normal.png`, `cis_tactical_center_normal.png`
- **Location**: `../textures/buildings/`

---

## License Summary

### Original Assets
- **Source**: Kenney.nl (https://kenney.nl)
- **License**: CC0 1.0 Universal (Public Domain Dedication)
- **Attribution**: Not required, but appreciated
- **Commercial Use**: Yes
- **Derivatives**: Yes

### Derived Works
- **Type**: Faction-specific texture transformations
- **Method**: HSV-based color space transformations
- **License**: CC0 1.0 Universal (inherited from source)
- **Attribution**: "Derived from Kenney.nl assets by DINOForge team"

See `provenance_index.json` for complete license details and links.

---

## Integration Points

### Pack Manifest
Building definitions are loaded via `manifest.yaml`:
```yaml
loads:
  buildings:
    - buildings/republic_buildings
    - buildings/cis_buildings
```

### Texture Manifest
All generated textures are documented in:
- `../textures/buildings/TEXTURE_MANIFEST.json`

### Asset Loader
The SDK ContentLoader reads definitions from:
- `buildings/republic_buildings.yaml`
- `buildings/cis_buildings.yaml`

---

## Workflow Example: Adding a New Building

1. **Add entry to `asset_index.json`**
   ```json
   {
     "vanilla_id": 25,
     "vanilla_name": "New Building",
     "republic_id": "rep_new_building",
     "cis_id": "cis_new_building",
     "status": "in_progress",
     "texture_status": "in_progress",
     "kenney_source": "kenney_new_pack"
   }
   ```

2. **Add entry to `VANILLA_BUILDINGS.json`**
   ```json
   {
     "vanilla_type": "economy",
     "vanilla_index": 25,
     "republic_name": "New Republic Building",
     "cis_name": "New CIS Building",
     "kenney_source": "kenney_new_pack.fbx"
   }
   ```

3. **Generate textures** (see `../texture_generation.py`)
   ```bash
   python ../texture_generation.py --building-id 25
   ```

4. **Define building stats** in YAML
   - Add to `../buildings/republic_buildings.yaml`
   - Add to `../buildings/cis_buildings.yaml`

5. **Update manifest** in `../VANILLA_BUILDING_COVERAGE.md`
   - Mark tasks as complete
   - Update summary statistics

6. **Validate** before commit
   ```bash
   python validate_vanilla_assets.py --verbose
   ```

---

## Scripts & Tools

### Texture Generation
- **Script**: `../texture_generation.py`
- **Purpose**: HSV-based color transformation of Kenney base textures
- **Input**: Base Kenney textures + faction color palettes
- **Output**: Faction-specific PNG textures with metadata

### Asset Validation
- **Script**: `./validate_vanilla_assets.py`
- **Purpose**: Automated completeness and correctness checks
- **Input**: asset_index.json + file system
- **Output**: Validation report with missing assets

### FBX Export (Batch)
- **Script**: `../blender_batch_export.py`
- **Purpose**: Export multiple FBX files from Blender in batch
- **Requirements**: Blender 3.0+ command-line access

---

## Maintenance

### Weekly
- Run validation script to catch missing files
- Update status flags in asset_index.json as builds complete

### Monthly
- Update VANILLA_BUILDING_COVERAGE.md with progress
- Review pending work and update effort estimates
- Cross-reference with pack manifest

### Per Release
- Validate all assets before packaging
- Update provenance_index.json if sources change
- Ensure all building definitions are tested

---

## Related Documentation

- **Pack Manifest**: `../pack.yaml`
- **Building Definitions**: `../buildings/republic_buildings.yaml`, `../buildings/cis_buildings.yaml`
- **Texture Pipeline**: `../texture_generation.py` and TEXTURE_MANIFEST.json
- **Coverage Report**: `../VANILLA_BUILDING_COVERAGE.md`
- **Build Checklist**: `../assets/BUILD_CHECKLIST_ENHANCED.md`

---

## Questions & Troubleshooting

**Q: How do I add a new building?**
A: Follow the "Workflow Example" section above.

**Q: What if validation fails?**
A: Check the error message, verify texture files exist, ensure JSON is valid, and rerun with `--verbose`.

**Q: Where do I get the Kenney source FBX files?**
A: Download from https://kenney.nl - direct links in `provenance_index.json`

**Q: Can I redistribute this pack commercially?**
A: Yes - all assets are CC0. Attribution appreciated but not required.

**Q: How do I regenerate faction textures?**
A: Run `python ../texture_generation.py` with appropriate flags.

---

**Last Updated**: 2026-03-12
**Pack Version**: 0.1.0
**Status**: 10/24 buildings complete (41.7%)
