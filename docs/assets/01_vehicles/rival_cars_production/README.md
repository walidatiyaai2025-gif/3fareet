# UART-004 Rival Cars — authored production candidates

This pack replaces the three runtime primitive rival presentations with three distinct authored vehicle profiles while preserving the existing Rigidbody, WheelColliders, gameplay controllers and collision setup.

## Variants

1. **Wedge Coupe** — low wedge body, compact glasshouse, narrow rear wing and center accent.
2. **Fastback Muscle** — longer/wider body, taller fastback glasshouse, wider haunches and side aero.
3. **Compact Prototype** — shorter/lower body, compact canopy, dorsal aero treatment and twin accents.

`RIVAL_DESIGN_PROFILES.json` is the tracked source-of-truth. `RivalProductionAssetBuilder` deterministically converts those profiles into three Unity production prefabs, each with LOD0/LOD1/LOD2, explicit UV0 and normals, authored body/glass/wheel/aero/light/accent geometry, and texture-mapped materials.

Generated Unity assets live under `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/` and are intentionally ignored by Git. Android preprocessing rebuilds and validates them before packaging so a missing or invalid authored rival fails closed.

The historical `CarFactory` primitive renderers remain only to preserve physics/collider hierarchy and Editor blockout resilience. In Player runtime they are hidden before a production rival visual is attached; missing/invalid production visuals do **not** re-enable the primitive presentation.

## Acceptance state

**BLOCKED pending licensed Unity render + Android APK visual proof.** Source generation, static contracts, or a successful non-rendering CI check do not constitute UART-004 visual acceptance.
