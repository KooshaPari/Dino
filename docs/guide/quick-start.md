# Quick Start

Create a minimal balance pack in under 5 minutes.

## 1. Create the Pack Directory

```bash
mkdir -p packs/my-balance-mod
```

## 2. Write the Manifest

Create `packs/my-balance-mod/pack.yaml`:

```yaml
id: my-balance-mod
name: My Balance Mod
version: 0.1.0
framework_version: ">=0.1.0 <1.0.0"
author: Your Name
type: balance
description: Doubles archer damage and increases wall HP
depends_on: []
conflicts_with: []
loads:
  units:
    - archer_buffed
  buildings: []
```

## 3. Define a Unit Override

Create `packs/my-balance-mod/units/archer_buffed.yaml`:

```yaml
id: archer_buffed
display_name: Elite Archer
unit_class: CoreLineInfantry
faction_id: vanilla
tier: 1
stats:
  hp: 100
  damage: 25       # doubled from vanilla ~12
  range: 8
  speed: 3.5
  cost:
    resource_1: 40
    resource_2: 10
    population: 1
  accuracy: 0.8
  fire_rate: 1.2
weapon: BallisticLight
defense_tags:
  - Unarmored
  - Biological
behavior_tags:
  - HoldLine
  - AdvanceFire
vanilla_mapping: archer
```

## 4. Validate the Pack

```bash
dotnet run --project src/Tools/PackCompiler -- validate packs/my-balance-mod
```

The validator checks:
- Manifest schema conformance
- Required fields present
- ID format (`^[a-z][a-z0-9-]*$` for packs, `^[a-z][a-z0-9_]*$` for units)
- Version format (semver)
- Dependency resolution
- Asset reference integrity

## 5. Build the Pack

```bash
dotnet run --project src/Tools/PackCompiler -- build packs/my-balance-mod
```

This produces a packaged artifact ready for installation.

## What Happened?

You just:
1. Defined a pack manifest with explicit metadata
2. Created a unit definition following the unit schema
3. Validated it against DINOForge's schema system
4. Built a distributable pack artifact

No C# code. No Harmony patches. No reverse engineering. Just YAML.

## Next Steps

- [Creating Packs](/guide/creating-packs) — Full guide to pack authoring
- [Schema Reference](/reference/schemas) — All content type schemas
- [Warfare Overview](/warfare/overview) — Build faction-themed packs
