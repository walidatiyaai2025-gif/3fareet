# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-13 00:15 (Asia/Kuwait)  
**Overall status:** 🟡 **PWR-006→014 + GAR-001 IN REVIEW — PREMIUM VISUAL / REAL-DEVICE GATES STILL OPEN**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-016 Verified |
| GAMEPLAY-050 | 🟢 Verified | VEH-001→016 + DRF-001→012 + RAC-001→016 Verified |
| P1-NEXT-050 | 🟢 Verified | CAM-001→011 + AI-001→018 + UIX-001→016 + PWR-001→005 |
| Camera | 🟢 Verified core | CAM-012 device tuning remains TODO |
| Offline AI | 🟢 Verified core | deterministic rivals integrated into RaceSession |
| UI / UX | 🟢 Verified core | Splash → Main Menu → Mode Select → Loading → Race plus HUD/Pause/Result/Error |
| Power-ups | 🟡 In review | PWR-006→014 implemented: trap, nitro boost, slow, multiplier, duration manager, immunity/stacking, AI policy, event hooks, cleanup tests |
| Garage | 🟡 In review | GAR-001 car catalog schema implemented with validation/unlock filtering |
| Rap × Shaabi music | 🟡 Integrating | existing audio integration preserved; real-device listening validation still required |
| Premium visual direction | 🔴 Open | VIS tasks require screenshot/device review and Team Lead approval |
| Android build evidence | 🟡 Running | PR #9 Flutter Prototype CI is executing for the new gameplay batch |
| Android verified release APK | 🔴 None | real-device smoke test still required |
| Backend architecture | 🟢 Locked | `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct Flutter→MySQL prohibited |

## Current engineering batch — PR #9

**Branch:** `feature/PWR-006-GAR-001-gameplay-batch`  
**Scope:** 10 P1 tasks  
**Status:** `IN REVIEW`  
**Tasks:** `PWR-006→PWR-014` + `GAR-001`

### Implemented scope
- Asphalt Shard trap.
- Nitro Spirit boost.
- Traffic Curse slow.
- Enchanted Pound multiplier.
- Shared effect duration manager.
- Immunity and stacking rules.
- AI power-up usage policy/interface.
- VFX/audio event hooks via gameplay events.
- End-of-race cleanup behavior and tests.
- Car catalog schema with validation and unlock filtering.

### Validation
- Expanded `test/powerup_system_test.dart` across activation, expiration, immunity, AI decisions, lifecycle hooks and cleanup.
- Added `test/car_catalog_test.dart` for valid catalog construction, unlock filtering and duplicate-ID rejection.
- Project Status Freshness Guard initially failed because this dashboard was not updated in the same PR; this commit resolves that process failure.
- Tasks remain `IN REVIEW` until the new CI run is Green. They must not be promoted to `VERIFIED` before build/test evidence passes.

## Architecture now locked

- Power-up rules remain pure Dart and isolated from rendering/networking.
- Gameplay effects are represented as explicit timed state rather than scattered booleans.
- Power-up consumers can subscribe to semantic events for VFX/audio without coupling the gameplay kernel to Flame audio/rendering.
- AI decisions use a dedicated policy interface so future rival logic can change without altering power-up state rules.
- Garage catalog validation rejects duplicate IDs and invalid stat ranges before UI consumption.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.

## P1 Playable Prototype Gate

**Status:** 🟡 **GAMEPLAY + CAMERA + AI + UI CORE READY / FULL P1 NOT VERIFIED**

Still required:
- Green CI for PR #9 and promotion of its 10 tasks to VERIFIED;
- CAM-012 camera tuning on multiple devices;
- VIS-001→VIS-014 implementation and screenshot/device Visual Gate;
- VEH-017 real-device driving-feel verification;
- RAC-017 integrated track-completion verification;
- remaining P0 engine/drift/nitro audio validation;
- real-device Android Release APK smoke test;
- final verified APK in `Last verified APK released/`.

## Highest priorities next

1. Finish CI/review and merge PR #9 if Green.
2. VIS implementation + screenshot review against `ART_DIRECTION.md`.
3. CAM-012 + VEH-017 + RAC-017 real-device verification.
4. Remaining P0 audio/SFX integration and listening validation.
5. Continue Garage tasks after GAR-001 without colliding with team-owned modules.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B03 | 🔴 High | Premium VIS gate remains open | Implement and perform screenshot/device review |
| STS-B04 | 🔴 High | No real-device Verified Release APK | Smoke-test a `main` release candidate on Android hardware |
| STS-B10 | 🟡 Medium | Engine/drift/nitro gameplay SFX still incomplete | Generate/acquire and validate P0 SFX |
| STS-B11 | 🟡 Medium | CAM-012/VEH-017/RAC-017 require device/integration evidence | Run device and integrated race verification |
| STS-B12 | 🟡 Medium | PR #9 pending full CI verification | Keep tasks IN REVIEW until Green CI |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Prototype Core Tasks](tasks/01-PROTOTYPE-CORE.md)
- [Gameplay/UI/Offline Tasks](tasks/02-GAMEPLAY-UI-OFFLINE.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Art Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
