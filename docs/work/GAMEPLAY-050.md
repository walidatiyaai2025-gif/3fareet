# GAMEPLAY-050 — Deterministic Race Loop Batch

**Owner:** Principal Mobile Game Architect  
**Scope:** 50 tasks exactly  
**Status:** IN REVIEW until CI evidence is Green

## Task count

- PRO-011 → PRO-016 = 6 tasks.
- VEH-001 → VEH-016 = 16 tasks.
- DRF-001 → DRF-012 = 12 tasks.
- RAC-001 → RAC-016 = 16 tasks.
- **Total = 50 tasks.**

## Architectural result

The gameplay kernel is intentionally deterministic and UI-neutral. Vehicle simulation, Spirit/Nitro and race rules consume normalized input snapshots and fixed time steps. This is the required shape for later multiplayer client prediction, authoritative reconciliation and replay/debug tooling.

The implementation adds an arcade vehicle model, drift state machine, Spirit charge/Nitro model, ordered checkpoints/laps/finish/results, safe respawn, wrong-way/out-of-bounds rules, lifecycle pause/resume, restart flow, Android debug/release build paths, smoke checklist and release-tag policy.

## Verification gate

- `flutter analyze` Green.
- all unit tests Green.
- Android debug APK builds.
- Android release skeleton APK builds.
- Project Status Freshness Guard Green.

Real-device driving feel (VEH-017), track completion determinism (RAC-017), camera, AI and Premium Visual Gate remain separate tasks and are not claimed by this batch.
