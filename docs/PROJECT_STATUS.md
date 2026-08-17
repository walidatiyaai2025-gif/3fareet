# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Last updated:** 2026-08-17 (Asia/Kuwait)  
**Overall status:** 🟠 **U-P1 ENGINEERING/SOURCE CONVERGENCE IN REVIEW — 11 EXTERNAL/RUNTIME/DEVICE/OWNER BLOCKERS REMAIN**

> هذه هي الصفحة الأولى للفريق. GitHub Issue #90 هو الـoperational ledger الحالي للـU-P1 fixed register، وهذه الصفحة وسجل Unity يجب أن يطابقاه. لا تستخدم `DONE` أو `VERIFIED` لمجرد نجاح source/static/CI.

**U-P1 aggregate:** `IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Product client | 🟢 Locked | Unity `6000.5.8f1` داخل `unity_game/`؛ Flutter/Flame مرجع Legacy فقط |
| U-P1 fixed register | 🟠 In review | 65 مهمة ثابتة: 54 `IN REVIEW` + 11 `BLOCKED`; لا توجد مهمة P1 في `READY` أو `TODO` |
| Engineering/source coverage | 🟢 Implemented for current scope | source/static/contracts موجودة للـ54 غير المحجوبة؛ هذه ليست runtime/device verification |
| Hero production source | 🔴 Blocked | المصدر الحقيقي externally-authored لـAfareet King غير موجود بعد؛ UART-003/UPER-009 لا يمكن إغلاقهما برمجيًا من داخل المستودع الحالي |
| Rival/Cairo/landmark/dressing/layout source | 🟡 Source-ready / runtime pending | مسارات المصدر والـstaging contracts موجودة، لكن licensed Unity + exact Player/device + owner proof ما زالت مطلوبة |
| Race restart regression | 🟡 Source-tested | URAC-012 لديه source/PlayMode regression؛ physical-device lap/results/restart proof ما زال مطلوبًا |
| Licensed Unity candidate | 🔴 Pending external execution | لا توجد على الحالة الحالية سلسلة licensed Unity مكتملة تنتج candidate صالحًا للإغلاق النهائي |
| Physical-device evidence | 🔴 Pending | UVEH-012 وURAC-012 وUPER-006 تتطلب evidence جديدًا من 0/16 على exact Git/APK fingerprint |
| Visual Gate | 🔴 Blocked | UPER-009 يحتاج owner/Art Director acceptance للـexact candidate بعد اكتمال authored production art |
| Publication | 🔴 Blocked | UPER-010 آخر gate يدوي؛ ممنوع publish/tag/Last Verified قبل اكتمال جميع الأدلة |
| Last Verified Unity APK | 🔴 None | المصدر الرسمي الوحيد: [`releases/LAST_VERIFIED_APK.md`](releases/LAST_VERIFIED_APK.md) |
| Post-P1 backlog | 🔵 Deferred | Garage/Career/Power-ups/Main Menu في Issues منفصلة؛ لا توسّع سجل U-P1 إلى task 66 |

## Current convergence topology

- Main-target gate: PR #112, `agent/unblock-final-5` → `main`, ويظل Draft حتى اكتمال الـP1 closure chain.
- Controlled convergence: PR #144, `agent/p1-remediation-convergence` → `agent/unblock-final-5`, ويظل Draft وغير publication-eligible حتى اكتمال الأدلة.
- سلسلة hardening/closure الحالية متراكبة حتى PR #225 (`agent/step25-p1-blocker-closure-audit`).
- Step 25 يحافظ على Issue #90 كـoperational source of truth ويضيف read-only blocker/evidence auditing؛ لا يمنح قبولًا أو verification تلقائيًا.
- أي head جديد يجب أن يمر بنفس CI/gate chain؛ نجاح head سابق لا يثبت head لاحقًا.

## Authoritative P1 blockers

هذه القائمة يجب أن تطابق Issue #90 حرفيًا من حيث الهوية والترتيب. لا تُزال مهمة منها إلا بعد evidence حقيقي ومراجعة بشرية مناسبة.

1. UART-003 — real Hero production model + licensed binding/render proof (#127)
2. UART-004 — licensed Rival production prefab binding/runtime/owner proof (#128)
3. UART-005 — licensed runtime/device/owner proof (#128)
4. UART-006 — licensed landmark runtime/device/owner proof (#128)
5. UART-007 — licensed dressing runtime/device/owner proof (#128)
6. URAC-011 — exact-candidate runtime/device/owner proof (#128)
7. UVEH-012 — real-device driving-feel acceptance
8. URAC-012 — physical-device lap/results/restart verification
9. UPER-006 — Android smoke/profiler/performance matrix
10. UPER-009 — owner/Art Director Visual Gate
11. UPER-010 — manual publication approval, last

## Required next execution

1. Commit the real owner-approved externally-authored Afareet King Hero source under the canonical production source path.
2. Run the licensed-staging readiness chain and require the exact source/authorization identity to be READY without changing task state.
3. Run licensed Unity staging on the authorized Windows path; review and commit only the approved `unity_game/Assets/` staging delta.
4. Build from the resulting clean reviewed candidate SHA; require licensed EditMode/PlayMode, including the restart regression, and Android ARM64 candidate output on that same identity.
5. Prove the authored Hero, rivals, Cairo street kit, landmarks, dressing and vertical-slice layout are active in the Player and no procedural/blockout fallback supplies accepted visual evidence.
6. Start fresh physical-device evidence from **0/16** on the exact candidate Git SHA + APK SHA-256; complete UVEH-012, URAC-012 and UPER-006.
7. Export/reconcile the sanitized evidence bundle while preserving exact staging/candidate/device lineage.
8. Obtain explicit UPER-009 owner/Art Director acceptance for that exact candidate.
9. Run final lineage-bound release preflight; only then obtain UPER-010 manual publication approval.
10. After publication, bind the receipt and fresh post-publication physical-device smoke before any Last Verified pointer can change; only then may convergence/main advancement be reviewed.

## Historical rejected candidate

Git `33a1b09ae68c9272a53d53b1c275804daa5be6db` / APK SHA-256 `034db4bdbcdbd0544167b3c6b588f3c0fe4aa88451f8385f1bb5b24acb825d11` remains diagnostic only. Its device evidence must not be reused for the current closure path.

## Guardrails

- لا تدمج PR #144 أو PR #112 قبل اكتمال الأدلة والـreview المطلوب.
- لا تستخدم نجاح source/static/CI كبديل عن licensed Unity/runtime/device/owner evidence.
- لا تنشر release/tag ولا تحدّث `LAST_VERIFIED_APK.md` قبل اكتمال الـmanual publication + post-publication verification policy.
- لا تضف U-P1 task جديدة لمعالجة #127/#128؛ هما defects تمنع مهام موجودة داخل الـ65.
- `docs/tasks/06-UNITY-3D-MIGRATION.md` يجب أن يبقى متطابقًا مع aggregate/blocker set في Issue #90.
- CI الخاص بـP1 blocker closure يقارن Issue #90 الحي بهذه الصفحة وبسجل Unity ويفشل عند drift.

## Team entry points

- [New contributor onboarding](ONBOARDING.md)
- [Team workflow and DoD](TEAM_WORKFLOW.md)
- [Module ownership and active locks](MODULE_OWNERSHIP.md)
- [Active Unity task register](tasks/06-UNITY-3D-MIGRATION.md)
- [Release policy](RELEASE_POLICY.md)
- [Last Verified APK pointer](releases/LAST_VERIFIED_APK.md)
- [Contributing rules](../CONTRIBUTING.md)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** أي PR يغيّر U-P1 Task/Milestone/Blocker/Build/Asset يجب أن يحدّث الحالة المتأثرة في نفس PR، مع بقاء Issue #90 والـrepository status متسقين.
