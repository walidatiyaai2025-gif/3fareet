# Contributing to 3fareet

أهلًا بك في فريق **عفاريت الأسفلت**. المنتج النهائي لعبة Unity 3D للموبايل؛ مشروع Flutter الموجود في الجذر Prototype مرجعي ولا تُضاف إليه Features إنتاجية جديدة إلا بمهمة `FLT-*` صريحة.

## قبل كتابة أي كود

1. اقرأ [`AGENTS.md`](AGENTS.md) ثم [`docs/ONBOARDING.md`](docs/ONBOARDING.md).
2. راجع الوضع الحالي في [`docs/PROJECT_STATUS.md`](docs/PROJECT_STATUS.md).
3. اختر Task بحالة `READY` من [`docs/tasks/06-UNITY-3D-MIGRATION.md`](docs/tasks/06-UNITY-3D-MIGRATION.md).
4. اطلب من Team Lead وضع اسمك كـOwner وتسجيل الـModule Lock.
5. أنشئ Branch باسم `feature/<TASK-ID>-short-name` أو `fix/<TASK-ID>-short-name`.

ممنوع بدء عمل بدون Task ID وOwner واحد واضح. ممنوع تعديل ملفات مملوكة لمهمة نشطة أخرى دون تنسيق مكتوب.

## المنتج ومساراته

- `unity_game/` — المنتج الأساسي Unity 3D؛ كل Gameplay وArt وUI إنتاجي جديد هنا.
- `lib/`, `test/`, `assets/` — Flutter/Flame legacy prototype؛ صيانة ومرجع ميكانيك فقط.
- `docs/` — مصدر الحقيقة للخطة والحالة والمهام والقرارات.
- `docs/assets/` — سجل وتسليمات الأصول الفنية.
- [`docs/releases/LAST_VERIFIED_APK.md`](docs/releases/LAST_VERIFIED_APK.md) — المؤشر الرسمي الوحيد لآخر Unity APK اجتاز جهازًا حقيقيًا. ملف APK نفسه يُرفع كـGitHub Release Asset ولا يُعمل له commit.

## شروط الـPR

- PR واحد = Task أو مجموعة صغيرة مترابطة وافق عليها Team Lead.
- املأ Task ID وOwner وValidation وملفات الـModule Lock.
- لا ترفع `Library/`, `Temp/`, `Builds/`, APK أو أسرارًا.
- لا تستخدم كلمة `Verified` لنسخة بُنيت فقط؛ يلزم real-device smoke ودليل ورابط GitHub Release حسب [`docs/RELEASE_POLICY.md`](docs/RELEASE_POLICY.md).
- حدّث سجل المهمة و`PROJECT_STATUS.md` في نفس PR إذا تغيرت الحالة الفعلية.
- `DONE` تعني التنفيذ مكتمل؛ `VERIFIED` تحتاج Evidence مستقل حسب نوع المهمة.
- لا تدمج PR بنفسك إن كنت مؤلفه؛ يلزم Reviewer واحد على الأقل، واثنان للتغييرات المعمارية/الاقتصاد/الشبكة.

التفاصيل الكاملة: [`docs/TEAM_WORKFLOW.md`](docs/TEAM_WORKFLOW.md).
