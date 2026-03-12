# Getting Started

This guide walks you through setting up DINOForge for development or mod authoring.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download) or later
- [Node.js 20+](https://nodejs.org/) (for docs only)
- Diplomacy is Not an Option (Steam)
- Git

## Install BepInEx and DINOForge

DINOForge runs on a **modified BepInEx 5** build with Unity ECS support. Standard BepInEx from GitHub will not work.

### Option A: Automated Installer (Recommended for Users)

DINOForge includes a GUI installer that handles BepInEx and DINOForge setup automatically.

1. Download the latest DINOForge installer from [Releases](https://github.com/KooshaPari/Dino/releases)
2. Run `DINOForge.Installer.exe`
3. Select your DINO game installation directory
4. The installer will:
   - Download and extract the modified BepInEx 5
   - Install DINOForge Runtime and example packs
   - Verify the installation
5. Launch the game and verify mods load (F10 to open mod menu)

### Option B: Manual Installation (For Developers)

If you prefer manual setup or the installer doesn't work for you:

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
5. Copy the built `DINOForge.Runtime.dll` to `BepInEx/ecs_plugins/`

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
    Runtime/           # BepInEx plugin — bootstrap, ECS detection, hooks
      Bridge/          #   ECS bridge: component mapping, stat modifiers
      HotReload/       #   Hot reload bridge
      UI/              #   Mod menu (F10), settings panel
    SDK/               # Public mod API — registries, schemas, pack loaders
      Assets/          #   Asset service, addressables catalog
      Dependencies/    #   Dependency resolver
      HotReload/       #   Pack file watcher
      Models/          #   Content data models
      Registry/        #   Generic registry with conflict detection
      Universe/        #   Universe Bible system
      Validation/      #   Schema validation (NJsonSchema)
    Bridge/
      Protocol/        #   JSON-RPC message types, IGameBridge
      Client/          #   Out-of-process game client
    Domains/
      Warfare/         #   Factions, doctrines, combat, waves
      Economy/         #   Rates, trade, balance
      Scenario/        #   Scripting, conditions
      UI/              #   HUD injection, menus
    Tools/
      Cli/             #   dinoforge CLI
      McpServer/       #   MCP server for Claude Code
      PackCompiler/    #   Pack compiler (validate, build)
      DumpTools/       #   Entity dump analysis (Spectre.Console)
      Installer/       #   BepInEx + DINOForge installer
    Tests/             # Unit tests (xUnit + FluentAssertions)
      Integration/     # Integration tests
  packs/               # Content packs (6 example packs)
  schemas/             # JSON/YAML schema definitions (17 schemas)
  docs/                # This documentation site
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
