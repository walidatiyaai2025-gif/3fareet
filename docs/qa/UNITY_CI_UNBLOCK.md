# Unity CI / Local Candidate Unblock

This document records the fail-closed Unity candidate path used by PR #108 and the final P1 verification chain.

## Hosted CI licensing boundary

The hosted Unity workflow validates its static contract first and then requires a complete supported Unity credential set before engine execution. If credentials are absent, the license preflight fails and Unity tests/builds are skipped. This is an external hosted-licensing limitation, not evidence that the local licensed Windows candidate path failed.

## Licensed Windows candidate generation

A licensed Windows workstation with Unity `6000.5.8f1` may produce the exact-head candidate without storing Unity credentials in GitHub Actions.

The **candidate-generation chain is Python-free**. It uses native PowerShell verifiers for text normalization, Unity package consistency and final local-candidate integrity. Python remains used by separate cross-platform/device-evidence tooling after the candidate exists, but it is not a prerequisite for generating the Windows candidate APK/manifest.

Prerequisites for candidate generation:

1. Unity `6000.5.8f1` installed and activated.
2. Android Build Support installed.
3. Git available.
4. Exact production branch/commit checked out.
5. Clean Git working tree.

Refresh the workstation first:

```powershell
git fetch origin
git reset --hard origin/agent/unblock-final-5
git clean -fd
```

Run the canonical candidate path:

```powershell
powershell -ExecutionPolicy Bypass -File tools/android/run_local_candidate_windows.ps1 `
  -UnityPath 'C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe'
```

Expected early native-verifier marker:

```text
AFAREET_WINDOWS_NATIVE_VERIFIERS_OK pythonRequired=False
```

## Exact local orchestration sequence

The local release-evidence path is intentionally ordered as follows:

1. resolve the full 40-character Git SHA;
2. reject an already-dirty tree, preserving `INITIAL_TREE` status/patch/stderr evidence first;
3. purge stale release-looking candidate evidence;
4. confirm the native PowerShell verifier chain;
5. **rematerialize governed Unity metadata as LF in the working tree**;
6. require the Git tree to remain clean after LF rematerialization;
7. **Unity text-normalization preflight**;
8. Git cleanliness check after text normalization;
9. **Unity package manifest/lock preflight**;
10. Git cleanliness check after package verification;
11. EditMode + PlayMode execution;
12. Git cleanliness check after tests;
13. Android build + APK inspection;
14. Git cleanliness check after build;
15. native same-SHA candidate integrity verification;
16. final Git cleanliness check;
17. emit `AFAREET_LOCAL_CANDIDATE_OK` only when all stages succeed.

The rematerialization step, preflights and candidate verifier are mandatory. Do not bypass them to save time.

## Stale Windows CRLF materialization

A long-lived Windows clone can contain CRLF bytes in tracked Unity metadata even after `.gitattributes` is updated to `text eol=lf`. Git can treat the checkout as clean depending on the clone/configuration while the strict byte verifier still sees CRLF. A normal `git reset --hard` is not a reliable way to force those already-tracked working-tree bytes to be rewritten.

`tools/android/materialize_unity_lf_windows.ps1` fixes that stale-checkout condition before Unity starts. It:

- enumerates only tracked files covered by the Unity LF contract;
- requires `git check-attr` to report `text: set` and `eol: lf` before touching a file;
- performs byte-level CRLF -> LF conversion without decoding/re-encoding the file;
- leaves every other byte unchanged;
- is followed immediately by the orchestrator's full Git-clean assertion.

Expected markers:

```text
AFAREET_WORKTREE_LF_MATERIALIZE_START gitSha=<sha>
AFAREET_UNITY_LF_MATERIALIZED files=<n> rewritten=<n> eol=lf
AFAREET_WORKTREE_LF_MATERIALIZE_OK gitSha=<sha>
```

If that byte-only repair creates any tracked Git diff, the candidate path fails closed before Unity.

## Text-normalization preflight

`tools/android/verify_unity_text_normalization_windows.ps1` checks every tracked file matching:

- `unity_game/ProjectSettings/*.asset`
- `unity_game/ProjectSettings/*.txt`
- `unity_game/Packages/*.json`

Each path must resolve through `.gitattributes` to `text: set` and `eol: lf`, must exist in the working tree, and must contain no CRLF bytes.

Expected markers:

```text
AFAREET_TEXT_NORMALIZATION_PREFLIGHT_START gitSha=<sha>
AFAREET_UNITY_TEXT_NORMALIZATION_OK files=<n> eol=lf verifier=windows-powershell
AFAREET_TEXT_NORMALIZATION_PREFLIGHT_OK gitSha=<sha>
```

Any failure stops before Unity starts.

## Package graph preflight

`tools/android/verify_unity_package_lock_windows.ps1` requires every direct `Packages/manifest.json` dependency to exist in `packages-lock.json` at the same version with `depth: 0`. Known Unity resolver child maps are also checked, including the resolved child packages themselves.

Expected markers:

```text
AFAREET_PACKAGE_PREFLIGHT_START gitSha=<sha>
AFAREET_UNITY_PACKAGE_LOCK_OK ...
AFAREET_PACKAGE_PREFLIGHT_OK gitSha=<sha>
```

Any mismatch stops before Unity starts.

## Native Windows verifier CI

`.github/workflows/windows-native-verifiers.yml` runs the native verifier chain on `windows-latest`. It parses all PowerShell scripts, executes the real repository LF/package checks, reproduces a CRLF working-tree fixture, repairs it with the LF materializer, and runs a candidate-integrity fixture that verifies:

- the strict verifier rejects CRLF working-tree bytes;
- LF rematerialization restores strict LF compliance and a clean Git tree;
- valid same-SHA test/build/APK evidence produces a candidate with `readyForDeviceEvidence: true` and `verified: false`;
- APK SHA-256/provenance are preserved;
- release-evidence booleans must be real JSON booleans rather than strings such as `"true"`.

A Green native-verifier workflow validates the tooling contract only. It does not replace the licensed Unity exact-head test/build run.

## Unity tests

`test_current_windows.ps1` runs EditMode and PlayMode using the licensed local Unity editor. Release-eligible metadata requires real non-empty NUnit evidence, zero failed tests, zero inconclusive tests, and fully accounted totals.

The current target test inventory is:

- EditMode: 94
- PlayMode: 3
- total: 97

Passing evidence from one Git SHA is not promoted to a later SHA; every release candidate must regenerate exact-head test evidence.

## Android build

`build_current_windows.ps1` builds and inspects the Unity Android candidate and records exact metadata under `artifacts/android-local/`. The expected production identity includes:

- package: `com.fiftysolutions.afareetunity3d`
- Unity: `6000.5.8f1`
- minSdk: 26
- ABI: `arm64-v8a`
- `libunity.so`

The candidate build metadata is release-eligible only when its pre/post-build Git state is clean.

## Local candidate integrity

A successful orchestrated local run produces:

```text
artifacts/unity-local-tests/test-metadata.json
artifacts/android-local/artifact-metadata.json
artifacts/android-local/afareet-unity3d-debug.apk
artifacts/local-candidate-manifest.json
```

`tools/android/verify_local_candidate_windows.ps1` requires:

- release-eligible clean test and build metadata with real JSON boolean fields;
- real passing EditMode + PlayMode evidence;
- Unity `6000.5.8f1`;
- one identical full Git SHA across test/build evidence;
- production APK package/minSdk/ABI identity;
- exact APK SHA-256 and byte size matching build metadata.

The local candidate verdict is readiness for physical-device evidence, not final verification:

```text
readyForDeviceEvidence: true
verified: false
verdict: READY_FOR_PHYSICAL_DEVICE_EVIDENCE
```

## Start physical-device evidence

The device-evidence tools are Python-based and are a **separate post-candidate phase**. On the machine that will run physical-device evidence, make Python 3 and ADB available, then consume the already-generated candidate manifest rather than rebuilding or bypassing it.

For the local candidate, use `tools/android/prepare_candidate_device.py` with the exact candidate manifest and exact APK bytes. PR #107 then collects candidate-bound physical Android evidence for the remaining gates:

- `UVEH-012`
- `URAC-012`
- `UPER-006`
- `UPER-009`
- `UPER-010`

Do not mark any of those tasks VERIFIED until its required same-APK physical evidence and approvals are present.
