# Star Wars Clone Wars Units - FBX Asset Sourcing Plan

**Objective**: Generate all 26 unit FBX meshes (13 archetypes × 2 factions)
**Output**: `assets/meshes/units/` (26 FBX files)
**Naming**: `{faction}_{unit_id}.fbx` (e.g., `rep_clone_militia.fbx`, `cis_b1_battle_droid.fbx`)
**Specs**: 300-600 tris per unit, faction colors applied, optimized
**Timeline**: Phase 2 (parallel batch export via Blender)

## Asset Sources Priority

### Primary: Kenney.nl 3D Models (CC0)
- **Source**: https://kenney.nl/assets/3d-models
- **License**: CC0 1.0 Universal (Public Domain)
- **Package**: Kenney Sci-Fi RTS collection
- **Available Models**:
  - `soldier-a.fbx` - Basic humanoid (Clone Militia template)
  - `soldier-b.fbx` - Medium armor (Clone Trooper, ARC Trooper)
  - `soldier-c.fbx` - Heavy armor (Clone Heavy, Clone Commando)
  - `soldier-d.fbx` - Elite/hero variant (Jedi Knight)
  - `robot-a.fbx` - B1 Battle Droid template
  - `robot-b.fbx` - Heavy robot (B2 Super, BX Commando, MagnaGuard)
  - `robot-c.fbx` - Small/lightweight (Probe Droid, Dwarf Spider)
  - `robot-d.fbx` - Hero droid (General Grievous)
  - `robot-e.fbx` - Droideka variant
  - `vehicle-a.fbx` - Speeder bike (BARC, STAP)
  - `vehicle-c.fbx` - Medium tank (AAT)
  - `vehicle-e.fbx` - Large walker (AT-TE)

### Fallback: Sketchfab Free Models
- **Source**: https://sketchfab.com
- **Filter**: Free, CC0/CC-BY license
- **Search Terms**:
  - "Clone Trooper" / "Republic Soldier"
  - "Battle Droid" / "B1 Droid"
  - "AT-TE Walker"
  - "AAT Tank"
  - "BARC Speeder"
  - "General Grievous"

### Exclude: Paid/Licensed Assets
- Don't use Star Wars official assets (copyright)
- Don't use models requiring commercial license
- All assets must be CC0 or clearly licensed for reuse

## Unit-to-Asset Mapping

### Republic Units (Clone Troopers - Order Archetype)

| Unit ID | Display Name | Unit Type | Kenney Base | Target Tris | Notes |
|---------|--------------|-----------|-------------|------------|-------|
| rep_clone_militia | Clone Militia | Militia | soldier-a | 400 | Basic training clone |
| rep_clone_trooper | Clone Trooper | Line Infantry | soldier-a | 450 | Standard issue blaster |
| rep_clone_heavy | Clone Heavy Trooper | Heavy Infantry | soldier-c | 500 | Heavy armor + rotary cannon |
| rep_clone_sharpshooter | Clone Sharpshooter | Ranged Infantry | soldier-a | 400 | Long-range sniper |
| rep_barc_speeder | BARC Speeder | Cavalry | vehicle-a | 550 | Fast reconnaissance speeder |
| rep_atte_crew | AT-TE Crew | Siege | vehicle-e | 600 | Large walker, siege capability |
| rep_clone_medic | Clone Medic | Support | soldier-a | 400 | Medical specialist |
| rep_arf_trooper | ARF Trooper | Scout | soldier-a | 380 | Advanced reconnaissance |
| rep_arc_trooper | ARC Trooper | Elite | soldier-b | 480 | Enhanced armor variant |
| rep_jedi_knight | Jedi Knight | Hero | soldier-d | 550 | Hero unit with lightsaber |
| rep_clone_wall_guard | Clone Wall Guard | Wall Defender | soldier-c | 420 | Fortification specialist |
| rep_clone_sniper | Clone Sniper | Skirmisher | soldier-b | 400 | Elite marksman |
| rep_clone_commando | Clone Commando | Special | soldier-c | 500 | Special forces elite |

### CIS Units (Battle Droids - Industrial Swarm Archetype)

| Unit ID | Display Name | Unit Type | Kenney Base | Target Tris | Notes |
|---------|--------------|-----------|-------------|------------|-------|
| cis_b1_battle_droid | B1 Battle Droid | Militia | robot-a | 380 | Basic cheap droid |
| cis_b1_squad | B1 Squad | Line Infantry | robot-a | 400 | Squad-based variant |
| cis_b2_super_battle_droid | B2 Super Battle Droid | Heavy Infantry | robot-b | 520 | Heavily armored |
| cis_sniper_droid | Sniper Droid | Ranged Infantry | robot-a | 360 | Modified for accuracy |
| cis_stap_pilot | STAP Pilot | Cavalry | vehicle-a | 480 | Speeder platform |
| cis_aat_crew | AAT Crew | Siege | vehicle-c | 600 | Medium tank, fire support |
| cis_medical_droid | Medical Droid | Support | robot-a | 360 | Repair/support function |
| cis_probe_droid | Probe Droid | Scout | robot-c | 320 | Reconnaissance drone |
| cis_bx_commando_droid | BX Commando Droid | Elite | robot-b | 480 | Elite commando variant |
| cis_general_grievous | General Grievous | Hero | robot-d | 580 | Hero commander |
| cis_droideka | Droideka | Wall Defender | robot-e | 520 | Destroyer droid, shielded |
| cis_dwarf_spider_droid | DSD1 Dwarf Spider Droid | Skirmisher | robot-c | 450 | Small walking tank |
| cis_magnaguard | IG-100 MagnaGuard | Special | robot-b | 500 | Elite bodyguard droid |

## Faction Color Application

### Republic (Clone Troopers)
- **Primary**: #F5F5F5 (Pristine White - 0.961, 0.961, 0.961)
- **Secondary**: #1A3A6B (Deep Blue - 0.102, 0.227, 0.420)
- **Tertiary**: #64A0DC (Accent Blue - 0.392, 0.627, 0.859)
- **Metallic**: 0.1 (minimal shine)
- **Roughness**: 0.7 (matte armor)

### CIS (Battle Droids)
- **Primary**: #444444 (Dark Grey - 0.267, 0.267, 0.267)
- **Secondary**: #B35A00 (Rust Orange - 0.702, 0.353, 0.0)
- **Tertiary**: #663300 (Dark Brown - 0.400, 0.200, 0.0)
- **Metallic**: 0.15 (slight metallic sheen)
- **Roughness**: 0.6 (glossy droid armor)

## Export Workflow

### Step 1: Prepare Kenney Assets
```bash
# Download Kenney Sci-Fi RTS pack
# Extract to: source/kenney/sci-fi-rts/Models/FBX/
```

### Step 2: Generate Batch Configuration
```bash
# Generate manifest + batch config
python3 blender_units_batch_export.py --batch-process
```

### Step 3: Parallel Batch Export
```bash
# Single unit test export
blender --background --python blender_units_batch_export.py -- \
    --unit clone_militia \
    --faction republic \
    --input source/kenney/sci-fi-rts/Models/FBX/soldier-a.fbx \
    --output assets/meshes/units/rep_clone_militia.fbx

# Full batch (requires Blender CLI scripting or task queue)
# Typically: GNU Parallel, xargs, or dedicated build orchestrator
for unit in $(cat batch_config.json | jq -r '.units[].full_id'); do
    blender --background --python blender_units_batch_export.py -- \
        --unit $unit --faction ... --input ... --output ...
done
```

### Step 4: Validation
```bash
# Verify all 26 files exist
ls -la assets/meshes/units/ | wc -l  # Should be 26

# Check file sizes and integrity
file assets/meshes/units/*.fbx

# Validate manifest
dotnet run --project src/Tools/PackCompiler -- validate packs/warfare-starwars
```

## Implementation Timeline

### Phase 1: Asset Sourcing (24h)
- [ ] Identify exact Kenney models or Sketchfab alternatives
- [ ] Document fallback sources for each unit type
- [ ] Create sourcing checklist

### Phase 2: FBX Batch Export (48h)
- [ ] Set up Blender environment with required plugins
- [ ] Generate stub FBX files (placeholders)
- [ ] Export 26 FBX files with faction colors
- [ ] Optimize geometry (300-600 tris target)
- [ ] Verify pivot points and scaling

### Phase 3: Validation & Integration (24h)
- [ ] Run pack validator (schema + reference checks)
- [ ] Verify texture/mesh asset registry
- [ ] Test in-game loading via ContentLoader
- [ ] Generate asset index + source attribution

### Phase 4: Documentation (12h)
- [ ] Create UNITS_ASSET_INDEX.md
- [ ] Document color application process
- [ ] Add Blender export templates
- [ ] Update manifest.yaml with unit assets

## Quality Checklist

- [ ] All 26 units have FBX files (100% coverage)
- [ ] File naming follows `{faction}_{unit_id}.fbx` pattern
- [ ] Triangle count in range 300-600 per unit
- [ ] Faction colors applied (not vanilla)
- [ ] Pivot points centered at base
- [ ] No missing material references
- [ ] FBX format valid (importable into Unity)
- [ ] Source attribution in ASSET_SOURCES.json
- [ ] Batch export log complete (UNITS_EXPORT_LOG.txt)
- [ ] Unit archetype mapping documented

## Success Criteria

✅ 26 FBX files generated in `assets/meshes/units/`
✅ All files follow naming convention: `{faction}_{unit_id}.fbx`
✅ Triangle count: 300-600 per unit
✅ Faction colors applied (republic = white/blue, cis = grey/orange)
✅ Files optimized and validated
✅ Source attribution complete
✅ Pack validation passing
✅ Ready for texture mapping + in-game integration

## Notes

- Kenney models are modular; expect to combine/adapt pieces
- Sketchfab as backup for unique units (heroes, special vehicles)
- Consider using Blender Python API for parallel batch processing
- FBX 2020 format recommended for maximum compatibility
- Target ~400-500 tris as sweet spot for game performance
