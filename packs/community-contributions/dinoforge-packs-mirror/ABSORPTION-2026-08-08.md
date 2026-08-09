# dinoforge-packs-mirror — Absorption Update 2026-08-08

This mirror was originally absorbed 2026-06-18 from
`KooshaPari/dinoforge-packs` (legacy non-namespaced unit IDs).

On 2026-08-08 the remainder of `KooshaPari/dinoforge-packs-archive-2026-07-14`
(origin = 404, snapshot taken 2026-07-14) was merged into this mirror to
complete the absorption.

## What was newly merged in 2026-08-08

### Documentation files (3)
- `docs/index.md` — top-level docs index
- `docs/intent/dinoforge-packs.md` — intent doc
- `docs/boundary/dinoforge-packs.md` — boundary doc

### Top-level governance files (17)
- `.editorconfig`
- `.gitignore`
- `.pre-commit-config.yaml`
- `AGENTS.md`
- `CHANGELOG.md`
- `CITATION.cff`
- `CLAUDE.md`
- `CODE_OF_CONDUCT.md`
- `CODEOWNERS`
- `CONTRIBUTING.md`
- `FUNCTIONAL_REQUIREMENTS.md`
- `FUNDING.yml`
- `LICENSE`
- `README.md`
- `SECURITY.md`
- `Taskfile.yml`
- `trufflehog.yml`

### Tests + CI (1 + 9 files)
- `tests/smoke_test.go`
- `.github/CODEOWNERS`
- `.github/dependabot.yml`
- `.github/FUNDING.yml`
- `.github/ISSUE_TEMPLATE/bug_report.md`
- `.github/ISSUE_TEMPLATE/feature_request.md`
- `.github/PULL_REQUEST_TEMPLATE.md`
- `.github/workflows/alert-sync-issues.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/doc-links.yml`
- `.github/workflows/fr-coverage.yml`
- `.github/workflows/quality-gate.yml`
- `.github/workflows/scorecard.yml`
- `.github/workflows/trufflehog.yml`

## What was already here (unchanged)

The original 2026-06-18 absorption placed all YAML content under
`warfare-starwars/` (buildings, doctrines, factions, manifest, pack, units,
waves, weapons) plus the 6 docs under `docs/` (journeys/manifests, operations,
sessions/20260428-taskfile-dinoforge-packs, worklogs).

A full re-diff of archive vs mirror in 2026-08-08 confirmed those YAML files
are byte-identical to the snapshot, so no YAML content was overwritten.

## ID divergence warning (still in effect)

The mirror's `warfare-starwars/` manifest uses **legacy non-namespaced unit
IDs** (`clone-trooper`, `b1-battle-droid`, `droideka`).

The canonical pack at [`../warfare-starwars/`](../warfare-starwars/) uses
**namespaced unit IDs** (`rep_clone_trooper`, `cis_b1_droid`,
`cis_droideka`).

**DO NOT load the mirrored manifest directly.** The unit IDs must be
remapped to the canonical namespaced form before this content can be used
in DINO.

## Source provenance

- `KooshaPari/dinoforge-packs` (deleted) — absorbed 2026-06-18
- `KooshaPari/dinoforge-packs-archive-2026-07-14` (origin returned 404) —
  remainder merged 2026-08-08
