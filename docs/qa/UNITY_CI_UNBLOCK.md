# Unity CI / Android Build Unblock

This runbook addresses the external blocker tracked in Issue #98 and provides two supported paths to obtain a current Android APK for the five remaining P1 device/release gates.

## Current blocker

Unity Production CI is implemented, but Unity engine jobs cannot start until a complete Unity licensing credential set is available to GitHub Actions.

The workflow now accepts only complete sets:

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

After secrets are configured, rerun `Unity Production CI` on the latest production-stack head. Do not use a Green static-contract job as evidence that Unity compile/build executed.

## Local Windows fallback — no GitHub Actions license secrets required

If a Windows workstation already has Unity `6000.5.8f1` activated through Unity Hub, the project can build the current checkout directly with the repository script.

Prerequisites:

1. Unity `6000.5.8f1` installed and licensed locally.
2. Android Build Support installed for that Unity version.
3. Repository checked out at the exact production candidate commit.
4. Clean Git working tree for release evidence.

Run from PowerShell at repository root:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/build_current_windows.ps1
```

If Unity is installed in a non-default path:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/build_current_windows.ps1 `
  -UnityPath 'D:\Unity\6000.5.8f1\Editor\Unity.exe'
```

The script:

- rejects the wrong Unity version;
- rejects a dirty Git tree by default;
- confirms Android Build Support exists;
- runs `Afareet.Editor.AfareetBuild.BuildAndroid` in batch mode;
- requires a non-empty APK;
- verifies package `com.fiftysolutions.afareetunity3d`;
- verifies minSdk API 26;
- verifies ARM64-only native payload and `libunity.so`;
- generates SHA-256 and JSON artifact metadata pinned to the Git commit;
- copies the inspected APK/evidence to `artifacts/android-local/`.

Successful output contains:

`AFAREET_LOCAL_ANDROID_BUILD_OK`

This is build evidence only. It is not Device Verified evidence.

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
