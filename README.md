# 3Fareet — عفاريت الأسفلت

المشروع الرسمي للعبة **3Fareet**.

## مراجع الفريق
- [Project Status](docs/PROJECT_STATUS.md)
- [Master Development Plan](docs/MASTER_DEVELOPMENT_PLAN.md)
- [Full Task Register](docs/TASK_REGISTER.md)
- [Premium Visual Direction](docs/ART_DIRECTION.md)
- [Missed Assets](docs/MISSED_ASSETS.md)
- [Last verified APK released](Last%20verified%20APK%20released/README.md)

## الاسم والهوية
- Display name: **3Fareet**
- Internal Flutter package/project name remains `afareet_asphalt` لتجنب كسر imports/package IDs أثناء التطوير.
- Android bootstrap يضبط `android:label="3Fareet"`.
- Splash key art: `assets/branding/3fareet_splash.jpg`.

## أهم قاعدة الآن
**P1 Playable Prototype هي أعلى أولوية.** لا يتم استنزاف المشروع في Backend/Store/Online قبل وجود سباق قابل للعب على Android واجتياز التحقق.

## Team rule
لا يبدأ أي مطور مهمة بدون Task ID وOwner واضح. أي تعديل على الخطة أو Asset يتم أولًا في ملفات `docs/` لكي يراه الفريق كله.

## Backend boundary
`Flutter / Flame → HTTPS API → Laravel → MySQL`

ممنوع ربط Flutter مباشرة بـMySQL أو تضمين database credentials داخل التطبيق.
