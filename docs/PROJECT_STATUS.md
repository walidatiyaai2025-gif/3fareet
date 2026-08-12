# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 20:43 (Asia/Kuwait)  
**Overall status:** 🟡 **POWER-UP EXPANSION VERIFIED — PREMIUM VISUAL / REAL-DEVICE GATES STILL OPEN**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-016 Verified |
| GAMEPLAY-050 | 🟢 Verified | VEH-001→016 + DRF-001→012 + RAC-001→016 Verified |
| P1-NEXT-050 | 🟢 Verified | **50 tasks exactly:** CAM-001→011 + AI-001→018 + UIX-001→016 + PWR-001→005 |
| Camera | 🟢 Verified core | follow/look-ahead/damping/drift/nitro/crash/air/FOV/shake/accessibility/bounds verified; CAM-012 device tuning remains TODO |
| Offline AI | 🟢 Verified core | racing line + controls + behavior + three AI rivals + stuck/finish rules integrated into RaceSession |
| UI / UX | 🟢 Verified core | Splash → Main Menu → Mode Select → Loading → Race plus HUD/Pause/Result/Error, SafeArea, RTL and text-scale clamp |
| Power-ups | 🟢 Verified | PWR-001→014 verified; Trap/Nitro/Curse/Multiplier, duration/stacking, immunity, AI policy, feedback hooks and cleanup covered |
| Garage catalog | 🟢 Verified first slice | GAR-001 typed/versioned catalog schema verified with JSON round-trip tests |
| Rap × Shaabi music | 🟡 Integrating | AST-061 audio integration from `main` is preserved; real-device listening validation still required |
| Premium visual direction | 🔴 Open | VIS tasks require screenshot/device review and Team Lead approval; not claimed by this code batch |
| Android build evidence | 🟢 CI verified | PWR-GAR-010 code head passed Debug APK + Release Skeleton APK + artifact upload |
| Android verified release APK | 🔴 None | CI artifacts are build evidence only; real-device smoke test still required |
| Backend architecture | 🟢 Locked | `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct Flutter→MySQL prohibited |

## Verified engineering batch — PWR-GAR-010

**Owner:** Principal Mobile Game Architect  
**Scope:** **10 tasks exactly**  
**Status:** `VERIFIED`  
**Branch:** `feature/PWR-006-GAR-001-gameplay-slice`  
**Verified code head:** `c565cd8d694b42c62477934406a1c9bbf0bc7b96`  
**Flutter Prototype CI:** `31623179639` — SUCCESS (attempt 2)  
**Project Status Freshness Guard:** `31623179586` — SUCCESS  
**Evidence:** [`work/PWR-GAR-010.md`](work/PWR-GAR-010.md)

### Exact count
- PWR-006 → PWR-014 = 9
- GAR-001 = 1
- **Total = 10**

### Delivered code scope
- Asphalt Shard deployable trap and handling penalty.
- Nitro Spirit timed gameplay multiplier.
- Traffic Curse targeted slow effect.
- Enchanted Pound timed reward multiplier.
- Centralized duration/stacking manager.
- Eye Shield immunity rules for incoming Trap/Curse attacks.
- Deterministic AI power-up usage policy interface.
- VFX/audio feedback hooks without rendering coupling.
- Full race-scoped power-up cleanup/reset path.
- Versioned Garage car catalog schema that references deterministic vehicle definitions and is compatible with future Laravel HTTPS transport.

### Verification evidence
Run `31623179639` completed Green on the tested code head and proved:
- formatter/static-analysis gate passed;
- `flutter analyze` with zero issues;
- **41 tests passed** including focused PWR-006→014 and GAR-001 coverage;
- Android scaffold generation passed;
- Android Debug APK build passed;
- Android Release Skeleton APK build passed;
- preview APK artifact upload passed.

The first CI attempt failed only while downloading Gradle with a transient socket EOF after format/analyze/tests were already Green. Attempt 2 reran the unchanged code and completed successfully. Task-promotion commits after `c565cd8d694b42c62477934406a1c9bbf0bc7b96` are documentation-only.

## Verified engineering batch — P1-NEXT-050

**Owner:** Principal Mobile Game Architect  
**Scope:** **50 tasks exactly**  
**Status:** `VERIFIED`  
**Verified code head:** `86a6ea2afb273cab14730e61a152676dc90ea24f`  
**Flutter Prototype CI:** `31613691078` — SUCCESS  
**Project Status Freshness Guard:** `31613691026` — SUCCESS  
**Evidence:** [`work/P1-NEXT-050.md`](work/P1-NEXT-050.md)

### Exact count
- CAM-001 → CAM-011 = 11
- AI-001 → AI-018 = 18
- UIX-001 → UIX-016 = 16
- PWR-001 → PWR-005 = 5
- **Total = 50**

### Verification evidence
Run `31613691078` completed Green and proved:
- formatter check;
- `flutter analyze` with zero issues;
- complete tests including Camera, AI, UI-flow and Power-up coverage;
- Android scaffold generation;
- Android Debug APK build;
- Android Release Skeleton APK build;
- preview APK artifact upload.

The task-promotion commits after the verified code head are documentation-only. No tested application/gameplay code changed after that Green run.

## Architecture now locked

- Camera feedback consumes deterministic fixed-step state and feeds Flame's viewfinder as an adapter.
- Camera invalid values are sanitized and hard-bounded; accessibility can disable shake without changing simulation.
- Offline AI uses seeded deterministic decisions, making race behavior reproducible for later replay, multiplayer debugging and authoritative reconciliation work.
- Three AI rivals are now part of `RaceSession`; HUD position derives from actual race progress instead of a placeholder.
- Front-end UI remains outside the gameplay kernel and keeps a persistent Game instance beneath menus, preserving audio and simulation lifecycle boundaries.
- UI supports SafeArea, Arabic RTL and bounded accessibility text scaling.
- Power-up effect state remains pure Dart; rendering/audio attach through feedback hooks instead of owning gameplay state.
- Garage catalog data references `vehicleDefinitionId`; physics tuning remains in deterministic vehicle definitions.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.

## P1 Playable Prototype Gate

**Status:** 🟡 **GAMEPLAY + CAMERA + AI + UI + POWER-UP CORE READY / FULL P1 NOT VERIFIED**

Still required:
- CAM-012 camera tuning on multiple devices;
- VIS-001→VIS-014 implementation and screenshot/device Visual Gate;
- VEH-017 real-device driving-feel verification;
- RAC-017 integrated track-completion verification;
- remaining P0 engine/drift/nitro audio validation;
- real-device Android Release APK smoke test;
- final verified APK in `Last verified APK released/`.

## Highest priorities next

1. VIS implementation + screenshot review against `ART_DIRECTION.md`.
2. CAM-012 + VEH-017 + RAC-017 real-device verification.
3. Remaining P0 audio/SFX integration and listening validation.
4. Continue Garage/UI progression after visual gate work is protected.
5. First real-device Verified Release APK.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B03 | 🔴 High | Premium VIS gate remains open | Implement and perform screenshot/device review |
| STS-B04 | 🔴 High | No real-device Verified Release APK | Smoke-test a `main` release candidate on Android hardware |
| STS-B10 | 🟡 Medium | Engine/drift/nitro gameplay SFX still incomplete | Generate/acquire and validate P0 SFX |
| STS-B11 | 🟡 Medium | CAM-012/VEH-017/RAC-017 require device/integration evidence | Run device and integrated race verification |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Prototype Core Tasks](tasks/01-PROTOTYPE-CORE.md)
- [Gameplay/UI/Offline Tasks](tasks/02-GAMEPLAY-UI-OFFLINE.md)
- [PWR-GAR-010 Evidence](work/PWR-GAR-010.md)
- [P1-NEXT-050 Evidence](work/P1-NEXT-050.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Art Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
