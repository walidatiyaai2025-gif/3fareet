# P1 Final Device / Release Gate Plan

This document defines the exact-candidate path for the final device, visual-review and release gates after the production-art source/runtime prerequisites are satisfied.

It never replaces licensed Unity execution, a physical Android device, owner/art review, Gameplay/QA review, or explicit release-owner approval. No script in this flow may mark an APK VERIFIED automatically.

## Current source of truth

The fixed U-P1 register remains 65 tasks. Current operational state is:

`IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`

The 11 blockers are:

1. `UART-003` — real authored Hero production model + licensed binding/render proof.
2. `UART-004` — licensed Rival production prefab/runtime/owner proof.
3. `UART-005` — licensed Cairo street/runtime/device/owner proof.
4. `UART-006` — licensed landmark runtime/device/owner proof.
5. `UART-007` — licensed dressing runtime/device/owner proof.
6. `URAC-011` — exact-candidate authored Cairo layout runtime/device/owner proof.
7. `UVEH-012` — real-device driving-feel acceptance.
8. `URAC-012` — physical-device lap / Results / restart verification.
9. `UPER-006` — Android smoke/profiler/performance matrix.
10. `UPER-009` — owner / Art Director Visual Gate.
11. `UPER-010` — final manual publication approval.

The historical name “final five” refers to the device/review/release tail (`UVEH-012`, `URAC-012`, `UPER-006`, `UPER-009`, `UPER-010`). The six production-art/runtime blockers must also be satisfied on the same future candidate before publication.

Issue #90 remains the operational ledger. Defects #127 and #128 are blockers against existing tasks, not task 66/67.

## 0. Production-art prerequisites

Before generating a release-evidence candidate:

1. Commit the real owner-approved externally-authored Afareet King source package under the accepted Unity `Assets/` source path.
2. Run `tools/android/p1_licensed_staging_readiness.py` and require `READY_FOR_LICENSED_STAGING`.
3. Run the licensed staging handoff from #157 so Hero/Rival source imports, prefabs and provenance are generated from the tracked authored sources.
4. Review and commit the staged Unity outputs. The later candidate must be built from the resulting new clean Git SHA, not from dirty/uncommitted staged bytes.
5. Keep procedural/blockout paths as dev/emergency fallback only; they cannot satisfy the production-art gate.

## 1. Produce one exact current candidate

Do not reuse an APK or device evidence from another SHA.

Accepted candidate types are:

- `github-actions-unity-ci`;
- `local-windows-licensed-unity`.

The candidate manifest must contain:

- full 40-character Git SHA;
- exact APK filename, size and SHA-256;
- package `com.fiftysolutions.afareetunity3d`;
- `releaseEvidenceEligible: true`;
- `readyForDeviceEvidence: true`;
- `verified: false`;
- verdict `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`.

The exact SHA must pass licensed Unity EditMode/PlayMode tests, including the URAC-012 restart regression, and produce the Android ARM64 candidate from that same SHA.

A Unity license-preflight failure is infrastructure state; it is not a gameplay failure and cannot be reported as Verified runtime coverage.

## 2. Bind the candidate, physical device and performance tier

The P1/release evidence chain must start with `prepare_candidate_device.py`, never raw `device_evidence.py prepare`.

For a local candidate:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device \
  --performance-tier mid
```

If the candidate bundle moved to another workstation:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device \
  --performance-tier mid
```

Use the actual approved capability tier for the connected physical device: `low`, `mid`, or `high`, according to `docs/performance/UNITY_DEVICE_TIERS.md`.

The wrapper revalidates candidate type, Git SHA, APK SHA/size, package id and candidate provenance, then persists `session.performanceTier`. Later UPER-006 analysis must use the same tier; missing or post-capture tier substitution fails closed.

## 3. Capture the exact 16 P1 checkpoints

The declarative source of truth is `tools/android/p1_gate_spec.json`.

### UVEH-012 — 5 driving checkpoints

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drive-straight
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label brake-reverse
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drift
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label nitro
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label reset-recovery
```

Human review covers steering, acceleration, braking/reverse, drift entry/recovery, Nitro, collision/recovery behavior and overall driving feel.

### URAC-012 — 4 race-lifecycle checkpoints

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-countdown
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-midlap
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-results
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-restart
```

Human review covers ordered checkpoints, legitimate finish, plausible position/time, Results, Retry/Restart, reset to grid and fresh countdown.

### UPER-006 — 3 smoke/performance checkpoints

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-cold-start
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-warm-race
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-after-restarts
```

Review crash/ANR/native-fatal scan, PSS, `gfxinfo`, thermal and battery evidence against the session-bound capability tier. Unity main/render/GPU profiler and sustained-device evidence remain separately required where the UPER-001/UPER-006 policy requires them.

### UPER-009 — 4 Visual Gate checkpoints

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hero
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-cairo
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hud
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-arabic
```

Human review must confirm the accepted authored Hero/rivals/Cairo/landmarks/dressing path is active, no accepted blockout/procedural fallback is supplying the visual proof, and HUD/SafeArea/contrast/Arabic presentation are acceptable.

Any automated Fatal/ANR/native-fatal red flag blocks the gate chain.

## 4. Finish and index the raw physical-device session

```bash
python3 tools/android/device_evidence.py finish \
  --session evidence/p1-device
```

The result remains `MANUAL_REVIEW_REQUIRED`.

## 5. Run deterministic UPER-006 Android-observable analysis

```bash
python3 tools/android/analyze_device_smoke.py \
  --session evidence/p1-device \
  --tier mid
```

The requested tier must exactly match `session.performanceTier`. Valid automated verdicts are only:

- `BLOCKED`;
- `PASSABLE_FOR_MANUAL_REVIEW`.

`verified` remains `false`.

## 6. Export and verify the privacy-safe review bundle

Export:

```bash
python3 tools/android/export_device_evidence.py \
  --session evidence/p1-device \
  --output evidence/p1-review
```

Verify after transfer:

```bash
python3 tools/android/verify_device_review_bundle.py \
  --bundle evidence/p1-review \
  --expected-git-sha <exact-candidate-git-sha> \
  --expected-apk-sha <exact-candidate-apk-sha256>
```

The exporter/verifier bind the review content to the exact candidate/device/checkpoint inventory and keep raw ADB serial/logcat/activity data out of the default shareable bundle. Clean verification still means `MANUAL_REVIEW_REQUIRED`, not approval.

## 7. Generate the fail-closed manual approval template

```bash
python3 tools/android/p1_gate_readiness.py approval-template \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --output evidence/manual-approvals.json
```

The generator fills candidate/review fingerprints and creates all approval records with `approved: false`. It cannot approve a gate and must never fabricate reviewer names.

Manual approvals must be bound to:

- exact candidate Git SHA;
- exact candidate APK SHA-256;
- exact verified review-bundle `contentSetSha256`.

If any of those change, the previous approvals no longer apply.

## 8. Produce candidate-bound production-art evidence

Use the schema-v2 production-art workflow and `tools/android/verify_p1_production_art.py` against the same candidate Git/APK fingerprint.

Required production-art tasks are:

- `UART-003`;
- `UART-004`;
- `UART-005`;
- `UART-006`;
- `UART-007`;
- `URAC-011`.

The gate must prove tracked authored source files, packaged runtime assets, screenshot/video evidence, no accepted procedural/blockout fallback, and explicit owner acceptance. Structural/static success alone does not satisfy UPER-009.

## 9. Run the authoritative combined preflight

```bash
python3 tools/android/verify_release_with_production_art.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/manual-approvals.json \
  --production-art-manifest <candidate-bound-production-art-manifest.json> \
  --performance-tier mid \
  --repo-root .
```

Use `--apk /path/to/afareet-unity3d-debug.apk` if the exact bundle moved workstations.

The combined preflight requires production-art acceptance, session-bound UPER-006 metrics, exact candidate/device/review fingerprints and manual gate approvals. A successful result means only **eligible for explicit manual publication review** and still emits `verified=false`.

## 10. UPER-010 remains human-only and last

Only after the same exact candidate has legitimately satisfied:

- all six production-art/runtime blockers;
- UVEH-012;
- URAC-012;
- UPER-006;
- UPER-009;
- combined publication preflight;

may the release owner approve `UPER-010` and follow the repository release policy.

Do not automatically tag, publish, update `Last Verified APK`, merge the release convergence to main, or call an APK VERIFIED while any required evidence/approval is missing.
