## IDENTITY
DINOForge — general-purpose mod platform / pack operating-system framework for the Unity-ECS game "Diplomacy is Not an Option" (BepInEx plugin + Desktop Companion + polyglot tooling). Repo: KooshaPari/Dino (ID 1177712269, v0.25.0-dev, last commit 5bf5d56 2026).

## INTENT
Provide YAML declarative packs, typed registries (units/buildings/factions), an ECS bridge (~30 component mappings), a 24-schema validator, asset pipeline with 3-level LOD, a WinUI 3 companion, and polyglot (Rust/Go/Zig) optimization modules — all to make third-party DINO mod authoring faster and safer than manual workflow.

## CURRENT STATE
Repo cloned at HEAD 5bf5d56 ("ci(validate-packs.yml): also trigger on spec/mature-recovery-20260929") — dependency/CI plumbing, not an in-game journey. v0.24.0 was released 2026-05-06; v0.25.0-dev sits on a closure-gate fix branch and the M0–M14 milestone table in README marks every milestone "Done". spec.md is an 8-line generic draft; the parent STATE.md flags `LOCAL_AND_BRANCH_PROGRESS_UNKNOWN` and "A successful SDK or documentation build is not an installed pack demonstration."

## CODE REALITY
- 207,883 source lines spread across `src/Runtime` (BepInEx plugin), `src/Bridge`, `src/SDK`, `src/Domains/{Warfare,Economy,UI,Scenario}`, `src/Tools` (PackCompiler, DumpTools, DesktopCompanion, Installer, McpServer, MockSteamworksNet, AssetPipelineRust/Go/Zig); 98,158 test lines under `tests/{unit,Runtime,fuzz,load,packs}`.
- Mock-by-design components present and explicit: `src/Tools/MockSteamworksNet/MockSteamworksPlugin.cs:1` documents "FOR HEADLESS CI TESTING ONLY. It should NOT be deployed to production or user instances."
- Concrete NotImplementedException stubs survive in delivered converters: `src/Tools/DesktopCompanion/Themes/Converters/NullToVisibilityConverter.cs:22` and `NullToStringConverter.cs:21`; 72 TODO/FIXME/NotImplementedException markers across src/ (full inventory pending).
- Polyglot claim ("Rust, Go, and Zig optimization modules with seamless .NET integration") is contradicted by `POLYGLOT_DELIVERY_SUMMARY.txt` line 6: "DESIGN PHASE COMPLETE — Ready for Phase 1 Implementation" — i.e. the Rust/Go/Zig modules are skeletons, not production.
- Root is heavily polluted with build/test logs despite `.gitignore` exclusions: 66 `.log`/`.binlog`/`.datacollector` files at root including 47MB `dotnet-build-m1-diag.log`, 47MB `solution-build2.log`, 16MB `test-diag.log`, multi-MB `dotnet-build-*.log` cluster — `.gitignore` matches `*.diag.log` but several entries like `build_output.log`, `coverage_run.log`, `test_output.log` are tracked.
- Tests/Runtime tree mixes real unit suites with dead/experimental Python probe scripts at repo root (`capture_ui_states.py`, `download_clone_trooper*.{js,mjs}`, `test_ui_*.py`, `test_with_focus.py`) — not in tests/.

## STUB/MOCK/MVP-LAZINESS INVENTORY
- MockSteamworksNet (entire `src/Tools/MockSteamworksNet/`): CI-only mock; plugging it into a real Steam client is unsupported.
- NullToVisibilityConverter / NullToStringConverter: literal `throw new NotImplementedException()` in a shipped Desktop Companion.
- Polyglot Rust/Go/Zig asset pipeline: design-phase skeletons, not shipped (see above; no real `.so`/`.a`/binary in repo).
- VFXPrefabGenerator, SkeletonExtractor, AssetPipelineZig tool subprojects: present in tree but not exercised by `DINOForge.sln`; provenance unclear.
- 72 TODO/FIXME/HACK markers across src/ (not enumerated line-by-line in budget — sampled, not exhaustive).
- 66 tracked log artifacts at root document historical CI thrashing (`build_worldupdate.log`, `build_xaml_diag.log`, multi-MB dotnet-build iteration logs from May 2026 retry storm) — operational evidence the build is not stable, archived in git.
- `spec.md`: 8-line generic placeholder ("draft"); no real product spec inside the project.
- Massive megalith: `src/DINOForge.sln`, `src/DINOForge.sln.metaproj`, ~120+ first-level `.sln`/`.csproj` siblings — sign of perpetually-in-progress solution-graph restructuring, not a finished product.

## FWD PATH TO FINISH
1. Resolve the open "real pack journey" gate (DOSSIER A2): build v0.25.0-dev, install the BepInEx plugin into an actual DINO install, load one shipped pack (`warfare-starwars`), capture side-by-side gameplay screenshots and run the six negative-case oracles from `PILOT.json` (pack-accepted-not-loaded, missing-asset silent, save corruption, update-break-injector, hooks-leak-on-disable, UI-without-runtime-receipt). Without this the dossier remains "schema/SDK build only".
2. Strip repository cruft: delete the 66 tracked logs at root, audit `.gitignore` against reality, move CI artifacts under `build_logs/` or untrack. Then enforce a pre-commit hook banning tracked `*.log` to stop the bleeding.
3. Replace or remove the `throw new NotImplementedException()` converters (Desktop Companion Themes/Converters/*) and audit the 72 TODO/HACK/NotImplementedException markers into a tracked follow-up file with owner/severity.
4. Convert polyglot modules from design-phase skeletons to either real FFI binaries or a documented "not built" status in the README milestone table — current state claims them as Done, code says otherwise.
5. Cut the dead Python/JS probes (`capture_ui_states.py`, `download_clone_trooper*.{js,mjs}`, `test_ui_*.py` at root) into `scripts/probes/` or delete; align `DINOForge.sln` so only one solution includes the polyglot tool subprojects (or none).

## TIME-TO-FINISH ESTIMATE
Best-case (happy workstream): 1 senior eng × 2 weeks to delete cruft, fix the two NotImplementedException converters, and gate a real pack journey inside a verified DINO install. Realistic: 4–6 weeks once game-license install, capture infra, and the six negative-control oracles are accounted for. Longer if polyglot Rust/Go/Zig modules are actually required to ship.

## SUNSET/KEEP CALL
KEEP, with hard scope discipline. Repo carries real signal (full BepInEx plugin, 24 schemas, polyglot architecture docs, real Warfare star-wars pack manifests) and matches a defensible niche. But the AI-Directed-Development / HITL-less self-labelling is the actual risk signal — the operator has explicitly accepted "AI slop expected" as a feature, which means a human-in-the-loop pass is overdue to convert "design-phase / skeleton" into "shipped" or remove it. Sunset only if game-license install or BepInEx/Steamworks Pro tooling can't be acquired.

## CONFIDENCE
Medium. Read-only repo snapshot, no build/test execution, no game install observed. Numbers (207,883 src lines, 98,158 test lines, 66 root logs, 72 TODO markers) are real but not enumerated; polyglot/design-phase claim is read from `POLYGLOT_DELIVERY_SUMMARY.txt` (author text), not from executed FFI. No claim of "is the shipped mod good" — that requires running it in the game.
