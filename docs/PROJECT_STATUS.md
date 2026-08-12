# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 15:34 (Asia/Kuwait)  
**Overall status:** 🟡 **P1 FOUNDATION VERIFIED — FIRST APK AUDIO INTEGRATED — PLAYABLE RACE NOT YET COMPLETE**

> أي PR يغيّر Task/Phase/Blocker/Asset/Build/Release يجب أن يحدّث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة، Art Direction، Task Register وstatus guard موجودة |
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-010 اجتازت analyze + tests + Android debug build |
| First preview APK | 🟡 Building | GitHub Actions يبني Debug APK ويرفع `3fareet-first-preview-apk` |
| First APK music | 🟡 In validation | owner audio integrated as an embedded Rap × Egyptian Shaabi preview loop; runtime playback starts from `AfareetGame.onLoad()` |
| Premium visual direction | 🔴 Not started | VIS tasks ما زالت TODO |
| P1 playable prototype | 🔴 Not playable | لا توجد سيارة/حلبة/قيادة فعلية بعد |
| Driving / Drift / Nitro | 🔴 Not started | VEH/DRF الأساسية ما زالت TODO |
| Race / Camera / AI | 🔴 Not started | RAC/CAM/AI الأساسية ما زالت TODO |
| Missing P0 SFX | 🔴 Open | Engine loop + tire skid/drift + Nitro Spirit activation |
| Android verified release APK | 🔴 None | لا يوجد Release APK موثق/مختبر على جهاز حقيقي بعد |
| Backend architecture | 🟢 Locked | `Flutter/Flame → HTTPS API → Laravel → MySQL`; ممنوع direct MySQL from Flutter |
| Backend implementation | ⚪ Deferred | التنفيذ الكبير مؤجل خلف P1 Playable Gate |

## Verified engineering batch — PRO-001 → PRO-010

**Status:** `VERIFIED`  
**Evidence:** [`work/PRO-001-010.md`](work/PRO-001-010.md)  

تم التحقق من Flutter/Flame baseline، bootstrap، prototype scene، input contract، fixed-step simulation، assets/config lifecycle، telemetry overlay، prototype HUD، unit tests وAndroid debug build.

## First downloadable APK pipeline

Workflow `Flutter Prototype CI`:
- runs format/analyze/tests
- generates Android scaffold from pinned Flutter template
- builds `flutter build apk --debug`
- uploads artifact `3fareet-first-preview-apk`
- classification: **Developer Preview / Debug**
- لا يوضع داخل `Last verified APK released/` لأنه ليس Release + real-device verified.

## AUD-MUS-001 — First APK music

المالك قدم المصدر الصوتي بتاريخ 2026-08-12 وتم دمجه في أول APK كتجربة تشغيل فعلية.

### Source
- Duration: `30.772 s`
- Stereo / `44.1 kHz` / ~`192 kbps`
- Estimated pulse: ~`120 BPM`
- SHA-256: `7e8a5119167f4e5333e6606bbefa1bfe55d735c231b2abc92698a1004b36be50`

### Musical direction locked for prototype
**Rap / Trap × Egyptian Shaabi / Mahraganat**:
- rap/trap kick + snare + hats
- 808 low end
- darbuka/shaabi percussion
- light oriental lead
- original supplied audio remains the source bed/reference
- no imitation of a named artist and no copyrighted vocal/lyric insertion

### APK implementation
- Embedded preview asset: `assets/audio/embedded/cairo_rap_shaabi_loop_4s.b64`
- Runtime decoding: Base64 → MP3 bytes in memory
- Playback: `flame_audio` / `AudioPlayer`
- Loop: `ReleaseMode.loop`
- Volume: `0.52`
- Controller: `lib/game/audio/prototype_music_controller.dart`
- Starts from: `AfareetGame.onLoad()`
- Audio failure is non-fatal and must not block game boot.
- Metadata: [`../assets/audio/music/cairo_fantasy_race_theme_01.asset.json`](../assets/audio/music/cairo_fantasy_race_theme_01.asset.json)
- Pipeline rules: [`AUDIO_PIPELINE.md`](AUDIO_PIPELINE.md)

**Current validation:** code + bundle integrated; Android CI and real-device listening test still required. The short embedded preview loop will be replaced by the full mastered track before production.

## P0 audio still required

1. Prototype engine idle/low/high loop.
2. Tire skid/drift intensity loop.
3. Nitro Spirit activation signature.
4. Then crash impacts, countdown, finish sting and UI click/confirm/back.

## Architecture decisions locked

- Fixed-step simulation for gameplay.
- Input contract separated from Flutter widgets for later prediction/reconciliation.
- Bootstrap/config/assets isolated from gameplay/networking.
- Backend: [`BACKEND_ARCHITECTURE.md`](BACKEND_ARCHITECTURE.md) — Laravel + MySQL.
- Mandatory path: `Flutter/Flame → HTTPS API → Laravel → MySQL`.
- No MySQL credentials or direct DB connection in the APK.

## Current phase

### 🟡 P0 — Foundation / Team Control
Executable Flutter/Flame foundation is Verified; platform/release/audio foundation continues.

### 🔴 P1 — Playable Prototype Gate
Still **NOT VERIFIED / NOT PLAYABLE** until all of the following exist:
- drivable car
- Cairo fantasy track
- lap/checkpoints/finish
- drift + Spirit Meter + Nitro Spirit
- at least 1 AI
- racing camera + premium HUD
- visual identity matching Art Direction
- Android Release APK tested on a real device

## Highest priorities next

1. Finish CI validation for the audio-enabled first APK.
2. PRO-011/012 — pause/resume + reset/restart lifecycle.
3. PRO-013/014 — stable Android debug/release build surface.
4. VEH-001 → VEH-006 — vehicle/throttle/brake/steering/grip.
5. Generate/acquire P0 Engine + Drift + Nitro audio.
6. VIS-001 → VIS-006 in parallel.
7. Then DRF/RAC/CAM toward first playable race.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B02 | 🔴 High | لا توجد سيارة أو حلبة قابلة للعب | Vehicle/gameplay next |
| STS-B03 | 🔴 High | VIS tasks TODO | Start VIS in parallel |
| STS-B04 | 🔴 High | No Verified Release APK | Release + real-device smoke required |
| STS-B06 | 🟡 Medium | Android platform scaffold still generated in CI | PRO-013/014 |
| STS-B07 | 🟡 Medium | Engine/drift/nitro SFX missing | Produce/import P0 SFX |
| STS-B08 | 🟡 Medium | Rap×Shaabi embedded loop needs Android listening validation/full master later | CI + device test + master replacement |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

لا يوضع APK هنا قبل Version + Commit SHA + Build date + Device/API + Tester + smoke result + SHA-256.

## Source of truth

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Audio Pipeline](AUDIO_PIPELINE.md)
- [Task Register](TASK_REGISTER.md)
- [Art Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** status update is part of Definition of Done.
