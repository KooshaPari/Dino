# Getting Started

This guide walks you through setting up DINOForge for development or mod authoring.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- [Node.js 20+](https://nodejs.org/) (for docs only)
- Diplomacy is Not an Option (Steam)
- Git

## Install BepInEx

DINOForge runs on a **modified BepInEx 5** build with Unity ECS support. Standard BepInEx from GitHub will not work.

1. Download **BepInEx 5 with Unity ECS Support** from [Nexus Mods](https://www.nexusmods.com/diplomacyisnotanoption/mods/1)
2. Extract into your DINO game directory (where the `.exe` lives)
3. Verify the folder structure:

```
GameRoot/
  winhttp.dll              # Modified Doorstop pre-loader
  doorstop_config.ini
  BepInEx/
    plugins/               # Standard plugins (empty for DINO)
    ecs_plugins/           # WHERE DINO MODS GO
    config/                # Configuration files
```

::: warning
Standard BepInEx does **not** work with DINO. The game uses full Unity ECS (DOTS) with Burst compilation, which requires a modified Doorstop pre-loader fork.
:::

4. Launch the game once to let BepInEx initialize, then close it.

## Clone and Build

```bash
git clone https://github.com/KooshaPari/Dino.git
cd Dino
```

Build the solution:

```bash
dotnet build src/DINOForge.sln
```

Run tests to verify everything works:

```bash
dotnet test src/DINOForge.sln --verbosity normal
```

## Project Structure

```
DINOForge/
  src/
    Runtime/       # BepInEx plugin — bootstrap, ECS detection, hooks
    SDK/           # Public mod API — registries, schemas, pack loaders
    Tools/
      PackCompiler/  # CLI: validate, build, package packs
      DumpTools/     # Entity/component dump utilities
    Tests/         # xUnit test suite
  packs/           # Content packs and example mods
  schemas/         # Canonical JSON/YAML schema definitions
  docs/            # This documentation site
```

## Load a Pack

Once built, the Runtime DLL goes into `BepInEx/ecs_plugins/`:

```
BepInEx/ecs_plugins/DINOForge.Runtime.dll
```

Content packs live in a `packs/` directory relative to the game root. The runtime discovers and loads them at boot.

## Next Steps

- [Quick Start](/guide/quick-start) — Create your first pack in 5 minutes
- [Creating Packs](/guide/creating-packs) — Full pack authoring guide
- [Architecture](/concepts/architecture) — Understand the layered design
