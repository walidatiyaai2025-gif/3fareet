# عفاريت الأسفلت — Full Task Register

**Document:** AFA-TASKS-001  
**Version:** 1.1 Baseline  
**Total baseline tasks:** 309

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

## تقسيم السجل لتقليل تعارض الفريق
تم تقسيم الـ309 مهمة إلى ملفات مستقلة حتى لا يضطر مطورون متعددون لتعديل نفس ملف المهام في نفس الوقت:

0. [Premium Visual Direction](tasks/00-VISUAL-DIRECTION.md) — VIS، إلزامي للـPrototype
1. [Prototype & Core](tasks/01-PROTOTYPE-CORE.md) — GOV/PRO/VEH/DRF/RAC/CAM/AI
2. [Gameplay, UI & Offline](tasks/02-GAMEPLAY-UI-OFFLINE.md) — PWR/UIX/GAR/CAR
3. [Economy, Backend & Online](tasks/03-ECONOMY-BACKEND-ONLINE.md) — ECO/BCK/NET
4. [Seasons & Admin](tasks/04-SEASONS-ADMIN.md) — SEA/ADM
5. [Assets, Performance & Release](tasks/05-ASSETS-PERFORMANCE-RELEASE.md) — ART/PER

## أولوية التنفيذ الحالية
أولوية قصوى: **VIS + P0/P1 Prototype tasks** حتى نجاح الـPlayable Prototype Gate. Backend/Online/Season work يبقى مخططًا ولكنه لا يزاحم إثبات القيادة والـDrift/Nitro والـAI والهوية البصرية الفخمة والـAPK.
