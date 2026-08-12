# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 15:23 (Asia/Kuwait)  
**Overall status:** 🟡 **P1 PROTOTYPE FOUNDATION VERIFIED — AUDIO SOURCE REGISTERED — PLAYABLE RACE NOT YET COMPLETE**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة، Art Direction، Task Register وstatus guard موجودة؛ GOV reconciliation ما زال مطلوبًا |
| Flutter / Flame foundation | 🟢 Verified | PRO-001 → PRO-010 اجتازت CI: analyze + tests + Android scaffold + debug APK build |
| First preview APK | 🟡 CI active | GitHub Actions يبني Debug APK ويرفعه كـArtifact باسم `3fareet-first-preview-apk`; هذا Preview وليس Verified Release |
| Premium visual direction | 🔴 Not started | VIS tasks ما زالت `TODO`; الـHUD shell يثبت tokens أولية فقط ولا يغلق Visual Gate |
| P1 playable prototype | 🔴 Not playable | foundation وPrototype scene entry موجودان، لكن لا توجد سيارة/حلبة/قيادة فعلية بعد |
| Driving / Drift / Nitro | 🔴 Not started | VEH/DRF الأساسية ما زالت `TODO` |
| Race / Camera / AI | 🔴 Not started | RAC/CAM/AI الأساسية ما زالت `TODO` |
| Audio foundation | 🟡 In review | owner-provided 30.772 s Cairo-fantasy music source registered as `AUD-MUS-001`; runtime folder/naming/import rules established; binary import + loop/device validation still pending |
| Missing assets | 🟡 Open | Cairo fantasy race music source is now provided; P0 engine/drift/nitro audio remain missing |
| Android verified release APK | 🔴 None | لا يوجد Release APK موثّق داخل `Last verified APK released/` حتى الآن |
| Backend architecture | 🟢 Locked | Laravel API + MySQL; Flutter لا يتصل مباشرة بقاعدة البيانات |
| Backend implementation / Online / Seasons | ⚪ Deferred | التنفيذ الكبير مؤجل حتى نجاح P1 Playable Prototype Gate |

## Verified engineering batch — PRO-001 → PRO-010

**Owner:** Principal Mobile Game Architect  
**Status:** `VERIFIED`  
**Evidence document:** [`work/PRO-001-010.md`](work/PRO-001-010.md)  
**Verified head:** `a9bef308fc47d4dea51f81539749d77939669ab0`  
**CI run:** `Flutter Prototype CI #9` / run `31594645225`

تم التحقق من:
1. PRO-001 — Flutter project baseline قابل للـdependency resolution والتحليل والاختبار.
2. PRO-002 — Flame `1.38.0` + root `GameWidget`.
3. PRO-003 — `GameBootstrap` lifecycle وdependency boundaries.
4. PRO-004 — `PrototypeScene` mounted في Flame world.
5. PRO-005 — mobile-neutral `GameInputState` / snapshots.
6. PRO-006 — fixed-step simulation clock مع bounded catch-up وfloating-point boundary hardening.
7. PRO-007 — asset loader lifecycle/cache/disposal.
8. PRO-008 — typed JSON game config loader.
9. PRO-009 — FPS + frame-time runtime telemetry overlay.
10. PRO-010 — prototype HUD shell: position/time/spirit/speed بالهوية dark/cyan/gold الأولية.

### Verification evidence
GitHub Actions على الـverified head نجحت بالكامل في:
- dependency resolution
- `flutter analyze` — **0 issues**
- `flutter test` — **all tests passed**
- Android scaffold generation from pinned Flutter template
- `flutter build apk --debug` — **success**
- Project Status Freshness Guard — **success**

خلال التحقق كشف اختبار fixed-step boundary حالة حقيقية كان فيها floating-point subtraction قد يفقد simulation tick؛ تم تغيير scheduler ليحسب عدد الخطوات قبل التنفيذ مع tolerance صغير، ثم أعيد CI حتى أصبح Green بالكامل.

**مهم:** نجاح Debug APK هنا يثبت buildability للـfoundation فقط. لا يعني وجود `Verified Release APK` للمستخدم. APK الخاصة بإغلاق P1 يجب أن تكون Release build، تعمل على جهاز حقيقي، وتجتاز smoke test ثم توضع فقط في `Last verified APK released/` مع metadata وSHA-256.

## First downloadable APK path

Workflow `Flutter Prototype CI` الآن يحتوي خطوة `actions/upload-artifact@v4` بعد نجاح `flutter build apk --debug`.

- Artifact name: `3fareet-first-preview-apk`
- Source: `build/app/outputs/flutter-apk/app-debug.apk`
- Retention: 14 days
- Classification: **Developer Preview / Debug**
- Must NOT be copied to `Last verified APK released/`.

## Audio source registered — AUD-MUS-001

المالك قدم مصدر موسيقى بتاريخ 2026-08-12 وتم تحليله وتسجيله كمرشح رسمي لـCairo fantasy race music.

- Duration: `30.772 s`
- Source: stereo / `44.1 kHz` / ~`192 kbps`
- Estimated pulse: ~`120 BPM`
- Source SHA-256: `7e8a5119167f4e5333e6606bbefa1bfe55d735c231b2abc92698a1004b36be50`
- Reserved runtime path: `assets/audio/music/cairo_fantasy_race_theme_01.mp3`
- Metadata: [`../assets/audio/music/cairo_fantasy_race_theme_01.asset.json`](../assets/audio/music/cairo_fantasy_race_theme_01.asset.json)
- Pipeline: [`AUDIO_PIPELINE.md`](AUDIO_PIPELINE.md)
- Current state: **SOURCE PROVIDED / BINARY IMPORT + LOOP + DEVICE VALIDATION PENDING**

P0 audio still required for playable driving feel:
1. Prototype engine loop.
2. Tire skid/drift loop.
3. Nitro Spirit activation signature.

## Architecture decisions now locked

- Gameplay simulation الجديدة تعتمد fixed-step clock بدل ربط physics مباشرة بتذبذب frame delta.
- Input contract منفصل عن Flutter widgets ليخدم touch controls الآن ثم multiplayer client prediction/reconciliation لاحقًا بدون إعادة تصميم gameplay API.
- Bootstrap/config/assets لها lifecycle وحدود مستقلة لمنع coupling مبكر بين UI وgameplay/networking.
- Backend stack is locked by [`BACKEND_ARCHITECTURE.md`](BACKEND_ARCHITECTURE.md): **Laravel + MySQL**.
- Mandatory data path: `Flutter/Flame → HTTPS API → Laravel → MySQL`.
- ممنوع تضمين MySQL credentials داخل التطبيق أو أي direct database connection من Flutter.
- Backend/Online implementation لا يسبق إثبات single-player driving loop والـP1 visual/performance gate.

## Current phase

### 🟡 P0 — Foundation / Team Control
الـexecutable Flutter/Flame foundation الأولى أصبحت Verified. ما زال GOV task reconciliation وبعض platform/release foundation مطلوبًا قبل إعلان P0 بالكامل `VERIFIED`.

### 🔴 P1 — Playable Prototype Gate
**الحالة: NOT VERIFIED / NOT PLAYABLE YET**

الـGate المطلوب:
- سيارة واحدة قابلة للقيادة.
- حلبة مصرية Fantasy واحدة.
- لفة واحدة + checkpoints + finish.
- Drift + Magic Spirit Meter + Nitro Spirit.
- 1 AI على الأقل.
- Racing camera + Premium HUD.
- Cairo fantasy lighting/look-dev مطابق للـArt Direction.
- Android Release APK يعمل على جهاز حقيقي.
- آخر APK ناجح فقط يوضع في `Last verified APK released/` مع metadata وSHA-256.

## Highest priorities next

1. PRO-011 / PRO-012 — pause/resume + reset/restart lifecycle.
2. PRO-013 / PRO-014 — تثبيت Android debug/release build surface داخل المستودع.
3. VEH-001 → VEH-006 — VehicleDefinition + throttle/brake/steering/grip.
4. P0 audio generation/acquisition — engine + drift + Nitro Spirit signature.
5. VIS-001 → VIS-006 بالتوازي حتى لا يتحول الـPrototype إلى شكل تقني مؤقت.
6. بعدها DRF/RAC/CAM للاقتراب من أول playable race.
7. BCK-001 architecture decision already VERIFIED; remaining Laravel/MySQL implementation stays deferred behind P1.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B02 | 🔴 High | لا توجد سيارة أو حلبة قابلة للعب بعد | التالي Gameplay/Vehicle foundation |
| STS-B03 | 🔴 High | VIS tasks ما زالت TODO | بدء VIS بالتوازي مع driving prototype |
| STS-B04 | 🔴 High | لا يوجد Verified Release APK | لا يغلق P1 قبل release + real-device smoke test |
| STS-B05 | 🟡 Medium | بعض GOV tasks TODO رغم وجود تنفيذ فعلي جزئي | Team Lead يعمل reconciliation مع Evidence |
| STS-B06 | 🟡 Medium | Android platform scaffold يولد حاليًا من Flutter pinned template في CI | PRO-013/014 تملكان تثبيت Android build surface النهائي |
| STS-B07 | 🟡 Medium | P0 engine/drift/nitro audio missing; provided music source is not yet binary-imported/loop-validated | Produce/import P0 SFX and complete AUD-MUS-001 runtime validation |

## Last verified APK

**Status:** 🔴 **NO VERIFIED RELEASE APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

لا يوضع أي APK هنا قبل تسجيل Version, Commit SHA, Build date, Device/API, Tester, smoke result وSHA-256.

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
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Audio Pipeline](AUDIO_PIPELINE.md)
- [Full Task Register](TASK_REGISTER.md)
- [Premium Visual Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
