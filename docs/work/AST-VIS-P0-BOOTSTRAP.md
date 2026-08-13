# AST-VIS P0 — Real Visual APK Execution

**Branch:** `agent/real-visual-apk-p0`  
**Priority:** P0  
**Status:** IN REVIEW  
**Goal:** make real image assets visible inside an installable APK as early as possible.

## Tracking issues

- #38 — AST-VIS-001 Prototype Hero Car artwork
- #39 — AST-VIS-002 Cairo Night Racing background
- #40 — AST-VIS-003 Prototype Track visual
- #41 — AST-VIS-004 Main Menu key art
- #42 — AST-VIS-005 Garage showroom background
- #43 — AST-VIS-006 Prototype HUD graphics
- #44 — AST-VIS-007 Drift and Nitro VFX sprite set
- #45 — AST-VIS-008 Optimize visual assets for mobile runtime
- #46 — AST-VIS-009 Integrate real visual assets into Flutter/Flame
- #47 — AST-VIS-010 Build and visually verify first real-assets APK

## Implemented bootstrap slice

This branch deliberately does **not** pretend that missing production art already exists. Instead it proves the end-to-end image packaging path using the existing 256px image candidate already stored in the repository.

Implemented:

1. Added `RealVisualAssets` as the typed runtime image manifest.
2. Registered the existing image candidate in `pubspec.yaml`.
3. Added the image path to `GameAssetLoader` startup loading so packaging mistakes fail early.
4. Added `RealVisualBootstrap`, a short startup visual proof rendered from `Image.asset` with a safe fallback.
5. Updated the live asset registry: AST-060 is `INTEGRATING` for preview-only runtime use.
6. Updated `PROJECT_STATUS.md` with the P0 real-visual milestone and explicit remaining blockers.

## Non-claims

- The 256px candidate is not a final production app icon.
- Hero Car, Cairo Track, environment, HUD and VFX production assets remain missing until their respective issues produce approved exports.
- No Android APK is marked VERIFIED until CI build evidence and screenshot/device review exist.
- No VIS task is promoted to VERIFIED by this bootstrap slice alone.

## Next acceptance gate

The next useful owner-visible milestone is not another documentation batch. It is an Android preview APK where at least:

- startup/main-menu uses a bundled image asset;
- garage or race path shows a production visual asset;
- missing-asset fallback is proven;
- CI format/analyze/tests/build are green;
- screenshot evidence is attached to the Visual Gate review.
