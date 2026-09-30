# Dino pass 21 — runtime consumer execution boundary

Date 2026-09-30. Frozen source remains `17119051e782b32615413049c1c3cd207f0b540e`.

## The SDK architecture is qualified more deeply than the Unity runtime

Candidate-bound recovery evidence now shows:
- three production reload controls remain red;
- seven generation architecture/control-plane tests are green, covering fresh-generation replacement, patch invalidation, failure preservation, digest-derived identity, desired-vs-active state, ACK/NACK and fail-closed receipts.

A follow-on test attempted to execute the actual Runtime static registry consumers:
- PackUnitSpawner
- WaveInjector
- AerialSpawnSystem
- BuildMenuInjector

The Runtime project itself built in GitHub-hosted CI, but the test could not compile because those Unity/ECS source files are **deliberately excluded** when `GameInstalled=false`. `DINOForge.Runtime.csproj` removes all Runtime C# files in that mode and allowlists only a small pure-C# subset. The source file for AerialSpawnSystem does declare `DINOForge.Runtime.Aviation`; its absence from the CI assembly is an environment/build-target property, not a namespace/product defect.

## Qualification rule

Do **not** widen the production Runtime allowlist or add fake Unity stubs merely to turn this gate green.

The actual runtime-consumer rebind test is quarantined from the SDK-only recovery project and remains a game-installed/self-hosted qualification item.

Source inspection establishes that the mounted consumers expose rebind entrypoints:
- PackUnitSpawner.Initialize
- WaveInjector.SetRegistryManager
- AerialSpawnSystem.Initialize
- BuildMenuInjector.Initialize

But source presence is not execution evidence. The remaining question is whether a real running Unity/DINO session can apply generation G2 to those consumers and whether derived ECS/UI state converges.

## Required next runtime receipt

On a game-installed runner/session:
1. bind G1 and capture each consumer's observed generation;
2. build/publish G2;
3. call/reach each consumer rebind path;
4. verify newly spawned/queried content reads G2;
5. verify existing ECS/materialized UI behavior according to accepted live-update vs restart-required semantics;
6. inject one consumer failure and ensure global receipt remains not-fully-active;
7. preserve raw game logs, exact host/install/world/build and generation digest.

Until then: SDK generation architecture = supported in isolation; Unity/ECS integration = UNQUALIFIED.
