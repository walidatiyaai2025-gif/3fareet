# Cairo Modular Street Kit — UART-005

## Current status: BLOCKED — authored source pass in progress

Owner visual rejection #128 supersedes the former `IN REVIEW` promotion. The original committed OBJ files were blockout geometry only; for example, the former facade was an 8-vertex rectangular box and the runtime prefabs still use Unity built-in primitive meshes.

The first authored-source replacement pass is now committed on the remediation branch:

- facade: 136 vertices / 204 triangles with pilasters, floor bands, window recesses, storefronts, door, balcony and cornice volumes;
- awning: 64 vertices / 96 triangles with pitched canopy, frame and ribs;
- lamp: 130 vertices / 236 triangles with cylindrical base/pole, lantern head and side fixtures;
- barrier: 48 vertices / 72 triangles with body, cap, feet and reflector volumes.

These geometry counts clear the manifest's **anti-blockout floors**, but that is necessary rather than sufficient. They are not called Production Accepted yet: UV/material authoring, runtime prefab replacement, environment expansion, exact Android visual evidence and owner/Art Director review are still outstanding.

## Production replacement contract

The complete pack must preserve meter scale and snap/contact pivots while supplying real authored 3D for:

- asphalt road, curb, edge and barrier modules;
- multiple Cairo facade/building/storefront variants;
- awnings, lamps, signs and roadside props;
- authored material detail appropriate to the final visual direction;
- mobile LODs/collision and deterministic Unity prefab packaging;
- landmark/skyline support consistent with the Premium Neon Egyptian Fantasy Racing art direction.

`ASSET_MANIFEST.json` defines minimum anti-blockout geometry floors and semantic detail requirements. Polygon counts cannot prove visual quality. `UPER-009` still requires exact-build screenshot/video review and explicit owner/Art Director acceptance.

## Candidate policy

- Procedural/blockout Cairo is allowed only for Editor/development iteration.
- A candidate/release Android build must fail closed while this manifest remains `BLOCKED`, while source geometry is under the anti-blockout floors, or while runtime prefabs still reference Unity built-in meshes.
- `UART-005` cannot return to `IN REVIEW` until authored source is imported into real runtime prefabs and the visual result is reviewable in Unity/Android.
