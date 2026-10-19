# Dino integration design — generation publication boundary

Date 2026-09-30. Based on three reproduced SDK generation failures. Design only; production code unchanged.

## Why swapping RegistryManager references alone is insufficient

Runtime consumers retain direct/static RegistryManager references:
- PackUnitSpawner
- WaveInjector
- BuildMenuInjector
- AerialSpawnSystem
- HotReloadBridge
- ModPlatform/domain plugins
- ContentLoader asset-swap enumeration

A fresh candidate RegistryManager can solve SDK stale state, but publishing by replacing only ModPlatform._registryManager would leave consumers attached to the old manager. This is a concrete integration hazard for ADR option B/C.

## Recommended integration seam: stable RegistryView / generation handle

Keep one stable object reference for runtime consumers, but make its current effective generation replaceable as a unit.

Conceptual API:

```
RegistryGeneration
  id
  candidate identity
  typed immutable/effectively immutable registries
  patch/dependency/conflict report

IRegistryView
  CurrentGenerationId
  Current.Registry<T>
  Publish(candidate) -> PublicationReceipt
```

Consumers receive IRegistryView or a generation-aware accessor, not a mutable RegistryManager whose lists are appended forever.

A lower-risk migration can begin with RegistryManager becoming a facade whose typed properties delegate to an internally published generation. Existing consumer references remain stable while generation state swaps beneath the facade. The facade must not expose candidate mutable registries before publication.

## Publication phases

1. Discover candidate complete pack set.
2. Normalize/identify artifacts.
3. Build dependency + patch graph.
4. Populate fresh candidate typed registries/import caches.
5. Validate semantic closure/conflicts.
6. Produce candidate generation digest.
7. Publish generation atomically under synchronization.
8. Notify runtime consumers with old/new generation + exact delta.
9. Consumers either acknowledge, declare restart-required, or fail.
10. Machine receipt distinguishes registry-published from fully-observed runtime generation.

## Consumer classes

### Lookup-on-demand
PackUnitSpawner/WaveInjector-style consumers that query registry at action time can follow the facade automatically after publication if they do not cache definitions.

### Materialized/cached runtime state
FactionSystem, BuildMenuInjector, stat modifiers, asset swaps and spawned ECS entities may require explicit reapply/rebuild. Their semantics differ:
- future spawns may use G2 while existing entities intentionally retain G1;
- menus/catalogs should generally reflect G2;
- stat overrides may need reapply;
- assets may require restart depending on host constraints.

Do not call the whole runtime G2-observed until all required consumer classes have a disposition.

## Compatibility migration

Phase 1: SDK-only facade/generation builder with old public RegistryManager shape.
Phase 2: generation-aware receipt + reload path.
Phase 3: runtime consumer acknowledgements.
Phase 4: host/native journey.
Avoid breaking pack schema during this migration.
