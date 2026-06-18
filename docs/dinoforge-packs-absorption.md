# dinoforge-packs absorption — 2026-06-18

This document records the absorption of `KooshaPari/dinoforge-packs` into
`KooshaPari/Dino` per kilo audit #144 (PRESERVE → ABSORBED) and ADR-023 (app
substrate placement).

## What was absorbed

| Source path (dinoforge-packs) | Destination (Dino) | Type | Status |
|-------------------------------|---------------------|------|--------|
| `example-balance/` | `packs/example-balance/` | New pack (not in Dino) | ✅ **ACTIVE** |
| `warfare-starwars/` | `packs/community-contributions/dinoforge-packs-mirror/warfare-starwars/` | Stale duplicate | ⚠️ **ID migration needed** |
| `tests/smoke_test.go` | `tests/packs/dinoforge-packs_smoke_test.go` | Test | ✅ active |
| `docs/{journeys,operations,sessions,worklogs}/` | `packs/community-contributions/dinoforge-packs-mirror/docs/` | Reference docs | ✅ preserved |
| `AGENTS.md` | `docs/dinoforge-packs_AGENTS.md` | Governance | ✅ preserved |
| `CLAUDE.md` | `docs/dinoforge-packs_CLAUDE.md` | Governance | ✅ preserved |
| `FUNCTIONAL_REQUIREMENTS.md` | `docs/dinoforge-packs_FUNCTIONAL_REQUIREMENTS.md` | Spec | ✅ preserved |
| `CHANGELOG.md` | `docs/dinoforge-packs_CHANGELOG.md` | History | ✅ preserved |
| `LICENSE` | inherited (repo MIT) | License | ✅ preserved |
| `README.md` | `packs/community-contributions/dinoforge-packs-mirror/README.md` | Doc | ✅ preserved |

## ID divergence (the important part)

The dinoforge-packs warfare-starwars manifest declares units with
**legacy non-namespaced IDs**:

```yaml
# dinoforge-packs/warfare-starwars/manifest.yaml (legacy)
units:
  - clone-trooper
  - arc-trooper
  - b1-battle-droid
  - droideka
```

These IDs **DO NOT EXIST** in the canonical Dino `packs/warfare-starwars/`
pack. The canonical pack uses **namespaced faction_kind-prefixed IDs**:

```yaml
# Dino/packs/warfare-starwars/manifest.yaml (canonical)
units:
  - rep_clone_trooper
  - rep_arc_trooper
  - cis_b1_droid
  - cis_droideka
```

### Why the divergence?

DINOForge refactored to faction-namespaced unit IDs in v0.5.0 to support
multi-faction coexistence in the same load order (legacy IDs collided when
Republic and CIS units loaded together). The dinoforge-packs repo was
frozen at v0.1.0 content and never received the v0.5.0 ID migration.

### Migration path (open follow-up)

1. Open PR remapping `units:`/`buildings:`/`weapons:`/`doctrines:` lists in
   the mirrored manifest:
   - `clone-trooper` → `rep_clone_trooper`
   - `arc-trooper` → `rep_arc_trooper`
   - `at-te` → `rep_atte_crew`
   - `clone-gunship` → `rep_v19_torrent`
   - `b1-battle-droid` → `cis_b1_droid`
   - `b2-super-battle-droid` → `cis_b2_droid`
   - `droideka` → `cis_droideka`
   - `hailfire-droid` → `cis_hailfire_droid`
   - `commando-droid` → `cis_commando_droid`
   - `magna-guard` → `cis_magnaguard`
2. Add `"<1.0.0"` upper bound to `framework_version`
3. Replace faction `replaces_vanilla:` values to match canonical names
4. Add `ui_theme:` block per canonical template
5. After migration passes `go test ./tests/packs/...`, promote from
   `community-contributions/` into `packs/warfare-starwars-republic-cis/`

## Governance doc retention

The absorbed `AGENTS.md` (74 lines) and `CLAUDE.md` (60 lines) are preserved
verbatim as `docs/dinoforge-packs_AGENTS.md` and `docs/dinoforge-packs_CLAUDE.md`
because they capture the original team's working agreements. They should be
read alongside (not in place of) Dino's `AGENTS.md` and `CLAUDE.md`.

## Source-of-truth reconciliation

After absorption, the canonical pack definitions live in `KooshaPari/Dino`.
The original `KooshaPari/dinoforge-packs` repo is marked archived (gh token
lacks `delete_repo` scope; archive is the available action).

## References

- Kilo audit #144: `.kilo/audits/kooshapari-absorption-2026-06-18.md`
- ADR-023: app substrate placement (no random `phenoShared`)
- `packs/community-contributions/README.md` — absorption manifest

---
*Absorption commit: feat/community-packs-subtree-2026-06-18*
*Recorded: 2026-06-18 by Forge (KooshaPari) for the kilo absorption closure*
