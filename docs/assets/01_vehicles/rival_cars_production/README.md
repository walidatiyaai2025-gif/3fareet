# UART-004 Rival Cars — external authored 3D handoff

The production goal is three visually distinct rival vehicles while preserving the existing Rigidbody, WheelColliders, gameplay controllers and collision hierarchy.

## Art-direction profiles

`RIVAL_DESIGN_PROFILES.json` is a tracked **design/LOD-budget brief**, not a production model asset and not production provenance by itself.

1. **Wedge Coupe** — low wedge body, compact glasshouse, narrow rear wing and center accent.
2. **Fastback Muscle** — longer/wider body, taller fastback glasshouse, wider haunches and side aero.
3. **Compact Prototype** — shorter/lower body, compact canopy, dorsal aero treatment and twin accents.

## Required production source authority

Each accepted rival must originate from an externally authored model file imported into the Unity project. Supported source suffixes are enforced by `RivalProductionPolicy`: `.fbx`, `.obj`, `.blend`, `.glb`, or `.gltf`.

Production source authority is isolated to:

`Assets/Afareet/ArtSource/Vehicles/Rivals/Production/`

Review, preview, refinement, blockout, generated and review-packaging paths are not production authority and cannot be promoted by metadata alone.

The deterministic licensed-staging exchange currently consumes exactly these three tracked OBJ files:

- `Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj`
- `Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj`
- `Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj`

The artist may retain editable Blender/FBX source outside this deterministic exchange, but the three OBJ files above are the exact source paths consumed by the current automated staging contract. Each must have its Unity `.meta` committed before licensed staging begins.

`RivalProductionAssetMetadata.sourceAssetId` must resolve to the real accepted Unity production source under the isolated production root. A bare filename, docs-only reference, nonexistent path, code-generated mesh, review candidate, Unity primitive body, profile-only geometry, or runtime Cube stripe/fin decoration cannot self-qualify as production art.

The Android build gate verifies that the declared source resolves through `AssetDatabase.LoadMainAssetAtPath`, has stable GUID/dependency provenance, and that every production LOD mesh remains backed by that same accepted source asset. A string that merely ends with a supported extension is not enough.

## Required production prefabs

The three production Resources paths are:

- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab`
- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab`
- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab`

These externally authored production resources are intentionally **tracked by Git**. Neither the production Resources tree nor the isolated production source root may be hidden by `.gitignore`; otherwise the accepted art could never become part of an exact candidate commit.

Each prefab must contain exactly three mobile LODs, UV0, authored normals, texture-mapped material data, matching variant metadata, source version and source fingerprint, and must stay inside the enforced triangle bands.

`RivalVariantPass` hides the historical primitive presentation in Player and attaches only a production prefab that passes the validator. The existing primitive hierarchy remains Editor/dev fallback and physics scaffolding only.

`RivalProductionBuildGate` does **not** synthesize production art during Android builds. The three externally authored model-backed prefabs and their declared imported production source assets must already exist and validate, otherwise the build fails closed.

## Licensed Unity staging path

`RivalProductionPrefabStager` provides an Editor-only assembly step for the three isolated production exchange files. It does **not** create meshes, primitives or replacement geometry.

Before the first prefab mutation, `ValidateAllSourcesBeforeMutation()` requires all three production OBJ files to be accepted by `RivalProductionPolicy`, imported by Unity and backed by Unity GUIDs. The higher-level P1 licensed staging handoff runs this Rival preflight before Cairo/world staging begins.

After that read-only preflight, the stager:

1. loads the exact production OBJ source from `Assets/Afareet/ArtSource/Vehicles/Rivals/Production/`;
2. instantiates that imported source while preserving source-backed Mesh references;
3. groups imported renderers by `_LOD0`, `_LOD1` and `_LOD2` object suffix;
4. refuses renderers whose mesh does not resolve back to the exact declared production OBJ source;
5. refuses missing UV0, normals or texture-mapped materials;
6. writes the corresponding tracked production prefab with a three-level `LODGroup`;
7. delegates GUID/dependency-hash capture and final prefab validation to `RivalProductionSourceBinder`.

Editor menu: `Afareet > Stage UART-004 > Stage + Bind All Rival Prefabs` (or the individual Rival 1/2/3 actions).

The Windows licensed-staging wrapper additionally requires the three production OBJ files **and their `.meta` files** to be non-empty and tracked in the clean starting commit before Unity is launched. This prevents a missing source package from being discovered only after Unity import or partial staging mutation begins.

This staging path still requires a licensed Unity Editor to import the production source dependencies, validate the production prefabs and execute the binder. Static repository presence of the policy/stager does not promote any asset to accepted Production Art.

## Review candidates are not production inputs

The older static OBJ candidates under `Assets/Afareet/ArtSource/Vehicles/Rivals/` remain review/reference material only. Their inventory can still be useful for engineering comparison, but they do not satisfy the isolated production source root and cannot make UART-004 source-ready.

The actual production exchange files under `/Rivals/Production/` are intentionally absent until genuine externally-authored production Rival art is delivered. Readiness must remain BLOCKED while those files are absent.

## Acceptance state

**BLOCKED.** UART-004 cannot leave BLOCKED until the three isolated source-backed production prefabs exist and bind successfully, licensed Unity import/compile/render succeeds, an exact Android build shows all three rivals in-race with no primitive fallback, and owner/Art Director visual review explicitly accepts them.
