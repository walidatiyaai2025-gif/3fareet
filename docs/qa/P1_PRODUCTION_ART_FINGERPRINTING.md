# P1 Production Art Manifest Fingerprinting

## Purpose

Production-art schema v2 requires exact SHA-256 fingerprints for every declared authored source, packaged runtime asset, and screenshot/video evidence file. These fingerprints must come from the actual bytes under review, not manually copied or guessed values.

`tools/android/fingerprint_p1_production_art_manifest.py` performs that preparation deterministically. It does **not** approve art, change task state, publish an APK, or mark anything `VERIFIED`.

The fingerprinter must run from the **exact candidate Git checkout**. Before it writes any output it verifies that repository `HEAD` equals `candidate.gitSha`, every declared source/runtime file is tracked by that commit with byte-identical Git blob content, and the declared artifacts satisfy the same task-specific source policy enforced by the final production-art verifier.

This means the fingerprinter intentionally fails early rather than producing a hashed manifest that the verifier would reject later.

## Source-policy requirements before fingerprinting

The global authored-source rules reject these non-production path families:

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

### UART-003

The authored Hero 3D source must be in a `Vehicles` role path and must not be in a `Rivals` role path.

Example valid role path:

```text
unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx
```

A tracked/hashable Rival, world, review, or refinement model is still invalid UART-003 evidence.

### UART-004

The authored 3D source set must be exactly:

```text
unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj
unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj
unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj
```

The runtime asset list must include all three production prefabs:

```text
unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_01_Production.prefab
unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_02_Production.prefab
unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/PF_Rival_03_Production.prefab
```

Companion non-3D dependencies may be declared, but the authored model set itself cannot omit an exchange OBJ or include an alternate fourth authored 3D model.

## Input template

Prepare a schema-v2 production-art manifest beside the visual evidence files. Declare each artifact as an object with a `path`; the SHA-256 may be absent because the fingerprinter will populate it.

The `candidate.gitSha` must already contain the exact source/runtime assets being reviewed.

Example UART-003 fragment:

```json
{
  "sourceFiles": [
    {
      "path": "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"
    }
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

From the exact candidate worktree root:

```bash
git rev-parse HEAD
python tools/android/fingerprint_p1_production_art_manifest.py \
  --manifest evidence/p1-review/p1-production-art-template.json \
  --repo-root . \
  --output evidence/p1-review/p1-production-art.json
```

The tool fails before writing output if:

- repository HEAD differs from the manifest candidate Git SHA;
- `--repo-root` is not the exact Git worktree root;
- a declared source/runtime artifact is untracked;
- current source/runtime bytes differ from the candidate Git blob;
- a declared file is missing;
- the authored source uses a globally forbidden non-production path family;
- UART-003 violates Hero vehicle-role / non-Rival policy;
- UART-004 does not declare exactly the three deterministic production OBJ files;
- UART-004 does not include all three required production prefabs;
- the input self-asserts `verified:true`.

The output must be beside the input manifest. Screenshot/video paths are resolved relative to the manifest directory, so moving the output elsewhere could silently change what a relative evidence path means.

The tool never overwrites the input or an existing output file.

Expected marker:

`AFAREET_PRODUCTION_ART_FINGERPRINT_OK artifacts=<N> gitCandidateBound=true taskArtifactPolicy=true schemaVersion=2 verified=false ...`

## Structural verification

Run the fail-closed production-art verifier against the new fingerprinted manifest from the same candidate checkout:

```bash
python tools/android/verify_p1_production_art.py \
  --manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --expected-git-sha <EXACT_GIT_SHA> \
  --expected-apk-sha <EXACT_APK_SHA256>
```

Any later byte change to a source model, packaged runtime asset, or visual-evidence file invalidates its stored SHA-256. For repository source/runtime assets, even updating the manifest hash cannot hide working-tree drift because the verifier also compares the reviewed bytes to the candidate Git blob.

## Publication sequence

The structural verifier above is not the final release command. After fingerprinting + structural verification + exact physical-device evidence/manual approvals, use the authoritative combined preflight:

```bash
python tools/android/verify_release_with_production_art.py \
  --candidate-manifest <EXACT_CANDIDATE_MANIFEST.json> \
  --session <PHYSICAL_DEVICE_SESSION_DIR> \
  --review-bundle <VERIFIED_REVIEW_BUNDLE_DIR> \
  --approvals <MANUAL_APPROVALS.json> \
  --production-art-manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --performance-tier <low|mid|high>
```

That wrapper re-runs the hardened production-art verifier against the same Git/APK fingerprint, enforces `UPER-006` smoke metrics, and then runs the publication preflight. It still returns `verified=false`; `UPER-010` remains the final manual publication approval.

## Current acceptance truth

This workflow only prepares tamper-evident, source-policy-valid, candidate-anchored review metadata. The current convergence line remains blocked pending real owner-approved Hero/Rival production sources, licensed Unity import/build proof, exact Android physical-device evidence, performance review, `UPER-009`, and final `UPER-010` manual approval.
