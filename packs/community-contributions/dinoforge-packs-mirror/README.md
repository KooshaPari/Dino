# dinoforge-packs

Resource packs for the DINOForge platform — content packs (units, factions, buildings, doctrines) consumed by the DINOForge framework.

## Status

The previous README listed status as "archived", but the GitHub repository is **not** archived. Treat this repo as inactive but still mutable; verify upstream DINOForge framework status before relying on these packs.

## Layout

| Path | Purpose |
|------|---------|
| `example-balance/` | Reference content pack (`pack.yaml`, `units/`, `factions/`, `buildings/`, `stats/`) demonstrating the pack pipeline |
| `warfare-starwars/` | Total-conversion pack: "Star Wars: The Clone Wars" (`pack.yaml`, `manifest.yaml`, `units/`, `factions/`, `buildings/`, `weapons/`, `doctrines/`, `waves/`) |
| `tests/` | Pack tests |
| `docs/` | Documentation |
| `AGENTS.md`, `CLAUDE.md` | Agent governance |
| `FUNCTIONAL_REQUIREMENTS.md` | FR tracker (stub) |
| `LICENSE` | MIT |
| `CODE_OF_CONDUCT.md` | Code of conduct |

Pack metadata declares framework compatibility:
- `example-balance`: `framework_version: ">=0.1.0 <1.0.0"`
- `warfare-starwars`: `framework_version: ">=0.5.0"`, `singleton: true`

## Quick Start

```bash
git clone https://github.com/KooshaPari/dinoforge-packs.git
```

Loading a pack requires the DINOForge framework runtime (not included here).

## License

MIT — see `LICENSE`.

## Links

- Canonical repo: https://github.com/KooshaPari/dinoforge-packs
