# Last Verified Unity APK

> This file is the single source of truth. A successful build is not verification.

## Current status

**No Unity APK has completed the required real-device verification yet.**

The current Unity APKs are `Latest Built` candidates only. Do not publish, link,
or rename one as `Last Verified APK` until every gate below is complete.

## Required record for the first verified APK

Replace the status above with all of these fields:

- Status: `DEVICE VERIFIED`
- Product: `Unity 3D` (never confuse with Flutter)
- Version name / version code:
- Git tag:
- Commit SHA:
- GitHub Release URL:
- Direct APK asset URL:
- APK filename: `afareet-unity3d-last-verified.apk`
- APK SHA-256:
- Unity version:
- Package ID:
- Build type and ABI:
- Verification date/time and timezone:
- Tester:
- Device model:
- Android version / API:
- Smoke checklist result:
- Screenshot/video evidence URLs:
- Known issues:

## Promotion procedure

1. Build a release candidate from a reviewed commit.
2. Run `docs/SMOKE_TEST_CHECKLIST.md` on a cleanly installed real Android device.
3. Record the tester, device, Android API, commit, result, and SHA-256.
4. Create a GitHub Release tag such as `unity-verified-v0.1.0-build.1`.
5. Upload the exact tested binary as `afareet-unity3d-last-verified.apk`.
6. Update this file and `docs/PROJECT_STATUS.md` in a reviewed PR.
7. Keep the previous verified release published for rollback.

If any gate fails, record the candidate as Built/Failed evidence without changing
this pointer.
