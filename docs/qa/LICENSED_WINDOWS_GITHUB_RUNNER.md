# Licensed Windows GitHub Runner — Exact-SHA Unity Candidate

**Purpose:** make the licensed Windows fallback traceable in GitHub Actions without storing Unity credentials in the repository or weakening any P1 verification gate.

## What this solves

Hosted `Unity Production CI` still requires GitHub secrets for Unity licensing. The Windows fallback can run Unity `6000.5.8f1`, tests, Android build and candidate verification on a self-hosted Windows x64 runner whose Unity installation is already licensed locally.

`.github/workflows/unity-licensed-windows-candidate.yml` bridges that gap by running the existing production candidate script and attaching exact-SHA evidence to GitHub Actions.

The workflow does not request, echo, upload or commit Unity credentials.

## Why the candidate ref is selectable

P1 remediation is intentionally converged in `agent/p1-remediation-convergence` **before merging** into the canonical integration branch `agent/unblock-final-5`.

A licensed workflow hard-wired only to `agent/unblock-final-5` creates a circular gate: the convergence PR cannot be merged until licensed Unity proof exists, but the licensed workflow cannot prove the convergence SHA until it is merged.

The workflow therefore accepts a `candidate_ref` input, but it is a GitHub Actions `choice` with a strict two-ref allowlist:

- `agent/p1-remediation-convergence` — pre-merge remediation proof;
- `agent/unblock-final-5` — canonical integration proof after the preceding gates legitimately allow integration.

No arbitrary PR branch, tag or user-supplied ref is accepted.

## Security boundary

The self-hosted job validates `candidate_ref` against the same explicit allowlist **before `actions/checkout` runs**. That ordering matters: an arbitrary repository ref must not be checked out onto the licensed self-hosted machine and then validated afterward.

After the allowlist guard, checkout uses only the validated step output. The operator must also explicitly supply the current full `expected_sha`; there is intentionally **no default SHA** because either allowed branch can move while another agent is working.

The job fails before Unity starts unless:

- the selected ref is one of the two approved production refs;
- checked-out `HEAD` exactly matches the supplied 40-character `expected_sha`;
- the working tree is clean.

`actions/checkout` uses `persist-credentials: false` on the self-hosted job.

This permits licensed proof on convergence before integration without weakening the exact-SHA or arbitrary-code boundary.

## One-time runner requirement

Use a Windows x64 machine with:

- GitHub self-hosted runner registered for this repository;
- labels `self-hosted`, `Windows`, `X64`;
- Git + Git LFS available;
- Unity `6000.5.8f1` installed and already licensed for the interactive/service account that runs the GitHub runner;
- Android support installed for that Unity editor.

Default Unity path:

`C:\Program Files\Unity\Hub\Editor\6000.5.8f1\Editor\Unity.exe`

The path can be changed at dispatch time without changing the workflow.

## Exact candidate dispatch

Before every dispatch, read the current head of the intended allowed production ref from GitHub and review any commits that landed since the last inspected SHA. Do not reuse an old SHA from documentation, a previous workflow run or a previous chat message.

For the current pre-merge remediation flow, use:

- `candidate_ref`: `agent/p1-remediation-convergence`;
- `expected_sha`: the exact reviewed 40-character convergence head;
- `unity_path`: the licensed Unity `6000.5.8f1` executable on that runner.

For a later canonical integration proof, use:

- `candidate_ref`: `agent/unblock-final-5`;
- `expected_sha`: the exact reviewed canonical integration head.

If the selected branch moves between inspection and checkout, the dispatch fails closed. Review the new commits and start a new dispatch with the new SHA; never edit evidence to make an older run appear current.

## Production-art staging boundary

If the real Hero/Rival Unity import outputs have not yet been committed, first use the separate licensed staging handoff documented in `P1_LICENSED_STAGING_HANDOFF.md`.

That phase may create tracked prefab/import metadata and must stop for review/commit. The licensed candidate workflow starts only after the approved staging outputs are committed to the selected ref and the selected exact SHA is clean.

Do not run a candidate build from a working tree modified by tracked staging.

## Execution chain

The workflow delegates production tests/build logic to:

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
- `artifacts/android-local/` including APK and SHA-256 evidence;
- `artifacts/logs/` including fail-closed dirty-tree diagnostics.

The artifact name is bound to the exact pinned Git SHA.

## Remaining gates

A successful licensed Windows workflow means only that the exact candidate is **ready for physical-device evidence**. It does not complete:

- `UVEH-012` driving feel;
- `URAC-012` lap/results/restart device verification;
- `UPER-006` device smoke/performance matrix;
- `UPER-009` visual gate;
- `UPER-010` verified APK publication.

Those tasks still require candidate-bound device/manual evidence before promotion.
