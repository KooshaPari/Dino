# DINOForge Agents

Agent definitions for the DINOForge mod platform.

## Agents

### runtime-specialist
- **Domain**: `src/Runtime/`, ECS Bridge, BepInEx integration
- **Skills**: Unity ECS, BepInEx, Harmony, C# netstandard2.0
- **Owns**: Plugin.cs, DumpSystem, EntityDumper, SystemEnumerator, DebugOverlay, Bridge/*

### sdk-architect
- **Domain**: `src/SDK/`, registries, schemas, content pipeline
- **Skills**: C# design patterns, schema validation, YAML/JSON, NuGet packaging
- **Owns**: Registry/*, Models/*, Validation/*, ContentLoader, PackManifest, Assets/*

### warfare-designer
- **Domain**: `src/Domains/Warfare/`, faction balance, combat model
- **Skills**: Game design, balance math, domain modeling
- **Owns**: Archetypes/*, Doctrines/*, Roles/*, Waves/*, Balance/*

### pack-builder
- **Domain**: `packs/`, content authoring, schema compliance
- **Skills**: YAML authoring, game design, faction theming
- **Owns**: packs/*, schemas/*

### toolsmith
- **Domain**: `src/Tools/`, CLI tooling, dev experience
- **Skills**: CLI design, Spectre.Console, System.CommandLine
- **Owns**: PackCompiler/*, DumpTools/*

### qa-engineer
- **Domain**: `src/Tests/`, quality assurance, CI/CD
- **Skills**: xUnit, FluentAssertions, test design, CI workflows
- **Owns**: Tests/*, .github/workflows/*

### docs-curator
- **Domain**: `docs/`, documentation site, ADRs
- **Skills**: VitePress, Mermaid, technical writing
- **Owns**: docs/*, CHANGELOG.md, README.md
