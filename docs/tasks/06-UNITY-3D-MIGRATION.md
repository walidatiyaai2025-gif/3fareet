# Unity 3D Production Task Register

**Document:** AFA-TASKS-U3D-001  
**Engine:** Unity `6000.5.8f1`  
**Product client:** `unity_game/`  
**Legacy client:** Flutter/Flame — maintenance only

هذه هي قائمة التنفيذ النشطة. لا يبدأ أي بند `TODO` قبل تحويله إلى `READY` وتحديد Owner بشري وModule Lock.

## Current milestone — U-P1 Vertical Slice

هدف المرحلة: سباق واحد كامل على Android، سيارة لاعب + 3 AI، هوية القاهرة، Drift/Nitro، HUD، صوت، وأداء مقبول على جهاز حقيقي.

### Foundation / Architecture

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| U3D-001 | P0 | إنشاء Unity project مستقل داخل المستودع | Principal Mobile Game Architect | IN REVIEW | Unity 6000.5 يفتح ويعمل |
| U3D-002 | P0 | Runtime bootstrap ومشهد Prototype | Principal Mobile Game Architect | IN REVIEW | Empty scene يولد vertical slice |
| U3D-003 | P0 | Windows build pipeline وأسماء artifacts | Principal Mobile Game Architect | IN REVIEW | `afareet-unity3d.exe` build green |
| U3D-004 | P0 | Android build pipeline وهوية package منفصلة | Principal Mobile Game Architect | IN REVIEW | build method + `com.fiftysolutions.afareetunity3d` |
| U3D-005 | P0 | App icon/branding لكل targets | Principal Mobile Game Architect | IN REVIEW | icon master + generated target icons |
| U3D-006 | P0 | إضافة asmdefs وحدود assemblies | Principal Mobile Game Architect | IN REVIEW | Core/Gameplay/UI/Editor منفصلة بلا circular refs |
| U3D-007 | P0 | Input System جديد مع keyboard/touch/gamepad | Unity Gameplay Engineer | READY | input actions + rebinding-safe abstraction |
| U3D-008 | P0 | Config عبر ScriptableObjects | Principal Mobile Game Architect | IN REVIEW | no production tuning hardcoded |
| U3D-009 | P0 | Logging/diagnostics policy | Unity Tech Lead | TODO | structured channels + release stripping |
| U3D-010 | P0 | Unity EditMode/PlayMode test assemblies | QA Automation Engineer | READY | tests execute headless in CI |
| U3D-011 | P0 | Unity CI compile + Windows artifact | DevOps / QA Engineer | READY | GitHub Actions green |
| U3D-012 | P0 | Unity Android CI artifact | DevOps / QA Engineer | BLOCKED | blocked: Android module/CI image |

### Vehicle / Camera / Feel

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UVEH-001 | P0 | Arcade Rigidbody controller baseline | Principal Mobile Game Architect | IN REVIEW | drive/brake/reverse/steer work |
| UVEH-002 | P0 | WheelCollider أو custom suspension decision ADR | Gameplay Lead | READY | measured decision + prototype |
| UVEH-003 | P0 | Grip/lateral slip/drift tuning assets | Vehicle Physics Engineer | READY | tunable profiles, no magic constants |
| UVEH-004 | P0 | Ground detection and surface types | Vehicle Physics Engineer | READY | asphalt/off-road behavior tested |
| UVEH-005 | P0 | Collision/crash response | Gameplay Engineer | TODO | stable at target speeds |
| UVEH-006 | P0 | Reset to last valid checkpoint | Gameplay Engineer | TODO | no upside-down/stuck lock |
| UVEH-007 | P0 | Nitro acceleration/consumption integration | Gameplay Engineer | READY | curve + meter + cooldown |
| UVEH-008 | P0 | Drift energy charge rules | Gameplay Engineer | READY | abuse guards + tests |
| UVEH-009 | P0 | Chase camera baseline | Principal Mobile Game Architect | IN REVIEW | follow/look/FOV nitro |
| UVEH-010 | P0 | Camera collision and obstruction | Camera Engineer | READY | no geometry clipping |
| UVEH-011 | P1 | Shake/impact/drift camera states | Camera Engineer | TODO | accessibility toggle included |
| UVEH-012 | P0 | Real-device driving feel pass | Gameplay Lead | BLOCKED | requires Android APK + device |

### Race / AI / Track

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| URAC-001 | P0 | Cairo procedural oval track baseline | Principal Mobile Game Architect | IN REVIEW | drivable loop generated |
| URAC-002 | P0 | Checkpoint volumes and ordered validation | Race Engineer | READY | skipped checkpoint rejected |
| URAC-003 | P0 | Lap/start/finish state machine | Race Engineer | READY | deterministic one-lap finish |
| URAC-004 | P0 | Ranking by checkpoint/lap/progress | Race Engineer | READY | no nearest-waypoint ranking exploit |
| URAC-005 | P0 | Countdown/results/restart flow | Race Engineer | READY | complete race lifecycle |
| URAC-006 | P0 | Track bounds/barriers/off-road | Level Designer | READY | player cannot leave playable area silently |
| URAC-007 | P0 | Waypoint AI baseline (3 rivals) | Principal Mobile Game Architect | IN REVIEW | three rivals complete loop |
| URAC-008 | P0 | AI racing line and braking zones | AI Engineer | READY | curve-aware speed planning |
| URAC-009 | P1 | AI avoidance/overtake/personality | AI Engineer | TODO | reproducible seeded behaviors |
| URAC-010 | P0 | AI stuck recovery and finish tests | AI Engineer | READY | automated coverage |
| URAC-011 | P0 | Replace blockout with Cairo vertical-slice layout | Level Designer | READY | landmarks + readable racing line |
| URAC-012 | P0 | Track completion device verification | QA Engineer | BLOCKED | requires Android APK |

### Art / VFX / UI / Audio

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UART-001 | P0 | 3D asset folder/naming/import convention | GPT-5.6 Sol (Technical Art/Unity Agent) | IN REVIEW | [`ASSET_PIPELINE.md`](../ASSET_PIPELINE.md) + [`UNITY_ASSET_CONVENTION.json`](../assets/UNITY_ASSET_CONVENTION.json); PR #52 |
| UART-002 | P0 | Player hero car blockout | Principal Mobile Game Architect | IN REVIEW | correct scale/pivot/wheels/collider |
| UART-003 | P0 | Hero car production model + LODs | Vehicle Artist | TODO | LOD/poly/texture budgets pass |
| UART-004 | P1 | Three rival color/material variants | Vehicle Artist | TODO | visually distinct + performant |
| UART-005 | P0 | Cairo modular street kit | Environment Artist | READY | source + prefabs + atlas |
| UART-006 | P0 | Pyramid/minaret/dome landmark kit | Environment Artist | READY | fantasy Egypt readable at speed |
| UART-007 | P0 | Track dressing/lighting vertical slice | Level Artist | TODO | matches Art Direction screenshots |
| UART-008 | P0 | Mobile URP materials and lighting setup | Technical Artist | READY | low/mid/high quality tiers |
| UVFX-001 | P0 | Drift smoke/spirit trail signature | VFX Artist | READY | pooled + budget documented |
| UVFX-002 | P0 | Nitro spirit burst/trail | VFX Artist | READY | readable at speed + low tier |
| UVFX-003 | P1 | Collision/boost pickup feedback | VFX Artist | TODO | pooled and profiled |
| UUI-001 | P0 | Runtime splash/loading | Principal Mobile Game Architect | IN REVIEW | supplied artwork + progress |
| UUI-002 | P0 | Production race HUD in uGUI/UI Toolkit | Unity UI Engineer | READY | pos/speed/spirit/time/safe area |
| UUI-003 | P0 | Touch controls production pass | Unity UI Engineer | READY | multi-touch, landscape, devices |
| UUI-004 | P0 | Pause/result/restart screens | Unity UI Engineer | TODO | full flow + Arabic/English |
| UUI-005 | P1 | RTL/localization framework | Unity UI Engineer | READY | Arabic shaping/font verified |
| UAUD-001 | P0 | Engine loop with RPM/speed layers | Audio Designer | READY | import settings + device listening |
| UAUD-002 | P0 | Drift/nitro/collision SFX | Audio Designer | READY | event hooks + balanced mix |
| UAUD-003 | P1 | Music integration and pause lifecycle | Audio Engineer | TODO | no duplicate players, lifecycle safe |

### Performance / Android / Release

| ID | Pri | Task | Owner | Status | Acceptance / Evidence |
|---|---|---|---|---|---|
| UPER-001 | P0 | Target device tiers and budgets | QA/Performance Lead | READY | FPS/memory/thermal targets documented |
| UPER-002 | P0 | Unity Profiler baseline capture | Performance Engineer | READY | CPU/GPU/memory report |
| UPER-003 | P0 | Object/material/mesh pooling audit | Technical Artist | TODO | allocations and draw calls reduced |
| UPER-004 | P0 | Android module + SDK/NDK/OpenJDK install | Principal Mobile Game Architect | IN REVIEW | Unity detects Android target |
| UPER-005 | P0 | First Unity Android debug APK | Principal Mobile Game Architect | IN REVIEW | APK built and package inspected |
| UPER-006 | P0 | Android device smoke matrix | QA Engineer | BLOCKED | depends UPER-005 |
| UPER-007 | P0 | Release keystore/secrets process | Release Engineer | TODO | no secrets in Git |
| UPER-008 | P0 | Unity release APK/AAB pipeline | Release Engineer | TODO | signed reproducible build |
| UPER-009 | P0 | P1 Visual Gate review | Art Director + Owner | BLOCKED | requires production assets/screenshots |
| UPER-010 | P0 | P1 Verified APK publication | QA/Release Lead | BLOCKED | all gates + SHA/device evidence |

## Flutter legacy tasks

أي إصلاح ضروري يأخذ ID `FLT-###`. الحالة الافتراضية لكل Feature جديد في Flutter هي `DEFERRED`; لا تنقل Gameplay إنتاجيًا إلى المسارين معًا. يسمح فقط بـ:

- إصلاح Build/CI يمنع استخدام المرجع.
- الحفاظ على اختبارات الميكانيك كمرجع.
- استخراج Config/Rules موثقة لنقلها إلى Unity.
- مقارنة سلوك Migration بمهمة مستقلة.

## Parallel work recommendation

يمكن تشغيل الفريق فورًا دون تعارض بهذه الحزم:

- Engineer A: `U3D-006` + `U3D-008`.
- Engineer B: `UVEH-002` + `UVEH-003`.
- Engineer C: `URAC-002` + `URAC-003`.
- AI Engineer: `URAC-008` + `URAC-010`.
- UI Engineer: `UUI-002` + `UUI-003`.
- Technical Artist: `UART-008`؛ `UART-001` حاليًا `IN REVIEW` في PR #52.
- Vehicle Artist: `UART-002`.
- Environment Artist: `UART-005` + `UART-006`.
- VFX Artist: `UVFX-001` + `UVFX-002`.
- Release Engineer: `UPER-004` ثم `UPER-005`.

كل حزمة تحتاج Owner حقيقي وBranch مستقل قبل البدء.
