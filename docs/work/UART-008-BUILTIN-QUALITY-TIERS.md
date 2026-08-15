# UART-008 — Built-in mobile material / lighting quality tiers

## State
- Task: `UART-008`
- Issue: #85
- Parent PR: #94
- Parent exact head: `d42e4ec6e81d2da0448360b2cab0ee3e8e8bc776`
- Branch: `agent/UART-008-builtin-quality-tiers`
- Architecture: Built-in/custom retained for U-P1 by ADR 0003
- Target state: IN REVIEW

## Blocker resolution
The old task wording required URP although production P1 has no URP package/pipeline. Issue #85 and ADR 0003 explicitly resolve that mismatch: P1 stays on the existing Built-in/custom pipeline, and UART-008 delivers Low/Mid/High production quality tiers for that pipeline. URP migration is a separate post-P1 architecture change.

## Data-driven profiles
`Resources/Config/runtime_builtin_quality_tiers.json` owns the runtime values.

### Low
- 30 FPS
- 0.80 render scale
- 2 pixel lights
- 35m shadow distance
- hard shadows / no cascades / low shadow resolution
- no MSAA
- anisotropic filtering disabled
- LOD bias 0.75 / maximum LOD level 1
- shader max LOD 150
- soft particles/realtime reflection probes off

### Mid
- 45 FPS
- 0.90 render scale
- 4 pixel lights
- 55m shadow distance
- soft shadows / 2 cascades / medium shadow resolution
- 2x MSAA
- per-texture anisotropic filtering
- LOD bias 1.0
- shader max LOD 200
- soft particles on / realtime reflection probes off

### High
- 60 FPS
- 1.00 render scale
- 6 pixel lights
- 75m shadow distance
- soft shadows / 4 cascades / high shadow resolution
- 4x MSAA
- forced anisotropic filtering
- LOD bias 1.25
- shader max LOD 300
- soft particles + realtime reflection probes on

The FPS/render-scale/light/shadow-distance baseline matches the earlier support artifact from PR #66, but this branch implements it on the current production line.

## Selection policy
`MobileRenderQualityPolicy` uses known device capabilities:
- any known memory/GPU/shader value below the Low threshold selects Low;
- all known values at/above High thresholds select High;
- unknown or mixed capability selects Mid.

QA can force `low`, `mid`, or `high` through PlayerPrefs key `afareet.render_quality`; deleting the key restores auto selection.

## Production controller
`MobileRenderQualityController` self-boots and applies the active profile through Built-in Unity APIs:
- `Application.targetFrameRate`;
- `QualitySettings.pixelLightCount`;
- shadow distance/mode/cascades/resolution;
- MSAA and anisotropic filtering;
- LOD bias/maximum LOD;
- soft particles/reflection probes;
- `Shader.globalMaximumLOD`;
- `ScalableBufferManager.ResizeBuffers`;
- dynamic-resolution permission on active cameras.

The module is isolated in `Afareet.Rendering`; no `AfareetBootstrap`, Package, ProjectSettings, gameplay or shader conversion is required.

## Tests committed
`Afareet.RenderingEditModeTests` covers:
1. low system memory → Low;
2. low graphics memory → Low;
3. strong known capabilities → High;
4. unknown/mixed capability → Mid;
5. case-insensitive QA override parsing;
6. `auto` does not force a tier;
7. valid production-style config passes;
8. non-monotonic render scale is rejected;
9. invalid MSAA value is rejected.

## Scope guard
Only ADR/evidence, the new Rendering assembly, its Resources JSON and isolated Rendering tests are owned here. No Vehicle/Race/UI/Audio/World/Packages/ProjectSettings/release logic or UART-003 Hero files are modified.

## Validation truth
Completed in repository:
- architecture decision and scope review;
- config/policy/controller static review;
- isolated test source + Unity metadata committed.

Not executed on this exact branch in the connector session:
- Unity 6000.5.8f1 import/compile;
- `Afareet.RenderingEditModeTests` execution;
- runtime tier selection on Android;
- dynamic render scale verification;
- Low/Mid/High screenshot comparison;
- FPS/GPU/memory/thermal measurements.

**VERIFIED: No.**

## Remaining QA
1. Compile exact PR head in Unity 6000.5.8f1.
2. Execute `Afareet.RenderingEditModeTests`.
3. Force Low/Mid/High overrides on Android and capture settings/log evidence.
4. Verify dynamic render scale/camera behavior and visual stability.
5. Capture representative screenshots for all three tiers.
6. Record FPS/GPU/memory/thermal results before VERIFIED promotion.
