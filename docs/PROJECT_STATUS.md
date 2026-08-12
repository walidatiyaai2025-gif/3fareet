# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-13 01:08 (Asia/Kuwait)  
**Overall status:** 🟡 **50-TASK BATCH EXECUTING — TASKS 1→20 IMPLEMENTED / STACKED REVIEW**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-016 Verified |
| GAMEPLAY-050 | 🟢 Verified | VEH-001→016 + DRF-001→012 + RAC-001→016 Verified |
| P1-NEXT-050 | 🟢 Verified | CAM-001→011 + AI-001→018 + UIX-001→016 + PWR-001→005 |
| Power-ups | 🟡 In review | PWR-006→014 implemented in PR #9 |
| Garage | 🟡 In review | GAR-001→014 now implemented across stacked PRs |
| Career | 🟡 In review | CAR-001→015 implemented across current stacked batches |
| Asset pipeline | 🟡 In review | ART-001 folder structure + ART-002 naming convention defined; ART-011 remains separately IN REVIEW by team |
| Premium visual direction | 🔴 Open | VIS screenshot/device gate remains required |
| Android verified release APK | 🔴 None | real-device smoke test still required |
| Backend architecture | 🟢 Locked | `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct Flutter→MySQL prohibited |

## 50-task execution manifest

Master coordination: Issue #11 / manifest #32.

### Tasks 1–10 — implemented in PR #33

Scope: `GAR-012→014` + `CAR-001→007`.

Implemented:
- versioned Garage local persistence codec;
- Garage catalog validation;
- first four vehicle archetypes;
- career chapter and map/navigation models;
- race-node foundation;
- circuit, time-trial, elimination, drift-challenge and boss race definitions.

Status: `IN REVIEW` pending CI/build evidence.

### Tasks 11–20 — current branch

Branch: `feature/CAR-008-ART-002-progression-assets`.

Scope: `CAR-008→015` + `ART-001→002`.

Implemented:
- star/objective system;
- unlock prerequisite checks;
- reward table model and idempotent reward claims;
- Chapter 1 objective/reward content;
- chapter completion flow;
- offline Career save codec;
- save migration/versioning from legacy data;
- offline progression tests;
- runtime asset folder structure standard;
- asset naming convention standard.

Status: `IN REVIEW` pending CI/build evidence.

## Architecture now locked

- Career progression is pure Dart and backend-independent for the prototype.
- Career saves are explicitly versioned and legacy saves migrate through a codec boundary.
- Reward claims track stable reward IDs to avoid duplicate local grants.
- Chapter progression depends on deterministic stars/completion state rather than UI state.
- Garage persistence remains separate from immutable catalog definitions.
- Runtime asset paths use domain folders and stable lowercase snake_case naming.
- ART-011 is not modified by this batch because another team workstream already owns it IN REVIEW.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.

## P1 Playable Prototype Gate

**Status:** 🟡 **CORE PLAYABLE SYSTEMS GROWING / VISUAL + DEVICE GATES STILL OPEN**

Still required:
- CI validation and orderly merge of stacked PR chain;
- VIS-001→VIS-014 implementation and screenshot/device Visual Gate;
- CAM-012 camera tuning on multiple devices;
- VEH-017 real-device driving-feel verification;
- RAC-017 integrated track-completion verification;
- remaining P0 engine/drift/nitro audio validation;
- real-device Android Release APK smoke test;
- final verified APK in `Last verified APK released/`.

## Highest priorities next

1. Continue tasks 21–30: ART-003→010 + ART-012→013 without touching ART-011.
2. Run CI across stacked Career/Garage batches and fix code failures only if present.
3. Continue tasks 31–40: ART-014 + PER-001→009.
4. Continue tasks 41–50: PER-010→019.
5. Execute VIS and real-device verification gates before claiming full P1 VERIFIED.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B03 | 🔴 High | Premium VIS gate remains open | Implement and perform screenshot/device review |
| STS-B04 | 🔴 High | No real-device Verified Release APK | Smoke-test a `main` release candidate on Android hardware |
| STS-B11 | 🟡 Medium | CAM-012/VEH-017/RAC-017 require device evidence | Run device/integrated verification |
| STS-B13 | 🟡 Medium | Current work is a stacked PR chain | Merge predecessors in order after Green CI |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Prototype Core Tasks](tasks/01-PROTOTYPE-CORE.md)
- [Gameplay/UI/Offline Tasks](tasks/02-GAMEPLAY-UI-OFFLINE.md)
- [Asset/Performance/Release Tasks](tasks/05-ASSETS-PERFORMANCE-RELEASE.md)
- [Asset Pipeline](ASSET_PIPELINE.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Art Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
