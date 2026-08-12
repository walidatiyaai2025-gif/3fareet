# PWR-GAR-010 — Power-up Expansion + Garage Schema

**Owner:** Principal Mobile Game Architect  
**Branch:** `feature/PWR-006-GAR-001-gameplay-slice`  
**Status:** IN REVIEW  
**Scope:** 10 tasks exactly

## Task scope

| # | Task | Delivery |
|---|---|---|
| 1 | PWR-006 | Asphalt Shard deployable trap + target handling penalty |
| 2 | PWR-007 | Timed Nitro Spirit gameplay multiplier |
| 3 | PWR-008 | Traffic Curse target slow effect |
| 4 | PWR-009 | Enchanted Pound timed reward multiplier |
| 5 | PWR-010 | Generic effect duration manager |
| 6 | PWR-011 | Shield immunity + deterministic stacking/refresh caps |
| 7 | PWR-012 | Pure-Dart AI power-up usage policy interface |
| 8 | PWR-013 | VFX/audio feedback event hooks |
| 9 | PWR-014 | Race-scoped power-up cleanup/reset verification |
| 10 | GAR-001 | Versioned typed car catalog transport schema |

## Architecture decisions

- Power-up gameplay state remains pure Dart and deterministic; Flame/UI layers consume modifiers and feedback events.
- Offensive effects are applied through an explicit target boundary; Eye Shield absorbs one incoming Trap/Curse hit.
- Duration and stacking logic is centralized so vehicle simulation does not own power-up timers.
- Feedback is emitted through a callback sink so VFX/audio can attach without contaminating gameplay logic.
- The Garage catalog references `vehicleDefinitionId` instead of duplicating physics tuning.
- The catalog JSON shape is intentionally compatible with the locked backend direction: Flutter/Flame consumes HTTPS API data from Laravel; it never connects directly to MySQL.

## Verification plan

CI must pass all existing and new gates before these tasks are promoted to VERIFIED:

- `dart format --output=none lib test`
- `flutter analyze`
- `flutter test`
- Android Debug APK build
- Android Release Skeleton APK build

New tests cover Trap deployment/hit/expiry, Nitro duration, Traffic Curse + Shield immunity, Enchanted Pound multiplier, duration/stacking rules, AI usage policy, feedback hooks, reset cleanup, and Garage catalog JSON round-trip/versioning.

## Verification evidence

Pending GitHub Actions PR run. This document must be updated with the Green workflow run ID before promotion to VERIFIED.
