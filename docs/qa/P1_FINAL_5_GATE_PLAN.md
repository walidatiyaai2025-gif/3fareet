# P1 Final Five Gate Plan

This document turns the five remaining U-P1 blockers into one deterministic candidate/device evidence and human-review flow. It does **not** remove the need for a current exact-head APK, a physical Android device, or explicit human approvals.

## Current ledger

`IN REVIEW 60 | READY 0 | TODO 0 | BLOCKED 5 = 65`

Remaining blockers:

1. `UVEH-012` — real-device driving feel.
2. `URAC-012` — ordered lap/results/restart device verification.
3. `UPER-006` — Android smoke/performance matrix.
4. `UPER-009` — P1 Visual Gate.
5. `UPER-010` — Verified APK publication gate.

`U3D-012` is `IN REVIEW`, not one of the final five. GitHub-hosted Unity execution still depends on Issue #98 licensing.

## Prerequisite — exact current candidate

Do not reuse an older APK from another commit/ancestry as final evidence. Produce one candidate through either the fully Green hosted path or the canonical licensed-Windows path.

### Hosted GitHub path

Configure one complete Unity credential set:

- Personal/file-license: `UNITY_LICENSE + UNITY_EMAIL + UNITY_PASSWORD`; or
- Professional: `UNITY_SERIAL + UNITY_EMAIL + UNITY_PASSWORD`.

Never commit secrets.

A valid hosted candidate requires the then-current `Unity Production CI` to execute and pass:

1. static/package contract;
2. license preflight;
3. EditMode + PlayMode with real passing NUnit evidence;
4. Android build;
5. package/minSdk/ARM64/libunity/APK SHA inspection;
6. `verify_ci_candidate.py` candidate binding.

The resulting `artifacts/android/ci-candidate-manifest.json` remains `verified: false`.

### Licensed-Windows fallback

On licensed Unity `6000.5.8f1` Windows, start from the exact current branch head:

```powershell
git fetch origin
git reset --hard origin/agent/unblock-final-5
git clean -fd
powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1 `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
```

The release-evidence orchestrator is mandatory. Its sequence is:

1. initial clean-tree check;
2. stale candidate-evidence purge;
3. **Unity text-normalization preflight**;
4. clean-tree recheck;
5. **Unity package manifest/lock preflight**;
6. clean-tree recheck;
7. EditMode + PlayMode;
8. clean-tree recheck;
9. Android build + inspection;
10. clean-tree recheck;
11. local candidate integrity verification;
12. final clean-tree recheck.

Required preflight markers before Unity is allowed to start:

```text
AFAREET_TEXT_NORMALIZATION_PREFLIGHT_START
AFAREET_UNITY_TEXT_NORMALIZATION_OK
AFAREET_TEXT_NORMALIZATION_PREFLIGHT_OK
AFAREET_PACKAGE_PREFLIGHT_START
AFAREET_UNITY_PACKAGE_LOCK_OK
AFAREET_PACKAGE_PREFLIGHT_OK
```

Text normalization validates every tracked Unity ProjectSettings/package metadata file covered by the repository contract for explicit text normalization, `eol=lf`, working-tree presence and absence of CRLF bytes. Package verification validates manifest/lock versions/depth plus known resolved child packages.

A failure in either preflight is a candidate failure and must be fixed **before** running Unity. Do not bypass the preflights or downgrade them to warnings.

After Unity runs, any tracked drift also fails the candidate. Exact status/binary patch/stderr evidence is retained under ignored `artifacts/logs/`; reconcile only legitimate generated/source changes and restart from a clean exact head.

Successful local orchestration emits:

```text
AFAREET_LOCAL_CANDIDATE_OK
```

and produces:

```text
artifacts/local-candidate-manifest.json
```

That manifest must be release-evidence eligible, `readyForDeviceEvidence: true`, and `verified: false`.

Independent success of `test_current_windows.ps1`, `build_current_windows.ps1` or `verify_local_candidate.py` is diagnostic evidence only; it is not a substitute for the orchestrated same-SHA clean candidate chain.

## Prepare physical-device evidence

### Local candidate

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the candidate bundle moved to another workstation, also provide the exact APK:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

### Fully Green hosted candidate

Download the exact Android artifact bundle from the fully Green `Unity Production CI` run, then use both its candidate manifest and exact APK:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/ci-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

Do **not** pass a downloaded or local APK directly to `device_evidence.py prepare` for final-five evidence.

Before ADB installation, the candidate-aware wrapper revalidates:

- supported candidate type;
- production package id;
- `READY_FOR_PHYSICAL_DEVICE_EVIDENCE` verdict;
- `releaseEvidenceEligible: true`;
- `readyForDeviceEvidence: true`;
- `verified: false`;
- full Git SHA;
- exact APK filename, size and SHA-256;
- hosted repository/workflow/event/ref/run provenance when applicable.

Expected marker:

```text
AFAREET_CANDIDATE_DEVICE_PRECHECK_OK
```

After prepare, the session retains a copied `candidate-manifest.json`, candidate-manifest SHA, candidate type, Git SHA and APK SHA. Expected binding marker:

```text
AFAREET_CANDIDATE_SESSION_BOUND
```

An unbound direct-APK session is not eligible for the final-five gates.

## Required captures

Use the exact labels below.

### UVEH-012 — driving feel

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drive-straight
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label brake-reverse
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label drift
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label nitro
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label reset-recovery
```

Manual review must approve steering, braking, reverse, drift, Nitro and recovery feel on a physical device.

### URAC-012 — race lifecycle

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-countdown
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-midlap
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-results
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label race-restart
```

Manual review must complete the ordered lap, see Results, restart, and confirm the second countdown/race lifecycle.

### UPER-006 — smoke/performance

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-cold-start
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-warm-race
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label smoke-after-restarts
```

Review logcat, memory, gfxinfo, thermal and battery evidence. Any automated crash/ANR/native-fatal red flag blocks every final gate.

### UPER-009 — Visual Gate

```bash
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hero
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-cairo
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-hud
python3 tools/android/device_evidence.py capture --session evidence/p1-device --label visual-arabic
```

Art/owner review must approve Hero readability, Cairo identity, HUD/SafeArea readability and Arabic presentation.

## Finish collection

```bash
python3 tools/android/device_evidence.py finish --session evidence/p1-device
```

The harness deliberately returns `MANUAL_REVIEW_REQUIRED`.

## Evaluate readiness

```bash
python3 tools/android/p1_gate_readiness.py plan
python3 tools/android/p1_gate_readiness.py validate --session evidence/p1-device
```

Before evaluating captures or approvals, readiness fails closed unless the session remains candidate-bound and consistent with the copied manifest:

- supported candidate type;
- full Git SHA;
- candidate APK SHA equals evidence-index APK SHA;
- copied manifest SHA equals the value stored in `session.json`;
- candidate type/Git SHA/APK SHA/release-evidence state all match;
- hosted candidates retain valid GitHub provenance;
- candidate remains `verified: false` and `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`.

With a valid candidate binding, all required captures present and no automated red flags, the first four gates may become `EVIDENCE_READY_FOR_MANUAL_REVIEW`. That is not approval.

## Manual approvals

Create approvals pinned to the exact APK SHA. Never fabricate reviewer names or approval state.

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

Then:

```bash
python3 tools/android/p1_gate_readiness.py validate \
  --session evidence/p1-device \
  --approvals /path/to/manual-approvals.json
```

`UPER-010` can only reach `READY_FOR_RELEASE_REVIEW` when:

- the APK came from an accepted exact-head candidate path;
- candidate binding remains valid;
- the session is physical-device evidence, not emulator evidence;
- all required checkpoints exist;
- automated crash/ANR/native-fatal flags are zero;
- `UVEH-012`, `URAC-012`, `UPER-006`, and `UPER-009` each have explicit human approval;
- approvals are pinned to the same APK SHA;
- `UPER-010` has explicit release-owner approval.

Even then the evaluator emits `verified: false`. Publication and `Last Verified APK` promotion remain an explicit release-policy decision.

## External blocker

Issue #98 remains open while GitHub-hosted Unity lacks a complete credential triple. The licensed-Windows path can produce exact-head test/build evidence, but it does not make hosted `Unity Production CI` Green and does not replace the physical-device/manual gates.