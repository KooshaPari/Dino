# Asset Registry System - Architecture

## System Overview

The asset registry is a comprehensive tracking and validation system for managing vanilla DINO building reskins in the warfare-starwars pack. It enforces consistency, tracks progress, and maintains provenance across all 24 buildings.

## Data Flow Architecture

```
[Kenney.nl CC0 Assets]
         |
         v
[texture_generation.py] -> [Faction Color Palettes]
         |                         |
         v                         v
[PNG Textures] ---------> [asset_index.json]
         |                         |
         |                         v
         |                [validate_vanilla_assets.py]
         |                         |
         v                         v
[textures/buildings/] <-- [VANILLA_BUILDINGS.json]
         |                         |
         |                         v
         |                [provenance_index.json]
         |                         |
         v                         v
[TEXTURE_MANIFEST.json] <----> [Building YAML]
         |                    (republic/cis)
         v
[Pack Deployment]
```

## Core Registry Files Structure

### asset_index.json (Master Tracking Database)
- **Size**: ~20KB
- **Records**: 24 vanilla buildings
- **Fields per Building**:
  - vanilla_id (1-24)
  - vanilla_name (vanilla DINO name)
  - republic_id / cis_id (faction-specific IDs)
  - status (complete, in_progress, pending)
  - texture_status (tracking textures)
  - manifest_status (tracking YAML definitions)
  - texture_file_republic / texture_file_cis (PNG references)
  - kenney_source (source FBX file)
  - license (attribution)

### VANILLA_BUILDINGS.json (Scripting Metadata)
- **Size**: ~14KB
- **Records**: 24 buildings + texture palettes
- **Use Cases**:
  - Batch texture generation
  - Effort estimation
  - Category classification
  - Palette definitions

### provenance_index.json (License & Attribution)
- **Size**: ~8.3KB
- **Key Sections**:
  - primary_sources (14 Kenney packs with URLs)
  - derivative_works (texture transformations)
  - contributors (Kenney + DINOForge)
  - buildings_by_kenney_pack (reverse index)
  - compliance verification

## Integration Architecture

### Pack Manifest
```yaml
loads:
  buildings:
    - buildings/republic_buildings    # rep_* IDs
    - buildings/cis_buildings         # cis_* IDs
```

Asset registry ensures all IDs in manifests match definition files.

### Texture Pipeline
```
1. texture_generation.py reads faction color palettes
2. Generates PNG files in textures/buildings/
3. Updates asset_index.json status
4. Writes metadata to TEXTURE_MANIFEST.json
5. Tracks in provenance_index.json
```

### Building Definitions
```
asset_index.json entries:
  vanilla_id: 1
  republic_id: rep_command_center
    |
    v
republic_buildings.yaml:
  - id: rep_command_center
    display_name: Republic Command Center
    ...
```

### Validation Integration
```
validate_vanilla_assets.py:
  Input:  asset_index.json + file system
  Checks: textures exist, manifests valid, sources documented
  Output: validation report + exit code (0=pass, 1=fail)
```

## State Transitions

Each building progresses through a state machine:

```
PENDING -> IN_PROGRESS -> COMPLETE

With sub-components tracking independently:
  texture_status:  not_started -> in_progress -> complete
  manifest_status: pending -> complete
```

## Status Tracking Hierarchy

```
Overall: 10/24 (41.7%)
  |
  +-- By Type
  |   +-- Command (1/1) = 100%
  |   +-- Barracks (3/3) = 100%
  |   +-- Defense (5/5) = 60%
  |   +-- Economy (3/4) = 75%
  |   +-- Research (1/1) = 100%
  |
  +-- By Status
  |   +-- Complete: 10
  |   +-- In Progress: 2
  |   +-- Pending: 12
  |
  +-- By Component
      +-- Textures: 10 complete, 2 in progress, 12 pending
      +-- Manifests: 10 complete, 14 pending
```

## License Compliance Model

```
Kenney.nl Assets (Original)
  |
  +-- License: CC0 1.0 Universal
  +-- Attribution: Not required
  +-- Commercial Use: Allowed
  +-- Derivatives: Allowed
  |
  v
provenance_index.json (Documentation)
  |
  +-- Primary source links
  +-- Pack URLs
  +-- Model lists
  +-- Attribution credits
  |
  v
Faction Textures (Derivatives)
  |
  +-- Method: HSV color space transformation
  +-- Attribution: DINOForge team
  +-- License: CC0 (inherited)
  +-- Tracked in: asset_index.json
  |
  v
Compliance Verification
  |
  +-- CC0 status: PRESERVED
  +-- Attribution: TRACKED
  +-- Commercial: ALLOWED
  +-- Derivatives: ALLOWED
```

## Query Patterns

```python
# Find buildings by status
complete = [b for b in index['buildings']
            if b['status'] == 'complete']

# Find textures needing generation
pending_textures = [b for b in index['buildings']
                    if b['texture_status'] == 'not_started']

# Get buildings from specific Kenney pack
from_structures = [b for b in index['buildings']
                   if b['kenney_source'] == 'kenney_structure_c']

# List missing texture files
missing = []
for b in index['buildings']:
    for file in [b['texture_file_republic'], b['texture_file_cis']]:
        if not path_exists(file):
            missing.append(file)
```

## Scalability

Designed to scale to 50+ buildings:

- asset_index.json: ~0.8KB per building (40KB at 50 buildings)
- VANILLA_BUILDINGS.json: ~0.6KB per building (30KB at 50 buildings)
- Validation time: O(n) performance (~100ms for 50 buildings)
- Directory structure: Flat directories scale to 1000+ files

## Extension Points

Future enhancements:

1. **Mesh Registry** - FBX compilation tracking
2. **Animation Mappings** - Destruction/construction/idle animations
3. **Sound Asset Tracking** - Audio variants per faction
4. **Lore Database** - Flavor text, descriptions, historical context
5. **Performance Profiling** - Draw calls, triangle counts, memory

All would follow the same registry pattern:
- Master index JSON
- Status tracking
- Validation automation
- Provenance tracking

## Files and Locations

```
packs/warfare-starwars/
  |
  +-- assets/
  |   |
  |   +-- registry/
  |   |   +-- asset_index.json
  |   |   +-- VANILLA_BUILDINGS.json
  |   |   +-- provenance_index.json
  |   |   +-- README.md
  |   |   +-- SYSTEM_ARCHITECTURE.md
  |   |
  |   +-- textures/buildings/
  |   |   +-- rep_*.png (20 files)
  |   |   +-- cis_*.png (20 files)
  |   |   +-- TEXTURE_MANIFEST.json
  |   |
  |   +-- meshes/buildings/
  |   |   +-- rep_*.fbx
  |   |   +-- cis_*.fbx
  |   |
  |   +-- VANILLA_BUILDING_COVERAGE.md
  |
  +-- buildings/
  |   +-- republic_buildings.yaml
  |   +-- cis_buildings.yaml
  |
  +-- validate_vanilla_assets.py
  +-- pack.yaml
```

---

**Last Updated**: 2026-03-12
**System Version**: 1.0
**Coverage**: 10/24 buildings (41.7%)
