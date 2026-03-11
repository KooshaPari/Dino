# Getting Started with DINOForge

This guide covers installation, development setup, and creating your first mod.

---

## Installation

### 1. Install BepInEx

DINOForge requires a **modified BepInEx 5.4.23.5** build with Unity ECS support. Standard BepInEx will not work with DINO.

1. Download BepInEx 5 with Unity ECS Support from [Nexus Mods](https://www.nexusmods.com/diplomacyisnotanoption/mods/1)
2. Extract into your DINO game directory (where the `.exe` lives)
3. Verify the folder structure:

```
GameRoot/
  winhttp.dll              # Modified Doorstop pre-loader
  doorstop_config.ini
  BepInEx/
    plugins/               # Standard plugins
    ecs_plugins/           # Where DINOForge Runtime goes
    config/
    dinoforge_packs/       # Where mod packs go
```

4. Launch the game once to let BepInEx initialize, then close it.

### 2. Build DINOForge

```bash
git clone https://github.com/KooshaPari/Dino.git
cd Dino
dotnet build src/DINOForge.sln
```

### 3. Deploy

Copy the built Runtime DLL to BepInEx:

```
BepInEx/ecs_plugins/DINOForge.Runtime.dll
```

Content packs go into:

```
BepInEx/dinoforge_packs/
```

### 4. Verify

Launch DINO and check:

- **F9**: Debug overlay (ECS introspection, system enumeration, entity counts)
- **F10**: Mod menu (loaded packs, status, enable/disable)

If both overlays respond, DINOForge is running.

---

## Development Setup

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- Git
- Diplomacy is Not an Option (Steam)

### Clone and Build

```bash
git clone https://github.com/KooshaPari/Dino.git
cd Dino
dotnet build src/DINOForge.sln
```

### Run Tests

```bash
dotnet test src/DINOForge.sln --verbosity normal
```

### Lint

```bash
dotnet format src/DINOForge.sln --verify-no-changes
```

### Validate Packs

```bash
dotnet run --project src/Tools/PackCompiler -- validate packs/
```

### Build a Pack

```bash
dotnet run --project src/Tools/PackCompiler -- build packs/<pack-name>
```

### Project Structure

```
DINOForge/
  src/
    Runtime/             # BepInEx plugin: bootstrap, ECS detection, hooks, debug overlay
    SDK/                 # Public mod API: registries, schemas, pack loaders, dependency resolver
    Schemas/             # Pack schemas and validators
    Registries/          # Unit/building/projectile/effect/AI registration systems
    Domains/
      Warfare/           # Warfare domain plugin (factions, doctrines, combat)
      Economy/           # Economy domain plugin
      Scenario/          # Scenario/campaign domain plugin
      UI/                # UI/UX domain plugin
    Tools/
      PackCompiler/      # CLI: validate, build, diff, package packs
      Inspector/         # In-game debug overlay and entity inspector
      DumpTools/         # Entity/component/prefab dump utilities
    Debug/               # Hot reload, logging, diagnostics
    Tests/               # xUnit test suite
  packs/                 # Content packs and example mods
  schemas/               # Canonical JSON/YAML schema definitions
  docs/                  # Documentation
  manifests/             # System contracts, ownership maps
```

---

## Quick Start: Create a Balance Mod

Create a simple mod that increases archer range.

### Step 1: Create Pack Directory

```bash
mkdir -p packs/my-first-mod/stats
```

### Step 2: Write pack.yaml

Create `packs/my-first-mod/pack.yaml`:

```yaml
id: my-first-mod
name: My First Mod
version: 0.1.0
framework_version: ">=0.1.0 <1.0.0"
author: Your Name
type: balance
description: Increases archer range by 25%

depends_on: []
conflicts_with: []

overrides:
  stats:
    - archer-range-buff
```

### Step 3: Add a Stat Override

Create `packs/my-first-mod/stats/archer-range-buff.yaml`:

```yaml
id: archer-range-buff
description: Boost archer range
targets:
  - match:
      tags: [ranged, archer]
    modifications:
      range:
        multiply: 1.25
```

### Step 4: Validate

```bash
dotnet run --project src/Tools/PackCompiler -- validate packs/my-first-mod
```

Fix any errors the validator reports.

### Step 5: Build and Deploy

```bash
dotnet run --project src/Tools/PackCompiler -- build packs/my-first-mod
```

Copy the built pack to `BepInEx/dinoforge_packs/my-first-mod/` in your game directory.

### Step 6: Test In-Game

1. Launch DINO
2. Press F10 to open the mod menu
3. Verify "My First Mod" appears and is loaded
4. Check that archers have increased range

---

## Next Steps

- [Mod Authoring Guide](MOD-AUTHORING.md) -- Full reference for all content types, pack types, and advanced topics
- [Creating Packs](/guide/creating-packs) -- Pack format details
- [Architecture](/concepts/architecture) -- Understand the layered design
