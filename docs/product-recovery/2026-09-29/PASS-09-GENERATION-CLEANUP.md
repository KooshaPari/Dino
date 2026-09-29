# Dino pass 9 — generation cleanup falsification

Date 2026-09-29. Frozen source `17119051e782b32615413049c1c3cd207f0b540e`.

Repository-wide source search for `Unregister(` and `Clear()` found unregister/clear APIs in several **domain-specific** registries (UI menus/themes/HUD, economy profiles/routes/resources, scenarios) and unrelated caches. It did not reveal an unregister-by-pack/generation operation on the core `IRegistry<T>` / `Registry<T>` used by `ContentLoader`.

That distinction matters: the existence of an Unregister method somewhere in the repository is not evidence that pack reload removes core unit/building/faction/weapon registrations.

The dedicated recovery workflow is now candidate-bound and fail-closed:
- candidate: `6f494343934bb37d8ff9211ab2792cac9624210e`;
- run: `36622441461`;
- targeted scope: `RecoveryGenerationOracleTests`;
- runner: windows-latest;
- raw TRX + receipt uploaded on completion;
- real-game workflow on this candidate is skipped, so no native host qualification.

Current execution state at receipt time: workflow in progress, no pass/fail inferred.

The stale patch test now uses the exact PatchSet YAML shape copied from `PatchApplicatorIntegrationTests.cs`: target_pack, operations, op=replace, JSON-pointer-style path and value.

No production cleanup primitive has been added. If the tests fail after successful compilation, architecture decision follows; if they pass, trace the hidden mechanism that made them pass and update the model.
