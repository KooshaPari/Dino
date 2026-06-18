# Community Contributions

This directory holds pack contributions from external repositories that have
been absorbed into the DINOForge fleet. Each subdirectory preserves a snapshot
of the contributing repo at absorption time, including its docs, tests, and
governance files.

## Current absorbed repos

| Directory | Source repo | Absorbed | Notes |
|-----------|-------------|----------|-------|
| `dinoforge-packs-mirror/` | `KooshaPari/dinoforge-packs` | 2026-06-18 | Reference content packs (example-balance) + stale warfare-starwars variant requiring ID migration |

## Important: ID divergence warning

The `dinoforge-packs-mirror/warfare-starwars/` manifest uses **legacy
non-namespaced unit IDs** (e.g. `clone-trooper`, `b1-battle-droid`,
`droideka`) that DO NOT EXIST in the canonical
[`packs/warfare-starwars/`](../warfare-starwars/) pack.

The canonical pack uses **namespaced unit IDs** (e.g. `rep_clone_trooper`,
`cis_b1_droid`, `cis_droideka`) which match the unit definitions in
`packs/warfare-starwars/units/`.

**DO NOT load the mirrored manifest directly.** It is preserved as historical
reference and as a migration source — the unit IDs must be remapped to the
canonical namespaced form before this content can be used in DINO.

See [`docs/dinoforge-packs-absorption.md`](../../docs/dinoforge-packs-absorption.md)
for the full absorption matrix.

## How to contribute

1. Open a PR adding your pack under `packs/community-contributions/<your-repo-name>/`
2. Manifest must declare `framework_version: ">=0.5.0 <1.0.0"` (with upper bound)
3. Unit/building/faction IDs must use the `faction_kind` namespace prefix
   (`rep_*`, `cis_*`, `cis_sep_*`, etc.) to avoid collision with vanilla content
4. Include `manifest.yaml`, `pack.yaml`, and at least one faction + one unit
5. PR must pass `go test ./tests/packs/...` smoke tests
