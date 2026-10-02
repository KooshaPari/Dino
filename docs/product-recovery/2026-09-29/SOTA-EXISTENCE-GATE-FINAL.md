# SOTA / existence gate — consolidation pass

Date: 2026-10-01. Research refreshed against current public material. This is a design decision record, not marketing.

## Best realistic absent-product stack

If DINOForge disappeared today, the realistic baseline is not "nothing":
- BepInEx for Unity/.NET plugin loading, configuration/logging and patcher substrate;
- HarmonyX/MonoMod for runtime detours/patching;
- game-specific reverse engineering and bespoke plugins;
- ordinary YAML/JSON/schema libraries for declarative data;
- custom asset tooling and per-mod installers;
- optional hot-reload helper such as UnityHotReload for plugin-code iteration.

That stack is credible for conventional mods. It is fragmented for the mature DINOForge thesis: a pack author still owns game-specific mapping, composition, lifecycle, diagnostics, total-conversion conventions and evidence.

## Commodity / integrate rather than rebuild

### USE / INTEGRATE
- BepInEx lifecycle/injection where compatible with the target host.
- HarmonyX/MonoMod for runtime patching rather than inventing a detour engine.
- standard schema/YAML/JSON tooling.
- existing Unity asset/runtime APIs and proven import/conversion tooling where licensing permits.
- platform-native filesystem/path/security primitives rather than custom canonicalization.

### LEARN / ADAPT
- UnityHotReload: useful evidence that state-preserving hot code replacement has strict type-layout constraints. It is prior art for lifecycle failure modes, not a replacement for pack-generation semantics.
- Paradox/Colossal Order's integrated mod UX: useful product prior art for discover/manage/install experience and the fact that code/map/asset mod surfaces can be first-class product concepts.

### BUILD CUSTOM ONLY WHERE THESIS SURVIVES
- typed DINO-specific content/host mapping;
- pack composition/effective-generation semantics across DINO content;
- total-conversion-oriented author/player contract;
- exact activation/consumer receipts and failure/restart semantics;
- DINO-specific runtime adapters that commodity frameworks cannot know.

## Differentiation ledger

### Commodity / falsified as differentiation
- "can inject a Unity plugin";
- "can patch methods";
- "can parse YAML";
- "can reload some plugin code";
- generic logging/configuration.

### Candidate differentiation that survives this pass
1. A coherent declarative-first contract over DINO's useful moddable surface rather than independent ad-hoc plugins.
2. Composition/override/dependency semantics tied to effective runtime generations.
3. Total-conversion journey: author can replace enough of the game to create a meaningfully different game without maintaining a game fork.
4. Productized author -> validate -> install -> activate -> observe -> revise -> recover workflow.
5. Machine-verifiable evidence binding exact pack/host/generation/consumer truth.

### Unverified differentiation
- breadth sufficient for a real Star Wars/modern total conversion;
- ergonomic advantage over expert bespoke BepInEx/Harmony development;
- reliable live reload across all materialized domains;
- asset pipeline breadth and legal redistribution workflow;
- support/maintenance cost advantage.

These require the post-build pilot; they are not certified by design.

## Architecture consequence

DINOForge should be a layer **above** commodity patch/injection substrate, not a replacement for it. The stable conceptual spine is:

source pack/artifacts
-> validated immutable candidate identity
-> dependency/override composition
-> DINO-specific typed effective generation
-> domain/runtime adapters
-> explicit consumer observation/materialization
-> activation/restart policy
-> machine receipt + human diagnostics

No single mutable RegistryManager is sufficient product truth once materialized consumers exist.

## Rejection conditions / existence gate

The product thesis should be reconsidered if a realistic composed commodity stack demonstrates, for the same DINO host:
- equivalent total-conversion breadth;
- equivalent declarative composition and conflict semantics;
- equivalent author/player management;
- equivalent lifecycle/recovery;
- equivalent exact runtime evidence;
with lower ongoing maintenance.

Current evidence does not establish that. It also does not establish DINOForge wins empirically. Existence gate: **provisionally survives; pilot required**.

## Sources refreshed

- BepInEx docs/repository, retrieved 2026-10-01: https://github.com/BepInEx/bepinex-docs/blob/master/index.md ; https://github.com/BepInEx/BepInEx
- HarmonyX repository/docs, retrieved 2026-10-01: https://github.com/BepInEx/HarmonyX
- UnityHotReload, retrieved 2026-10-01: https://github.com/xiaoxiao921/UnityHotReload
- Paradox Cities: Skylines II modding/code-modding material, retrieved 2026-10-01: https://www.paradoxinteractive.com/games/cities-skylines-ii/modding ; https://www.paradoxinteractive.com/games/cities-skylines-ii/modding/dev-diary-3-code-modding

Claims from these sources are prior-art context, not evidence that DINOForge implements anything.
