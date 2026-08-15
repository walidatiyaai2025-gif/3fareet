# UART-004 Rival Cars — external authored 3D handoff

The production goal is three visually distinct rival vehicles while preserving the existing Rigidbody, WheelColliders, gameplay controllers and collision hierarchy.

## Art-direction profiles

`RIVAL_DESIGN_PROFILES.json` is a tracked **design/LOD-budget brief**, not a production model asset and not production provenance by itself.

1. **Wedge Coupe** — low wedge body, compact glasshouse, narrow rear wing and center accent.
2. **Fastback Muscle** — longer/wider body, taller fastback glasshouse, wider haunches and side aero.
3. **Compact Prototype** — shorter/lower body, compact canopy, dorsal aero treatment and twin accents.

## Required production source

Each accepted rival must originate from an externally authored model file imported into the Unity project. Supported source suffixes are enforced by `RivalProductionPolicy`: `.fbx`, `.obj`, `.blend`, `.glb`, or `.gltf`.

`RivalProductionAssetMetadata.sourceAssetId` must be the real Unity project asset path beginning with `Assets/` (for example `Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01.fbx`). A bare filename, docs-only reference, nonexistent path, code-generated mesh, Unity primitive body, profile-only geometry, or runtime Cube stripe/fin decoration cannot self-qualify as production art.

The Android build gate verifies that the declared source resolves through `AssetDatabase.LoadMainAssetAtPath`; a string that merely ends with a supported extension is not enough.

## Required production prefabs

The three production Resources paths are:

- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab`
- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab`
- `Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab`

These externally authored production resources are intentionally **tracked by Git**. The Rival Resources tree must not be added to `.gitignore`; otherwise the actual accepted art could never become part of an exact candidate commit.

Each prefab must contain exactly three mobile LODs, UV0, authored normals, texture-mapped material data, matching variant metadata, source version and source fingerprint, and must stay inside the enforced triangle bands.

`RivalVariantPass` hides the historical primitive presentation in Player and attaches only a production prefab that passes the validator. The existing primitive hierarchy remains Editor/dev fallback and physics scaffolding only.

`RivalProductionBuildGate` does **not** synthesize production art during Android builds. The three externally authored model-backed prefabs and their declared imported source assets must already exist and validate, otherwise the build fails closed.

## Licensed Unity staging path

`RivalProductionPrefabStager` provides an Editor-only assembly step for the three tracked source candidates. It does **not** create meshes, primitives or replacement geometry. It only:

1. loads the already-imported tracked OBJ source from `Assets/Afareet/ArtSource/Vehicles/Rivals/`;
2. instantiates that imported source while preserving the source-backed Mesh references;
3. groups imported renderers by `_LOD0`, `_LOD1` and `_LOD2` object suffix;
4. refuses renderers whose mesh does not resolve back to the exact declared OBJ source;
5. refuses missing UV0, normals or texture-mapped materials;
6. writes the corresponding tracked production prefab with a three-level `LODGroup`;
7. delegates GUID/dependency-hash capture and final prefab validation to `RivalProductionSourceBinder`.

Editor menu: `Afareet > Stage UART-004 > Stage + Bind All Rival Prefabs` (or the individual Rival 1/2/3 actions).

This staging path still requires a licensed Unity Editor to import the OBJ/MTL/PNG dependencies, create stable Unity `.meta` provenance and execute the binder. Static repository presence of the stager does not promote the assets to accepted Production Art.

## Repository inventory at the current remediation head

Three distinct static OBJ source **candidates** now exist under `Assets/Afareet/ArtSource/Vehicles/Rivals/`, each carrying LOD0/LOD1/LOD2 geometry, UV0, explicit normals, MTL material mappings and a tracked PNG base-color texture. Their exact source inventory and hashes are recorded in `SOURCE_CANDIDATES.json`.

These source files are **not accepted Production Art yet**. The required Rival production prefabs have not yet been staged/bound by a licensed Unity Editor on the exact candidate, stable Unity source GUID/dependency provenance has not been captured, and no Android runtime visual proof or owner acceptance exists.

## Acceptance state

**BLOCKED.** UART-004 cannot leave BLOCKED until the three source-backed production prefabs exist and bind successfully, licensed Unity import/compile/render succeeds, an exact Android build shows all three rivals in-race with no primitive fallback, and owner/Art Director visual review explicitly accepts them.
