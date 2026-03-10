# CLI Reference

DINOForge includes command-line tools for pack validation, building, and diagnostics.

## PackCompiler

The primary CLI tool for working with packs.

### Validate

Check packs against schemas and dependency rules:

```bash
# Validate a single pack
dotnet run --project src/Tools/PackCompiler -- validate packs/my-pack

# Validate all packs in directory
dotnet run --project src/Tools/PackCompiler -- validate packs/
```

**What it checks:**
- Manifest schema conformance
- All YAML/JSON files match their respective schemas
- Required fields present with correct types
- ID format and uniqueness
- Semantic version format
- Dependency graph resolution (no cycles, no missing deps)
- Asset reference integrity
- ECS registration conflict detection
- Cross-pack compatibility

**Exit codes:**
| Code | Meaning |
|------|---------|
| 0 | All validations passed |
| 1 | Validation errors found |
| 2 | Fatal error (missing files, invalid arguments) |

### Build

Compile a validated pack into a distributable artifact:

```bash
dotnet run --project src/Tools/PackCompiler -- build packs/my-pack
```

**Build pipeline:**
1. Runs full validation (same as `validate`)
2. Resolves all cross-file references
3. Checks for missing assets
4. Checks for circular dependencies
5. Produces pack artifact
6. Emits compatibility metadata

### Assets

Manage pack assets:

```bash
# List assets referenced by a pack
dotnet run --project src/Tools/PackCompiler -- assets list packs/my-pack

# Check for missing asset references
dotnet run --project src/Tools/PackCompiler -- assets check packs/my-pack
```

## DumpTools

Offline analysis of entity/component dumps from the Runtime.

```bash
# Run the dump analyzer
dotnet run --project src/Tools/DumpTools
```

DumpTools uses Spectre.Console for interactive terminal UI. It can:
- Parse entity dumps from the Runtime's Entity Dumper
- Analyze component distributions
- Compare dumps across game versions
- Generate mapping tables for the SDK

## Build Commands

Standard .NET build and test commands:

```bash
# Build everything
dotnet build src/DINOForge.sln

# Run tests
dotnet test src/DINOForge.sln --verbosity normal

# Check formatting
dotnet format src/DINOForge.sln --verify-no-changes

# Fix formatting
dotnet format src/DINOForge.sln
```
