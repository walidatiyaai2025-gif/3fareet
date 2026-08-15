# عفاريت الأسفلت — Master Development Plan

**Document:** AFA-PLAN-001  
**Version:** 2.0 Unity Production Baseline
**Date:** 2026-08-13
**Status:** Controlled team reference

> قاعدة التحكم: أي تعديل في Scope أو Architecture أو ترتيب الأولويات أو Art Direction يجب أن يدخل الخطة وسجل المهام قبل بدء التنفيذ.

## Project Status Dashboard — Mandatory

الصفحة التنفيذية الرسمية لمعرفة الوضع الحالي للمشروع هي [`docs/PROJECT_STATUS.md`](PROJECT_STATUS.md).

**قاعدة إلزامية للفريق:** أي PR يغير حالة Task أو Phase أو Milestone أو Blocker/Risk أو Asset مؤثر أو Build/Release أو محتوى `Last verified APK released/` يجب أن يحدّث `PROJECT_STATUS.md` في **نفس PR**. تحديث صفحة الحالة جزء من Definition of Done وليس عملاً مؤجلاً بعد الدمج.

يجب أن تعرض الصفحة دائمًا:
- Overall project status بالألوان.
- Current phase ودرجة جاهزيتها.
- ما تم وما يجري وما هو Blocked/Deferred.
- أعلى الأولويات التالية.
- Active blockers/risks.
- حالة آخر Verified APK وEvidence المطلوبة.
- روابط مباشرة إلى Master Plan وTask Register وMissed Assets وArt Direction.

إذا تعارضت صفحة الحالة مع ملفات المهام التفصيلية، يجب إصلاح التعارض في نفس PR قبل الدمج، ولا يجوز رفع نسبة تقدم أو إعلان `VERIFIED` بدون Evidence.

## Product vision
- 3D Casual Arcade Racing بطابع مصري فانتازي.
- **Premium Neon Egyptian Fantasy Racing** هو الاتجاه البصري الإلزامي.
- Core loop: Drive → Drift → Spirit Charge → Nitro Spirit → Overtake/Attack → Reward.
- Offline Career + Time Trial + Challenges + Bosses.
- Online Real-time PvP لأربعة لاعبين.
- Garage + customization + seasons + Asphalt Pass.
- Target: 60 FPS على الأجهزة المستهدفة مع quality tiers.

## Locked client architecture — 2026-08-13

- **Production game client:** Unity `6000.5.8f1` داخل `unity_game/`.
- **Rendering target:** Mobile-first stylized 3D؛ يبدأ Built-in pipeline الحالي كـVertical Slice ثم يحسم `UART-008` قرار URP وترحيله قبل التوسع الفني.
- **Flutter/Flame:** Legacy mechanics/UI prototype في الجذر. يبقى قابلًا للبناء للاختبارات والمقارنة، لكن لا يستقبل Features إنتاجية جديدة دون Task `FLT-*`.
- **Source migration rule:** تنقل القواعد والـConfig المقصودة إلى Unity بإعادة تنفيذ واختبارات؛ لا يعتمد Unity Runtime على Dart/Flutter.
- **Artifact identity:** `afareet-unity3d-*` للمنتج و`afareet-flutter-*` للمرجع.
- **Active task source:** [`tasks/06-UNITY-3D-MIGRATION.md`](tasks/06-UNITY-3D-MIGRATION.md).

هذا القرار يحل محل Architecture Risk Gate القديم الذي افترض Flutter كعميل نهائي. أي عكس للقرار يحتاج ADR وموافقة Product Owner + Unity Tech Lead.

## Mandatory Art Direction
المرجع التفصيلي موجود في [`docs/ART_DIRECTION.md`](ART_DIRECTION.md). الصور المرجعية المقدمة من مالك المشروع أصبحت Visual Constitution للمشروع.

**ملخص الهوية:**
- Cairo night/sunset cinematic fantasy.
- Midnight Navy/Black + Cyan/Turquoise Neon + Warm Gold/Amber.
- Stylized 3D cars بstance قوي وخامات لامعة وإضاءة rim/under-glow.
- Dark premium glass/metal UI مع cyan outlines وgold accents.
- Signature Drift/Nitro VFX من أول Prototype.
- Main Menu وGarage وRace HUD يجب أن تبدو كلعبة Premium وليست Flutter template.

**قاعدة:** إذا نجح الأداء والكود لكن الشكل بعيد عن `ART_DIRECTION.md`، تبقى المرحلة `DONE` وليست `VERIFIED`.

## Locked backend architecture

القرار المعماري المعتمد موثق في [`docs/BACKEND_ARCHITECTURE.md`](BACKEND_ARCHITECTURE.md):

- Game client: **Unity 3D production client**؛ Flutter legacy prototype ليس عميل الإنتاج.
- Backend/API: **Laravel**.
- Primary database: **MySQL**.
- مسار البيانات الإلزامي: `Unity Game Client → HTTPS API → Laravel → MySQL`.
- ممنوع اتصال أي Client مباشرة بقاعدة MySQL أو تضمين credentials قاعدة البيانات داخل التطبيق.
- Laravel مسؤول عن auth/authorization/validation/business rules/economy validation/persistence/audit/rate limiting.
- التنفيذ الكبير للـBackend يظل مؤجلًا حتى نجاح P1، لكن هذا القرار Locked من الآن لمنع أي coupling خاطئ في كود اللعبة.

## U-P1 — Mandatory Unity 3D Vertical Slice Gate
أعلى أولوية في المشروع. لا يبدأ التوسع الكبير في Backend/Store/Online قبل نجاح هذا الـGate.

**Required:**
- سيارة واحدة قابلة للقيادة.
- حلبة مصرية Fantasy واحدة، لفة واحدة.
- Start/Finish + checkpoints + reset.
- Arcade steering/braking/traction/drift.
- Magic Drift Meter + Nitro Spirit.
- Racing camera.
- 1 AI على الأقل والهدف 3.
- HUD: position/speed/spirit/timer.
- Premium visual skin مطابق للمرجع البصري.
- Cairo fantasy lighting/look-dev ظاهر داخل الـPrototype.
- Release Android build يعمل على جهاز حقيقي.
- آخر APK اجتاز التحقق يوضع في `Last verified APK released/`.
- Build صادر من `unity_game/` باسم `afareet-unity3d-*`؛ Flutter APK لا يغلق هذا الـGate.

## Phases

### P0 — تأسيس المشروع ونظام الفريق — DONE/MAINTAINED
- هيكل Repository واضح
- وثائق Architecture/Tasks/Assets/Art Direction
- CI skeleton
- مجلد Last verified APK released
- Project Status Dashboard + freshness enforcement

### U-P1 — Unity 3D Vertical Slice — أعلى أولوية حاليًا
- سيارة واحدة قابلة للقيادة
- حلبة مصرية Fantasy واحدة
- لفة واحدة
- Drift + Nitro Spirit
- 1-3 AI
- Premium HUD وvisual identity
- APK Android Verified

### FLT-L — Flutter Legacy Reference
- يبقى Build/Analyze/Test صالحًا.
- لا Features إنتاجية مزدوجة.
- يستخدم كمرجع للـRules والـTelemetry والـFlow أثناء Migration.
- كل عمل جديد يحتاج Task `FLT-*` ومبررًا واضحًا.

### P2 — Driving & Racing Core
- Vehicle configuration
- Checkpoints/Laps/Positions
- Camera states
- Collision/Reset
- Race lifecycle

### P3 — Magic Gameplay & Power-ups
- Magic Meter
- Nitro Spirit tiers
- أول 5 Power-ups
- VFX/Audio hooks

### P4 — Offline AI & Career
- AI personalities
- Career chapters
- Time Trial
- Elimination
- Boss races

### P5 — Garage & Customization
- Premium dark showroom presentation
- Car catalog
- Paint/Wheels/Trails
- Stats
- Unlocks
- Local persistence

### P6 — Backend Foundation — Laravel + MySQL
- Laravel versioned REST API (`/api/v1` baseline).
- MySQL as primary relational data store.
- Auth/Profile.
- Inventory/Garage.
- Economy ledger/rewards.
- Remote Config/feature flags.
- Telemetry contracts.
- Rate limiting/audit logging.
- Separate local/staging/production environments and secrets.
- No direct Flutter-to-MySQL connectivity.

### P7 — Real-time Multiplayer
- Lobby/Matchmaking
- Server state
- Prediction/Reconciliation
- Reconnect
- Result validation

### P8 — League, Seasons & Asphalt Pass
- Ranks
- Leaderboards
- Season reset
- Asphalt Pass
- Reward claims

### P9 — Monetization
- Rewarded Ads
- IAP
- Store rules
- Purchase restore
- Fraud guards

### P10 — Admin & LiveOps
- Player ops
- Economy config
- Track rotation
- Events
- Bans
- Dashboards

### P11 — Performance, QA & Device Matrix
- Performance budgets
- LOD/VFX tiers
- Visual quality tiers Low/Medium/High
- Crash/ANR
- Regression tests
- Device tiers

### P12 — Beta & Production Release
- Closed beta
- Store assets
- Release signing
- Rollout
- Monitoring
- Rollback

## Team coordination rules
- كل عمل له Task ID.
- Branch: `feature/<TASK-ID>-short-name` أو `fix/<TASK-ID>-short-name`.
- Owner واحد لكل Task.
- Module Lock عند لمس نفس الملفات الجوهرية.
- PR صغير ومحدد؛ لا تجمع Epics متعددة.
- `VERIFIED` تحتاج Evidence ولا تساوي `DONE`.
- أي interface مشتركة تُعدل في Task مستقلة أولًا.
- VIS tasks إلزامية للـPrototype ولا تعتبر polish اختياريًا.
- أي تغير فعلي في وضع المشروع يجب أن ينعكس في `docs/PROJECT_STATUS.md` داخل نفس PR.
- كل مساهم جديد يبدأ من [`ONBOARDING.md`](ONBOARDING.md) ويلتزم بـ[`TEAM_WORKFLOW.md`](TEAM_WORKFLOW.md).
- Active locks تسجل في [`MODULE_OWNERSHIP.md`](MODULE_OWNERSHIP.md) قبل `IN PROGRESS`.

## Task states
`TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`

## Last verified APK released policy
- لا Debug APK.
- آخر APK Verified فقط.
- اسم مقترح: `afareet-v0.1.0-prototype-verified.apk`.
- metadata: commit SHA, build date, device/API, tester, smoke result, SHA-256.
- إذا لا توجد نسخة Verified، لا يتم وضع APK وهمي.

## Architecture decision record
تم حسم الـArchitecture Risk Gate لصالح Unity 3D في 2026-08-13. مشروع Flutter أصبح Legacy Reference. يجب أن يثبت U-P1 عمليًا القيادة والكاميرا والأصول والـVFX والأداء على Android قبل التوسع في Career/Backend/Online.

قرار Backend مستقل ومقفل: Laravel + MySQL خلف HTTPS API؛ لا يتم استخدام MySQL كواجهة مباشرة للعميل تحت أي ظرف.

## Source of truth
- `docs/PROJECT_STATUS.md` — Executive current-state dashboard
- `docs/MASTER_DEVELOPMENT_PLAN.md`
- `docs/BACKEND_ARCHITECTURE.md`
- `docs/ART_DIRECTION.md`
- `docs/TASK_REGISTER.md`
- `docs/tasks/06-UNITY-3D-MIGRATION.md`
- `docs/ONBOARDING.md`
- `docs/TEAM_WORKFLOW.md`
- `docs/MODULE_OWNERSHIP.md`
- `docs/MISSED_ASSETS.md`
