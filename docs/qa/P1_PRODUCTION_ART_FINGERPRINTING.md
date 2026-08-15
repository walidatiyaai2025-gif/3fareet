# P1 Production Art Manifest Fingerprinting

## Purpose

Production-art schema v2 requires exact SHA-256 fingerprints for every declared authored source, packaged runtime asset and screenshot/video evidence file. These fingerprints must come from the actual bytes under review, not from manually copied or guessed values.

`tools/android/fingerprint_p1_production_art_manifest.py` performs that preparation deterministically. It does **not** approve art, change task state, publish an APK or mark anything `VERIFIED`.

## Input template

Prepare a schema-v2 production-art manifest beside the visual evidence files. Declare each artifact as an object with a `path`; the SHA-256 may be absent in the template because the fingerprinter will populate it.

Example task fragment:

```json
{
  "sourceFiles": [
    {"path": "unity_game/Assets/.../hero.fbx"}
  ],
  "runtimeAssets": [
    {"path": "unity_game/Assets/.../PF_Hero.prefab"}
  ],
  "evidence": [
    {"kind": "screenshot", "path": "visual-hero.png"}
  ]
}
```

The input must keep `schemaVersion: 2` and `verified: false`.

## Command

```bash
python tools/android/fingerprint_p1_production_art_manifest.py \
  --manifest evidence/p1-review/p1-production-art-template.json \
  --repo-root . \
  --output evidence/p1-review/p1-production-art.json
```

The output must be beside the input manifest. This is deliberate: screenshot/video paths are resolved relative to the manifest directory, so moving the output elsewhere could silently change what a relative evidence path means.

The tool never overwrites the input or an existing output file.

Expected marker:

`AFAREET_PRODUCTION_ART_FINGERPRINT_OK artifacts=<N> schemaVersion=2 verified=false ...`

## Verification

Run the normal fail-closed production-art gate against the new fingerprinted manifest:

```bash
python tools/android/verify_p1_production_art.py \
  --manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --expected-git-sha <EXACT_GIT_SHA> \
  --expected-apk-sha <EXACT_APK_SHA256>
```

Any later byte change to a source model, packaged runtime asset or visual-evidence file invalidates its stored SHA-256 and blocks the gate.

## Current acceptance truth

This workflow only prepares tamper-evident review metadata. The current convergence line remains blocked pending the real owner-approved Afareet King production source, licensed Unity import/build proof, exact Android device evidence, performance review, `UPER-009` and final `UPER-010` manual approval.
