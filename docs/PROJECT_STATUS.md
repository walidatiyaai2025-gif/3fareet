# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-30 (Asia/Kuwait)  
**Overall status:** 🟡 **UNITY 3D RUNTIME ACTIVE — PERCEPTUAL 3D / PREMIUM VISUAL / FULL ANDROID CLOSURE OPEN**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead. لا يجوز دمج PR يغيّر Task/Phase/Blocker/Asset/Build/Release truth بدون تحديث هذه الصفحة في نفس PR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Worker governance | 🟡 PR pending | Root `AGENTS.md` + AI-first Unity 3D execution plan are being established to keep parallel Codex workers on one path |
| Canonical new 3D runtime | 🟢 Unity active | Real-device diagnostics show Unity-side authored Cairo runtime systems active |
| Legacy Flutter/Flame prototype | ⚪ Legacy / non-authoritative for new 3D core | Existing code/history may remain, but new vehicle/camera/world/traffic/rendering/VFX work should follow Unity unless explicitly tasked otherwise |
| Authored Cairo route | 🟢 Runtime evidence | `cairo-night-vertical-slice-v1`, 24 control points, 72 runtime segments |
| Authored road/curb | 🟢 Runtime evidence | `SM_Track_CairoRoad_A` + `SM_Track_CairoCurb_A` active |
| Night lighting bootstrap | 🟢 Runtime evidence | `AFAREET_NIGHT_LIGHTING_NORMALIZED`, active `Moon Light` |
| Building variation | 🟡 Functional but visually limited | Runtime shows 3 facades / 2 awnings / 1 sign; U3D density/variation expansion remains open |
| Garage session | 🟢 Initialization evidence | `afareet_king` equipped; no legacy migration/invalid recovery in shown excerpt |
| Chase-camera perceptual depth | 🔴 Open visual gate | Must be verified by raw gameplay screenshots/device evidence, not assumed from technical camera existence |
| Vehicle grounding/mass | 🔴 Open visual gate | Wheel/suspension/body/contact-shadow acceptance still required |
| Cairo foreground/midground/background density | 🔴 Open visual gate | Current goal is a convincing city around the road, not track-in-empty-space |
| Traffic / parked scale cues | 🔴 Open visual gate | Required for depth/scale and later performance tuning |
| Road/material depth | 🔴 Open visual gate | Curbs exist; flat-strip appearance still must be closed through height/material/decal pass |
| Premium visual direction | 🔴 Open | Updated toward grounded Cairo-after-midnight premium 3D rather than excessive neon |
| Android runtime evidence | 🟡 Partial real-device evidence | Initialization excerpt exists; complete player/camera/traffic/race/FPS/exceptions evidence still required |
| Verified release APK | 🔴 Not established by current evidence | Do not promote a build without exact-SHA + real-device smoke + visual/performance evidence |
| Backend architecture | 🟢 Locked boundary | `Game Client → HTTPS API → Laravel → MySQL`; direct client→MySQL prohibited |

## Current governing program

The controlling execution program is the **Unity AI-First 3D Closure**:

- [`../AGENTS.md`](../AGENTS.md)
- [`AI_AUTONOMOUS_3D_EXECUTION_PLAN.md`](AI_AUTONOMOUS_3D_EXECUTION_PLAN.md)
- [`tasks/06-UNITY-AI-3D-CLOSURE.md`](tasks/06-UNITY-AI-3D-CLOSURE.md)

The owner should not be required to manually model, place Unity objects, wire routine references or operate Blender for production tasks that can be automated.

## Real-device diagnostic baseline — 2026-08-30

Evidence is persisted in:

[`qa/2026-08-30-unity-device-diagnostic-baseline.md`](qa/2026-08-30-unity-device-diagnostic-baseline.md)

Known device/runtime facts:
- HONOR ALI-NX1;
- Android 15 / API 35;
- Adreno 710;
- 2652×1200;
- authored Cairo route active;
- authored road/curb active;
- building variation active;
- Moon Light normalized/active;
- garage session initialization active.

This does **not** yet prove full race lifecycle, final camera quality, traffic, stable FPS or absence of later exceptions.

## Highest priorities next

1. `U3D-GOV-001/002` — identify/reconcile canonical Unity branch/head and remove remaining architecture ambiguity.
2. `U3D-CAM-*` + `U3D-VEH-*` — make the raw frame read as true third-person 3D and ground the vehicle.
3. `U3D-ROAD-*` + `U3D-WLD-*` + `U3D-LGT-*` — dense Cairo depth, road height/material variation and lighting hierarchy.
4. `U3D-TRF-*` — parked/ambient traffic for scale.
5. `U3D-QA-003/004` — screenshot/device gates for G1/G2.
6. `U3D-QA-006` — frame pacing / quality tiers on Adreno-710-class hardware.
7. `U3D-REL-*` — exact-SHA Android candidate and real-device verified promotion.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-U3D-01 | 🔴 High | Repository history contains Flutter/Flame architecture while live 3D runtime evidence is Unity | Complete canonical-head reconciliation and keep docs/interfaces aligned |
| STS-U3D-02 | 🔴 High | Technical 3D can still look flat/2D | Close camera + grounding + depth composition gates before broad feature expansion |
| STS-U3D-03 | 🔴 High | No current screenshot evidence proving premium third-person 3D presentation | Capture raw HUD-off representative frames after U3D-CAM/VEH work |
| STS-U3D-04 | 🔴 High | Cairo density/traffic/road-material depth not yet visually verified | Execute U3D-WLD/ROAD/LGT/TRF vertical-slice tasks |
| STS-U3D-05 | 🟡 Medium | Current diagnostic excerpt ends before full player/camera/traffic/race/FPS closure | Extend `AFAREET_` runtime markers and collect longer real-device evidence |
| STS-U3D-06 | 🟡 Medium | Parallel workers can create duplicate systems/conflicting interfaces | Enforce `AGENTS.md`, Task IDs, Module Locks and focused branches |

## Current gates

### G0 — Live-state reconciliation
**Status:** 🟡 In progress / governance PR pending.

### G1 — Camera + vehicle grounding
**Status:** 🔴 Open.

### G2 — Dense Cairo street slice
**Status:** 🔴 Open.

### G3 — Premium moving frame
**Status:** 🔴 Open.

### G4 — Integrated playable event loop
**Status:** 🟡 Existing systems may contribute, but not re-verified against current canonical Unity head here.

### G5 — Android performance / release evidence
**Status:** 🔴 Open for current final closure.

## Last verified APK

**Status:** Do not infer from old Flutter-era documentation.  
A current Unity APK may only be promoted as verified after exact-SHA build metadata, real-device smoke, diagnostics, visual review and performance evidence are all attached.

## Source of truth links

- [Repository Constitution](../AGENTS.md)
- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [AI Autonomous 3D Execution Plan](AI_AUTONOMOUS_3D_EXECUTION_PLAN.md)
- [Unity AI-First 3D Closure Tasks](tasks/06-UNITY-AI-3D-CLOSURE.md)
- [Art Direction](ART_DIRECTION.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Task Register](TASK_REGISTER.md)
- [Missed Assets](MISSED_ASSETS.md)
- [2026-08-30 Unity Device Diagnostic Baseline](qa/2026-08-30-unity-device-diagnostic-baseline.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
