# Dino pass 20 — required generation consumers v0

Date 2026-09-30. Frozen source `17119051e782b32615413049c1c3cd207f0b540e`.

The first concrete required-consumer set for generation activation is now source-mapped:

- `PackUnitSpawner.Initialize(RegistryManager)` — stores a static registry reference; called by ModPlatform.
- `WaveInjector.SetRegistryManager(RegistryManager)` — stores a static registry reference; called after system creation by ModPlatform.
- `AerialSpawnSystem.Initialize(RegistryManager?)` — stores a static registry reference; called by ModPlatform.
- `BuildMenuInjector.Initialize(RegistryManager?)` — stores a static registry reference and resets its internal done flag; called by ModPlatform.

These are not necessarily the **complete** runtime consumer set. Faction initialization and other domain/plugin consumers receive registry or typed-registry references through different patterns and remain to enumerate.

## Generation activation consequence

A production generation cannot be called fully active merely because ModPlatform publishes a new RegistryManager. Each required consumer must either:
1. resolve the current GenerationStore on each read; or
2. apply the candidate generation and ACK its exact ID.

If a consumer cannot live-apply a generation, it should NACK/restart-required rather than silently keep the old registry while the global receipt says success.

The test-only activation coordinator now models desired vs active generation and keeps the prior active generation authoritative until all required consumers ACK. NACK reasons are included in the recovery receipt.

## Next denominator work

Enumerate all consumers of:
- RegistryManager;
- IRegistry<T>;
- LoadedOverrides;
- AssetSwapRegistry/asset caches;
- domain-specific registries populated from pack content.

Classify each as:
- dynamic lookup;
- static retained reference;
- copied/derived cache;
- existing-ECS materialization;
- new-entity-only application.

That classification determines whether a generation update can be live-applied, requires re-materialization, or is restart-only.


## Reachability correction

Source-wide `RegistryManager` references also exist in EconomyPlugin, ScenarioPlugin, UIPlugin and WarfarePlugin facades. However, targeted constructor search at the frozen revision did not establish those facades as mounted by ModPlatform; matches are largely tests/docs/coverage. They remain **potential/unmounted consumers**, not members of the required live ACK denominator until a real caller is traced.

HotReloadBridge **is** mounted by ModPlatform and retains the original RegistryManager reference, so it is a required migration surface or must be replaced by a generation-aware bridge.

This prevents class-existence inflation: only mounted consumers can block live activation; unmounted facades still matter to mature architecture but not to the current in-game generation-ACK denominator.
