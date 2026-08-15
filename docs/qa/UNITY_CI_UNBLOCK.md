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

## GitHub-hosted Unity Production CI path

When licensing is configured, the workflow is intentionally fail-closed:

1. static contract validates the APK verifier and candidate/test verifier CLIs;
2. license preflight requires one complete credential triple;
3. EditMode and PlayMode execute through GameCI;
4. `verify_unity_test_results.py` recursively validates NUnit XML for each mode and rejects missing/zero/all-skipped/failed/inconclusive/incompletely-accounted evidence;
5. Windows and Android builds only run after both test jobs pass;
6. Android APK inspection validates package `com.fiftysolutions.afareetunity3d`, minSdk 26, ARM64-only native payload and `libunity.so`, then writes SHA/size plus GitHub run provenance;
7. `verify_ci_candidate.py` binds those workflow metadata to the exact APK bytes and creates `artifacts/android/ci-candidate-manifest.json`;
8. the Android artifact upload contains the APK plus `artifacts/android/`, including the candidate manifest.

The CI artifact metadata/candidate provenance must identify:

- repository `walidatiyaai2025-gif/3fareet`;
- workflow `Unity Production CI`;
- a supported `pull_request`, `push`, or `workflow_dispatch` event;
- a valid `refs/*` GitHub ref;
- full Git SHA, positive run ID and run attempt;
- exact APK SHA-256 and size.

For pull-request workflows, `GITHUB_SHA` can be the PR merge commit checked out by Actions. That is the correct provenance for the bytes built by that run; do not replace it with the branch-head SHA by assumption.

### Prepare a downloaded CI artifact for device QA

Do not pass a downloaded CI APK directly to `device_evidence.py prepare`.

Use the candidate manifest uploaded by the Android job and explicitly provide the downloaded APK path because the manifest's original APK path refers to the Actions runner:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/ci-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

Before ADB install, `prepare_candidate_device.py` independently rechecks the GitHub candidate type/provenance, non-self-VERIFIED state, full Git SHA, package identity, APK filename, byte length and SHA-256. A candidate from another repo/workflow or altered APK is rejected.

`verify_ci_candidate.py` does not make a network call to attest the downloaded run later. Retain the GitHub Actions run/artifact context and only use the candidate bundle from the fully Green run under review.

## Local Windows fallback — licensed workstation

If a Windows workstation already has Unity `6000.5.8f1` activated through Unity Hub, the repository provides a clean exact-head fallback for automated Unity tests and the Android APK build without storing Unity credentials in GitHub Actions.

Prerequisites:

1. Unity `6000.5.8f1` installed and licensed locally.
2. Android Build Support installed for the APK build step.
3. Git CLI installed and the repository checked out at the exact production candidate commit.
4. Python 3 available as `python` or `py -3`.
5. Clean Git working tree for release-eligible evidence.

### Canonical fail-closed candidate command

For release-candidate evidence, use the orchestrator rather than running the three local stages independently:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1
```

If Unity is installed in a non-default path:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1 `
  -UnityPath 'D:\Unity\6000.5.8f1\Editor\Unity.exe'
```

The orchestrator:

- uses named PowerShell splatting so `RepoRoot` and optional `UnityPath` bind reliably even when paths contain spaces;
- requires a clean Git tree before Unity starts;
- runs exact-head EditMode + PlayMode through `test_current_windows.ps1`;
- immediately rechecks Git and fails if Unity/UPM changed tracked repository content during tests;
- runs the Android build/inspection through `build_current_windows.ps1` only if the post-test tree is still clean;
- rechecks Git after the build;
- runs `verify_local_candidate.py` against the exact test metadata, build metadata and APK bytes;
- rechecks Git again after candidate verification;
- emits `AFAREET_LOCAL_CANDIDATE_OK` only after every stage succeeds without source/package mutation.

If Unity/UPM writes package/source changes at any stage, reconcile and commit those changes first, then restart from a clean exact head. Do not promote the previous run as release evidence.

The individual steps below remain useful for diagnosis, but the orchestrator is the required local release-evidence path.

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
- requires `total > 0` and `passed > 0` for each mode;
- rejects any failed or inconclusive tests;
- requires `passed + failed + skipped + inconclusive == total` and a passing NUnit result state;
- writes `artifacts/unity-local-tests/test-metadata.json` pinned to the exact Git SHA.

Unity is started with `Start-Process -Wait -PassThru`, so PowerShell waits for the real editor process and reads its actual exit code instead of continuing early from the Windows GUI executable.

Successful output contains:

`AFAREET_LOCAL_UNITY_TESTS_OK`

This standalone step is diagnostic/test evidence. For local release-candidate evidence, the orchestrator must also prove the working tree remains clean after Unity tests.

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
- detects post-build Git mutation and makes such output non-release-eligible;
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
- EditMode and PlayMode each have real passing, fully accounted NUnit evidence with zero failures/inconclusive tests;
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

- one of the two supported candidate types and the production package id;
- `releaseEvidenceEligible: true`;
- `readyForDeviceEvidence: true`;
- the expected non-self-VERIFIED manifest contract;
- verdict `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`;
- a valid full Git SHA;
- exact APK filename, positive byte length and SHA-256 match;
- exact GitHub run provenance when the candidate type is `github-actions-unity-ci`.

Successful precheck contains:

`AFAREET_CANDIDATE_DEVICE_PRECHECK_OK`

It then invokes the existing `device_evidence.py prepare` flow against those exact APK bytes.

## Evidence consistency rule

For local release review, use `run_local_candidate_windows.ps1` so the tree is checked before Unity, after tests, after build, and after candidate verification. For GitHub-hosted release review, use the `ci-candidate-manifest.json` generated inside the successful Android job. In both cases the resulting manifest must be consumed by `prepare_candidate_device.py` before physical-device collection.

Never use an arbitrary APK path as final P1 evidence without candidate-manifest binding. A dirty-tree/local-mutated run or a GitHub candidate with wrong provenance/hash/size must never be promoted to release evidence.

## After a device session exists

Follow `docs/qa/P1_FINAL_5_GATE_PLAN.md` to capture and review:

- `UVEH-012` driving feel;
- `URAC-012` race completion/results/restart;
- `UPER-006` smoke/performance;
- `UPER-009` Visual Gate;
- `UPER-010` release review.

The release gate remains pinned to the exact APK SHA and physical-device evidence. No tooling in this repository automatically marks an APK VERIFIED.
