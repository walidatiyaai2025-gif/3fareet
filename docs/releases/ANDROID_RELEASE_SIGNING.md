# Android release signing process

Task: `UPER-007`

## Rules
- Release keystores, aliases and passwords are never committed to Git.
- The repository stores only variable names, validation rules and operator steps.
- CI secrets must be scoped to the release workflow and protected environment.
- Local release signing uses environment variables; no password is written to `ProjectSettings` or documentation.

## Required environment contract
- `AFAREET_KEYSTORE_PATH` — absolute path to the release keystore on the build machine.
- `AFAREET_KEYSTORE_PASS` — keystore password.
- `AFAREET_KEY_ALIAS` — signing alias.
- `AFAREET_KEY_ALIAS_PASS` — alias password.

## Operator flow
1. Provision the keystore outside the repository.
2. Export the four variables only for the release process.
3. Run the dedicated release build entry point once available on the validated Unity head.
4. Record artifact SHA-256, package, version, ABI and signing-certificate fingerprint in release evidence.
5. Remove local secret variables after the build session.

## Rotation / recovery
- Keep the source keystore in an access-controlled secrets vault, not in GitHub contents or Actions artifacts.
- Rotate credentials through the release owner and update CI secrets without changing source code.
- A lost production signing key is a release incident; do not generate an ad-hoc replacement and publish it as the same production lineage.

## Acceptance status
Process and secret boundary are implemented by this document. Actual signed APK/AAB creation remains a separate exact-head release validation step.
