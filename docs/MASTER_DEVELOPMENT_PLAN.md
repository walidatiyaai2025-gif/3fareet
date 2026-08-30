# عفاريت الأسفلت — Master Development Plan

**Document:** AFA-PLAN-001  
**Version:** 2.0 — Unity AI-First 3D Baseline  
**Date:** 2026-08-30  
**Status:** Controlled team reference

> قاعدة التحكم: أي تعديل في Scope أو Architecture أو ترتيب الأولويات أو Art Direction يجب أن يدخل الخطة وسجل المهام قبل بدء التنفيذ.

## Project Status Dashboard — Mandatory

الصفحة التنفيذية الرسمية لمعرفة الوضع الحالي للمشروع هي [`docs/PROJECT_STATUS.md`](PROJECT_STATUS.md).

**قاعدة إلزامية للفريق:** أي PR يغير حالة Task أو Phase أو Milestone أو Blocker/Risk أو Asset مؤثر أو Build/Release أو محتوى `Last verified APK released/` يجب أن يحدّث `PROJECT_STATUS.md` في **نفس PR**. تحديث صفحة الحالة جزء من Definition of Done وليس عملاً مؤجلاً بعد الدمج.

إذا تعارضت صفحة الحالة مع ملفات المهام التفصيلية، يجب إصلاح التعارض في نفس PR قبل الدمج، ولا يجوز رفع نسبة تقدم أو إعلان `VERIFIED` بدون Evidence.

## Repository worker constitution

كل Codex/AI worker أو مطور يجب أن يبدأ من [`../AGENTS.md`](../AGENTS.md).  
التنفيذ التفصيلي لمسار Unity 3D موجود في [`AI_AUTONOMOUS_3D_EXECUTION_PLAN.md`](AI_AUTONOMOUS_3D_EXECUTION_PLAN.md).

## Product vision

- Original 3D casual/arcade street racing بطابع مصري/قاهري ليلي مميز.
- Premium cinematic third-person presentation: depth, vehicle mass, traffic, city density and lighting hierarchy.
- Core loop: Drive → Drift → Spirit Charge → Nitro Spirit → Overtake/Attack → Reward.
- Offline Career + Time Trial + Challenges + Bosses.
- Online Real-time PvP لاحقًا بعد إثبات الـoffline/core runtime.
- Garage + customization + seasons + Asphalt Pass.
- Target: 60 FPS حيث يكون واقعيًا على الأجهزة المستهدفة مع Low/Medium/High quality tiers.

## Mandatory Art Direction

المرجع التفصيلي موجود في [`ART_DIRECTION.md`](ART_DIRECTION.md). أي صور مرجعية يقدمها المالك هي **quality/feel references only** وليست إذنًا لنسخ assets أو خرائط أو UI أو علامات سيارات.

**ملخص الهوية:**
- Cairo night cinematic street racing مع لمسة fantasy أصلية ومضبوطة.
- Midnight Navy/Black + Cyan/Turquoise + Warm Gold/Amber، بدون neon/bloom مفرط.
- سيارات 3D أصلية بstance قوي وخامات مقروءة وانعكاسات محسوبة.
- Drift/Nitro VFX جزء من الهوية.
- UI داكن premium لا يغطي الطريق.
- الأهم: المشهد نفسه يجب أن يقرأ فورًا كلعبة سباق 3D حتى عند إخفاء الـHUD.

**قاعدة:** إذا نجح الأداء والكود لكن الشكل بعيد عن `ART_DIRECTION.md` و3D acceptance gates، تكون المهمة `DONE` وليست `VERIFIED`.

## Canonical client/runtime architecture

### Current game-runtime path

**Unity is the canonical runtime for new 3D gameplay/rendering work.**

Known live runtime systems/evidence include Unity-side architecture such as:
- `AfareetBootstrap`;
- `CairoTrackBuilder`;
- `CairoCareerTrackBuilder`;
- `CairoAuthoredStreetKit`;
- authored Cairo route/road/curb runtime;
- Garage session runtime;
- Android JSONL diagnostics using `AFAREET_` markers.

Legacy Flutter/Flame implementation may remain in the repository as historical prototype / launcher / compatibility code, but no new core vehicle, camera, world, traffic, rendering or VFX work should be added there unless a task explicitly targets that legacy surface.

### AI-first production architecture

The owner is not expected to manually build production content in Unity or Blender.

Preferred pipeline:

`Codex → C#/Unity Editor Automation + Blender Python/CLI + Procedural Generation → Unity Runtime → Automated Validation → Android Build → Device Evidence`

Manual editor steps are fallback/debugging tools, not the normal production contract.

### Backend architecture

Backend remains:

`Game Client → HTTPS API → Laravel → MySQL`

Laravel owns auth/authorization/validation/business rules/economy/persistence/audit/rate limiting. No game client may connect directly to MySQL or contain database credentials.

Substantial backend expansion remains gated behind the playable 3D runtime slice.

## Highest priority — P1 3D Presentation / Playable Vertical Slice Gate

This is the controlling gate. Broad backend/store/online expansion must not outrun it.

### Required runtime/content
- One original hero vehicle, drivable.
- One Cairo-inspired authored/procedural vertical slice.
- Start/finish/checkpoints/reset.
- Arcade steering/braking/traction/drift.
- Spirit/Nitro loop.
- At least one rival; ambient/parked traffic for scale.
- Functional mobile HUD and touch controls.
- Android build on a real device.

### Required perceptual 3D presentation
- Perspective third-person chase camera with damping and speed FOV.
- Vehicle grounded by wheel/suspension/body response + contact shadow.
- Road, curb and sidewalk have visible height/material separation.
- Foreground + midground + background depth in normal driving views.
- Dense Cairo modular environment; no nearby empty voids.
- Traffic/parked vehicles establish scale.
- Bright/medium/dark lighting hierarchy.
- Road decals/variation prevent flat-strip appearance.
- Restrained post-processing.
- Representative raw screenshot without HUD already looks like a modern 3D mobile racer.

Detailed gates and workstream boundaries are in [`AI_AUTONOMOUS_3D_EXECUTION_PLAN.md`](AI_AUTONOMOUS_3D_EXECUTION_PLAN.md).

## P1 execution order

1. **G0 — Live state reconciliation**: canonical Unity head, active workers, stale documentation.
2. **G1 — Camera + vehicle grounding**.
3. **G2 — Dense Cairo street slice**: road depth, modular city, lighting, parked/traffic scale cues.
4. **G3 — Premium moving frame**: materials, VFX, atmosphere, HUD without masking scene quality.
5. **G4 — Integrated playable event loop**.
6. **G5 — Android performance/release evidence**.

Do not skip an open P0 gate to build lower-priority content.

## Phases

### P0 — Governance / automation / team system
- Repository constitution (`AGENTS.md`).
- Architecture/tasks/assets/art direction docs.
- Module-lock and branch discipline.
- Unity validation/build automation.
- Blender/procedural asset automation path.
- CI / licensed runner path where available.
- Project Status Dashboard freshness enforcement.

### P1 — 3D Playable Vertical Slice — highest priority
- Hero vehicle.
- Cairo-inspired dense slice.
- Chase camera and strong perspective depth.
- Drift + Nitro Spirit.
- 1–3 rivals plus scale traffic/parked vehicles.
- Premium HUD/touch controls.
- Lighting/material/road-depth gate.
- Real-device Android evidence.

### P2 — Driving & Racing Core
- Vehicle configuration/tuning.
- Checkpoints/Laps/Positions.
- Camera states.
- Collision/Reset.
- Race lifecycle.

### P3 — Magic Gameplay & Power-ups
- Magic/Spirit meter.
- Nitro Spirit tiers.
- Initial power-ups.
- VFX/audio hooks.

### P4 — Offline AI & Career
- AI personalities.
- Career chapters.
- Time Trial.
- Elimination.
- Boss races.

### P5 — Garage & Customization
- 3D premium showroom.
- Car catalog.
- Paint/Wheels/Trails.
- Stats/Unlocks.
- Local persistence.

### P6 — Backend Foundation — Laravel + MySQL
- Laravel versioned REST API (`/api/v1` baseline).
- MySQL primary store.
- Auth/Profile.
- Inventory/Garage.
- Economy ledger/rewards.
- Remote Config/feature flags.
- Telemetry contracts.
- Rate limiting/audit logging.
- Separate local/staging/production secrets.
- No direct client-to-MySQL connectivity.

### P7 — Real-time Multiplayer
- Lobby/Matchmaking.
- Server state.
- Prediction/Reconciliation.
- Reconnect.
- Result validation.

### P8 — League, Seasons & Asphalt Pass
- Ranks.
- Leaderboards.
- Season reset.
- Asphalt Pass.
- Reward claims.

### P9 — Monetization
- Rewarded Ads.
- IAP.
- Store rules.
- Purchase restore.
- Fraud guards.

### P10 — Admin & LiveOps
- Player ops.
- Economy config.
- Track rotation.
- Events.
- Bans.
- Dashboards.

### P11 — Performance, QA & Device Matrix
- Frame-time/performance budgets.
- LOD/VFX tiers.
- Quality tiers Low/Medium/High.
- Crash/ANR diagnostics.
- Regression tests.
- Device tiers.

### P12 — Beta & Production Release
- Closed beta.
- Store assets only if/when owner chooses publication.
- Release signing.
- Rollout/monitoring/rollback if publication is later authorized.

## Target device baseline

Current known real target hardware:
- HONOR ALI-NX1;
- Android 15 / API 35;
- Adreno 710;
- ~11 GB RAM;
- 2652×1200.

Optimize for stable frame pacing; 60 FPS is the target where realistic, not an excuse for severe visual/runtime instability.

## Team coordination rules

- Every meaningful implementation has a Task ID.
- Branch: `feature/<TASK-ID>-short-name`, `fix/<TASK-ID>-short-name`, `infra/<TASK-ID>-short-name` or `docs/<TASK-ID>-short-name`.
- One owner per Task.
- Fetch LIVE state before work.
- Do not restart verified work.
- Module Lock when tasks touch shared bootstrap/interfaces/core files.
- One focused PR; do not combine unrelated Epics.
- Shared interface changes should be isolated before dependent parallel work.
- `VERIFIED` requires evidence and is not equivalent to compile success.
- Visual tasks are mandatory for P1, not optional polish.
- Any real project-state change updates `PROJECT_STATUS.md` in the same PR.

## Task states

`TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`

## Last verified APK released policy

- No debug APK promoted as verified release.
- Store only the latest genuinely verified APK.
- Metadata must include commit SHA, build date, device/API, smoke result and SHA-256.
- If no verified APK exists, do not create a fake placeholder.

## Source of truth

- `AGENTS.md` — mandatory worker constitution
- `docs/PROJECT_STATUS.md` — executive live state
- `docs/MASTER_DEVELOPMENT_PLAN.md` — master product/phase plan
- `docs/AI_AUTONOMOUS_3D_EXECUTION_PLAN.md` — Unity AI-first workstreams/gates
- `docs/ART_DIRECTION.md` — visual constitution
- `docs/TASK_REGISTER.md` — task index
- `docs/tasks/06-UNITY-AI-3D-CLOSURE.md` — current Unity 3D closure task matrix
- `docs/BACKEND_ARCHITECTURE.md` — backend/security boundary
- `docs/MISSED_ASSETS.md` — asset gap register
