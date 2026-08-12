# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-13 00:24 (Asia/Kuwait)  
**Overall status:** 🟡 **GARAGE FLOW GAR-002→011 IN REVIEW — PR #9 PREDECESSOR RECHECKING CI**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-016 Verified |
| GAMEPLAY-050 | 🟢 Verified | VEH-001→016 + DRF-001→012 + RAC-001→016 Verified |
| P1-NEXT-050 | 🟢 Verified | CAM-001→011 + AI-001→018 + UIX-001→016 + PWR-001→005 |
| Power-ups | 🟡 In review | PWR-006→014 implemented in PR #9; 40 tests/analyze passed, Android build retrying after transient Gradle download EOF |
| Garage schema | 🟡 In review | GAR-001 implemented in PR #9 |
| Garage flow | 🟡 In review | GAR-002→011 implemented on stacked branch: list, detail, preview, cosmetics, stats, unlock, equip |
| Premium visual direction | 🔴 Open | VIS screenshot/device gate remains required |
| Android verified release APK | 🔴 None | real-device smoke test still required |
| Backend architecture | 🟢 Locked | `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct Flutter→MySQL prohibited |

## Predecessor engineering batch — PR #9

**Branch:** `feature/PWR-006-GAR-001-gameplay-batch`  
**Scope:** `PWR-006→014` + `GAR-001`  
**Status:** `IN REVIEW`

Validation on Flutter CI #40 reached:
- `flutter analyze`: zero issues;
- all **40 tests passed**;
- Android scaffold generated successfully;
- Android Debug APK build then failed only while Gradle wrapper was downloading with `java.net.SocketException: Unexpected end of file from server`.

That failed job was re-run because the observed failure is infrastructure/network-related rather than a test or analyzer regression.

## Current stacked engineering batch — GAR-002→011

**Branch:** `feature/GAR-002-011-garage-flow`  
**Base dependency:** `feature/PWR-006-GAR-001-gameplay-batch` / GAR-001 catalog schema  
**Scope:** 10 P1 tasks  
**Status:** `IN REVIEW`

### Implemented tasks
- GAR-002 Garage list with selected/locked/equipped state.
- GAR-003 Car detail screen.
- GAR-004 Preview model and asset-aware preview widget with safe fallback.
- GAR-005 Paint customization.
- GAR-006 Wheel customization.
- GAR-007 Magic trail customization.
- GAR-008 Spirit cosmetic slot.
- GAR-009 Normalized stat visualization for speed/acceleration/handling/nitro.
- GAR-010 Level-based unlock state.
- GAR-011 Equip flow guarded by unlock state.

### Engineering structure
- `garage_controller.dart` owns pure garage state/loadout rules and exposes a ChangeNotifier boundary for UI.
- `garage_screen.dart` provides responsive compact/wide list-detail layouts and customization controls.
- `garage_controller_test.dart` covers locked behavior, unlock progression, all four customization slots, preview synchronization, equip flow and stat bounds.
- Garage work remains isolated from race simulation, backend and persistence; GAR-012 persistence is intentionally still TODO.

## Architecture now locked

- Garage catalog remains the source of truth for vehicle identity, stats and unlock level.
- Runtime customization is represented as a per-vehicle `GarageLoadout` rather than mutating catalog definitions.
- Locked vehicles may be inspected but cannot be equipped/customized.
- Preview state is derived from selected catalog entry + loadout, allowing later replacement of placeholder assets without changing garage rules.
- No local persistence is claimed yet; that remains GAR-012.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.

## P1 Playable Prototype Gate

**Status:** 🟡 **GAMEPLAY + CAMERA + AI + UI CORE READY / FULL P1 NOT VERIFIED**

Still required:
- Green Android build evidence for PR #9 after infrastructure retry;
- CI validation for GAR-002→011 stacked PR;
- CAM-012 camera tuning on multiple devices;
- VIS-001→VIS-014 implementation and screenshot/device Visual Gate;
- VEH-017 real-device driving-feel verification;
- RAC-017 integrated track-completion verification;
- remaining P0 engine/drift/nitro audio validation;
- real-device Android Release APK smoke test;
- final verified APK in `Last verified APK released/`.

## Highest priorities next

1. Complete predecessor PR #9 CI retry and merge when Green.
2. Validate and merge GAR-002→011 stacked batch after its predecessor.
3. GAR-012 local persistence + GAR-013 config validation + GAR-014 first four archetypes.
4. VIS implementation + screenshot/device review.
5. CAM-012 + VEH-017 + RAC-017 real-device verification.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B03 | 🔴 High | Premium VIS gate remains open | Implement and perform screenshot/device review |
| STS-B04 | 🔴 High | No real-device Verified Release APK | Smoke-test a `main` release candidate on Android hardware |
| STS-B11 | 🟡 Medium | CAM-012/VEH-017/RAC-017 require device evidence | Run device/integrated verification |
| STS-B12 | 🟡 Medium | PR #9 Android build retry pending | Merge only after Green build evidence |
| STS-B13 | 🟡 Medium | GAR-002→011 depends on GAR-001 predecessor | Keep as stacked PR until PR #9 merges |

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
