# UART-005 — Cairo modular street kit

## State
- Task: `UART-005`
- Issue: #88
- Parent PR: #87 (`agent/UVEH-003-004-handling-surfaces`)
- Branch: `agent/UART-005-cairo-modular-street-kit`
- Status: IN REVIEW

## Source pipeline
Original project-generated source lives under:
`docs/assets/02_tracks_environments/cairo_street_kit/`

Committed source modules:
- `SM_Env_CairoFacade_A.obj` — 6.0m × 5.0m × 0.4m;
- `SM_Env_CairoAwning_A.obj` — 3.0m × 0.2m × 1.5m;
- `SM_Prop_CairoLamp_A.obj` — 0.2m × 3.0m × 0.2m;
- `SM_Prop_CairoBarrier_A.obj` — 2.0m × 0.6m × 0.4m.

All source blockouts use meters, +Y up / +Z forward / +X right, and place the modular contact/snap minimum corner at the source origin. `ASSET_MANIFEST.json` records ownership, source/license status, runtime target and review state.

## Runtime kit
Runtime assets live under:
`unity_game/Assets/Afareet/Art/TracksEnvironments/CairoStreetKit/`

Delivered:
- 4 committed prefabs with snap-origin roots and one geometry/collider child each;
- one original 256×256 shared base-color atlas `T_Env_CairoStreetKit_BC.png`;
- 4 materials selecting atlas quadrants through `_MainTex` scale/offset;
- Built-in Forward-compatible `S_Env_CairoStreetAtlas.shader`, matching the current non-URP production pipeline;
- stable folder, texture, shader, material and prefab `.meta` GUIDs;
- `CairoStreetKitValidator` Editor gate.

## Atlas layout
Logical 128×128 tiles:
- top-left: sandstone facade/window language;
- top-right: cyan/purple/orange awning stripe language;
- bottom-left: dark/cyan/gold lamp language;
- bottom-right: concrete/orange barrier language with neon accents.

The kit deliberately shares one atlas to reduce material/texture switching for the small P1 environment set.

## Validator contract
`Afareet/Validate Cairo Street Kit` checks after Unity import:
1. atlas imports and remains 256×256;
2. atlas shader imports under the expected shader name;
3. all 4 materials import, use the shared shader and shared atlas;
4. all 4 prefabs import;
5. each prefab root remains at the modular snap origin;
6. each prefab has exactly one geometry child;
7. each child retains the expected material binding and non-trigger BoxCollider.

`ValidateOrThrow()` is public so Unity CI can call the same gate once the licensing path is available.

## Scope guard
No Vehicle, Race/AI, UI, Audio, World generator, Package, ProjectSettings, Android/build or release file is modified by this task. The current procedural Cairo runtime remains untouched; integration/replacement of procedural placement with authored prefab placement should be a separate reviewed world-layout change.

## Validation truth
- Source manifest/static path review: completed in-repo.
- Unity 6000.5.8f1 import: NOT EXECUTED in this connector session.
- Shader/material compile/render: NOT EXECUTED.
- Prefab deserialization/render: NOT EXECUTED.
- Editor validator execution: NOT EXECUTED.
- Android Visual Gate: NOT EXECUTED.
- VERIFIED: No.

## Remaining QA before VERIFIED
1. Import exact PR head in Unity 6000.5.8f1.
2. Run `CairoStreetKitValidator.ValidateOrThrow()`.
3. Place all four prefabs in a neutral preview scene and review scale/pivot/material UV tiles.
4. Confirm atlas mip/compression quality on target Android Low/Mid/High tiers.
5. Capture speed-context screenshots and complete the P1 Visual Gate before any production-art VERIFIED promotion.
