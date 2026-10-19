# Dino pass 6 — machine reload evidence boundary

Date 2026-09-29. Frozen source `17119051e782b32615413049c1c3cd207f0b540e`.

## Existing tests leave replacement semantics untested

`src/Tests/HotReloadTests.cs` tests DTO shape, debounce, watcher lifecycle, concurrent enqueue and non-null ReloadAll outcomes. It does not assert generation replacement, removal convergence, patch invalidation or live-game adoption.

Coverage reports showing ReloadPack executed are therefore implementation-coverage evidence, not semantic replacement evidence.

## D-F12 — bridge reload receipt is insufficient for autonomous acceptance

`GameBridgeServer.HandleReloadPacks` runs `_platform.LoadPacks()` on the main thread and maps ContentLoadResult to protocol ReloadResult.

ContentLoadResult contains:
- IsSuccess;
- LoadedPacks;
- Errors;
- ErrorsByPack.

ReloadResult narrows that further to:
- Success;
- LoadedPacks;
- Errors.

No generation ID, artifact digest, dependency/patch digest, exact semantic delta, removed IDs, conflict set, partial-effective-state description, runtime-application acknowledgement or world generation is returned.

Therefore `game_reload-packs` cannot by itself support the MACE question "did this exact candidate become the accepted effective state?" An agent can only learn that pack IDs were reported loaded without fatal aggregate errors.

### Required future receipt

A versioned reload/activation receipt should include:
- candidate/effective generation ID;
- exact pack artifact/version digests;
- previous generation;
- added/changed/removed effective content IDs by type;
- unresolved conflicts and dependency failures;
- patch-set digest;
- activation policy result (committed/partial/rejected/restart-required);
- runtime consumers applied/pending/failed;
- install/profile/world generation;
- evidence/run correlation.

Keep legacy ReloadResult for compatibility if needed; do not silently change its semantics.

## D-F13 — reloadPacks ignores its protocol path parameter at the runtime handler

IGameBridge/ReloadResult APIs accept an optional path and GameClient serializes it. In the inspected `HandleReloadPacks(JObject? parameters)`, the parameters are not read; the handler always calls `_platform.LoadPacks()`.

Thus CLI/MCP callers may believe they selected a pack path while the runtime handler reloads the platform's configured/default packs path. This is a concrete source-level subject-identity problem.

Required control: configure two pack roots A/B with distinguishable content, call reloadPacks(path=B), then observe which root actually becomes effective. Until fixed/explicitly unsupported, path-bearing receipts must not qualify target selection.

## Experimental tests to add

These are expected to fail if the source hypotheses are correct:
- `ReloadPack_ReplacingSamePack_DoesNotAccumulateGeneration`
- `ReloadPack_RemovedItem_NoLongerEffectiveOrReportsRestartRequired`
- `ReloadPack_RemovedPatch_DoesNotUseStalePatchedYaml`
- `ReloadPacks_PathParameter_SelectsRequestedRoot`
- `ReloadReceipt_BindsExactEffectiveGeneration` (design test once receipt exists)

No production fix in this pass.
