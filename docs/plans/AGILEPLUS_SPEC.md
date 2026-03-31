# AgilePlus Methodology Specification for DINOForge

**Version**: 1.0.0
**Status**: Active
**Last Updated**: 2026-03-31
**Rig ID**: `6c6d4555-91e8-4f06-a974-018cf3e766d2`
**Town**: `78a8d430-a206-4a25-96c0-5cd9f5caf984`
**Audience**: All contributors, agents, and maintainers

---

## 1. Overview

### 1.1 What is AgilePlus?

**AgilePlus** (`kooshapari/agileplus`) is the **spec-driven development engine** governing DINOForge project management. It implements an 8-phase pipeline with cryptographic audit trails, work package decomposition, and agent-agnostic dispatch.

AgilePlus inverts the typical development workflow. Instead of jumping from idea to code, every feature begins as a **specification** — a structured document that defines:

- What gets built and why
- Who the actors and users are
- What success looks like (acceptance criteria)
- Explicit scope boundaries
- Assumptions and dependencies
- Trade-offs and design rationale

### 1.2 AgilePlus in the DINOForge Ecosystem

DINOForge participates in a multi-repo AgilePlus ecosystem:

| Repo | Role | AgilePlus Integration |
|------|------|----------------------|
| `kooshapari/agileplus` | Core engine | Hosts the CLI, domain model, and docs |
| `kooshapari/Dino` (this repo) | Mod platform | Consumes AgilePlus; specs live in `docs/plans/` |
| `AgilePlus-*` convoy repos | Content packs | Feature branches track AgilePlus work packages |

The **DINOForge Town** (rig `78a8d430-a206-4a25-96c0-5cd9f5caf984`) coordinates work across all repos using **Kilo Gastown** — a multi-agent orchestration system that dispatches polecat agents to work items (beads) tracked in AgilePlus.

**Dashboard URL**: `https://kooshapari.github.io/AgilePlus`

---

## 2. Core Concepts

### 2.1 8-Phase Pipeline

Every feature flows through a deterministic state machine:

```
Created → Specified → Researched → Planned → Implementing → Validated → Shipped → Retrospected
```

Every state transition is recorded with cryptographic integrity (SHA-256 hash chain) and is auditable. No stage can be skipped. No work ships without validation.

| Phase | Purpose | Key Artifact |
|-------|---------|--------------|
| **Created** | Feature idea recorded | Feature slug, title, target branch |
| **Specified** | Requirements documented | `spec.md` with functional requirements, acceptance criteria |
| **Researched** | Feasibility assessed | `research.md` with codebase scan, risk analysis |
| **Planned** | Work decomposed | `plan.md` with work packages and dependency graph |
| **Implementing** | Code written | Work packages in isolated branches |
| **Validated** | Quality verified | Test results, lint output, security scans |
| **Shipped** | Deployed | Merged to target branch |
| **Retrospected** | Lessons learned | `retrospective.md` with metrics and action items |

### 2.2 Work Packages (WPs)

Large features decompose into **work packages** — small, independently implementable units with:

- Unique ID (WP01, WP02, etc.)
- Isolated git branch (`feature/my-feature/WP01`)
- Clear acceptance criteria (boolean success conditions)
- Defined file scope (which source files can be touched)
- Dependency tracking (explicit + file-overlap + data dependencies)

### 2.3 Governance by Default

Every state transition enforces preconditions:

- Cannot move to `Specified` without a valid `spec.md`
- Cannot move to `Planned` without research output
- Cannot move to `Implementing` without work packages assigned
- Cannot `Ship` without all validation passing

Evidence artifacts (test results, CI output, reviews) are recorded and chained with SHA-256 hashes.

### 2.4 Agent-Agnostic Dispatch

AgilePlus supports multiple AI agents through structured prompts:

- **Claude Code** — CLI dispatch with structured prompt
- **Cursor** — rule files + slash commands
- **Custom agents** — via gRPC API

All agents receive: spec context, plan context, WP definition, acceptance criteria, and file scope.

---

## 3. DINOForge-Specific Conventions

### 3.1 Spec Location and Naming

Feature specs are stored in `docs/plans/` with the naming convention:

```
docs/plans/<FEATURE_NAME>_SPEC.md
```

Example:
```
docs/plans/AGILEPLUS_SPEC.md      ← This document
docs/plans/KILO_GASTOWN_SPEC.md
```

### 3.2 State Machine for DINOForge

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

### 3.3 Spec-to-Story Mapping

Specifications in `docs/specs/` map to AgilePlus stories:

| Spec File | AgilePlus Story | Phase |
|----------|-----------------|-------|
| `user-spec.md` | Core user workflows | Ongoing |
| `technical-spec.md` | Platform architecture | Ongoing |
| `SPEC-002-native-menu-injector.md` | F10 menu injection | Shipped |
| `SPEC-003-prove-features-skill.md` | Prove features automation | Shipped |
| `SPEC-005-duplicate-instance-bypass.md` | Second instance bypass | Shipped |

---

## 4. Integration with Kilo Gastown

### 4.1 Convoy System

DINOForge uses **convoys** to coordinate multi-repo feature development:

```
Feature Branch: convoy/agileplus-kilo-specs-dino/<convoy_id>/head
```

Current active convoy: `convoy/agileplus-kilo-specs-dino/381d5195/head`

### 4.2 Bead Lifecycle

Each work package (WP) becomes a **bead** in Gastown:

| Bead State | AgilePlus Equivalent | Description |
|------------|---------------------|--------------|
| `open` | PLANNED | Work package is queued |
| `in_progress` | IMPLEMENTING | Agent is actively working |
| `in_review` | VALIDATED | Work is under review |
| `closed` | SHIPPED | Work is merged and complete |

### 4.3 Polecat Agents

Polecat agents (e.g., `Polecat-23`) are dispatched to beads. Each polecat:

1. **Hoods the bead** — claims it for work
2. **Researches** — studies the spec, codebase, and context
3. **Implements** — makes changes in an isolated worktree
4. **Commits** — pushes with descriptive messages
5. **Calls `gt_done`** — signals completion, transitions to `in_review`

### 4.4 Agent Role Assignments

DINOForge follows a role-based agent model documented in `AGENTS.md`:

| Agent Role | Domain | File Ownership |
|------------|--------|----------------|
| runtime-specialist | ECS bridge, BepInEx | `src/Runtime/` |
| sdk-architect | Registry, SDK, schemas | `src/SDK/` |
| warfare-designer | Warfare domain, balance | `src/Domains/Warfare/` |
| pack-builder | Content packs, YAML | `packs/` |
| toolsmith | CLI tools, PackCompiler | `src/Tools/` |
| qa-engineer | Tests, CI/CD | `src/Tests/`, `.github/` |
| docs-curator | Documentation | `docs/`, `CHANGELOG.md` |

---

## 5. Development Workflows

### 5.1 Feature Development Flow

```
1. Idea → Specify (agileplus specify "feature-name")
2. Review spec with stakeholders
3. Research (agileplus research <id>)
4. Plan (agileplus plan <id>)
   → Creates WPs with dependency graph
5. Implement WPs (agileplus implement WP01 --agent claude-code)
6. Review (agileplus review WP01)
7. Validate (transition to Validated state)
8. Merge (agileplus merge <id>)
9. Retrospect (agileplus retrospective <id>)
```

### 5.2 Handoff Protocol

When completing work, agents MUST:

1. Run `dotnet test src/DINOForge.sln` — verify 0 failures
2. Run `dotnet format src/DINOForge.sln --verify-no-changes`
3. Update CHANGELOG.md `[Unreleased]` section
4. Commit with: `Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>`
5. Push to remote
6. Call `gt_done` to transition bead to `in_review`

### 5.3 Integration with DINOForge Tooling

AgilePlus work packages integrate with DINOForge's agent command infrastructure:

| Command | Purpose | Agent |
|---------|---------|-------|
| `/new-pack <id> [type]` | Scaffold new content pack | pack-builder |
| `/add-unit <pack> <id> <class>` | Add unit to pack | pack-builder |
| `/spawn-unit <pack:unit> [x] [z]` | Test unit spawner | toolsmith |
| `/validate` | Validate all packs | toolsmith |
| `/test` | Run all tests | qa-engineer |
| `/check-ci` | Run full CI locally | qa-engineer |

---

## 6. Quality Gates

Before any commit to main, DINOForge enforces quality gates:

### 6.1 Pre-Commit Gates

- [ ] `dotnet build src/DINOForge.sln` — solution compiles
- [ ] `dotnet test src/DINOForge.sln` — all tests pass (0 failures)
- [ ] `dotnet format src/DINOForge.sln --verify-no-changes` — code formatted
- [ ] CHANGELOG.md updated

### 6.2 Pre-Merge Gates (CI)

| Check | Tool | Threshold |
|-------|------|-----------|
| Build | dotnet build | Must succeed |
| Tests | dotnet test | 0 failures |
| Lint | dotnet format --verify-no-changes | No diff |
| Schema Validation | PackCompiler | All packs valid |

### 6.3 Pre-Ship Gates

- [ ] All WPs merged cleanly
- [ ] CHANGELOG.md entry added under correct version
- [ ] Spec marked as `SHIPPED`

---

## 7. AgilePlus ↔ DINOForge Terminology Mapping

| AgilePlus Term | DINOForge/Gastown Equivalent |
|----------------|----------------------------|
| Feature | Spec document in `docs/plans/` |
| Work Package (WP) | Bead in Gastown rig |
| Spec | `*_SPEC.md` file |
| Pipeline | State machine in CLAUDE.md |
| Implement | Polecat working a bead |
| Review | `gt_done` → in_review state |
| Merge | Git worktree merged to main |
| Tracker | Gastown rig (this repo) |

---

## 8. Related Documents

- [CLAUDE.md](./CLAUDE.md) — Core governance, build commands, architecture
- [AGENTS.md](./AGENTS.md) — Agent roster, domain ownership, coordination
- [CHANGELOG.md](./CHANGELOG.md) — Keep a Changelog format version history
- [AgilePlus Docs](https://kooshapari.github.io/AgilePlus/) — Full AgilePlus reference

---

**Last Updated**: 2026-03-31
**Status**: Active
**Owner**: docs-curator
