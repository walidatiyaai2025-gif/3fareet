# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Last updated:** 2026-08-13 (Asia/Kuwait)
**Overall status:** 🟡 **UNITY 3D PRODUCTION STARTED — WINDOWS SLICE VERIFIED / ANDROID GATE BLOCKED**

> هذه هي الصفحة الأولى للفريق. أي PR يغيّر Task/Milestone/Blocker/Build/Asset يجب أن يحدّثها في نفس PR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Product client | 🟢 Locked | Unity `6000.5.8f1` داخل `unity_game/` |
| Flutter/Flame | 🔵 Legacy verified | مرجع ميكانيك/UI قابل للبناء؛ لا Features إنتاجية جديدة دون `FLT-*` |
| Unity compile | 🟢 Verified | Import وC# compile ناجحان على Unity 6000.5.8f1 |
| Unity Windows | 🟢 Verified smoke | `afareet-unity3d.exe` بُني واشتغل 15 ثانية بلا Exceptions |
| Unity Android | 🟢 Build verified | Debug APK بُني؛ package/icon/SDK/ARM64 تم فحصها بـ `aapt` |
| 3D driving | 🟡 Blockout | Rigidbody arcade car + drift/nitro + chase camera؛ يحتاج production tuning/tests |
| Race/AI | 🟡 Blockout | procedural Cairo oval + 3 waypoint rivals؛ checkpoints/lap/ranking production rules مفتوحة |
| Visuals | 🔴 Gate open | procedural placeholders؛ production car/environment/VFX/lighting غير منفذة |
| UI | 🟡 Prototype | splash + IMGUI HUD/touch controls؛ production UI/RTL/safe-area مفتوحة |
| Branding | 🟢 Integrated | master icon ومقاسات Flutter، وأسماء Packages/Artifacts منفصلة |
| Audio | 🔴 Open | engine/drift/nitro production audio غير مدمج في Unity |
| Team system | 🟢 Baseline ready | onboarding/workflow/module ownership/Unity task register موجودة |
| Verified release APK | 🔴 None | لا يوجد Unity Android APK اجتاز جهازًا حقيقيًا |
| Backend | 🔵 Deferred/Locked | Unity → HTTPS API → Laravel → MySQL؛ لا direct DB |

## Current milestone — U-P1 Unity 3D Vertical Slice

**Gate target:** سباق 3D واحد كامل على Android به سيارة لاعب، 3 AI، Cairo premium look، Drift/Nitro، HUD/Touch، Audio، وDevice evidence.

### Delivered in PR #49

- Unity project and runtime bootstrap.
- Procedural Cairo track blockout, pyramids/buildings/neon rails.
- Arcade controller, drift, nitro, chase camera and three waypoint rivals.
- Splash, prototype HUD/touch controls and custom runtime shaders.
- Repeatable Windows/Android build entry points.
- Windows build + headless runtime smoke test.
- Flutter visual prototype/splash preservation as legacy reference.
- Shared app icon and engine-specific naming.
- Team onboarding, ownership and active Unity task system.

هذه الدفعة `IN REVIEW` حتى دمج PR #49. لا تحول مهامها إلى `VERIFIED` في السجل قبل الدمج وربط Evidence بالـcommit النهائي.

## Highest priorities next

1. `UPER-006`: تثبيت APK وتشغيل smoke matrix على أجهزة Android حقيقية.
2. `U3D-010`: توسيع الاختبارات إلى PlayMode وruntime smoke آلي.
3. `UVEH-002/UVEH-003`: قرار suspension وdriving feel قابل للضبط.
4. `URAC-002→005`: checkpoints/lap/ranking/race lifecycle production rules.
5. `UART-001/UART-002/UART-005/UART-008`: pipeline + hero car + Cairo kit + mobile rendering.
6. `UUI-002/UUI-003`: production HUD and touch controls.
7. `UPER-009/010`: Visual Gate ثم Verified Android APK.

## Active blockers / risks

| ID | Severity | Blocker / Risk | Owner / Action |
|---|---|---|---|
| STS-U01 | 🟡 Medium | APK مبني لكن لم يجتز device smoke حقيقي بعد | QA Engineer — `UPER-006` |
| STS-U02 | 🔴 High | المشهد الحالي blockout procedural وليس Visual Gate quality | Art Director/Art team — `UART-*`, `UVFX-*` |
| STS-U03 | 🟡 Medium | asmdefs/config/EditMode tests موجودة؛ PlayMode coverage ما زالت مفتوحة | Unity Tech Lead — `U3D-010` |
| STS-U04 | 🟡 Medium | Race progress الحالي nearest-waypoint prototype | Race Engineer — `URAC-002→005` |
| STS-U05 | 🟡 Medium | لا Audio production في Unity | Audio team — `UAUD-001→003` |
| STS-U06 | 🟡 Medium | مطورون كثيرون قد يتعارضون على bootstrap/ProjectSettings | Team Lead — enforce Module Locks |

## Team entry points

- [New contributor onboarding](ONBOARDING.md)
- [Team workflow and DoD](TEAM_WORKFLOW.md)
- [Module ownership and locks](MODULE_OWNERSHIP.md)
- [Active Unity task register](tasks/06-UNITY-3D-MIGRATION.md)
- [Contributing rules](../CONTRIBUTING.md)

## Historical Flutter evidence

Flutter engineering batches remain valid historical evidence, not Unity production completion:

- PRO-001→016 verified.
- GAMEPLAY-050 verified.
- P1-NEXT-050 verified at code head `86a6ea2afb273cab14730e61a152676dc90ea24f`.
- Evidence: [`work/P1-NEXT-050.md`](work/P1-NEXT-050.md).

## Last verified APK

**Status:** 🔴 **NO UNITY VERIFIED RELEASE APK YET**
**Folder:** [`../Last verified APK released/`](../Last%20verified%20APK%20released/)  

Flutter debug/release-skeleton APKs لا تعتبر المنتج النهائي ولا تدخل هذا المجلد.

## Source of truth

- [Master Development Plan](MASTER_DEVELOPMENT_PLAN.md)
- [Full Task Register](TASK_REGISTER.md)
- [Unity Active Tasks](tasks/06-UNITY-3D-MIGRATION.md)
- [Art Direction](ART_DIRECTION.md)
- [Backend Architecture](BACKEND_ARCHITECTURE.md)
- [Release Policy](RELEASE_POLICY.md)
- [Missed Assets](MISSED_ASSETS.md)

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
