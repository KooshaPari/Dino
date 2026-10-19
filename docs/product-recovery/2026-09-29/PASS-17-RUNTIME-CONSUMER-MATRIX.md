# Dino pass 17 — runtime generation-consumer matrix

Date2026-09-30. Frozen source17119051. Source trace extends the SDK generation prototype into live-runtime semantics.

## Consumer classes

| Consumer | Registry interaction | Generation behavior inferred from source | Required G1->G2 policy |
|---|---|---|---|
| PackUnitSpawner | Static RegistryManager reference; unit lookup at request processing time | Lookup-on-demand. With stable facade it can naturally use newly published definitions for future spawn requests. Existing spawned entities are separate state. | Future spawns use G2; existing entities require explicit retain/reapply policy. |
| WaveInjector | Static RegistryManager; lookup WaveDefinition when request begins, then stores Definition inside ActiveWave | New waves can use G2; already-active waves retain the materialized G1 definition. | Explicitly allow active waves to finish on creation generation or migrate/cancel them; receipt records disposition. |
| FactionSystem | Materializes static FactionRuntime dictionary once; `_initialized` causes later InitializeFactions to skip | Does NOT follow registry generation after initialization. | Needs generation-aware rebuild/delta application; preserve mutable runtime counters/ownership separately from definition data. |
| AerialSpawnSystem | Registry lookup during a building sweep guarded by `_buildingSweepDone`; system feature gate currently off by default | One-shot materialization; a new generation will not automatically re-sweep. | Restart-required or generation-aware selective rescan. Do not claim G2 observed while old components remain. |
| BuildMenuInjector | Retains RegistryManager plus materialized registration list / one-time state (source inspected partially) | Likely materialized consumer, not automatically generation-safe. | Rebuild menu registrations or declare restart-required; exact cleanup trace still open. |
| HotReloadBridge | Holds RegistryManager and re-applies selected runtime updates | Existing bridge acknowledges pack-level changes, not exact generation/delta. | Consume PublicationReceipt and report per-consumer apply status. |
| Faction/asset/stat/domain plugins | Mixed | Must be classified individually. | No global observedGeneration until every required consumer has disposition. |

## Important semantic distinction

Generation publication does not imply retroactively rewriting every live object.

A valid mature policy can say:
- registry/config generation G2 is published;
- future unit spawns use G2;
- active wave W created under G1 completes under G1;
- existing unit entities retain G1 stats until explicit reapply/restart;
- menus/config projections rebuild to G2;
- unsupported one-shot host mutations require restart.

That is more truthful than pretending hot reload means universal instantaneous mutation.

## Evidence model

PublicationReceipt:
- candidate_generation;
- previous_generation;
- effective delta;
- registry publication status.

ConsumerObservation:
- consumer id/version;
- observed generation;
- disposition = APPLIED | RETAINED_OLD_FOR_EXISTING_OBJECTS | RESTART_REQUIRED | FAILED;
- subject IDs affected;
- raw evidence.

Overall "fully active" is a policy over required consumer observations, not merely ContentLoadResult.IsSuccess.

## New architecture risk

FactionSystem mixes definition state with mutable runtime state (EntityCount/ownership) in a static dictionary. A naive rebuild from G2 can destroy live mutable state. The migration must separate durable/runtime faction instance state from pack-provided faction definition, or perform a keyed merge preserving accepted mutable fields.

This is a concrete reason to avoid replacing every runtime cache wholesale.
