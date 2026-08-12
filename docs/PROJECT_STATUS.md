# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 15:22 (Asia/Kuwait)  
**Overall status:** 🟠 **P1 GAMEPLAY RACE LOOP IN REVIEW — VISUAL/CAMERA/AI GATES STILL OPEN**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة، Art Direction، Task Register وstatus guard موجودة؛ GOV reconciliation ما زال مطلوبًا |
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-010 Verified سابقًا مع analyze/tests/debug APK evidence |
| Current 50-task gameplay batch | 🟠 In review | PRO-011→016 + VEH-001→016 + DRF-001→012 + RAC-001→016 منفذة وتنتظر CI على PR الحالي |
| Vehicle / Driving | 🟠 In review | acceleration/brake/reverse/steering/grip/slip/drift/collision/off-track/reset/tuning/preset موجودة بالكود والاختبارات |
| Magic Drift / Nitro | 🟠 In review | Spirit charge, anti-abuse, 3 feedback levels, Nitro curve/cooldown/hooks/UI states/balance موجودة |
| Race core | 🟠 In review | track/start grid/countdown/checkpoints/laps/finish/timer/state/ranking/wrong-way/OOB/respawn/result/restart موجودة |
| Touch controls / lifecycle | 🟠 In review | touch steer/throttle/brake/drift/nitro + pause/restart/reset وربط Android lifecycle موجود |
| Premium visual direction | 🔴 Not started | VIS tasks ما زالت TODO؛ الـHUD الحالي ليس إغلاقًا للـPremium Visual Gate |
| Camera / AI | 🔴 Not started | CAM وAI الأساسية ما زالت TODO |
| Backend architecture | 🟢 Baseline verified | Laravel API + MySQL؛ direct client-to-DB ممنوع؛ التنفيذ الكبير مؤجل بعد P1 |
| Missing assets | 🟡 Open | سجل `MISSED_ASSETS.md` ما زال مفتوحًا |
| Android verified release APK | 🔴 None | CI artifacts ليست Verified APK؛ لا يوجد real-device verified Release APK بعد |

## Current engineering batch — GAMEPLAY-050

**Owner:** Principal Mobile Game Architect  
**Branch:** `agent/gameplay-050-race-loop`  
**Task count:** **50 exactly**  
**Status:** `IN REVIEW` pending CI  
**Evidence:** [`work/GAMEPLAY-050.md`](work/GAMEPLAY-050.md)

### Included tasks

- PRO-011 → PRO-016 = 6
- VEH-001 → VEH-016 = 16
- DRF-001 → DRF-012 = 12
- RAC-001 → RAC-016 = 16
- **Total = 50**

### Architecture delivered

- deterministic fixed-step vehicle simulation consumable by future multiplayer prediction/reconciliation;
- normalized touch-independent input snapshots;
- arcade acceleration/braking/reverse and speed-dependent steering;
- grip/lateral slip/drift entry/sustain/exit and safe collision/off-track behavior;
- Spirit Energy with low-speed abuse guard, 3 drift feedback tiers and Nitro consumption/cooldown;
- Trail/Camera/Audio feedback hooks without coupling gameplay core to VFX/audio implementations;
- ordered checkpoint/lap/finish state machine and deterministic one-lap prototype flow;
- wrong-way/out-of-bounds/safe respawn/result/restart/quit contracts;
- real touch-control overlay for throttle, brake, steering, drift and nitro;
- pause/resume app lifecycle integration and restart/reset controls;
- Android debug build path + release skeleton path + smoke checklist + release-tag policy.

### CI gate before VERIFIED

The batch remains `IN REVIEW` until the current PR proves all of the following Green:

- `flutter analyze`
- complete `flutter test` suite including vehicle, Spirit, race and deterministic session tests
- Android debug APK build
- Android release skeleton APK build
- preview artifact upload
- Project Status Freshness Guard

## Current phase

### 🟡 P0 — Foundation / Team Control
Executable foundation is Verified. PRO-011→016 are now in the current review batch; GOV reconciliation remains separate.

### 🟠 P1 — Playable Prototype Gate
The code-level driving/race loop is now substantially implemented, but **P1 is not closed** because the following remain mandatory:

- Cairo/Egyptian Fantasy track visual implementation and Premium Visual Gate;
- racing camera and feedback integration;
- at least 1 AI opponent;
- real-device driving feel test (VEH-017);
- deterministic track-completion verification (RAC-017) after integration;
- real-device Android Release APK smoke test;
- latest successful verified APK copied to `Last verified APK released/` with metadata and SHA-256.

## Highest priorities next after GAMEPLAY-050 verification

1. CAM-001 → CAM-005 — follow/look-ahead/damping/drift/nitro camera.
2. AI-001 → AI-006 — racing line/path/throttle/steering/braking/drift zones.
3. VIS-001 → VIS-006 — Art Bible/color/lighting/material/road/landmark silhouette gates.
4. RAC-017 + VEH-017 — integrated determinism and real-device feel verification.
5. Produce first real-device verified Release APK only after these gates are credible.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B07 | 🟠 Medium | GAMEPLAY-050 waits for current CI evidence | No task in the batch becomes VERIFIED before Green PR checks |
| STS-B03 | 🔴 High | Premium VIS tasks remain TODO | Start VIS in parallel immediately after gameplay CI is stable |
| STS-B08 | 🔴 High | Camera and AI are still missing | Next implementation batch targets CAM + AI |
| STS-B04 | 🔴 High | No real-device Verified Release APK | P1 cannot close from CI artifacts alone |
| STS-B05 | 🟡 Medium | GOV register still understates some implemented governance | Team Lead reconciliation remains required |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

CI Debug/Release artifacts are build evidence only. A file enters this folder only after a real-device smoke test records Version, Commit SHA, Build date, Device/API, Tester, result and SHA-256.

## Team workload rules

المصدر التفصيلي هو [`TASK_REGISTER.md`](TASK_REGISTER.md).  
**State vocabulary:** `TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`

- Owner واحد لكل Task.
- Module Lock عند لمس نفس interfaces/files.
- كل PR يذكر Task IDs.
- تغير الحالة = تحديث ملف المهمة + هذه الصفحة في نفس PR.
- `DONE` لا تساوي `VERIFIED`; Evidence شرط أساسي.

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Full Task Register](TASK_REGISTER.md)
- [Premium Visual Direction](ART_DIRECTION.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Missed Assets](MISSED_ASSETS.md)
- [GAMEPLAY-050 Evidence](work/GAMEPLAY-050.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
