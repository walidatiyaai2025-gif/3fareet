# عفاريت الأسفلت — Full Task Register

**Document:** AFA-TASKS-001  
**Version:** 2.0  
**Baseline:** legacy task families + current Unity AI-first closure program

## قواعد إلزامية

- لا يبدأ أي عمل بدون Task ID.
- Owner واحد فقط لكل Task.
- Branch: `feature/<TASK-ID>-short-name` أو `fix/<TASK-ID>-short-name` أو `infra/<TASK-ID>-short-name` أو `docs/<TASK-ID>-short-name`.
- Team Lead يطبق Module Lock إذا كانت مهمتان ستلمسان نفس الملفات الجوهرية أو interface مشتركة.
- Scope جديد = Task جديدة، وليس توسيعًا صامتًا لمهمة قائمة.
- حالات العمل: `TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`.
- `VERIFIED` تحتاج Build/Test/Device/Visual evidence حسب نوع المهمة.
- P1 لا يغلق بدون Android real-device evidence وVisual Gate.
- **Visual Gate إلزامي:** لا يمكن إعلان P1 VERIFIED إذا كان الشكل بعيدًا عن `docs/ART_DIRECTION.md` و`docs/AI_AUTONOMOUS_3D_EXECUTION_PLAN.md` حتى لو الكود ناجح.
- كل Worker يقرأ `AGENTS.md` قبل التنفيذ.

## تقسيم السجل لتقليل تعارض الفريق

تم تقسيم المهام إلى ملفات مستقلة حتى لا يضطر مطورون متعددون لتعديل نفس الملف:

0. [Premium Visual Direction](tasks/00-VISUAL-DIRECTION.md) — VIS
1. [Prototype & Core](tasks/01-PROTOTYPE-CORE.md) — GOV/PRO/VEH/DRF/RAC/CAM/AI legacy/core register
2. [Gameplay, UI & Offline](tasks/02-GAMEPLAY-UI-OFFLINE.md) — PWR/UIX/GAR/CAR
3. [Economy, Backend & Online](tasks/03-ECONOMY-BACKEND-ONLINE.md) — ECO/BCK/NET
4. [Seasons & Admin](tasks/04-SEASONS-ADMIN.md) — SEA/ADM
5. [Assets, Performance & Release](tasks/05-ASSETS-PERFORMANCE-RELEASE.md) — ART/PER
6. **[Unity AI-First 3D Closure](tasks/06-UNITY-AI-3D-CLOSURE.md) — U3D current execution program**

## أولوية التنفيذ الحالية

الأولوية القصوى هي **U3D P0 + Visual Gate** حتى تصبح اللعبة مقنعة بصريًا كلعبة سباق 3D على جهاز Android حقيقي.

Current closure order:

1. `U3D-GOV-*` — canonical Unity live-state reconciliation.
2. `U3D-CAM-*` + `U3D-VEH-*` — perspective + vehicle grounding.
3. `U3D-ROAD-*` + `U3D-WLD-*` + `U3D-LGT-*` — Cairo depth/density/lighting.
4. `U3D-TRF-*` + `U3D-VFX-*` — scale, motion and feedback.
5. `U3D-UI-*` / `U3D-AUD-*` — supporting presentation.
6. `U3D-QA-*` / `U3D-REL-*` — Android evidence, performance and verified build.

Backend/Online/Season expansion يبقى مخططًا ولكنه لا يزاحم إغلاق الـ3D vertical slice.

## Multi-worker rule

التوازي يتم حسب Module ownership وليس بعدد الملفات. لا يجوز إنشاء Camera system ثانية أو Vehicle controller ثانٍ أو World builder منافس فقط لتجنب conflict. عند الحاجة إلى تعديل interface مشتركة، تنفذ interface task أولًا ثم تعاد قاعدة الفروع التابعة عليها.
