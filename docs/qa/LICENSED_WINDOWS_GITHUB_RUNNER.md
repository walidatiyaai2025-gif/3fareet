# Licensed Windows GitHub Runner — Exact-SHA Unity Candidate

**Purpose:** make the existing licensed Windows fallback traceable in GitHub Actions without storing Unity credentials in the repository or weakening any P1 verification gate.

## What this solves

Hosted `Unity Production CI` still requires GitHub secrets for Unity licensing. The local Windows fallback can already run Unity `6000.5.8f1`, tests, Android build and candidate verification, but local execution alone is not automatically attached to a GitHub workflow run.

`.github/workflows/unity-licensed-windows-candidate.yml` bridges that gap by running the existing production candidate script on a **self-hosted Windows x64 runner whose Unity installation is already licensed locally**.

The workflow does not request, echo, upload or commit Unity credentials.

## Security boundary

The self-hosted job deliberately does **not** accept an arbitrary Git ref. It always checks out:

`agent/unblock-final-5`

The operator supplies only an `expected_sha`. The job fails before Unity starts unless the checked-out branch head exactly matches that 40-character SHA. This prevents an accidental branch move from producing evidence for a different revision and prevents the workflow from being used to execute an arbitrary PR branch on the self-hosted machine.

`actions/checkout` also uses `persist-credentials: false` for the self-hosted job.

## One-time runner requirement

Use a Windows x64 machine with:

- GitHub self-hosted runner registered for this repository;
- default labels `self-hosted`, `Windows`, `X64`;
- Git + Git LFS available;
- Unity `6000.5.8f1` installed and already licensed for the interactive/service account that runs the GitHub runner;
- Android support installed for that Unity editor.

Default Unity path used by the workflow:

`C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe`

The path can be changed at dispatch time without changing the workflow.

## Current exact candidate dispatch

For the current canonical production head:

`61364edf05ce72fd1aaf98ecd6ce28f4d4a12a55`

Open **Actions → Unity Licensed Windows Candidate → Run workflow** and keep/enter:

- `expected_sha`: `61364edf05ce72fd1aaf98ecd6ce28f4d4a12a55`
- `unity_path`: the licensed Unity `6000.5.8f1` executable on that runner.

If `agent/unblock-final-5` moves, the old SHA dispatch fails closed. Use the new exact head only after reviewing the new commits.

## Execution chain

The workflow delegates all production logic to the existing:

`tools/android/run_local_candidate_windows.ps1`

That chain remains authoritative:

1. clean-tree assertion;
2. stale evidence purge;
3. LF materialization + strict text-normalization preflight;
4. Unity package graph preflight;
5. licensed EditMode + PlayMode execution;
6. clean-tree assertion after Unity tests;
7. Android ARM64 debug APK build and inspection;
8. clean-tree assertion after build;
9. exact-SHA/APK candidate verification;
10. candidate manifest generation.

The workflow adds a final guard that requires:

- manifest `gitSha` equals the dispatch SHA;
- `releaseEvidenceEligible` is JSON boolean `true`;
- `readyForDeviceEvidence` is JSON boolean `true`;
- `verified` is JSON boolean `false`;
- verdict is `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`.

It therefore cannot promote a candidate to VERIFIED.

## GitHub evidence

The workflow uploads, when present:

- `artifacts/local-candidate-manifest.json`;
- `artifacts/unity-local-tests/`;
- `artifacts/android-local/` including the APK and SHA-256 evidence;
- `artifacts/logs/` including fail-closed dirty-tree diagnostics.

Artifact name is bound to the exact pinned Git SHA.

## Remaining gates

A successful licensed Windows workflow means only that the exact candidate is **ready for physical-device evidence**. It does not complete:

- `UVEH-012` driving feel;
- `URAC-012` lap/results/restart device verification;
- `UPER-006` device smoke/performance matrix;
- `UPER-009` visual gate;
- `UPER-010` verified APK publication.

Those five tasks still require candidate-bound device/manual evidence before promotion.
