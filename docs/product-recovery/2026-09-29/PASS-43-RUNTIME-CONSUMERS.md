# Dino generation denominator — runtime consumer pass 1

Date 2026-09-30.
Frozen source: `17119051e782b32615413049c1c3cd207f0b540e`.
Authority: RECOVERY SOURCE ANALYSIS.

## Core domain denominator remains ten RegistryManager registries

Units, Buildings, Factions, Weapons, Projectiles, Doctrines, Skills, Waves, Squads, FactionPatches.

The absence of a direct `.Get` search hit is **not** generation-independence. It means runtime consumption is still unknown until callers/materialization are traced.

## Newly classified consumers

### Factions -> FactionSystem

FactionSystem is a real runtime system with its own static materialized `Dictionary<string,FactionRuntime>`, registered/initialized from ModPlatform.

Historical docs describe it as logical/cosmetic grouping and note its OnUpdate is effectively empty; that does not make generation adoption automatic.

Generation implication:
- RegistryManager.Factions may be G2 while FactionSystem's materialized static dictionary remains G1.
- FactionSystem must be classified as MATERIALIZED_RECONCILABLE, MATERIALIZED_NEW_ONLY, or RESTART_REQUIRED after source/host tracing.
- A global G2 receipt cannot ignore this second faction state owner.

### Buildings -> multiple consumers

Known consumers include:
- BuildMenuInjector live/menu materialization (already RESTART_REQUIRED candidate);
- AerialSpawnSystem one-shot building sweep (already RESTART_REQUIRED candidate);
- economy ProductionCalculator reads an IRegistry<BuildingDefinition> on demand.

Therefore the Buildings domain itself has mixed consumer semantics. One registry-level ACK is insufficient.

ProductionCalculator appears lookup-oriented, but whether its owning service retains an old typed registry/reference must still be traced.

## Still open domains

Direct runtime `Weapons.Get`, `Projectiles.Get`, `Doctrines.Get`, `Skills.Get`, and `Squads.Get` consumers were not found by the first exact search.

Do NOT interpret this as covered or unused.

For each, next search must include:
- constructor/Initialize injection of typed `IRegistry<T>`;
- RegistryManager property enumeration;
- copied definitions into runtime DTO/ECS/assets;
- deserialized references by ID from units/buildings;
- override/patch systems;
- domain services under `src/Domains`;
- UI/bridge/catalog snapshots.

If no mounted runtime consumer survives that search, classify as DECLARATIVE_ONLY / ORPHAN / FUTURE rather than silently treating it as a mature supported mod surface.

## Receipt consequence

A fully-active generation receipt eventually needs domain-level evidence, e.g.:
- Units: registry + spawner/new-entity scope;
- Waves: registry + queued/active-wave scope;
- Factions: registry + FactionSystem materialization;
- Buildings: registry + menu/aerial/economy consumers;
- each remaining supported domain: named consumers or explicit declarative-only status.

Unknown domain consumer = `fullyObserved=false` for any product claim that says the whole pack generation is active.
