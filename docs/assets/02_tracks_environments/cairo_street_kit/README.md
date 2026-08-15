# Cairo Modular Street Kit — UART-005

## Current status: BLOCKED — implementation/source work complete enough for licensed runtime review

Owner visual rejection #128 remains authoritative. The package has advanced well beyond the original primitive/blockout state, but it is **not Production Accepted** because licensed Unity import/render/runtime proof, exact Android visual/device evidence, final material refinement where required, and owner/Art Director acceptance are still outstanding.

## Authored core street kit — 10/10 surfaced

All ten repeated core modules now have tracked OBJ → MTL → PNG candidate chains with UV0 and explicit normal references:

- Road A — 96 vertices / 150 triangles;
- Curb A — 80 / 120;
- Awning A — 64 / 96;
- Awning B — 88 / 132;
- Facade A — 136 / 204;
- Facade B — 216 / 324;
- Facade C — 200 / 300;
- Lamp A — 68 / 112;
- Barrier A — 58 / 92;
- Hanging Sign A — 80 / 120.

Facade selection is 3/3, awning selection is 2/2, and hanging-sign coverage is 1/1. `CairoAuthoredStreetKit` deterministically selects variants and preserves imported source materials in Player. Primitive road, rail, building and lamp visual fallbacks are disabled in Player.

## Authored roadside clutter — 3/3

The second source handoff is also complete at source/implementation level:

- Planter A — 174 vertices / 300 triangles;
- Market Crate Stack A — 216 / 324;
- Café Table A — 306 / 552.

`CairoAuthoredRoadsideClutter` and `CairoRoadsideClutterRuntimePass` implement deterministic source-backed building decoration without primitive or generated-Mesh fallback. `P1ProductionRoadsideClutterBuildGate` stages the tracked sources and fails Android builds closed on missing imports, UV0, normals or texture-mapped materials.

Runtime implementation exists, but `runtimeIntegrated=false` and `runtimeIntegrationVerified=false` remain deliberate until licensed Unity/runtime evidence exists.

## Mobile LOD source/runtime path — 13/13

`MOBILE_LOD_MANIFEST.json` tracks one distinct LOD1 and one distinct LOD2 OBJ for each of the 13 repeated visual families: core facade/awning/prop modules, all three clutter modules, Road A and Curb A. That is **26 distinct secondary LOD source assets**.

Every family must satisfy `LOD0 triangles > LOD1 triangles > LOD2 triangles > 0`, and same-Mesh LOD reuse is forbidden. Road and curb are included explicitly:

- Road A — 150 → 36 → 12 triangles;
- Curb A — 120 → 24 → 12 triangles.

The runtime/build path is implemented through `CairoStreetKitMobileLodRuntimePass`, `CairoRoadCurbMobileLodRuntimePass`, `P1ProductionMobileLodBuildGate`, and `P1ProductionRoadCurbMobileLodBuildGate`. Secondary LOD colliders are rejected and imported UV0/normals/textured materials are mandatory.

Use `python tools/android/author_uart005_mobile_lods_complete.py` as the canonical deterministic authoring entry point. The complete author reproduces the full 13-module / 26-source manifest rather than the historical 11-family intermediate set.

## Production replacement contract

The final accepted pack must preserve meter scale and snap/contact pivots while supplying authored source-backed 3D, mobile-safe LODs, collision separation, and coherent Cairo Night materials. The remaining work is **verification/refinement**, not re-implementation of completed source families:

- licensed Unity import/compile/render proof for core + clutter + all LOD Resources;
- runtime proof that deterministic variants, clutter placement and all expected LODGroups use the correct distinct source assets;
- physical-device performance and transition review;
- final base-color / normal / ORM or equivalent material refinement where licensed render review requires it;
- coordinated landmark/skyline review with UART-006/UART-007;
- exact Android in-race screenshot/video evidence;
- owner/Art Director Visual Gate acceptance.

`ASSET_MANIFEST.json`, `ROADSIDE_CLUTTER_MANIFEST.json`, `ROADSIDE_CLUTTER_RUNTIME_STATUS.json`, `MOBILE_LOD_MANIFEST.json`, and `MOBILE_LOD_RUNTIME_STATUS.json` are expected to agree on this implementation-versus-verification boundary.

## Candidate policy

- Procedural/blockout Cairo is allowed only for Editor/development iteration where explicitly retained.
- A candidate/release Android build must fail closed while this task remains `BLOCKED` or any production source/import/runtime contract fails.
- Source/implementation completion must never be treated as `VERIFIED` without licensed Unity evidence, exact Android evidence and explicit owner/Art Director acceptance.
