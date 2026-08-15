# Android Device Evidence Harness

## Purpose

`tools/android/device_evidence.py` standardizes the evidence collected from the exact 3Fareet APK and a physical Android device. It is a **collection tool, not an automatic QA approval tool**.

It supports evidence for:

- `UVEH-012` — real-device driving feel;
- `URAC-012` — ordered lap / Results / restart verification;
- `UPER-006` — Android smoke/performance/device matrix;
- `UPER-009` — P1 Visual Gate screenshots;
- `UPER-010` — later consumes approved evidence but is never published by this harness.

A successful harness command does **not** move any of those tasks out of BLOCKED or call an APK VERIFIED.

## Requirements

- Python 3.10+;
- Android platform tools with `adb` on `PATH`;
- one authorized physical Android device, or pass `--serial` when multiple devices are connected;
- the exact APK being reviewed.

By default emulators are rejected because the P1 acceptance gates require physical-device evidence. `--allow-emulator` exists only to debug the harness itself.

## 1. Prepare a pinned session

```bash
python tools/android/device_evidence.py prepare \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output artifacts/device-evidence/pixel8-run1
```

`prepare`:

- computes APK SHA-256 and size;
- selects/records the exact ADB device;
- records manufacturer/model/Android/API/ABI/display metadata;
- rejects emulators by default;
- installs the APK with `adb install -r -t`;
- confirms `com.fiftysolutions.afareetunity3d` is installed;
- clears logcat, force-stops and launches the app;
- writes `session.json` and the installed package dump.

The session remains pinned to the APK SHA and device serial hash for every later capture.

## 2. Capture named manual checkpoints

The QA operator drives/uses the game normally and invokes `capture` when a required state is visible.

Recommended minimum sequence:

```bash
python tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label start
python tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label racing-2min
python tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label results
python tools/android/device_evidence.py capture --session artifacts/device-evidence/pixel8-run1 --label restart-countdown
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
python tools/android/device_evidence.py finish \
  --session artifacts/device-evidence/pixel8-run1
```

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
Do not publish based on harness output alone. Publication requires the exact APK SHA, successful exact-head build/test evidence and all required manual device/visual approvals.

## Evidence handling

The local `session.json` keeps the raw ADB serial for repeatability on the QA machine. `evidence-index.json` exposes only the serial SHA-256 plus device characteristics. Review before attaching raw local evidence publicly.

Do not commit generated `artifacts/device-evidence/**` folders to Git. Store approved evidence according to the release/QA process for the exact APK/commit being promoted.

## 4. Export a privacy-safe review bundle

After the candidate-bound session is finished, generate the default shareable review bundle with:

```bash
python tools/android/export_device_evidence.py \
  --session artifacts/device-evidence/pixel8-run1 \
  --output artifacts/device-review/pixel8-run1
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
python tools/android/verify_device_review_bundle.py \
  --bundle artifacts/device-review/pixel8-run1 \
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

Successful output still ends with `verified=false` and `MANUAL_REVIEW_REQUIRED`. The verifier proves evidence integrity and candidate binding only; Gameplay/QA/Art reviewers still make the four manual P1 decisions, and `UPER-010` still requires explicit release approval.
