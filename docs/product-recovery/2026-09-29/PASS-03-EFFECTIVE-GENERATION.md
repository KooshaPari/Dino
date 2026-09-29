# Dino pass 3 — effective-generation ownership

Date 2026-09-29. Frozen source `17119051e782b32615413049c1c3cd207f0b540e`. Source analysis only; no native host run.

## Raw registrations are intentionally multi-version; effective state is the real transaction subject

`Registry<T>` blob `988f7ba40f55a470c4487d688517da9e52935166` stores a list of registrations per ID, sorted by priority. `Get` and `All` project the first/highest-priority entry. Equal-priority multi-source entries are detected as conflicts. Therefore "transactional activation" must not mean erasing all competing registrations: coexistence is part of the conflict model.

The state that needs an acceptance contract is an **effective generation**:
- discovered pack/version set;
- dependency graph and ordering;
- patch-set result;
- raw registrations attributed to exact source pack/version;
- conflict/precedence projection;
- runtime applications derived from that projection;
- active generation identity exposed to UI/bridge/evidence.

A failed candidate generation should either not publish, or publish an explicitly partial generation whose missing/failed obligations are queryable.

## D-F05 — reload registration provenance has no observed replacement primitive

At the frozen source, `Registry<T>` exposes Register/Override/Get/Contains/All/DetectConflicts. In the inspected class there is no remove-by-pack, clear, generation, or replacement operation. `ContentLoader.LoadPacks` reuses the same RegistryManager created by ModPlatform. `IPackReloadService.ReloadPack` calls `LoadPack` against that same loader/registry.

This creates a concrete source question: reloading a pack can append new registrations from the same source pack without first removing the old generation. Priority sorting alone cannot identify "newer version of the same pack" because RegistryEntry ordering is priority-based, not generation-based. Equal-priority old/new entries can coexist and conflict or leave old entries reachable if content was removed from the new version.

Do not claim the failure until native/unit reproduction, but the inspected API has no visible mechanism that would make replacement semantics automatic.

Required controls:
1. load pack A v1 with IDs x,y; reload v2 with x changed and y removed;
2. inspect all raw registrations, effective x/y, conflicts and source metadata;
3. reload same bytes repeatedly and prove state does not accumulate;
4. lower/raise load_order across reload and observe old-generation precedence;
5. disable/remove pack after activation and prove its entries cease to be effective.

## D-F06 — patched YAML cache appears generationless and persistent

`RegistryImportService` holds `_patchedYamlCache: Dictionary<string,string>`; `SetPatchedYaml` overwrites by absolute path. Source search found no `_patchedYamlCache.Clear` or ClearPatched API. `ApplyPatchPhase` returns immediately when no patches are declared.

Therefore a previously patched path can remain cached across a later LoadPacks/ReloadPack call even when the patch declaration is removed or target content changes. `LoadAndRegisterContent` prefers cached YAML over disk whenever the path key remains present.

This is a stronger candidate stale-generation defect than the generic null-schema issue. Reproduction:
- load target T + patcher P modifying T/path;
- remove/disable P or remove its patch declaration;
- change T/path on disk;
- reload;
- verify whether RegistryImportService still consumes old patched YAML.

Also test renamed/deleted files and pack-root changes. Cache identity should include effective generation or be rebuilt from scratch per load.

## D-F07 — downstream runtime publication continues after partial load

Pass2 established that ModPlatform initializes spawner/build menu/aerial systems and applies overrides after a partial ContentLoadResult. Combined with generationless raw registries, this means runtime consumers may observe a mixture of historical and candidate registrations unless another cleanup mechanism exists outside inspected paths.

The architectural experiment is now specific: build an isolated candidate effective registry from immutable inputs, compare it with current published generation, then swap publication only according to accepted partial/atomic policy. This can be prototyped without changing the public pack schema.

## Next source/runtime proof

Trace RegistryEntry identity fields and all consumers of RegistryManager.All/Get; inspect asset-swap and UI-domain registries for separate replacement semantics; run repeated reload/removal fixtures; trace disabled-pack toggle into registry cleanup; measure whether live ECS consumers retain old values after effective registry changes.

No implementation fix is committed in this pass.
