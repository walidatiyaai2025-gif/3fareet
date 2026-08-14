# P1 Final Five Gate Plan

This document turns the five remaining U-P1 blockers into one deterministic evidence/review flow. It does **not** remove the need for a current Android APK, a physical device, or human approval.

## Current ledger

`IN REVIEW 60 | READY 0 | TODO 0 | BLOCKED 5 = 65`

The five remaining blocked tasks are:

1. `UVEH-012` — real-device driving feel pass.
2. `URAC-012` — track/lap/results/restart device verification.
3. `UPER-006` — Android smoke/performance matrix.
4. `UPER-009` — P1 Visual Gate.
5. `UPER-010` — Verified APK publication gate.

`U3D-012` is no longer one of these five: the Android CI workflow exists and is `IN REVIEW`; GitHub-hosted Unity engine execution is currently blocked by repository licensing secrets tracked in Issue #98.

## Prerequisite — exact current candidate

Do not reuse an older APK from a different ancestry as final evidence. Obtain exact-head automated Unity test evidence and an inspected Android APK through one of the supported paths.

### Preferred GitHub-hosted path

Configure one complete Unity/GameCI credential set and require the current `Unity Production CI` run to execute and pass the applicable Unity tests/build/APK verification.

Supported complete credential sets are:

- Personal/file-license: `UNITY_LICENSE + UNITY_EMAIL + UNITY_PASSWORD`; or
- Professional: `UNITY_SERIAL + UNITY_EMAIL + UNITY_PASSWORD`.

Secrets must never be committed to Git.

### Licensed-Windows fallback

On an already licensed Unity `6000.5.8f1` Windows workstation, from the same clean exact commit:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/test_current_windows.ps1
powershell -ExecutionPolicy Bypass -File tools/android/build_current_windows.ps1
```

Then require the machine-checked same-candidate integrity gate:

```bash
python3 tools/android/verify_local_candidate.py \
  --test-metadata artifacts/unity-local-tests/test-metadata.json \
  --build-metadata artifacts/android-local/artifact-metadata.json \
  --apk artifacts/android-local/afareet-unity3d-debug.apk \
  --output artifacts/local-candidate-manifest.json
```

A successful local manifest must say `readyForDeviceEvidence: true` and still says `verified: false`. It proves clean same-SHA test/build/APK integrity only.

## Prepare physical-device evidence

### Local licensed-Windows candidate

Do not bypass the candidate manifest by manually selecting an APK. Bind the physical-device session to the exact integrity-checked candidate:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the evidence bundle was copied to another machine, pass the moved APK explicitly; its filename, byte length and SHA-256 still must match the manifest exactly:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

The wrapper fails before ADB install unless the candidate is release-evidence eligible, `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`, non-self-VERIFIED, pinned to a full Git SHA, and the actual APK bytes match the manifest.

### Fully Green GitHub-hosted candidate

Once `Unity Production CI` is fully Green and its exact APK artifact has passed the workflow verifier, prepare the downloaded exact artifact directly:

```bash
python3 tools/android/device_evidence.py prepare \
  --apk /path/to/exact-ci-artifact.apk \
  --output evidence/p1-device
```

Retain the GitHub run/commit/artifact metadata alongside the device evidence. The harness hashes the APK again and pins the session to that exact SHA plus the physical device identity.

## Required captures

Use the exact labels below so the readiness evaluator can determine evidence completeness.

### UVEH-012 — driving feel

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drive-straight
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label brake-reverse
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drift
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label nitro
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label reset-recovery
```

Manual reviewer must confirm steering, braking, reverse, drift, Nitro and recovery feel are acceptable on the physical device.

### URAC-012 — race completion

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-countdown
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-midlap
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-results
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-restart
```

Manual reviewer must complete the ordered lap, see Results, restart and confirm the second countdown/race lifecycle.

### UPER-006 — smoke/performance

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-cold-start
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-warm-race
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-after-restarts
```

Review logcat, memory, gfxinfo, thermal and battery evidence. Any crash/ANR/native-fatal automated red flag blocks every final gate.

### UPER-009 — Visual Gate

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hero
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-cairo
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hud
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-arabic
```

Art/owner review must approve Hero readability, Cairo identity, HUD/SafeArea readability and Arabic presentation.

## Finish evidence collection

```bash
python3 tools/android/device_evidence.py finish --session evidence/p1-device
```

The harness deliberately returns `MANUAL_REVIEW_REQUIRED`.

## Check evidence readiness

```bash
python3 tools/android/p1_gate_readiness.py plan
python3 tools/android/p1_gate_readiness.py validate --session evidence/p1-device
```

The evaluator writes `p1-gate-readiness.json`. With all captures present and no automated red flags, the first four gates become `EVIDENCE_READY_FOR_MANUAL_REVIEW`; they are **not** automatically approved.

## Manual approvals contract

Create a local approvals file pinned to the exact APK SHA. Do not fabricate reviewer names or approval state.

```json
{
  "schemaVersion": 1,
  "apkSha256": "<exact sha256 from evidence-index.json>",
  "approvals": {
    "UVEH-012": {"approved": true, "reviewer": "<name>"},
    "URAC-012": {"approved": true, "reviewer": "<name>"},
    "UPER-006": {"approved": true, "reviewer": "<name>"},
    "UPER-009": {"approved": true, "reviewer": "<name>"},
    "UPER-010": {"approved": true, "reviewer": "<release owner>"}
  }
}
```

Then run:

```bash
python3 tools/android/p1_gate_readiness.py validate \
  --session evidence/p1-device \
  --approvals /path/to/manual-approvals.json
```

`UPER-010` can only reach `READY_FOR_RELEASE_REVIEW` when:

- the APK came from an accepted exact-head candidate path;
- the session is from a physical device;
- all required checkpoints exist;
- automated fatal/crash/ANR red flags are zero;
- UVEH-012, URAC-012, UPER-006 and UPER-009 each have explicit human approval;
- approvals are pinned to the same APK SHA;
- UPER-010 has an explicit release-owner approval.

Even then the evaluator emits `verified: false`. Publication and `Last Verified APK` promotion remain a separate release-policy decision.

## External blocker still open

Issue #98 remains open while GitHub-hosted Unity execution lacks one complete credential triple. The licensed-Windows path can produce exact-head test/build evidence, but it does not make GitHub `Unity Production CI` Green and does not replace physical-device/manual gates.
