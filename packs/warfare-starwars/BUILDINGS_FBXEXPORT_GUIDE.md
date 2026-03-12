# DINOForge Star Wars Buildings - FBX Export Guide

**Status**: Generated (44 stub files created), Ready for Blender Production Export

**Date**: 2026-03-12
**Target**: 24 total buildings (12 archetypes × 2 factions)
**Current**: 44 stub files (development placeholders)
**Next**: Blender batch export for production-quality assets

---

## Overview

This guide covers the generation and export of all 24 building FBX meshes for the DINOForge Star Wars content pack, derived from Kenney asset libraries (sci-fi-rts, space-kit, modular-space-kit).

**Specifications**:
- Triangle budget: <400 tris per building
- Faction variants: Republic (white/blue) + CIS (grey/orange)
- Export format: FBX 7.4 with faction-specific materials
- Output directory: `packs/warfare-starwars/assets/meshes/buildings/`

---

## Building Inventory (24 Total)

### 1. Residential (House)
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| house_clone_quarters | Clone Quarters Pod | ✓ | ✓ | 320 | structure_c |
| house_droid_pod | Droid Pod | ✓ | ✓ | 320 | structure_c |

### 2. Economy (Farms/Harvesting)
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| farm_hydroponic | Hydroponic Farm | ✓ | ✓ | 280 | structure_e |
| farm_fuel_harvester | Fuel Harvester | ✓ | ✓ | 280 | structure_e |
| farm_moisture_farm | Moisture Farm | ✓ | ✓ | 280 | structure_e |
| farm_moisture_extractor | Moisture Extractor | ✓ | ✓ | 280 | structure_e |

### 3. Resources - Stone/Mining
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| stone_crystal_mine | Crystal Mine | ✓ | ✓ | 320 | structure_f |
| stone_mineral_processor | Mineral Processor | ✓ | ✓ | 320 | structure_f |

### 4. Resources - Iron/Forging
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| iron_forge_station | Forge Station | ✓ | ✓ | 350 | structure_g |
| iron_metal_foundry | Metal Foundry | ✓ | ✓ | 350 | structure_g |

### 5. Resources - Storage/Granary
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| granary_biodome | Biodome Storage | ✓ | ✓ | 300 | structure_d |
| granary_ore_vault | Ore Vault | ✓ | ✓ | 300 | structure_d |

### 6. Special Resources - Soul/Mystical
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| soul_meditation_chamber | Meditation Chamber | ✓ | ✓ | 290 | structure_b |
| soul_sith_altar | Sith Altar | ✓ | ✓ | 290 | structure_b |

### 7. Military/Support - Builder/Construction
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| builder_engineering_station | Engineering Station | ✓ | ✓ | 380 | structure_h |
| builder_construction_bot_factory | Construction Bot Factory | ✓ | ✓ | 380 | structure_h |

### 8. Military/Support - Guild/Trading
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| guild_merchant_bazaar | Merchant Bazaar | ✓ | ✓ | 310 | structure_a |
| guild_trade_hub | Trade Hub | ✓ | ✓ | 310 | structure_a |

### 9. Defense - Gates
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| gate_shield_generator | Shield Generator Gate | ✓ | ✓ | 340 | structure_c |
| gate_repulse_barrier | Repulse Barrier Gate | ✓ | ✓ | 340 | structure_c |

### 10. Medical/Support - Hospital
| Building ID | Display Name | Republic | CIS | Tris | Source |
|-------------|--------------|----------|-----|------|--------|
| hospital_medical_bay | Medical Bay | ✓ | ✓ | 260 | structure_d |
| hospital_clone_bank | Clone Bank | ✓ | ✓ | 260 | structure_d |

---

## Faction Color Schemes

### Republic (white/blue)
```
Primary:    #F5F5F5 (pristine white)
Secondary:  #1A3A6B (deep blue)
Tertiary:   #64A0DC (accent blue)
Metallic:   0.1
Roughness:  0.8
```

### CIS (grey/orange)
```
Primary:    #444444 (dark grey)
Secondary:  #B35A00 (rust orange)
Tertiary:   #663300 (dark brown)
Metallic:   0.2
Roughness:  0.7
```

---

## File Structure

```
packs/warfare-starwars/
  assets/meshes/buildings/
    rep_house_clone_quarters.fbx
    cis_house_clone_quarters.fbx
    rep_farm_hydroponic.fbx
    cis_farm_hydroponic.fbx
    ... (44 total files)

  build_all_buildings.py          # Batch export orchestrator
  generate_all_fbx_stubs.py       # Stub generator for dev/testing
  blender_batch_export.py         # Blender Python module (Kenney → FBX)
  BUILDINGS_FBXEXPORT_GUIDE.md    # This file
  EXPORT_LOG.txt                  # Export metadata (appended)
  EXPORT_RESULTS.json             # Batch execution results
```

---

## Naming Convention

All FBX files follow this naming scheme:

```
[faction_prefix]_[building_id].fbx
```

Where:
- `faction_prefix` = `rep` (republic) or `cis`
- `building_id` = canonical building identifier (e.g., `house_clone_quarters`)

Examples:
- `rep_house_clone_quarters.fbx`
- `cis_farm_moisture_extractor.fbx`
- `rep_stone_crystal_mine.fbx`

---

## Current Status: Stub Files

All 44 files have been generated as **development placeholders**. These stubs:
- ✓ Have valid FBX binary structure (header + metadata)
- ✓ Include building_id, faction, and tri-count metadata
- ✓ Are suitable for testing ContentLoader, pack validation, and UI integration
- ✗ Do **NOT** contain actual 3D geometry
- ✗ Should **NOT** be used in production

---

## Production Export (Blender)

To generate production-quality FBX meshes with real geometry:

### Prerequisites
```bash
# Install Blender (any recent version)
# Download Kenney assets:
#   - sci-fi-rts (structures a-h)
#   - space-kit (optional)
#   - modular-space-kit (optional)
```

### Single Building Export

```bash
blender --background --python blender_batch_export.py -- \
  --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \
  --faction republic \
  --building-id house_clone_quarters \
  --output assets/meshes/buildings/rep_house_clone_quarters.fbx
```

### Batch Export (All 24 Buildings)

```bash
# Validate configuration (dry-run)
python build_all_buildings.py --dry-run

# Run 4 parallel Blender exports
python build_all_buildings.py --parallel 4

# Run with all CPU cores
python build_all_buildings.py --parallel $(nproc)
```

### Options
- `--dry-run` - Validate config without running Blender
- `--parallel N` - Number of parallel workers (default: cpu_count - 1)
- `--blender-path PATH` - Custom Blender executable path (default: `blender` in PATH)

### Output

After batch export completes:
- **EXPORT_LOG.txt** - Appended with per-file metadata (import time, material applied, optimization)
- **EXPORT_RESULTS.json** - Summary with success/failed/timeout counts
- **assets/meshes/buildings/*.fbx** - Production FBX files

Example EXPORT_RESULTS.json:
```json
{
  "timestamp": "2026-03-12T14:05:00.123456",
  "summary": {
    "total": 44,
    "successful": 44,
    "failed": 0,
    "timeout": 0,
    "errors": 0,
    "dry_run": 0
  },
  "results": [
    {
      "building_id": "house_clone_quarters",
      "faction": "republic",
      "status": "success",
      "output": "assets/meshes/buildings/rep_house_clone_quarters.fbx"
    },
    ...
  ]
}
```

---

## Pipeline: Blender Script Flow

Each FBX export runs through this pipeline:

1. **Import** (FBX from Kenney source)
   - Preserve axis orientation (-Y forward, Z up)
   - Apply bone orientation automatically

2. **Material Application**
   - Create Principled BSDF shader
   - Apply faction-specific colors (primary, secondary, metallic, roughness)
   - Assign to all mesh slots

3. **Geometry Optimization**
   - Count initial triangles
   - If > 400 tris: apply Decimate modifier (0.8 ratio)
   - Apply modifier and commit to mesh

4. **Pivot Centering**
   - Set object origin to geometry center
   - Ensures consistent placement in-game

5. **Export to FBX**
   - Use Blender FBX exporter (7.4 format)
   - Bake transformations
   - Include modifiers in export
   - Mesh smooth type: FACE

6. **Metadata Logging**
   - Write export metadata JSON to EXPORT_LOG.txt
   - Record poly count, material applied, optimization method

---

## Kenney Source File Mapping

| Kenney File | DINOForge Buildings | Estimated Tris |
|-------------|---------------------|----------------|
| structure-a | guild_{merchant_bazaar, trade_hub} | 310 |
| structure-b | soul_{meditation_chamber, sith_altar} | 290 |
| structure-c | house_{clone_quarters, droid_pod}, gate_{shield_generator, repulse_barrier} | 320-340 |
| structure-d | granary_{biodome, ore_vault}, hospital_{medical_bay, clone_bank} | 260-300 |
| structure-e | farm_{hydroponic, fuel_harvester, moisture_farm, moisture_extractor} | 280 |
| structure-f | stone_{crystal_mine, mineral_processor} | 320 |
| structure-g | iron_{forge_station, metal_foundry} | 350 |
| structure-h | builder_{engineering_station, construction_bot_factory} | 380 |

---

## Integration with ContentLoader

Once FBX files are generated, the ContentLoader will:

1. **Detect** FBX files in `assets/meshes/buildings/`
2. **Parse** faction/building_id from filename
3. **Load** via Unity's ModelImporter (async)
4. **Register** in BuildingRegistry with metadata:
   ```csharp
   registry.Register(new BuildingModel {
       Id = "house_clone_quarters",
       Faction = "republic",
       DisplayName = "Clone Quarters Pod",
       MeshPath = "buildings/rep_house_clone_quarters.fbx",
       PolyCount = 320,
       Source = "kenney/sci-fi-rts/structure-c.fbx"
   });
   ```

---

## Validation Checklist

Before committing FBX files to repository:

- [ ] All 44 files present in `assets/meshes/buildings/`
- [ ] All files named correctly (`rep_`/`cis_` prefix)
- [ ] File sizes reasonable (5-50KB for production FBX)
- [ ] Blender export log contains no errors
- [ ] Tri counts < 400 for all buildings
- [ ] Faction colors visually distinct in Blender viewport
- [ ] Pivot points centered at base
- [ ] Mesh modifiers applied (decimation, etc.)
- [ ] Pack validation passes (dotnet run ... -- validate packs/)

---

## Troubleshooting

### Blender Export Timeout
- Increase timeout in `build_all_buildings.py` (currently 300s)
- Reduce `--parallel` count to run fewer simultaneously
- Check system disk space (Blender may need 2-4GB temp space)

### Missing Kenney Files
- Ensure Kenney assets are in `source/kenney/sci-fi-rts/Models/FBX/`
- Download from kenney.nl if missing
- Verify file names match `structure-a.fbx` through `structure-h.fbx`

### Material Not Applied
- Check Blender version (>= 2.9 recommended)
- Verify faction color palette in `blender_batch_export.py`
- Ensure Principled BSDF shader is available

### High Poly Count After Decimation
- Increase `decimate.ratio` from 0.8 to 0.6 (more aggressive)
- Use separate decimate pass (2-3 iterations)
- Manually simplify in Blender before export

---

## Scripts Reference

### `generate_all_fbx_stubs.py`
Generates minimal placeholder FBX files for all 24 buildings.

```bash
python generate_all_fbx_stubs.py
```

**Output**: 44 stub .fbx files (binary FBX headers + metadata)
**Purpose**: Development/testing; ContentLoader validation

### `build_all_buildings.py`
Orchestrates parallel batch export to real FBX via Blender.

```bash
python build_all_buildings.py --dry-run              # Validate only
python build_all_buildings.py --parallel 4           # Export with 4 workers
python build_all_buildings.py --blender-path /path   # Custom Blender path
```

**Features**:
- Multiprocessing pool (configurable worker count)
- Timeout per export (300s default)
- JSON result summary
- Detailed logging to BUILD_ALL_BUILDINGS.log

### `blender_batch_export.py`
Blender Python module (runs inside Blender via --python).

```bash
blender --background --python blender_batch_export.py -- \
  --input <kenney-fbx> --faction <republic|cis> \
  --building-id <id> --output <output-fbx>
```

**Pipeline**:
1. Import FBX from Kenney source
2. Apply faction-specific material
3. Optimize geometry (decimate if > 400 tris)
4. Center pivot
5. Export to target FBX
6. Log metadata

---

## Next Steps

1. **Install Blender** (if not already installed)
   ```bash
   # Ubuntu/Debian
   sudo apt install blender

   # macOS (via Homebrew)
   brew install blender

   # Windows (download from blender.org or winget)
   winget install Blender.Blender
   ```

2. **Download Kenney Assets**
   - sci-fi-rts: https://kenney.nl/assets/sci-fi-rts
   - Extract to `source/kenney/sci-fi-rts/`

3. **Test Dry-Run**
   ```bash
   python build_all_buildings.py --dry-run
   ```

4. **Run Batch Export**
   ```bash
   python build_all_buildings.py --parallel 4
   ```

5. **Validate Results**
   ```bash
   # Check output files
   ls -la assets/meshes/buildings/*.fbx

   # View summary
   cat EXPORT_RESULTS.json | jq '.summary'

   # Run pack validation
   dotnet run --project ../../Tools/PackCompiler -- validate packs/warfare-starwars
   ```

6. **Commit to Repository**
   ```bash
   git add assets/meshes/buildings/*.fbx
   git commit -m "feat: add production building FBX meshes (all 24 buildings)"
   ```

---

## References

- **Kenney Assets**: https://kenney.nl/
- **Blender FBX Export**: https://docs.blender.org/manual/en/latest/addons/import_export/scene_fbx.html
- **FBX Format Spec**: https://help.autodesk.com/view/FBX/2020/ENU/
- **DINOForge ContentLoader**: `src/SDK/ContentLoader/`
- **Registry System**: `src/SDK/Registry/`

---

**Author**: DINOForge Agent
**Last Updated**: 2026-03-12
**Status**: Ready for Blender Production Export
