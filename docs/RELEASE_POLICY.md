# Unity Production + Flutter Legacy Release Policy

## Artifact identity

- Unity Android debug: `afareet-unity3d-debug.apk`.
- Unity release candidate: `afareet-unity3d-v<version>-rc<build>.apk`.
- Unity verified release: `afareet-unity3d-v<version>-verified.apk`.
- Flutter reference: `afareet-flutter-debug.apk` أو `afareet-flutter-release-skeleton.apk`؛ لا يوضع في مجلد Verified المنتج.

## Tag format

`unity-prototype-vMAJOR.MINOR.PATCH+BUILD`

Example: `unity-prototype-v0.1.0+1`.

## Required gates before a prototype tag

1. PR merged to `main` with all required checks Green.
2. Unity headless compile/tests Green.
3. Unity Android debug build Green.
4. Unity release candidate build Green.
5. For a user-facing verified release: real-device smoke checklist Green.
6. `docs/PROJECT_STATUS.md` updated with the release state.
7. Upload the exact tested APK as a GitHub Release asset named `afareet-unity3d-last-verified.apk`.
8. Update `docs/releases/LAST_VERIFIED_APK.md` with release/asset links, commit, SHA-256, device and evidence.
9. Never move the pointer for a merely built, CI-only, emulator-only or failed candidate.

Flutter checks تبقى مطلوبة فقط إذا لمس PR مسار Flutter. CI artifacts هي preview/build evidence وليست تلقائيًا Verified APKs.

## Last Verified invariants

- Build success = `Latest Built`, not `Verified`.
- Real-device checklist + recorded evidence + exact binary hash = eligible for `Verified`.
- The GitHub Release asset is the distributed binary; APK files remain excluded from Git commits.
- `docs/releases/LAST_VERIFIED_APK.md` is the repository pointer and must always distinguish Unity from Flutter.
- Keep the previous verified GitHub Release available for rollback.
