# UART-005 complete mobile LOD authoring reproducibility

## Problem found

`MOBILE_LOD_MANIFEST.json` has advanced to **13/13 modules** and **26 distinct LOD1/LOD2 sources** after Road A and Curb A joined the mobile LOD path. The original `tools/android/author_uart005_mobile_lods.py` still owns only the original 11 repeated environment/prop families and emits an 11/11 manifest.

Running that original core author directly after the road/curb extension can therefore regress the committed manifest metadata from 13/13 back to 11/11 even though the road/curb runtime and Android gates are present.

## Canonical complete entry point

Use:

```bash
python tools/android/author_uart005_mobile_lods_complete.py
```

The complete authoring command:

1. deterministically rebuilds the original 11 mobile LOD families through the existing core author;
2. deterministically rebuilds Road A LOD1/LOD2;
3. deterministically rebuilds Curb A LOD1/LOD2;
4. preserves strict LOD0 > LOD1 > LOD2 triangle monotonicity;
5. preserves full UV0, authored normal and texture-mapped MTL contracts;
6. rejects duplicate source hashes;
7. emits exactly **13 modules / 26 distinct sources**;
8. leaves `reviewState=BLOCKED` and `runtimeLodIntegrationVerified=false`.

The core 11-family success marker is suppressed by the complete wrapper so CI/operator logs expose only the final complete truth.

## Regression protection

`tools/android/tests/test_uart005_complete_lod_authoring_repro.py` runs the complete author in a temporary clean working directory and requires the regenerated parsed manifest to equal the committed `MOBILE_LOD_MANIFEST.json` exactly. It also locks the Road/Curb source hashes to the committed deterministic candidates.

This is an engineering/reproducibility fix only. It does **not** satisfy licensed Unity import/render proof, physical-device performance review, exact Android visual evidence, or the owner/Art Director Visual Gate.
