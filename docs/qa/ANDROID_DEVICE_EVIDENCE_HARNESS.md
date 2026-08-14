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
