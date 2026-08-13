# UART-002 — Hero Car Visual Polish

## Goal
Make the existing procedural Hero Car visually distinctive enough to judge in the Unity prototype while preserving gameplay, physics and team-owned systems.

## Art direction
- Egyptian supernatural street-racer silhouette.
- Primary body: deep Afareet purple/black.
- Secondary: hot gold aero accents.
- Signature: purple spirit-eye lighting and underglow.
- Readability from chase camera and overtake/rear angles.

## Implemented visual pass
- wider front/rear bumpers and black/gold splitter treatment;
- gold side skirts and hood stripe;
- paired supernatural hood runes;
- windshield, rear glass and side-window treatment to break up the blockout cabin;
- black grille, spirit-eye housings and bright headlight cores;
- fangs retained as the playful Afareet signature;
- stronger purple underglow and local spirit lamps;
- purple/gold rear wing with supports;
- rear tail lamps and twin exhaust treatment;
- gold rims with purple spirit hubs on all four wheels;
- thicker Hero-only spirit trails while rivals keep the lighter baseline.

## Technical boundary
This remains a procedural visual prototype, not a production-authored mesh. It intentionally keeps the existing Rigidbody, BoxCollider, ArcadeCarController and gameplay configuration unchanged. Production mesh, UVs, baked/optimized materials and LOD0/1/2 remain required before the Hero Car can become READY/VERIFIED as a final asset.

## Evidence
Implementation commit starts at `a3f29e050151da2ca53ccb5dd7b6cb6808552da2` on `agent/UART-002-hero-car-art`, branched directly from PR #49 head `4d71d2c328be64c2d84fc6400188e02b464df32b`.

## Validation truth
The code is structurally limited to visual construction inside `CarFactory.cs`. Unity compile/player render and Android APK execution still require the Unity CI/build path; no APK is called Verified until an actual build and device/visual smoke pass exist.
