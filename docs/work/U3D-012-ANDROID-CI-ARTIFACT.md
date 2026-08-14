# U3D-012 — Unity Android CI artifact

## State
- Task: `U3D-012`
- Issue: #96
- Parent: PR #95 / `agent/UART-008-builtin-quality-tiers`
- Exact base: `b61e7540b5172eb1a22d5814022165dd86ccd3b3`
- Branch: `agent/U3D-012-android-ci-artifact`
- Target task state after Draft PR: `IN REVIEW`
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
`.github/workflows/unity-ci.yml` now provides five gates:

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

## Current validation truth
At authoring time:

- production Android build method: reviewed in repository;
- PR #50 Unity CI contract: reviewed and ported;
- GameCI Android target support: confirmed by current GameCI documentation;
- exact-head GitHub workflow: pending Draft PR creation / trigger;
- Unity import/compile: **NOT EXECUTED on this branch yet**;
- EditMode / PlayMode: **NOT EXECUTED on this branch yet**;
- Android APK artifact: **NOT PRODUCED yet**;
- APK identity/ABI/SHA verifier against a real APK: **NOT EXECUTED yet**;
- device verification: **NOT EXECUTED**;
- `VERIFIED`: **No**.

## Known external dependency
The last reviewed Unity CI run on PR #50 (`31687240167`) failed before engine execution because Unity licensing secrets were missing. This branch deliberately keeps the same explicit credential preflight. If repository secrets are still absent, the static contract job can pass but the Unity/test/build jobs must remain blocked/failing rather than reporting a false Green result.

## Promotion rule
`U3D-012` can move `BLOCKED → IN REVIEW` when this workflow and evidence land on a Draft PR because the Android CI-image/workflow implementation blocker is removed. It must not be called `VERIFIED` until an exact-head run completes tests, produces the APK, passes artifact inspection, and exposes the retained Actions artifact.

Real-device driving/race/smoke/visual gates remain separate tasks (`UVEH-012`, `URAC-012`, `UPER-006`, `UPER-009`).
