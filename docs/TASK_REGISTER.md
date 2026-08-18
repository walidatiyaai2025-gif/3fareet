# عفاريت الأسفلت — Full Task Register

**Document:** AFA-TASKS-001  
**Version:** 2.0 Unity Production Baseline
**Legacy baseline tasks:** 309 (Flutter-era history preserved)
**Active production register:** Unity 3D task file below

## قواعد إلزامية
- لا يبدأ أي عمل بدون Task ID.
- Owner واحد فقط لكل Task.
- Branch: `feature/<TASK-ID>-short-name` أو `fix/<TASK-ID>-short-name`.
- Team Lead يطبق Module Lock إذا كانت مهمتان ستلمسان نفس الملفات الجوهرية أو interface مشتركة.
- Scope جديد = Task جديدة، وليس توسيعًا صامتًا لمهمة قائمة.
- حالات العمل: `TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`.
- `VERIFIED` تحتاج Build/Test/Device evidence حسب نوع المهمة.
- P1 Prototype لا تغلق بدون APK Android Verified في `Last verified APK released/`.
- **Visual Gate إلزامي:** لا يمكن إعلان P1 VERIFIED إذا كان الشكل بعيدًا عن `docs/ART_DIRECTION.md` حتى لو الكود والأداء ناجحين.
- **External Asset Handoff Policy إلزامي:** أي مبرمج أو AI agent يكتشف Asset خارجيًا لا يمكن إنشاؤه بشكل صحيح من كود المستودع يجب أن يسجله في ملف root `ASSET_CREATION_REQUESTS.txt` في نفس الـPR/commit مع Tool + Prompt + Procedure + Paths + Acceptance + Provenance. ممنوع اختراع Production asset بديل أو تخفيف gate لإخفاء النقص.
- **Programming Closure Mode:** حتى يطلب Owner صراحة استئناف الـvisual polish/asset production، الأولوية لإغلاق code/contracts/tests/CI/runtime defects. اكتشاف Asset ناقص يُسجل كـhandoff ولا يوقف إغلاق النواقص البرمجية الأخرى.
- المنتج الأساسي الآن Unity 3D. حالات Flutter `VERIFIED` تاريخ هندسي ولا تعني أن Unity Feature مكتملة.
- العمل الإنتاجي الجديد يأخذ IDs من سجل Unity؛ Flutter maintenance يأخذ `FLT-*`.

## تقسيم السجل لتقليل تعارض الفريق
تم تقسيم الـ309 مهمة إلى ملفات مستقلة حتى لا يضطر مطورون متعددون لتعديل نفس ملف المهام في نفس الوقت:

0. [Premium Visual Direction](tasks/00-VISUAL-DIRECTION.md) — VIS، إلزامي للـPrototype
1. [Prototype & Core](tasks/01-PROTOTYPE-CORE.md) — GOV/PRO/VEH/DRF/RAC/CAM/AI
2. [Gameplay, UI & Offline](tasks/02-GAMEPLAY-UI-OFFLINE.md) — PWR/UIX/GAR/CAR
3. [Economy, Backend & Online](tasks/03-ECONOMY-BACKEND-ONLINE.md) — ECO/BCK/NET
4. [Seasons & Admin](tasks/04-SEASONS-ADMIN.md) — SEA/ADM
5. [Assets, Performance & Release](tasks/05-ASSETS-PERFORMANCE-RELEASE.md) — ART/PER
6. [Unity 3D Production & Migration](tasks/06-UNITY-3D-MIGRATION.md) — **ACTIVE** U3D/UVEH/URAC/UART/UVFX/UUI/UAUD/UPER

## أولوية التنفيذ الحالية
أولوية قصوى: **U-P1 Unity 3D Vertical Slice** حسب الملف السادس. ملفات 0→5 تحفظ Backlog وتاريخ Flutter والخطة الطويلة، لكنها لا تمنح إذنًا ببدء Feature خارج Unity Active Register. Backend/Online/Seasons تبقى Deferred حتى Unity Android Verified APK.
