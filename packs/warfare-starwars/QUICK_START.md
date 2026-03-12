# DINOForge Star Wars Buildings - Quick Start

**Status**: 44 FBX files generated (22 buildings × 2 factions)
**Location**: `packs/warfare-starwars/assets/meshes/buildings/`
**Next Step**: Blender production export or development testing

---

## Files Generated

✅ **44 FBX Stub Files** - Development placeholders ready for testing
✅ **2 Batch Scripts** - Automation for Blender export
✅ **4 Documentation Files** - Comprehensive guides

---

## Quick Commands

### Test Stub Files (No Blender Required)
```bash
# Just created! Ready to use.
ls -la packs/warfare-starwars/assets/meshes/buildings/*.fbx | head -5

# Validate with pack compiler
cd src
dotnet run --project Tools/PackCompiler -- validate ../packs/warfare-starwars
```

### Generate Production FBX Files

**1. Install Blender**
```bash
# macOS
brew install blender

# Linux
sudo apt install blender

# Windows
winget install Blender.Blender
```

**2. Download Kenney Assets**
```bash
# From https://kenney.nl/assets/sci-fi-rts
# Extract to source/kenney/sci-fi-rts/
# Should have: structure-a.fbx through structure-h.fbx
```

**3. Validate Config (Dry-Run)**
```bash
cd packs/warfare-starwars
python build_all_buildings.py --dry-run
```

**4. Run Batch Export**
```bash
# With 4 parallel workers
python build_all_buildings.py --parallel 4

# Or use all CPU cores
python build_all_buildings.py --parallel $(nproc)
```

**5. Check Results**
```bash
# File count (should be 44)
ls -1 assets/meshes/buildings/*.fbx | wc -l

# Summary
jq '.summary' EXPORT_RESULTS.json

# Details
jq '.results[] | select(.status != "success")' EXPORT_RESULTS.json
```

---

## Building Categories (10 Types, 22 Archetypes, 44 Files)

| Category | Buildings | Rep | CIS | Tris |
|----------|-----------|-----|-----|------|
| Residential | house_clone_quarters, house_droid_pod | 2 | 2 | 320 |
| Economy | farm_hydroponic, farm_fuel_harvester, farm_moisture_farm, farm_moisture_extractor | 4 | 4 | 280 |
| Storage | granary_biodome, granary_ore_vault | 2 | 2 | 300 |
| Stone Mining | stone_crystal_mine, stone_mineral_processor | 2 | 2 | 320 |
| Iron Forging | iron_forge_station, iron_metal_foundry | 2 | 2 | 350 |
| Soul/Mystical | soul_meditation_chamber, soul_sith_altar | 2 | 2 | 290 |
| Builder | builder_engineering_station, builder_construction_bot_factory | 2 | 2 | 380 |
| Guild/Trading | guild_merchant_bazaar, guild_trade_hub | 2 | 2 | 310 |
| Defense Gate | gate_shield_generator, gate_repulse_barrier | 2 | 2 | 340 |
| Hospital | hospital_medical_bay, hospital_clone_bank | 2 | 2 | 260 |

---

## File Naming Convention

```
[faction_prefix]_[building_id].fbx

rep_house_clone_quarters.fbx    # Republic variant
cis_house_clone_quarters.fbx    # CIS variant
rep_farm_hydroponic.fbx         # Republic variant
cis_farm_hydroponic.fbx         # CIS variant
```

---

## Faction Colors

**Republic**: White (#F5F5F5) + Blue (#1A3A6B / #64A0DC)
**CIS**: Grey (#444444) + Orange (#B35A00 / #663300)

---

## Documentation

- **BUILDINGS_FBXEXPORT_GUIDE.md** - Complete reference (building inventory, pipeline, integration)
- **BUILDINGS_GENERATION_SUMMARY.md** - Summary with metrics and troubleshooting
- **BUILDINGS_MANIFEST.txt** - File listing and verification commands
- **EXECUTION_REPORT.md** - Detailed execution summary
- **QUICK_START.md** - This file

---

## Scripts

**build_all_buildings.py**
```bash
# Validate without running Blender
python build_all_buildings.py --dry-run

# Run with 4 parallel workers
python build_all_buildings.py --parallel 4

# Custom Blender path
python build_all_buildings.py --blender-path /custom/path/blender
```

**generate_all_fbx_stubs.py**
```bash
# Create all 44 stub files (instant)
python generate_all_fbx_stubs.py
```

---

## Integration Points

### ContentLoader
```csharp
var buildings = await contentLoader.LoadAssetsAsync<BuildingModel>(
    "buildings/rep_*.fbx",
    FactionFilter.Republic
);
```

### BuildingRegistry
```csharp
registry.Register(new BuildingModel {
    Id = "house_clone_quarters",
    Faction = "republic",
    MeshPath = "buildings/rep_house_clone_quarters.fbx",
    PolyCount = 320
});
```

---

## Troubleshooting

**Q: Blender not found?**
A: Install it or use `--blender-path /custom/path`

**Q: Kenney files not found?**
A: Download from kenney.nl and extract to `source/kenney/sci-fi-rts/`

**Q: Export timeout?**
A: Reduce `--parallel` count or increase timeout in script

**Q: High poly count?**
A: Check decimate settings in `blender_batch_export.py`

---

## Next Steps

1. **Now**: Stubs created ✅
2. **Next**: Test with stub files
   - `dotnet run ... -- validate packs/warfare-starwars`
3. **Then**: Generate production FBX
   - `python build_all_buildings.py --parallel 4`
4. **Finally**: Integrate with DINO

---

**Author**: DINOForge Agent
**Created**: 2026-03-12
**Status**: Ready for Development & Production Export
