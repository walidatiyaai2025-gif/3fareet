# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 15:40 (Asia/Kuwait)  
**Overall status:** 🟡 **GAMEPLAY CORE VERIFIED — P1 STILL OPEN FOR VISUAL + CAMERA + AI + REAL-DEVICE APK**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة، Art Direction، Task Register وstatus guard موجودة؛ GOV reconciliation ما زال مطلوبًا |
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-010 Verified |
| GAMEPLAY-050 | 🟢 Verified | **50 Task بالضبط** Verified: PRO-011→016 + VEH-001→016 + DRF-001→012 + RAC-001→016 |
| Vehicle / Driving | 🟢 Verified core | throttle/brake/reverse/steering/grip/slip/drift/collision/off-track/reset/tuning/preset Verified بالاختبارات والبناء |
| Magic Drift / Nitro | 🟢 Verified core | Spirit charge + anti-abuse + 3 feedback levels + Nitro curve/cooldown/hooks/UI states Verified |
| Race core | 🟢 Verified core | track/start grid/countdown/checkpoints/laps/finish/timer/state/ranking/wrong-way/OOB/respawn/result/restart Verified |
| Touch controls / lifecycle | 🟢 Verified core | steer/throttle/brake/drift/nitro + pause/reset/restart + lifecycle + TUNE overlay موجودة وتبني بنجاح |
| Android Debug APK | 🟢 CI verified | Debug APK build succeeded in GAMEPLAY-050 verification run |
| Android Release Skeleton | 🟢 CI verified | Release skeleton APK build succeeded and artifact upload succeeded |
| Premium visual direction | 🔴 Not started | VIS tasks ما زالت TODO؛ الـHUD الحالي لا يغلق Premium Visual Gate |
| Camera / AI | 🔴 Not started | CAM وAI الأساسية ما زالت TODO |
| Backend architecture | 🟢 Locked | Laravel API + MySQL؛ direct Flutter→MySQL ممنوع |
| Backend implementation / Online / Seasons | ⚪ Deferred | التنفيذ الكبير مؤجل خلف P1 |
| Android verified release APK | 🔴 None | CI release artifact ليس Verified Release APK؛ real-device smoke test غير منفذ بعد |

## Verified engineering batch — GAMEPLAY-050

**Owner:** Principal Mobile Game Architect  
**Scope:** **50 tasks exactly**  
**Status:** `VERIFIED`  
**Verified code head:** `70ab63797d7161e752006b4a97d3e842ab417543`  
**GitHub Actions run:** `31596838749`  
**Evidence:** [`work/GAMEPLAY-050.md`](work/GAMEPLAY-050.md)

### Task count

- PRO-011 → PRO-016 = 6
- VEH-001 → VEH-016 = 16
- DRF-001 → DRF-012 = 12
- RAC-001 → RAC-016 = 16
- **Total = 50**

### Verification evidence

Run `31596838749` completed Green and proved:

- dependency resolution;
- `flutter analyze` success;
- complete `flutter test` success including vehicle, Spirit/Nitro, race-controller and deterministic race-session tests;
- Android scaffold generation;
- Android Debug APK build;
- Android Release Skeleton APK build;
- preview APK artifact upload;
- Project Status Freshness Guard.

## Architecture now locked

- Gameplay simulation consumes fixed-step time, not variable frame delta.
- Input remains UI-neutral so multiplayer client prediction/reconciliation can reuse the same command contract.
- Vehicle physics, Spirit/Nitro and race rules do not depend on Flutter widgets.
- Runtime touch UI is an adapter over the same gameplay input contract.
- Race/checkpoint rules are deterministic and tested independently of rendering.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.

## APK classification

The CI pipeline now produces both Debug and Release Skeleton APK artifacts. These are **developer/build evidence**, not the final verified APK.

A file may enter `Last verified APK released/` only after:

- candidate comes from `main`;
- real Android device smoke test using [`SMOKE_TEST_CHECKLIST.md`](SMOKE_TEST_CHECKLIST.md);
- Version, Commit SHA, Build date, Device/API, Tester, result and SHA-256 are recorded;
- only the latest verified APK is retained there.

## P1 Playable Prototype Gate

**Status:** 🟡 **GAMEPLAY CORE READY / FULL P1 NOT VERIFIED**

Still required:

- Cairo/Egyptian Fantasy track visual implementation and Premium Visual Gate;
- racing camera and feedback integration;
- at least 1 AI opponent;
- VEH-017 real-device driving-feel verification;
- RAC-017 integrated track-completion verification;
- real-device Android Release APK smoke test;
- final verified APK in `Last verified APK released/`.

## Highest priorities next

1. CAM-001 → CAM-005 — follow/look-ahead/damping/drift/nitro camera.
2. AI-001 → AI-006 — racing line/path/throttle/steering/braking/drift zones.
3. VIS-001 → VIS-006 — color/lighting/material/road/landmark silhouette implementation.
4. VEH-017 + RAC-017 — device feel and integrated race verification.
5. First real-device verified Release APK.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B03 | 🔴 High | Premium VIS tasks remain TODO | Start VIS in parallel with Camera/AI |
| STS-B08 | 🔴 High | Camera and AI are missing | Next implementation batch targets CAM + AI |
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
