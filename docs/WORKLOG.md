# DINOForge Worklog

## 2026-03-09 - Project Inception

### Research Phase
- Analyzed 3 ChatGPT planning conversations covering:
  - Star Wars modding framework / agentic production pipeline design
  - Best modding DX/UX reference games (Factorio, RimWorld, Satisfactory, Minecraft Bedrock, UEFN)
  - Futuristic warfare total conversion architecture (5-layer architecture, faction archetypes, unit role matrix)
- Researched kooshapari GitHub projects for engineering conventions:
  - CLAUDE.md governance, CHANGELOG format, polyrepo-hexagonal architecture
  - BDD/SDD/TDD testing philosophy, spec-driven development
  - 17-mode SPARC framework, memory-centric agent coordination
- Researched DINO game modding landscape:
  - Unity ECS game with Burst compilation
  - BepInEx loader via modified `ecs_plugins` path
  - Harmony performance warning (halves framerate)
  - Small Nexus Mods community (~handful of mods)
  - No official mod API documentation
  - Steam Community guide (id:3348001330) as primary modding reference

### Deliverables Created
- [x] Git repository initialized
- [x] CLAUDE.md - agent governance and project overview
- [x] docs/PRD.md - full product requirements document
- [x] ADR-001 through ADR-006 - core architectural decisions
- [x] CHANGELOG.md
- [x] docs/WORKLOG.md (this file)
- [x] docs/warfare/ - warfare domain specification
- [x] docs/reference/ - modding DX/UX reference analysis
- [x] schemas/ - initial pack manifest and faction schemas

### Key Decisions
1. Product name: **DINOForge**
2. Three-product architecture: Runtime -> SDK -> Packs
3. Warfare as first domain plugin (not hardcoded)
4. Declarative-first: YAML/JSON manifests over C# patches
5. Agent-first repo design with legal move classes
6. ECS-native modding preferred over Harmony
7. Pack system with explicit manifests, deps, conflicts
8. 3 faction archetypes (Order, Industrial Swarm, Asymmetric) across 5 factions
9. Build order: Modern warfare first, Star Wars second, Guerrilla third

### Open Questions
- [ ] Exact DINO Unity version and Mono vs IL2CPP status
- [ ] Component dump format and ECS system discovery approach
- [ ] Asset replacement feasibility (models, textures, audio)
- [ ] Steam Workshop integration potential
- [ ] BepInEx version compatibility matrix for DINO
