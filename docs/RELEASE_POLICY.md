# Prototype Release / Tag Policy

## Tag format

`prototype-vMAJOR.MINOR.PATCH+BUILD`

Example: `prototype-v0.1.0+1`.

## Required gates before a prototype tag

1. PR merged to `main` with all required checks Green.
2. `flutter analyze` and `flutter test` Green.
3. Android debug build Green.
4. Android release skeleton build Green.
5. For a user-facing verified release: real-device smoke checklist Green.
6. `docs/PROJECT_STATUS.md` updated with the release state.
7. Only the latest real-device verified APK may be copied to `Last verified APK released/`.

CI artifacts are previews/build evidence; they are **not** automatically Verified APKs.
