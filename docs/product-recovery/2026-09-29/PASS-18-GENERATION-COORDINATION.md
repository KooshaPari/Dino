# Dino pass 18 — generation coordination discrimination

Date 2026-09-30.

Candidate `000db7b1dcd15573bf81321efb69187a5c0a72b5`; run `36703814791`; artifact `11091451179`; sha256 `6468ec9ca805606628d191140baf1c8f2e9cf372a6962604f342423ce1293f87`.

10 recovery tests executed: 7 passed, 3 failed.

The same three production reload controls remain red:
- removed content remains effective;
- identical reload accumulates conflict;
- removed patch retains stale patched value.

All seven architecture-prototype controls pass, now including:
- fresh-generation removal convergence;
- fresh-generation patch isolation;
- failed candidate retains prior published generation;
- content-derived generation ID stability/change;
- required-consumer NACK prevents fully-active receipt;
- two-phase activation retains prior active generation until all required consumers ACK;
- stale/wrong-generation consumer acknowledgement cannot qualify current generation.

This moves the preferred design beyond "fresh RegistryManager fixes a unit test": generation identity and consumer observation/activation state are independently modeled and discriminate correctly in the isolated SDK harness.

Still open before production migration:
- adapt actual PackUnitSpawner/WaveInjector/AerialSpawnSystem/BuildMenuInjector consumers;
- define existing-entity vs new-entity update semantics;
- include asset/domain registries outside core RegistryManager;
- machine reload receipt and path selection;
- licensed-host vertical evidence.
