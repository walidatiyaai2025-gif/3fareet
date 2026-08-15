# Unity CI / Android Build Unblock

This runbook defines the supported ways to obtain exact-head Unity test evidence and a current Android APK for the five remaining P1 device/release gates. It is fail-closed: a Green static/tooling check is never equivalent to Unity execution, and no APK is called VERIFIED without the later physical-device/manual gates.

## Current external blocker

GitHub-hosted `Unity Production CI` cannot run Unity engine jobs until one complete credential set exists in repository Actions secrets.

Supported sets:

### Personal / file-license

- `UNITY_LICENSE`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

### Professional / serial

- `UNITY_SERIAL`
- `UNITY_EMAIL`
- `UNITY_PASSWORD`

Never commit those values to Git.

The license preflight deliberately fails partial sets. When credentials are absent, EditMode, PlayMode, Windows build and Android build are skipped and no APK is produced.

## GitHub-hosted production path

When licensing is configured, `Unity Production CI` must pass in this order:

1. static contract and package-graph checks;
2. license preflight;
3. real EditMode + PlayMode execution;
4. NUnit verification for both modes (`total > 0`, `passed > 0`, zero failed/inconclusive and fully accounted counters);
5. Windows x64 build;
6. Android ARM64 APK build;
7. APK inspection for package `com.fiftysolutions.afareetunity3d`, minSdk 26, ARM64-only native payload and `libunity.so`;
8. SHA-256 / size / workflow provenance metadata;
9. `verify_ci_candidate.py` binding those metadata to the exact APK bytes;
10. upload of the APK plus `artifacts/android/ci-candidate-manifest.json`.

For pull-request workflows, the checked-out `GITHUB_SHA` may be the PR merge commit. That is valid provenance for the bytes produced by that run; do not silently replace it with the branch-head SHA.

A downloaded hosted candidate must be consumed through the candidate-aware bridge, not by passing an arbitrary APK directly to the device harness:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest /path/to/ci-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --output evidence/p1-device
```

The bridge rechecks repository/workflow/run provenance, package identity, full Git SHA, APK filename, byte size and SHA-256 before ADB installation. The candidate remains `verified: false`.

## Licensed-Windows fallback — canonical path

A licensed Windows workstation with Unity `6000.5.8f1` may produce the exact-head candidate without storing Unity credentials in GitHub Actions.

Prerequisites:

1. Unity `6000.5.8f1` installed and activated.
2. Android Build Support installed.
3. Git available.
4. Python 3 available as `python` or `py -3`.
5. Exact production branch/commit checked out.
6. Clean Git working tree.

Refresh the workstation first:

```powershell
git fetch origin
git reset --hard origin/agent/unblock-final-5
git clean -fd
```

Then run the canonical orchestrator:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1
```

Or provide Unity explicitly:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1 `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
```

## Exact local orchestration sequence

The local release-evidence path is now intentionally ordered as follows:

1. resolve the full 40-character Git SHA;
2. reject an already-dirty tree, preserving `INITIAL_TREE` status/patch/stderr evidence first;
3. purge stale release-looking candidate evidence;
4. **Unity text-normalization preflight**;
5. Git cleanliness check after text normalization;
6. **Unity package manifest/lock preflight**;
7. Git cleanliness check after package verification;
8. EditMode + PlayMode execution;
9. Git cleanliness check after tests;
10. Android build + APK inspection;
11. Git cleanliness check after build;
12. same-SHA candidate integrity verification;
13. final Git cleanliness check;
14. emit `AFAREET_LOCAL_CANDIDATE_OK` only when all stages succeed.

The two preflights are mandatory. Do not bypass them to save time.

### Text-normalization preflight

`tools/android/verify_unity_text_normalization.py` checks every tracked file matching:

- `unity_game/ProjectSettings/*.asset`
- `unity_game/ProjectSettings/*.txt`
- `unity_game/Packages/*.json`

Each file must:

- be covered by explicit Git text normalization;
- resolve to `eol=lf`;
- exist in the working tree;
- contain no CRLF bytes.

The repository `.gitattributes` pins those paths to LF so Windows `core.autocrlf` cannot create false post-Unity dirty state.

Expected markers:

```text
AFAREET_TEXT_NORMALIZATION_PREFLIGHT_START
AFAREET_UNITY_TEXT_NORMALIZATION_OK
AFAREET_TEXT_NORMALIZATION_PREFLIGHT_OK
```

Any failure stops before Unity starts.

### Package graph preflight

`tools/android/verify_unity_package_lock.py` requires every direct `Packages/manifest.json` dependency to exist in `packages-lock.json` at the same version with `depth: 0`. Known Unity resolver child maps are also checked, including the resolved child packages themselves.

Expected markers:

```text
AFAREET_PACKAGE_PREFLIGHT_START
AFAREET_UNITY_PACKAGE_LOCK_OK
AFAREET_PACKAGE_PREFLIGHT_OK
```

Any mismatch stops before Unity starts.

## Unity tests

`test_current_windows.ps1`:

- requires Unity `6000.5.8f1`;
- binds evidence to a full Git SHA;
- runs both EditMode and PlayMode;
- uses `Start-Process -Wait -PassThru` so PowerShell waits for the actual Unity process;
- requires real non-empty NUnit evidence;
- rejects failed or inconclusive tests and inconsistent counters;
- writes `artifacts/unity-local-tests/test-metadata.json`.

Standalone success marker:

```text
AFAREET_LOCAL_UNITY_TESTS_OK
```

Standalone test success is useful diagnostics, but release-candidate evidence still requires the orchestrator's post-test clean-tree gate.

## Android build and inspection

`build_current_windows.ps1`:

- requires Unity `6000.5.8f1` and exact Git provenance;
- confirms Android Build Support;
- deletes stale APK/log output;
- waits for the actual Unity process;
- requires `AFAREET_BUILD_SUCCESS target=Android` from Unity;
- requires a non-empty APK;
- verifies package id, minSdk 26, ARM64-only payload and `libunity.so`;
- writes SHA-256 and artifact metadata under `artifacts/android-local/`.

Standalone success marker:

```text
AFAREET_LOCAL_ANDROID_BUILD_OK
```

That is build evidence, not device verification.

## Dirty-tree evidence

The canonical orchestrator never ignores or auto-reverts Unity/UPM changes.

For a dirty phase it preserves under ignored `artifacts/logs/`:

- `git-dirty-<phase>.status.txt`
- `git-dirty-<phase>.patch`
- `git-dirty-<phase>.stderr.txt`

Binary Git diff capture uses a waited native Git process with stdout/stderr separated. Non-fatal Git warnings cannot corrupt the patch or become a Windows PowerShell `NativeCommandError`.

If Unity/UPM changes tracked content, review and commit only legitimate generated/source changes, then restart from a clean exact head. A failed/dirty run is never promoted to release evidence.

## Candidate integrity handoff

A successful orchestrated local run produces:

```text
artifacts/local-candidate-manifest.json
```

The candidate verifier requires:

- release-eligible clean test and build metadata;
- real passing EditMode + PlayMode evidence;
- Unity `6000.5.8f1`;
- one identical full Git SHA across test/build evidence;
- correct package/minSdk/ABI/artifact identity;
- exact APK SHA-256 and byte-size match.

Successful candidate-verifier marker:

```text
AFAREET_LOCAL_CANDIDATE_READY
```

The manifest deliberately remains:

```text
readyForDeviceEvidence: true
verified: false
```

## Start physical-device evidence

For the local candidate:

```bash
python3 tools/android/prepare_candidate_device.py \
  --candidate-manifest artifacts/local-candidate-manifest.json \
  --output evidence/p1-device
```

If the candidate bundle was moved, also supply the exact moved APK via `--apk`.

Expected precheck marker:

```text
AFAREET_CANDIDATE_DEVICE_PRECHECK_OK
```

The wrapper validates the candidate manifest and APK bytes before delegating to ADB and persists candidate provenance into the evidence session. Do not use direct arbitrary-APK sessions for the final-five release gates.

## Final rule

The current project ledger remains:

`IN REVIEW 60 | READY 0 | TODO 0 | BLOCKED 5 = 65`

The local or hosted candidate only unblocks physical-device evidence. Follow `docs/qa/P1_FINAL_5_GATE_PLAN.md` for `UVEH-012`, `URAC-012`, `UPER-006`, `UPER-009` and `UPER-010`.

No tool in this repository automatically marks an APK VERIFIED.