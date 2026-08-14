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

`U3D-012` is no longer one of these five: the Android CI workflow exists and is `IN REVIEW`; Unity engine execution is currently blocked by repository licensing secrets tracked in Issue #98.

## Prerequisite

Generate the Android APK from the latest production stack head after Unity/GameCI licensing is configured. Do not reuse an older APK from a different ancestry as final evidence.

Then prepare a physical-device evidence session:

```bash
python3 tools/android/device_evidence.py prepare \
  --apk /path/to/current.apk \
  --output evidence/p1-device
```

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

- the session is from a physical device;
- all required checkpoints exist;
- automated fatal/crash/ANR red flags are zero;
- UVEH-012, URAC-012, UPER-006 and UPER-009 each have explicit human approval;
- approvals are pinned to the same APK SHA;
- UPER-010 has an explicit release-owner approval.

Even then the evaluator emits `verified: false`. Publication and `Last Verified APK` promotion remain a separate release-policy decision.

## External blocker still open

Issue #98 must be resolved first by configuring one supported Unity/GameCI Actions licensing path:

- `UNITY_LICENSE`, or
- `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL`.

Secrets must never be committed to Git.
