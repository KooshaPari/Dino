# Mod Authoring Guide

A practical guide for creating mods with DINOForge.

---

## Getting Started

### Prerequisites

- **BepInEx 5.4.23.5** installed in your DINO game directory (the modified Doorstop fork, not standard BepInEx)
- **DINOForge built** from source (`dotnet build src/DINOForge.sln`)
- **Runtime deployed** to `BepInEx/ecs_plugins/DINOForge.Runtime.dll`
- .NET 8 SDK installed

### Pack Directory Structure

Every mod is a **pack** -- a folder containing a manifest and content files:

```
packs/my-mod/
  pack.yaml              # Required: pack metadata and content declarations
  units/                 # Unit definitions (YAML)
  buildings/             # Building definitions
  weapons/               # Weapon definitions
  factions/              # Faction definitions
  doctrines/             # Doctrine definitions
  waves/                 # Wave template definitions
  stats/                 # Stat override files
  scenarios/             # Scenario definitions
  assets/                # Icons, audio, VFX references
    icons/
    audio/
  localization/          # Text/string bundles
    en.yaml
```

### pack.yaml Manifest Format

Every pack requires a `pack.yaml` at its root. All fields:

```yaml
# --- Required Fields ---
id: my-mod                          # Unique ID (lowercase, hyphens ok, must start with letter)
name: My Mod                        # Human-readable display name
version: 0.1.0                      # Semantic version (major.minor.patch, optional pre-release suffix)
framework_version: ">=0.1.0 <1.0.0" # Compatible DINOForge version range
author: Your Name                   # Author name
type: balance                       # Pack type: content | balance | ruleset | total_conversion | utility

# --- Optional Fields ---
description: Buffs melee units      # Brief description of what the pack does

depends_on:                         # List of pack IDs this pack requires
  - dino-warfare-domain

conflicts_with:                     # List of pack IDs that cannot coexist with this pack
  - some-other-mod

load_order: 100                     # Load priority (lower number = loads earlier, default: 100)

game_version: ">=1.0.0"            # Compatible DINO game version range

loads:                              # Content this pack adds to registries
  factions: []
  units: []
  buildings: []
  weapons: []
  projectiles: []
  effects: []
  doctrines: []
  audio: []
  visuals: []
  localization: []
  wave_templates: []
  tech_nodes: []
  scenarios: []

overrides:                          # Existing registry entries this pack modifies
  units: []
  buildings: []
  stats: []

asset_policy:                       # Asset sourcing metadata
  allow_generated: true
  allow_public_assets: true
  allow_borrowed_assets: only_with_permission  # yes | only_with_permission | no
  credits_manifest: assets/CREDITS.md
  provenance_manifest: assets/PROVENANCE.md
```

---

## Creating Your First Mod

Walk-through: a balance mod that buffs melee units.

### Step 1: Create the Pack Directory

```bash
mkdir -p packs/melee-buff/stats
```

### Step 2: Write pack.yaml

Create `packs/melee-buff/pack.yaml`:

```yaml
id: melee-buff
name: Melee Unit Buff
version: 0.1.0
framework_version: ">=0.1.0 <1.0.0"
author: Your Name
type: balance
description: Increases melee unit HP and damage by 20%

depends_on: []
conflicts_with: []

overrides:
  stats:
    - melee-overrides
```

### Step 3: Write the Stat Override

Create `packs/melee-buff/stats/melee-overrides.yaml`:

```yaml
id: melee-overrides
description: Buff all melee units
targets:
  - match:
      tags: [melee]
    modifications:
      hp:
        multiply: 1.2
      damage:
        multiply: 1.2
```

### Step 4: Validate

```bash
dotnet run --project src/Tools/PackCompiler -- validate packs/melee-buff
```

### Step 5: Build

```bash
dotnet run --project src/Tools/PackCompiler -- build packs/melee-buff
```

### Step 6: Deploy and Test

Copy the built pack to `BepInEx/dinoforge_packs/` in your game directory, launch DINO, and verify melee units have higher stats.

---

## Content Types

### Units

Define units in `units/<name>.yaml`. Key fields:

```yaml
id: heavy-trooper
name: Heavy Trooper
faction: west
role: line_infantry
tags: [infantry, armored, melee]
stats:
  hp: 200
  armor: 15
  speed: 3.5
  damage: 25
  attack_speed: 1.2
  range: 1.5
  cost:
    food: 80
    wood: 20
    iron: 40
  train_time: 12
```

See `schemas/unit.schema.json` for the full schema.

### Buildings

Define buildings in `buildings/<name>.yaml`:

```yaml
id: armory
name: Armory
faction: west
category: military
stats:
  hp: 500
  armor: 10
  build_time: 30
  cost:
    wood: 200
    stone: 100
    iron: 50
provides:
  - unlock: heavy-trooper
```

See `schemas/building.schema.json` for the full schema.

### Factions

Define factions in `factions/<name>.yaml`:

```yaml
id: west
name: Western Alliance
archetype: order
description: Strong line infantry, reliable DPS, better defenses
palette:
  primary: "#2E5090"
  secondary: "#C0C0C0"
roster:
  units:
    - rifleman
    - heavy-trooper
    - sniper
  buildings:
    - barracks
    - armory
    - watchtower
```

See `schemas/faction.schema.yaml` for the full schema.

### Weapons

Define weapons in `weapons/<name>.yaml`:

```yaml
id: assault-rifle
name: Assault Rifle
type: ranged
stats:
  damage: 15
  fire_rate: 5.0
  range: 25
  accuracy: 0.75
  projectile: bullet-standard
```

See `schemas/weapon.schema.json` for the full schema.

### Doctrines

Doctrines modify faction-wide behavior:

```yaml
id: defensive-stance
name: Defensive Stance
faction: west
effects:
  - target: all_units
    stat: armor
    multiply: 1.3
  - target: all_buildings
    stat: hp
    multiply: 1.15
  - target: all_units
    stat: speed
    multiply: 0.85
```

See `schemas/doctrine.schema.json` for the full schema.

### Stat Overrides

The stat override system lets balance mods modify existing content without redefining it:

```yaml
id: archer-nerf
description: Reduce archer effectiveness
targets:
  - match:
      id: vanilla_archer
    modifications:
      damage:
        multiply: 0.8
      range:
        add: -2
  - match:
      tags: [ranged, light]
    modifications:
      accuracy:
        multiply: 0.9
```

See `schemas/stat-override.schema.json` for the full schema.

### Scenarios

Define scripted scenarios in `scenarios/<name>.yaml`:

```yaml
id: last-stand
name: The Last Stand
description: Defend against 10 escalating waves with limited resources
starting_conditions:
  resources:
    food: 500
    wood: 300
    stone: 200
  buildings:
    - town_center
wave_overrides:
  - wave: 1
    enemy_count: 50
  - wave: 10
    enemy_count: 500
    boss: true
victory: survive_all_waves
```

See `schemas/scenario.schema.json` for the full schema.

### Economy Profiles

Economy profiles adjust resource rates and costs globally. Define economy profiles in `profiles/<name>.yaml`:

```yaml
id: balanced-economy
description: Balanced resource rates for extended games
resource_rates:
  food: 1.0
  wood: 1.0
  stone: 0.9
  iron: 0.8
  gold: 0.7
trade_modifiers:
  buy_markup: 1.2
  sell_discount: 0.8
```

See `schemas/economy-profile.schema.json` for the full schema. Also see `packs/economy-balanced/` for a working example.

---

## Pack Types

| Type | Purpose | Typical Content |
|------|---------|-----------------|
| `content` | Add new things to the game | Units, buildings, factions, weapons, projectiles |
| `balance` | Tweak existing values | Stat overrides, cost adjustments, timing changes |
| `ruleset` | Change game rules | Research requirements, wave timings, victory conditions, population caps |
| `total_conversion` | Replace major game systems | Full faction sets, themed assets, Universe Bible-driven content |
| `utility` | Development and debug tools | Entity inspectors, profilers, debug overlays |

---

## Testing Your Pack

### Schema Validation

Validate that all YAML files conform to their schemas:

```bash
# Validate one pack
dotnet run --project src/Tools/PackCompiler -- validate packs/my-mod

# Validate all packs
dotnet run --project src/Tools/PackCompiler -- validate packs/
```

The validator checks:
- Schema conformance for all YAML files
- Required fields and correct types
- ID format and uniqueness
- Dependency resolution (no cycles, no missing deps)
- Asset reference integrity

### Running Tests

```bash
dotnet test src/DINOForge.sln --verbosity normal
```

### Linting

```bash
dotnet format src/DINOForge.sln --verify-no-changes
```

---

## Hot Reload

DINOForge supports hot module replacement (HMR) for YAML-based pack content.

### How It Works

The runtime watches your pack directories for file changes. When you save a YAML file, the runtime:

1. Detects the changed file
2. Re-validates the modified content against its schema
3. Re-applies the content to the appropriate registry
4. Updates in-game state without restarting

### Editing YAML Live

1. Launch DINO with DINOForge loaded
2. Open any pack YAML file in your editor
3. Make changes and save
4. Changes appear in-game within seconds

### Using the F10 Mod Menu

Press **F10** in-game to open the mod menu overlay. From here you can:

- View loaded packs and their status
- Enable/disable packs
- Trigger manual reload
- See validation errors for any pack

Press **F9** for the debug overlay with ECS introspection info.

---

## Advanced Topics

### Dependencies and Conflicts

Packs declare explicit relationships in `pack.yaml`:

- **`depends_on`**: Lists pack IDs that must be loaded before this pack. The dependency resolver ensures correct load order and flags missing dependencies.
- **`conflicts_with`**: Lists pack IDs that cannot be active at the same time. The loader prevents conflicting packs from loading together.
- **`framework_version`**: Semver range specifying compatible DINOForge versions. Packs targeting an incompatible framework version are rejected at load time.

### Load Order

Content is applied in a deterministic order:

1. Packs with lower `load_order` values load first (default: 100)
2. Within the same `load_order`, dependencies load before dependents
3. If two packs override the same registry entry, the later-loading pack wins

### Registry Layering

DINOForge applies content through a four-layer registry stack:

```
BaseGame         # Vanilla DINO values (read from ECS at boot)
  Framework      # DINOForge framework defaults
    DomainPlugin # Domain-specific content (e.g. Warfare plugin)
      Pack       # Your mod's additions and overrides
```

Each layer can add new entries or override entries from layers below. The final merged state is what the game uses.

### Universe Bible System

For total conversion packs, the Universe Bible system provides a structured way to define an entire themed universe:

- Lore, faction backstories, unit flavor text
- Consistent naming conventions across all content
- Theme-driven generation of pack content from a central bible document
- Pack generator that scaffolds a full total conversion from a Universe Bible

This is particularly useful for themed conversions like Star Wars or modern military, where consistency across hundreds of content entries matters.

See `schemas/universe-bible.json` for the schema. The SDK provides:
- `UniverseBible` / `UniverseLoader` -- Parse and validate Bible documents
- `FactionTaxonomy` -- Faction classification and relationships
- `NamingGuide` / `StyleGuide` -- Naming conventions and visual style rules
- `CrosswalkDictionary` -- Map vanilla elements to themed equivalents
- `PackGenerator` -- Scaffold a full total conversion pack from a Universe Bible
