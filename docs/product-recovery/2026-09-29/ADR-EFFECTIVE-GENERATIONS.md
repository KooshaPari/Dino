# Dino architecture decision record — effective generations

Date 2026-09-30. Status: PROVISIONAL, experiment-backed; not accepted implementation mandate.

## Evidence forcing the decision

Recovery run 36628553200 on candidate 8cfc9986 reproduced:
- removed content remains effective after successful same-pack reload;
- identical reload creates a conflict.

Core registry entries lack generation identity/removal by source generation. Machine reload receipts also lack exact semantic delta.

## Alternatives

### A — mutate global registries + add UnregisterByPack

Smallest patch. Before LoadPack, remove all registrations attributed to SourcePackId, clear patch cache, then re-register.

Pros: minimal change.
Cons: destructive gap between removal/reload; failure can erase last accepted state; cross-pack dependency/patch precedence can transiently break; hard to make atomic; runtime consumers can observe mixed generations; SourcePackId alone cannot distinguish artifact/version.

Disposition: REJECT as mature architecture, acceptable only as temporary experiment behind a lock/snapshot if it never publishes intermediate state.

### B — clone current registry, mutate clone, swap on success

Candidate registry starts from current effective/raw state, removes prior source-pack generation, applies candidate, validates, then replaces RegistryManager reference.

Pros: smaller conceptual change; can preserve old state on failure.
Cons: clone correctness across every typed/domain registry/cache; runtime consumers may hold references to old typed registries; patch/dependency identity still needs explicit generation metadata.

Disposition: ADAPT/EXPERIMENT.

### C — immutable generation build + effective projection + publication handle

Build candidate generation from the complete declared pack set and patch graph in isolated state. Each generation binds pack/artifact/version/dependency/patch digests. Derive effective registries/conflicts. Publish one generation handle. Runtime consumers report observed/applied generation; old generations remain historical/evidence objects and are retired from effective lookup.

Pros: directly solves stale removals, idempotence, exact evidence identity, rollback and MACE observed-generation problem; naturally rebuilds path-only patch cache per generation.
Cons: larger migration; domain registries and live ECS consumers need adapters; memory/cost of candidate generation; partial activation requires explicit design.

Disposition: PREFERRED PROTOTYPE.

### D — event-sourced incremental registry

Record register/unregister/patch events and derive current state.

Pros: rich audit/history.
Cons: substantially more machinery; still needs snapshots/generation publication and semantic ownership; no evidence the product needs event sourcing itself.

Disposition: REJECT for now; immutable receipts/history can exist without event-sourcing core state.

## External patterns

Kubernetes Server-Side Apply tracks field ownership/conflicts and removes fields omitted by their last manager when no other manager owns them. This is useful prior art for source ownership/removal semantics, not a library to embed. Kubernetes also uses observedGeneration to distinguish desired generation from what a controller has actually reflected. citeturn0search3turn0search4

TUF snapshot metadata provides a useful conceptual analogue for a consistent view of a set of versioned metadata/files and prevents mixing states from different repository times. DINO does not need to implement TUF merely to get generation consistency. citeturn0search2

## Prototype contract

Generation G contains:
- generation_id = digest of normalized candidate inputs + framework compatibility profile;
- complete pack artifact identities;
- dependency graph;
- patch graph/result;
- raw registrations with source generation;
- effective registries/conflicts;
- validation findings;
- activation disposition.

Publish only after candidate construction completes. Runtime consumers expose `observed_generation`. A reload receipt cannot claim fully active until required consumers acknowledge G. Removed fields/content disappear because G is rebuilt from declared current intent, not appended to G-1.

Partial activation, if retained, must identify which capability/content closure belongs to G and what remains on G-1; it cannot be one ambiguous global "success".

## Experiment before production migration

Implement a small SDK-only `RegistryGenerationBuilder` for Units first, with two packs and patch graph. Re-run the three recovery oracles plus:
- failed v2 retains G1;
- identical v2 produces same semantic digest/effective result;
- removing patch/content changes G digest and removes prior effect;
- conflicting second pack yields explicit conflict without corrupting published G;
- simulated consumer observedGeneration lags until acknowledgement.

Do not migrate every domain until this prototype survives.
