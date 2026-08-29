# Unity 3D Production Task Register

**Document:** AFA-TASKS-U3D-001  
**Engine:** Unity `6000.5.8f1`  
**Product client:** `unity_game/`  
**Legacy client:** Flutter/Flame — maintenance only  
**Operational authority:** Issue #90  
**Reconciled:** 2026-08-29 (Asia/Kuwait)

> هذا السجل مكوّن من **65 مهمة ثابتة**. الحالة التشغيلية الحالية هي:
> `IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`.
>
> `IN REVIEW` هنا تعني أن مسار التنفيذ/المصدر الخاص بالمهمة موجود تحت المراجعة/التقارب، ولا تعني `VERIFIED`.
> لا يجوز ترقية أي بند إلى `VERIFIED` بدون Evidence المناسبة حسب `TEAM_WORKFLOW.md`.
> Issue #90 هو المرجع التشغيلي عند أي تعارض بين هذه الصفحة وسجل التنفيذ الحي.

## Current milestone — U-P1 Vertical Slice

هدف المرحلة: سباق Unity 3D واحد كامل على Android، سيارة لاعب + 3 AI، هوية القاهرة، Drift/Nitro، HUD/Touch، Audio، ثم exact-candidate licensed Unity + physical-device + owner/release evidence.

العمل البرمجي/العقود المصدرية متقارب على `agent/p1-remediation-convergence`. لا توجد مهام `READY` أو `TODO` داخل سجل U-P1 الآن؛ حدود الإغلاق المتبقية هي المهام الـ11 المحجوبة أدناه.

### Foundation

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| U3D-001 | P0 | إنشاء Unity project مستقل داخل المستودع | Principal Mobile Game Architect | IN REVIEW | Unity 6000.5 يفتح ويعمل |
| U3D-002 | P0 | Runtime bootstrap ومشهد Prototype | Principal Mobile Game Architect | IN REVIEW | Empty scene يولد vertical slice |
| U3D-003 | P0 | Windows build pipeline وأسماء artifacts | Principal Mobile Game Architect | IN REVIEW | `afareet-unity3d.exe` build green |
| U3D-004 | P0 | Android build pipeline وهوية package منفصلة | Principal Mobile Game Architect | IN REVIEW | build method + `com.fiftysolutions.afareetunity3d` |
| U3D-005 | P0 | App icon/branding لكل targets | Principal Mobile Game Architect | IN REVIEW | icon master + generated target icons |
| U3D-006 | P0 | إضافة asmdefs وحدود assemblies | Principal Mobile Game Architect | IN REVIEW | Core/Gameplay/UI/Editor منفصلة بلا circular refs |
| U3D-007 | P0 | Input System جديد مع keyboard/touch/gamepad | Unity Gameplay Engineer | IN REVIEW | input actions + rebinding-safe abstraction |
| U3D-008 | P0 | Config عبر ScriptableObjects | Principal Mobile Game Architect | IN REVIEW | no production tuning hardcoded |
| U3D-009 | P0 | Logging/diagnostics policy | Unity Tech Lead | IN REVIEW | structured channels + release stripping |
| U3D-010 | P0 | Unity EditMode/PlayMode test assemblies | QA Automation Engineer | IN REVIEW | tests execute headless in CI |
| U3D-011 | P0 | Unity CI compile + Windows artifact | DevOps / QA Engineer | IN REVIEW | GitHub Actions green |
| U3D-012 | P0 | Unity Android CI artifact | DevOps / QA Engineer | IN REVIEW | Android exact-candidate artifact path |

### Vehicle / Camera / Feel

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UVEH-001 | P0 | Arcade Rigidbody controller baseline | Principal Mobile Game Architect | IN REVIEW | drive/brake/reverse/steer work |
| UVEH-002 | P0 | WheelCollider أو custom suspension decision ADR | Gameplay Lead | IN REVIEW | measured decision + prototype |
| UVEH-003 | P0 | Grip/lateral slip/drift tuning assets | Vehicle Physics Engineer | IN REVIEW | tunable profiles, no magic constants |
| UVEH-004 | P0 | Ground detection and surface types | Vehicle Physics Engineer | IN REVIEW | asphalt/off-road behavior tested |
| UVEH-005 | P0 | Collision/crash response | Gameplay Engineer | IN REVIEW | stable at target speeds |
| UVEH-006 | P0 | Reset to last valid checkpoint | Gameplay Engineer | IN REVIEW | no upside-down/stuck lock |
| UVEH-007 | P0 | Nitro acceleration/consumption integration | Gameplay Engineer | IN REVIEW | curve + meter + cooldown |
| UVEH-008 | P0 | Drift energy charge rules | Gameplay Engineer | IN REVIEW | abuse guards + tests |
| UVEH-009 | P0 | Chase camera baseline | Principal Mobile Game Architect | IN REVIEW | follow/look/FOV nitro |
| UVEH-010 | P0 | Camera collision and obstruction | Camera Engineer | IN REVIEW | no geometry clipping |
| UVEH-011 | P1 | Shake/impact/drift camera states | Camera Engineer | IN REVIEW | accessibility toggle included |
| UVEH-012 | P0 | Real-device driving feel pass | Gameplay Lead | BLOCKED | exact-candidate physical-device acceptance |

### Race / AI / Track

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| URAC-001 | P0 | Cairo procedural oval track baseline | Principal Mobile Game Architect | IN REVIEW | drivable loop generated |
| URAC-002 | P0 | Checkpoint volumes and ordered validation | Race Engineer | IN REVIEW | skipped checkpoint rejected |
| URAC-003 | P0 | Lap/start/finish state machine | Race Engineer | IN REVIEW | deterministic one-lap finish |
| URAC-004 | P0 | Ranking by checkpoint/lap/progress | Race Engineer | IN REVIEW | no nearest-waypoint ranking exploit |
| URAC-005 | P0 | Countdown/results/restart flow | Race Engineer | IN REVIEW | complete race lifecycle |
| URAC-006 | P0 | Track bounds/barriers/off-road | Level Designer | IN REVIEW | player cannot leave playable area silently |
| URAC-007 | P0 | Waypoint AI baseline (3 rivals) | Principal Mobile Game Architect | IN REVIEW | three rivals complete loop |
| URAC-008 | P0 | AI racing line and braking zones | AI Engineer | IN REVIEW | curve-aware speed planning |
| URAC-009 | P1 | AI avoidance/overtake/personality | AI Engineer | IN REVIEW | reproducible seeded behaviors |
| URAC-010 | P0 | AI stuck recovery and finish tests | AI Engineer | IN REVIEW | automated coverage |
| URAC-011 | P0 | Replace blockout with Cairo vertical-slice layout | Level Designer | BLOCKED | authored layout + exact-candidate runtime/device/owner proof |
| URAC-012 | P0 | Track completion device verification | QA Engineer | BLOCKED | physical-device lap/results/restart verification |

### Art / VFX / UI / Audio

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UART-001 | P0 | 3D asset folder/naming/import convention | Technical Artist | IN REVIEW | documented + validator-ready |
| UART-002 | P0 | Player hero car blockout | Principal Mobile Game Architect | IN REVIEW | correct scale/pivot/wheels/collider |
| UART-003 | P0 | Hero car production model + LODs | Vehicle Artist | BLOCKED | acceptable external production source + licensed binding/render proof |
| UART-004 | P1 | Three rival production models/variants | Vehicle Artist | BLOCKED | acceptable external Rival production package + licensed prefab/runtime/owner proof |
| UART-005 | P0 | Cairo modular street kit | Environment Artist | BLOCKED | licensed runtime/device/owner proof |
| UART-006 | P0 | Pyramid/minaret/dome landmark kit | Environment Artist | BLOCKED | licensed landmark runtime/device/owner proof |
| UART-007 | P0 | Track dressing/lighting vertical slice | Level Artist | BLOCKED | licensed dressing runtime/device/owner proof |
| UART-008 | P0 | Mobile URP materials and lighting setup | Technical Artist | IN REVIEW | low/mid/high quality tiers |
| UVFX-001 | P0 | Drift smoke/spirit trail signature | VFX Artist | IN REVIEW | pooled + budget documented |
| UVFX-002 | P0 | Nitro spirit burst/trail | VFX Artist | IN REVIEW | readable at speed + low tier |
| UVFX-003 | P1 | Collision/boost pickup feedback | VFX Artist | IN REVIEW | pooled and profiled |
| UUI-001 | P0 | Runtime splash/loading | Principal Mobile Game Architect | IN REVIEW | supplied artwork + progress |
| UUI-002 | P0 | Production race HUD in uGUI/UI Toolkit | Unity UI Engineer | IN REVIEW | pos/speed/spirit/time/safe area |
| UUI-003 | P0 | Touch controls production pass | Unity UI Engineer | IN REVIEW | multi-touch, landscape, devices |
| UUI-004 | P0 | Pause/result/restart screens | Unity UI Engineer | IN REVIEW | full flow + Arabic/English |
| UUI-005 | P1 | RTL/localization framework | Unity UI Engineer | IN REVIEW | Arabic shaping/font verified |
| UAUD-001 | P0 | Engine loop with RPM/speed layers | Audio Designer | IN REVIEW | import settings + device listening |
| UAUD-002 | P0 | Drift/nitro/collision SFX | Audio Designer | IN REVIEW | event hooks + balanced mix |
| UAUD-003 | P1 | Music integration and pause lifecycle | Audio Engineer | IN REVIEW | no duplicate players, lifecycle safe |

### Performance / Android / Release

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UPER-001 | P0 | Target device tiers and budgets | QA/Performance Lead | IN REVIEW | FPS/memory/thermal targets documented |
| UPER-002 | P0 | Unity Profiler baseline capture | Performance Engineer | IN REVIEW | CPU/GPU/memory report |
| UPER-003 | P0 | Object/material/mesh pooling audit | Technical Artist | IN REVIEW | allocations and draw calls reduced |
| UPER-004 | P0 | Android module + SDK/NDK/OpenJDK install | Principal Mobile Game Architect | IN REVIEW | Unity detects Android target |
| UPER-005 | P0 | First Unity Android debug APK | Principal Mobile Game Architect | IN REVIEW | APK built and package inspected |
| UPER-006 | P0 | Android device smoke matrix | QA Engineer | BLOCKED | exact-candidate Android smoke/profiler/performance matrix |
| UPER-007 | P0 | Release keystore/secrets process | Release Engineer | IN REVIEW | no secrets in Git |
| UPER-008 | P0 | Unity release APK/AAB pipeline | Release Engineer | IN REVIEW | signed reproducible build |
| UPER-009 | P0 | P1 Visual Gate review | Art Director + Owner | BLOCKED | owner/Art Director Visual Gate on exact candidate |
| UPER-010 | P0 | P1 Verified APK publication | QA/Release Lead | BLOCKED | all gates + SHA/device evidence + manual publication approval |

## Fixed blocked set — 11

1. `UART-003` — acceptable externally-authored Hero production source + licensed binding/render proof.
2. `UART-004` — acceptable externally-authored Rival production package + licensed prefab/runtime/owner proof.
3. `UART-005` — licensed runtime/device/owner proof.
4. `UART-006` — licensed landmark runtime/device/owner proof.
5. `UART-007` — licensed dressing runtime/device/owner proof.
6. `URAC-011` — exact-candidate runtime/device/owner proof.
7. `UVEH-012` — physical-device driving-feel acceptance.
8. `URAC-012` — physical-device lap/results/restart verification.
9. `UPER-006` — Android smoke/profiler/performance matrix.
10. `UPER-009` — owner/Art Director Visual Gate.
11. `UPER-010` — final manual publication approval.

## Closure sequence

1. Supply acceptable external Hero + Rival production art that satisfies the repository source/dependency/provenance gates.
2. Run the licensed-staging readiness chain on one clean exact convergence SHA.
3. Run licensed Unity staging and review/commit the resulting approved import metadata/prefabs/provenance.
4. Build/test the same exact candidate with Unity `6000.5.8f1`.
5. Prove production Hero/Rivals/Cairo/landmarks/dressing/layout in the real Player.
6. Restart physical-device evidence at 0/16; finish UVEH-012, URAC-012 and UPER-006.
7. Complete candidate-bound production-art review and UPER-009.
8. UPER-010 remains the explicit final manual publication approval.
9. Only then may convergence/integration advance and `LAST_VERIFIED_APK.md` change.

## Flutter legacy tasks

أي إصلاح ضروري يأخذ ID `FLT-###`. الحالة الافتراضية لكل Feature جديد في Flutter هي `DEFERRED`; لا تنقل Gameplay إنتاجيًا إلى المسارين معًا. يسمح فقط بـ:

- إصلاح Build/CI يمنع استخدام المرجع.
- الحفاظ على اختبارات الميكانيك كمرجع.
- استخراج Config/Rules موثقة لنقلها إلى Unity.
- مقارنة سلوك Migration بمهمة مستقلة.

## Dispatch status

**لا توجد حاليًا U-P1 tasks بحالة `READY`.** لا تنشئ موجة برمجية جديدة لمجرد إبقاء Workers مشغولين. أي عمل جديد يجب أن يكون إصلاحًا حقيقيًا ناتجًا عن licensed Unity/runtime/device/art review أو مهمة governance منفصلة لا تغيّر عداد الـ65.

External execution/input gates لا تُحوّل إلى نجاحات صناعية أو mock evidence. `Built` لا تساوي `Device Verified`.
