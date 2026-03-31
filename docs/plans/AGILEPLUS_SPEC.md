# AgilePlus Methodology — DINOForge Implementation

**Status:** Active
**Spec ID:** `spec-agileplus-dino-001`
**Version:** 1.0.0
**Owner:** DINOForge Agent Orchestration

---

## Overview

**AgilePlus** (`kooshapari/agileplus`) is the spec-driven development engine used for DINOForge project management. It is a Next.js application providing user stories, sprint tracking, spec management, and roadmap visualization for the DINOForge ecosystem.

AgilePlus is not a general-purpose PM tool — it is tightly integrated with the DINOForge codebase. Specs defined in `docs/specs/` map directly to AgilePlus stories, and vice versa. Changes to one propagate to the other through manual agent synchronization.

---

## Architecture

### System Topology

```
agileplus (PM tool)          DINOForge (code repo)
────────────────────          ────────────────────────
Next.js app                   Git repo
Port 3000 (dev)               docs/specs/ (specs)
Supabase (data)               packs/ (content)
Story editor                  C# source
Sprint board                  YAML manifests
Roadmap view                  JSON schemas
```

### AgilePlus Components

| Component | Purpose |
|-----------|---------|
| Story Editor | Create/edit user stories with acceptance criteria |
| Sprint Board | Kanban board (Backlog → In Progress → Done) |
| Roadmap View | Quarterly roadmap with milestone tracking |
| Spec Manager | Link specs to stories, track spec status |
| Integration Panel | Webhook/API triggers for CI/CD integration |

---

## Integration with DINOForge

### Spec → Story Mapping

Every spec in `docs/specs/` corresponds to one or more AgilePlus stories. The mapping is stored in the spec frontmatter:

```yaml
---
agileplus:
  story_id: "AP-123"
  sprint: "S9"
  status: "in_progress"
---
```

### Story → Code Mapping

AgilePlus stories are implemented as beads (work items) in the Gastown orchestration system. Each bead has a `source_story_id` metadata field linking it to the originating AgilePlus story.

```json
{
  "type": "issue",
  "title": "Implement wave scripting system",
  "metadata": {
    "source_story_id": "AP-123",
    "source_spec": "docs/specs/SPEC-009-wave-scripting.md"
  }
}
```

### The Convoy Pattern

AgilePlus manages work through **convoys** — collections of related stories shipped together. In the DINOForge ecosystem:

- A **convoy** is a Git feature branch containing multiple related beads
- Convoys are created in AgilePlus and materialized as feature branches
- The convoy ID is used as the Git branch prefix: `convoy/<convoy-id>/<agent>/<bead-id>`

Example:
```
convoy/agileplus-kilo-specs-dino/381d5195/head
```

This pattern allows:
- Multiple agents working in parallel on the same logical feature set
- Atomic commits per bead within the convoy
- Sprint delivery = convoy merge to main

---

## Sprint Workflow

### Sprint Ceremonies (Agent-Automated)

| Ceremony | Frequency | Agents Involved |
|----------|-----------|-----------------|
| Sprint Planning | Bi-weekly | orchestrator |
| Daily Standup | Daily | all polecats |
| Sprint Review | Bi-weekly | all agents + refinery |
| Retrospective | Bi-weekly | orchestrator |

### Sprint States

```
BACKLOG → SPRINT_NEXT → IN_PROGRESS → IN_REVIEW → DONE
   │            │              │            │         │
   │            │              │            │         └── Convoy merged to main
   │            │              │            └── Refinery reviewing
   │            │              └── Agent actively working
   │            └── Assigned to sprint, not yet started
   └── Not yet assigned to any sprint
```

### Story Point Estimation

Stories are estimated using a modified Fibonacci scale:

| Points | Complexity | Examples |
|--------|------------|----------|
| 1 | Trivial change | Documentation fix, typo |
| 2 | Simple change | Add validation rule, new test fixture |
| 3 | Medium change | New schema, new registry entry |
| 5 | Large change | New domain plugin, new MCP tool |
| 8 | Epic | New pack type, major API redesign |
| 13 | Initiate epic | Multi-convoys, total conversion |

---

## Spec-Driven Development

### Spec Lifecycle

```
DRAFT → REVIEW → APPROVED → ACTIVE → DEPRECATED
  │         │         │         │         │
  │         │         │         │         └── Superseded by newer spec
  │         │         │         └── Being implemented by agents
  │         │         └── Approved for sprint assignment
  │         └── Agent review in progress
  └── Initial draft
```

### Spec Categories in DINOForge

| Category | Path | Examples |
|----------|------|---------|
| Architecture | `docs/specs/ARCH-*.md` | ADR-001, ADR-002 |
| Feature | `docs/specs/FEAT-*.md` | SPEC-003 prove-features |
| Integration | `docs/specs/INT-*.md` | MCP bridge specs |
| Domain | `docs/specs/DOM-*.md` | Warfare domain specs |

### Spec Document Template

```markdown
# Spec: [Title]

**Spec ID:** `spec-xxx`
**Status:** [DRAFT|REVIEW|APPROVED|ACTIVE|DEPRECATED]
**Sprint:** [S#]
**Story:** [AP-###]

## Problem Statement

## Solution

## Acceptance Criteria

- [ ] Criterion 1
- [ ] Criterion 2

## Technical Notes

## Related Specs

## Changelog
```

---

## AgilePlus + Kilo Gastown Integration

The DINOForge ecosystem uses two parallel coordination systems:

1. **AgilePlus** — Human-facing PM (sprints, stories, roadmap)
2. **Kilo Gastown** — Agent-facing orchestration (beads, convoys, gt tools)

### Handoff Protocol

When an AgilePlus story is ready for implementation:

1. **Story Created** → AgilePlus story appears in sprint board
2. **Bead Spawned** → Orchestrator creates a bead linked to the story
3. **Convoy Created** → Bead assigned to a convoy (feature branch)
4. **Polecat Hooked** → Agent picks up the bead via `gt_prime`
5. **Implementation** → Agent implements spec, commits to convoy branch
6. **Review** → Refinery reviews, requests changes or merges
7. **Merge** → Convoy merged to main, story marked DONE in AgilePlus

### Metadata Synchronization

| AgilePlus Field | Kilo Gastown Field |
|-----------------|-------------------|
| Story ID | `source_story_id` in bead metadata |
| Sprint | `sprint` in bead metadata |
| Status (story) | Bead status (in_progress → in_review → closed) |
| Assignee | `assignee_agent_bead_id` |

---

## Running AgilePlus

### Local Development

```bash
cd C:\Users\koosh\agileplus
bun run dev
# Opens at http://localhost:3000
```

### Production Deployment

```bash
cd C:\Users\koosh\agileplus
npm run build
npm run start
```

### Environment Variables

| Variable | Purpose |
|----------|---------|
| `NEXT_PUBLIC_SUPABASE_URL` | Supabase project URL |
| `NEXT_PUBLIC_SUPABASE_ANON_KEY` | Supabase anon key |
| `SUPABASE_SERVICE_ROLE_KEY` | Supabase service role (server-side) |

---

## Agent Workflow with AgilePlus

### Starting a New Feature

1. Create spec in `docs/specs/SPEC-XXX-feature-name.md`
2. Add AgilePlus frontmatter (`story_id`, `sprint`, `status: REVIEW`)
3. Submit for review (story created in AgilePlus)
4. After approval, update `status: APPROVED`
5. When sprint starts, update `status: ACTIVE`
6. Spawn bead linked to `story_id`
7. Implement and commit per AGENTS.md protocol

### Completing a Story

1. All acceptance criteria verified (tests pass, docs updated)
2. Convoy merged to main
3. Bead closed via `gt_bead_close`
4. Story status updated to DONE in AgilePlus
5. Spec status updated to `DEPRECATED` if superseded

### Sprint Burndown

Agents track velocity through bead closure rate:

```
Sprint Velocity = Stories Completed / Total Stories in Sprint
Ideal Burndown = Linear from sprint start to end
Actual Burndown = Plotted from daily bead closures
```

---

## Anti-Patterns

| Anti-Pattern | Why | Correct Approach |
|-------------|-----|-----------------|
| Creating beads without spec | No acceptance criteria | Spec-first, then bead |
| Implementing outside convoy | No atomic commits | Always commit to convoy branch |
| Skipping `dotnet test` | Breaks invariants | Always test before commit |
| Hardcoding content IDs | Breaks registry pattern | Use registry lookup |
| Bypassing validators | Schema drift | Always validate |
| Committing without owner annotation | Ownership confusion | Include "Owned by: role" |

---

## See Also

- [AGENTS.md](../AGENTS.md) — Agent collaboration guide
- [CLAUDE.md](../CLAUDE.md) — Project governance and build commands
- [KILO_GASTOWN_SPEC.md](./KILO_GASTOWN_SPEC.md) — Kilo Gastown methodology
- [ADR-001: Agent-Driven Development](../adr/ADR-001-agent-driven-development.md)
