# UART-003 Hero Asset Intake

This is the pre-Unity intake boundary for the real externally-authored **Afareet King** production Hero source.

It does not create, approve, stage, bind, render, or publish production art. It exists to reject malformed deliveries before the licensed Unity handoff.

## Required source location

Place the real source under:

`unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/`

Accepted source suffixes remain aligned with the Unity production metadata contract:

- `.obj`
- `.fbx`
- `.glb`
- `.gltf`
- `.blend`

Production sources must not live under path segments named `Generated`, `Preview`, `Blockout`, or `Rivals`.

The source must be tracked by Git before authoritative intake.

## OBJ delivery contract

OBJ is the only format this standard-library preflight can structurally inspect without pretending to be Unity or a DCC package.

A production OBJ must expose exactly one object/group ending in each of:

- `_LOD0`
- `_LOD1`
- `_LOD2`

Every face vertex in all three LODs must carry:

- UV0 (`vt`)
- authored normal (`vn`)

Every LOD must use a material. The OBJ must reference at least one `.mtl`, and the MTL chain must contain a base-color texture mapping (`map_Kd`, `map_BaseColor`, or `map_Base_Color`). Referenced MTL and texture files must exist and be tracked by Git.

Geometry budgets are read from the authoritative `HeroCarLodPolicy.cs` rather than copied into this validator. Current policy is range-based and requires decreasing detail from LOD0 to LOD2.

Run:

```bash
python3 tools/android/validate_hero_asset_intake.py \
  --source unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/<hero-source>.obj \
  --output artifacts/uart003-hero-intake.json
```

A structurally clean OBJ reports:

`READY_FOR_LICENSED_UNITY_IMPORT`

This is **not** production-art acceptance and keeps `verified=false` / `productionArtApproved=false`.

## FBX / GLB / GLTF / BLEND deliveries

The script checks source location, forbidden paths, supported suffix and Git tracking, then deliberately reports:

`UNITY_INSPECTION_REQUIRED`

It does not infer mesh topology, UVs, normals, textures, LOD hierarchy or production quality from opaque binary/DCC bytes.

Use licensed Unity for the real source-backed validation.

## Required continuation after intake

1. Commit the real authored source package.
2. Run this intake gate.
3. Run `p1_licensed_staging_readiness.py` and require `READY_FOR_LICENSED_STAGING`.
4. Run the licensed staging handoff that invokes the existing `HeroCarProductionPrefabStager` / binder.
5. Review and commit generated Unity import metadata / production prefab / provenance.
6. Build and test one new clean exact SHA under licensed Unity.
7. Prove the Hero is active in the Android Player on that exact APK.
8. Capture candidate-bound visual evidence and obtain explicit owner/Art Director acceptance under UPER-009.

No step above permits a generated procedural Preview Hero to qualify as UART-003 Production Art.
