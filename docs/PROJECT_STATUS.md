# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 14:48 (Asia/Kuwait)  
**Overall status:** 🟡 **P1 PROTOTYPE FOUNDATION IN REVIEW — NOT YET PLAYABLE**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة، Art Direction، Task Register وstatus guard موجودة؛ GOV reconciliation ما زال مطلوبًا |
| Flutter / Flame foundation | 🟠 In review | PRO-001 → PRO-010 تم تنفيذها على فرع `agent/pro-001-010-prototype-foundation` وتنتظر CI evidence |
| Premium visual direction | 🔴 Not started | VIS tasks ما زالت `TODO`; الـHUD shell فقط يثبت visual tokens الأولية ولا يغلق Visual Gate |
| P1 playable prototype | 🔴 Not playable | يوجد game foundation وPrototype scene entry، لكن لا توجد سيارة/حلبة/قيادة فعلية بعد |
| Driving / Drift / Nitro | 🔴 Not started | VEH/DRF الأساسية ما زالت `TODO` |
| Race / Camera / AI | 🔴 Not started | RAC/CAM/AI الأساسية ما زالت `TODO` |
| Missing assets | 🟡 Open | سجل `MISSED_ASSETS.md` ما زال مفتوحًا ويجب تحديثه مع كل Asset مؤثر |
| Android verified APK | 🔴 None | لا يوجد Release APK موثّق داخل `Last verified APK released/` حتى الآن |
| Backend / Online / Seasons | ⚪ Deferred | مؤجلة حتى نجاح P1 Playable Prototype Gate |

## Current engineering batch — PRO-001 → PRO-010

**Owner:** Principal Mobile Game Architect  
**Status:** `IN REVIEW` pending automated CI  
**Evidence document:** [`work/PRO-001-010.md`](work/PRO-001-010.md)

تم تنفيذ:
1. PRO-001 — Flutter project baseline قابل للـdependency resolution والتحليل والاختبار.
2. PRO-002 — Flame `1.38.0` + root `GameWidget`.
3. PRO-003 — `GameBootstrap` lifecycle وdependency boundaries.
4. PRO-004 — `PrototypeScene` mounted في Flame world.
5. PRO-005 — mobile-neutral `GameInputState` / snapshots.
6. PRO-006 — fixed-step simulation clock policy مع bounded catch-up.
7. PRO-007 — asset loader lifecycle/cache/disposal.
8. PRO-008 — typed JSON game config loader.
9. PRO-009 — FPS + frame-time runtime telemetry overlay.
10. PRO-010 — prototype HUD shell: position/time/spirit/speed بالهوية dark/cyan/gold الأولية.

### Validation gate لهذه الحزمة
GitHub Actions workflow `Flutter Prototype CI` يجب أن ينجح في:
- `dart format --set-exit-if-changed`
- `flutter analyze`
- `flutter test`
- توليد Android scaffold من Flutter 3.44.0 pinned template
- `flutter build apk --debug`

**مهم:** نجاح Debug APK هنا لا يعني وجود `Verified APK` للمستخدم. الـVerified APK المطلوب في P1 يجب أن يكون Release build ويجتاز device smoke test ثم يوضع فقط في `Last verified APK released/`.

## Architecture decisions now locked

- Gameplay simulation الجديدة يجب أن تعتمد fixed-step clock بدل ربط physics مباشرة بتذبذب frame delta.
- Input contract منفصل عن Flutter widgets ليخدم touch controls الآن، ثم multiplayer client prediction/reconciliation لاحقًا بدون إعادة تصميم gameplay API.
- Bootstrap/config/assets لها lifecycle وحدود مستقلة لمنع coupling مبكر بين UI وgameplay/networking.
- Backend/Online لا يسبق إثبات single-player driving loop والـP1 visual/performance gate.

## Current phase

### 🟡 P0 — Foundation / Team Control
جزء التنفيذ البرمجي بدأ فعليًا. ما زال GOV task reconciliation مطلوبًا قبل إعلان P0 VERIFIED بالكامل.

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

1. إغلاق CI للحزمة PRO-001 → PRO-010 ونقلها فقط بعد Evidence إلى VERIFIED.
2. PRO-011 / PRO-012 — pause/resume + reset/restart lifecycle.
3. PRO-013 — Android debug build يصبح repository-owned بدل bootstrap مؤقت.
4. VEH-001 → VEH-006 — VehicleDefinition + throttle/brake/steering/grip.
5. VIS-001 → VIS-006 بالتوازي حتى لا يتحول الـPrototype إلى شكل تقني مؤقت.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B01 | 🟠 Medium | PRO-001 → PRO-010 تنتظر CI build evidence | لا تتحول VERIFIED قبل نجاح workflow بالكامل |
| STS-B02 | 🔴 High | لا توجد سيارة أو حلبة قابلة للعب بعد | التالي Gameplay/Vehicle foundation |
| STS-B03 | 🔴 High | VIS tasks ما زالت TODO | بدء VIS بالتوازي مع driving prototype |
| STS-B04 | 🔴 High | لا يوجد Verified Release APK | لا يغلق P1 قبل release + real-device smoke test |
| STS-B05 | 🟡 Medium | بعض GOV tasks TODO رغم وجود تنفيذ فعلي جزئي | Team Lead يعمل reconciliation مع Evidence |
| STS-B06 | 🟡 Medium | Android platform scaffold يولد حاليًا من Flutter pinned template في CI | PRO-013/014 تملكان تثبيت Android build surface النهائي |

## Last verified APK

**Status:** 🔴 **NO VERIFIED APK YET**  
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
- [Full Task Register](TASK_REGISTER.md)
- [Premium Visual Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
