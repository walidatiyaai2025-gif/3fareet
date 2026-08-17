# Android Device Evidence Harness

## Purpose

`tools/android/device_evidence.py` standardizes evidence collection from a 3Fareet APK and an Android device. It is a **collection tool, not an automatic QA approval tool**.

It supports evidence for:

- `UVEH-012` — real-device driving feel;
- `URAC-012` — ordered lap / Results / restart verification;
- `UPER-006` — Android smoke/performance/device matrix;
- `UPER-009` — owner/Art Director visual review;
- `UPER-010` — later consumes approved evidence but is never published by this harness.

A successful harness command does not move a task out of BLOCKED, approve a gate, publish a release, update Last Verified, or call an APK VERIFIED.

## P1/release rule: candidate-bound preparation is mandatory

For P1 acceptance, release review, or publication evidence, do **not** start with raw `device_evidence.py prepare`.

The authoritative flow starts with `prepare_candidate_device.py`, which binds one exact candidate, one physical-device session and one UPER-001 performance tier before any checkpoint capture:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device \
  --performance-tier mid
```

If the candidate bundle moved workstations:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device \
  --performance-tier mid
```

Choose the actual approved device capability tier from `low`, `mid`, or `high` according to `docs/performance/UNITY_DEVICE_TIERS.md`.

The wrapper revalidates the candidate full Git SHA, APK SHA-256/size, package id, candidate type, device-evidence eligibility, hosted provenance where applicable, and `verified=false`. It then persists `session.performanceTier`. Later UPER-006 analysis must use exactly that same tier.

An arbitrary direct-APK session created with raw `device_evidence.py prepare` cannot satisfy the P1/release evidence chain.

The complete current sequence is documented in [`P1_FINAL_5_GATE_PLAN.md`](P1_FINAL_5_GATE_PLAN.md). That document also records the current 11-blocker ledger and the six production-art/runtime prerequisites that must pass before release.

## Requirements

- Python 3.10+;
- Android platform tools with `adb` on `PATH`;
- one authorized physical Android device, or `--serial` when multiple devices are connected;
- the exact candidate APK;
- for P1/release evidence, the validated candidate manifest for that exact APK.

By default emulators are rejected because P1 acceptance requires physical-device evidence. `--allow-emulator` exists only for harness debugging and cannot satisfy P1 gates.

## Generic/non-release preparation

Raw preparation remains available for harness development, diagnostics or non-release collection:

```bash
python3 tools/android/device_evidence.py prepare \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output artifacts/device-evidence/pixel8-run1
```

Generic preparation computes the APK SHA-256/size, selects the exact ADB device, records device characteristics, rejects emulators by default, installs and launches the package, and writes `session.json` plus the package dump.

Use the candidate-bound wrapper for all P1/release evidence.

## Capture checkpoints

The QA operator drives/uses the game normally and calls `capture` when a required state is visible. The exact P1 labels are defined by `tools/android/p1_gate_spec.json` and [`P1_FINAL_5_GATE_PLAN.md`](P1_FINAL_5_GATE_PLAN.md).

Generic example:

```bash
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label start
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label racing-2min
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label results
```

Each checkpoint records:

- `screen.png`;
- `logcat.txt`;
- `meminfo.txt`;
- `gfxinfo.txt`;
- `thermalservice.txt`;
- `battery.txt`;
- current activity state;
- `checkpoint.json` pinned to APK/device identity.

The collector scans for Fatal Exception, package ANR, native fatal signal and Unity fatal/crash lines. Red flags return non-zero, but absence of red flags is not a PASS verdict.

## Finish the session

```bash
python3 tools/android/device_evidence.py finish \
  --session evidence/p1-device
```

`finish` creates `evidence-index.json` and keeps the verdict:

`MANUAL_REVIEW_REQUIRED`

until the responsible reviewers make task-specific decisions.

## UPER-006 smoke analysis

For a candidate-bound P1 session, the Android-observable performance analyzer must use the same tier selected during preparation:

```bash
python3 tools/android/analyze_device_smoke.py \
  --session evidence/p1-device \
  --tier mid
```

The analyzer fails closed when `session.performanceTier` is missing/invalid or differs from the requested tier. It evaluates APK/device fingerprint integrity, crash/ANR red flags, PSS, `gfxinfo` P95/P99, restart memory growth and Android thermal status. It does not invent Unity main/render/GPU profiler evidence or sustained-device review.

Valid automated verdicts are only `BLOCKED` and `PASSABLE_FOR_MANUAL_REVIEW`; `verified` stays `false`.

## Task-specific manual review

### UVEH-012
Review acceleration, braking/reverse, steering, drift entry/recovery, Nitro, collisions and reset behavior on a physical device. Tooling cannot decide whether driving feels correct.

### URAC-012
Complete a real ordered lap. Confirm skipped/early checkpoints cannot finish, Results appear only after a legitimate finish, position/time are plausible, Restart returns to grid and the second countdown starts correctly.

### UPER-006
Review cold start, warm race, repeated restarts, crash/ANR scan, memory, frame-time, thermal and representative device behavior against the bound Low/Mid/High tier. Add required Unity profiler/sustained evidence separately.

### UPER-009
Review the exact candidate for authored Hero/rivals/Cairo/landmarks/dressing, no accepted primitive/blockout fallback, HUD/SafeArea, contrast and Arabic/English presentation.

### UPER-010
Never publish from harness output alone. Publication requires the exact candidate, licensed Unity evidence, production-art acceptance, required device/performance/visual approvals and the authoritative combined preflight.

## Export a privacy-safe review bundle

After a candidate-bound session is finished:

```bash
python3 tools/android/export_device_evidence.py \
  --session evidence/p1-device \
  --output evidence/p1-review
```

The exporter validates candidate/device/checkpoint binding, rejects emulator evidence and excludes raw `session.json`, candidate manifest, package dump, logcat and activity dump from the default shareable bundle. It emits deterministic content-file SHA-256/size metadata and `contentSetSha256`.

A clean export is still `MANUAL_REVIEW_REQUIRED`.

## Verify a transferred review bundle

```bash
python3 tools/android/verify_device_review_bundle.py \
  --bundle evidence/p1-review \
  --expected-git-sha <exact-candidate-40-char-sha> \
  --expected-apk-sha <exact-candidate-apk-sha256>
```

The verifier fails on missing/changed/unexpected files, forbidden raw files, symlinks/path traversal, content-set mismatch, candidate mismatch, evidence-index/checkpoint binding disagreement, or changed manual-review/privacy contracts.

Successful verification still reports `verified=false` and `MANUAL_REVIEW_REQUIRED`.

## Release/publication handoff

After candidate-bound device evidence, review-bundle verification, manual approvals, production-art evidence and UPER-006 metrics are legitimately complete, run:

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

The combined preflight requires the requested performance tier to match the one already bound to the physical-device session. A successful result means only that the exact candidate is eligible for explicit manual publication review. It still emits `verified=false`; publication and Last Verified promotion remain human/release-policy actions.

## Evidence handling

The local `session.json` contains the raw ADB serial for repeatability. `evidence-index.json` and the sanitized review bundle expose only the serial hash plus non-sensitive device characteristics.

Do not commit generated device-evidence folders. Store approved evidence according to the release/QA policy for the exact Git/APK candidate being reviewed.
