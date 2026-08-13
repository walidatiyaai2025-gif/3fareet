# Missed Assetes

**Document:** AFA-ASSETS-001  
**Version:** 1.1 Live Registry

هذا السجل هو **مصدر الحقيقة الحي** للأصول الناقصة/المستلمة/قيد التنفيذ. لا يبتكر المصمم أو المبرمج أصلًا بديلًا غير مسجل، ولا يبدأ أي شخص أصلًا عليه Owner نشط بدون تنسيق Team Lead.

- P0: يمنع Prototype أو تقييم الأداء/القيادة.
- P1: مطلوب لـCore Alpha/الهوية.
- P2: توسع لاحق.
- Placeholder مسموح فقط إذا كان مسجلًا وواضحًا.

## Asset coordination — mandatory

**قبل بدء أي Asset أو تعديل Asset قائم:** راجع هذا الملف أولًا، ثم سجّل الـOwner والحالة والـTarget path والـBranch/PR. أي PR يضيف أو يعدل أو يدمج Asset يجب أن يحدث هذا الملف في **نفس الـPR**.

الحالات: `MISSING → CLAIMED → IN_PROGRESS → REVIEW → READY → INTEGRATING → INTEGRATED → VERIFIED`.

الحالات `CLAIMED / IN_PROGRESS / REVIEW / READY / INTEGRATING` تعتبر **Asset Lock**: ممنوع شخص ثانٍ يبدأ نسخة موازية من نفس الأصل بدون موافقة Team Lead موثقة هنا.

- المصمم: `MISSING → CLAIMED → IN_PROGRESS → REVIEW → READY`.
- المبرمج: `READY → INTEGRATING → INTEGRATED`.
- Team Lead / QA: `INTEGRATED → VERIFIED` بعد التحقق.
- ملفات `references/` للمرجع فقط؛ المصدر القابل للتعديل في `source/`، والنسخة الجاهزة للدمج في `exports/`.
- ملف Word هو مرجع إداري؛ هذا الملف Markdown هو السجل الذي يحدّثه الفريق أثناء العمل لتجنب Binary merge conflicts.

## Active asset claims / received references

| AST-ID | Asset | Owner | Status | Target path | Branch / PR | Last update | Notes |
|---|---|---|---|---|---|---|---|
| AST-060 | App icon family | UNASSIGNED | REFERENCE_AVAILABLE | `docs/assets/05_ui_hud/app_icons/` | — | 2026-08-12 | استلمنا لوحة المقاسات/التصميم. المرجع محفوظ، ويوجد 256px candidate للمعاينة فقط. ما زال مطلوب clean 1024 master + platform exports قبل VERIFIED. |
| AST-061 | Cairo Rap × Shaabi prototype music | Technical Audio / Integration | INTEGRATING | `assets/audio/` | `agent/audio-002-first-apk-rap-shaabi` | 2026-08-12 | Owner source received; first APK uses a short embedded preview loop. CI + real-device audio smoke required before VERIFIED. |
| AST-062 | Afareet black/purple/gold hero car | Principal Mobile Game Architect | REVIEW | `docs/assets/01_vehicles/references/`, procedural Unity blockout in `CarFactory` | `agent/unity-3d-prototype` / PR #49 | 2026-08-13 | User reference received. Procedural hero blockout integrated; production mesh, UVs and LODs remain required for UART-003. |

## Missing / production asset register

| Category | Asset | Priority | Phase | Required spec | Allowed placeholder | Status |
|---|---|---|---|---|---|---|
| Vehicle 3D | Prototype Hero Car | P0 | P1 | GLB/GLTF أو format معتمد، stylized Egyptian sedan، LODs/Collider/Pivot صحيح | black/purple/gold procedural hero blockout | REVIEW |
| Vehicle 3D | Shahin-inspired fictional car | P1 | Garage/Alpha | Stylized non-infringing design + LOD0/1/2 + collider | Prototype Hero Car | MISSING |
| Vehicle 3D | Ritmo-inspired fictional car | P1 | Garage/Alpha | نفس pipeline مع silhouette مختلف | Placeholder car | MISSING |
| Vehicle 3D | Microbus-inspired fictional vehicle | P1 | Garage/Alpha | High silhouette، collision footprint مضبوط | Placeholder box vehicle | MISSING |
| Vehicle 3D | 128-inspired fictional car | P1 | Garage/Alpha | Stylized proportions + optimized materials | Placeholder car | MISSING |
| Track | Cairo Fantasy Prototype Track | P0 | P1 | مسار مغلق، Start/Finish، checkpoints، safe respawn، collision | Greybox track مسموح حتى gameplay gate | MISSING |
| Track | Corniche magical environment set | P1 | Core Alpha | Road modules + Nile/rails/buildings + night neon palette | Prototype environment | MISSING |
| Track | Downtown Cairo fantasy set | P2 | Career | Road + façades + signage + props | Blockout | MISSING |
| Track | Khan El-Khalili fantasy set | P2 | Career | Narrow streets + arches + lanterns + props | Blockout | MISSING |
| Track | Ring Road supernatural set | P2 | Career | Highway modules + billboards + ramps | Blockout | MISSING |
| Track | Pyramids Midnight set | P2 | Career | Desert/roads/pyramids-inspired fantasy silhouettes | Blockout | MISSING |
| Environment Props | Egyptian road barriers pack | P0 | P1 | Optimized static meshes + colliders | Primitive cubes | MISSING |
| Environment Props | Street lights neon pack | P1 | P1/Core | Emissive-friendly materials | Simple poles | MISSING |
| Environment Props | Arabic fantasy signage pack | P1 | P1/Core | Original fictional signs، atlased textures | Generic shapes | MISSING |
| Environment Props | Traffic cones/crates/barrels | P1 | P1/Core | Low poly + collision variants | Primitives | MISSING |
| Environment Props | Magical graffiti decals | P1 | Core Alpha | Atlas + emissive masks | None | MISSING |
| Environment Props | Floating coin pickup model | P1 | P1/Core | Readable silhouette + spin animation | 2D icon/billboard | MISSING |
| VFX | Tire smoke | P0 | P1 | Pooled particles، low/med/high profiles | Simple particles | MISSING |
| VFX | Drift sparks | P0 | P1 | Wheel-anchor compatible، pooled | Simple sparks | MISSING |
| VFX | Magic Drift aura L1-L3 | P0 | P1 | 3 intensity tiers، color/alpha scalable | Colored trail | MISSING |
| VFX | Nitro Spirit apparition | P0 | P1 | Signature spirit effect، optimized mobile | Trail + glow placeholder | MISSING |
| VFX | Nitro speed streaks | P0 | P1 | Camera/vehicle speed linked | Simple lines | MISSING |
| VFX | Crash impact burst | P1 | Core Alpha | Directional burst + debris budget | Simple flash | MISSING |
| VFX | Shield - عين الحصودة | P1 | Power-ups | Readable shield bubble/eye motif | Sphere overlay | MISSING |
| VFX | Asphalt shard trap | P1 | Power-ups | Ground hazard + impact feedback | Spike primitives | MISSING |
| VFX | Traffic curse slow effect | P1 | Power-ups | Opponent debuff readable at distance | Color tint | MISSING |
| VFX | Coin multiplier effect | P1 | Power-ups | Short celebratory feedback | Glow | MISSING |
| UI | Game logo Arabic | P1 | P1/Core | Vector/transparent + light/dark variants | Text logo | MISSING |
| UI | Prototype HUD frame | P0 | P1 | Speed/position/spirit/timer safe areas | Plain Flutter widgets | MISSING |
| UI | Drift/Spirit meter art | P0 | P1 | States empty/charge/full/nitro | Simple gradient meter | MISSING |
| UI | Position badge 1-4 | P1 | P1 | Readable mobile sizes | Text | MISSING |
| UI | Pause icons | P1 | P1 | Vector icons | System icons | MISSING |
| UI | Race result medals | P1 | Core | 1st/2nd/3rd/finish visuals | Text badges | MISSING |
| UI | Garage card frame | P2 | Garage | Reusable card + rarity/lock states | Plain card | MISSING |
| UI | Career map nodes | P2 | Career | Race/time/drift/boss node states | Basic circles | MISSING |
| UI | Rank badges Bronze→Afreet | P2 | Online | 7 ranks + locked/current variants | Text labels | MISSING |
| UI | Asphalt Pass track art | P2 | Season | Free/premium track + claim states | Plain list | MISSING |
| Audio | Prototype engine loop | P0 | P1 | Idle/low/high loop or layered loop | Synthetic placeholder | MISSING |
| Audio | Tire skid/drift | P0 | P1 | Loopable + intensity response | Synthetic placeholder | MISSING |
| Audio | Nitro activation | P0 | P1 | Distinct magic signature | Temporary whoosh | MISSING |
| Audio | Crash impacts pack | P1 | P1/Core | Light/medium/heavy | Generic impacts | MISSING |
| Audio | UI click/confirm/back | P1 | P1/Core | Consistent set | Platform clicks | MISSING |
| Audio | Countdown 3-2-1-Go | P1 | P1 | Clear and punchy | Tones | MISSING |
| Audio | Race finish sting | P1 | P1 | Short success/fail variants | Tone | MISSING |
| Audio | Cairo fantasy race music | P2 | Core Alpha | Loopable، original، energetic; Rap/Trap × Egyptian Shaabi/Mahraganat direction | Embedded Rap×Shaabi preview loop for first APK only | INTEGRATING |
| Animation | Vehicle wheel rotation rig | P0 | P1 | Compatible with car model | Code rotation if possible | MISSING |
| Animation | Suspension/body lean hooks | P1 | Core Alpha | Arcade exaggeration | Procedural only | MISSING |
| Animation | Nitro spirit animation | P1 | Core Alpha | Spawn/flight/fade | VFX-only placeholder | MISSING |
| Animation | Power-up pickup animation | P1 | Power-ups | Spawn/collect loop | Scale pulse | MISSING |
| Textures | Prototype road surface | P0 | P1 | Tileable + normal/roughness as supported | Flat color | MISSING |
| Textures | Neon asphalt magic cracks | P0 | P1 | Tile/decal variants + emissive mask | Simple emissive lines | MISSING |
| Textures | Car paint material set | P1 | Garage | Base/metallic/roughness variants | Flat material | MISSING |
| Textures | Environment atlas | P1 | Core | Mobile optimized atlas | Flat materials | MISSING |
| Technical | Collision meshes for prototype track | P0 | P1 | Simple stable collision geometry | Generated primitives | MISSING |
| Technical | AI racing line data | P0 | P1 | Waypoints/speeds/drift flags | Manual points | MISSING |
| Technical | Safe respawn markers | P0 | P1 | Per checkpoint/sector | Manual transforms | MISSING |
| Technical | Power-up spawn marker set | P1 | Power-ups | Track-integrated transforms | Manual transforms | MISSING |
| Marketing/Store | App icon | P2 | Beta | Clean 1024x1024 master + Android adaptive foreground/background + Play Store 512x512 + iOS AppIcon family; small-size readability/mask review | Received reference sheet in `docs/assets/05_ui_hud/app_icons/references/` + 256 candidate for preview only | REFERENCE_AVAILABLE |
| Marketing/Store | Feature graphic/screenshots | P2 | Beta | Store-compliant assets | None until beta | MISSING |
| Localization | Arabic game font license/selection | P1 | P1/Core | Readable Arabic + Latin numerals | Noto Sans Arabic during development | MISSING |
