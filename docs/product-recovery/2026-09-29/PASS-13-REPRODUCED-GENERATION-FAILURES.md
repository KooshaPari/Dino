# Dino pass 13 — reproduced generation failures

Candidate `8cfc998678d14bca3329083efa93f741aa5c7f5e`; recovery run `36628553200`; raw artifact `11061109680`, sha256 `4ef143c274a9c9d9ca63b8c3dea9a3f044a773d9837169ace5efeea6a45b8404`.

The isolated SDK-only recovery project compiled and executed 3 tests.

## D-BEH-01 CONFIRMED — removed item remains effective

Pack v1 registered x and y. Pack v2 removed y. The actual watcher-facing `IPackReloadService.ReloadPack` delegates to `LoadPack`. Reload returned without hard errors, but `Registry.Units.Get("recovery-y")` still returned the v1 UnitDefinition.

This confirms stale registration across successful same-pack reload in the isolated SDK path.

## D-BEH-02 CONFIRMED — identical reload manufactures conflict

Initial load of one pack/unit had zero conflicts. Three identical reloads produced one detected conflict. This confirms repeated candidate application is not convergent/idempotent at registry conflict semantics.

## Patch test UNCLASSIFIED, oracle repaired

The patch fixture failed before its stale-cache assertion because `ContentLoadResult.Errors` contains informational `[patch]` progress messages. Existing SmokeTests already filter those messages as non-hard errors. The recovery oracle now follows that repository precedent and will rerun.

Separate design finding: progress/info should not share an `Errors` channel if machine consumers are expected to reason reliably about success.

## Architecture decision now justified

Do not patch D-BEH-01/02 with ad-hoc special cases first. Prototype a generation boundary:
- construct candidate registry/import context separately;
- identify source pack/version/digest and patch-set digest;
- derive effective projection/conflicts;
- publish/replace current generation according to accepted atomic/partial policy;
- retire previous registrations from effective state;
- emit exact delta/receipt;
- verify repeated identical candidate is idempotent.

Alternative: explicit partial/historical registry with generation-aware effective projection. Either is acceptable if removal/reload semantics are explicit and the machine receipt identifies current generation.

No production fix committed yet.
