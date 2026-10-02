# Dino pass 20 — real runtime consumer generation classes

Date 2026-09-30. Frozen source inspected: `17119051e782b32615413049c1c3cd207f0b540e`.

## Consumer classes are not equivalent

### Lookup-on-demand consumers

`PackUnitSpawner`
- static `RegistryManager? _registry`;
- `Initialize(registry)` replaces the retained reference;
- queued spawn requests later look up definitions from that registry.

`WaveInjector`
- static `RegistryManager? _registry`;
- `SetRegistryManager(registry)` replaces it;
- later wave processing resolves definitions against the retained registry.

For these consumers, a generation adapter can plausibly rebind the registry for **future requests** and ACK the generation after the reference swap plus any required validation. Existing already-spawned ECS entities are a separate materialization domain and must not be implied updated.

### Materializing / one-shot consumers

`BuildMenuInjector`
- retains a registry reference;
- `Initialize` resets `_done=false`;
- `RunInjection` materializes a cached `_registrations` list and modifies live menu structures;
- subsequent RunInjection calls are no-op while `_done` is true.

Therefore generation application is not a pointer swap. It requires an explicit reinjection/reconciliation policy against live UI and must distinguish pack registration cache from actual live menu mutation.

`AerialSpawnSystem`
- retains a registry;
- has a per-system `_buildingSweepDone` one-shot materialization;
- source comments explicitly say the sweep runs once.

Rebinding its static registry does not reset an existing system instance's `_buildingSweepDone`. Existing building ECS materialization therefore needs an explicit generation-change action or restart-required classification.

## Generation consumer contract

Every runtime consumer must declare one of:
- LOOKUP_ON_DEMAND — rebind current generation for future operations;
- MATERIALIZED_RECONCILABLE — compute/apply delta and ACK only after live state reconciles;
- MATERIALIZED_NEW_ONLY — new entities/actions use G2; existing materialization remains G1 by explicit contract;
- RESTART_REQUIRED — cannot safely adopt G2 live;
- GENERATION_INDEPENDENT.

ACK means that consumer's declared semantics are satisfied, not merely that a setter returned.

Receipt must include per-consumer:
- consumer id;
- desired generation;
- observed/applied generation;
- disposition above;
- affected scope (future requests, existing entities, UI, etc.);
- ACK/NACK/restart-required;
- evidence identity.

## Next prototype

Create SDK/runtime-neutral adapter fakes for one lookup consumer and one materialized consumer:
1. G1 active.
2. Propose G2.
3. lookup consumer rebinds and ACKs G2.
4. materialized consumer NACKs/restart-required or completes explicit reconcile.
5. global receipt remains not-fully-active until required policy is satisfied.
6. existing materialization must never be silently represented as G2 merely because future lookups use G2.

Then map the remaining runtime consumer denominator before changing production classes.
