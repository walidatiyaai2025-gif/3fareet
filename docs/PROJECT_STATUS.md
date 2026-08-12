# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 15:34 (Asia/Kuwait)  
**Overall status:** 🟠 **P1 GAMEPLAY RACE LOOP IN REVIEW — FIRST PREVIEW APK ACTIVE — VISUAL/CAMERA/AI GATES OPEN**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة، Art Direction، Task Register وstatus guard موجودة؛ GOV reconciliation ما زال مطلوبًا |
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-010 Verified مع analyze/tests/debug APK evidence |
| First preview APK | 🟢 Pipeline active | CI يرفع Debug APK preview artifact؛ ليس Verified Release APK |
| GAMEPLAY-050 | 🟠 In review | **50 Task بالضبط** منفذة على branch نظيف من `main`: PRO-011→016 + VEH-001→016 + DRF-001→012 + RAC-001→016 |
| Vehicle / Driving | 🟠 In review | throttle/brake/reverse/steering/grip/slip/drift/collision/off-track/reset/tuning/preset موجودة |
| Magic Drift / Nitro | 🟠 In review | Spirit charge + anti-abuse + 3 feedback levels + Nitro curve/cooldown/hooks/UI states موجودة |
| Race core | 🟠 In review | track/start grid/countdown/checkpoints/laps/finish/timer/state/ranking/wrong-way/OOB/respawn/result/restart موجودة |
| Touch controls / lifecycle | 🟠 In review | steer/throttle/brake/drift/nitro + pause/reset/restart + app lifecycle integration + TUNE overlay موجودة |
| Premium visual direction | 🔴 Not started | VIS tasks ما زالت TODO؛ الـHUD الحالي لا يغلق Premium Visual Gate |
| Camera / AI | 🔴 Not started | CAM وAI الأساسية ما زالت TODO |
| Backend architecture | 🟢 Locked | Laravel API + MySQL؛ direct Flutter→MySQL ممنوع |
| Backend implementation / Online / Seasons | ⚪ Deferred | التنفيذ الكبير مؤجل خلف P1 |
| Android verified release APK | 🔴 None | CI artifacts ليست Verified APK؛ real-device smoke test غير منفذ بعد |

## Verified foundation — PRO-001 → PRO-010

**Status:** `VERIFIED`  
**Evidence:** [`work/PRO-001-010.md`](work/PRO-001-010.md)

Foundation CI أثبت dependency resolution و`flutter analyze` و`flutter test` وAndroid debug build. كما تم إصلاح floating-point boundary bug في fixed-step scheduler قبل التحقق النهائي.

## Current engineering batch — GAMEPLAY-050

**Owner:** Principal Mobile Game Architect  
**Branch:** `agent/gameplay-050-race-loop-v2`  
**Status:** `IN REVIEW`  
**Evidence:** [`work/GAMEPLAY-050.md`](work/GAMEPLAY-050.md)

### Task count — exactly 50

- PRO-011 → PRO-016 = 6
- VEH-001 → VEH-016 = 16
- DRF-001 → DRF-012 = 12
- RAC-001 → RAC-016 = 16
- **Total = 50**

### Delivered architecture

- fixed-step deterministic gameplay simulation suitable for future client prediction/reconciliation;
- normalized input snapshots plus actual touch controls;
- arcade acceleration/braking/reverse, speed-sensitive steering, grip and lateral slip;
- drift entry/sustain/exit, max speed, collision response, off-track slowdown and safe reset;
- runtime vehicle tuning surface and prototype car preset;
- Spirit Energy charge with low-speed abuse guard and three drift feedback tiers;
- Nitro activation/acceleration curve/drain/cooldown plus trail/camera/audio hooks and UI meter states;
- ordered checkpoint/lap/finish race state machine, race timer/ranking/wrong-way/out-of-bounds/safe respawn/results;
- Android app lifecycle pause/resume, restart and reset flow;
- debug build script, release skeleton build script, smoke checklist and prototype tag policy.

### Verification gate before VERIFIED

The batch remains `IN REVIEW` until the clean PR from current `main` proves Green:

- `flutter analyze`
- full `flutter test` suite including vehicle/Spirit/race/session tests
- Android debug APK build
- Android release skeleton APK build
- preview artifact upload
- Project Status Freshness Guard

## APK classification

The CI pipeline publishes preview APK artifacts for developer testing. They must **not** be copied automatically to `Last verified APK released/`.

A real `Verified Release APK` requires:

- release candidate from `main`;
- real Android device smoke test using [`SMOKE_TEST_CHECKLIST.md`](SMOKE_TEST_CHECKLIST.md);
- recorded Version, Commit SHA, Build date, Device/API, Tester, result and SHA-256;
- only the latest verified APK retained in `Last verified APK released/`.

## Architecture decisions now locked

- Gameplay simulation consumes fixed-step time, not variable frame delta.
- Input remains UI-neutral so multiplayer command prediction/reconciliation can reuse the same contract.
- Vehicle physics, Spirit/Nitro and race rules do not depend on Flutter widgets.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.
- Backend/Online implementation remains behind the P1 playable/visual/performance gate.

## Current phase

### 🟡 P0 — Foundation / Team Control
Core executable foundation is Verified. PRO-011→016 are in the current 50-task review batch. GOV reconciliation remains separate.

### 🟠 P1 — Playable Prototype Gate
The code-level driving/race loop is now implemented, but **P1 is not closed** until all of these are delivered:

- Cairo/Egyptian Fantasy track visual implementation and Premium Visual Gate;
- racing camera and feedback integration;
- at least 1 AI opponent;
- real-device driving feel test (VEH-017);
- integrated track-completion verification (RAC-017);
- real-device Android Release APK smoke test;
- latest successful verified APK copied to `Last verified APK released/` with metadata and SHA-256.

## Highest priorities after GAMEPLAY-050 verification

1. CAM-001 → CAM-005 — follow/look-ahead/damping/drift/nitro camera.
2. AI-001 → AI-006 — racing line/path/throttle/steering/braking/drift zones.
3. VIS-001 → VIS-006 — Art Bible/color/lighting/material/road/landmark silhouette gates.
4. RAC-017 + VEH-017 — integrated determinism and real-device feel verification.
5. First real-device verified Release APK.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B07 | 🟠 Medium | GAMEPLAY-050 waits for current clean PR CI | No task becomes VERIFIED before Green checks |
| STS-B03 | 🔴 High | Premium VIS tasks remain TODO | Start VIS in parallel after gameplay CI stabilizes |
| STS-B08 | 🔴 High | Camera and AI are still missing | Next batch targets CAM + AI |
| STS-B04 | 🔴 High | No real-device Verified Release APK | P1 cannot close from CI artifacts alone |
| STS-B05 | 🟡 Medium | GOV register still understates some implemented governance | Team Lead reconciliation remains required |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Full Task Register](TASK_REGISTER.md)
- [Premium Visual Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [GAMEPLAY-050 Evidence](work/GAMEPLAY-050.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
