# BRAND-SPLASH-001 — 3Fareet display name + branded splash

**Date:** 2026-08-13  
**Status:** IMPLEMENTED / CI PENDING  

## Changes
- Locked user-facing app display name to `3Fareet`.
- Added branded key art at `assets/branding/3fareet_splash.jpg`.
- Flutter startup shows the branded splash before the persistent game shell.
- Android bootstrap sets `android:label="3Fareet"`.
- Android launch background uses the same splash image.
- `docs/MISSED_ASSETS.md` registers the splash as AST-062.

## Important current visual reality
The gameplay APK still does **not** contain real car image/model assets in the runtime asset bundle. Vehicle reference art remains under `docs/assets` and is not automatically rendered in the game.

## Verification gate
Do not mark this work VERIFIED until Flutter CI builds successfully and the launch sequence is smoke-tested on a real Android device.
