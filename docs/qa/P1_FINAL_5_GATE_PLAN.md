# P1 Final Five Gate Plan

This document turns the five remaining U-P1 blockers into one exact-candidate, physical-device, content-addressed evidence and human-review flow.

It does **not** remove the need for:
- licensed Unity execution on the exact current production SHA;
- a physical Android device;
- explicit human Gameplay/QA/Art approvals;
- explicit release-owner approval for publication.

## Current ledger

`IN REVIEW 60 | READY 0 | TODO 0 | BLOCKED 5 = 65`

Remaining blockers:

1. `UVEH-012` — real-device driving feel.
2. `URAC-012` — ordered lap / Results / restart device verification.
3. `UPER-006` — Android smoke/performance matrix.
4. `UPER-009` — P1 Visual Gate.
5. `UPER-010` — Verified APK publication gate.

No script in this flow is allowed to mark an APK VERIFIED automatically.

## 1. Produce one exact current candidate

Do not reuse an older APK from another SHA.

Accepted candidate types:
- `github-actions-unity-ci`;
- `local-windows-licensed-unity`.

The candidate must contain:
- full 40-character Git SHA;
- exact APK filename, size and SHA-256;
- production package `com.fiftysolutions.afareetunity3d`;
- `releaseEvidenceEligible: true`;
- `readyForDeviceEvidence: true`;
- `verified: false`;
- verdict `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`.

### Hosted GitHub Unity path

`Unity Production CI` must be completely Green on the exact SHA:
1. static/package contract;
2. Unity license preflight;
3. EditMode + PlayMode with real passing NUnit evidence;
4. Android build;
5. package/minSdk/ARM64/libunity/APK inspection;
6. `verify_ci_candidate.py`.

Never commit Unity credentials.

### Licensed Windows path

Preferred repository-traceable fallback:

**GitHub → Actions → Unity Licensed Windows Candidate → Run workflow**

Enter the then-current reviewed full SHA of `agent/unblock-final-5`.

The self-hosted job is fail-closed:
- checkout is fixed to `agent/unblock-final-5`;
- `persist-credentials: false`;
- no hard-coded SHA default;
- branch movement causes SHA mismatch before Unity starts;
- runner must be Windows x64 with licensed Unity `6000.5.8f1` and Android support.

Local equivalent:

```powershell
git fetch origin
git reset --hard origin/agent/unblock-final-5
git clean -fd

powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1 `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
```

The orchestrator must complete:
1. clean-tree check;
2. stale evidence purge;
3. LF/text-normalization preflight;
4. Unity package graph preflight;
5. EditMode + PlayMode;
6. clean-tree check after tests;
7. Android ARM64 debug APK build + inspection;
8. clean-tree check after build;
9. exact-SHA/APK candidate verification;
10. candidate manifest generation.

The resulting candidate remains `verified: false`.

## 2. Bind the candidate to a physical-device session

For a local candidate:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the bundle moved to another workstation:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

For a fully Green hosted candidate, use its `ci-candidate-manifest.json` and exact APK.

The wrapper revalidates candidate type, Git SHA, APK SHA/size, package id, release/device-evidence flags and hosted provenance when applicable.

An arbitrary direct-APK session cannot satisfy the final-five gates.

## 3. Capture the exact required checkpoints

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

Human review: steering, braking, reverse, drift entry/recovery, Nitro, collision/recovery feel.

### URAC-012 — race lifecycle

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-countdown
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-midlap
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-results
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-restart
```

Human review: ordered lap, no early finish, Results, plausible time/position, Restart and second countdown.

### UPER-006 — smoke/performance

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-cold-start
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-warm-race
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-after-restarts
```

Review logcat red flags, memory, gfxinfo, thermal and battery evidence against the approved device matrix.

### UPER-009 — Visual Gate

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hero
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-cairo
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hud
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-arabic
```

Human review: Hero Car identity/LOD, Cairo readability, HUD/SafeArea, contrast and Arabic presentation.

Any automated Fatal/ANR/native-fatal red flag blocks the final gates.

## 4. Finish collection

```bash
python3 tools/android/device_evidence.py finish \
  --session evidence/p1-device
```

The result deliberately remains:

`MANUAL_REVIEW_REQUIRED`

## 5. Export the privacy-safe content-addressed review bundle

```bash
python3 tools/android/export_device_evidence.py \
  --session evidence/p1-device \
  --output evidence/p1-review
```

The exported bundle excludes by policy:
- raw `session.json`;
- copied candidate source manifest;
- package dump;
- raw logcat;
- raw activity dump.

It also:
- rejects emulator evidence;
- validates raw ADB serial against the stored serial hashes;
- scans exported text for the raw serial;
- validates candidate manifest bytes/type/SHA;
- regenerates sanitized checkpoint metadata;
- records exact SHA-256 + byte size for every review file;
- emits deterministic `contentSetSha256`.

A clean export is still **not approval**.

## 6. Verify the transferred bundle before review

On the review workstation:

```bash
python3 tools/android/verify_device_review_bundle.py \
  --bundle evidence/p1-review \
  --expected-git-sha <exact-candidate-git-sha> \
  --expected-apk-sha <exact-candidate-apk-sha256>
```

The verifier fails on:
- changed/truncated/replaced screenshot or metrics;
- missing or unexpected files;
- raw forbidden files;
- symlinks/path traversal/non-canonical paths;
- content-set fingerprint mismatch;
- candidate Git/APK mismatch;
- evidence-index/checkpoint device/APK mismatch;
- changed privacy/manual-review contracts.

Successful verification still reports:

`verified=false` and `MANUAL_REVIEW_REQUIRED`.

## 7. Record schema-v2 manual approvals

**Schema v1 approvals are intentionally rejected.**

A human approval must be pinned to the exact evidence the reviewer saw:
- candidate Git SHA;
- candidate APK SHA-256;
- verified review bundle `contentSetSha256`.

Example:

```json
{
  "schemaVersion": 2,
  "gitSha": "<exact 40-character candidate Git SHA>",
  "apkSha256": "<exact candidate APK SHA-256>",
  "reviewContentSetSha256": "<contentSetSha256 from verified review-manifest.json>",
  "approvals": {
    "UVEH-012": {"approved": true, "reviewer": "<gameplay reviewer>"},
    "URAC-012": {"approved": true, "reviewer": "<QA reviewer>"},
    "UPER-006": {"approved": true, "reviewer": "<performance/QA reviewer>"},
    "UPER-009": {"approved": true, "reviewer": "<art/visual reviewer>"},
    "UPER-010": {"approved": true, "reviewer": "<release owner>"}
  }
}
```

Never fabricate reviewer names or approval state.

If the bundle changes, its `contentSetSha256` changes and the previous approval file no longer applies.

If the candidate SHA or APK SHA changes, the previous approval file no longer applies.

## 8. Evaluate final-five readiness

Evidence-only check:

```bash
python3 tools/android/p1_gate_readiness.py validate \
  --session evidence/p1-device
```

Evidence + exact review bundle + approvals:

```bash
python3 tools/android/p1_gate_readiness.py validate \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals /path/to/manual-approvals.json
```

The evaluator independently invokes the offline review-bundle verifier before accepting any approval.

The first four gates can become `MANUALLY_APPROVED` only when:
- candidate binding is valid;
- physical-device checkpoints are complete;
- automated red flags are zero;
- the review bundle verifies for the same Git/APK candidate;
- approval schema is v2;
- approval Git SHA matches;
- approval APK SHA matches;
- approval `reviewContentSetSha256` matches the verified review bundle;
- the task has `approved: true` and a non-empty reviewer.

`UPER-010` can become `READY_FOR_RELEASE_REVIEW` only after all four dependent approvals and its own explicit release-owner approval are bound to that same candidate/evidence fingerprint.

Even then:

- `p1_gate_readiness.py` emits `verified: false`;
- no script publishes a release automatically;
- `Last Verified APK` promotion remains an explicit release-policy action.

## Human-only blocker

The implementation/tooling path can prepare and verify evidence, but it cannot perform subjective driving/visual review or physically connect a device.

Smallest remaining human sequence:
1. dispatch/run licensed Unity on the then-current exact production SHA;
2. connect an authorized physical Android device;
3. capture all required checkpoints;
4. export and verify the content-addressed review bundle;
5. Gameplay/QA/Art reviewers inspect that exact bundle;
6. record schema-v2 approvals;
7. release owner approves `UPER-010`;
8. run readiness evaluator and then follow release policy.

No final task is promoted before that evidence exists.