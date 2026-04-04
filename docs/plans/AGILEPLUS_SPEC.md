# AgilePlus Methodology — DINOForge Implementation Spec

**Status:** Active
**Owner:** All DINOForge agents
**Related Docs:** `docs/plans/KILO_GASTOWN_SPEC.md`, `CLAUDE.md`, `AGENTS.md`

---

## Overview

**AgilePlus** (`kooshapari/agileplus`) is the spec-driven development engine powering DINOForge project management. It provides user story tracking, sprint management, spec management, and roadmap visualization for the DINOForge ecosystem.

AgilePlus is not a general Agile framework — it is a **spec-first project management system** where all work originates from written specifications. In the DINOForge rig, AgilePlus coordinates the multi-agent bead pool, convoy feature branches, and cross-repo methodology propagation.

---

## Core Concepts

### Specs as Primary Artifacts

A **spec** (specification) is the authoritative source of truth for any feature or task. Every piece of work in DINOForge begins with a spec document that defines:

- **What** is being built (clear description)
- **Why** it exists (problem or requirement it solves)
- **How** it maps to AgilePlus stories
- **Acceptance criteria** that define done-ness

Specs live in `docs/specs/` and are written before any code. They are the contract between the idea and its implementation.

### User Stories

An **AgilePlus user story** captures work from a user or agent perspective. Stories are derived from specs and have:

- A story ID (e.g., `AP-042`)
- Title and description
- Effort estimate
- Sprint assignment
- Status: `backlog` | `in_sprint` | `in_progress` | `done`

In the DINOForge context, stories typically map to:
- New pack features (unit types, buildings, factions)
- SDK/registry extensions
- Tooling improvements (CLI, MCP server)
- Documentation deliverables

### Sprints

A **sprint** is a time-boxed iteration (default: 1 week). Each sprint has:

- Start and end dates
- Goal statement
- Assigned stories
- Velocity metric

AgilePlus dashboards show sprint progress via burndown charts and story completion rates.

### Backlog

The **backlog** is the ordered queue of all user stories not yet in a sprint. The backlog is continuously refined:

1. New specs are written and reviewed
2. Specs are broken into user stories
3. Stories are estimated and prioritized
4. Stories are pulled into upcoming sprints

---

## AgilePlus in the DINOForge Rig

### Integration Architecture

```
AgilePlus (kooshapari/agileplus)
  └─ Dashboard: http://localhost:3000 (default)
  └─ Specs:     docs/specs/
  └─ Stories:   tracked in AgilePlus backend
  └─ Sprints:   managed via AgilePlus UI
```

Launch AgilePlus:
```bash
cd C:\Users\koosh\agileplus && bun run dev
# or
cd C:\Users\koosh\agileplus && npm run dev
```

### Spec-to-Story Mapping

Every spec in `docs/specs/` maps to one or more AgilePlus user stories:

| Spec Location | AgilePlus Story Type | Example |
|---|---|---|
| `docs/plans/*.md` | Planning / Methodology | KILO_GASTOWN_SPEC.md → AP-Methodology-001 |
| `docs/specs/<feature>.md` | Feature | warfare-starwars → AP-Pack-042 |
| `docs/adr/*.md` | Architecture Decision | ADR-012 → AP-Tool-011 |
| `docs/research/*.md` | Research | phase-2c-cis-sourcing → AP-Content-038 |

The mapping is maintained by **docs-curator** and reviewed during sprint planning.

### Sprint Cycle

1. **Sprint Planning** (Monday)
   - Review backlog
   - Pull highest-priority stories into sprint
   - Assign stories to agent roles

2. **Daily Execution**
   - Agents pick up beads via `gt_prime`
   - Work is tracked via bead status updates
   - `gt_status` provides dashboard visibility

3. **Sprint Review** (Friday)
   - All completed stories demonstrated
   - Proof artifacts reviewed (screenshots, game videos)
   - Metrics recorded (velocity, cycle time)

4. **Retrospective**
   - What worked / what didn't
   - Process improvements captured as new specs

---

## Spec Document Conventions

All specs in `docs/specs/` follow this template:

```markdown
# <Feature Name>

**Status:** draft | in_review | active | completed
**Story:** AP-XXX
**Sprint:** <sprint_id>
**Owner:** <agent_role>
**Created:** <YYYY-MM-DD>
**Updated:** <YYYY-MM-DD>

## Summary
One-paragraph overview of what this spec covers.

## Motivation
Why this feature is needed. What problem does it solve?

## Specification

### <Sub-feature 1>
Description + acceptance criteria.

### <Sub-feature 2>
Description + acceptance criteria.

## Acceptance Criteria
- [ ] Criterion 1
- [ ] Criterion 2

## Dependencies
- Other specs or packs this depends on
- Any blocking issues

## Anti-Goals
What this spec does NOT cover (explicit scope boundary).
```

### Spec Naming

- Files: `kebab-case.md` (e.g., `wave-spawning-system.md`)
- Status in frontmatter YAML header
- Always include Story ID once assigned

### Spec Locations

| Spec Type | Location |
|---|---|
| Feature specs | `docs/specs/<feature-name>.md` |
| Plan/procedure specs | `docs/plans/<name>.md` |
| Architecture decisions | `docs/adr/ADR-XXX-<name>.md` |
| Research logs | `docs/research/<name>.md` |
| Sprint plans | `docs/plans/sprint-<YYYY>-<WW>.md` |

---

## AgilePlus + Kilo Gastown Coordination

AgilePlus and Kilo Gastown operate together:

| Concern | Tool |
|---|---|
| What to build | AgilePlus stories + specs |
| Who builds it | Kilo Gastown bead assignment |
| When it's done | Bead status → AgilePlus story close |
| Where work lives | Worktree branch per agent |
| How to track | `gt_status` dashboard, AgilePlus burndown |

### Bead-to-Story Workflow

```
AgilePlus Story (AP-042)
  └─ Assigned to: pack-builder
  └─ Sprint: 2026-W14
  └─ Creates bead: <bead_id> in Kilo Gastown
  └─ Agent picks up bead via gt_prime
  └─ Work done, gt_done called
  └─ Story marked: done in AgilePlus
```

### Convoy + Sprint Alignment

Convoys (feature branches) are sized to fit within a sprint:

| Convoy | Sprint | Scope |
|---|---|---|
| `convoy/agileplus-kilo-specs-dino/381d5195/head` | 2026-W14 | Methodology docs |
| `convoy/methodology-dino/c61d464c/head` | 2026-W13 | GEMINI.md propagation |

When a convoy lands, its stories are all closed in AgilePlus.

---

## Quality Gates

All spec-driven work in AgilePlus follows the DINOForge pre-submission gates:

```bash
# Build
dotnet build src/DINOForge.sln

# Test
dotnet test src/DINOForge.sln --verbosity normal

# Format
dotnet format src/DINOForge.sln --verify-no-changes

# Pack validation (for content packs)
dotnet run --project src/Tools/PackCompiler -- validate packs/
```

Stories are only marked **done** when all relevant gates pass.

---

## AgilePlus Artifacts

AgilePlus generates these artifacts during the sprint cycle:

| Artifact | Description | Location |
|---|---|---|
| Sprint Burndown | Story points completed per day | AgilePlus dashboard |
| Velocity Chart | Average points per sprint | AgilePlus dashboard |
| Story Report | All stories with status | AgilePlus export |
| Spec Coverage | Spec-to-story mapping table | `docs/specs/INDEX.md` |
| Sprint Retro Notes | What went well / improve | `docs/plans/sprint-<id>-retro.md` |

---

## Anti-Patterns

These patterns are prohibited in AgilePlus-driven work:

| Pattern | Reason | Correct Approach |
|---|---|---|
| Code without a spec | No acceptance criteria | Write spec first |
| Spec drift | Implementation diverges from spec | Re-review spec before committing |
| Story scope creep | Sprint goal expands mid-sprint | Create new story for new scope |
| Untracked beads | Work not in AgilePlus | Always create bead from story |
| Skip quality gates | Technical debt | All gates pass before marking done |

---

## References

- [AgilePlus Repo](file:///C:/Users/koosh/agileplus) — Internal PM tool
- [`docs/plans/KILO_GASTOWN_SPEC.md`](docs/plans/KILO_GASTOWN_SPEC.md) — Kilo Gastown rig methodology
- [`CLAUDE.md`](CLAUDE.md) — AgilePlus PM Dashboard integration
- [`AGENTS.md`](AGENTS.md) — Agent roster and coordination
- [`docs/adr/ADR-001-agent-driven-development.md`](docs/adr/ADR-001-agent-driven-development.md) — Agent-driven development principles

---

**Spec Owner:** All agents
**Last Updated:** 2026-04-04
**Used By:** All DINOForge polecat agents
