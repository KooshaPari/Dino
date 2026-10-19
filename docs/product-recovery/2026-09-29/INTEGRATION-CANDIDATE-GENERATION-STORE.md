# Dino integration-candidate contract — GenerationStore and runtime adapters

Date 2026-09-30. Status: READY FOR IMPLEMENTATION CANDIDATE, not merge approval.

## Evidence threshold reached

Candidate0124edd7aff871ca63d8f92fc4cd959bd3cbb5f4, run36706610552, artifact11091884496 sha25621578ca478df3651f1911e1d41826c60c9ce39b6c1f4bd5630e949233b186302:
-9 generation/coordination/consumer prototype greens;
-3 unchanged production reload reds.

The isolated architecture question is sufficiently closed to begin an integration candidate.

## Minimal production-shaped boundary

`GenerationStore`
- CurrentActive generation handle;
- Proposed generation handle;
- desired_generation;
- active_generation;
- per-required-consumer observed/materialized generation + disposition;
- BeginProposal(candidate);
- RecordConsumerResult(...);
- Commit only when activation policy is satisfied;
- Reject retains prior active generation;
- immutable receipt/history.

`IGenerationConsumer`
- ConsumerId;
- disposition: LookupOnDemand / MaterializedReconciliable / NewOnly / RestartRequired;
- ApplyGeneration(candidate, previous) -> structured result;
- result states ACK / NACK / RESTART_REQUIRED / PARTIAL with exact scope.

First adapters:
1. PackUnitSpawner: lookup-on-demand; rebind registry for future queued/new spawn resolution. Existing spawned entities explicitly out of scope.
2. WaveInjector: lookup-on-demand for future queued wave definition resolution; already-active waves require separate semantics because ActiveWave may have materialized data.
3. BuildMenuInjector: materialized; Initialize+RunInjection is not automatically safe reconciliation. Candidate adapter initially RESTART_REQUIRED unless a reversible/live-menu delta experiment proves reconciliation.
4. AerialSpawnSystem building sweep: initial adapter RESTART_REQUIRED for building materialization because instance one-shot flag is not reset by static registry rebind. New aerial-unit behavior may have separate semantics.

## Non-negotiable receipt

Reload/activation receipt must expose:
- requested path/root and actual resolved root;
- candidate generation id and exact pack/artifact identities;
- prior active generation;
- patch/dependency graph digest;
- semantic added/changed/removed IDs;
- conflicts/findings;
- each required consumer disposition/result;
- desired vs active vs observed/materialized generation;
- overall COMMITTED / REJECTED / PARTIAL / RESTART_REQUIRED;
- runtime/install/profile/world identity.

A generic Success=true cannot replace this receipt.

## Integration-candidate tests before licensed host

- G1 active, G2 removes content: candidate effective state correct; consumers see explicit removal.
- one lookup consumer NACK: G1 remains active.
- BuildMenu restart-required: receipt cannot say fully active.
- stale ACK for G1 while desired G2 rejected.
- consumer throws mid-apply: policy retains/rolls back to declared state; no false G2 green.
- requested pack root B must bind receipt to B, not silently platform default A.
- existing entity scope explicitly remains G1 or is reconciled according to declared adapter.

This is the handoff boundary for developer-agent implementation; it is not authorization to merge or claim runtime success.
