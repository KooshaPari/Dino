# Dino pass 22 — real-game evidence audit

Date 2026-09-30. Frozen/current main remains `17119051e782b32615413049c1c3cd207f0b540e`.

## Three workflows with very different evidentiary value

### 1. `.github/workflows/game-launch.yml` — admissible design, currently no recent execution

This is the only inspected workflow that clearly requires a real licensed/game-installed environment:

- `runs-on: [self-hosted, windows, dino-installed]`;
- requires DINO + BepInEx paths;
- installs the Runtime plugin;
- runs the dedicated GameLaunch project;
- runs `prove-features-gate.ps1 -RequireGame`;
- uploads TRX and BepInEx/UI artifacts.

However, recent scheduled runs found in repository Actions history are all **cancelled**:

| Date | Run | Head | Result |
|---|---:|---|---|
| 2026-09-28 | 36415112957 | 17119051... | cancelled |
| 2026-09-21 | 35588105879 | 17119051... | cancelled |
| 2026-09-14 | 34832072673 | 17119051... | cancelled |
| 2026-09-07 | 34108316258 | b299c28f... | cancelled |
| 2026-08-31 | 33386127802 | 2e6eea85... | cancelled |
| 2026-08-24 | 32693826847 | f9b884cd... | cancelled |

For the latest three, GitHub reports the `Game Launch E2E` job as cancelled with `steps: null`. No checkout, build, launch, test or evidence-collection step executed. These runs provide **zero native qualification**.

No successful `Game Launch / E2E` run was found while paging repository action history back through the observed Aug24–Sep30 period.

This does not prove the self-hosted runner can never execute. It establishes that recent scheduled history supplies no executed native evidence.

### 2. `.github/workflows/game-launch-validation.yml` — workflow name overstates evidence

This workflow runs on GitHub-hosted `windows-latest` and checks a hard-coded game path:

`G:\SteamLibrary\steamapps\common\Diplomacy is Not an Option`

If the executable is absent, it writes `game_available=false` and exits **0**. All later build/deploy/game-test steps are conditional on `game_available == true`.

Additionally, the `Run game launch tests` step is marked `continue-on-error: true`.

A concrete historical false-green was inspected:

- run: **34687636798**
- event: push
- head: **17119051e782b32615413049c1c3cd207f0b540e** (current main)
- workflow conclusion: **success**
- verify-game log: `Game not found at G:\SteamLibrary\steamapps\common\Diplomacy is Not an Option — skipping Real Game Launch Validation (game not available on this runner)`
- Restore dependencies: skipped
- Build solution: skipped
- Deploy to game: skipped
- Run game launch tests: skipped

Therefore a green result from this workflow **cannot be used as evidence that the game launched at all** unless the run receipt independently proves `game_available=true` and the game-test/deploy steps executed. Workflow-level success is insufficient.

Current recovery-PR runs of this workflow are skipped for a different reason: the PR is draft and the job-level condition excludes draft pull requests. Those skips also provide zero evidence but do not imply game absence.

### 3. `.github/workflows/game-automation.yml` — explicitly mock-capable

If the expected game path does not exist, this workflow creates:

- the directory;
- an empty file named `Diplomacy is Not an Option.exe`.

It then exercises MCP/automation scripts against that environment.

Thus even a green `Game Automation Tests` run is **not a real-game qualification** unless a separate trustworthy predicate proves the actual executable/install was present and used. The inspected workflow does not provide that fail-closed guarantee.

Recent scheduled runs on current main are repeatedly failing anyway (examples Sep13–Sep30), but failure/green here remains automation-harness evidence, not native gameplay evidence.

## Evidence-policy defect

The repository currently has at least one workflow whose display name says **Real Game Launch Validation** while its aggregate success can mean **game absent; no game step executed**.

This is exactly the false-green class prohibited by the recovery program:

> Missing evidence / skipped evidence / wrong subject / collector unavailability is not green.

### Required repair before using this workflow for grading

A real-game evidence job should emit an explicit machine receipt with:

- `game_available`;
- exact executable/build identity;
- game install path identity;
- BepInEx/runtime artifact digest;
- candidate commit/artifact digest;
- executed test count;
- game process/session/world identity where applicable;
- raw logs/screenshots/state artifacts;
- final qualification status.

For a criterion that requires real-game execution:

- `game_available=false` => **UNAVAILABLE/BLOCKED**, never PASS;
- zero executed game tests => **UNVERIFIED**, never PASS;
- test step failure => FAIL even if diagnostics continue;
- workflow/job success must not be the product acceptance predicate.

A separate optional smoke workflow may remain skip-friendly, but it must not carry a name/required-check role suggesting native qualification.

## Current Dino native gate

- SDK generation architecture: strong candidate-bound evidence exists.
- Unity/ECS consumer source: mapped but not executable in GameInstalled=false build.
- Genuine self-hosted game E2E: recent schedules cancelled before execution.
- GitHub-hosted “Real Game Launch Validation”: demonstrated false-green when game absent.
- Licensed host vertical journey: **NOT QUALIFIED**.

This blocks native vertical-slice closure and architecture-high-risk completion. It does not invalidate the SDK generation architecture results.
