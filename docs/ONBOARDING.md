# Onboarding — أول يوم في فريق عفاريت الأسفلت

**الهدف:** أي مطور أو فنان جديد يفتح المستودع ويعرف أين يعمل، ماذا يشغل، ومن يراجع عمله خلال أقل من ساعة.

## 1. افهم القرار الحالي

- **المنتج النهائي:** Unity 6، لعبة 3D للموبايل.
- **Unity version:** `6000.5.8f1` كما هو مثبت في `unity_game/ProjectSettings/ProjectVersion.txt`.
- **Flutter/Flame:** Prototype Legacy مثبت ومرجع للميكانيك والاختبارات؛ ليس مسار إنتاج اللعبة النهائي.
- **Backend لاحقًا:** Laravel + MySQL خلف HTTPS API. لا يتصل أي Client بقاعدة البيانات مباشرة.
- **الأولوية الحالية:** Unity P1 Vertical Slice ثم Android Verified APK.

## 2. اقرأ بهذا الترتيب

1. [`PROJECT_STATUS.md`](PROJECT_STATUS.md) — الواقع الحالي والـBlockers.
2. [`MASTER_DEVELOPMENT_PLAN.md`](MASTER_DEVELOPMENT_PLAN.md) — المراحل والـGates.
3. [`tasks/06-UNITY-3D-MIGRATION.md`](tasks/06-UNITY-3D-MIGRATION.md) — المهام النشطة القابلة للتوزيع.
4. [`MODULE_OWNERSHIP.md`](MODULE_OWNERSHIP.md) — من يملك أي ملفات.
5. [`TEAM_WORKFLOW.md`](TEAM_WORKFLOW.md) — Branch/PR/Review/DoD.
6. [`ART_DIRECTION.md`](ART_DIRECTION.md) — الدستور البصري.

## 3. تجهيز Unity

ثبت Unity `6000.5.8f1` مع:

- Android Build Support.
- Android SDK & NDK Tools.
- OpenJDK.
- Windows Build Support للاختبار المحلي.

ثم أضف مجلد `unity_game/` إلى Unity Hub. افتح `Assets/Scenes/Prototype.unity` واضغط Play.

### التحكم المحلي

- `W/S` أو الأسهم: تسارع/فرامل.
- `A/D`: توجيه.
- `Space`: Drift.
- `Left Shift`: Nitro.
- `R`: Reset.

### Build محلي

من Unity Batch Mode أو Editor:

- `Afareet.Editor.AfareetBuild.BuildWindows`
- `Afareet.Editor.AfareetBuild.BuildAndroid`

النواتج لها أسماء منفصلة: `afareet-unity3d-*`. لا ترفع `Builds/` إلى Git.

## 4. تجهيز Flutter Legacy عند الحاجة فقط

من جذر المستودع:

```powershell
flutter pub get
flutter analyze
flutter test
.\tool\bootstrap_android.ps1
.\tool\build_debug.ps1
```

الناتج: `afareet-flutter-debug.apk`. لا تعدّل Flutter Feature دون Task `FLT-*`.

## 5. استلام أول مهمة

لا تختَر Task `TODO` وتبدأ وحدك. Team Lead يحولها إلى `READY` بعد التأكد من المتطلبات، ثم:

1. يكتب اسمك في Owner.
2. تسجل المهمة في Active Work Board داخل سجل Unity.
3. تسجل الملفات المحجوزة في Module Locks.
4. تنشئ Branch يحمل Task ID.
5. تضع PR Draft مبكرًا إذا ستغيّر Interface مشتركة.

## 6. قبل طلب المراجعة

- المشروع يفتح ويعمل دون Console Errors.
- الاختبارات المناسبة ناجحة.
- لا توجد ملفات مولدة أو أسرار.
- المهمة تحولت إلى `IN REVIEW`.
- PR يحتوي Evidence: فيديو/صور/Build log/Test output حسب المهمة.
- `PROJECT_STATUS.md` محدث إذا تغير Milestone أو Blocker أو Build.

## 7. أين تطلب القرار؟

- Scope/Priority: Product Owner / Team Lead.
- Architecture/Shared interfaces: Unity Tech Lead.
- Driving/AI/Race rules: Gameplay Lead.
- Visual approval: Art Director / Owner.
- Performance/Build/Device: QA & Release Lead.
- Backend contracts: Backend Lead.

لا تحسم قرارًا عابرًا للموديولات داخل كودك؛ افتح Task/ADR أولًا.
