# 3fareet Asset Pipeline

هذا المجلد هو المسار الرسمي لكل أصول اللعبة ومراجعها.

قواعد الـ3D naming/import المعتمدة موثقة في [`../ASSET_PIPELINE.md`](../ASSET_PIPELINE.md)، والعقد القابل للقراءة آليًا موجود في [`UNITY_ASSET_CONVENTION.json`](UNITY_ASSET_CONVENTION.json).

## Structure

- `00_reference/` — مراجع عامة معتمدة من مالك المشروع؛ لا تُرقّى تلقائيًا إلى Runtime.
- `01_vehicles/` — Cars / wheels / spoilers / vehicle materials.
- `02_tracks_environments/` — Tracks / roads / lighting / environment sets.
- `03_props_architecture/` — Egyptian architecture / shops / signs / props.
- `04_vfx_spirits/` — Drift / Nitro / Spirits / particles / power-ups.
- `05_ui_hud/` — UI / HUD / icons / menus / app icons.
- `06_audio_music/` — Engine / SFX / music / UI audio.
- `07_animation/` — Vehicle / spirit / power-up animations.
- `08_marketing_keyart/` — Loading / splash / key art / store assets.
- `09_technical_exports/` — Colliders / racing lines / respawn / spawn data.

## Standard lifecycle inside a category

عند انطباق المسار على نوع الأصل، استخدم هذه المراحل ولا تنقل الملفات مباشرة من reference إلى Unity:

- `references/` — صور/فيديو/مراجع فقط.
- `source/` — ملفات DCC القابلة للتعديل أو manifest يشير إلى source storage خارجي.
- `exports_candidate/` — export جاهز تقنيًا لكن ما زال ينتظر review.
- `exports/` — export معتمد وجاهز للإدخال إلى `unity_game/Assets/Afareet/Art/<Category>/` بواسطة المهمة المالكة لمسار Unity.

وجود ملف داخل `exports/` لا يعني أن الـAPK أصبحت Verified؛ إدخاله إلى اللعبة، Visual Gate، الأداء، واختبار الجهاز مراحل منفصلة.

## Mandatory coordination

`docs/MISSED_ASSETS.md` هو سجل الحجز الحي للأصول الفردية. قبل إنشاء أو دمج أي Asset يجب تحديث Owner/Status/Target path في السجل. ممنوع العمل المتوازي على نفس Asset ID دون موافقة Team Lead.

`references` ملفات مرجعية لا تستخدم كـproduction export تلقائياً. الملفات الجاهزة للمراجعة توضع في `exports_candidate`، وبعد الاعتماد فقط تنتقل إلى `exports`.

## Machine-readable validation contract

`UNITY_ASSET_CONVENTION.json` يثبت نفس القواعد بشكل يمكن لـUnity Editor/CI validator استهلاكه لاحقًا: axes/scale، source-to-runtime category mapping، naming regexes، texture suffixes، LOD suffixes، import defaults، والmetadata المطلوبة.

لا تعدّل الـJSON وحده أو الوثيقة وحدها؛ أي تغيير في العقد يجب أن يحافظ على تطابق الاثنين في نفس PR.
