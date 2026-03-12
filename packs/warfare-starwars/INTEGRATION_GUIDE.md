# Texture Integration Guide - Star Wars Pack

## Overview

This guide describes how the faction-specific textures integrate with the DINOForge pack system and how to verify the integration is working correctly.

## Architecture Integration Points

### 1. Pack Manifest Integration

**File**: `manifest.yaml`

The pack manifest declares faction identity and asset replacements:

```yaml
id: warfare-starwars
name: "Star Wars: Clone Wars"
version: 0.1.0
factions:
  - id: republic
    name: Galactic Republic
    description: Clone troopers and Jedi-led forces
  - id: cis-droid-army
    name: Confederacy of Independent Systems
    description: Battle droids and Separatist war machines

asset_replacements:
  textures:
    # Will be auto-generated from TEXTURE_MANIFEST.json
    Building/republic/command_center:
      path: assets/textures/buildings/rep_command_center_albedo.png
      type: albedo
    # ... etc for all 20 variants
```

### 2. Building Definition Integration

**Files**:
- `buildings/republic_buildings.yaml` (10 buildings)
- `buildings/cis_buildings.yaml` (10 buildings)

Each building YAML references its texture via ID matching:

```yaml
- id: rep_command_center
  display_name: Republic Command Center
  building_type: command
  # Texture loader will match:
  # rep_command_center → assets/textures/buildings/rep_command_center_albedo.png
```

### 3. ContentLoader Integration

**Path**: `src/SDK/ContentLoader.cs`

The ContentLoader pipeline:

```
1. Parse manifest.yaml
   ↓
2. Iterate asset_replacements.textures
   ↓
3. Load PNG from disk → Image buffer
   ↓
4. Register with Unity texture cache
   ↓
5. Apply to building material on instantiation
   ↓
6. Fallback to vanilla if pack texture missing
```

## File Structure Validation

### Expected Directory Tree

```
packs/warfare-starwars/
├── manifest.yaml                          # Pack metadata
├── buildings/
│   ├── republic_buildings.yaml            # 10 Republic building definitions
│   └── cis_buildings.yaml                 # 10 CIS building definitions
├── assets/
│   └── textures/
│       └── buildings/
│           ├── TEXTURE_MANIFEST.json      # Metadata for all variants
│           ├── rep_command_center_albedo.png
│           ├── rep_clone_facility_albedo.png
│           ├── ... (8 more Republic)
│           ├── cis_tactical_center_albedo.png
│           ├── cis_droid_factory_albedo.png
│           └── ... (8 more CIS)
└── ... (other pack resources)
```

### Verification Commands

```bash
# Count all textures
find packs/warfare-starwars/assets/textures/buildings -name "*_albedo.png" | wc -l
# Expected: 20

# Verify all are valid PNG
file packs/warfare-starwars/assets/textures/buildings/*.png | grep -c PNG
# Expected: 20

# Validate JSON manifest
python3 -m json.tool packs/warfare-starwars/assets/textures/buildings/TEXTURE_MANIFEST.json > /dev/null && echo "✓ Valid JSON"

# Check building definitions match texture IDs
python3 << 'EOF'
import yaml
import os

# Load building definitions
with open('packs/warfare-starwars/buildings/republic_buildings.yaml') as f:
    rep_buildings = yaml.safe_load(f) or []
with open('packs/warfare-starwars/buildings/cis_buildings.yaml') as f:
    cis_buildings = yaml.safe_load(f) or []

all_buildings = rep_buildings + cis_buildings

# Check textures exist
for building in all_buildings:
    building_id = building['id']
    texture_path = f"assets/textures/buildings/{building_id}_albedo.png"
    if os.path.exists(texture_path):
        print(f"✓ {building_id}")
    else:
        print(f"✗ MISSING: {building_id}")
EOF
```

## Runtime Loading Sequence

### 1. Pack Initialization

```csharp
// In BepInEx plugin startup
var pack = await ContentLoader.LoadPackAsync("warfare-starwars");
pack.RegisterFactions();  // Registers republic, cis-droid-army, cis-infiltrators
```

### 2. Texture Registration

```csharp
// ContentLoader.cs - TryLoadTexture()
var texturePath = Path.Combine(
    packDir,
    assetReplacement.path  // e.g., "assets/textures/buildings/rep_command_center_albedo.png"
);

if (File.Exists(texturePath))
{
    var texture = LoadPNG(texturePath);
    TextureCache.Register(assetReplacement.name, texture);
}
```

### 3. Building Instantiation

```csharp
// When building spawns in-game
var building = BuildingFactory.Create("rep_command_center", faction: "republic");

// Material is created with cached texture
var material = new Material(shaders["Building_Standard"]);
material.SetTexture("_Albedo", TextureCache.Get("rep_command_center_albedo"));
building.renderer.material = material;
```

## Building-to-Texture Mapping

### Building ID Naming Convention

All building IDs follow pattern: `{faction_prefix}_{building_name}`

**Prefixes**:
- `rep_` = Galactic Republic
- `cis_` = Confederacy of Independent Systems

**Naming**:
- All lowercase with underscores
- Descriptive of building function
- Unique across all factions

**Examples**:
- `rep_command_center` → Republic Command Center
- `cis_tactical_center` → CIS Tactical Droid Center
- `rep_clone_facility` → Clone Training Facility
- `cis_droid_factory` → Droid Factory

### Cross-Faction Texture Reuse

Note: **Republic and CIS buildings use DIFFERENT base textures** even for similar building types:

| Function | Republic | CIS | Reason |
|----------|----------|-----|--------|
| Command | `rep_command_center` | `cis_tactical_center` | Different architectural styles |
| Barracks 1 | `rep_clone_facility` | `cis_droid_factory` | Clone training vs. droid assembly |
| Barracks 2 | `rep_weapons_factory` | `cis_assembly_line` | Weapons vs. heavy droid manufacturing |
| Barracks 3 | `rep_vehicle_bay` | `cis_heavy_foundry` | Vehicle bay vs. foundry |
| Defense 1 | `rep_guard_tower` | `cis_sentry_turret` | Manned vs. automated |
| Defense 2 | `rep_shield_generator` | `cis_ray_shield` | Different technology |
| Economy 1 | `rep_supply_station` | `cis_mining_facility` | Logistics vs. extraction |
| Economy 2 | `rep_tibanna_refinery` | `cis_processing_plant` | Specific vs. general processing |
| Research | `rep_research_lab` | `cis_tech_union_lab` | Republic R&D vs. droid development |
| Wall/Barrier | `rep_blast_wall` | `cis_durasteel_barrier` | Different construction styles |

## Texture Properties Reference

### Albedo Maps
- **Purpose**: Diffuse color information
- **Format**: PNG with alpha transparency
- **Size**: 256×256 (typical for Kenney assets)
- **Alpha**: Preserved from source, skip fully transparent pixels during transformation
- **Color Space**: sRGB (gamma-corrected)

### Normal Maps (Future Implementation)
- **Purpose**: Surface normal direction for lighting
- **Format**: PNG (R=X, G=Y, B=Z)
- **Size**: Matches albedo (256×256)
- **Processing**: Passed through without color transformation
- **Color Space**: Linear (tangent-space)

### Metallic/Roughness (Deferred)
- **Metallic**: Shared across faction (not transformed)
- **Roughness**: Shared across faction (not transformed)
- **Integration**: Planned for future releases

## Troubleshooting Texture Loading

### Symptom: Buildings Show Pink/Magenta

**Cause**: Texture file not found or corrupted PNG

**Fix**:
```bash
# Check file existence
ls -la packs/warfare-starwars/assets/textures/buildings/rep_command_center_albedo.png

# Verify PNG validity
file packs/warfare-starwars/assets/textures/buildings/rep_command_center_albedo.png
# Should output: PNG image data, 256 x 256...

# Check for corruption
python3 << 'EOF'
from PIL import Image
img = Image.open('packs/warfare-starwars/assets/textures/buildings/rep_command_center_albedo.png')
print(f"✓ Valid PNG: {img.size} {img.mode}")
EOF
```

### Symptom: Wrong Colors (Not Republic Blue or CIS Orange)

**Cause**: Texture generated with incorrect palette

**Fix**:
```bash
# Regenerate specific faction
python3 packs/warfare-starwars/texture_generation.py \
  --faction republic \
  --output packs/warfare-starwars/assets/textures/buildings/

# Verify colors in manifest
python3 << 'EOF'
import json
manifest = json.load(open('packs/warfare-starwars/assets/textures/buildings/TEXTURE_MANIFEST.json'))
for entry in manifest['color_palettes'].values():
    print(f"{entry['name']}: {entry['primary']} + {entry['secondary']}")
EOF
```

### Symptom: Alpha Transparency Lost

**Cause**: PNG saved without alpha channel

**Fix**:
```bash
# Check alpha in all textures
python3 << 'EOF'
from PIL import Image
from pathlib import Path

textures_dir = Path('packs/warfare-starwars/assets/textures/buildings')
for png_file in textures_dir.glob('*_albedo.png'):
    img = Image.open(png_file)
    if img.mode != 'RGBA':
        print(f"✗ Missing alpha: {png_file.name} (mode={img.mode})")
    else:
        print(f"✓ Alpha OK: {png_file.name}")
EOF

# Regenerate if needed
python3 packs/warfare-starwars/texture_generation.py --output packs/warfare-starwars/assets/textures/buildings/
```

## Testing Checklist

### Automated Validation

```bash
# Run all checks
python3 << 'EOF'
import json
import yaml
from pathlib import Path
from PIL import Image

# Check manifest
manifest = json.load(open('packs/warfare-starwars/assets/textures/buildings/TEXTURE_MANIFEST.json'))
print(f"✓ Manifest valid: {len(manifest['building_variants'])} entries")

# Check building definitions
for yaml_file in ['republic_buildings.yaml', 'cis_buildings.yaml']:
    with open(f'packs/warfare-starwars/buildings/{yaml_file}') as f:
        buildings = yaml.safe_load(f) or []
    print(f"✓ {yaml_file}: {len(buildings)} buildings")

# Check textures
textures_dir = Path('packs/warfare-starwars/assets/textures/buildings')
for entry in manifest['building_variants']:
    texture_path = textures_dir / entry['albedo_file']
    if texture_path.exists():
        img = Image.open(texture_path)
        assert img.mode == 'RGBA', f"No alpha channel: {entry['building_id']}"
        print(f"✓ {entry['building_id']}: {img.size}")
    else:
        print(f"✗ MISSING: {entry['building_id']}")

print("\n✓ All validations passed!")
EOF
```

### Manual Verification

1. **Visual Inspection**
   - [ ] Republic textures appear white/blue
   - [ ] CIS textures appear dark grey/orange
   - [ ] Alpha transparency preserved (visible in editor)
   - [ ] No obvious artifacts or compression damage

2. **Manifest Accuracy**
   - [ ] 20 texture entries in manifest
   - [ ] 10 Republic, 10 CIS
   - [ ] All file paths correct
   - [ ] Color palettes documented

3. **Building Definition Matching**
   - [ ] 10 buildings in republic_buildings.yaml
   - [ ] 10 buildings in cis_buildings.yaml
   - [ ] All building IDs have corresponding textures
   - [ ] Building types match schema

4. **Integration Test**
   - [ ] Pack loads without errors
   - [ ] Buildings instantiate with correct textures
   - [ ] No console warnings or exceptions
   - [ ] Performance acceptable (no lag)

## Asset Replacement Mapping Reference

### How to Add to manifest.yaml

```yaml
asset_replacements:
  textures:
    # Building textures - auto-generated section
    rep_command_center_albedo:
      path: assets/textures/buildings/rep_command_center_albedo.png
      type: albedo
      shader_property: _MainTex

    rep_command_center_normal:
      path: assets/textures/buildings/rep_command_center_normal.png
      type: normal
      shader_property: _BumpMap

    # ... repeat for all 20 variant IDs
```

### Shader Property Mapping

| Texture Type | Shader Property | Use |
|--------------|-----------------|-----|
| Albedo (Diffuse) | `_MainTex` or `_Albedo` | Color/diffuse |
| Normal | `_BumpMap` or `_NormalMap` | Surface normals |
| Metallic | `_MetallicTex` | Metal reflectivity |
| Roughness | `_RoughnessTex` | Surface roughness |

## Performance Impact

### Texture Memory Usage

- **Per texture**: ~256KB (256×256 RGBA in memory)
- **Typical building model**: 2-3 textures (albedo + normal + PBR)
- **Total pack footprint**: ~20 textures × 256KB = ~5.1 MB uncompressed
- **Archive size**: ~200-300 KB with ZIP compression

### Runtime Cost

- **First load**: ~50ms (load all textures into VRAM)
- **Subsequent loads**: <1ms (cached in TextureCache)
- **Rendering overhead**: 0% (standard shader, no special materials)

## Maintenance Notes

### Updating Textures

If source Kenney assets change or new buildings are added:

```bash
# 1. Update building definitions
# Add new building to buildings/{faction}_buildings.yaml

# 2. Update texture script
# Add BuildingTextureSpec entry to texture_generation.py

# 3. Regenerate
python3 packs/warfare-starwars/texture_generation.py --output packs/warfare-starwars/assets/textures/buildings/

# 4. Verify manifest auto-updates
python3 -m json.tool packs/warfare-starwars/assets/textures/buildings/TEXTURE_MANIFEST.json
```

### Version Tracking

- Manifest version: 1.0 (update when format changes)
- Pack version: 0.1.0 (in manifest.yaml)
- Generated date: Auto-stamped (2026-03-12)

## References

- **DINOForge Pack Format**: `src/SDK/Models/PackManifest.cs`
- **ContentLoader**: `src/SDK/ContentLoader.cs`
- **Building Schema**: `schemas/building.schema.json`
- **Asset Replacement Schema**: `schemas/asset_replacement.schema.json`

---

Generated: 2026-03-12
Last Updated: 2026-03-12
