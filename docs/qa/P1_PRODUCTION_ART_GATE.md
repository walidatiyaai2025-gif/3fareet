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

## Artifact fingerprint requirement

Schema v2 binds the review not only to the candidate Git SHA and APK SHA-256, but also to the **exact bytes** reviewed for every declared source file, packaged runtime asset and screenshot/video.

Every `sourceFiles`, `runtimeAssets` and `evidence` entry must therefore carry a 64-hex `sha256`. The verifier recomputes each digest from disk and fails if bytes changed after the review manifest was created, even when the path is unchanged.

The gate also rejects:

- authored source paths containing `Generated`, `Preview` or `Blockout` path segments;
- reuse of the same screenshot/video file across multiple required production-art tasks;
- legacy schema-v1 manifests that do not pin artifact bytes.

This prevents a reviewed manifest from becoming a transferable approval token for different source/runtime/evidence bytes.

## What the verifier rejects

`tools/android/verify_p1_production_art.py` rejects the evidence manifest when any of these are true:

- candidate Git SHA or APK SHA is absent/invalid/mismatched;
- owner acceptance is absent;
- any required task is missing or not `ACCEPTED`;
- any required task is still marked `blockout`/non-production;
- authored 3D source or packaged runtime asset paths are missing;
- any source/runtime/evidence SHA-256 is missing, malformed or mismatched;
- an authored source is under a generated/preview/blockout path segment;
- no screenshot/video evidence exists for a required task;
- one visual evidence file is reused across tasks;
- Hero, rivals, track, Cairo world or landmarks still use a procedural fallback;
- the evidence manifest attempts to self-assert `verified:true`.

The structural pass result is only:

`PRODUCTION_ART_GATE_PASSED`, `verified=false`.

## Evidence manifest shape

The real manifest is produced for one exact post-art Android candidate and should live with that candidate's review evidence, not as a permanent pre-approved repository file.

Minimal schema-v2 shape:

```json
{
  "schemaVersion": 2,
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
      "sourceFiles": [
        {"path": "unity_game/Assets/.../hero.fbx", "sha256": "<64-hex>"}
      ],
      "runtimeAssets": [
        {"path": "unity_game/Assets/.../PF_Hero.prefab", "sha256": "<64-hex>"}
      ],
      "evidence": [
        {"kind": "screenshot", "path": "visual-hero.png", "sha256": "<64-hex>"}
      ]
    }
  }
}
```

All required task records follow the same contract, and each visual evidence path must be unique across tasks.

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

`AFAREET_PRODUCTION_ART_GATE_OK ... fingerprints=<N> verdict=PRODUCTION_ART_GATE_PASSED verified=false`

## Current repository truth

The convergence line contains the engineering/source remediation for the Cairo world, dressing, landmarks, rivals, route, handling and smoke gates, plus the fail-closed external-source Hero staging/binding path. That engineering convergence is still **BLOCKED/unverified**.

The repository still does not contain the owner-accepted real Afareet King production model required by `UART-003`, and the converged art stack has not yet been imported/rendered/built under licensed Unity or accepted on an exact Android candidate by the owner/Art Director.

Therefore no current APK should have a production-art acceptance manifest that passes this verifier. A valid schema-v2 manifest can only be created after the real authored production assets are integrated, their exact bytes are fingerprinted, and fresh in-race evidence is captured and accepted on one exact candidate.
