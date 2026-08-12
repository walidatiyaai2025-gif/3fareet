# P1-NEXT-050 — Camera + AI + UI + Power-up Batch

**Owner:** Principal Mobile Game Architect  
**Scope:** exactly 50 tasks  
**Status:** VERIFIED

## Task count

- CAM-001 → CAM-011 = 11 tasks.
- AI-001 → AI-018 = 18 tasks.
- UIX-001 → UIX-016 = 16 tasks.
- PWR-001 → PWR-005 = 5 tasks.
- **Total = 50 tasks.**

## Verification evidence

- **Verified code head:** `86a6ea2afb273cab14730e61a152676dc90ea24f`
- **Flutter Prototype CI:** run `31613691078` — SUCCESS
- **Project Status Freshness Guard:** run `31613691026` — SUCCESS

The successful CI run proved:
- `dart format --output=none lib test`;
- `flutter analyze` with zero issues;
- complete `flutter test` suite including Camera, AI, UI-flow and Power-up coverage;
- Android scaffold generation;
- Android Debug APK build;
- Android Release Skeleton APK build;
- preview APK artifact upload.

Task-promotion commits after the verified code head are documentation-only and do not modify the tested gameplay/application code.

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

## Explicitly not claimed

- CAM-012 multi-device camera tuning remains TODO.
- VIS tasks remain TODO because their Definition of Done requires screenshot/device review and Team Lead approval.
- VEH-017 real-device driving feel remains TODO.
- RAC-017 integrated track-completion verification remains TODO.
- Final real-device Verified Release APK remains open.
