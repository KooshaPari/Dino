# Dino generation denominator v0

Date 2026-09-30. Frozen source revision `17119051e782b32615413049c1c3cd207f0b540e`.
Authority: RECOVERY DRAFT. This is a denominator/checklist, not proof that every domain is reloadable.

## Core RegistryManager domains

The frozen RegistryManager owns exactly ten typed registries:
1. Units
2. Buildings
3. Factions
4. Weapons
5. Projectiles
6. Doctrines
7. Skills
8. Waves
9. Squads
10. FactionPatches

Every generation implementation must state for each domain:
- candidate construction source;
- source pack/artifact identity;
- patch/dependency interaction;
- effective conflict semantics;
- removal semantics;
- runtime consumers;
- lookup vs materialized behavior;
- existing-object vs new-object semantics;
- consumer ACK/restart policy;
- host oracle.

The current unit-based generation prototype proves architecture shape, not automatic parity across the other nine registries.

## Known runtime-consumer classes already traced

| Consumer | Relevant domain | Current class | Generation contract |
|---|---|---|---|
| PackUnitSpawner | Units | lookup-on-demand for future spawn | rebind/ACK future lookups; existing entities separate |
| WaveInjector | Waves + referenced Units | lookup + active materialization | future queued definitions can ACK; active wave separate |
| BuildMenuInjector | building/unit menu materialization | one-shot/materialized | RESTART_REQUIRED until reversible reconcile proven |
| AerialSpawnSystem building sweep | Buildings | one-shot/materialized | RESTART_REQUIRED for existing system instances |
| FactionSystem | Factions / registry reference | retained registry consumer | classify/host-test before ACK |

## Denominator expansion required

Search every Runtime/Bridge/Aviation/UI/gameplay surface for:
- RegistryManager retention;
- typed IRegistry<T> retention;
- definition copied into ECS/component/live UI;
- static caches;
- one-shot flags;
- source-pack-derived asset caches;
- vanilla mappings/overrides;
- patch outputs.

Classify each as LOOKUP_ON_DEMAND, MATERIALIZED_RECONCILABLE, MATERIALIZED_NEW_ONLY, RESTART_REQUIRED, or GENERATION_INDEPENDENT.

## Non-core generation surfaces

Do not assume RegistryManager is exhaustive. Explicitly denominator-check:
- patched YAML cache;
- asset/material/audio/UI registries or loaders;
- vanilla mapping/override state;
- dependency graph;
- disabled-pack state;
- content hashes/receipts;
- generated Universe/pack artifacts where runtime-visible.

A generation cannot be called complete if one of these remains on G1 while the receipt reports G2 globally.

## Gate

Before a fully-active G2 receipt is allowed, every required domain/surface must either:
- prove G2 effective/observed/materialized according to its contract; or
- be explicitly generation-independent; or
- force PARTIAL/RESTART_REQUIRED.

Unknown consumer/domain = not fully active.
