# DINOForge mature contract recovery — draft v0.1

Program `gaming-pair-20260929`; date2026-09-29; analyzed source **17119051e782b32615413049c1c3cd207f0b540e**. **NOT a complete or accepted replacement specification.** Source ledger remains open. Candidate obligations here are excluded from completion grading until provenance, semantic review and acceptance are resolved. No requirement quota is selected.

## Recovered product identity

A general-purpose, declarative-first mod platform for Diplomacy is Not an Option. A pack author (human or agent) should be able to express supported content/behavior, understand compatibility and conflicts, deploy it into the correct real game, observe whether it actually works, and recover from failure without destroying user state. A player must be able to use the resulting pack, not merely see a successful compiler or static model preview.

Basis: README and runtime/SDK source; retrieved March10 framework-first user request; foundation commit `a0d42051e0e62d758397ba56987b0f17ae550ae0` (historical design assertion). A Star Wars/warfare pack is one consumer, not the definition of the framework. Early assistant-proposed repo topology is not an accepted architectural mandate.

## Mature horizon, before stage slicing

The currently evidenced horizon includes supported content/domain packs; composition/dependencies/overrides; game/ECS mapping; asset creation/import/transformation and provenance; author diagnostics and CLI/machine interfaces; installation/profiles/configuration; in-game management/settings; lifecycle-safe activation/reload/deactivation; observable host integration; compatible releases and maintenance. Exact supported content kinds, host versions, user platforms, distributable assets, remote registries and service obligations are still being recovered. No marketplace, cloud account service or general game engine is invented by this draft.

## Ontology: overlapping projections, not one forced tree

| Projection | Entities / relations | Boundary to resolve |
|---|---|---|
| Product semantics | Domain -> capability -> content type -> distinct obligation; pack implements content; overrides conflict/precedence edges | Warfare must not hardcode the whole platform; other actual domain contracts need archaeology |
| Author experience | Discover capability -> author -> validate -> diagnose -> build -> install -> observe -> revise | CLI/MCP, editor/companion and docs are projections over the same pack semantics |
| Player experience | Choose correct install/profile -> enable compatible pack -> play -> configure -> recover/disable | Real game process and live world matter, not only registry entries |
| Runtime | HostCompatibilityProfile, Installation, PackVersion, ContentIdentity, ActivationGeneration, Scene/WorldGeneration, Command/Observation | Cross-scene objects and background callbacks are not durable content identity |
| Artifact/supply chain | SourceAsset -> rights record -> transformation/tool version -> built asset -> pack digest -> release/deployment | User possession or a public URL does not grant redistribution rights |
| State/lifecycle | discovered -> validated -> staged -> activating -> active; rejected/failed/rolled-back/disabled as explicit outcomes | States are proposed semantics; reconcile real loader and user expectations before freezing |
| Verification | criterion -> oracle -> run -> artifact -> qualified candidate/configuration; trace edges preserve authority | Generated/unverified catalogs remain quarantined |
| Quality/operations | Compatibility, failure atomicity, latency, memory, accessibility, security, maintenance/support | Subject/workload/target-specific overlays; no mechanical cloning across requirements |

Worker attempt, development effort and product state have independent lifetimes. Pack/install/world truth cannot live only in an agent process or temporary worktree. See [oracle contract](ORACLE-AND-VERTICAL-SLICE.md).

## Stage projections, provisional

Stages are filters over the mature contract, not new mini-products. None is certified reached by this recovery.

| Stage | Encapsulated usable form | Required journeys | Growth / transition debt |
|---|---|---|---|
| CVP | One rights-cleared supported pack on one explicitly qualified host/profile, with human or agent author-to-player handoff | D-J01 author/install/play; D-J02 reject/recover; D-J03 restart/scene continuity | Same content/activation/evidence identities as mature; limited adapter breadth is explicit |
| MVP | Independent pack composition and routine author/player management | CVP plus D-J04 compose/conflict/configure; routine disable/uninstall preserving user state | Widen domain/host adapters only with compatible schemas/migrations |
| Beta | Repeatable distribution and user recovery across declared supported configurations | Install/update/rollback/support diagnostics using released artifact identity | Support matrix, accessibility/performance targets and migrations must be accepted and measured |
| GA | Reliably supported, externally usable declared scope | All GA-selected actor outcomes and critical quality gates; stable release human approval | No hidden unsupported cases marketed as complete |
| Mature | All accepted capabilities and lifecycle projections, with maintained extensibility and support | Complete accepted journey set, including author/agent/operator/player paths | Add/enrich contracts without replacing core identity/state semantics |

Existing milestone labels do not establish these stages. Later expansion must record transition debt: runtime bypass of validation, pack-specific branches, mutable unversioned registries, incompatible host mappings, ambiguous profile paths, undocumented unload/reload side effects and untraceable asset transformations.

## Semantic obligation anchors (candidates; not the complete catalog)

### REC-DINO-ACTIVATION — supported ingress enforces one accepted content contract

Statement: every supported pack activation ingress must enforce accepted structural/semantic compatibility for the exact bytes it activates, or verify a trusted immutable validation artifact for those bytes before activation. Rationale: author validation is not enough when files can change or players install directly. Parent: pack lifecycle/composition. Sources: D-S03/04 schema-first design; D-S07/08 actual null-validator path; current user evidence policy. Authority: recovered design plus source-derived obligation candidate, not newly approved scope. Role: core spine. Stages: all where activation exists. Journeys: D-J01/02/04. Dependencies: pack identity/schema version, compatibility profile and accepted error/rollback policy.

Positive: a compatible fixture pack built/installed through each declared ingress becomes active with matching digest and expected live game effect. Negative: valid YAML with a schema-invalid value, missing dependency, incompatible framework version or changed bytes cannot become silently active; prior accepted state remains intact or a clearly specified safe recovery outcome occurs. Surfaces: RuntimeDriver.InitializeRoutine -> ModPlatform.Initialize -> ContentLoader constructor/LoadPack/LoadPacks; downstream registry importer and mounted reload callers incomplete. Oracle: compare validation, registry activation, actual host effect and previous-state receipt; leaf parse tests insufficient. Growth: enrich schemas/adapters/migrations without changing pack identity semantics.

### REC-DINO-HOST-SUBJECT — actions and observations refer to the intended installation/world

Statement: supported commands, activation outcomes and evidence identify the selected running installation/profile and current scene/world generation; stale or foreign targets must not qualify the requested effect. Rationale: copied install paths and resurrected runtime roots create real scope confusion. Parent: host bridge. Sources: actual path reanchoring and scene-lifetime code D-S06/07/10/13. Authority: source-derived candidate under current verification mandate. Role: core spine. Stages: all runtime stages. Journeys: D-J01/02/03. Dependencies: compatibility profile, world generation/command identity.

Positive: an action is observed in the selected game and declared current world. Negative: bridge to another install, stale destroyed-world handle, prefix-colliding install directory, or screenshot from another run cannot satisfy it. Surfaces: Plugin/RuntimeDriver, MainThreadDispatcher/GameBridgeServer and target selection; actual full dispatcher path not yet traced. Oracle: independent process/build/pack/scene identity plus state observation and correlated user action. Growth: more hosts/profiles preserve identity, not ad-hoc path-string aliases.

### REC-DINO-LIFECYCLE — scene changes and failed changes preserve a usable mod session

Statement: supported scene transitions, restarts, reloads and rejected changes have explicit outcomes and do not silently leave a partially active pack masquerading as usable. Parent: lifecycle/recovery. Sources: persistent-root/resurrection implementation and framework-first usable-product intent. Role: core spine; stages: CVP onward; journeys: D-J02/03. Dependencies: exact state ownership, activation transaction and host lifecycle contract.

Positive: load -> playable scene -> menu -> reload/restart retains expected enabled-pack configuration and reconnects observations to the new world. Negative: lost callback, disposed world, invalid reload, missing asset/dependency, or interrupted update reports failure and restores/retains the last accepted configuration according to policy. Surfaces: Plugin, RuntimeDriver, ModPlatform, profile/settings stores/hot reload; complete mapping open. Oracle: state and user-action evidence before/after each boundary, including forced failure. Growth: new adapters must implement the same lifecycle contract; transition debt recorded when they cannot.

### REC-DINO-COMPOSITION — packs compose without product identity being hardcoded to a specimen

Statement: supported pack dependencies, overrides and conflicts produce a specified effective registry and game behavior independent of arbitrary discovery ordering or a hardcoded sample pack. Parent: domains/composition. Sources: foundation registry/domain design and current ContentLoader dependency modes. Role: differentiation candidate; stages: mature/MVP, CVP exercises a narrow non-hardcoded path; journey D-J04. Dependencies: schemas, precedence policy, lifecycle guard.

Positive: two distinct compatible packs resolve to the declared effective content. Negative: dependency cycle/missing peer or conflicting identity produces explicit failure; renaming a pack within accepted ID rules cannot unexpectedly change unrelated behavior. Surfaces: dependency resolver/registry/overrides/assets and runtime systems, not fully inspected. Oracle: independently expected dependency graph/effective registry plus in-game effects. Growth: widen domains and precedence policies only through versioned contracts, not special-case branches.

## Quality overlays and acceptance still missing

Fault safety, diagnostics and exact subject identity are directly relevant now. Numerical performance budgets, supported host/platform matrix, accessibility target, update trust model, privacy of recorded sessions, asset licensing policy, data retention and stable support guarantees are not yet fully recovered. Each needs a named subject/configuration, acceptance authority, positive and failure criteria and evidence strategy. No 'fast/secure/accessibility' row is cloned onto every feature.

## Architecture/existence gate

Use existing loader/patching/schema/manager/provenance primitives unless a concrete domain gap survives comparison. Current architecture is not frozen. See PhenoRegistry `docs/sessions/20260929-gaming-pair/Dino/PASS-01-ARCHAEOLOGY-SOTA.md` at research commit `7c7c724ebcfd0fa353da6b090c36e66ddb474b03`. That research is provisional, not a library adoption order.

Before architecture acceptance: enumerate all ingress and state owners; qualify exact host/dependency pins; run validation parity and lifecycle experiments; complete asset/license and maintenance comparisons; reconcile archived/renamed intent sources; independently challenge omitted behaviors. No current claim of optimality or completeness is made.
