# U3D-012 — Unity Android CI artifact

## State
- Task: `U3D-012`
- Issue: #96
- Draft PR: #97
- Parent: PR #95 / `agent/UART-008-builtin-quality-tiers`
- Exact base: `b61e7540b5172eb1a22d5814022165dd86ccd3b3`
- Branch: `agent/U3D-012-android-ci-artifact`
- Exact head at first CI handoff: `0ddb16ac1205cad12c836f9bab8dfb446ab69bc6`
- Task state: `IN REVIEW`
- `VERIFIED`: **No** until a successful exact-head workflow produces the inspected APK artifact.

## Why this workstream exists
The current production integration line did not contain the reviewed Unity CI workflow from PR #50. The U-P1 register therefore still described `U3D-012` as blocked on Android module / CI image even though the production project already has:

- `Afareet.Editor.AfareetBuild.BuildAndroid()`;
- Android package id `com.fiftysolutions.afareetunity3d`;
- Android API 26 minimum;
- ARM64-only target architecture;
- debug APK output `unity_game/Builds/Android/afareet-unity3d-debug.apk`.

PR #50 remains the semantic source for the reviewed EditMode/PlayMode + Windows CI contract. This branch ports that contract onto the current production line and extends it with a first-class Android artifact job.

## Workflow contract
`.github/workflows/unity-ci.yml` provides five gates:

1. **Static contract** — runs without Unity licensing and verifies the APK verifier script plus the production `BuildAndroid()` package/ARM64/output contract.
2. **Unity license preflight** — requires either `UNITY_LICENSE`, or `UNITY_EMAIL + UNITY_PASSWORD + UNITY_SERIAL`; missing credentials fail loudly.
3. **EditMode / PlayMode tests** — GameCI test runner on Unity `6000.5.8f1`.
4. **Windows x64 build** — preserves the PR #50 reviewed Windows artifact gate.
5. **Android ARM64 debug APK** — GameCI Android builder using `Afareet.Editor.AfareetBuild.BuildAndroid`, gated on successful tests.

The workflow also watches source-only Hero/Environment asset paths because current production builders ingest versioned source under `docs/assets/` before packaging.

## Android artifact verification
`.github/scripts/verify-unity-android-apk.sh` fails unless all of the following are true:

- APK exists and is non-empty;
- Android package id is exactly `com.fiftysolutions.afareetunity3d`;
- minSdk is API 26;
- the only native ABI found under `lib/` is `arm64-v8a`;
- `lib/arm64-v8a/libunity.so` is present;
- SHA-256 is generated;
- machine-readable artifact metadata is written with Git SHA/run identifiers.

The successful Android job uploads the APK together with `aapt` badging, SHA-256 and JSON metadata as the `afareet-unity3d-android-${GITHUB_SHA}` Actions artifact for 14 days.

## Scope guard
This workstream changes only:

- `.github/workflows/unity-ci.yml`;
- `.github/scripts/verify-unity-android-apk.sh`;
- this evidence document.

No gameplay, Vehicle physics/config, Race, UI, Audio, Art, Rendering, Packages, ProjectSettings, signing or release logic is modified.

## Exact-head CI evidence
GitHub Actions run `31838144010` (`Unity Production CI`, run #6) executed on exact head `0ddb16ac1205cad12c836f9bab8dfb446ab69bc6`.

Observed jobs:

- `Unity CI static contract`: **SUCCESS**.
  - checkout succeeded;
  - APK verifier `bash -n` passed;
  - production `BuildAndroid()` / package / ARM64 / output-path contract checks passed.
- `Unity license preflight`: **FAILURE**.
  - runner log showed `UNITY_LICENSE`, `UNITY_EMAIL`, `UNITY_PASSWORD` and `UNITY_SERIAL` all empty;
  - failure is the explicit missing-secret error, not an Android image/workflow error.
- EditMode / PlayMode tests: **SKIPPED** because license preflight failed.
- Windows build: **SKIPPED** because the test gate did not execute.
- Android ARM64 debug APK: **SKIPPED** because the test gate did not execute.

Therefore the original Android CI-image/workflow implementation blocker is removed, but engine execution still has an external repository-secret dependency.

## Current validation truth
- production Android build method: reviewed in repository;
- PR #50 Unity CI contract: reviewed and ported;
- GameCI Android target support: confirmed by current GameCI documentation;
- exact-head workflow syntax/trigger/static contract: **EXECUTED / PASS**;
- Unity licensing preflight: **EXECUTED / FAIL — missing secrets**;
- Unity import/compile: **NOT EXECUTED**;
- EditMode / PlayMode: **NOT EXECUTED**;
- Android APK artifact: **NOT PRODUCED**;
- APK identity/ABI/SHA verifier against a real APK: **NOT EXECUTED**;
- device verification: **NOT EXECUTED**;
- `VERIFIED`: **No**.

## External unblock required
Configure one supported Unity CI licensing path in repository Actions secrets:

- Personal/file path: `UNITY_LICENSE` (and the account credentials expected by the GameCI action), or
- Professional/serial path: `UNITY_EMAIL` + `UNITY_PASSWORD` + `UNITY_SERIAL`.

After secrets are configured, rerun exact PR #97 head and require tests + Android build + APK inspection to pass before any VERIFIED promotion.

## Promotion rule
`U3D-012` is now `IN REVIEW` because the Android CI-image/workflow implementation exists and its license-free static contract passed on the exact PR head. It must not be called `VERIFIED` until an exact-head run completes tests, produces the APK, passes artifact inspection, and exposes the retained Actions artifact.

Real-device driving/race/smoke/visual gates remain separate tasks (`UVEH-012`, `URAC-012`, `UPER-006`, `UPER-009`).
