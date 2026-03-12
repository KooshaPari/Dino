# FBX Export Workflow for DINOForge Star Wars Pack

This directory contains tools and documentation for generating FBX mesh files for all 24 vanilla DINO buildings with faction-specific variants (Republic and CIS).

## Quick Start

### Prerequisites

- **Blender 3.6+** (free, open-source)
- **Kenney 3D Assets** (free, MIT license)
- **Python 3.8+**

### Installation

1. **Install Blender**:
   - Download from https://www.blender.org/download/
   - Install version 3.6 or newer

2. **Download Kenney Assets**:
   - Visit https://kenney.nl/assets/space-kit
   - Download the "space-kit" asset pack (free, MIT license)
   - Extract to: `source/kenney/sci-fi-rts/`

3. **Verify Script**:
   ```bash
   cd packs/warfare-starwars
   python blender_batch_export.py --help
   ```

## Usage

### Single Building Export

Export one building at a time:

```bash
blender --background --python blender_batch_export.py -- \
  --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \
  --faction republic \
  --building-id house_clone_quarters \
  --output packs/warfare-starwars/assets/meshes/buildings/rep_house_clone_quarters.fbx \
  --log EXPORT_LOG.txt
```

### Batch Export Script (Recommended)

Create a batch script to export all 24 buildings:

```python
#!/usr/bin/env python3
import subprocess
import sys

buildings = [
    # Republic buildings
    ("structure-c.fbx", "republic", "house_clone_quarters", "rep_house_clone_quarters"),
    ("structure-e.fbx", "republic", "farm_hydroponic", "rep_farm_hydroponic"),
    ("structure-c.fbx", "republic", "command_center", "rep_command_center"),
    # ... add all 24 buildings
    # CIS buildings (same structure sources, different faction)
    ("structure-c.fbx", "cis", "house_droid_pod", "cis_house_droid_pod"),
    ("structure-e.fbx", "cis", "farm_fuel_harvester", "cis_farm_fuel_harvester"),
]

for kenney_file, faction, building_id, output_name in buildings:
    cmd = [
        "blender", "--background", "--python", "blender_batch_export.py", "--",
        "--input", f"source/kenney/sci-fi-rts/Models/FBX/{kenney_file}",
        "--faction", faction,
        "--building-id", building_id,
        "--output", f"packs/warfare-starwars/assets/meshes/buildings/{output_name}.fbx",
        "--log", "EXPORT_LOG.txt"
    ]
    subprocess.run(cmd, check=True)
    print(f"✓ Exported {output_name}.fbx")
```

### Dry Run (Validate Configuration)

Test the script without running Blender:

```bash
python blender_batch_export.py \
  --input source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx \
  --faction republic \
  --building-id house_clone_quarters \
  --output packs/warfare-starwars/assets/meshes/buildings/rep_house_clone_quarters.fbx \
  --dry-run
```

## Arguments Reference

```
--input PATH              (required) Path to source Kenney FBX file
--faction FACTION         (required) Target faction: "republic" or "cis"
--building-id ID          (required) DINOForge building ID (e.g., house_clone_quarters)
--output PATH             (required) Target FBX output path
--log PATH                (optional) Log file path (default: EXPORT_LOG.txt)
--dry-run                 (optional) Validate config without running Blender
```

## Export Settings

The script applies these settings automatically:

### FBX Export Configuration
- **Scale**: 1.0 (maintain Kenney scale)
- **Forward Axis**: -Y Forward
- **Up Axis**: Z Up
- **Include Mesh**: Yes
- **Include Materials**: Yes
- **Smoothing Groups**: Yes
- **Apply Scalings**: FBX Units

### Material/Shader Settings

**Republic Faction**:
- Primary Color: #F5F5F5 (Pristine White)
- Secondary Color: #1A3A6B (Deep Blue)
- Metallic: 0.1 (subtle reflectivity)
- Roughness: 0.8 (matte finish)

**CIS Faction**:
- Primary Color: #444444 (Dark Grey)
- Secondary Color: #B35A00 (Rust Orange)
- Metallic: 0.2 (slight industrial sheen)
- Roughness: 0.7 (more matte)

### Optimization
- **Target Polygon Count**: < 400 triangles per building
- **Method**: Automatic decimation if exceeded
- **Pivot Point**: Centered at base for proper ground placement

## Building Types & Sources

| Building | Faction | Kenney Source | Poly Target | Building Type |
|----------|---------|---------------|-------------|---------------|
| House/Pod | Rep/CIS | structure-c.fbx | 320 | Residential |
| Farm | Rep/CIS | structure-e.fbx | 280 | Economy |
| Command Center | Rep/CIS | structure-c.fbx | 400 | Command |
| Clone Facility | Rep/CIS | structure-b.fbx | 350 | Military |
| Guard Tower | Rep/CIS | tower-a.fbx | 250 | Defense |
| Shield Generator | Rep/CIS | structure-f.fbx | 380 | Defense |
| Supply Station | Rep/CIS | structure-a.fbx | 300 | Economy |
| Tibanna Refinery | Rep/CIS | structure-g.fbx | 360 | Economy |
| Research Lab | Rep/CIS | structure-h.fbx | 340 | Research |
| Blast Wall | Rep/CIS | wall-segment.fbx | 200 | Wall |

Plus CIS variants with specialized names:
- Droid Pod Storage, Fuel Harvester, Tactical Center, Droid Factory, etc.

## Output

The script generates:

### FBX Files
- Location: `packs/warfare-starwars/assets/meshes/buildings/`
- Naming: `[faction]_[building_id].fbx`
- Includes: Geometry, materials, faction colors, optimized UVs

### Export Log
- Appends metadata to `EXPORT_LOG.txt`
- Per-building information:
  - Timestamp
  - Input/output paths
  - Faction colors applied
  - Polygon count before/after optimization
  - Export status and any errors

### Example Log Entry
```json
{
  "timestamp": "2026-03-12T14:30:45.123456",
  "blender_version": "3.6.0",
  "input_file": "source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx",
  "faction": "republic",
  "building_id": "house_clone_quarters",
  "output_file": "packs/warfare-starwars/assets/meshes/buildings/rep_house_clone_quarters.fbx",
  "status": "success",
  "material_applied": {
    "material_name": "republic_house_clone_quarters_material",
    "base_color": [0.961, 0.961, 0.961],
    "metallic": 0.1,
    "roughness": 0.8
  },
  "optimization": {
    "method": "decimate",
    "initial_triangles": 450,
    "final_triangles": 320,
    "reduction_ratio": 0.71
  }
}
```

## Troubleshooting

### Blender Not Found
```
Error: Command 'blender' not found
```
**Solution**: Add Blender to PATH or use full path:
```bash
/opt/blender-3.6/blender --background --python blender_batch_export.py -- [args]
```

### Kenney Assets Not Found
```
Error: Source FBX not found: source/kenney/sci-fi-rts/Models/FBX/structure-c.fbx
```
**Solution**:
1. Download Kenney space-kit from https://kenney.nl/
2. Extract to `source/kenney/sci-fi-rts/`
3. Verify structure: `source/kenney/sci-fi-rts/Models/FBX/` should contain `*.fbx` files

### Texture File Missing
```
Error: Texture file not found: assets/textures/buildings/rep_house_clone_quarters_albedo.png
```
**Solution**:
- Textures are generated by `texture_generation.py`
- Run: `python packs/warfare-starwars/texture_generation.py`
- Or reference: `TEXTURE_MANIFEST.json` for texture assignments

### Export Fails in Headless Mode
If Blender crashes in `--background` mode:
1. Try with GUI first to debug: Remove `--background` flag
2. Check Blender version (3.6+ required)
3. Verify FBX import works manually
4. Check disk space for large FBX files

## Validation Checklist

After exporting, verify:

- [ ] FBX file exists at output path
- [ ] File size > 1 KB (not empty)
- [ ] Can reimport FBX in Blender
- [ ] Faction colors visible (white/blue or grey/orange)
- [ ] Polygon count logged and < 400
- [ ] No import errors in game engine
- [ ] Building renders correctly in-game
- [ ] Textures applied without seams
- [ ] Z-fighting (flickering) not present

## Integration with Pack System

Once FBX files are exported and validated:

1. **Register in Building Definitions**:
   ```yaml
   # packs/warfare-starwars/buildings/republic_buildings.yaml
   rep_house_clone_quarters:
     display_name: Clone Quarters Pod
     mesh: assets/meshes/buildings/rep_house_clone_quarters.fbx
     texture: assets/textures/buildings/rep_house_clone_quarters_albedo.png
     health: 100
     cost: 500
   ```

2. **Link in Pack Manifest**:
   ```yaml
   # packs/warfare-starwars/pack.yaml
   loads:
     buildings:
       - buildings/republic_buildings.yaml
       - buildings/cis_buildings.yaml
   ```

3. **Validate Pack**:
   ```bash
   dotnet run --project src/Tools/PackCompiler -- validate packs/warfare-starwars/
   ```

4. **Build Pack**:
   ```bash
   dotnet run --project src/Tools/PackCompiler -- build packs/warfare-starwars/
   ```

## Performance Notes

- **Export Time**: ~2-5 minutes per building (depends on Blender and system)
- **Total Time for 24 Buildings**: 1-2 hours (serial), 10-15 min (4-way parallel)
- **File Sizes**: 5-20 KB per FBX file (varies with geometry complexity)
- **Memory**: ~500 MB Blender overhead + per-building geometry

For faster processing with multiple buildings, use parallel Blender instances:
```bash
# Run 4 exports in parallel
for i in 0 1 2 3; do
  blender --background --python blender_batch_export.py -- [args_$i] &
done
wait
```

## Advanced Options

### Custom Texture Assignment

Modify faction colors by editing `FACTION_COLORS` in `blender_batch_export.py`:

```python
FACTION_COLORS = {
    "republic": {
        "primary": (0.961, 0.961, 0.961),  # White
        "secondary": (0.102, 0.227, 0.420),  # Blue
        "tertiary": (0.392, 0.627, 0.859),  # Accent
        "metallic": 0.1,
        "roughness": 0.8,
    }
}
```

### Custom Polygon Budget

Change decimate ratio by modifying optimization logic:

```python
# In _optimize_geometry() method
if initial_tris > 350:  # Change target from 400 to 350
    decimate.ratio = 0.75  # More aggressive decimation
```

### Batch Processing with Python

```python
from blender_batch_export import BlenderFBXExporter

buildings = [...]
for input_path, faction, building_id, output_path in buildings:
    exporter = BlenderFBXExporter(input_path, faction, building_id, output_path)
    exporter.export()
```

## References

- **Blender Docs**: https://docs.blender.org/
- **FBX Export Guide**: https://docs.blender.org/manual/en/latest/addons/import_export/scene_fbx/export.html
- **Kenney Assets**: https://kenney.nl/
- **DINOForge Pack System**: `CLAUDE.md` in project root
- **Building Assembly Guide**: `BLENDER_ASSEMBLY_TEMPLATE.md`

## Support

For issues or questions:
1. Check `EXPORT_LOG.txt` for error messages
2. Review `BLENDER_ASSEMBLY_TEMPLATE.md` for manual process
3. Verify Blender version: `blender --version`
4. Ensure Kenney assets are extracted correctly

---

**Status**: Production-ready workflow
**Last Updated**: 2026-03-12
**Maintained By**: DINOForge Agents
