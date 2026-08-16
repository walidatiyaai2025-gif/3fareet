# Android Device Evidence Harness

## Purpose

`tools/android/device_evidence.py` standardizes evidence collected from a 3Fareet APK and a physical Android device. It is a **collection tool, not an automatic QA approval tool**.

It supports evidence for:

- `UVEH-012` — real-device driving feel;
- `URAC-012` — ordered lap / Results / restart verification;
- `UPER-006` — Android smoke/performance/device matrix;
- `UPER-009` — P1 Visual Gate screenshots;
- `UPER-010` — later consumes approved evidence but is never published by this harness.

A successful harness command does **not** move any task out of BLOCKED, approve a visual/performance gate, publish a release, or call an APK VERIFIED.

## Release/P1 rule: candidate-bound preparation is mandatory

For **P1 acceptance, release review, or publication evidence**, do **not** start with raw `device_evidence.py prepare`.

The authoritative flow is documented in [`P1_FINAL_5_GATE_PLAN.md`](P1_FINAL_5_GATE_PLAN.md) and starts by binding the physical-device session to the already-validated candidate manifest:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the candidate bundle moved to another workstation, provide the exact APK explicitly:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

`prepare_candidate_device.py` revalidates and binds the session to the exact full Git SHA, APK SHA-256/size, package id, candidate type, device-evidence eligibility, and `verified=false` state before delegating physical-device preparation to the harness.

An arbitrary direct-APK session created with raw `device_evidence.py prepare` **cannot satisfy the P1/release evidence chain** and will be rejected by the later export/readiness/publication checks.

After candidate-bound capture, review, approvals, production-art evidence, and UPER-006 smoke evidence are complete, the authoritative combined manual-publication preflight is:

`tools/android/verify_release_with_production_art.py`

That preflight requires the same exact candidate/session/review/approval fingerprints plus production-art evidence and a `low|mid|high` performance tier. It remains fail-closed and returns `verified=false`; it never publishes automatically.

## Requirements

- Python 3.10+;
- Android platform tools with `adb` on `PATH`;
- one authorized physical Android device, or pass `--serial` when multiple devices are connected;
- the exact APK being reviewed;
- for P1/release evidence, the validated candidate manifest for that exact APK.

By default emulators are rejected because P1 acceptance gates require physical-device evidence. `--allow-emulator` exists only to debug the harness itself.

## 1. Generic/non-release harness preparation

Raw preparation remains useful for harness development, diagnostics, or other **non-release/general evidence collection**:

```bash
python3 tools/android/device_evidence.py prepare \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output artifacts/device-evidence/pixel8-run1
```

Do not use this raw form as the starting point for P1/release acceptance. For that path, use the candidate-bound wrapper shown above.

Generic `prepare`:

- computes APK SHA-256 and size;
- selects/records the exact ADB device;
- records manufacturer/model/Android/API/ABI/display metadata;
- rejects emulators by default;
- installs the APK with `adb install -r -t`;
- confirms `com.fiftysolutions.afareetunity3d` is installed;
- clears logcat, force-stops and launches the app;
- writes `session.json` and the installed package dump.

The session remains pinned to the APK SHA and device serial hash for every later capture, but only the candidate-bound wrapper adds the release candidate identity/provenance required by P1 gates.

## 2. Capture named manual checkpoints

The QA operator drives/uses the game normally and invokes `capture` when a required state is visible.

For the exact P1 checkpoint labels, use [`P1_FINAL_5_GATE_PLAN.md`](P1_FINAL_5_GATE_PLAN.md) and `tools/android/p1_gate_spec.json` as the declarative sources of truth.

A generic/non-release example is:

```bash
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label start
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label racing-2min
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label results
python3 tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label restart-countdown
```

For each checkpoint the harness records:

- `screen.png`;
- `logcat.txt`;
- package `meminfo.txt`;
- package `gfxinfo.txt`;
- `thermalservice.txt`;
- `battery.txt`;
- current activity state;
- `checkpoint.json` pinned to APK/device identity.

It scans logcat for obvious Fatal Exception, package ANR, native fatal signal and Unity fatal/crash lines. A red flag makes the capture command return non-zero, but **absence of red flags is not a PASS verdict**.

## 3. Finish and index the evidence

```bash
python3 tools/android/device_evidence.py finish \
  --session artifacts/device-evidence/pixel8-run1
```

For a P1/release run, use the candidate-bound session path instead, for example `evidence/p1-device`.

`finish` creates `evidence-index.json` with:

- APK SHA;
- hashed device serial + non-sensitive device characteristics;
- checkpoint labels;
- automated red flags;
- the manual review checklist.

The verdict is always:

```text
MANUAL_REVIEW_REQUIRED
```

until the responsible QA/Gameplay/Art reviewers record the task-specific decision.

## Task-specific manual review

### UVEH-012
Review acceleration, braking/reverse, steering, drift entry/recovery, Nitro, collisions and reset behavior on a physical device. The harness cannot decide whether driving *feels* correct.

### URAC-012
Complete a real ordered lap. Confirm skipped/early checkpoints cannot finish, Results appear at legitimate finish, position/time are plausible, Restart returns to the grid, and the second countdown starts correctly.

### UPER-006
Review startup stability, Fatal/ANR scan, memory/gfx captures, sustained thermal behavior and representative devices against the approved Low/Mid/High device matrix.

### UPER-009
Review screenshots for Hero Car identity/LOD defects, Cairo readability, race line, HUD/SafeArea, Pause/Results overlays, contrast, Arabic/English presentation and visible rendering regressions.

### UPER-010
Do not publish based on harness output alone. Publication requires the exact APK SHA, successful exact-head build/test evidence, production-art acceptance, required device/performance/visual approvals, and the authoritative combined publication preflight.

## Evidence handling

The local `session.json` keeps the raw ADB serial for repeatability on the QA machine. `evidence-index.json` exposes only the serial SHA-256 plus device characteristics. Review before attaching raw local evidence publicly.

Do not commit generated `artifacts/device-evidence/**` or local `evidence/**` capture folders to Git. Store approved evidence according to the release/QA process for the exact APK/commit being promoted.

## 4. Export a privacy-safe review bundle

After a **candidate-bound** session is finished, generate the default shareable review bundle with:

```bash
python3 tools/android/export_device_evidence.py \
  --session evidence/p1-device \
  --output evidence/p1-review
```

The exporter fails closed unless:

- the session was prepared through `prepare_candidate_device.py` and is bound to a full candidate Git SHA + APK SHA-256;
- candidate state is still `READY_FOR_PHYSICAL_DEVICE_EVIDENCE` with `verified=false`;
- session/index/checkpoint APK hashes agree;
- the raw ADB serial hashes to the exact serial SHA-256 recorded by both the session and index;
- evidence comes from a physical device, not an emulator;
- every indexed checkpoint has the expected candidate/device binding and required review files.

The default review bundle includes:

- `evidence-index.json`;
- `review-manifest.json` with candidate Git SHA/APK SHA and explicit `MANUAL_REVIEW_REQUIRED` verdict;
- per-checkpoint `screen.png`, `checkpoint.json`, `meminfo.txt`, `gfxinfo.txt`, `thermalservice.txt`, and `battery.txt`.

The default review bundle **does not include**:

- raw `session.json` (contains the ADB serial);
- `candidate-manifest.json` (may contain workstation-local paths);
- `package-dump.txt`;
- raw `logcat.txt`;
- raw `activity.txt`.

Before completing the export, every copied text file is scanned and the command fails if the raw ADB serial appears anywhere in the bundle. The exporter also refuses to place its output inside the raw session directory.

If automated crash/ANR red flags exist, the exporter still writes the sanitized bundle for diagnosis but exits non-zero. A clean export still has verdict `MANUAL_REVIEW_REQUIRED`; it **never** approves `UVEH-012`, `URAC-012`, `UPER-006`, `UPER-009`, or `UPER-010` automatically.

### Export integrity metadata

Review-manifest schema v2 adds a deterministic integrity inventory before the bundle leaves the QA workstation:

- every exported evidence file is recorded in `contentFiles` with exact `sizeBytes` and SHA-256;
- `copiedFiles` must be the exact sorted file set;
- `contentSetSha256` hashes the canonical `contentFiles` inventory so reviewers can compare one compact bundle fingerprint;
- `review-manifest.json` itself is intentionally outside the recursive content set, while the evidence payload it describes is fail-closed and content-addressed.

This is integrity/corruption detection, not a cryptographic signature or human approval.

## 5. Verify a transferred review bundle offline

Before reviewing screenshots/metrics or attaching evidence to a release decision, verify the transferred directory from the repository toolchain:

```bash
python3 tools/android/verify_device_review_bundle.py \
  --bundle evidence/p1-review \
  --expected-git-sha <exact-candidate-40-char-sha> \
  --expected-apk-sha <exact-candidate-apk-sha256>
```

The verifier uses only the Python standard library and fails if:

- any screenshot/metric/checkpoint/index file is missing, changed, truncated or replaced;
- an unexpected file appears in the bundle;
- a forbidden raw file such as `session.json`, `candidate-manifest.json`, `logcat.txt`, `activity.txt` or `package-dump.txt` is listed as review content;
- a content path is absolute, traverses directories or is non-canonical;
- the bundle contains a symlink;
- `contentSetSha256` no longer matches the exact content inventory;
- candidate Git/APK SHA differs from the reviewer-supplied expected values;
- candidate state no longer says `verified=false` / `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`;
- evidence-index/checkpoint candidate/device bindings disagree;
- the physical-device/privacy/manual-review contracts are changed.

Successful output still ends with `verified=false` and `MANUAL_REVIEW_REQUIRED`. The verifier proves evidence integrity and candidate binding only; Gameplay/QA/Art reviewers still make the manual P1 decisions, and `UPER-010` still requires explicit release-owner approval.

## 6. Release/publication handoff

Continue with the readiness/approval sequence in [`P1_FINAL_5_GATE_PLAN.md`](P1_FINAL_5_GATE_PLAN.md). Once all candidate-bound evidence and human approvals are legitimately complete, run the repository's authoritative combined publication preflight:

```bash
python3 tools/android/verify_release_with_production_art.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/manual-approvals.json \
  --production-art-manifest <candidate-bound-production-art-manifest.json> \
  --performance-tier <low|mid|high> \
  --repo-root .
```

Use `--apk /path/to/afareet-unity3d-debug.apk` when the exact candidate bundle moved workstations. The tool also accepts the production-art spec and gate spec overrides when required.

A successful combined preflight means only that the exact candidate is **eligible for manual publication review**. It still emits `verified=false`; tagging, release publication, and `Last Verified APK` promotion remain explicit human/release-policy actions.
