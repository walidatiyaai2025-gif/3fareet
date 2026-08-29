# P1 Final Five Gate Plan

## Purpose

This document is the operator runbook for the **final five manual/device/release gates** after the production-visual source pipeline is structurally ready.

It is **not** the complete current blocker list. The fixed U-P1 register currently remains:

`IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`

No script in this flow is allowed to mark an APK `VERIFIED` automatically.

## Current blocker model: 6 production-visual/runtime + final 5

### Production-visual/runtime blockers that must participate in the same exact-candidate chain

1. `UART-003` — real Hero production model + licensed binding/render proof.
2. `UART-004` — real three-Rival production source package + licensed prefab/runtime/owner proof.
3. `UART-005` — licensed Cairo street-kit runtime/device/owner proof.
4. `UART-006` — licensed authored-landmark runtime/device/owner proof.
5. `UART-007` — licensed authored-dressing runtime/device/owner proof.
6. `URAC-011` — exact-candidate authored-layout runtime/device/owner proof.

### Final five manual/device/release gates represented by `tools/android/p1_gate_spec.json`

7. `UVEH-012` — real-device driving feel.
8. `URAC-012` — ordered lap / Results / restart device verification.
9. `UPER-006` — Android smoke/performance matrix.
10. `UPER-009` — owner/Art Director Visual Gate.
11. `UPER-010` — final manual publication approval.

The phrase **Final Five** refers only to items 7–11. It must never be used to rewrite the repository ledger to `60/5` while UART-003/UART-004/UART-005/UART-006/UART-007/URAC-011 remain blocked.

## 0. Production-source and licensed-staging precondition

Do not build the acceptance candidate from review/refinement/blockout art.

Before the production candidate exists:

1. deliver an acceptable externally authored Hero source satisfying the integrated UART-003 source policy;
2. deliver the exact three UART-004 production exchange OBJ files and Unity metadata;
3. use the clean convergence branch `agent/p1-remediation-convergence` as the pre-integration production line;
4. run the read-only staging readiness audit;
5. require every external source/handoff check to pass;
6. run licensed staging;
7. review the generated import metadata/prefabs/provenance;
8. commit only approved staging output;
9. start candidate tests/build from the resulting **new clean exact SHA**.

Readiness example:

```bash
python3 tools/android/p1_licensed_staging_readiness.py \
  --repo-root . \
  --hero-source Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx
```

Licensed staging is documented in:

- `docs/qa/P1_LICENSED_STAGING_READINESS.md`
- `docs/qa/P1_LICENSED_STAGING_HANDOFF.md`

Readiness/staging success is not runtime proof, owner acceptance, publication eligibility, or `VERIFIED` state.

## 1. Produce one exact current production candidate

Do not reuse an older APK, SHA, evidence bundle, approval file, or production-art manifest.

Accepted candidate types:

- `github-actions-unity-ci` when the hosted licensed Unity path is genuinely green;
- `local-windows-licensed-unity` through the strict licensed Windows flow.

The candidate must contain:

- full 40-character Git SHA;
- exact APK filename, size and SHA-256;
- production package `com.fiftysolutions.afareetunity3d`;
- `releaseEvidenceEligible: true`;
- `readyForDeviceEvidence: true`;
- `verified: false`;
- verdict `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`.

### Hosted GitHub Unity path

`Unity Production CI` must be completely green on the exact SHA:

1. static/package contract;
2. Unity license preflight;
3. EditMode + PlayMode with real passing NUnit evidence;
4. Android build;
5. package/minSdk/ARM64/libunity/APK inspection;
6. `verify_ci_candidate.py`.

A static-contract success followed by license-preflight failure is **not** a candidate.

### Licensed Windows path before #144 integration

Use:

**GitHub → Actions → Unity Licensed Windows Candidate → Run workflow**

For the current pre-integration convergence path:

- `candidate_ref`: `agent/p1-remediation-convergence`
- `expected_sha`: the exact reviewed 40-character current convergence head
- `candidate_mode`: `production`
- `unity_path`: licensed Unity `6000.5.8f1` on the self-hosted Windows x64 runner

The workflow explicitly allowlists both `agent/p1-remediation-convergence` and `agent/unblock-final-5`, but the convergence ref is the correct choice **before** #144 is allowed to merge. Never dispatch an arbitrary feature/PR ref.

The actual licensed job must execute. A workflow-contract-only success with the licensed job `SKIPPED` is not licensed proof.

Local equivalent from an exact clean convergence checkout:

```powershell
git fetch origin
git reset --hard origin/agent/p1-remediation-convergence
git clean -fd

powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1 `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
```

The resulting candidate remains `verified: false`.

## 2. Bind the candidate to one physical-device session

For a local candidate:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the candidate bundle moved workstations, pass the exact APK explicitly:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

An arbitrary direct-APK session cannot satisfy the P1/release evidence chain.

## 3. Capture the exact required final-five checkpoints

The declarative source of truth is:

`tools/android/p1_gate_spec.json`

### UVEH-012 — driving feel

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drive-straight
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label brake-reverse
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drift
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label nitro
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label reset-recovery
```

### URAC-012 — race lifecycle

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-countdown
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-midlap
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-results
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-restart
```

### UPER-006 — smoke/performance

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-cold-start
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-warm-race
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-after-restarts
```

### UPER-009 — visual checkpoints

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hero
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-cairo
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hud
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-arabic
```

These checkpoints also provide candidate-bound review material for the production-visual tasks. Review must confirm the authored Hero, exact Rival variants, Cairo kit, landmarks, dressing and authored URAC-011 layout are active in the Player, with acceptance-path procedural/blockout fallback inactive.

Any automated Fatal/ANR/native-fatal red flag blocks the final gates.

## 4. Finish collection and export the privacy-safe review bundle

```bash
python3 tools/android/device_evidence.py finish \
  --session evidence/p1-device

python3 tools/android/export_device_evidence.py \
  --session evidence/p1-device \
  --output evidence/p1-review
```

Collection/export success remains `MANUAL_REVIEW_REQUIRED`.

Verify the transferred bundle before human review:

```bash
python3 tools/android/verify_device_review_bundle.py \
  --bundle evidence/p1-review \
  --expected-git-sha <exact-candidate-git-sha> \
  --expected-apk-sha <exact-candidate-apk-sha256>
```

Successful bundle verification still reports `verified=false`.

## 5. Build and fingerprint the production-art manifest

`UPER-009` is not satisfied by the four visual checkpoint names alone. Prepare candidate-anchored production-art evidence for all six visual/runtime tasks:

- UART-003
- UART-004
- UART-005
- UART-006
- UART-007
- URAC-011

Follow:

- `docs/qa/P1_PRODUCTION_ART_FINGERPRINTING.md`
- `docs/qa/P1_PRODUCTION_ART_GATE.md`

The production-art source/runtime paths must satisfy the same task-specific authority as the Unity pipeline. In particular, UART-004 authored 3D evidence uses exactly the three deterministic production exchange OBJ files and includes all three production Rival prefabs.

Fingerprint the template:

```bash
python3 tools/android/fingerprint_p1_production_art_manifest.py \
  --manifest evidence/p1-review/p1-production-art-template.json \
  --repo-root . \
  --output evidence/p1-review/p1-production-art.json
```

Then structurally verify it against the exact candidate:

```bash
python3 tools/android/verify_p1_production_art.py \
  --manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --expected-git-sha <EXACT_GIT_SHA> \
  --expected-apk-sha <EXACT_APK_SHA256>
```

`PRODUCTION_ART_GATE_PASSED` still means `verified=false`; it is structural/candidate/source-policy proof, not owner approval by itself.

## 6. Generate a fail-closed approval template

Do not copy candidate/review fingerprints by hand:

```bash
python3 tools/android/p1_gate_readiness.py approval-template \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --output evidence/manual-approvals.json
```

The generator cannot approve any gate. It creates all five final-gate approval records with `approved:false`.

## 7. Record schema-v2 manual approvals

Schema v1 is rejected.

Human approvals must remain pinned to:

- exact candidate Git SHA;
- exact APK SHA-256;
- verified review-bundle `contentSetSha256`.

The five approval records are:

- `UVEH-012`
- `URAC-012`
- `UPER-006`
- `UPER-009`
- `UPER-010`

Never fabricate reviewer identities or approval state.

`UPER-009` approval requires review of the candidate-bound production-art evidence as well as the physical-device visual evidence. `UPER-010` remains the release owner's final manual publication decision.

## 8. Evaluate final-five readiness

```bash
python3 tools/android/p1_gate_readiness.py validate \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/manual-approvals.json
```

The readiness evaluator remains `verified:false`; no script publishes automatically.

## 9. Run the authoritative combined publication preflight

Do **not** use `verify_release_publication.py` alone as the final P1 publication command. The authoritative P1 combined preflight is:

```bash
python3 tools/android/verify_release_with_production_art.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/manual-approvals.json \
  --production-art-manifest evidence/p1-review/p1-production-art.json \
  --repo-root . \
  --performance-tier <low|mid|high> \
  --output evidence/p1-review/combined-publication-preflight.json
```

This wrapper:

1. re-verifies candidate identity;
2. re-runs hardened production-art verification against the same Git/APK fingerprint;
3. runs the UPER-006 Android-observable smoke analyzer;
4. runs the final publication preflight.

Its strongest automatic result is only:

`ELIGIBLE_FOR_MANUAL_PUBLICATION_WITH_PRODUCTION_ART_AND_SMOKE_METRICS`, `verified=false`.

It never tags, uploads, publishes, or updates Last Verified.

## 10. Integration / publication boundary

Only after the **same exact candidate** has legitimate evidence for all 11 blockers may integration advance:

1. UART-003/004 real production source + licensed runtime proof;
2. UART-005/006/007/URAC-011 authored Player runtime proof;
3. UVEH-012/URAC-012 physical-device acceptance;
4. UPER-006 performance/smoke acceptance;
5. UPER-009 owner/Art Director production-art acceptance;
6. UPER-010 release-owner manual approval;
7. authoritative combined publication preflight eligible on the same Git/APK fingerprints.

Then, and only then:

- PR #144 may merge convergence into `agent/unblock-final-5`;
- the canonical integration line can receive its own exact-SHA proof if release policy requires it;
- PR #112 may later advance toward `main` only under the same release guardrails.

Do not merge #144/#112, publish/tag, or update Last Verified while any blocker remains.
