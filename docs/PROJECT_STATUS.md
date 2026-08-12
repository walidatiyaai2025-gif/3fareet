# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-12 14:38 (Asia/Kuwait)  
**Overall status:** 🟡 **FOUNDATION / P1 PROTOTYPE NOT YET PLAYABLE**

> هذه الصفحة هي أول صفحة يراجعها مالك المشروع وTeam Lead لمعرفة الحالة الحالية. لا يجوز دمج PR يغيّر حالة Task أو Phase أو Blocker أو Asset أو Build/Release بدون تحديث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Repository / governance | 🟡 In setup | الخطة والـArt Direction وسجل المهام موجودة، لكن سجل حالات بعض مهام التأسيس يحتاج reconciliation مع الواقع |
| Premium visual direction | 🔴 Not started | مهام VIS ما زالت `TODO`؛ الهوية موثقة لكن لم تدخل Prototype مرئي بعد |
| P1 playable prototype | 🔴 Not started | لا يوجد Flutter/Flame playable build في `main` حتى الآن |
| Driving / Drift / Nitro | 🔴 Not started | مهام VEH/DRF الأساسية ما زالت `TODO` |
| Race / Camera / AI | 🔴 Not started | مهام RAC/CAM/AI الأساسية ما زالت `TODO` |
| Missing assets | 🟡 Open | يوجد سجل `MISSED_ASSETS.md` ويجب تحديثه باستمرار أثناء إنشاء/استلام الأصول |
| Android verified APK | 🔴 None | لا يوجد APK موثّق داخل `Last verified APK released/` حتى الآن |
| Backend / Online / Seasons | ⚪ Deferred | مخطط لها، لكنها ليست أولوية قبل نجاح P1 Prototype Gate |

## Current phase

### 🟡 P0 — Foundation / Team Control
الوثائق الأساسية موجودة في `docs/`، لكن قبل اعتبار P0 مكتملة بالكامل يجب أن يعكس `TASK_REGISTER` الحقيقة الفعلية لحالات GOV tasks، وأن يبدأ مشروع Flutter/Flame القابل للبناء.

### 🔴 P1 — Playable Prototype Gate
**الحالة: NOT VERIFIED / NOT PLAYABLE YET**

الـGate المطلوب قبل الانتقال للتوسع:
- سيارة واحدة قابلة للقيادة.
- حلبة مصرية Fantasy واحدة.
- لفة واحدة + checkpoints + finish.
- Drift + Magic Spirit Meter + Nitro Spirit.
- 1 AI على الأقل.
- Racing camera + Premium HUD.
- Cairo fantasy lighting/look-dev مطابق للـArt Direction.
- Android Release APK يعمل على جهاز حقيقي.
- آخر APK ناجح فقط يوضع في `Last verified APK released/` مع metadata وSHA-256.

## Highest priorities now

1. **Prototype first:** إنشاء Flutter + Flame project قابل للبناء والتشغيل.
2. **Visual from day one:** تنفيذ VIS بالتوازي مع أول Prototype؛ ممنوع تأجيل الشكل للآخر.
3. **First drivable loop:** Steering → braking → traction → drift → spirit → nitro.
4. **First Egyptian fantasy track:** Track واحد يكفي للـGate، لكن يجب أن يظهر الهوية البصرية فعليًا.
5. **Verified APK:** أول milestone مرئي لمالك المشروع هو APK Release قابل للتجربة على جهاز حقيقي.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Action |
|---|---|---|---|
| STS-B01 | 🔴 High | لا يوجد playable application/code في root حاليًا | بدء PRO-001 ثم PRO-002/003/004 |
| STS-B02 | 🔴 High | كل VIS tasks الظاهرة ما زالت TODO | بدء VIS-001..VIS-006 بالتوازي مع الـPrototype |
| STS-B03 | 🔴 High | لا يوجد Verified APK | لا يغلق P1 قبل Release device verification |
| STS-B04 | 🟡 Medium | بعض GOV tasks ما زالت TODO رغم وجود وثائق تنفذ جزءًا منها | Team Lead يعمل status reconciliation بدون ادعاء Verification بلا Evidence |
| STS-B05 | 🟡 Medium | Missing assets قد تمنع الوصول للشكل الفخم | تحديث `MISSED_ASSETS.md` فور اكتشاف/إنشاء/استلام أي Asset |

## Last verified APK

**Status:** 🔴 **NO VERIFIED APK YET**  
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

عند توفر أول APK موثّق يجب تسجيل:
- Version / filename
- Commit SHA
- Build date/time
- Device + Android API
- Tester
- Smoke-test result
- SHA-256

## Team workload status

المصدر التفصيلي للمهام هو [`TASK_REGISTER.md`](TASK_REGISTER.md). لا تعتمد هذه الصفحة كبديل عن تفاصيل كل Task؛ هي Executive Dashboard فقط.

**State vocabulary:** `TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`

### منع تداخل الفريق
- Owner واحد لكل Task.
- لا يبدأ العمل قبل تحديث Owner + Status في ملف task المختص.
- Module Lock إلزامي عند لمس نفس الملفات أو interface مشتركة.
- كل PR يذكر Task ID.
- عند تغير الحالة، يتم تحديث ملف المهمة **و** هذه الصفحة في نفس PR.

## Status update contract — إلزامي

يجب تحديث `docs/PROJECT_STATUS.md` في **نفس PR** عند حدوث أي مما يلي:
- Task تنتقل بين TODO/READY/IN PROGRESS/BLOCKED/IN REVIEW/DONE/VERIFIED.
- بدء أو إغلاق Phase/Milestone.
- ظهور أو إزالة Blocker/Risk مهم.
- إنشاء أو استلام أو فقد Asset مؤثر على الـPrototype.
- تغيير Scope أو Architecture أو Priority.
- نجاح/فشل Build مهم يؤثر على قابلية التجربة.
- إصدار APK جديد أو تغيير محتوى `Last verified APK released/`.

### Definition of "Up to date"
تعتبر الصفحة Up to date فقط إذا:
1. `Last updated` يعكس آخر تغيير حقيقي في حالة المشروع.
2. Executive snapshot لا يتعارض مع Task Register.
3. لا يوجد APK موصوف كـVerified بدون Evidence.
4. Blockers الحالية ظاهرة بوضوح.
5. Next priorities تعكس أقرب أعمال قابلة للتنفيذ.

## Source of truth links

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Full Task Register](TASK_REGISTER.md)
- [Premium Visual Direction](ART_DIRECTION.md)
- [Missed Assets](MISSED_ASSETS.md)
- [Last verified APK released](../Last%20verified%20APK%20released/)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** لا يوجد "هحدثها لاحقًا"؛ تحديث الحالة جزء من Definition of Done للـPR نفسه.
