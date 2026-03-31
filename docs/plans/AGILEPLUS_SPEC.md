# AgilePlus Methodology Specification for DINOForge

**Version**: 1.0.0
**Status**: Active
**Last Updated**: 2026-03-31
**Audience**: All contributors, agents, and maintainers

---

## 1. Overview

This document describes how **AgilePlus** — the spec-driven development engine at `kooshapari/agileplus` — is applied to the DINOForge project. It defines the conventions, workflows, and governance rules that govern how features are specified, implemented, validated, and shipped in this repository.

### 1.1 What is AgilePlus?

AgilePlus is a **spec-driven development engine** that inverts the typical development workflow. Instead of jumping from idea to code, every feature begins as a **specification** — a structured document that defines:

- What gets built and why
- Who the actors and users are
- What success looks like (acceptance criteria)
- Explicit scope boundaries
- Assumptions and dependencies
- Trade-offs and design rationale

AgilePlus enforces an **8-stage governed pipeline**:

```
Created → Specified → Researched → Planned → Implementing → Validated → Shipped → Retrospected
```

Every state transition is recorded with cryptographic integrity (SHA-256 hash chain) and is auditable. No stage can be skipped. No work ships without validation.

### 1.2 AgilePlus in the DINOForge Ecosystem

DINOForge participates in a multi-repo AgilePlus ecosystem:

| Repo | Role | AgilePlus Integration |
|------|------|----------------------|
| `kooshapari/agileplus` | Core engine | Hosts the CLI, domain model, and docs |
| `kooshapari/Dino` (this repo) | Mod platform | Consumes AgilePlus; specs live in `docs/plans/` |
| `AgilePlus-*` convoy repos | Content packs | Feature branches track AgilePlus work packages |

The **DINO Forge Town** (rig `78a8d430-a206-4a25-96c0-5cd9f5caf984`) coordinates work across all repos using **Kilo Gastown** — a multi-agent orchestration system that dispatches polecat agents to work items (beads) tracked in AgilePlus.

---

## 2. DINOForge-Specific AgilePlus Conventions

### 2.1 Spec Location and Naming

Feature specs are stored in `docs/plans/` with the naming convention:

```
docs/plans/<FEATURE_NAME>_SPEC.md
```

Example:
```
docs/plans/AGILEPLUS_SPEC.md      ← This document
docs/plans/ASSET_PIPELINE_SPEC.md
docs/plans/MCP_BRIDGE_SPEC.md
```

Each spec file includes:

- **Spec Hash**: SHA-256 hash computed on creation for immutable tracking
- **Feature ID**: Auto-incremented identifier (e.g., `SPEC-001`, `SPEC-002`)
- **Status**: Current AgilePlus pipeline stage
- **Owner**: Agent or developer responsible
- **FR IDs**: Functional requirement identifiers (FR-001, FR-002, etc.)

### 2.2 Feature ID Format

Features use the pattern: `SPEC-<NNN>`

Examples: `SPEC-001`, `SPEC-002`, `SPEC-010`

Work packages within a feature use: `WP-<FEATURE>-<NN>`

Example: `WP-001-01`, `WP-001-02` (Work Packages for Feature 001)

### 2.3 State Machine for DINOForge

DINOForge features follow this state machine:

```
CREATED → SPECIFIED → RESEARCHED → PLANNED → IMPLEMENTING → VALIDATED → SHIPPED → RETROSPECTED
```

| Transition | Enforced Precondition |
|------------|----------------------|
| CREATED → SPECIFIED | Spec artifact exists with minimum required fields |
| SPECIFIED → RESEARCHED | Research output attached (codebase scan or feasibility) |
| RESEARCHED → PLANNED | WP decomposition generated; dependency graph is acyclic |
| PLANNED → IMPLEMENTING | At least one WP assigned; branch created; agent notified |
| IMPLEMENTING → VALIDATED | All WPs marked Done; automated tests queued |
| VALIDATED → SHIPPED | All governance checks pass; all WPs merged cleanly |
| SHIPPED → RETROSPECTED | Post-incident analysis / lessons learned documented |

---

## 3. Integration with Kilo Gastown

DINOForge uses **Kilo Gastown** for multi-agent orchestration. Every AgilePlus work package becomes a **bead** (work item) in Gastown.

### 3.1 Convoy System

AgilePlus work that spans multiple repositories flows through **convoys**:

```
Feature Branch: convoy/agileplus-kilo-specs-dino/<convoy_id>/head
```

Example from this repo:
```
convoy/agileplus-kilo-specs-dino/381d5195/head
```

Convoys group related work packages across repos under a single feature umbrella.

### 3.2 Bead Lifecycle

Each work package (WP) becomes a **bead** in Gastown:

| Bead State | AgilePlus Equivalent | Description |
|------------|---------------------|--------------|
| `open` | PLANNED | Work package is queued |
| `in_progress` | IMPLEMENTING | Agent is actively working |
| `in_review` | VALIDATED | Work is under review |
| `closed` | SHIPPED | Work is merged and complete |

### 3.3 Polecat Agents

Polecat agents (e.g., `Polecat-23`) are dispatched to beads. Each polecat:

1. **Hoods the bead** — claims it for work
2. **Researches** — studies the spec, codebase, and context
3. **Implements** — makes changes in an isolated worktree
4. **Commits** — pushes with descriptive messages
5. **Calls `gt_done`** — signals completion, transitions to `in_review`

---

## 4. Spec Document Template

Every DINOForge spec MUST follow this template:

```markdown
# <FEATURE NAME>

**Version**: X.Y.Z
**Status**: <AGILEPLUS_STATE>
**Last Updated**: YYYY-MM-DD
**Spec Hash**: <SHA-256>
**Feature ID**: SPEC-<NNN>
**Owner**: <agent_or_developer>

---

## 1. Summary

Brief description of the feature (2-4 sentences).

## 2. Value Proposition

Why are we building this? What problem does it solve?

## 3. Actors and Users

Who benefits from this feature?

## 4. Functional Requirements

| ID | Requirement | Priority | Acceptance Criteria |
|----|-------------|----------|---------------------|
| FR-001 | Description | MUST | Boolean condition |
| FR-002 | Description | SHOULD | Boolean condition |

## 5. Out of Scope

What is explicitly NOT included in this feature.

## 6. Assumptions and Dependencies

- Assumption 1
- Dependency on X repo

## 7. Technical Approach

High-level design decisions and rationale.

## 8. File Impact

| File/Directory | Change Type | Purpose |
|---------------|-------------|---------|
| src/Runtime/Plugin.cs | Modify | Reason |

## 9. Work Packages

| WP ID | Title | Owner | Dependencies |
|-------|-------|-------|-------------|
| WP-001-01 | Sub-task 1 | polecat-23 | None |
| WP-001-02 | Sub-task 2 | polecat-24 | WP-001-01 |

## 10. Success Metrics

How do we measure success?

## 11. Governance

- Spec hash on creation: `<hash>`
- State transitions logged in AgilePlus
- Evidence artifacts stored in `docs/sessions/`
```

---

## 5. AgilePlus Workflow Commands

### 5.1 CLI Reference

AgilePlus CLI (when installed via `cargo install agileplus`):

| Command | Description |
|---------|-------------|
| `agileplus init` | Initialize AgilePlus governance in a repo |
| `agileplus specify "Feature title"` | Create new spec via interactive interview |
| `agileplus plan <spec-id>` | Generate work package decomposition |
| `agileplus tasks <spec-id>` | Generate individual WP files |
| `agileplus implement <wp-id>` | Create worktree and begin work |
| `agileplus review <wp-id>` | Review work package |
| `agileplus accept <spec-id>` | Accept feature (all WPs done) |
| `agileplus merge <spec-id>` | Merge feature to main |

### 5.2 DINOForge Git Integration

Work is conducted in **worktrees** to keep changes isolated:

```bash
# Create feature worktree
git worktree add ../worktrees/convoy__<feature>__<id>__gt__<agent> main

# Work happens in the worktree
cd ../worktrees/convoy__<feature>__<id>__gt__<agent>

# Commit with AgilePlus context
git commit -m "feat(WP-001-01): implement sub-task description"
```

### 5.3 Handoff Protocol

When completing work, agents MUST:

1. Run `dotnet test src/DINOForge.sln` — verify 0 failures
2. Run `dotnet format src/DINOForge.sln --verify-no-changes`
3. Update CHANGELOG.md `[Unreleased]` section
4. Commit with: `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>`
5. Push to remote
6. Call `gt_done` to transition bead to `in_review`

---

## 6. Quality Gates

Before any work is considered complete, these gates MUST pass:

### 6.1 Pre-Commit Gates

- [ ] `dotnet build src/DINOForge.sln` — solution compiles
- [ ] `dotnet test src/DINOForge.sln` — all tests pass (0 failures)
- [ ] `dotnet format src/DINOForge.sln --verify-no-changes` — code formatted
- [ ] Spec updated with new file impacts
- [ ] CHANGELOG.md updated

### 6.2 Pre-Merge Gates (CI)

| Check | Tool | Threshold |
|-------|------|-----------|
| Build | dotnet build | Must succeed |
| Tests | dotnet test | 0 failures |
| Lint | dotnet format --verify-no-changes | No diff |
| Security | CodeQL | 0 alerts |
| Dependency Audit | trust-advisory | 0 vulnerabilities |

### 6.3 Pre-Ship Gates

- [ ] All WPs merged cleanly
- [ ] Feature branch deleted
- [ ] CHANGELOG.md entry added under correct version
- [ ] Docs updated if public API changed
- [ ] Spec marked as `SHIPPED`

---

## 7. Governance and Audit

### 7.1 Audit Trail

Every state transition is recorded in an append-only JSONL audit log:

```json
{
  "timestamp": "2026-03-31T12:00:00Z",
  "feature_id": "SPEC-001",
  "wp_id": "WP-001-01",
  "transition": "IMPLEMENTING → VALIDATED",
  "actor": "polecat-23",
  "evidence": ["test-results.json", "lint-output.txt"],
  "prev_hash": "abc123...",
  "curr_hash": "def456..."
}
```

### 7.2 Evidence Artifacts

Evidence for each transition is stored in:

- **Test results**: `docs/sessions/<date>-test-results/`
- **Lint output**: `docs/sessions/<date>-lint/`
- **Build logs**: `*.log` in repo root (existing)
- **Research artifacts**: `docs/research/` or `docs/sessions/`

### 7.3 File Ownership

To prevent conflicts, DINOForge uses a **file ownership map** (see `AGENTS.md`):

| Agent Role | Domain | Can Modify |
|-----------|--------|-----------|
| runtime-specialist | src/Runtime/ | Plugin.cs, Bridge/*, HotReload/* |
| sdk-architect | src/SDK/ | Registry/*, Models/*, Validation/* |
| warfare-designer | src/Domains/Warfare/ | Archetypes/*, Doctrines/*, Balance/* |
| pack-builder | packs/ | All pack content |
| toolsmith | src/Tools/ | PackCompiler/*, McpServer/* |
| qa-engineer | src/Tests/ | Tests/**, workflows/* |
| docs-curator | docs/ | docs/**, CHANGELOG.md |

---

## 8. AgilePlus ↔ DINOForge Terminology Mapping

| AgilePlus Term | DINOForge/Gastown Equivalent |
|----------------|------------------------------|
| Feature | Spec document in `docs/plans/` |
| Work Package (WP) | Bead in Gastown rig |
| Spec | `*_SPEC.md` file |
| Pipeline | State machine in CLAUDE.md |
| Implement | Polecat working a bead |
| Review | `gt_done` → in_review state |
| Merge | Git worktree merged to main |
| Tracker | Gastown rig (this repo) |

---

## 9. Quick Reference

### 9.1 Starting a New Feature

1. Create spec in `docs/plans/<FEATURE>_SPEC.md`
2. Run `agileplus plan <spec-id>` to decompose into WPs
3. Create beads for each WP in Gastown
4. Dispatch polecats to beads

### 9.2 Completing a Work Package

1. Implement changes in worktree
2. Run quality gates (build, test, format)
3. Update CHANGELOG.md
4. Commit with Co-Authored-By
5. Push branch
6. Call `gt_done`

### 9.3 Feature Complete Checklist

- [ ] All WPs merged
- [ ] All tests pass
- [ ] CHANGELOG.md updated
- [ ] Spec marked SHIPPED
- [ ] Feature branch deleted
- [ ] Post-mortem documented (if needed)

---

## 10. Related Documents

- [CLAUDE.md](./CLAUDE.md) — Core governance, build commands, architecture
- [AGENTS.md](./AGENTS.md) — Agent roster, domain ownership, coordination
- [CHANGELOG.md](./CHANGELOG.md) — Keep a Changelog format version history
- [AgilePlus Docs](https://kooshapari.github.io/AgilePlus/) — Full AgilePlus reference

---

**Last Updated**: 2026-03-31
**Status**: Active
**Owner**: docs-curator
