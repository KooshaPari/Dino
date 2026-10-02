# Dino pass 2 — activation transaction and validation parity

Date 2026-09-29. Frozen source remains `17119051e782b32615413049c1c3cd207f0b540e`. This is a recovery finding, not native execution or accepted implementation change.

## Correction: schema-null is narrower than initially stated

`RegistryImportService` blob `f4e5d844acaeceec71e7a5943b5d7acd70818517` was read in full. The runtime-created service receives a null `ISchemaValidator`, so JSON-schema validation is skipped. However, after YAML deserialization, `RegisterItems<T>` calls `JsonGuard.ValidateOrThrow(item)`. Types implementing `IValidatable` therefore receive semantic validation even when schema validation is absent.

The prior finding is narrowed to **validation parity by content type and ingress**. It is incorrect to summarize the runtime as accepting arbitrary invalid content. Repository TRUTH_TABLE history itself records an incomplete IValidatable rollout at one point; current type-by-type coverage must be verified from actual model definitions, not inherited from that dated claim.

Required matrix for every supported content type: schema validation at compiler; semantic validation at runtime; cross-field/domain validation; patched-YAML path; normal full load; single load where supported; hot reload; UI/domain-specific loaders. A parse-valid fixture must violate one obligation at a time. Missing one layer is not automatically a defect if another accepted layer enforces the same semantic contract.

## Stronger finding: activation is not an observed transaction

The inspected runtime path mutates shared state incrementally:

1. `LoadPacksImpl` calls `ContentLoader.LoadPacks` against the existing RegistryManager.
2. RegistryImportService deserializes a list and calls `register(item)` after each individual item passes JsonGuard.
3. A later item can throw and be recorded as an error after earlier items from the same file have already registered.
4. LoadPacksImpl continues to initialize runtime systems and apply overrides even when `result.IsSuccess` is false; it logs "loaded N with errors" and proceeds.
5. `HotReloadBridge.HandlePackReloadFailed` explicitly says partial updates may have been applied and calls `ApplyRuntimeUpdates` whenever `UpdatedEntries.Count > 0`.
6. `ApplyRuntimeUpdates` re-applies StatModifierSystem and raises `OnRuntimeUpdated`.

This does not prove every failure leaves harmful state, but it **does falsify any assumed all-or-nothing activation model** for the inspected paths.

### Contract consequence

REC-DINO-ACTIVATION/LIFECYCLE must not casually require rollback unless rollback is an accepted product decision. The product has two legitimate design alternatives to resolve:

**A. Transactional activation** — build/validate an isolated candidate registry/effective graph, then atomically publish it. On failure, retain the last accepted registry/runtime state.

**B. Explicit partial activation** — define which independently valid entries may commit, which dependencies must remain atomic, how UI communicates partial state, and how disable/reload/restart converges. A pack cannot be reported simply "active" if its required subset failed.

A third implicit alternative — mutate partially and report aggregate errors without a precise effective-state contract — cannot support trustworthy automation or user recovery.

## Disabled-pack filesystem mutation

LoadPacksImpl temporarily renames disabled pack directories to `.disabled`, invokes loading, then attempts to rename them back in a finally block. Failure to rename back is logged as warning. This is a product-state mutation at load time, not merely discovery filtering. It requires interruption/restart and collision tests: process termination between rename and restore; existing `.disabled` destination; permission failure; symlink/canonical path behavior; two loaders; watcher observing the temporary move.

No destructive test was run here.

## Hot-reload semantic gap

`HotReloadBridge` states that direct per-entity surgical updates need a future bidirectional entity-to-registry index and currently uses a reapply-all approach for stat modifiers. This is an explicit implementation limitation. A registry update is therefore not sufficient evidence that every affected live entity/content kind changed. The mature contract should model content-type-specific hot-reload support and fallback/restart requirements rather than one boolean "hot reload supported".

## Next falsification set

- Multi-item YAML: first item valid, second semantically invalid. Observe registry/effective game state after failure.
- Multi-file pack: valid unit file followed by invalid required building/projectile/dependency. Observe aggregate pack state and UI label.
- Hot reload a valid active pack into a partially invalid version. Observe registry, existing entities, newly spawned entities and restart convergence.
- Kill process after disabled-pack rename and before restore; relaunch and observe recovery.
- Change patched YAML into schema-invalid but semantically-deserializable content; compare compiler/runtime/hot-reload decisions.
- For every content type, mutate schema-only, IValidatable-only and cross-content constraints independently.

Native host execution remains required. This pass closes neither D-F01 nor D-F02; it makes them more precise.
