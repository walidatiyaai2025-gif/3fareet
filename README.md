# 3Fareet — عفاريت الأسفلت

المشروع الرسمي للعبة **3Fareet**.

> **3D development track:** The playable production client lives in
> [`unity_game/`](unity_game/). The Flutter/Flame implementation remains as the
> legacy mechanics and UI prototype while production moves to mobile-first 3D.

Build names are engine-specific: Unity outputs use `afareet-unity3d-*` and the
Flutter prototype uses `afareet-flutter-*`.

## 🚦 حالة المشروع الآن

**افتح أولًا:** [Project Status Dashboard — الوضع الحالي للمشروع](docs/PROJECT_STATUS.md)

هذه هي الصفحة التنفيذية الرسمية التي تعرض آخر وضع للمشروع، المرحلة الحالية، الجاري والمتعطل، الأولويات، الـMissing Assets، وحالة آخر Verified APK. أي PR يغيّر وضع المشروع يجب أن يحدّثها في نفس الـPR.

## مراجع الفريق الرسمية

- [New Contributor Onboarding](docs/ONBOARDING.md) — **ابدأ من هنا إذا انضممت للفريق**
- [Contributing & PR Rules](CONTRIBUTING.md)
- [Team Workflow & Definition of Done](docs/TEAM_WORKFLOW.md)
- [Module Ownership & Active Locks](docs/MODULE_OWNERSHIP.md)
- [Active Unity 3D Tasks](docs/tasks/06-UNITY-3D-MIGRATION.md)
- [Project Status Dashboard](docs/PROJECT_STATUS.md) — **ابدأ من هنا لمعرفة الحالة الحالية**
- [Master Development Plan](docs/MASTER_DEVELOPMENT_PLAN.md)
- [Premium Visual Direction](docs/ART_DIRECTION.md)
- [Full Task Register](docs/TASK_REGISTER.md)
- [Missed Assets](docs/MISSED_ASSETS.md)
- [Last verified APK released](Last%20verified%20APK%20released/README.md)

## الاسم والهوية

- Display name: **3Fareet**.
- Internal Flutter package/project name remains `afareet_asphalt` لتجنب كسر imports/package IDs أثناء التطوير.
- Unity Android package remains `com.fiftysolutions.afareetunity3d` ضمن مسار الإنتاج ثلاثي الأبعاد.
- Flutter Android bootstrap يضبط `android:label="3Fareet"`.
- Branded splash key art: `assets/branding/3fareet_splash.jpg`.

## أهم قاعدة الآن

**P1 Playable Prototype هي أعلى أولوية.** لا يتم استنزاف المشروع في Backend/Store/Online قبل وجود سباق قابل للعب على Android واجتياز التحقق.

## Visual rule

الشكل المطلوب هو **Premium Neon Egyptian Fantasy Racing**. الصور المرجعية التي قدمها مالك المشروع أصبحت Visual Constitution؛ لا تعتبر P1 `VERIFIED` إذا كان الشكل بعيدًا عنها حتى لو الكود والأداء ناجحين.

## Team rule

لا يبدأ أي مطور مهمة بدون Task ID وOwner واضح. أي تعديل على الخطة أو الاتجاه البصري يتم أولًا في ملفات `docs/` لكي يراه الفريق كله. وأي تغيير فعلي في حالة المشروع يجب أن ينعكس في `docs/PROJECT_STATUS.md` داخل نفس PR.

## Backend boundary

`Flutter / Unity → HTTPS API → Laravel → MySQL`

ممنوع ربط أي عميل لعبة مباشرة بـMySQL أو تضمين database credentials داخل التطبيق.
