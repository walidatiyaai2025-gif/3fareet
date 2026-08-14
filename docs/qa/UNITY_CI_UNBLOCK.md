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

If a Windows workstation already has Unity `6000.5.8f1` activated through Unity Hub, the repository now provides a clean exact-head fallback for both automated Unity tests and the Android APK build without storing Unity credentials in GitHub Actions.

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
- invokes Unity with an explicit PowerShell argument array, then runs `Afareet.Editor.AfareetBuild.BuildAndroid` in batch mode;
- requires a non-empty APK;
- verifies package `com.fiftysolutions.afareetunity3d`;
- verifies minSdk API 26;
- verifies ARM64-only native payload and `libunity.so`;
- generates SHA-256 and JSON artifact metadata pinned to the Git commit;
- copies the inspected APK/evidence to `artifacts/android-local/`.

Successful output contains:

`AFAREET_LOCAL_ANDROID_BUILD_OK`

The APK build/inspection evidence is not Device Verified evidence.

## Evidence consistency rule

For release review, the local Unity test metadata and APK metadata must both be release-eligible and pin to the same exact Git SHA. A dirty-tree run must never be promoted to release evidence.

The safest GitHub-hosted path remains a fully Green `Unity Production CI` run after a complete licensing secret set is configured. The local path exists to make exact-head test/build execution possible on an already licensed workstation without weakening the physical-device or manual review gates.

## After an APK exists

Use the exact APK produced from the current production candidate:

```bash
python3 tools/android/device_evidence.py prepare \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

Then follow `docs/qa/P1_FINAL_5_GATE_PLAN.md` to capture and review:

- `UVEH-012` driving feel;
- `URAC-012` race completion/results/restart;
- `UPER-006` smoke/performance;
- `UPER-009` Visual Gate;
- `UPER-010` release review.

The release gate remains pinned to the exact APK SHA and physical-device evidence. No tooling in this repository automatically marks an APK VERIFIED.
