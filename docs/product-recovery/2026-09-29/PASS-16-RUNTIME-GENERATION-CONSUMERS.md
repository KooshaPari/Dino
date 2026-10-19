# Dino pass 16 — runtime consumer migration boundary

Date 2026-09-30.

The test-only fresh-generation prototype is intentionally narrower than production integration. Source tracing shows why swapping a RegistryManager reference inside ModPlatform alone would be insufficient:

Static/runtime consumers retain RegistryManager references:
- PackUnitSpawner.Initialize(RegistryManager)
- WaveInjector static registry
- AerialSpawnSystem.Initialize(RegistryManager?)
- BuildMenuInjector.Initialize(RegistryManager?)

Other consumers receive registry references/typed registries during initialization (FactionSystem etc.).

No generic SetRegistry/update-generation API was found. Therefore production adoption of immutable generations requires a **consumer publication protocol**, not merely replacing `_registryManager` in one owner.

Proposed integration boundary:
1. GenerationStore owns current immutable generation handle.
2. Runtime consumers either resolve current registries through GenerationStore at use time, or implement `ApplyGeneration(G)`.
3. Publication records required consumer acknowledgements.
4. `observed_generation` is per consumer where behavior can lag.
5. A generation is not "fully active" until required consumers acknowledge; partial state is explicit.
6. Existing entities versus newly spawned entities need separate semantics where content changes cannot safely mutate already-instantiated ECS data.

This is the next architecture experiment after SDK prototype discrimination. Do not replace every static field before the prototype passes all three isolated controls.
