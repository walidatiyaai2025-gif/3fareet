# UART-003 — Hero Car production model + LODs

## State
- Task: `UART-003`
- Issue: #93
- Parent: PR #92 / `agent/UUI-004-race-lifecycle-integration`
- Parent exact head: `92dc867cca99388c3ec9013e64d88ff2638f7cdb`
- Branch: `agent/UART-003-hero-car-production-lods`
- State: **IN REVIEW candidate** pending final PR/scope check

## Acceptance gap closed by this branch
The production player car on the parent line is visually assembled at runtime from Unity primitive meshes. That blockout remains useful as a fail-safe, but it is not a production model + LOD asset.

This branch adds an original, versioned Hero model source set and a deterministic Unity ingestion pipeline instead of counting the earlier procedural visual polish as UART-003 completion.

## Authored source model
Source root:
`docs/assets/01_vehicles/hero_car_production/source/`

| LOD | Vertices | Triangles | Cap | Screen height |
|---|---:|---:|---:|---:|
| `SM_Vehicle_AfareetKing_LOD0` | 274 | 476 | 600 | 0.18 |
| `SM_Vehicle_AfareetKing_LOD1` | 194 | 332 | 400 | 0.07 |
| `SM_Vehicle_AfareetKing_LOD2` | 104 | 180 | 220 | 0.01 |

The final meshes were deliberately optimized down from the initial authoring pass before commit. Triangle count decreases at each level while the distant LOD retains the body/wheel/Spirit read. Near LODs retain gold aero and Spirit groups.

Geometry is original project-authored work: fictional low aggressive coupe, meter scale, +Y up, +Z forward, +X right. No third-party production-car model is used.

## Source integrity
`ASSET_MANIFEST.json` records exact counts and SHA-256 values for all three source OBJ files. It also records the target runtime generation path, ownership/source status and zero-texture decision.

## Mobile material / texture budget
The P1 Hero uses `Afareet/RuntimeLit` color/emission materials with no texture maps:
- Hero texture-map count: **0**
- Hero texture-map memory: **0 KB**

This is intentional. Adding body/normal/ORM textures is deferred until Android visual/performance evidence shows available budget.

## Deterministic Unity generation
`HeroCarProductionAssetBuilder`:
1. reads the versioned OBJ files directly from `docs/assets`;
2. rejects missing/non-triangulated/out-of-range source;
3. validates exact vertex/triangle counts and known material groups;
4. creates generated Unity Mesh assets;
5. creates shared color/emission Materials;
6. creates `PF_Vehicle_AfareetKing_Production` in Resources;
7. creates a 3-level `LODGroup` at 0.18 / 0.07 / 0.01;
8. disables shadows on LOD2 and receive-shadows beyond LOD0/1 budget;
9. validates the generated prefab, LOD renderers, mesh counts, material bindings and zero-texture contract.

`HeroCarProductionBuildPreprocessor` calls the same builder before every Unity Player build. Interactive Editor sessions also generate once when the derived prefab is absent.

Generated assets are git-ignored. Stable parent folder `.meta` files are committed so Unity does not create team-noise GUID files outside the generated boundary.

## Runtime integration
`HeroCarProductionVisualInstaller` is intentionally additive and does **not** modify `CarFactory`:
- finds only the player Hero;
- caches/disables existing visual `MeshRenderer` blockout;
- attaches the generated Resources prefab;
- keeps Rigidbody, gameplay BoxCollider, `ArcadeCarController`, TrailRenderers, lights and VFX untouched;
- restores procedural renderers if the production prefab is unavailable/invalid.

The narrower installer was selected after reviewing the shared factory, to avoid mixing an art migration with vehicle creation/physics ownership.

## Automated coverage committed
`HeroCarLodPolicyTests` covers:
- internal budget/transition contract validation;
- exact 0.18 / 0.07 / 0.01 transitions;
- all three authored exact vertex/triangle counts;
- wrong/over-budget rejection;
- monotonic triangle reduction;
- stable Resources prefab path.

The Editor builder contains the full source/generated-asset validator for execution once Unity is available.

## Validation truth
Completed in repository:
- source naming/convention review;
- deterministic authoring count review: 274/476, 194/332, 104/180;
- source/build/runtime scope review;
- Unity `.meta` coverage for committed C# and stable parent folders.

Not executed on this exact branch in the connector session:
- Unity 6000.5.8f1 import/compile;
- Editor source parser/builder execution;
- generated prefab serialization/deserialization;
- `HeroCarLodPolicyTests` execution;
- Android render/LOD switching;
- Visual Gate screenshots;
- device FPS/GPU/memory comparison.

**VERIFIED: No.**

## Remaining QA before VERIFIED
1. Open exact PR head in Unity 6000.5.8f1.
2. Run `Afareet/Build Hero Car Production LODs` and `Afareet/Validate Hero Car Production LODs`.
3. Run `Afareet.EditModeTests` including `HeroCarLodPolicyTests`.
4. Confirm the generated Resources prefab replaces the primitive Hero visually while gameplay collider/controller remain unchanged.
5. Force/inspect all three LODs and confirm silhouette/material identity and no pop-breaking geometry.
6. Build Android through the normal pipeline and confirm build-preprocessor generation from a clean checkout.
7. Capture Visual Gate screenshots and device performance evidence before any VERIFIED promotion.
