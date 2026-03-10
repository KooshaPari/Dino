# Contributing to DINOForge

DINOForge is an agent-driven mod platform for Diplomacy is Not an Option. Contributions are welcome.

## Prerequisites

- .NET 8.0 SDK
- Git
- (Optional) pre-commit for local hooks

## Building

```bash
# Build everything (except Runtime, which needs game DLLs)
dotnet build src/DINOForge.CI.sln

# Build the full solution (requires DINO game installed)
dotnet build src/DINOForge.sln
```

## Testing

```bash
dotnet test src/DINOForge.CI.sln --verbosity normal
```

All tests must pass before submitting a PR.

## Formatting

```bash
# Check formatting
dotnet format src/DINOForge.CI.sln --verify-no-changes

# Auto-fix formatting
dotnet format src/DINOForge.CI.sln
```

## Creating a Content Pack

1. Create a directory under `packs/` with your pack name.
2. Add a `pack.yaml` manifest:
   ```yaml
   id: my-pack
   name: My Pack
   version: 0.1.0
   framework_version: ">=0.1.0 <1.0.0"
   author: Your Name
   type: content
   depends_on: []
   conflicts_with: []
   loads:
     factions: []
     units: []
     buildings: []
   ```
3. Add content files under `factions/`, `units/`, `buildings/` subdirectories.
4. Validate your pack:
   ```bash
   dotnet run --project src/Tools/PackCompiler -- validate packs/my-pack
   ```

## Pull Request Process

1. Create a feature branch from `main`.
2. Make your changes following the code style in `CLAUDE.md`.
3. Run tests and formatting checks.
4. Submit a PR using the provided template.
5. Ensure CI passes (build, test, lint, pack validation).

## Code Style

- C# 12+ with nullable reference types enabled
- XML doc comments on all public APIs
- Immutable data models preferred
- Registry pattern for all extensible domains
- See `CLAUDE.md` for full style guide

## Architecture Rules

- **Wrap, don't handroll** -- always prefer existing libraries over custom implementations.
- **Declarative before imperative** -- YAML/JSON manifests over C# patches.
- **Framework before content** -- platform stability before themed mods.
- All work should map to a legal move class (see `CLAUDE.md`).

## Setting Up Pre-commit Hooks

```bash
pip install pre-commit
pre-commit install
```
