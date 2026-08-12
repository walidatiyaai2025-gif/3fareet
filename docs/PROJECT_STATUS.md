# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 18:26 (Asia/Kuwait)  
**Overall status:** 🟡 **GAMEPLAY CORE VERIFIED — P1-NEXT-050 IN REVIEW — PREMIUM VISUAL / REAL-DEVICE GATES STILL OPEN**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-016 Verified |
| GAMEPLAY-050 | 🟢 Verified | VEH-001→016 + DRF-001→012 + RAC-001→016 Verified |
| P1-NEXT-050 | 🟡 In review | **50 tasks exactly:** CAM-001→011 + AI-001→018 + UIX-001→016 + PWR-001→005 |
| Camera | 🟡 In review | follow/look-ahead/damping/drift/nitro/crash/air/FOV/shake/accessibility/bounds implemented; CAM-012 device tuning remains TODO |
| Offline AI | 🟡 In review | racing line + controls + behavior + 3 AI + stuck/finish rules integrated into RaceSession |
| UI / UX | 🟡 In review | Splash → Main Menu → Mode Select → Loading → Race plus HUD/Pause/Result/Error, SafeArea, RTL and text-scale clamp |
| Power-ups | 🟡 In review | definitions, spawn/pickup, collection, one-slot inventory and Eye Shield implemented |
| Rap × Shaabi music | 🟡 Integrating | AST-061 audio integration from current main is preserved in this branch; device listening validation still required |
| Premium visual direction | 🔴 Open | VIS tasks require screenshot/device review and Team Lead approval; not claimed by this code batch |
| Android verified release APK | 🔴 None | CI artifacts are build evidence only; real-device smoke test still required |
| Backend architecture | 🟢 Locked | `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct Flutter→MySQL prohibited |

## Current implementation batch — P1-NEXT-050

**Owner:** Principal Mobile Game Architect  
**Status:** `IN REVIEW`  
**Evidence:** [`work/P1-NEXT-050.md`](work/P1-NEXT-050.md)

### Exact count
- CAM-001 → CAM-011 = 11
- AI-001 → AI-018 = 18
- UIX-001 → UIX-016 = 16
- PWR-001 → PWR-005 = 5
- **Total = 50**

### Architectural changes
- Camera state is deterministic and driven from the existing fixed-step gameplay loop.
- Flame viewfinder consumes the camera controller output; invalid values are sanitized and bounded.
- Offline AI is independent of Flutter widgets and uses seeded deterministic behavior suitable for later replay/network debugging.
- Three AI rivals are part of `RaceSession`, and player position is now derived from AI progress.
- Front-end UI remains outside the gameplay kernel and keeps one persistent Game instance underneath menus to preserve audio/game lifecycle.
- UI provides premium dark/cyan/gold tokens, Arabic RTL, SafeArea and bounded accessibility text scaling.
- Initial power-up rules are pure Dart and isolated from rendering/network code.

## Verification required before promotion

The 50 tasks stay `IN REVIEW` until the PR head passes:
- formatter check;
- `flutter analyze` with zero issues;
- complete tests including new camera/AI/UI-flow/power-up tests;
- Android Debug APK;
- Android Release Skeleton APK;
- artifact upload;
- Project Status Freshness Guard.

## Previously verified engineering

### GAMEPLAY-050
- Verified CI run: `31596838749`.
- Vehicle/drift/nitro/race core remains the stable base for this batch.

### AST-061 audio baseline
The current `main` includes Rap/Trap × Egyptian Shaabi/Mahraganat prototype BGM integration through `PrototypeMusicController`. This branch was created from that audio-enabled main head, so the new Camera/AI/UI work does not remove or replace it.

## P1 Playable Prototype Gate

**Status:** 🟡 **CORE IMPLEMENTED / FULL P1 NOT VERIFIED**

Still required after this batch:
- CAM-012 camera tuning on multiple devices;
- VIS-001→VIS-014 visual implementation/review gate;
- VEH-017 real-device driving-feel verification;
- RAC-017 integrated track completion verification;
- production engine/drift/nitro SFX validation;
- real-device Android Release APK smoke test;
- final verified APK in `Last verified APK released/`.

## Highest priorities after P1-NEXT-050

1. Fix any CI findings on the current 50-task PR and promote only after Green evidence.
2. VIS implementation + screenshot review against `ART_DIRECTION.md`.
3. CAM-012 + VEH-017 + RAC-017 real-device verification.
4. Remaining P0 audio/SFX.
5. First real-device Verified Release APK.

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Prototype Core Tasks](tasks/01-PROTOTYPE-CORE.md)
- [Gameplay/UI/Offline Tasks](tasks/02-GAMEPLAY-UI-OFFLINE.md)
- [P1-NEXT-050 Evidence](work/P1-NEXT-050.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Art Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
