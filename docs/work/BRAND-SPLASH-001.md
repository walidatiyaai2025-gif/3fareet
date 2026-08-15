# BRAND-SPLASH-001 — 3Fareet display name + branded splash

**Date:** 2026-08-15  
**Status:** IMPLEMENTED / CI PENDING  

## Changes
- Locked the user-facing display name to `3Fareet` while keeping existing internal package identifiers stable.
- Carries the approved branded key art at `assets/branding/3fareet_splash.jpg` from `main` into the active Unity production integration line.
- Flutter startup shows the branded splash before the persistent game shell.
- Flutter Android bootstrap sets `android:label="3Fareet"` and uses the same image in the launch background.
- `docs/MISSED_ASSETS.md` registers the splash as `AST-063` because `AST-062` is already the active Unity Hero Car asset ID.

## Current visual reality
The legacy Flutter client uses this 2D launch branding and still does not render the production Hero Car model in its runtime bundle.

The Unity production stack does contain the original Hero Car LOD source/integration pipeline, but the current exact production head has not yet completed licensed Unity execution, Android candidate generation, physical-device Visual Gate, or performance approval. No current-head Unity asset is promoted to `VERIFIED` by this branding synchronization.

## Verification gate
Do not mark `AST-063` VERIFIED until the Flutter build/test path is Green and the launch sequence is smoke-tested on a real Android device. Unity P1 verification remains governed separately by the exact-candidate device/release gates.
