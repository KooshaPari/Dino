# Final non-execution contract and gate matrix — DINOForge

Date: 2026-10-01. Program gaming-pair-20260929.
Scope: all specification/documentation/test-design/research/trace layers that can be closed without a licensed running DINO host. This document does NOT waive execution evidence.

## Mature product contract

DINOForge is a general DINO modding platform/framework whose mature falsification journey is a substantial total conversion without maintaining a fork of DINO solely because required mod surfaces are closed.

### Product pillars

D-P1 Content model: typed/versioned definitions for every accepted mod surface.
D-P2 Composition: pack identity, dependencies, precedence, conflicts, patches/overrides and effective generation.
D-P3 Host adaptation: exact DINO version/install/profile/world mapping.
D-P4 Runtime realization: definitions become actual live-game behavior/materialization.
D-P5 Lifecycle: install/enable/activate/reload/restart/disable/update/rollback.
D-P6 Assets/presentation: rights-aware asset identity/import/build/runtime realization.
D-P7 Author interfaces: schemas/docs/CLI/machine APIs/diagnostics.
D-P8 Player/operator interfaces: discover/manage/configure/recover with truthful state.
D-P9 Extensibility: plugins/domain adapters without hardcoding one specimen pack.
D-P10 Evidence/operations: candidate/config/host/generation/consumer-bound receipts, releases, compatibility/support.

GenerationStore is spine infrastructure across P2/P4/P5/P10, not a pillar substitute.

## Domain realization matrix

Every declared content domain must end in:
DEFINITION_ACCEPTED -> PRODUCTION_MOUNTED -> RUNTIME_EFFECT_PROVEN -> GENERATION_TRANSITION_PROVEN.

Current denominator:
- Units: mounted consumers found; host transition proof open.
- Buildings: BuildMenu/Aerial/economy consumers; mixed materialization; host transition proof open.
- Factions: Registry + FactionSystem materialized owner; host transition proof open.
- Weapons: definition/loader surfaces; mounted runtime effect unproven.
- Projectiles: runtime static bolt-color materialization found; cache/live visual transition proof open.
- Doctrines: validation/calculation consumers found; production lifetime/mounted game effect open.
- Skills: declared ECS mapping; mounted applicator unproven.
- Waves: mounted WaveInjector; queued vs active transition proof open.
- Squads: declarative/loader surfaces; production consumer unproven.
- FactionPatches: composition/effective-definition semantics; runtime consequence and removal proof open.
- Archetypes and non-core assets/overrides/caches: independent generation surfaces; cannot be hidden by ten-registry count.

No domain gets mature credit merely for schema/model/registry tests.

## Journeys

D-J01 Total-conversion authoring: discover supported surface -> author pack -> validate -> compose -> install -> activate -> observe actual game effect -> revise.
D-J02 Rejection/recovery: malformed/incompatible/conflicting candidate -> explicit rejection -> previous accepted game state remains usable.
D-J03 Lifecycle continuity: active pack -> scene/restart -> reconnect to exact current world -> expected configuration/effects converge.
D-J04 Composition: two independent packs -> dependency/override resolution -> expected effective definitions -> expected live effects.
D-J05 Removal: G1 content/patch/materialization -> G2 removes it -> no stale registry/config/cache/live artifact according to declared existing-object policy.
D-J06 Total-conversion breadth: specimen conversion exercises every surface necessary for a substantially different game; unsupported surface is explicitly reported, not silently ignored.
D-J07 Player management: identify active exact packs/version/config -> change/disable -> receive truthful effect/restart requirement.
D-J08 Agent/machine management: same semantics as human path, with structured errors/receipts and no privileged false-green ingress.

Stage closure:
- CVP requires J01 narrow + J02 + J03 on one host/profile.
- MVP adds J04/J05 and routine management.
- Beta adds declared support matrix, migration/update/rollback, broad diagnostics.
- GA requires externally usable declared scope.
- Mature requires accepted total-conversion breadth/J06 and maintained support.

## Cross-cutting invariants

D-I01 Exact subject: pack bytes/digest, host install/build/profile, desired/effective generation and world/scene are bound.
D-I02 No partial green: required NACK/unknown/restart-required prevents fullyObserved.
D-I03 Removal symmetry: activation has defined deactivation/removal semantics.
D-I04 Authority: registry truth cannot override contradictory live materialization.
D-I05 Atomic acceptance: candidate existence != accepted active generation.
D-I06 Existing/new object distinction is explicit for materialized game state.
D-I07 Commodity substrate is reused unless DINO-specific need survives bootstrap gate.
D-I08 Unsupported declared surfaces cannot be marketed as realized.
D-I09 Evidence cannot be rebound across candidate/config/host.
D-I10 Worker/development state cannot become product truth.

## Oracle catalogue

For each domain:
1. positive G1 realization;
2. changed G2;
3. removed G2;
4. invalid G2;
5. repeated identical G2;
6. dependency/conflict;
7. existing-object disposition;
8. restart convergence;
9. stale-cache/materialization negative;
10. wrong host/path/world negative.

Composition oracle uses independently computed expected effective graph.
Runtime oracle checks actual host state/effect, not only RegistryManager.
Visual/material domains require runtime capture/state probe bound to same run.
Machine receipt oracle rejects missing/skipped/collector-failed/wrong-candidate evidence.

## Trace skeleton

USER INTENT universal modding / total conversion
-> pillars P1-P10
-> domain realization rows + journeys
-> design: effective immutable generations + explicit consumer dispositions
-> implementation: SDK registries/loaders, ModPlatform, bridge, runtime consumers, assets/interfaces
-> tests: recovery controls + candidate tests + future game-host suite
-> evidence: exact run/artifact/host receipt
-> runtime: selected DINO process/world.

Every edge records authority/provenance. An LLM-inferred consumer edge remains inference until source/host verified.

## Architecture decisions frozen at non-exec layer

1. Retain commodity BepInEx/Harmony-class substrate.
2. DINOForge owns DINO-specific typed/product semantics.
3. Effective generation is immutable candidate identity; global mutable registries are not sufficient activation truth.
4. Consumers declare lookup/materialized/restart behavior.
5. Path selection is canonicalized and authorized before loading.
6. Receipt schema binds requested/resolved path and desired/active/observed generation.
7. A total conversion is the mature breadth falsifier.
8. Runtime breadth claims require mounted/effect evidence.

## Remaining non-execution blockers

These cannot responsibly be resolved from current evidence alone:
- explicit user authority for some historical catalog requirements;
- final supported DINO versions/platforms/distribution policy;
- legal/redistribution policy for third-party game/assets;
- exact stable support/SLA expectations.

They are product decisions, not missing prose.

## Execution blockers that prevent overall 100%

- licensed GameInstalled=true integration;
- real ModPlatform path-aware generation coordination;
- host qualification for all mounted/materialized consumers;
- total-conversion vertical;
- scene/restart/removal/failure journeys;
- external user/pilot comparison.

Therefore: non-execution contract is structurally closed subject to the named authority decisions; repository is NOT overall spec/design100% under the original gate because architecture high-risk runtime unknowns remain experimentally open.
