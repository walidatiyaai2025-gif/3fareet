# P1 Production Art Gate

## Purpose

This gate exists because a technically valid Unity 3D build can still render a procedural/blockout presentation that is unacceptable as production art.

The gate is a **fail-closed precondition** for `UPER-009` and release review. It does not replace owner/Art Director judgment and it never marks an APK `VERIFIED`.

## Required P1 visual tasks

One exact candidate must provide accepted production-art evidence for all six existing U-P1 tasks:

- `UART-003` — Hero car production model + LODs.
- `UART-004` — rival production variants.
- `UART-005` — Cairo modular street kit.
- `UART-006` — pyramid/minaret/dome landmark kit.
- `UART-007` — track dressing/lighting vertical slice.
- `URAC-011` — replace blockout with Cairo vertical-slice layout.

Defects #127 and #128 remain the current owner rejection records. This gate does not create task 66/67.

## What the verifier rejects

`tools/android/verify_p1_production_art.py` rejects the evidence manifest when any of these are true:

- candidate Git SHA or APK SHA is absent/invalid/mismatched;
- owner acceptance is absent;
- any required task is missing or not `ACCEPTED`;
- any required task is still marked `blockout`/non-production;
- authored 3D source or packaged runtime asset paths are missing;
- no screenshot/video evidence exists for a required task;
- Hero, rivals, track, Cairo world or landmarks still use a procedural fallback;
- the evidence manifest attempts to self-assert `verified:true`.

The structural pass result is only:

`PRODUCTION_ART_GATE_PASSED`, `verified=false`.

## Evidence manifest shape

The real manifest is produced for one exact post-art Android candidate and should live with that candidate's review evidence, not as a permanent pre-approved repository file.

Minimal shape:

```json
{
  "schemaVersion": 1,
  "visualGate": "UPER-009",
  "verified": false,
  "ownerAccepted": true,
  "candidate": {
    "gitSha": "<40-hex exact candidate SHA>",
    "apkSha256": "<64-hex exact APK SHA-256>"
  },
  "fallbackState": {
    "heroProcedural": false,
    "rivalsProcedural": false,
    "trackProcedural": false,
    "cairoWorldProcedural": false,
    "landmarksProcedural": false
  },
  "assets": {
    "UART-003": {
      "reviewState": "ACCEPTED",
      "quality": "production",
      "authored3D": true,
      "runtimeActive": true,
      "proceduralFallbackActive": false,
      "ownerAccepted": true,
      "sourceFiles": ["docs/assets/.../source/hero.fbx"],
      "runtimeAssets": ["unity_game/Assets/.../PF_Hero.prefab"],
      "evidence": [{"kind": "screenshot", "path": "visual-hero.png"}]
    }
  }
}
```

All required task records follow the same contract.

## Command

```bash
python tools/android/verify_p1_production_art.py \
  --manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --expected-git-sha <EXACT_GIT_SHA> \
  --expected-apk-sha <EXACT_APK_SHA256> \
  --output evidence/p1-review/production-art-gate.json
```

Expected success marker:

`AFAREET_PRODUCTION_ART_GATE_OK ... verdict=PRODUCTION_ART_GATE_PASSED verified=false`

## Current repository truth

The current integration line does **not** satisfy this gate:

- Hero source is intentionally tiny/textureless low-poly and has been rejected by owner visual review (#127).
- Cairo street-kit sources are blockout modules and do not replace the procedural runtime world.
- `CairoTrackBuilder`, `CairoLandmarkRuntimePass`, and rival variant code still supply primitive/procedural presentation on the current integration head.

Therefore no current APK should have a production-art acceptance manifest that passes this verifier. The next valid manifest can only be created after real authored production 3D is integrated and captured in-race on a new exact candidate.
