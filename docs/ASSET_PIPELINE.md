# 3fareet Asset Pipeline

## ART-001 — Folder structure

All production-ready assets live under `assets/` and are grouped by domain:

- `assets/cars/` — vehicle meshes, textures, preview renders and metadata.
- `assets/tracks/` — track scenes, collision data and environment bundles.
- `assets/environment/` — reusable Cairo/Egypt props and set dressing.
- `assets/vfx/` — drift, nitro, power-up and ambient effects.
- `assets/audio/music/` — music stems and mixes.
- `assets/audio/sfx/` — engine, drift, UI and gameplay SFX.
- `assets/ui/` — icons, backgrounds and non-generated UI art.
- `assets/placeholders/` — temporary assets only; never promoted silently.

Source/DCC files should stay separate from runtime-optimized exports when both exist. Runtime code must reference only packaged runtime assets.

## ART-002 — Naming convention

Use lowercase snake_case ASCII filenames:

`<domain>_<subject>_<variant>_<lod-or-resolution>.<ext>`

Examples:

- `car_street_runner_body_lod0.glb`
- `car_street_runner_albedo_2k.webp`
- `track_cairo_corniche_collision_v01.json`
- `vfx_nitro_spirit_trail_01.webp`
- `sfx_engine_street_runner_loop_01.ogg`

Rules:

1. No spaces, Arabic filenames, dates, `final`, `new`, or ambiguous suffixes.
2. Increment explicit revision suffixes only when multiple runtime variants must coexist.
3. Keep logical IDs stable even when binary files are replaced.
4. Placeholder files must contain `_placeholder_` in the filename.
5. Asset paths are case-sensitive and must match `pubspec.yaml` exactly.
