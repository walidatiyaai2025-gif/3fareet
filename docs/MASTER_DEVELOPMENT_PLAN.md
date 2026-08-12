# عفاريت الأسفلت — Master Development Plan

**Document:** AFA-PLAN-001  
**Version:** 1.0 Baseline  
**Date:** 2026-08-12  
**Status:** Controlled team reference

> قاعدة التحكم: أي تعديل في Scope أو Architecture أو ترتيب الأولويات يجب أن يدخل الخطة وسجل المهام قبل بدء التنفيذ.

## Product vision
- 3D Casual Arcade Racing بطابع مصري فانتازي.
- Core loop: Drive → Drift → Spirit Charge → Nitro Spirit → Overtake/Attack → Reward.
- Offline Career + Time Trial + Challenges + Bosses.
- Online Real-time PvP لأربعة لاعبين.
- Garage + customization + seasons + Asphalt Pass.
- Target: 60 FPS على الأجهزة المستهدفة مع quality tiers.

## P1 — Mandatory Playable Prototype Gate
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
- Release Android build يعمل على جهاز حقيقي.
- آخر APK اجتاز التحقق يوضع في `Last verified APK released/`.

## Phases

### P0 — تأسيس المشروع ونظام الفريق
تثبيت هيكل المستودع، قواعد الفروع، تعريف Done/Verified، ونظام حجز المهام.
- هيكل Repository واضح
- وثائق Architecture/Tasks/Assets
- CI skeleton
- مجلد Last verified APK released

### P1 — Prototype قابل للعب - أعلى أولوية
إثبات أن القيادة ممتعة قبل أي Backend أو متجر أو Online.
- سيارة واحدة قابلة للقيادة
- حلبة مصرية Fantasy واحدة
- لفة واحدة
- Drift + Nitro Spirit
- 1-3 AI
- HUD أساسي
- APK Android Verified

### P2 — Driving & Racing Core
تحويل البروتوتايب إلى نواة سباق مرنة وقابلة للتوسع.
- Vehicle configuration
- Checkpoints/Laps/Positions
- Camera states
- Collision/Reset
- Race lifecycle

### P3 — Magic Gameplay & Power-ups
تثبيت هوية Magic Drift والفوضى التكتيكية.
- Magic Meter
- Nitro Spirit tiers
- أول 5 Power-ups
- VFX/Audio hooks

### P4 — Offline AI & Career
بناء رحلة الشوارع والتحديات بدون الاعتماد على الشبكة.
- AI personalities
- Career chapters
- Time Trial
- Elimination
- Boss races

### P5 — Garage & Customization
إنشاء الكراج وتخصيص السيارات بطريقة Data-driven.
- Car catalog
- Paint/Wheels/Trails
- Stats
- Unlocks
- Local persistence

### P6 — Backend Foundation
فصل خدمات الحساب والاقتصاد والملف الشخصي عن عميل اللعبة.
- Auth/Profile
- Inventory
- Economy
- Remote Config
- Telemetry contracts

### P7 — Real-time Multiplayer
سباق PvP لحظي لأربعة لاعبين ببنية Authoritative.
- Lobby/Matchmaking
- Server state
- Prediction/Reconciliation
- Reconnect
- Result validation

### P8 — League, Seasons & Asphalt Pass
نظام تنافسي أسبوعي ومواسم ومكافآت.
- Ranks
- Leaderboards
- Season reset
- Asphalt Pass
- Reward claims

### P9 — Monetization
إعلانات مكافأة ومشتريات بدون كسر اقتصاد اللعبة.
- Rewarded Ads
- IAP
- Store rules
- Purchase restore
- Fraud guards

### P10 — Admin & LiveOps
لوحة تحكم لتعديل اللعبة بدون إصدار APK جديد قدر الإمكان.
- Player ops
- Economy config
- Track rotation
- Events
- Bans
- Dashboards

### P11 — Performance, QA & Device Matrix
تثبيت 60 FPS المستهدف مع fallback آمن للأجهزة الأضعف.
- Performance budgets
- LOD/VFX tiers
- Crash/ANR
- Regression tests
- Device tiers

### P12 — Beta & Production Release
تحويل اللعبة من مشروع تطوير إلى منتج قابل للنشر والتحديث.
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

## Task states
`TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`

## Last verified APK released policy
- لا Debug APK.
- آخر APK Verified فقط.
- اسم مقترح: `afareet-v0.1.0-prototype-verified.apk`.
- metadata: commit SHA, build date, device/API, tester, smoke result, SHA-256.
- إذا لا توجد نسخة Verified، لا يتم وضع APK وهمي.

## Architecture risk gate
المشروع يبدأ بـFlutter + Flame حسب الرؤية الحالية. P1 يجب أن يثبت عمليًا أن متطلبات القيادة/الكاميرا/عرض الأصول/الـVFX والأداء قابلة للتنفيذ بصورة مستقرة. إذا فشل الـGate، يرفع ADR قبل أي توسع.

## Source of truth
- `docs/MASTER_DEVELOPMENT_PLAN.md`
- `docs/TASK_REGISTER.md`
- `docs/MISSED_ASSETS.md`
