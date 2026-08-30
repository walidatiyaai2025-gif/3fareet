# 3FAREET — Unity Android Diagnostic Baseline — 2026-08-30

**Evidence type:** owner-provided real-device JSONL diagnostic excerpt  
**Purpose:** persist current Unity runtime facts so parallel workers do not restart already proven initialization work.

## Device/session

- App: `0.1.0`
- Platform: Android
- Device: `HONOR ALI-NX1`
- OS: Android 15 / API 35
- GPU: `Adreno (TM) 710`
- Memory reported: `11457 MB`
- Resolution: `2652x1200`
- Diagnostic path reported by runtime: `/storage/emulated/0/Android/data/com.fiftysolutions.afareetunity3d/files/3fareet-diagnostic-20260830-123511.jsonl`

## Confirmed runtime markers

### Lighting

`AFAREET_NIGHT_LIGHTING_NORMALIZED active=Moon Light disabledLegacyDirectional=0`

Interpretation: night-lighting normalization ran and `Moon Light` is active.

### Authored Cairo layout

`AFAREET_URAC011_AUTHORED_LAYOUT_ACTIVE layout=cairo-night-vertical-slice-v1 controlPoints=24 runtimeSegments=72`

Interpretation: the authored Cairo night vertical-slice route resolved with 24 control points and 72 runtime segments.

### Authored runtime geometry/resources

`AFAREET_UART005_AUTHORED_RUNTIME_ACTIVE geometry=tracked-obj resources=staged playerMaterials=source-authored`

Interpretation: authored runtime geometry/resources path is active and source-authored player materials are in use.

### Authored road/curb

`AFAREET_UART005_AUTHORED_ROAD_ACTIVE source=tracked-obj road=SM_Track_CairoRoad_A curb=SM_Track_CairoCurb_A playerMaterials=source-authored`

Interpretation: authored road and curb assets are active in runtime.

### Building variants

`AFAREET_UART005_BUILDING_VARIANTS_ACTIVE facades=3 awnings=2 signs=1 selection=stable-position-hash playerMaterials=source-authored`

Interpretation: deterministic building variation exists, but the current visible variety is still modest and should be expanded by the U3D world/art program.

### Road joins

`AFAREET_URAC011_MITER_JOINS_ACTIVE roadWidth=14 authoredSegments=72 railDirection=forward`

Interpretation: miter-join handling is active across the authored route.

### Garage session

`AFAREET_GARAGE_SESSION_ACTIVE equipped=afareet_king migratedLegacy=False recoveredInvalid=False`

Interpretation: garage session initialization succeeded for `afareet_king`; this excerpt does not show a legacy migration or invalid-save recovery event.

## What this evidence proves

This excerpt proves that several Unity initialization/runtime systems were active on the real Android device:
- Unity application startup;
- night lighting setup;
- authored route resolution;
- authored road/curb path;
- building variants;
- road join processing;
- garage session initialization.

## What this evidence does NOT yet prove

Do not infer the following without later evidence:
- final player spawn success;
- complete vehicle-controller health;
- chase-camera visual acceptance;
- traffic runtime health;
- complete race lifecycle;
- stable long-duration FPS/frame pacing;
- no exceptions after the shown excerpt;
- final visual quality;
- final release APK verification.

## Required next diagnostic evidence

Future U3D QA should capture markers for:
- player spawn/controller;
- camera state and selected camera profile;
- traffic/AI counts;
- race/event state;
- quality tier;
- FPS/frame-time samples;
- exceptions/recoveries;
- exact source/build SHA.

This baseline is evidence for **G0 live-state reconciliation**, not proof that later 3D visual gates are complete.
