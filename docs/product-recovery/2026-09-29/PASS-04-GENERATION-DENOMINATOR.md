# Dino pass 4 — generation replacement denominator

Date 2026-09-29. Frozen source `17119051e782b32615413049c1c3cd207f0b540e`. Source evidence only.

## Registry identity cannot distinguish pack generations

`RegistryEntry<T>` blob `7895f4f012fb71b6bd5f80c1b5f630bede91ccfb` carries:
- Id;
- Data;
- Source;
- Priority;
- SourcePackId.

It has no pack version, artifact digest, activation generation, registration timestamp, or unique registration identity. Priority is only source tier ×1000 plus load_order.

`RegistryManager` owns ten typed registries and exposes no generation/commit object. The inspected `Registry<T>` exposes no remove-by-source-pack operation.

This strengthens D-F05: after reload, old and new registrations from the same SourcePackId/load_order are indistinguishable by effective priority. The system can audit that they came from the same pack ID but not which candidate bytes/version produced each entry.

## Hot reload's updatedEntries are pack IDs, not actual effective deltas

`PackFileWatcher.ExecuteReload` calls ReloadPack for affected pack roots. On success it adds `loadResult.LoadedPacks` to `updatedEntries`. Those are pack IDs. It does not compute which registry entries changed, were removed, lost precedence, or failed to update.

HotReloadBridge later uses UpdatedEntries as the trigger to reapply runtime state. Thus "updated entry" naming overstates the evidence: the receipt is pack-level load outcome, not an exact semantic delta.

Mature evidence must identify old generation, candidate generation, effective changed IDs/content types, removals, conflicts, and runtime application outcome.

## D-F08 — file deletion/rename is structurally dangerous for reload

PackFileWatcher watches Changed/Created/Renamed in the inspected dispose wiring; deletion handling is not observed there. Even when a changed path resolves to a pack root and reload occurs, a content ID removed from disk has no observed unregister path in Registry<T>.

Required controls:
- delete a YAML file from active pack;
- rename file within pack;
- remove one item from a multi-item YAML;
- remove entire pack directory;
- disable pack;
- compare raw/effective registries and live game state.

The correct outcome may require restart for unsupported hot removals, but the system must say so explicitly rather than leaving stale state presented as current.

## D-F09 — patch cache identity is path-only

RegistryImportService's patched cache is keyed by absolute file path. No source pack version, patch-set digest or effective-generation identity is part of the key. This makes path reuse across generations unsafe unless the cache is rebuilt/cleared.

The minimal architecture repair to prototype is not necessarily a large transaction system:
1. discover immutable candidate pack set;
2. compute patch result into a generation-scoped import context;
3. build generation-scoped raw registries;
4. derive effective projection/conflicts;
5. validate required dependency closure;
6. publish a generation handle;
7. runtime consumers acknowledge/application-report against that generation;
8. retire prior generation only when policy allows.

This preserves intentional multi-source conflict history while preventing stale generations from masquerading as current.

## Evidence identity addition

For Dino activation evidence, bind at least:
- pack ID + declared version;
- exact pack/artifact digest;
- dependency graph digest;
- patch-set/effective-content digest;
- activation generation ID;
- host/install/profile/world generation;
- runtime build;
- criterion and verifier.

SourcePackId alone is insufficient.

## Next

Trace UI/asset/domain registries that may bypass core RegistryManager, all deletion/disable pathways, and actual game consumers that cache registry-derived values. Then implement/run a small isolated registry-generation experiment before touching the licensed host.
