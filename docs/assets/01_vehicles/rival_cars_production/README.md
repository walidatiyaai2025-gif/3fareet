# UART-004 Rival Cars — external authored 3D handoff

The production goal is three visually distinct rival vehicles while preserving the existing Rigidbody, WheelColliders, gameplay controllers and collision hierarchy.

## Art-direction profiles

`RIVAL_DESIGN_PROFILES.json` is a tracked **design/LOD-budget brief**, not a production model asset and not production provenance by itself.

1. **Wedge Coupe** — low wedge body, compact glasshouse, narrow rear wing and center accent.
2. **Fastback Muscle** — longer/wider body, taller fastback glasshouse, wider haunches and side aero.
3. **Compact Prototype** — shorter/lower body, compact canopy, dorsal aero treatment and twin accents.

## Required production source

Each accepted rival must originate from an externally authored model file referenced by production metadata. Supported source suffixes are enforced by `RivalProductionPolicy`: `.fbx`, `.obj`, `.blend`, `.glb`, or `.gltf`.

Code-generated meshes, Unity primitive bodies, profile-only geometry and runtime Cube stripe/fin decoration cannot self-qualify as production art.

The three production Resources paths are:

- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab`
- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab`
- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab`

Each prefab must contain exactly three mobile LODs, UV0, authored normals, texture-mapped material data, matching variant metadata, source version and source fingerprint, and must stay inside the enforced triangle bands.

`RivalVariantPass` hides the historical primitive presentation in Player and attaches only a production prefab that passes the validator. The existing primitive hierarchy remains Editor/dev fallback and physics scaffolding only.

`RivalProductionBuildGate` does **not** synthesize production art during Android builds. The three externally authored model-backed prefabs must already exist and validate, otherwise the build fails closed.

## Acceptance state

**BLOCKED.** UART-004 cannot leave BLOCKED until the actual external 3D models/prefabs exist, licensed Unity import/compile/render succeeds, an exact Android build shows all three rivals in-race with no primitive fallback, and owner/Art Director visual review explicitly accepts them.
