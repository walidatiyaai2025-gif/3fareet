# P1 Licensed Staging Readiness Audit

## Purpose

`tools/android/p1_licensed_staging_readiness.py` is a read-only, fail-closed preflight for the licensed Unity production-art staging handoff introduced by PR #157.

It does **not** run Unity, create/import production prefabs, run tests, build an APK, capture device evidence, or change any UART/UPER acceptance state. A `READY_FOR_LICENSED_STAGING` verdict means only that the exact clean Git checkout has the structural inputs needed to start the separate licensed staging phase.

The report always keeps:

- `candidateBuildStarted=false`;
- `publicationEligible=false`;
- `verified=false`.

## Current expected state

Until the real owner-approved externally-authored Afareet King Hero source is committed, running the audit without `--hero-source` must return `BLOCKED` and include `UART-003_HERO_SOURCE_SUPPLIED` in `blockedCheckIds`.

This is expected and must not be worked around with the Generated Hero V2, Preview/Blockout content, or one of the Rival models.

## What is checked

The audit verifies:

1. `--repo-root` is the exact Git worktree root;
2. `HEAD` resolves to a full Git SHA;
3. the worktree is clean for a production staging attempt;
4. the #157 handoff entry point, Windows wrapper, all five staging adapters, exact-SHA candidate runner and licensed workflow are tracked;
5. UART-005/UART-006/UART-007 tracked manifests are present;
6. the three UART-004 Rival OBJ + MTL + base-color texture source chains are tracked;
7. an explicit Hero source was supplied;
8. the Hero resolves under `unity_game/Assets/` (or is passed as Unity `Assets/...`);
9. the Hero extension is one of `.fbx`, `.obj`, `.blend`, `.glb`, `.gltf`;
10. the Hero path does not contain `Generated`, `Preview` or `Blockout`;
11. the Hero source exists and is tracked by the current Git HEAD;
12. a Rival source is not being reused as the Hero source.

The audit intentionally does not pretend to validate Unity import semantics, LOD renderer structure, material bindings or runtime rendering. Those remain the responsibility of the licensed Unity staging/binding gates and later exact-candidate/device/owner evidence.

## Report current blocker state

From the repository root:

```powershell
python tools/android/p1_licensed_staging_readiness.py --allow-blocked
```

A non-reporting production preflight omits `--allow-blocked`; a BLOCKED verdict then returns a non-zero exit code.

## Validate a delivered Hero source before Unity

After the real source package has been reviewed and committed on the convergence line:

```powershell
python tools/android/p1_licensed_staging_readiness.py `
  --hero-source Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx `
  --output artifacts/p1/licensed-staging-readiness.json
```

`--output`, when used, is confined to `<repo>/artifacts/` so the audit cannot dirty tracked source through its own report.

Do not proceed unless the state is exactly:

```text
READY_FOR_LICENSED_STAGING
```

## Next phase after READY_FOR_LICENSED_STAGING

Run the separate licensed Unity staging command on Unity `6000.5.8f1`:

```powershell
.\tools\android\stage_production_candidate_windows.ps1 `
  -HeroSource Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx
```

That command must stop at `STAGED_FOR_COMMIT_NOT_CANDIDATE`. Review the generated/imported Unity metadata, production Hero/Rival prefabs and provenance changes, then commit only approved outputs.

Only the resulting **new clean SHA** may be passed to `run_local_candidate_windows.ps1` / `Unity Licensed Windows Candidate` for licensed tests and Android build.

## Acceptance boundary

Neither this audit nor a successful staging pass closes UART-003/004/005/006/007, URAC-011, UVEH-012, URAC-012, UPER-006, UPER-009 or UPER-010. The fixed operational register remains fail-closed until the exact same candidate completes the licensed Unity, Android device, performance, owner visual and final manual publication gates.
