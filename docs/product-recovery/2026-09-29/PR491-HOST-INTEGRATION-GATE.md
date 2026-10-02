# Dino PR491 next integration gate — host consumer qualification

Date 2026-09-30. Applies only to experiment/generation-store-20260930.

Pure GenerationStore candidate is qualified by run36715187076:4/4 passed, artifact11096300343 sha256 b43844c7314a57c7bb76b9ec99c047450b0ad6f63d93c4bf50b4be7c38ff2c6f.

Before wiring ModPlatform reload to GenerationStore, the following must be proven on a GameInstalled=true build/host:

1. PackUnitSpawner adapter:
   - G1 spawn resolves G1 definition;
   - apply G2 lookup adapter;
   - new spawn resolves G2;
   - existing G1 entity remains explicitly G1 unless a separate reconciliation contract exists;
   - receipt says future-lookups scope, not all-world scope.

2. WaveInjector:
   - queued/new wave after G2 uses G2 definition;
   - already-active/materialized wave behavior is explicitly retained or reconciled;
   - receipt scopes ACK accordingly.

3. BuildMenuInjector:
   - current candidate returns RESTART_REQUIRED; verify runtime receipt exposes this and global generation does not become fully active;
   - only replace with MATERIALIZED_RECONCILABLE after a reversible menu delta/removal experiment.

4. AerialSpawnSystem:
   - current building-sweep candidate returns RESTART_REQUIRED;
   - verify a static registry rebind alone cannot produce a full ACK from an existing one-shot system instance.

5. Failure:
   - one consumer NACK/throw/restart-required cannot make desired G2 look fully active;
   - G1 remains the accepted active generation according to policy.

Only after these controls pass may ModPlatform use GenerationStore as the activation coordinator. Even then reloadPacks path identity and versioned receipt remain separate blockers.
