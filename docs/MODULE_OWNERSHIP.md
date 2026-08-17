# Module Ownership & Active Locks

**Owner:** Team Lead  
**قاعدة:** Role ownership للمراجعة، وActive Lock للحجز المؤقت فقط. الحالة التشغيلية للـU-P1 يجب أن تبقى متسقة مع Issue #90 و`PROJECT_STATUS.md`.

## Ownership map

| Module | Paths | Primary reviewer | Backup reviewer |
|---|---|---|---|
| Unity core/bootstrap | `unity_game/Assets/Afareet/Scripts/Core/` | Unity Tech Lead | Gameplay Lead |
| Vehicle physics | `unity_game/Assets/Afareet/Scripts/Vehicle/` | Gameplay Lead | Unity Tech Lead |
| Race & AI | `unity_game/Assets/Afareet/Scripts/Race/` | Gameplay Lead | AI Engineer |
| World/Track | `unity_game/Assets/Afareet/Scripts/World/`, `unity_game/Assets/Scenes/` | Level Lead | Unity Tech Lead |
| Unity UI | `unity_game/Assets/Afareet/Scripts/UI/` | UI/UX Lead | Unity Tech Lead |
| Unity editor/build | `unity_game/Assets/Afareet/Editor/`, `unity_game/ProjectSettings/`, `unity_game/Packages/` | Unity Tech Lead | QA/Release Lead |
| 3D authored sources / production art | `unity_game/Assets/Afareet/ArtSource/`, `unity_game/Assets/Afareet/Art/`, `docs/assets/` | Art Director | Technical Artist |
| Branding | `assets/branding/`, `unity_game/Assets/Afareet/Branding/` | Art Director | UI/UX Lead |
| Flutter legacy | `lib/`, `test/`, root `assets/`, `tool/` | Flutter Maintainer | Unity Tech Lead |
| Project governance / P1 closure tooling | `docs/`, `.github/`, `tools/android/` release/evidence contracts | Team Lead | QA/Release Lead |
| Backend | future `backend/`, API contracts | Backend Lead | Unity Tech Lead |

## Active work board

| Task | Owner | Branch/PR | Locked paths/contracts | Since | Status |
|---|---|---|---|---|---|
| U-P1 Step 25 — authoritative blocker/state consistency closure | Team Lead | `agent/step25-p1-blocker-closure-audit` / PR #225 | `docs/PROJECT_STATUS.md`, `docs/tasks/06-UNITY-3D-MIGRATION.md`, `docs/MODULE_OWNERSHIP.md`, P1 closure audit/consistency tools and their workflows/tests | 2026-08-17 | IN REVIEW |

The older PR #49 bootstrap locks were removed from this **active** board because they no longer describe the current branch/PR topology. Removing stale locks is not a task-state promotion and does not mark any U-P1 work `VERIFIED`.

## External / blocking ownership

هذه ليست Active Locks للبرمجة داخل المستودع؛ هي حدود evidence/approval التي تمنع إغلاق الـ11 blockers:

| Boundary | Responsible role | Current requirement |
|---|---|---|
| UART-003 Hero source | Vehicle Artist + Art Director | real owner-approved externally-authored Afareet King production source under canonical source path |
| Licensed Unity staging/build | Unity Tech Lead + QA/Release Lead | authorized licensed Windows execution and exact candidate lineage |
| Physical-device evidence | QA/Release Lead + Gameplay Lead | fresh 0/16 exact Git/APK device session for driving/race/performance blockers |
| UPER-009 Visual Gate | Art Director + Owner | explicit exact-candidate visual acceptance |
| UPER-010 publication | QA/Release Lead | explicit manual publication approval after all prior gates |

## Lock procedure

1. أضف صفًا قبل تعديل الملفات/العقود المحجوزة لمهمة نشطة، مع Owner واحد وBranch/PR واضحين.
2. لا تستخدم `TBD` كـOwner لمهمة `IN PROGRESS` أو `IN REVIEW`.
3. إذا احتجت Path محجوزًا، اتفق مع Owner ودوّن Shared Lock أو قسّم Contract.
4. احذف الصف بعد الدمج أو الإلغاء، وانقل النتيجة إلى Task status/evidence المناسب.
5. Lock أقدم من 3 أيام بلا تحديث يراجعه Team Lead؛ لا يزال فعالًا حتى إلغائه صراحة.
6. في stacked PRs، يكون lock المسجل هو أعلى branch نشط يملك التغيير الحالي؛ لا تترك locks قديمة توحي بأن branch تاريخيًا ما زال هو مصدر الحقيقة.
7. أي تغيير في `PROJECT_STATUS.md` أو سجل U-P1 يجب أن يحافظ على تطابق `IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65` وblocker identities مع Issue #90 إلى أن توجد evidence شرعية تغيّر السجل.
