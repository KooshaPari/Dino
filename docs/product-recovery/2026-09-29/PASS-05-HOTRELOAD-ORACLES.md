# Dino pass 5 — existing hot-reload oracle audit

Date 2026-09-29. Frozen source `17119051e782b32615413049c1c3cd207f0b540e`. No native host execution.

## Existing hot-reload tests do not test semantic replacement

`src/Tests/HotReloadTests.cs` blob `00aa3f255966897653b2b34689fae425f710d01c` was inspected.

Its tested subjects include:
- HotReloadResult property shapes;
- debounce event behavior;
- watcher start/stop/dispose;
- concurrent enqueue;
- ReloadAll returns a non-null result for a manifest-only pack / empty directory.

It does **not** in the inspected file assert:
- old content removed after reload;
- changed content replaces old generation rather than accumulating;
- patch cache invalidation;
- dependency/conflict projection after version change;
- live game consumer update;
- deletion/rename behavior.

Coverage of `ReloadPack` as a one-line call to `LoadPack` therefore cannot establish replacement semantics.

## D-F10 — current unit test architecture is capable of reproducing stale generations without the game

This is useful: ContentLoader + RegistryManager + temp directories are already constructed in HotReloadTests. The first falsification suite can run without BepInEx/Unity.

Proposed exact fixtures:

### reload_replaces_same_pack_generation
1. pack v1 loads one UnitDefinition ID with value A;
2. reload same pack root with same ID value B;
3. inspect raw registry entries and effective Get;
4. require an accepted replacement invariant: exactly one current-generation candidate from that pack, or explicit historical entries excluded from effective conflict computation.

### reload_removes_deleted_entry
1. v1 contains x and y;
2. v2 contains x only;
3. reload;
4. y must be absent from current effective generation or reload must explicitly report hot-removal unsupported/restart-required. Silent stale y fails.

### reload_without_patches_does_not_reuse_previous_patched_yaml
1. target file T value A + patch P→B;
2. load and observe B;
3. remove patch declaration, change T→C;
4. reload;
5. effective value must be C (or explicit restart-required), never stale B.

### repeated_reload_converges
Load identical bytes N times. Effective state/conflict count must remain stable after first load. This catches generation accumulation without requiring arbitrary performance thresholds.

### deletion_event_semantics
Delete/rename active content while watcher runs. Either watcher triggers a correct generation rebuild or explicitly reports unsupported deletion. No event + stale state presented as current fails.

## D-F11 — current HotReloadResult terminology is semantically misleading

`UpdatedEntries` receives `ContentLoadResult.LoadedPacks`: pack IDs. It is not a list of registry entry IDs. Existing unit tests reinforce that by using `"my-pack"` and `"pack-a"`.

Before MACE consumes this field as evidence, rename/version the API or document it as UpdatedPacks and add an exact semantic delta separately. Otherwise an agent can interpret "updated entries" as proof that specific content was replaced when the receipt establishes only pack-level loading.

## Next

Inspect ContentLoadResult and bridge ReloadResult propagation to see how this ambiguity reaches CLI/MCP. Then add the isolated failing tests on the specification/experiment branch if repository policy allows tests there; no production behavior fix until expected semantics are accepted.
