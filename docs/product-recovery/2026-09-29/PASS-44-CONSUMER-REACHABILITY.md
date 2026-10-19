# Pass 44 — generation consumer reachability pass

Date 2026-09-30.
Frozen source: `17119051e782b32615413049c1c3cd207f0b540e`.

## WarfareContentLoader is currently not a mounted runtime consumer

The class retains eight typed registries (Factions, Units, Buildings, Weapons, Projectiles, Doctrines, Waves, Squads) plus ArchetypeRegistry and can load pack directories into them.

However repository-wide constructor search found construction only inside `WarfareCoverageTests`; no production construction/mount was found.

Therefore:
- it is an implementation surface / possible future domain loader;
- it is **not evidence that those eight domains are mounted into the live product**;
- its retained registries do not currently create a live generation-staleness blocker unless a production mount is recovered elsewhere;
- mature universal-modding coverage cannot count its tests as runtime realization.

This is exactly why implementation presence and mounted product surface remain separate dimensions.

## Projectiles — real runtime materialization found

`ModPlatform` enumerates `_registryManager.Projectiles.All.Values` and applies each projectile's `BoltColor` into static/global `BlasterBoltConfig` faction colors.

Consequences:
- Projectiles are not merely declarative.
- Generation adoption must account for materialized static bolt-color state.
- Removal/change semantics need explicit reconciliation: applying G2 additions/changes without resetting colors removed from G2 can leave G1 visual state.
- ProjectileMeshSwapSystem later resolves colors from BlasterBoltConfig, so stale static config can remain user-visible after registry G2.

Classify this consumer as MATERIALIZED_RECONCILABLE candidate pending a reset/delta oracle; otherwise RESTART_REQUIRED.

## Doctrines — domain-level consumers exist, live mount still unresolved

DoctrineEngine/BalanceCalculator consume DoctrineDefinition values; WarfarePlugin enumerates registry doctrines for pack validation. First pass has not yet established whether those services are mounted into the live game runtime or tooling/domain-only paths.

Status: DOMAIN_CONSUMER_FOUND / LIVE_MOUNT_UNKNOWN.

## Weapons / Squads

WarfareContentLoader retains these registries but is itself unmounted in production search. No direct live Runtime consumer established yet.

Status: DOMAIN/LOADER_SURFACE_FOUND / LIVE_RUNTIME_CONSUMER_UNKNOWN.

## Skills

SkillDefinition claims mapping to DINO `Components.Skills.*` ECS types and schemas/docs advertise skills as a typed registry. First production search did not find a corresponding ContentRegistrationService `case "skills"` or a live mapper applying SkillDefinition into ECS.

Status: ADVERTISED_RUNTIME_MAPPING / IMPLEMENTATION_REALIZATION_UNPROVEN.

This is a mature-product gap candidate: universal moddability cannot count skills as supported merely because model/schema/registry/tests exist.

## Next execution

1. Reproduce projectile stale-removal semantics in a pure/static oracle where possible.
2. Trace ModPlatform application order and whether BlasterBoltConfig has a reset-to-default primitive.
3. Trace WarfarePlugin mount/callers.
4. Trace weapon references from UnitDefinition/vanilla bridge.
5. Trace skill mapper absence/falsify via component-map/override systems.
6. Trace SquadDefinition consumption from Wave/Unit spawning.

No domain receives a green from schema/registry existence alone.
