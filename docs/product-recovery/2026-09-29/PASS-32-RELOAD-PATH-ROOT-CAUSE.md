# Dino pass 32 — reload path identity root cause

Date 2026-09-30.

The path-subject bug is now traced end-to-end:

1. GameClient.ReloadPacksAsync(path) serializes { path }.
2. GameBridgeServer.HandleReloadPacks(parameters) never reads parameters.
3. Handler calls _platform.LoadPacks().
4. ModPlatform.LoadPacksImpl() unconditionally sets `packsDir = _packsDirectory.Value`.
5. ContentLoader itself already supports `LoadPacks(string packsRootDirectory)`.

Therefore fixing only the bridge handler cannot honestly honor path; ModPlatform needs a shared path-aware load entry point.

## Required refactor shape

Do not duplicate LoadPacksImpl.

Refactor toward:
- `LoadPacks()` -> `LoadPacksFromRoot(_packsDirectory.Value)`
- `LoadPacks(string packsRootDirectory)` or internal equivalent -> validate/canonicalize requested root -> `LoadPacksFromRoot(resolvedRoot)`
- one shared implementation performs disabled-pack handling, ContentLoader.LoadPacks(resolvedRoot), runtime consumer application, overrides, UI and telemetry.

Receipt binds:
- RequestedPath = raw caller value;
- ResolvedPath = canonical absolute root actually supplied to ContentLoader;
- reject nonexistent/unauthorized/out-of-policy roots explicitly;
- default/no path reports configured _packsDirectory.Value after canonicalization.

Do not connect this to GenerationStore until the path-aware shared load entry point has isolated tests and GameInstalled=true host evidence. Otherwise generation receipts could precisely identify the wrong subject.

## Current evidence

GenerationStore + receipt DTO candidate run36716747784 is green. That establishes representational/coordinator mechanics, not runtime path correctness.
