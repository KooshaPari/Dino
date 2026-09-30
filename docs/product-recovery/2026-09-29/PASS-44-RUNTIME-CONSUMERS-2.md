# Dino generation denominator — runtime consumer pass 2

Date 2026-09-30.
Frozen source: `17119051e782b32615413049c1c3cd207f0b540e`.
Authority: RECOVERY SOURCE ANALYSIS.

This pass supersedes the shallow conclusion that Weapons/Projectiles/Doctrines/Skills/Squads merely lacked direct `.Get` consumers. Type/injection/materialization tracing found materially different states.

## WarfareContentLoader retains eight warfare registries

`WarfareContentLoader` constructor retains:
- Factions
- Units
- Buildings
- Weapons
- Projectiles
- Doctrines
- Waves
- Squads

and loads each corresponding directory into those retained registry instances.

Generation consequence: swapping RegistryManager or creating an isolated generation does not automatically rebind an already-constructed WarfareContentLoader. Whether the loader is long-lived after initial load, reconstructed per generation, or used only author-side must be traced at its mounted callers. Until then these eight registry references are generation-sensitive retained references.

## Projectiles — runtime materialization found

`ModPlatform` enumerates `_registryManager.Projectiles.All.Values` and applies `ProjectileDefinition.BoltColor` into `BlasterBoltConfig.SetFactionColorHex`.

`ProjectileMeshSwapSystem` later resolves bolt colors through `BlasterBoltConfig`, not by looking up the projectile registry.

Therefore projectile color is a **materialized secondary runtime owner**. G2 registry publication alone cannot qualify projectile visuals as G2. The activation contract needs:
- rebuild/reconcile BlasterBoltConfig from the candidate generation; or
- restart-required disposition; and
- removal semantics so a removed G1 projectile/color cannot survive as an unowned global override.

This is a concrete stale-materialization risk analogous to BuildMenu, not an unknown lookup.

## Doctrines — domain consumers found

DoctrineDefinition is consumed by:
- WarfarePlugin validation/enumeration of the doctrine registry;
- DoctrineEngine ApplyDoctrine/ApplyAll;
- BalanceCalculator methods taking DoctrineDefinition.

The remaining question is mounting/lifetime: determine whether live gameplay resolves doctrine per use, copies modified UnitStats at activation/spawn, or uses doctrine only in analysis/validation.

Provisional classification: **SUPPORTED DOMAIN, CONSUMER SCOPE OPEN**. Do not ACK generation until the live call path is known.

## Weapons — registered and retained, live realization still open

WeaponDefinition is:
- a RegistryManager domain;
- registered by ContentRegistrationService;
- retained/loaded by WarfareContentLoader.

No mounted live-game consumer was established in this pass. This is now **SUPPORTED DECLARATIVE DOMAIN / LIVE REALIZATION OPEN**, not merely unknown existence.

For the mature universal-modding thesis, a weapon schema that never changes actual host weapons is not a closed capability.

## Squads — registered/retained, live realization still open

SquadDefinition is retained/loaded by WarfareContentLoader and has registry/model tests. No mounted host consumer was established.

Classification: **SUPPORTED DECLARATIVE DOMAIN / LIVE REALIZATION OPEN**.

## Skills — stronger gap

RegistryManager advertises Skills and SkillDefinition explicitly claims mapping to DINO `Components.Skills.*` ECS components. Schemas/docs advertise skills as a warfare content type.

But:
- WarfareContentLoader's eight retained warfare registries omit Skills entirely;
- first ContentRegistrationService exact case search did not establish a skills registration case;
- first runtime search found model/schema claims, not a mounted ECS materializer.

This is a **capability realization gap candidate**, not generation coverage. Before treating Skills as a supported mature surface, prove:
1. ingestion into RegistryManager.Skills from a real pack path;
2. mapping from SkillDefinition to the named ECS component;
3. host-observed effect;
4. generation/reload semantics.

If those cannot be found, README/typed-registry support claims overstate realized modding breadth.

## Updated domain state

| Domain | Current recovered state |
|---|---|
| Units | live future-spawn consumer known; existing entity policy separate |
| Buildings | mixed materialized + lookup consumers |
| Factions | registry + FactionSystem materialized owner |
| Weapons | declared/loaded; live realization open |
| Projectiles | registry + materialized BlasterBoltConfig owner |
| Doctrines | domain consumers found; mounted gameplay lifetime open |
| Skills | advertised registry/ECS mapping; ingestion/materializer gap candidate |
| Waves | live queue/active-wave consumer known |
| Squads | declared/loaded; live realization open |
| FactionPatches | patch/effective-registry semantics; stale-removal control already reproduced |

## Next execution

Trace mounted constructors/callers for WarfareContentLoader, WarfarePlugin, DoctrineEngine, BalanceCalculator and any skill bridge. Then add generation consumer dispositions to the machine receipt contract. No domain gets a green merely because its DTO/schema/registry tests pass.


## Pass-44 correction — mountedness after caller search

Deeper caller search changes the interpretation of WarfareContentLoader/WarfarePlugin:

- `new WarfareContentLoader` is found only in WarfareCoverageTests plus the class itself.
- repository orphan analysis records `WarfarePlugin` with `prod_refs: 0`.
- no production constructor/mount for WarfareContentLoader was established.

Therefore these are **implemented domain machinery / currently unmounted in the recovered production graph**, not live runtime consumers.

Consequences:
- Weapons/Squads/Doctrines do not receive runtime-generation credit from WarfareContentLoader.
- DoctrineEngine/BalanceCalculator functionality is not a gameplay consumer unless a mounted production caller is established.
- The existence of substantial tested domain code can coexist with a product husk: implementation breadth without a closed author-to-host journey.

Projectiles remain different: ModPlatform directly enumerates RegistryManager.Projectiles and materializes bolt color into runtime config, so that path is production-mounted subject to host evidence.

Skills remain a stronger realization gap: advertised typed registry + ECS mapping claim, but no mounted ingestion/materializer was found in this pass.

This correction supersedes any wording above that called WarfareContentLoader a runtime-retained consumer.
