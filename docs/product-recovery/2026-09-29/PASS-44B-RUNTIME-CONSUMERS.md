# Dino runtime consumer pass 2 — declarative vs mounted surfaces

Date 2026-10-01.
Frozen source: `17119051e782b32615413049c1c3cd207f0b540e`.
Authority: RECOVERY SOURCE ANALYSIS.

## Important correction

The earlier first-pass absence of direct `RegistryManager.<Domain>.Get` calls did not mean the remaining domains were unused. Deeper search found indirect consumers and retained typed registries.

## WarfareContentLoader

`WarfareContentLoader` retains typed registries for Factions, Units, Buildings, Weapons, Projectiles, Doctrines, Waves and Squads plus an ArchetypeRegistry. Source search found construction only in tests, not a mounted production construction site.

Classification: POTENTIAL/STAGED DOMAIN LOADER; production mounting unresolved. If made long-lived, it must be generation-scoped or rebound or it can retain G1 registries.

## Projectiles — real materialization found

`ModPlatform.ApplyBlasterBoltConfig()` iterates Projectiles, resets global BlasterBoltConfig defaults, then materializes projectile bolt colors into static faction color config.

Generation qualification still needs:
- execute derivation against G2;
- removed G1 override returns to default/G2 value;
- cached recolored materials do not retain stale G1 visuals.

Projectiles therefore have registry -> static config -> live/cached material layers. Registry ACK alone is insufficient.

## Doctrines

`WarfarePlugin` enumerates Doctrines for pack validation; DoctrineEngine / BalanceCalculator consume DoctrineDefinition in warfare calculations. Mounted lifetime remains to be traced.

Classification: DOMAIN CONSUMERS FOUND / PRODUCTION LIFETIME UNRESOLVED.

## Skills

SkillDefinition/schema explicitly claim mapping to DINO `Components.Skills.*` ECS components, but this pass found model/schema/tests rather than a mounted RegistryManager.Skills applicator.

Classification: DECLARED_RUNTIME_SURFACE / MOUNTING_UNPROVEN.

For the universal-modding thesis this is a realization gap, not merely a reload gap.

## Squads

WarfareContentLoader retains Squads, but its production construction is unproven; tests/serialization/registry coverage exist.

Classification: DECLARATIVE_DOMAIN / PRODUCTION_CONSUMER_UNPROVEN.

## Weapons

WarfareContentLoader retains Weapons; content registration/model validation exist. Direct mounted runtime effect remains unproven.

Classification: DECLARATIVE_DOMAIN / PRODUCTION_CONSUMER_UNPROVEN.

## Four-stage realization gate

For every declared mod surface distinguish:
1. DEFINITION_ACCEPTED;
2. PRODUCTION_MOUNTED;
3. RUNTIME_EFFECT_PROVEN;
4. GENERATION_TRANSITION_PROVEN.

A YAML/model/schema/registry/test can establish stage 1 while the product remains a husk for stages 2-4.
