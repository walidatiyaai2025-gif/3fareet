# Release QA and Device Matrix

## PER-013 — Smoke test suite

Minimum prototype smoke:

1. App boots without uncaught exception.
2. Main menu renders and navigation reaches race flow.
3. Race starts, player input responds, pause/resume works.
4. Race can finish/restart/quit without stale state.
5. Garage opens, vehicle selection/equip works for unlocked cars.
6. Career Chapter 1 opens and prerequisite-gated events behave deterministically.
7. Audio initialization failure degrades safely without crashing.

## PER-014 — Prototype acceptance test

Acceptance requires:

- analyzer and automated tests Green;
- Android debug and release-skeleton APK build Green;
- no known P0 gameplay blocker;
- no unregistered production asset substitution;
- Project Status current in the same PR;
- real-device smoke evidence before the build is called Verified Release APK.

## PER-015 — Regression checklist

Before merging a release-affecting PR verify: boot, input, race state, camera, AI rivals, drift/nitro, power-ups, HUD, garage, career save/migration, asset validation, audio initialization, Android build and artifact publication.

## PER-016 — Device matrix

| Tier | RAM class | Target render profile | FPS target | Evidence required |
|---|---:|---|---:|---|
| Low | 4 GB | Low | 60 preferred / 30 fallback | physical Android device |
| Mid | 6–8 GB | Medium | 60 | physical Android device |
| High | 8+ GB | High | 60 | physical Android device |

## PER-017 — Low-tier profile

- Texture budget: 220 MB resident target.
- VFX cap: 600 active particles.
- Disable non-critical premium particles first.
- Allow 30 FPS fallback only if 60 FPS cannot be sustained.

Status remains `IN REVIEW` until measured on representative hardware.

## PER-018 — Mid-tier profile

- Texture budget: 350 MB resident target.
- VFX cap: 1,200 active particles.
- Standard lighting/VFX quality.
- 60 FPS target.

Status remains `IN REVIEW` until measured on representative hardware.

## PER-019 — High-tier profile

- Texture budget: 500 MB resident target.
- VFX cap: 2,000 active particles.
- Premium visual effects only while frame-time remains within budget.
- 60 FPS target.

Status remains `IN REVIEW` until measured on representative hardware.
