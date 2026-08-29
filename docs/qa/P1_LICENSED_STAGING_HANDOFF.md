# P1 Licensed Production Staging Handoff

## Purpose

The P1 production-art pipeline has two different provenance phases and they must not be collapsed into one dirty-tree build:

1. **Licensed staging/binding** may create Unity `.meta` files and tracked Hero/Rival production prefabs/provenance.
2. **Exact-SHA candidate tests/build** must start from a clean Git commit containing the approved staging outputs.

`tools/android/stage_production_candidate_windows.ps1` implements phase 1. It deliberately stops before tests or build. The existing `tools/android/run_local_candidate_windows.ps1` remains phase 2 and still requires a clean tree.

This workflow does not mark any task accepted or VERIFIED.

## Prerequisites

- Unity `6000.5.8f1` is installed and licensed on the Windows workstation.
- The real externally-authored Afareet King source package has already been added to the Unity project and committed to Git.
- The starting repository is clean.
- The Hero source path is below Unity `Assets/`, uses `.fbx`, `.obj`, `.blend`, `.glb` or `.gltf`, and is not under a `Generated`, `Preview` or `Blockout` directory.

The requirement to commit the raw artist source first is intentional. It gives the licensed staging pass a stable input commit and prevents an unrelated dirty workspace from being mixed into generated Unity import/provenance output.

## Phase 1 — licensed staging handoff

Example:

```powershell
.\tools\android\stage_production_candidate_windows.ps1 `
  -HeroSource "Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing.fbx"
```

Or provide Unity explicitly:

```powershell
.\tools\android\stage_production_candidate_windows.ps1 `
  -HeroSource "Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing.fbx" `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe"
```

The script launches Unity in batch mode and executes:

`Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit`

That entry point:

- validates the imported Hero source before staging mutation;
- stages UART-005 Cairo world source packaging;
- stages UART-006 landmarks;
- stages UART-007 track dressing;
- stages/binds all three UART-004 Rival prefabs from their tracked authored model sources;
- stages/binds UART-003 Hero from the supplied real external source;
- writes `artifacts/production-staging/p1-staging-handoff.json` with `state=STAGED_FOR_COMMIT_NOT_CANDIDATE`, `publicationEligible=false`, `verified=false`.

The PowerShell wrapper then records exact Git status to `artifacts/production-staging/p1-staging-handoff.git-status.txt` and rejects any staging change outside `unity_game/Assets/`.

It never runs `git add`, `git commit`, tests, or a Player build.

## Commit boundary

If the marker is:

`AFAREET_P1_STAGING_COMMIT_REQUIRED`

review the exact `unity_game/Assets/` changes. Expected changes can include Unity import metadata and source-backed Hero/Rival production prefab/provenance outputs. Do not commit unexpected package, project-setting, tool, or documentation changes as part of this handoff.

Commit only the reviewed staging/source-import outputs. That new commit becomes the potential exact candidate SHA.

A staging pass is not evidence that runtime integration or owner visual acceptance passed. The task/manifest states must remain truthful and fail closed until their actual licensed/runtime/device/owner requirements are met.

## Phase 2 — clean exact-SHA candidate

Only after approved staging changes are committed and `git status` is clean, run:

```powershell
.\tools\android\run_local_candidate_windows.ps1
```

That existing orchestrator performs package/text preflights, Unity EditMode/PlayMode tests, Android build and exact candidate verification while failing if Unity changes tracked repository content.

Do **not** modify the candidate runner to invoke phase-1 tracked staging automatically. Building from a tree modified by staging would make the APK source state differ from its claimed Git SHA.

## Ignored Resources staging

UART-005/UART-006/UART-007 source packaging under ignored Unity Resources is deterministic and is not itself a commit boundary. The existing build path already rematerializes these sources:

- `AfareetBuild.PrepareProject()` stages UART-005 world resources;
- `P1ProductionLandmarkBuildPreprocessor` stages UART-006 landmarks;
- `P1ProductionTrackDressingBuildPreprocessor` stages UART-007 dressing.

Therefore a clean workstation does not depend on ignored Resources left behind by a previous staging session.

## Current blocker truth

This handoff closes orchestration risk only. It does not provide the missing real Afareet King model, Unity license credentials on hosted CI, a new APK, device evidence, `UPER-009` owner acceptance or `UPER-010` publication approval.

The fixed U-P1 register remains `54 IN REVIEW / 11 BLOCKED = 65`; `verified=false`.
