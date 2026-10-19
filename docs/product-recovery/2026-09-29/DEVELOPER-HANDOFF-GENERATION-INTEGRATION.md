# Developer-agent handoff — Dino generation integration

Date 2026-09-30. Status: READY FOR GAME-HOST IMPLEMENTATION TRACK. This is not merge authorization.

## Immutable evidence baseline

Frozen source analyzed: `17119051e782b32615413049c1c3cd207f0b540e`.

Canonical recovery/spec PR: #490, branch `spec/mature-recovery-20260929`.
Implementation experiment PR: #491, branch `experiment/generation-store-20260930`.

Host-independent candidate qualified:
- candidate `50d677b8b1074a4b27f12374cd9b252b14d9b83f`
- run `36717763618`
- artifact `11097236544`
- sha256 `00e622ed9936da92b2ac2fe4535adcdb3149b986a139de3292da8d8eb33db23d`
- conclusion: success.

Earlier production controls remain red for stale removed content, identical-reload conflict accumulation, and stale patched YAML. Do not delete/weaken those controls.

## Accepted implementation direction

Use isolated candidate generation + explicit publication/consumer observation. Do not return to append-only global-registry semantics.

Host-independent candidate already contains:
- GenerationStore / consumer result semantics;
- ACK/NACK/restart-required distinction;
- receipt path/generation fields;
- PackRootResolver;
- conservative PackRootPolicy.

## Developer-agent assignment

1. Refactor ModPlatform pack loading so root selection is separate from one shared load/apply implementation.
   - default path resolves configured root;
   - explicit path passes PackRootPolicy;
   - do not duplicate current LoadPacksImpl.
2. Make GameBridgeServer.HandleReloadPacks parse caller path and bind RequestedPath/ResolvedPath.
3. Connect candidate generation construction and GenerationStore proposal only after the resolved root is fixed.
4. Apply real consumers:
   - PackUnitSpawner: future lookups only;
   - WaveInjector: future queued definitions only; active wave semantics explicit;
   - BuildMenuInjector: RESTART_REQUIRED until reversible reconciliation is independently proven;
   - Aerial building sweep: RESTART_REQUIRED for existing one-shot materialization.
5. Return exact activation receipt. Success=true alone is forbidden as acceptance evidence.

## Required game-host tests

- G1 -> G2 changed content;
- G2 removed content;
- removed patch;
- repeated identical G2;
- malformed G2 retains G1;
- conflict retains/explicitly partials according to accepted policy;
- requested child pack root is actually used;
- outside-root request rejected;
- new spawn after G2 resolves G2;
- existing G1 entity disposition explicit;
- new/queued wave after G2 resolves G2;
- active G1 wave disposition explicit;
- BuildMenu/Aerial restart-required prevents fullyObserved green;
- restart converges to accepted generation.

## Forbidden shortcuts

- do not merge #491 to main;
- do not mark restart-required consumer ACK;
- do not silently mutate existing entities and call that migration;
- do not bypass PackRootPolicy;
- do not delete red recovery controls;
- do not claim licensed-host success from CI-safe pure tests.

## Completion evidence

Return exact commit, host/game version/install identity, requested/resolved root, generation IDs, consumer receipts, raw logs/screenshots where needed, restart result, and grader version. Update #491 and #490; PhenoRegistry indexes the receipt.
