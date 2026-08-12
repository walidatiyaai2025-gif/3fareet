# 3fareet Asset Pipeline

هذا المجلد هو المسار الرسمي لكل أصول اللعبة ومراجعها.

## Structure
- `00_reference/` — مراجع عامة معتمدة من مالك المشروع.
- `01_vehicles/` — Cars / wheels / spoilers / vehicle materials.
- `02_tracks_environments/` — Tracks / roads / lighting / environment sets.
- `03_props_architecture/` — Egyptian architecture / shops / signs / props.
- `04_vfx_spirits/` — Drift / Nitro / Spirits / particles / power-ups.
- `05_ui_hud/` — UI / HUD / icons / menus / app icons.
- `06_audio_music/` — Engine / SFX / music / UI audio.
- `07_animation/` — Vehicle / spirit / power-up animations.
- `08_marketing_keyart/` — Loading / splash / key art / store assets.
- `09_technical_exports/` — Colliders / racing lines / respawn / spawn data.

## Mandatory coordination
`docs/MISSED_ASSETS.md` هو سجل الحجز الحي. قبل إنشاء أو دمج أي Asset يجب تحديث Owner/Status/Target path في السجل. ممنوع العمل المتوازي على نفس Asset ID دون موافقة Team Lead.

`references` ملفات مرجعية لا تستخدم كـproduction export تلقائياً. الملفات الجاهزة للدمج توضع في `exports` أو `exports_candidate` حتى اعتمادها.
