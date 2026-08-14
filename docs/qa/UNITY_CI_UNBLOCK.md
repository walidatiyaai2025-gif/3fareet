# Unity CI / Android Build Unblock

This runbook addresses the external blocker tracked in Issue #98 and provides two supported paths to obtain exact-head Unity test evidence and a current Android APK for the five remaining P1 device/release gates.

## Current blocker

Unity Production CI is implemented, but Unity engine jobs cannot start until a complete Unity licensing credential set is available to GitHub Actions.

The workflow accepts only complete sets:

### Unity Personal / file-license path

Configure all three repository Actions secrets:

- `UNITY_LICENSE` — contents of the local Unity `.ulf` license file;
- `UNITY_EMAIL` — Unity account email;
- `UNITY_PASSWORD` — Unity account password.

### Unity Pro / serial path

Configure all three repository Actions secrets:

- `UNITY_SERIAL`;
- `UNITY_EMAIL`;
- `UNITY_PASSWORD`.

Never commit any of these values to Git.

After secrets are configured, rerun `Unity Production CI` on the latest production-stack head. Do not use a Green static-contract job as evidence that Unity compile/tests/build executed.

## Local Windows fallback — licensed workstation

If a Windows workstation already has Unity `6000.5.8f1` activated through Unity Hub, the repository provides a clean exact-head fallback for automated Unity tests and the Android APK build without storing Unity credentials in GitHub Actions.

Prerequisites:

1. Unity `6000.5.8f1` installed and licensed locally.
2. Android Build Support installed for the APK build step.
3. Git CLI installed and the repository checked out at the exact production candidate commit.
4. Clean Git working tree for release-eligible evidence.

### 1. Run EditMode + PlayMode tests

From PowerShell at repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/test_current_windows.ps1
```

If Unity is installed in a non-default path:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/test_current_windows.ps1 `
  -UnityPath 'D:\Unity\6000.5.8f1\Editor\Unity.exe'
```

The test script:

- rejects the wrong Unity version;
- requires Git and a full 40-character commit SHA;
- rejects a dirty tree by default;
- treats `-AllowDirty` output as debug-only and marks it `releaseEvidenceEligible: false`;
- runs Unity Test Framework from the command line in both `EditMode` and `PlayMode`;
- writes separate NUnit XML results and Unity logs;
- rejects missing/empty XML, zero-test runs, non-passing result state, or any failed test;
- writes `artifacts/unity-local-tests/test-metadata.json` pinned to the exact Git SHA.

Unity is started with `Start-Process -Wait -PassThru`, so PowerShell waits for the real editor process and reads its actual exit code instead of continuing early from the Windows GUI executable.

Successful output contains:

`AFAREET_LOCAL_UNITY_TESTS_OK`

This is exact-head local automated-test evidence. It does **not** make the GitHub `Unity Production CI` workflow Green and does not substitute for CI provenance where CI provenance is explicitly required.

### 2. Build and inspect Android APK

Run:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/build_current_windows.ps1
```

If Unity is installed in a non-default path:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/build_current_windows.ps1 `
  -UnityPath 'D:\Unity\6000.5.8f1\Editor\Unity.exe'
```

The build script:

- rejects the wrong Unity version;
- requires a full Git commit SHA and records the active branch;
- rejects a dirty Git tree by default;
- treats `-AllowDirty` output as debug-only and marks it `releaseEvidenceEligible: false`;
- confirms Android Build Support exists;
- waits for the actual Unity process with `Start-Process -Wait -PassThru` and captures its exit code;
- deletes stale APK/log output before launching Unity;
- requires the explicit `AFAREET_BUILD_SUCCESS target=Android` Unity log marker before accepting a zero exit code;
- requires a non-empty APK;
- verifies package `com.fiftysolutions.afareetunity3d`;
- verifies minSdk API 26;
- verifies ARM64-only native payload and `libunity.so`;
- generates SHA-256 and JSON artifact metadata pinned to the Git commit;
- copies the inspected APK/evidence to `artifacts/android-local/`.

Successful output contains:

`AFAREET_LOCAL_ANDROID_BUILD_OK`

The APK build/inspection evidence is not Device Verified evidence.

### 3. Verify tests + APK are one candidate

Do not manually compare metadata. Run the fail-closed candidate integrity gate:

```bash
python3 tools/android/verify_local_candidate.py \
  --test-metadata artifacts/unity-local-tests/test-metadata.json \
  --build-metadata artifacts/android-local/artifact-metadata.json \
  --apk artifacts/android-local/afareet-unity3d-debug.apk \
  --output artifacts/local-candidate-manifest.json
```

The gate rejects the candidate unless:

- test and build metadata are both release-eligible and clean;
- EditMode and PlayMode each executed at least one test and passed with zero failures;
- both metadata files use Unity `6000.5.8f1`;
- test and build metadata pin to the same full Git SHA;
- package/minSdk/ABI/artifact identity matches the production contract;
- the actual APK SHA-256 and file size exactly match build metadata.

Successful output contains:

`AFAREET_LOCAL_CANDIDATE_READY`

The generated manifest deliberately contains `readyForDeviceEvidence: true` and `verified: false`. It is an integrity handoff to physical-device QA, not release approval.

### 4. Start device evidence only from that candidate

For the local path, do not manually pass an arbitrary APK to the ADB collector. Use the candidate-aware bridge:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the candidate manifest/APK bundle was moved to another workstation, provide the moved APK explicitly:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

Before invoking ADB, this wrapper independently requires:

- supported local candidate type and production package id;
- `releaseEvidenceEligible: true`;
- `readyForDeviceEvidence: true`;
- the expected non-self-VERIFIED manifest contract;
- verdict `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`;
- a valid full Git SHA;
- exact APK filename, positive byte length and SHA-256 match.

Successful precheck contains:

`AFAREET_CANDIDATE_DEVICE_PRECHECK_OK`

It then invokes the existing `device_evidence.py prepare` flow against those exact APK bytes.

## Evidence consistency rule

For local release review, the Unity test metadata, APK metadata and actual APK bytes must pass `verify_local_candidate.py`, and the resulting manifest must be consumed by `prepare_candidate_device.py` before physical-device collection. A dirty-tree run must never be promoted to release evidence.

The safest GitHub-hosted path remains a fully Green `Unity Production CI` run after a complete licensing secret set is configured. The local path exists to make exact-head test/build execution possible on an already licensed workstation without weakening the physical-device or manual review gates.

## After a device session exists

Follow `docs/qa/P1_FINAL_5_GATE_PLAN.md` to capture and review:

- `UVEH-012` driving feel;
- `URAC-012` race completion/results/restart;
- `UPER-006` smoke/performance;
- `UPER-009` Visual Gate;
- `UPER-010` release review.

The release gate remains pinned to the exact APK SHA and physical-device evidence. No tooling in this repository automatically marks an APK VERIFIED.
