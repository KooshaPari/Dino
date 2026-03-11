# DINOForge Packs

Official example and reference packs for DINOForge.

## Directory Structure

This directory contains the official example packs bundled with DINOForge:

- **example-balance** - Simple balance pack demonstrating basic unit and faction definitions
- **warfare-modern** - Modern warfare theme with contemporary units and weapons
- **warfare-starwars** - Star Wars theme with Republic vs CIS factions and units
- **warfare-guerrilla** - Guerrilla warfare theme with asymmetric faction composition
- **economy-balanced** - Economy system demonstration with resource rates and trade routes
- **scenario-tutorial** - Scenario pack with tutorial, survival, and resource challenges

## Adding Community Packs

Community and third-party packs live at [github.com/KooshaPari/dinoforge-packs](https://github.com/KooshaPari/dinoforge-packs).

To add a pack from the community repository as a git submodule:

```bash
dinoforge pack add <repository-url>
```

For example:

```bash
dinoforge pack add https://github.com/KooshaPari/dinoforge-packs/tree/main/your-pack
```

This will:
1. Clone the pack repository into `packs/<pack-name>`
2. Register it as a git submodule in `.gitmodules`
3. Track the pack as part of your DINOForge installation

## Managing Pack Submodules

### List installed packs
```bash
dinoforge pack list
```

### Update all pack submodules
```bash
dinoforge pack update
```

### Generate packs.lock for reproducible builds
```bash
dinoforge pack lock
```

The `packs.lock` file captures the exact commit SHAs of all installed packs, ensuring consistent builds across environments.

## Pack Manifest Format

Each pack must contain a `pack.yaml` manifest. See the example packs for templates.

## Validation

Validate any pack directory with:

```bash
dinoforge validate <pack-directory>
```

## References

- [Pack Manifest Schema](../schemas/pack-manifest.schema.json)
- [DINOForge Documentation](../docs/)
- [Contributing Guide](../CONTRIBUTING.md)
