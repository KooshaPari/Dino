# Star Wars Clone Wars Units - FBX Generation Guide

**Objective**: Generate 26 unit FBX files (13 archetypes × 2 factions)
**Output Directory**: `packs/warfare-starwars/assets/meshes/units/`
**Generated**: March 12, 2026
**Status**: Ready for execution

## Quick Start

### Option 1: Generate Stub Files (Fast - 5 minutes)

For testing the pipeline without Blender:

```bash
cd packs/warfare-starwars
python3 generate_units_fbx_stubs.py
ls assets/meshes/units/ | wc -l  # Should output 26
```

**Output**: 26 minimal FBX placeholder files with metadata

### Option 2: Full Blender Batch Export (Production - 1 hour)

With Blender 3.0+ installed and Kenney assets available:

```bash
cd packs/warfare-starwars

# 1. Download Kenney Sci-Fi RTS assets
mkdir -p source/kenney/sci-fi-rts/Models/FBX/
# Download from https://kenney.nl/assets/3d-models
# Extract FBX files to source/kenney/sci-fi-rts/Models/FBX/

# 2. Run parallel batch export (requires 4+ CPU cores)
chmod +x run_units_batch_export.sh
./run_units_batch_export.sh --parallel 4

# 3. Verify output
ls -la assets/meshes/units/ | wc -l  # Should output 26
file assets/meshes/units/*.fbx | head -5

# 4. Check log
tail -50 UNITS_EXPORT_LOG.txt
```

**Output**: 26 optimized FBX files with faction colors applied

### Option 3: Single Unit Test (Debug - 5 minutes)

Test the export pipeline with a single unit:

```bash
cd packs/warfare-starwars

# Dry-run (validate config without Blender)
blender --background --python blender_units_batch_export.py -- \
    --unit clone_militia \
    --faction republic \
    --input source/kenney/sci-fi-rts/Models/FBX/soldier-a.fbx \
    --output assets/meshes/units/rep_clone_militia.fbx \
    --dry-run

# Actual export (requires Blender)
blender --background --python blender_units_batch_export.py -- \
    --unit clone_militia \
    --faction republic \
    --input source/kenney/sci-fi-rts/Models/FBX/soldier-a.fbx \
    --output assets/meshes/units/rep_clone_militia.fbx
```

## Detailed Workflow

### Step 1: Prepare Environment

```bash
cd packs/warfare-starwars

# Create directories
mkdir -p source/kenney/sci-fi-rts/Models/FBX/
mkdir -p assets/meshes/units/

# Download Kenney assets
# Visit: https://kenney.nl/assets/3d-models
# Extract to source/kenney/sci-fi-rts/Models/FBX/

# Verify Blender installation
blender --version  # Should output Blender 3.0+
```

### Step 2: Generate Batch Configuration

```bash
# Review batch configuration
cat units_batch_config.json | jq '.units | length'  # Should output 26

# List all units to be exported
cat units_batch_config.json | jq -r '.units[] | "\(.faction) - \(.display_name)"'
```

### Step 3: Execute Batch Export

#### Option A: Parallel (Recommended)
```bash
./run_units_batch_export.sh --parallel 4
```

**Timing**: ~30-60 minutes (4 parallel Blender instances)
**Resources**: 8+ GB RAM, 4+ CPU cores

#### Option B: Sequential
```bash
./run_units_batch_export.sh --parallel 1
```

**Timing**: ~2-3 hours (single Blender instance)
**Resources**: 4+ GB RAM, 1+ CPU cores

#### Option C: Single Faction
```bash
# Export only Republic units
./run_units_batch_export.sh --faction republic --parallel 4

# Export only CIS units
./run_units_batch_export.sh --faction cis --parallel 4
```

### Step 4: Verify Output

```bash
# Check file count
ls assets/meshes/units/ | wc -l  # Should output 26

# Check file sizes
du -sh assets/meshes/units/  # Should be 3-5 MB total

# Verify all units are present
for faction in rep cis; do
    echo "$faction units:"
    ls assets/meshes/units/${faction}_*.fbx | wc -l
done

# Check for errors in log
grep "ERROR" UNITS_EXPORT_LOG.txt || echo "No errors found"

# Summary statistics
tail -100 UNITS_EXPORT_LOG.txt | grep "status.*success"
```

### Step 5: Validate Pack

```bash
# Run DINOForge pack validator
cd /path/to/DINOForge
dotnet run --project src/Tools/PackCompiler -- validate packs/warfare-starwars

# Expected output:
# [OK] packs/warfare-starwars/pack.yaml
# [OK] 26 unit mesh assets found
# [OK] All faction color variants present
```

### Step 6: Integration Test (Optional)

```bash
# In Unity editor with BepInEx + DINOForge runtime loaded:
# 1. Load the Star Wars pack via ContentLoader
# 2. Verify all 26 unit meshes render with correct colors
# 3. Check performance: should maintain 60+ FPS with all units visible

# Command-line test:
dotnet run --project src/Tools/Inspector -- \
    --pack warfare-starwars \
    --inspect units
```

## Configuration Reference

### Batch Export Script Options

```bash
./run_units_batch_export.sh [OPTIONS]

Options:
  --dry-run              Validate config without exporting (fast)
  --parallel N           Number of parallel Blender instances (default: 4)
  --faction FACTION      Export only "republic" or "cis" units

Examples:
  ./run_units_batch_export.sh                    # Full batch, 4 parallel
  ./run_units_batch_export.sh --dry-run          # Validate only
  ./run_units_batch_export.sh --parallel 2       # 2 parallel instances
  ./run_units_batch_export.sh --faction republic # Republic only
```

### Blender Python Script Options

```bash
blender --background --python blender_units_batch_export.py -- [OPTIONS]

Options:
  --unit UNIT_ID         Unit archetype ID (e.g., clone_militia)
  --faction FACTION      "republic" or "cis"
  --input FILE           Source Kenney FBX file
  --output FILE          Output FBX path
  --log FILE             Export log file (default: UNITS_EXPORT_LOG.txt)
  --dry-run              Validate without exporting
  --batch-process        Generate batch configuration for all 26 units

Examples:
  # Single export with faction colors
  blender --background --python blender_units_batch_export.py -- \
      --unit clone_militia \
      --faction republic \
      --input source/kenney/sci-fi-rts/Models/FBX/soldier-a.fbx \
      --output assets/meshes/units/rep_clone_militia.fbx

  # Generate batch config
  blender --background --python blender_units_batch_export.py -- \
      --batch-process
```

## Troubleshooting

### Issue: "Blender not found in PATH"

**Solution:**
```bash
# Add Blender to PATH
export PATH="/path/to/blender:$PATH"

# Or use full path
/usr/bin/blender --background --python blender_units_batch_export.py -- ...
```

### Issue: "Source FBX not found"

**Solution:**
```bash
# Verify Kenney assets are downloaded
ls source/kenney/sci-fi-rts/Models/FBX/soldier-*.fbx

# Download from: https://kenney.nl/assets/3d-models
# Extract to: source/kenney/sci-fi-rts/Models/FBX/
```

### Issue: "Permission denied" on shell script

**Solution:**
```bash
chmod +x run_units_batch_export.sh
./run_units_batch_export.sh
```

### Issue: Blender crashes during export

**Solution:**
```bash
# Reduce parallel jobs to lower memory usage
./run_units_batch_export.sh --parallel 1

# Or increase system swap/virtual memory
# Windows: System Properties > Performance > Virtual Memory
# Linux: sudo swapon -s && sudo fallocate -l 4G /swapfile
```

### Issue: Export timeout (running > 3 hours)

**Solution:**
```bash
# Increase parallel jobs
./run_units_batch_export.sh --parallel 8

# Or check for stalled Blender processes
ps aux | grep blender
kill -9 [PID]  # Force terminate if stuck
```

## Expected Output

### File Structure After Generation

```
packs/warfare-starwars/
  assets/
    meshes/
      units/
        rep_clone_militia.fbx        (400 tris)
        rep_clone_trooper.fbx        (450 tris)
        rep_clone_heavy.fbx          (500 tris)
        rep_clone_sharpshooter.fbx   (400 tris)
        rep_barc_speeder.fbx         (550 tris)
        rep_atte_crew.fbx            (600 tris)
        rep_clone_medic.fbx          (400 tris)
        rep_arf_trooper.fbx          (380 tris)
        rep_arc_trooper.fbx          (480 tris)
        rep_jedi_knight.fbx          (550 tris)
        rep_clone_wall_guard.fbx     (420 tris)
        rep_clone_sniper.fbx         (400 tris)
        rep_clone_commando.fbx       (500 tris)
        cis_b1_battle_droid.fbx      (380 tris)
        cis_b1_squad.fbx             (400 tris)
        cis_b2_super_battle_droid.fbx (520 tris)
        cis_sniper_droid.fbx         (360 tris)
        cis_stap_pilot.fbx           (480 tris)
        cis_aat_crew.fbx             (600 tris)
        cis_medical_droid.fbx        (360 tris)
        cis_probe_droid.fbx          (320 tris)
        cis_bx_commando_droid.fbx    (480 tris)
        cis_general_grievous.fbx     (580 tris)
        cis_droideka.fbx             (520 tris)
        cis_dwarf_spider_droid.fbx   (450 tris)
        cis_magnaguard.fbx           (500 tris)
  UNITS_EXPORT_LOG.txt             (Export metadata)
  UNITS_MANIFEST.json              (Generation manifest)
```

### Log Output Example

```
DINOForge Star Wars Units - FBX Batch Export
==============================================

Configuration:
  Batch config: units_batch_config.json
  Log file: UNITS_EXPORT_LOG.txt
  Dry run: false
  Parallel jobs: 4
  Target faction: all

GNU Parallel detected. Using parallel mode.
Exporting units (parallel mode, 4 jobs)...
======================================
✓ clone_militia exported
✓ clone_trooper exported
✓ clone_heavy exported
...
======================================
Exported: 26/26 units

Export complete! Ready for texture generation and validation.
```

## Next Steps

After successful FBX generation:

1. **Generate Textures**: Run `texture_generation.py` to create faction color variants
2. **Validate Pack**: Execute `dotnet run --project src/Tools/PackCompiler -- validate packs/warfare-starwars`
3. **Integration Test**: Load Star Wars pack in DINO with BepInEx
4. **Generate Asset Atlas**: Combine unit textures into shared material atlases
5. **Performance Profiling**: Measure frame rate with all 26 units visible

## References

- **UNITS_ASSET_INDEX.md** - Complete asset inventory and specifications
- **UNITS_FBX_SOURCING_PLAN.md** - Asset sourcing strategy and fallback sources
- **blender_units_batch_export.py** - Main export script (280 lines)
- **run_units_batch_export.sh** - Parallel execution controller (150 lines)
- **units_batch_config.json** - Configuration manifest (all 26 units)

## Summary

You now have a complete FBX generation pipeline for all 26 Star Wars Clone Wars unit meshes:

✅ Batch configuration with all 26 units mapped to Kenney sources
✅ Blender Python scripts with automatic faction color application
✅ Parallel batch processing infrastructure (4-8 instances)
✅ Quality assurance checklist
✅ Troubleshooting guide
✅ Validation workflow

**Time to completion**: 30 minutes - 3 hours (depending on parallel jobs and system resources)
**Output**: ~3-5 MB (26 optimized FBX files with faction colors)
**Next phase**: Texture generation and in-game integration testing
