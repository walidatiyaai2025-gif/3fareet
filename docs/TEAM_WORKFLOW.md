# Team Workflow & Definition of Done

**Document:** AFA-GOV-TEAM-001  
**Owner:** Team Lead  
**Applies to:** كل المساهمين، البشر والوكلاء الآليين

## Roles

| Role | مسؤولية القرار |
|---|---|
| Product Owner | الرؤية، Scope، قبول Milestones |
| Team Lead | الأولويات، Owners، Module Locks، الدمج |
| Unity Tech Lead | Architecture وshared interfaces وBuild pipeline |
| Gameplay Lead | Vehicle/Race/AI/Power-ups والتوازن |
| Art Director | الهوية، Concepts، 3D quality وVisual Gate |
| Technical Artist | Import/Materials/LOD/VFX/optimization |
| UI/UX Lead | Flow/HUD/RTL/accessibility |
| QA & Release Lead | Test matrix، device evidence، APK release |
| Backend Lead | API/auth/economy/security لاحقًا |

شخص واحد يمكن أن يحمل أكثر من Role مؤقتًا، لكن كل Task لها Owner واحد فقط.

## حالات المهمة

| State | المعنى |
|---|---|
| `TODO` | مسجلة لكن المتطلبات أو الأولوية غير جاهزة |
| `READY` | قابلة للاستلام، Dependencies واضحة ولا يوجد Lock متعارض |
| `IN PROGRESS` | Owner يعمل عليها وModule Lock مسجل |
| `BLOCKED` | لا يمكن التقدم؛ السبب والخطوة التالية مكتوبان |
| `IN REVIEW` | PR مفتوح وValidation الأولي مكتمل |
| `DONE` | الكود/الأصل مدمج لكن Evidence النهائي قد يبقى |
| `VERIFIED` | اجتازت Evidence المطلوبة وقبلها المسؤول المناسب |
| `DEFERRED` | خارج المرحلة الحالية بقرار واضح |

## Branch وCommit

- `feature/<TASK-ID>-short-name`
- `fix/<TASK-ID>-short-name`
- `docs/<TASK-ID>-short-name`
- Commit صغير بصيغة: `<TASK-ID>: فعل مختصر`.
- ممنوع Branch شخصي طويل العمر؛ اسحب `main` باستمرار وحل التعارض قبل المراجعة.

## Module Lock

قبل `IN PROGRESS`، يسجل Owner الصف في [`MODULE_OWNERSHIP.md`](MODULE_OWNERSHIP.md). الـLock ليس ملكية دائمة؛ هو حجز مؤقت لمنع شخصين من تغيير نفس Contract. ينتهي عند الدمج أو الإلغاء.

التغييرات التالية تحتاج تنسيق Tech Lead قبل البدء:

- public C# APIs وassemblies.
- `ProjectSettings/` و`Packages/`.
- Save/config/schema formats.
- Scene bootstrap وbuild pipeline.
- Backend API contracts.
- الاقتصاد والعملات والمكافآت.

## Definition of Ready

- Task ID ووصف Acceptance واضح.
- Owner واحد وReviewer معروف.
- Dependencies مكتملة أو Mock متفق عليه.
- الملفات المتوقعة والـModule Lock محددان.
- لا توجد Task أخرى نشطة على نفس Contract.

## Definition of Done — كل المهام

- Acceptance Criteria مكتملة بلا توسع Scope صامت.
- لا Console errors ولا compiler warnings جديدة مقصودة.
- Tests/Build المناسبة ناجحة.
- لا secrets أو cache أو generated builds في Git.
- Documentation وتسجيل الحالة محدثان.
- PR مراجع ومدمج، والـModule Lock أزيل.

## Evidence حسب التخصص

| النوع | Evidence المطلوبة لـ`VERIFIED` |
|---|---|
| Unity Gameplay | PlayMode/EditMode tests + فيديو/لقطة + Windows/Android smoke حسب التأثير |
| 3D Art | Source + export + import screenshot + poly/texture/LOD report + Art approval |
| UI | Screenshots على aspect ratios مستهدفة + RTL + safe area + readability |
| VFX | فيديو + particle/overdraw budget + low-tier check |
| Audio | ملف مرخص/مصدر + import settings + listening test على جهاز |
| Build/Release | clean build log + SHA-256 + device/API/tester/smoke checklist |
| Backend | automated tests + migration + security review + API contract |
| Docs | links valid + contradictions removed + Team Lead review |

## Review policy

- Reviewer واحد لتغيير محلي داخل Module.
- Reviewerان لتغيير Architecture/Packages/ProjectSettings/API/Economy/Save schema.
- Art Director يقبل أي Asset يدخل Visual Gate.
- QA/Release Lead وحده يرفع Build إلى `Last verified APK released/`.

## التعامل مع التعارض

الأولوية لمصدر الحقيقة بهذا الترتيب:

1. ADR/قرار معماري Approved.
2. `MASTER_DEVELOPMENT_PLAN.md`.
3. `PROJECT_STATUS.md` للواقع الحالي.
4. Task register للتنفيذ.
5. PR description/comments.

عند وجود تعارض لا تفترض؛ أوقف الدمج وأصلح الوثيقتين في نفس PR.
