# 3Fareet — Road Language + Hero Refinement Production Spec

## Hero silhouette refinement
Goal: make the player car recognizable from 3/4 front and 3/4 rear at mobile gameplay scale without changing collider, wheelbase, or gameplay dimensions.

### Front signature
- Add two cyan spirit brows above the existing purple spirit eyes.
- Each brow: approx 0.62 x 0.06 x 0.08 Unity units.
- Left brow yaw -7°, roll -5°; right mirrored.
- Keep white headlight cores visible beneath the cyan brows.
- Do not widen the physical body or collider.

### Rear signature
- Add two thin rear spirit fins behind the rear quarter silhouette.
- Left fin: purple. Right fin: cyan.
- Approx size each: 0.10 x 0.72 x 0.48.
- Mirror roll ±14°.
- Fins must stay visually behind the spoiler and never obscure tail lights.

### Roof/spine accent
- One narrow gold spine centered behind the cabin.
- Approx size: 0.10 x 0.12 x 1.25.
- Gold remains an accent, not a dominant body color.

## Road language
Goal: improve road reading at speed without adding HUD clutter.

### Spirit rune pairs
- Place every ~8 waypoints.
- Two emissive thin slashes per marker, mirrored around road center.
- Purple/cyan alternate by sector.
- Gold reserved for the start/finish marker.
- Marker thickness should stay visually decal-like: y thickness ~0.035.
- Keep colliders removed.

### Extended rune marker
- Every second rune station may add a short rear pair to create a four-mark chevron rhythm.
- Do not fill the whole road surface; asphalt must remain the dominant visual field.

### Edge readability
- Existing roadside blades remain the outer boundary language.
- Road runes are the inner guidance language.
- Never use gold continuously along the lap; gold means hero/start/finish/premium focal moment.

## Cairo landmark sector rhythm
Current intended sequence:
1. Spirit Minaret Cluster — early sector identity.
2. Neon Dome Gate — mid-lap architectural focal point.
3. Pyramid Horizon Pair — late-lap distant silhouette.
4. Cairo Bridge Gantry — high-speed transition landmark.

## Mobile constraints
- No gameplay colliders on decorative art.
- No transparent full-screen layers required for these assets.
- Favor emission and silhouette over small surface detail.
- Hero accents must remain readable on a 6–7 inch display.

## Acceptance views
- 3/4 front Hero Car: cyan brows readable over purple eyes.
- 3/4 rear Hero Car: purple/cyan fins readable without hiding spoiler/tail lights.
- Fast straight: road rune pair readable without looking like lane clutter.
- Cairo sector shot: one dominant landmark at a time.
- Start/finish: gold remains visually unique.

## Implementation status
- Minaret landmark: implemented on PR #59.
- Dome Gate landmark: implemented on PR #59.
- Bridge Gantry landmark: implemented on PR #59.
- Pyramid Horizon code: added; Unity metadata still needs confirmation.
- Road language: spec ready; script write pending connector acceptance.
- Hero refinement: spec ready; additive script write pending connector acceptance.
