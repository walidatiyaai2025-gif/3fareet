# Unity 3D Production Task Register

**Document:** AFA-TASKS-U3D-001  
**Engine:** Unity `6000.5.8f1`  
**Product client:** `unity_game/`  
**Legacy client:** Flutter/Flame — maintenance only  
**Operational ledger:** GitHub Issue #90

هذا هو سجل التنفيذ الثابت للـU-P1. حالة التشغيل الحالية يجب أن تطابق Issue #90؛ `IN REVIEW` تعني أن التنفيذ/المصدر الهندسي موجود تحت المراجعة ولا تعني `DONE` أو `VERIFIED`. أي إثبات licensed Unity/runtime/device/owner مطلوب يظل blocker حتى وجود الدليل الحقيقي.

**U-P1 aggregate:** `IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`

## Current milestone — U-P1 Vertical Slice

هدف المرحلة: سباق واحد كامل على Android، سيارة لاعب + 3 AI، Cairo premium look، Drift/Nitro، HUD، صوت، وأداء مقبول، ثم evidence حقيقي على جهاز فعلي وVisual Gate ونشر يدوي مضبوط.

### Foundation / Architecture

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
| U3D-010 | P0 | Unity EditMode/PlayMode test assemblies | QA Automation Engineer | IN REVIEW | tests execute headless in CI; licensed execution still required where applicable |
| U3D-011 | P0 | Unity CI compile + Windows artifact | DevOps / QA Engineer | IN REVIEW | GitHub Actions contracts present; licensed/runtime evidence remains separate |
| U3D-012 | P0 | Unity Android CI artifact | DevOps / QA Engineer | IN REVIEW | Android build orchestration present; exact licensed candidate evidence remains separate |

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
| UVEH-012 | P0 | Real-device driving feel pass | Gameplay Lead | BLOCKED | physical-device driving-feel acceptance required on exact candidate |

### Race / AI / Track

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| URAC-001 | P0 | Cairo procedural oval track baseline | Principal Mobile Game Architect | IN REVIEW | drivable loop generated; blockout is not Visual Gate evidence |
| URAC-002 | P0 | Checkpoint volumes and ordered validation | Race Engineer | IN REVIEW | skipped checkpoint rejected |
| URAC-003 | P0 | Lap/start/finish state machine | Race Engineer | IN REVIEW | deterministic one-lap finish |
| URAC-004 | P0 | Ranking by checkpoint/lap/progress | Race Engineer | IN REVIEW | no nearest-waypoint ranking exploit |
| URAC-005 | P0 | Countdown/results/restart flow | Race Engineer | IN REVIEW | complete race lifecycle |
| URAC-006 | P0 | Track bounds/barriers/off-road | Level Designer | IN REVIEW | player cannot leave playable area silently |
| URAC-007 | P0 | Waypoint AI baseline (3 rivals) | Principal Mobile Game Architect | IN REVIEW | three rivals complete loop |
| URAC-008 | P0 | AI racing line and braking zones | AI Engineer | IN REVIEW | curve-aware speed planning |
| URAC-009 | P1 | AI avoidance/overtake/personality | AI Engineer | IN REVIEW | reproducible seeded behaviors |
| URAC-010 | P0 | AI stuck recovery and finish tests | AI Engineer | IN REVIEW | automated coverage |
| URAC-011 | P0 | Replace blockout with Cairo vertical-slice layout | Level Designer | BLOCKED | exact-candidate authored runtime + physical-device + owner visual proof required (#128) |
| URAC-012 | P0 | Track completion device verification | QA Engineer | BLOCKED | physical-device lap/results/restart verification on exact candidate required |

### Art / VFX / UI / Audio

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UART-001 | P0 | 3D asset folder/naming/import convention | Technical Artist | IN REVIEW | documented + validator-ready |
| UART-002 | P0 | Player hero car blockout | Principal Mobile Game Architect | IN REVIEW | engineering/blockout reference only; cannot satisfy Visual Gate |
| UART-003 | P0 | Hero car production model + LODs | Vehicle Artist | BLOCKED | real externally-authored Hero source + licensed binding/render + owner acceptance required (#127) |
| UART-004 | P1 | Three rival color/material variants | Vehicle Artist | BLOCKED | licensed Rival production prefab binding/runtime/owner proof required (#128) |
| UART-005 | P0 | Cairo modular street kit | Environment Artist | BLOCKED | licensed runtime/device/owner proof required (#128) |
| UART-006 | P0 | Pyramid/minaret/dome landmark kit | Environment Artist | BLOCKED | licensed landmark runtime/device/owner proof required (#128) |
| UART-007 | P0 | Track dressing/lighting vertical slice | Level Artist | BLOCKED | licensed dressing runtime/device/owner proof required (#128) |
| UART-008 | P0 | Mobile URP materials and lighting setup | Technical Artist | IN REVIEW | low/mid/high quality tiers |
| UVFX-001 | P0 | Drift smoke/spirit trail signature | VFX Artist | IN REVIEW | pooled + budget documented |
| UVFX-002 | P0 | Nitro spirit burst/trail | VFX Artist | IN REVIEW | readable at speed + low tier |
| UVFX-003 | P1 | Collision/boost pickup feedback | VFX Artist | IN REVIEW | pooled and profiled |
| UUI-001 | P0 | Runtime splash/loading | Principal Mobile Game Architect | IN REVIEW | supplied artwork + progress |
| UUI-002 | P0 | Production race HUD in uGUI/UI Toolkit | Unity UI Engineer | IN REVIEW | pos/speed/spirit/time/safe area |
| UUI-003 | P0 | Touch controls production pass | Unity UI Engineer | IN REVIEW | multi-touch, landscape, devices |
| UUI-004 | P0 | Pause/result/restart screens | Unity UI Engineer | IN REVIEW | full flow + Arabic/English |
| UUI-005 | P1 | RTL/localization framework | Unity UI Engineer | IN REVIEW | Arabic shaping/font verified by appropriate evidence before final acceptance |
| UAUD-001 | P0 | Engine loop with RPM/speed layers | Audio Designer | IN REVIEW | import settings + device listening |
| UAUD-002 | P0 | Drift/nitro/collision SFX | Audio Designer | IN REVIEW | event hooks + balanced mix |
| UAUD-003 | P1 | Music integration and pause lifecycle | Audio Engineer | IN REVIEW | no duplicate players, lifecycle safe |

### Performance / Android / Release

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UPER-001 | P0 | Target device tiers and budgets | QA/Performance Lead | IN REVIEW | FPS/memory/thermal targets documented |
| UPER-002 | P0 | Unity Profiler baseline capture | Performance Engineer | IN REVIEW | source/tooling path present; final exact-candidate device matrix remains UPER-006 |
| UPER-003 | P0 | Object/material/mesh pooling audit | Technical Artist | IN REVIEW | allocations and draw calls controlled within target budgets |
| UPER-004 | P0 | Android module + SDK/NDK/OpenJDK install | Principal Mobile Game Architect | IN REVIEW | Unity Android toolchain contracts present |
| UPER-005 | P0 | First Unity Android debug APK | Principal Mobile Game Architect | IN REVIEW | historical/build evidence exists; not a current Device Verified release |
| UPER-006 | P0 | Android device smoke matrix | QA Engineer | BLOCKED | fresh Android smoke/profiler/performance matrix on exact candidate required |
| UPER-007 | P0 | Release keystore/secrets process | Release Engineer | IN REVIEW | no secrets in Git; release path remains manual/policy-bound |
| UPER-008 | P0 | Unity release APK/AAB pipeline | Release Engineer | IN REVIEW | reproducible exact-candidate release tooling without auto-publication |
| UPER-009 | P0 | P1 Visual Gate review | Art Director + Owner | BLOCKED | owner/Art Director visual acceptance on exact candidate required |
| UPER-010 | P0 | P1 Verified APK publication | QA/Release Lead | BLOCKED | manual publication approval is final; publication alone still does not self-mark VERIFIED |

## Blocker execution order

هذه ليست مهام جديدة؛ هي خطوات إغلاق الـ11 blockers الموجودة داخل نفس سجل الـ65:

1. `UART-003`: إدخال الـHero production source الحقيقي ثم licensed staging/runtime proof.
2. `UART-004/005/006/007` + `URAC-011`: licensed Unity + exact candidate Player proof للمصادر authored الموجودة/المستلمة.
3. `UVEH-012` + `URAC-012` + `UPER-006`: fresh physical-device evidence من 0/16 على exact Git/APK fingerprint.
4. `UPER-009`: owner/Art Director Visual Gate على نفس candidate.
5. `UPER-010`: manual publication approval أخيرًا، ثم post-publication reconciliation/smoke قبل أي Last Verified change.

## Flutter legacy tasks

أي إصلاح ضروري في Flutter يأخذ ID `FLT-###`. الحالة الافتراضية لكل Feature جديد في Flutter هي `DEFERRED`; لا تنقل Gameplay إنتاجيًا إلى المسارين معًا. يسمح فقط بـ:

- إصلاح Build/CI يمنع استخدام المرجع.
- الحفاظ على اختبارات الميكانيك كمرجع.
- استخراج Config/Rules موثقة لنقلها إلى Unity.
- مقارنة سلوك Migration بمهمة مستقلة.

## State guardrail

`tools/android/p1_repository_state_consistency.py` وCI الخاص بـP1 blocker closure يثبتان هوية وترتيب الـ65 Task ويقارنان aggregate/blocker set مع Issue #90 و`docs/PROJECT_STATUS.md`. أي رجوع صامت إلى `READY`/`TODO` أو اختلاف blocker set يجب أن يفشل مغلقًا بدل أن يخلق خطة تنفيذ متناقضة.
