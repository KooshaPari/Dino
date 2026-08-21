# ADR-0003: MCP Module Decomposition

## Status
Accepted (2026-08-20)

## Context
The MCP server (server.py) grew to 2,298 lines and isolation_layer.py to 1,172 lines, causing:
- Difficult code navigation and review
- Slow test execution due to transitive import chains
- Proof gate failures when importing standalone tools
- Risk of merge conflicts in a single large file

## Decision
Decompose both monoliths into focused modules:

### server.py -> 8 modules
- `server.py` (81 lines) - Entry point, CLI args, MCP instance
- `config.py` (~200 lines) - Constants, paths, shared helpers
- `game_control.py` (~1,400 lines) - Game bridge, UI, screenshots, ECS
- `asset_pipeline.py` (~170 lines) - Asset & pack management
- `catalog_inspection.py` (~80 lines) - Addressables catalog
- `log_analysis.py` (~300 lines) - Debug log tools
- `voice_commands.py` (~200 lines) - Voice intent recognition
- `routes_prompts.py` (~400 lines) - Routes, resources, prompts

### isolation_layer.py -> 6 modules
- `isolation_layer/__init__.py` - Package init
- `isolation_layer/models.py` - Data models
- `isolation_layer/hidden_desktop.py` - Desktop isolation
- `isolation_layer/playcua_client.py` - PlayCUA client
- `isolation_layer/playcua_backend.py` - Backend implementation
- `isolation_layer/context.py` - Context management

## Consequences
### Positive
- Each module is independently testable
- Lazy imports in __init__.py prevent transitive dependency issues
- Backward-compatible re-exports in server.py maintain API stability
- Proof gate can now run standalone tools

### Negative
- Import paths changed for internal code
- Some tests needed updating (test_pipe_resolution.py)
- Two modules (game_control, routes_prompts) are still >400 lines

## Follow-up
- Further decompose game_control.py into ui/, bridge/, ecs/ submodules
- Add import cycle detection to CI
