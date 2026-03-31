# Kilo Gastown Methodology Spec

## Overview

This document describes the **Kilo Gastown** multi-agent orchestration methodology as implemented in the DINOForge rig (ID: `6c6d4555-91e8-4f06-a974-018cf3e766d2`) within town `78a8d430-a206-4a25-96c0-5cd9f5caf984`.

Kilo Gastown provides structured patterns for coordinating multiple agents working in parallel on related tasks (convoys), delegating work between agents (gt_sling), and tracking overall progress.

---

## Core Concepts

### Towns and Rigs

- **Town**: A collection of rigs sharing the same LLM backend and orchestration infrastructure.
- **Rig**: A single-agent worktree with a dedicated bead queue and git branch.

### Beads

**Beads** are the fundamental unit of work in Kilo Gastown.

| Type | Purpose |
|------|---------|
| `issue` | A standalone task to be completed by one agent |
| `convoy` | A coordinated group of related beads traveling together on a feature branch |
| `merge_request` | A review request for a completed bead or convoy |

#### Bead Lifecycle

```
open → in_progress → in_review → closed
                      ↓
                   rework (reopened to in_progress)
```

1. **open**: Task available for agent pickup
2. **in_progress**: Agent actively working the bead
3. **in_review**: Work submitted, awaiting review
4. **rework**: Reviewer requested changes, bead returned to agent
5. **closed**: Work accepted and merged

---

## Convoys

**Convoys** group related beads that must land together on a shared feature branch.

### Structure

```
convoy/
├── feature-branch-name/   # e.g., convoy/agileplus-kilo-specs-dino/381d5195/head
│   ├── bead-1 (issue)
│   ├── bead-2 (issue)
│   └── bead-N (issue)
```

### Creating a Convoy

1. TownDO creates a convoy bead (type: `convoy`)
2. TownDO creates child issue beads attached to the convoy
3. Agents checkout the convoy's feature branch
4. All beads in the convoy are worked in parallel
5. All beads must pass review before the convoy can land

### Dino Convoys (Active Examples)

| Convoy ID | Feature Branch | Status |
|-----------|----------------|--------|
| `381d5195-27f8-4843-9efe-62f11234815e` | `convoy/agileplus-kilo-specs-dino/381d5195/head` | Open |
| `c61d464c-2332-489e-becb-ebc5d1efa639` | `convoy/methodology-dino/c61d464c/head` | Open |

---

## Work Delegation: gt_sling

**gt_sling** is the mechanism for delegating a bead from one agent to another within the same rig.

### gt_sling (Single Bead)

```bash
gt_sling <bead_id> <target_agent_id>
```

Transfers a specific bead to another agent's queue. The bead retains its position and priority.

### gt_sling_batch (Multiple Beads)

```bash
gt_sling_batch <bead_id_1,bead_id_2,...,bead_id_N> <target_agent_id>
```

Delegates multiple beads atomically. All beads transfer together or none do.

### Delegation Rules

1. Only transfer beads you own or that are unassigned
2. Include a clear handoff message explaining context
3. The receiving agent inherits the full bead history
4. Original assignee is preserved in bead metadata for audit

---

## Merge Modes

When a convoy lands, the TownDO selects a merge mode:

| Mode | Strategy |
|------|----------|
| `squash` | All convoy commits squash into one (clean history, no bisect) |
| `rebase` | Commits replayed on target (preserves history, requires clean commits) |
| `merge` | True merge commit (preserves all intermediate commits) |

For methodology documentation (this convoy), **squash** is preferred to keep the feature branch history clean.

---

## Progress Tracking: gt_list_convoys

**gt_list_convoys** displays all active convoys in the rig with their status.

```bash
gt_list_convoys [--format json]
```

Returns:
- Convoy ID and title
- Feature branch name
- Number of beads in convoy
- Aggregate status (blocked if any bead blocked)
- `ready_to_land` flag when all beads merged

### Progress Checkpoints

Agents should call `gt_checkpoint` after:
- Completing a bead (passing tests)
- Opening a merge_request
- Resolving rework feedback
- Landing a convoy

---

## Kilo Gastown Tools Reference

| Tool | Purpose |
|------|---------|
| `gt_prime` | Get full context: identity, hooked bead, mail, open beads |
| `gt_sling` | Delegate single bead to another agent |
| `gt_sling_batch` | Delegate multiple beads atomically |
| `gt_done` | Complete hooked bead, push branch, transition to in_review |
| `gt_bead_close` | Close a bead (mark complete) |
| `gt_bead_status` | Inspect current state of any bead |
| `gt_mail_send` | Send typed message to another agent |
| `gt_mail_check` | Read undelivered mail |
| `gt_escalate` | Create escalation bead for stuck/blocked work |
| `gt_checkpoint` | Write crash-recovery state |
| `gt_status` | Emit plain-language dashboard status |
| `gt_nudge` | Real-time poke to another agent |
| `gt_mol_current` | Get current molecule step info |
| `gt_mol_advance` | Advance to next molecule step |

---

## DINOForge Integration

### Agent Roster (This Rig)

| Role | Domain | File Ownership |
|------|--------|----------------|
| runtime-specialist | ECS bridge, BepInEx | src/Runtime/ |
| sdk-architect | Registry, SDK, schemas | src/SDK/ |
| warfare-designer | Warfare domain, balance | src/Domains/Warfare/ |
| pack-builder | Content packs, YAML | packs/ |
| toolsmith | CLI tools, PackCompiler | src/Tools/ |
| qa-engineer | Tests, CI/CD | src/Tests/, .github/ |
| docs-curator | Documentation, VitePress | docs/, CHANGELOG.md |

### Decision Tree: Where Changes Live

```
"Where does this change live?"
├─ Game engine glue → src/Runtime/ (runtime-specialist)
├─ Data model or registry → src/SDK/ (sdk-architect)
├─ Domain logic → src/Domains/<Domain>/ (domain specialist)
├─ Pack content → packs/ (pack-builder)
├─ CLI / tooling / MCP → src/Tools/ (toolsmith)
├─ Tests → src/Tests/ (qa-engineer)
└─ Documentation → docs/ or CHANGELOG.md (docs-curator)
```

### Pre-Submission Gates

Before calling `gt_done`, run:

```bash
dotnet test src/DINOForge.sln
dotnet format src/DINOForge.sln --verify-no-changes
```

All tests must pass. Format must be clean.

### Commit Conventions

Format: `<type>(<scope>): <description>`

Examples:
- `docs(plans): add KILO_GASTOWN_SPEC.md`
- `feat(warfare): add wave scripting system`
- `fix(runtime): resolve hot reload race condition`

Reference bead ID in commit message when applicable.

---

## Workflow Example: Methodology Convoy

```
1. TownDO creates convoy "Methodology: Dino"
   → bead: c61d464c-2332-489e-becb-ebc5d1efa639

2. TownDO creates child issue beads:
   → Add AGENTS.md with Kilo Gastown mechanics (f8c1b3ea)
   → Add CLAUDE.md methodology guide (5545b9ab)
   → Add GEMINI.md methodology guide (ab1d6f33)
   → Add KILO_GASTOWN_SPEC.md (70a19eec)

3. Multiple agents checkout convoy feature branch:
   → git fetch origin
   → git checkout convoy/methodology-dino/c61d464c/head

4. Agents work beads in parallel via gt_sling

5. Each agent calls gt_done when bead complete

6. When all beads in_review, convoy ready_to_land

7. TownDO reviews and lands convoy (squash merge)
```

---

## Emergency Protocols

### Stuck Agent

If an agent has not made progress for 3 dispatch attempts:
1. TownDO calls `gt_nudge` to wake agent
2. If no response, TownDO calls `gt_sling` to reassign beads
3. Beads return to open state for new agent pickup

### Blocked Bead

If a bead is blocked (waiting on external dependency):
1. Agent calls `gt_escalate` with blocked reason
2. TownDO either unblocks or defers bead
3. Deferred beads marked with `blocked` label

### Rework Loop

If a bead cycles back for rework >2 times:
1. TownDO reviews the pattern
2. May split the bead into smaller units
3. Or escalate to human review

---

## Reference

- **Town ID**: `78a8d430-a206-4a25-96c0-5cd9f5caf984`
- **Rig ID**: `6c6d4555-91e8-4f06-a974-018cf3e766d2`
- **Stack**: DINO game mod, C#/.NET, .NET 11 preview
- **Build**: `dotnet build src/DINOForge.sln`
- **Test**: `dotnet test src/DINOForge.sln`
