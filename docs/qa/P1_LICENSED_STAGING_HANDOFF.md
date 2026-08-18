# P1 Licensed Production Staging Handoff

## Purpose

The P1 production-art pipeline has two different provenance phases and they must not be collapsed into one dirty-tree build:

1. **Licensed staging/binding** may create/update Unity import state and tracked Hero/Rival production prefabs/provenance.
2. **Exact-SHA candidate tests/build** must start from a clean Git commit containing the approved staging outputs.

`tools/android/stage_production_candidate_windows.ps1` implements phase 1. It deliberately stops before tests or build. The existing `tools/android/run_local_candidate_windows.ps1` remains phase 2 and still requires a clean tree.

This workflow does not mark any task accepted or VERIFIED.

## Prerequisites

- Unity `6000.5.8f1` is installed and licensed on the Windows workstation.
- The real externally-authored Afareet King **production** source has already been added to the Unity project and committed to Git with its non-empty `.meta` file.
- All three real externally-authored Rival **production** OBJ exchanges have already been committed with their non-empty `.meta` files at exactly:
  - `unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj`
  - `unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj`
  - `unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj`
- The old tracked `Rival_01_WedgeCoupe.obj`, `Rival_02_FastbackMuscle.obj`, and `Rival_03_CompactPrototype.obj` outside `/Rivals/Production/` are authored-review candidates only. They are not licensed-staging production inputs and cannot be copied/renamed/rebound to satisfy UART-004.
- The starting repository is clean.
- The Hero source path is below Unity `Assets/`, uses `.fbx`, `.obj`, `.blend`, `.glb` or `.gltf`, and is not under Generated/Preview/Refinement/Blockout/Rivals/Review paths.
- The three mandatory authorization SHA-256 values passed to the staging wrapper must come from the current authoritative READY/operator handoff chain for the exact starting Git SHA. Do not invent placeholder hashes or reuse values from another commit.

The requirement to commit all raw production vehicle inputs and their Unity metadata first is intentional. Unity imports project assets before the batch entry point executes; requiring tracked `.meta` files prevents the staging session from silently creating source GUID metadata before the read-only handoff preflight runs.

## Phase 1 — licensed staging handoff

`stage_production_candidate_windows.ps1` is **not a standalone unauthorised command**. It requires the current handoff packet, native handoff verification and operator-chain SHA-256 values in addition to the Hero source. Use the exact values emitted/approved by the authoritative READY/operator chain for the same clean Git SHA.

Example shape after those three values have been obtained:

```powershell
$handoffPacketSha256 = '<CURRENT_READY_PACKET_SHA256>'
$nativeHandoffVerificationSha256 = '<CURRENT_NATIVE_HANDOFF_VERIFICATION_SHA256>'
$operatorChainSha256 = '<CURRENT_OPERATOR_CHAIN_SHA256>'

.\tools\android\stage_production_candidate_windows.ps1 `
  -HeroSource "Assets/Afareet/ArtSource/Vehicles/HeroCar/Production/AfareetKing.fbx" `
  -HandoffPacketSha256 $handoffPacketSha256 `
  -NativeHandoffVerificationSha256 $nativeHandoffVerificationSha256 `
  -OperatorChainSha256 $operatorChainSha256
```

Or provide Unity explicitly with the same authorization values:

```powershell
.\tools\android\stage_production_candidate_windows.ps1 `
  -HeroSource "Assets/Afareet/ArtSource/Vehicles/HeroCar/Production/AfareetKing.fbx" `
  -HandoffPacketSha256 $handoffPacketSha256 `
  -NativeHandoffVerificationSha256 $nativeHandoffVerificationSha256 `
  -OperatorChainSha256 $operatorChainSha256 `
  -UnityPath "C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe"
```

The angle-bracket values above are documentation placeholders only; they are not valid authorization material and must never be substituted with arbitrary 64-character strings just to make the script run.

### Pre-Unity fail-fast boundary

Before Unity is launched, the PowerShell wrapper requires all of these files to exist, be non-empty, and already be tracked by Git in the clean starting commit:

- Hero production source;
- Hero production source `.meta`;
- Rival 01 production OBJ + `.meta`;
- Rival 02 production OBJ + `.meta`;
- Rival 03 production OBJ + `.meta`.

It emits `AFAREET_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK ... mutationStarted=false verified=false` only after all seven source/metadata pairs have passed that boundary. If any production Rival source is still missing, phase 1 must stop **before opening Unity** rather than stage unrelated Cairo files first.

The script then launches Unity in batch mode and executes:

`Afareet.Editor.P1ProductionCandidateStagingHandoff.StageForCommit`

That entry point performs a second, Unity-authoritative read-only vehicle preflight **before the first staging mutation**:

- validates the imported Hero production source;
- validates all three isolated UART-004 Rival production sources through `RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow()`;
- only after both vehicle families pass does it stage UART-005 Cairo world source packaging;
- stages UART-006 landmarks;
- stages UART-007 track dressing;
- stages/binds all three UART-004 Rival prefabs from the isolated production source root;
- stages/binds UART-003 Hero from the supplied real external source;
- writes `artifacts/production-staging/p1-staging-handoff.json` with `state=STAGED_FOR_COMMIT_NOT_CANDIDATE`, `publicationEligible=false`, `verified=false`.

The C# entry point emits `AFAREET_P1_STAGING_EXTERNAL_SOURCE_PREFLIGHT_OK ... mutationStarted=false verified=false` before any world/landmark/dressing stager is called. `RivalProductionPrefabStager` keeps its own source preflight as defense-in-depth.

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

This handoff closes orchestration risk only. At the current repository/library state, the final production Hero and three final production Rival sources have not satisfied the production-art acceptance chain, so phase 1 must remain blocked until those actual inputs are delivered and committed. The current Hero refinement candidate and current Rival authored-review OBJ files are explicitly non-production substitutes.

The workflow also does not provide hosted Unity license credentials, a new APK, device evidence, `UPER-009` owner acceptance or `UPER-010` publication approval.

The fixed U-P1 register remains `54 IN REVIEW / 11 BLOCKED = 65`; `verified=false`.