# P1-NEXT-050 — Camera + AI + UI + Power-up Batch

**Owner:** Principal Mobile Game Architect  
**Scope:** exactly 50 tasks  
**Status:** IN REVIEW until CI is Green

## Task count

- CAM-001 → CAM-011 = 11 tasks.
- AI-001 → AI-018 = 18 tasks.
- UIX-001 → UIX-016 = 16 tasks.
- PWR-001 → PWR-005 = 5 tasks.
- **Total = 50 tasks.**

## Delivered architecture

### Camera
- Deterministic follow camera state.
- Speed look-ahead and exponential damping.
- Drift roll/lateral state.
- Nitro FOV/zoom state.
- Crash impulse shake.
- Airborne pitch behavior.
- Speed/FOV illusion.
- Deterministic shake manager with accessibility disable.
- Finite-value sanitization and hard camera bounds.
- Flame viewfinder integration from the fixed-step loop.

### Offline racing AI
- Racing-line representation and follower.
- Throttle, steering, brake-zone and drift-zone controllers.
- Overtake, defensive positioning and collision-avoidance behavior.
- Seeded deterministic mistake probability.
- Aggression, difficulty and personality profiles.
- Nitro strategy and power-up strategy hook.
- Three prototype AI rivals.
- Stuck recovery and finish consistency.
- AI pack integrated into `RaceSession`; player race position derives from actual AI progress.

### UI / UX
- Splash, main menu, play-mode selection and loading states.
- Prototype race HUD with speed, position, Spirit, timer, lap and progress.
- Pause menu and race-result screen.
- Error/retry state.
- SafeArea coverage.
- Arabic RTL toggle.
- Central design tokens.
- Accessibility text-scaling clamp.
- One persistent Flame game instance under the front-end layers to preserve audio/game lifecycle.

### Power-ups
- `PowerUpDefinition` and five prototype definitions.
- Spawn-point/pickup lifecycle.
- One-slot race inventory.
- Pickup collection rules.
- Eye Shield activation, timeout and one-hit absorption.

## Verification gates

The batch stays `IN REVIEW` until all of the following succeed on the PR head:

- `dart format --output=none lib test`.
- `flutter analyze` with zero issues.
- complete `flutter test` suite.
- Android scaffold generation.
- Android Debug APK build.
- Android Release Skeleton APK build.
- preview artifact upload.
- Project Status Freshness Guard.

## Explicitly not claimed

- CAM-012 multi-device camera tuning remains TODO.
- VIS tasks remain TODO because their Definition of Done requires screenshot/device review and Team Lead approval.
- VEH-017 real-device driving feel and final Verified Release APK remain open.
