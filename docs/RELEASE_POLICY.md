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
7. Only the latest real-device verified APK may be copied to `Last verified APK released/`.

Flutter checks تبقى مطلوبة فقط إذا لمس PR مسار Flutter. CI artifacts هي preview/build evidence وليست تلقائيًا Verified APKs.
