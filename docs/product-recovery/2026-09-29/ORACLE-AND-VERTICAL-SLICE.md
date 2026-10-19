# Dino — journey/oracle and vertical slice contract v0.1

Date2026-09-29; source **17119051e782b32615413049c1c3cd207f0b540e**. Design draft, not executed product evidence. Candidate slice does not replace mature breadth. Derived anchors in MATURE-CONTRACT remain excluded from grading until accepted.

## Journeys

D-J01: author selects a supported domain/content contract -> creates a rights-cleared pack -> validates -> builds/installs in the intended game/profile -> player activates/plays -> author obtains exact evidence of expected live effect.

D-J02: author/player introduces invalid schema/dependency/asset/configuration -> activation rejects or rolls back according to explicit policy -> last accepted usable session remains recoverable -> error identifies the exact faulty pack/criterion without claiming success.

D-J03: player moves menu -> playable scene -> menu, restarts and restores profile -> pack behavior and management remain usable -> collector reconnects to correct new scene/world identity.

D-J04: author composes two independently authored supported packs -> resolves dependency/precedence/conflict policy -> player uses effective content -> removing/disabling one yields specified dependent behavior, not silent partial success.

Required stage projection: CVP J01/J02/J03; MVP adds full J04 and routine management; Beta/GA/mature widen accepted configurations and quality overlays. Stage definitions remain proposals awaiting full source recovery, not claims current code meets them.

## One real vertical slice

Use a rights-cleared, deliberately small data-driven pack that causes an observable in-game effect through normal SDK/runtime APIs. Do not special-case its ID in the engine. Include valid, parse-valid/schema-invalid, missing-dependency, conflicting-content and missing-asset variants. Fixture IDs/digests/version must be declared; they are not chosen here without inspecting actual schemas/host support.

The slice must traverse real storage (pack/profile/config), accepted domain model, actual machine entry point, actual player interaction, independent observation, and restart/scene recovery. A screenshot of a model viewer or a CLI printing a pack name is insufficient. A command listed in README is not presumed mounted. Produce raw native logs/state capture and interaction frames bound to the same run/candidate; explain any collector blind spot.

## Evidence subject and trust boundary

Identity includes product/accepted contract/criterion, runtime and SDK build artifact digests, host game build, Unity/backend/OS/loader version, installation/profile, pack/dependency/asset digests, feature flags, scene/world generation, command/observation correlation, verifier build/version, evaluation ID, timestamp and raw artifact digest.

The grader policy and approved verifier build are outside candidate-worker write access. Evidence issued by the same writable runtime is an observation requiring corroboration, not automatic independent truth. Use standard attestation/signing tooling where appropriate; this draft does not deploy or certify a trust root. No skipped, empty, stale, foreign-candidate or collector-error result may be green.

## Concrete adversarial oracle matrix

| Case | Stimulus | Accepted observable result | False green exposed |
|---|---|---|---|
| Supported pack | Normal installation and player activation | Exact expected game effect and pack/install/world identity | Loader logged success but ECS effect missing |
| Runtime schema parity | Parse-valid schema-invalid content installed directly / edited after compilation | Rejection or trusted exact-byte validation before activation; explicit diagnostic | Compiler tests green while runtime bypasses schema |
| Dependency failure | Missing peer / cycle / incompatible version | Specified safe failure with effective graph diagnostic | Pack count incremented despite unusable partial graph |
| Wrong target | Two installs/profiles, collector attached to the other | Subject mismatch blocks qualification | Old/demo screenshot qualifies a different running instance |
| Stale world | Change scenes while queued action references disposed world | Reject/rebind under explicit generation policy; no stale accepted effect | Persistent bridge hides dead runtime state |
| Broken reload | Change valid active pack to invalid version | Prior committed state retained/recovered or explicit safe disabled outcome | Registry partially changed but UI reports active |
| Restart | Stop/relaunch with existing enabled/disabled settings | Expected settings restored; fresh world identity | Ephemeral worker/in-memory state mistaken for persistence |
| Interrupted update | Fail during staged artifact/config switch | Prior accepted configuration remains recoverable | Round-trip unit test misses partial-write failure |
| Authorization | Unapproved process/client attempts mutation | Denied without state change; audit evidence | Merely possessing endpoint access enables writes |
| Missing collector | Disable capture/error artifact upload | UNVERIFIED/BLOCKED, never pass | Absent evidence interpreted as no failures |
| Regressed effect | Remove actual game mapping but retain menu/registry | Oracle detects absent effect | Mock UI and type declarations satisfy requirements |
| Worker replacement | End worker attempt, resume same durable effort with new agent | Same accepted contract/evidence history; no automatic acceptance/reset | Worker lifecycle conflated with product state |

Mutation design precedes mass test generation. All native cases above are NOT RUN in this cycle; no generated stub participates in grade.

## Current implementation trace, bounded

| Source path at pinned revision | What was actually observed | What is NOT established |
|---|---|---|
| Plugin.cs:1-230 | BepInEx class/Awake, config, persistent-root/scene-resurrection fields, Harmony startup | Complete initialization call chain and actual successful host launch |
| RuntimeDriver.cs:1-335, blob f4024a7b69d2b83d1bf94a77616e9bcaca9b3736 | Initialize starts coroutine; InitializeRoutine constructs ModPlatform and calls Initialize; profile/settings/dispatcher setup | Coroutines survive all real DINO teardown cases; full dispatcher/reload reachability |
| ModPlatform.cs:1-440 | Runtime constructor supplies null validator, registers systems/catalog in guarded blocks | System execution/effective activation/complete OnWorldReady path |
| ContentLoader.cs:1-220 | Null skips schema validation by contract; compatibility/dependency handling exists | All importer guards and all supported ingress are resolved |
| Registry/bridge/UI/hot reload/installer/MCP | Referenced by observed code or docs | Complete call paths, persistence and native tests |

This continuation expands ledger D-S06..10 but does not close those source families. Source history search additionally found foundation a0d42051 and early harness a3146aa4; entire history is still open.

## Independent assessment

The shared synthetic evidence-predicate experiment is a design sanity check only. Native tests, licensed host session, real pack interaction, compatibility qualification and independent/fresh semantic review remain absent. A same-worker adversarial pass cannot satisfy the required independent final review.
