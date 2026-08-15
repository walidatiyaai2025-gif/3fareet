# U-P1 side-branch integration recovery

## Purpose
Restore reconciled U-P1 deliverables that were already counted `IN REVIEW` on owning side PRs but were absent from the current production tree, then connect those restored contracts to the current production runtime without replacing newer integration work.

## Production base
- parent: PR #97 / `agent/U3D-012-android-ci-artifact`
- exact base: `9685f8a05517a683011dbcf810c5cbc396da32bd`
- tracker: Issue #101

## Owning side-branch deliverables recovered
- PR #51 / `UPER-001`: Android device-tier/performance budget contract.
- PR #52 / `UART-001`: human + machine-readable 3D asset pipeline contract.
- PR #53 / `U3D-010`: PlayMode test assembly + race-start tests.
- PR #58 / `URAC-006`: track boundary/off-road policy, runtime, tests.
- PR #60 / `URAC-008`: corner speed/racing-line policies + tests.
- PR #61 / `URAC-010`: rival motion/reset guards + tests.
- PR #84 / `UVEH-002`: WheelCollider suspension prototype + tests/evidence.

The unchanged recovered production/test/evidence files were ported using the owning PRs' exact Git blobs, preserving Unity `.meta` GUIDs. Stale snapshots of `docs/PROJECT_STATUS.md`, `docs/MODULE_OWNERSHIP.md`, and `docs/tasks/06-UNITY-3D-MIGRATION.md` were deliberately excluded.

## Production wiring recovery
Static review after the blob-level recovery found that several restored contracts were still not active in the current production runtime. The integration branch therefore adds the missing wiring instead of treating file presence as completion.

### URAC-008 — racing line / braking
Current `AiRacer` now consumes `RacingLineLookahead` and `CornerSpeedPolicy`:
- lookahead selects the aim waypoint and upcoming corner severity;
- corner speed plan reduces throttle and requests braking when overspeeding;
- nearby-car avoidance can add a minimum brake request;
- Nitro is permitted only on the policy's straight/low-brake plan;
- drift decision incorporates planned corner severity.

`ArcadeCarController.SetAiInput(...)` gains an optional `brake` parameter so the restored braking-zone policy can control the real production brake path while preserving all existing four-argument callers.

### URAC-006 — track bounds / off-road
`RaceDirector` now:
- builds a dedicated `TRACK BOUNDARY EDGES` root using `TrackBoundaryRuntimeBuilder.BuildEdges(...)`;
- uses the P1 road half-width of 7m, matching the current procedural 14m road width;
- attaches/configures a `TrackBoundaryMonitor` for every registered racer.

The generated edge colliders provide continuous solid race limits independent of the small decorative neon rail geometry.

### URAC-010 — rival recovery
`RaceDirector` now adds/configures `RivalResetController` for each rival after its ordered checkpoint tracker is configured. Recovery is:
- inactive before START/countdown;
- enabled when Racing begins;
- disabled when racers are frozen/results/reset;
- based on the last accepted ordered checkpoint, preserving the reviewed recovery semantics.

### U3D-010 — PlayMode compatibility
The original PR #53 test branch predated the current production `RaceDirector` contract and called `Configure(player, null)`. Recovery updates the test to:
- build a valid four-waypoint `TrackRuntime`;
- configure the rival AI with that path;
- add `Afareet.World` to the PlayMode test assembly references;
- assert the current `Ready → Countdown → Racing` contract;
- remove the obsolete expectation that `CountdownText` remains `GO!` after the phase has already changed to Racing.

### Hidden test-assembly dependency fixed
The restored `RivalLifecycleTests` directly use `Afareet.Vehicle`. `Afareet.RaceEditModeTests` now explicitly references `Afareet.Vehicle`, closing a compile dependency that was hidden while the side branch had never executed under Unity CI.

## Integration tests added
`ProductionRaceIntegrationTests` adds focused EditMode coverage for:
1. production `RaceDirector.Configure()` building two solid edge colliders per track segment and attaching the player off-road monitor;
2. rival registration attaching off-road monitoring and configured rival recovery;
3. the production AI brake-input contract;
4. lookahead requesting braking and suppressing Nitro for a fast sharp corner.

## Scope guard
This recovery intentionally does **not** replace the current production `RaceDirector`, `AiRacer`, vehicle controller/config, Race lifecycle, or other newer files with older side-branch snapshots. Only the missing additive deliverables are recovered, and three current production files are modified specifically to wire those reviewed contracts into the latest runtime.

No Packages, ProjectSettings, signing, release, Audio, UI, Art, or rendering architecture changes are part of this recovery.

## State / validation truth
This recovery does not add tasks and does not change the operational count. It makes the production tree match work already counted `IN REVIEW`.

- Exact Unity `6000.5.8f1` compile: **NOT EXECUTED** on this recovery head yet.
- Restored/new EditMode tests: committed, **NOT EXECUTED**.
- Updated PlayMode tests: committed, **NOT EXECUTED**.
- Android build/device validation: **NOT EXECUTED**.
- `VERIFIED`: **No**.

Unity Actions engine execution remains externally blocked by missing repository licensing secrets tracked in Issue #98. The production Unity workflow is expected to run its license-free static gate on the integration PR and fail loudly at license preflight until that infrastructure dependency is resolved.
