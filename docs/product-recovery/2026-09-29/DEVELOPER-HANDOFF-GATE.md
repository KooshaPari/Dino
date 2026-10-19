# Dino developer-agent handoff gate

Date2026-09-30. This file answers when implementation agents may take over without rediscovering the product.

## READY NOW — bounded experimental work only

A developer agent may:
- run/extend isolated SDK recovery oracles;
- implement a **test-only** stable facade/generation builder prototype;
- instrument consumer observed-generation acknowledgements in test doubles;
- inspect/mapping work.

It may NOT yet:
- rewrite production ContentLoader/RegistryManager;
- claim hot reload fixed;
- merge spec PR;
- broaden to another product.

## Preconditions for production integration handoff

1. All three current production controls remain reproducibly red on the bound baseline.
2. Fresh-generation prototype passes:
   - removal convergence;
   - identical candidate idempotence;
   - patch removal/current bytes;
   - failed candidate retains prior generation.
3. Consumer matrix covers every materialized runtime consumer.
4. PublicationReceipt schema accepted for candidate/effective/observed generations.
5. Partial-vs-atomic activation policy resolved.
6. reloadPacks target-path semantics resolved.
7. Native licensed-host vertical slice plan identifies actual install/profile/world and evidence collector.

## Proposed implementation work packages after gate

D-GEN-01: stable RegistryManager facade/current-generation handle, preserving public shape where feasible.
D-GEN-02: candidate builder from complete pack set with generation-scoped import/patch state.
D-GEN-03: publication receipt + exact semantic delta.
D-GEN-04: migrate lookup-on-demand consumers.
D-GEN-05: migrate materialized consumers with per-type semantics.
D-GEN-06: bridge/MCP subject identity and observed-generation receipts.
D-GEN-07: native host vertical + restart/recovery.
D-GEN-08: regression/migration/compatibility cleanup.

Each WP must keep the existing red controls and add its own negative controls. No WP gets product acceptance from compile/test existence alone.
