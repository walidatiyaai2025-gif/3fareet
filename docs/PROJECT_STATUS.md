# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 15:55 (Asia/Kuwait)  
**Overall status:** 🟡 **GAMEPLAY CORE VERIFIED — FIRST APK RAP×SHAABI AUDIO INTEGRATING — P1 STILL OPEN**

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
| First APK music | 🟡 Integrating | `AST-061` Rap/Trap × Egyptian Shaabi/Mahraganat preview is bundled and starts from game load; CI + device listening still required |
| Android Debug APK | 🟢 Previous CI verified | Debug APK build succeeded in GAMEPLAY-050 verification run; audio-enabled candidate requires new CI |
| Android Release Skeleton | 🟢 Previous CI verified | Release skeleton succeeded in GAMEPLAY-050; audio-enabled candidate requires new CI |
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
Run `31596838749` completed Green and proved dependency resolution, `flutter analyze`, complete `flutter test`, Android scaffold generation, Debug APK, Release Skeleton APK, artifact upload and Project Status Freshness Guard.

## AST-061 — First APK Rap × Shaabi music

The owner-provided source has been registered and transformed into the prototype music direction:

**Rap / Trap × Egyptian Shaabi / Mahraganat**
- rap/trap kick, snare and hats;
- controlled 808 low end;
- darbuka/shaabi percussion;
- light original oriental lead;
- no named-artist imitation and no third-party copyrighted vocals/lyrics added.

### Source evidence
- Duration: `30.772 s`
- Stereo / `44.1 kHz` / ~`192 kbps`
- Estimated tempo: ~`120 BPM`
- Source SHA-256: `7e8a5119167f4e5333e6606bbefa1bfe55d735c231b2abc92698a1004b36be50`

### First APK runtime implementation
- Asset lock: `AST-061` / `INTEGRATING` in [`MISSED_ASSETS.md`](MISSED_ASSETS.md)
- Embedded preview: `assets/audio/embedded/cairo_rap_shaabi_loop_4s.b64`
- Metadata: `assets/audio/music/cairo_fantasy_race_theme_01.asset.json`
- Controller: `lib/game/audio/prototype_music_controller.dart`
- Runtime: Base64 → MP3 bytes in memory
- Playback: `flame_audio` / `AudioPlayer`
- Loop mode enabled; prototype BGM volume `0.52`
- Starts during `AfareetGame.onLoad()` after race-session initialization
- Audio errors are non-fatal and cannot block game boot
- Full production master replaces the short preview after real-device mix validation
- Pipeline: [`AUDIO_PIPELINE.md`](AUDIO_PIPELINE.md)

### Remaining P0 audio
1. Prototype engine idle/low/high loop.
2. Tire skid/drift intensity loop.
3. Nitro Spirit activation signature.

## Architecture now locked

- Gameplay simulation consumes fixed-step time, not variable frame delta.
- Input remains UI-neutral so multiplayer client prediction/reconciliation can reuse the same command contract.
- Vehicle physics, Spirit/Nitro and race rules do not depend on Flutter widgets.
- Runtime touch UI is an adapter over the same gameplay input contract.
- Race/checkpoint rules are deterministic and tested independently of rendering.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct client database access is prohibited.

## APK classification

The CI pipeline produces both Debug and Release Skeleton APK artifacts. The audio-enabled candidate must pass CI again. These remain **developer/build evidence**, not the final verified APK.

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

1. CI-build and listen-test the audio-enabled APK candidate.
2. CAM-001 → CAM-005 — follow/look-ahead/damping/drift/nitro camera.
3. AI-001 → AI-006 — racing line/path/throttle/steering/braking/drift zones.
4. VIS-001 → VIS-006 — color/lighting/material/road/landmark silhouette implementation.
5. P0 Engine + Drift + Nitro audio generation/integration.
6. VEH-017 + RAC-017 — device feel and integrated race verification.
7. First real-device verified Release APK.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B03 | 🔴 High | Premium VIS tasks remain TODO | Start VIS in parallel with Camera/AI |
| STS-B08 | 🔴 High | Camera and AI are missing | Next implementation batch targets CAM + AI |
| STS-B04 | 🔴 High | No real-device Verified Release APK | P1 cannot close from CI artifacts alone |
| STS-B09 | 🟡 Medium | AST-061 first APK music needs CI + Android listening validation | Build candidate, smoke audio, then promote asset state |
| STS-B10 | 🟡 Medium | Engine/drift/nitro gameplay SFX still missing | Generate/acquire P0 SFX |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Audio Pipeline](AUDIO_PIPELINE.md)
- [Full Task Register](TASK_REGISTER.md)
- [Premium Visual Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [GAMEPLAY-050 Evidence](work/GAMEPLAY-050.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
