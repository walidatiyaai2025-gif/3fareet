# P1 Production Art Gate

## Purpose

This gate exists because a technically valid Unity build can still render a procedural/blockout presentation that is unacceptable as production art.

The gate is a **fail-closed precondition** for `UPER-009` and release review. It does not replace owner/Art Director judgment and it never marks an APK `VERIFIED`.

## Required P1 visual tasks

One exact candidate must provide accepted production-art evidence for all six existing U-P1 tasks:

- `UART-003` — Hero car production model + LODs.
- `UART-004` — three Rival production variants.
- `UART-005` — Cairo modular street kit.
- `UART-006` — authored landmark kit.
- `UART-007` — authored track dressing/lighting vertical slice.
- `URAC-011` — authored Cairo vertical-slice layout.

Defects #127 and #128 remain the owner rejection/remediation records. This gate does not create task 66/67.

## Source-authority parity

Production-art evidence must name source/runtime artifacts that the production Unity pipeline itself can accept. A tracked file with a valid SHA-256 is **not sufficient** if its task/path role is invalid.

The gate rejects authored 3D source paths containing any of these non-production path segments, case-insensitively:

- `Generated`
- `Placeholder`
- `LegacyProcedural`
- `Preview`
- `Refinement`
- `RefinementCandidates`
- `Blockout`
- `Review`
- `ReviewPackaging`

Allowed authored 3D suffixes remain `.fbx`, `.obj`, `.blend`, `.glb`, and `.gltf`.

### UART-003 Hero policy

Every authored 3D Hero source declared in `UART-003.sourceFiles` must:

- live in a `Vehicles` role path;
- **not** live in a `Rivals` role path;
- satisfy the global non-production-segment rules above.

A Rival model, world model, review/refinement model, or generated/placeholder model cannot satisfy Hero production-art evidence merely because it is tracked and hashed.

### UART-004 exact Rival policy

The authored 3D source set for `UART-004` must be **exactly** these three repository paths:

```text
unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj
unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj
unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj
```

The runtime evidence must include all three production prefabs:

```text
unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab
unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab
unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab
```

Companion non-3D dependencies may still be declared in `sourceFiles`, but the authored 3D model set itself cannot contain an alternate fourth model or omit one of the three deterministic exchange OBJ files.

## Artifact fingerprint requirement

Schema v2 binds the review not only to the candidate Git SHA and APK SHA-256, but also to the **exact bytes** reviewed for every declared source file, packaged runtime asset, and screenshot/video.

Every `sourceFiles`, `runtimeAssets`, and `evidence` entry must carry a 64-hex `sha256`. The verifier recomputes each digest and fails if bytes changed after the review manifest was created, even when the path is unchanged.

The gate also rejects:

- reuse of the same screenshot/video file across multiple required production-art tasks;
- legacy schema-v1 manifests that do not pin artifact bytes;
- source/runtime declarations that violate the task-specific source policy above.

## Candidate Git provenance requirement

A matching manifest SHA is not enough by itself. Repository source/runtime files must also belong to the **same Git commit named by `candidate.gitSha`**.

The verifier requires:

- `--repo-root` to be the exact Git worktree root;
- repository `HEAD` to equal the manifest candidate Git SHA;
- every declared source/runtime artifact to be tracked by that candidate commit;
- the candidate Git blob bytes for each tracked source/runtime path to have the same SHA-256 as the reviewed working-tree artifact.

This blocks false-acceptance paths such as reviewing an untracked model, changing a tracked artifact after checkout, or copying an exact-candidate SHA into a manifest produced from another checkout.

## What the verifier rejects

`tools/android/verify_p1_production_art.py` rejects the evidence manifest when any of these are true:

- candidate Git SHA or APK SHA is absent/invalid/mismatched;
- repository HEAD does not equal the candidate Git SHA;
- a source/runtime path is untracked or absent from the candidate commit;
- working-tree source/runtime bytes differ from the candidate Git blob;
- owner acceptance is absent;
- any required task is missing or not `ACCEPTED`;
- any required task is still blockout/non-production;
- authored 3D source or packaged runtime asset paths are missing;
- any source/runtime/evidence SHA-256 is missing, malformed, or mismatched;
- global or task-specific source authority is violated;
- the UART-004 authored model set is not exactly the three deterministic exchange OBJ files;
- one or more required UART-004 production prefabs is absent from runtime evidence;
- no screenshot/video evidence exists for a required task;
- one visual evidence file is reused across tasks;
- Hero, rivals, track, Cairo world, or landmarks still use a procedural fallback;
- the evidence manifest attempts to self-assert `verified:true`.

The structural pass result is only:

`PRODUCTION_ART_GATE_PASSED`, `verified=false`.

## Evidence manifest shape

The real manifest is produced for one exact post-art Android candidate and should live with that candidate's review evidence, not as a permanent pre-approved repository file.

Example UART-003 fragment:

```json
{
  "reviewState": "ACCEPTED",
  "quality": "production",
  "authored3D": true,
  "runtimeActive": true,
  "proceduralFallbackActive": false,
  "ownerAccepted": true,
  "sourceFiles": [
    {
      "path": "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx",
      "sha256": "<64-hex>"
    }
  ],
  "runtimeAssets": [
    {"path": "unity_game/Assets/.../PF_Hero.prefab", "sha256": "<64-hex>"}
  ],
  "evidence": [
    {"kind": "screenshot", "path": "visual-hero.png", "sha256": "<64-hex>"}
  ]
}
```

UART-004 must use the exact source/runtime path sets documented above. Every visual evidence path must be unique across tasks.

## Structural gate command

Run from the exact candidate checkout:

```bash
git rev-parse HEAD
python tools/android/verify_p1_production_art.py \
  --manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --expected-git-sha <EXACT_GIT_SHA> \
  --expected-apk-sha <EXACT_APK_SHA256> \
  --output evidence/p1-review/production-art-gate.json
```

Expected success marker:

`AFAREET_PRODUCTION_ART_GATE_OK ... verdict=PRODUCTION_ART_GATE_PASSED verified=false`

## Authoritative publication preflight

Do **not** treat the structural production-art verifier above as the final release/publication command. The authoritative combined manual-publication preflight is:

```bash
python tools/android/verify_release_with_production_art.py \
  --candidate-manifest <EXACT_CANDIDATE_MANIFEST.json> \
  --session <PHYSICAL_DEVICE_SESSION_DIR> \
  --review-bundle <VERIFIED_REVIEW_BUNDLE_DIR> \
  --approvals <MANUAL_APPROVALS.json> \
  --production-art-manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --performance-tier <low|mid|high> \
  --output evidence/p1-review/combined-publication-preflight.json
```

This wrapper re-runs the production-art verifier against the candidate's exact Git/APK fingerprint, enforces `UPER-006` Android-observable smoke metrics, and then runs the publication preflight. Its success remains only:

`ELIGIBLE_FOR_MANUAL_PUBLICATION_WITH_PRODUCTION_ART_AND_SMOKE_METRICS`, `verified=false`.

`UPER-010` remains the final manual publication approval; the wrapper never publishes, tags, uploads, or marks the APK `VERIFIED`.

## Current repository truth

The convergence line contains source/runtime remediation and fail-closed production-source/evidence controls, but it remains **BLOCKED/unverified**.

The repository still does not contain owner-accepted real Hero/Rival production packages required by UART-003/UART-004, and the current line has not completed licensed Unity import/build proof, exact Android physical-device evidence, or owner/Art Director acceptance.

Therefore no current APK should be represented as having passed production-art acceptance. A valid manifest can only be produced from a new exact candidate after real authored production assets are integrated, tracked, licensed-staged, built, physically exercised, and explicitly accepted on that same Git/APK fingerprint.
