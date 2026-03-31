# GEMINI.md - Development Guidelines for DINOForge

## Project Overview

DINOForge is a general-purpose mod platform and agent-oriented development scaffold for **Diplomacy is Not an Option (DINO)**. It is a **mod operating system**, not a single mod.

- **Game**: Diplomacy is Not an Option (Unity ECS, BepInEx-compatible)
- **Architecture**: Polyrepo-hexagonal, declarative-first, agent-driven
- **Language**: C# (.NET), YAML/JSON schemas, CLI tooling
- **Mod Loader**: BepInEx + custom ECS plugin loader (`BepInEx/ecs_plugins/`)

## Key Files

- `CLAUDE.md` - Governance, build commands, architecture, and design principles
- `AGENTS.md` - Agent roster, domain ownership, and collaboration rules
- `src/Runtime/` - BepInEx plugin: bootstrap, ECS detection, debug overlay
- `src/SDK/` - Public mod API: registries, schemas, pack loaders
- `src/Tools/` - CLI tools, PackCompiler, MCP server
- `packs/` - Content packs (warfare-starwars, warfare-modern, etc.)

## Development Commands

```bash
# Build
dotnet build src/DINOForge.sln

# Test
dotnet test src/DINOForge.sln --verbosity normal

# Lint
dotnet format src/DINOForge.sln --verify-no-changes

# Validate packs
dotnet run --project src/Tools/PackCompiler -- validate packs/

# Package a mod pack
dotnet run --project src/Tools/PackCompiler -- build packs/<pack-name>
```

## Architecture Principles

- **Registry Pattern**: All extensible content uses registries — no switch statements on content type IDs
- **Declarative First**: YAML/JSON manifests over C# patches
- **Wrap Don't Handroll**: Use established libraries; prefer thin wrappers over custom implementations
- **Framework Before Content**: Platform first, themed mods second
- **Schema-Driven**: JSON schemas are source-of-truth for all data shapes

## Agent Behavior Guidelines

When working in this repository as a Gemini agent:

1. **GUPP Principle**: Work is on your hook — execute immediately
2. **Commit Frequently**: Push after every meaningful unit of work
3. **Checkpoint**: Call `gt_checkpoint` after significant milestones
4. **No Destructive Ops**: Never force push, hard reset, or merge to main
5. **Pre-Submission Gates**: Run `dotnet test` and `dotnet format --verify-no-changes` before considering work complete

## Phenotype Org Rules

- UTF-8 encoding only in all text files
- Worktree discipline: canonical repo stays on `main`
- CI completeness: fix all CI failures before merging
- Never commit agent directories (`.gemini/`, `.claude/`, `.codex/`, `.cursor/`)
- All tests must pass before any commit to main
- Every public API needs XML doc comments (triple-slash `///`)

## Communication

- Check mail periodically with `gt_mail_check`
- Use `gt_mail_send` for coordination with other agents
- Keep messages concise and actionable
