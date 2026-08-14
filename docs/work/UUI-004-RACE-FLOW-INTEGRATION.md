# UUI-004 — production Pause / Results / Restart integration

## State
- Task: `UUI-004`
- Issue: #91
- Parent PR: #89 (`agent/UART-005-cairo-modular-street-kit`)
- Branch: `agent/UUI-004-race-lifecycle-integration`
- Target state: IN REVIEW

## Why this integration was required
The production integration line still used the original `RaceDirector` countdown and nearest-waypoint position approximation. That runtime had no deterministic one-lap finish, Results state or restart lifecycle, so a production results screen could not truthfully be completed.

The reviewed Race work already existed on the separate stacked chain:
- PR #54 — ordered checkpoint validation and runtime checkpoint volumes;
- PR #55 — deterministic one-lap state/tracker;
- PR #56 — checkpoint/lap/progress ranking;
- PR #57 — Ready → Countdown → Racing → Results/restart flow.

This branch ports those six reviewed Race runtime source assets into the current production line with their original source blobs and Unity GUIDs rather than reimplementing the semantics.

## Reviewed Race assets ported unchanged
- `OrderedCheckpointValidator.cs` + `.meta`;
- `RaceCheckpointRuntime.cs` + `.meta`;
- `OneLapRaceState.cs` + `.meta`;
- `RaceRanking.cs` + `.meta`;
- `RaceRoundFlowState.cs` + `.meta`;
- `RaceRoundController.cs` + `.meta`.

PR #57 remains the semantic owner of the reviewed round lifecycle. Coordination notes were posted to PR #57 and the production parent PR #89.

## Production RaceDirector integration
`RaceDirector` now adapts the reviewed contracts to the existing generated Cairo race:
- creates ordered checkpoint trigger volumes from `TrackRuntime.Waypoints`;
- ensures player and all registered rivals have checkpoint + one-lap trackers;
- supports the production bootstrap order where player/track are configured before rivals are registered;
- uses `RaceRoundController` for 3-second countdown, racing, Results and round reset;
- starts all rival lap trackers when the reviewed round enters Racing;
- freezes/releases Rigidbody and AI/player input at the correct lifecycle boundaries;
- ends the player round only after the reviewed ordered one-lap tracker emits a real finish;
- restart resets lifecycle/tracker state, places racers back on the grid and starts a new countdown;
- Pause is accepted only during Racing and uses `Time.timeScale` without mutating the reviewed Race phase;
- player position is now calculated with `RaceRanking` using validated checkpoint/lap progress and segment projection as the tie-breaker, replacing the production nearest-waypoint ranking exploit.

## UUI-004 presentation
Added `ProductionRaceFlowOverlay`:
- duplicate-safe runtime bootstrap;
- Screen Space Overlay uGUI with SafeArea handling;
- localized Pause button;
- localized Pause panel + Resume control;
- localized Results panel with position, finish time and Restart control;
- Arabic/English strings use the existing `RuntimeLocalization` contract;
- touch hit-testing uses the same direct-input style as the existing prototype controls, so no parallel EventSystem/input module is introduced;
- keyboard Escape/P fallback is available in Editor/Standalone.

Added pure `RaceUiPresentationPolicy` so overlay visibility/action guards can be tested without depending on rendered UI.

`PrototypeHud` now suppresses drive input and touch-control rendering while the race is Paused or in Results, preventing controls behind the production overlay from affecting the car.

## Automated coverage committed
New `Afareet.UiFlowEditModeTests` assembly includes 5 tests:
1. Pause overlay only while Racing + paused;
2. Results overlay priority;
3. pause/resume/restart phase guards;
4. reviewed countdown → Racing → Results → Restart state flow;
5. skipped ordered checkpoint rejection.

The original Race tests on PRs #54→#57 remain the authoritative detailed coverage for their source contracts.

## Scope guard
No Vehicle physics, Audio, Art assets, World generation, Packages, ProjectSettings, Android build or Release files are changed by this integration.

## Validation truth
- Static source/API review: completed in-repo.
- Unity 6000.5.8f1 import/compile on this exact head: NOT EXECUTED.
- `Afareet.UiFlowEditModeTests`: committed, NOT EXECUTED.
- Existing Race EditMode/PlayMode suites on this exact integration head: NOT EXECUTED.
- Pause/Resume touch interaction on Android: NOT EXECUTED.
- Results/Restart end-to-end on Android: NOT EXECUTED.
- Arabic shaping/font/layout on device: NOT EXECUTED.
- VERIFIED: No.

## Remaining QA before VERIFIED
1. Import exact PR head in Unity 6000.5.8f1 and compile with zero errors.
2. Execute UI-flow tests plus Race EditMode/PlayMode suites.
3. Complete one real ordered lap; confirm early/skipped checkpoint crossing cannot reach Results.
4. Pause/resume on Android and confirm no hidden driving input leaks through.
5. Finish race, verify result position/time, restart, and complete second countdown.
6. Switch English/Arabic and visually verify labels, shaping, alignment and SafeArea on target devices.
