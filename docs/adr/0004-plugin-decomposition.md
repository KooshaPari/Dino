# ADR-0004: Plugin.cs God File Decomposition

## Status: Planned (2026-08-25)

## Context
Plugin.cs is 179KB (3,451 lines) — the entire DINOForge mod framework in one file.
ModMenuPanel.cs is 173KB (3,569 lines) — full mod management UI.
GameBridgeServer.cs is 130KB (2,733 lines) — named pipe server + all commands.

## Decision: partial class decomposition

### Plugin.cs (11 modules)
| Module | Responsibility |
|--------|---------------|
| Plugin.cs (core) | BepInEx entry point, lifecycle |
| Plugin.Resurrection.cs | Crash recovery, restart |
| Plugin.Heartbeat.cs | Heartbeat file writer |
| Plugin.EventSystem.cs | EventSystem setup |
| RuntimeDriver.cs (core) | Driver initialization |
| RuntimeDriver.UI.cs | NativeMenu, LoadingScreen |
| RuntimeDriver.EcsPolling.cs | ECS system creation |
| RuntimeDriver.PackLoading.cs | Pack discovery |
| RuntimeDriver.SceneManagement.cs | Scene transitions |
| RuntimeDriver.OnDestroy.cs | Cleanup |
| HmrAdapters.cs | Hot-reload |

### ModMenuPanel.cs (8 modules)
| Module | Responsibility |
|--------|---------------|
| ModMenuPanel.cs | Core state + lifecycle |
| ModMenuHeader.cs | Status bar |
| ModMenuListPane.cs | Pack list, search |
| ModMenuDetailPane.cs | Pack details |
| ModMenuConflictResolver.cs | Diff modal |
| ModMenuSettingsSection.cs | Settings UI |
| ModMenuProfiles.cs | Profile CRUD |
| ModMenuTelemetry.cs | Debug telemetry |

### GameBridgeServer.cs (6 modules)
| Module | Responsibility |
|--------|---------------|
| GameBridgeServer.cs | Server loop |
| GameBridgeServer.StatusHandlers.cs | Connect, ping |
| GameBridgeServer.EcsHandlers.cs | ECS queries |
| GameBridgeServer.UiHandlers.cs | UI tree |
| GameBridgeServer.GameFlowHandlers.cs | Scenes, screenshots |
| GameBridgeServer.InputHandlers.cs | Keyboard sim |

## Consequences
+ Each file becomes independently testable
+ Code review becomes manageable (200-400 lines each)
+ Clear ownership boundaries
- Merge conflicts during transition period
