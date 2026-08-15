# Cairo Modular Street Kit — UART-005

## Current status: BLOCKED

The committed `source/` OBJ files are **blockout geometry only**. They exist for scale, snapping, collision/layout prototyping and must not be presented as production Cairo art.

Owner visual rejection #128 supersedes the former `IN REVIEW` promotion. The current facade is an 8-vertex rectangular box; the other modules are similarly minimal blockouts. The existing runtime prefabs also use built-in Unity primitive meshes rather than accepted authored production geometry.

## Production replacement contract

The replacement pack must preserve meter scale and snap/contact pivots while supplying real authored 3D for:

- asphalt road, curb, edge and barrier modules;
- multiple Cairo facade/building/storefront variants;
- awnings, lamps, signs and roadside props;
- authored material detail appropriate to the final visual direction;
- mobile LODs/collision and deterministic Unity prefab packaging.

`ASSET_MANIFEST.json` defines minimum anti-blockout geometry floors and semantic detail requirements. Those automated floors are **necessary but not sufficient**: polygon counts cannot prove visual quality. `UPER-009` still requires exact-build screenshot/video review and explicit owner/Art Director acceptance.

## Candidate policy

- Procedural/blockout Cairo is allowed only for Editor/development iteration.
- A candidate/release Android build must fail closed while this manifest remains `BLOCKED`, while source geometry is under the anti-blockout floors, or while runtime prefabs still reference Unity built-in meshes.
- `UART-005` cannot return to `IN REVIEW` until real authored production source is committed and used by the runtime world.
