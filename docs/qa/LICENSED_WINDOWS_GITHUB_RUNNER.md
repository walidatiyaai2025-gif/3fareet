# Licensed Windows GitHub Runner — Exact-SHA Unity Candidate

**Purpose:** make the licensed Windows fallback traceable in GitHub Actions without storing Unity credentials in the repository or weakening any P1 verification gate.

## What this solves

Hosted `Unity Production CI` and `Unity Experimental APK` require GitHub secrets for Unity licensing. The Windows fallback can use Unity `6000.5.8f1` on a self-hosted Windows x64 runner whose Unity installation is already licensed locally.

`.github/workflows/unity-licensed-windows-candidate.yml` exposes two explicit modes:

- `production` — the existing full tests + production Android candidate/evidence chain;
- `experimental` — the isolated unified ARM64 development APK path, producing `afareet-unity3d-experimental.apk` without claiming release/device verification.

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
- the working tree is clean;
- `candidate_mode` is exactly `production` or `experimental`;
- the selected mode's runner script exists.

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

For the current convergence flow, use:

- `candidate_ref`: `agent/p1-remediation-convergence`;
- `expected_sha`: the exact reviewed 40-character convergence head;
- `candidate_mode`: `production` or `experimental`;
- `unity_path`: the licensed Unity `6000.5.8f1` executable on that runner.

For a later canonical integration proof, use:

- `candidate_ref`: `agent/unblock-final-5`;
- `expected_sha`: the exact reviewed canonical integration head;
- `candidate_mode`: the intended evidence class.

If the selected branch moves between inspection and checkout, the dispatch fails closed. Review the new commits and start a new dispatch with the new SHA; never edit evidence to make an older run appear current.

## Experimental APK mode

Choose `candidate_mode=experimental` when the goal is the first current unified APK and hosted GameCI cannot run because Unity Actions credentials are absent.

The workflow calls:

`tools/android/build_experimental_windows.ps1`

That path:

1. requires a clean exact-SHA checkout and Unity `6000.5.8f1` with Android support;
2. executes `Afareet.Editor.AfareetBuild.BuildAndroidExperimental`;
3. builds `com.fiftysolutions.afareetunity3d` as API 26 / ARM64;
4. preserves the explicit procedural Hero fallback only for the experimental build;
5. validates package id, minSdk, ABI and `libunity.so`;
6. computes SHA-256 and writes `artifacts/android-experimental/artifact-metadata.json`;
7. forces `artifactClass=experimental`, `releaseEvidenceEligible=false` and `physicalDeviceVerified=false`.

The workflow re-checks those metadata invariants before uploading the artifact. Experimental mode never emits the production candidate manifest and never promotes release/device state.

## Production-art staging boundary

If the real Hero/Rival Unity import outputs have not yet been committed, first use the separate licensed staging handoff documented in `P1_LICENSED_STAGING_HANDOFF.md` before a `production` run.

That phase may create tracked prefab/import metadata and must stop for review/commit. The production candidate workflow starts only after the approved staging outputs are committed to the selected ref and the selected exact SHA is clean.

The experimental mode does not convert blockout/procedural art into production evidence.

## Production execution chain

With `candidate_mode=production`, the workflow delegates to:

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

- `artifacts/local-candidate-manifest.json` for production mode;
- `artifacts/unity-local-tests/` for production mode;
- `artifacts/android-local/` for production mode;
- `artifacts/android-experimental/` for experimental mode, including APK, SHA-256 and metadata;
- `artifacts/logs/` including Unity and fail-closed diagnostics.

The artifact name includes the selected mode and exact pinned Git SHA.

## UPER-006 physical-device performance handoff

After a successful **production** candidate run, use the production manifest when collecting performance evidence. Do not type a remembered Git SHA into the evidence file and do not collect against an experimental APK.

On a device-evidence workstation with Python 3 and ADB available:

```powershell
python tools/android/collect_uper006_performance.py `
  --apk artifacts/android-local/afareet-unity3d-debug.apk `
  --candidate-manifest artifacts/local-candidate-manifest.json `
  --serial <ADB_SERIAL> `
  --output artifacts/device-evidence/uper006-performance-evidence.json
```

Before running the collector, install that exact production APK on the selected physical device and run it long enough for the in-app `uper006-performance-baseline.json` report to reach the required sample count.

The collector fails closed unless:

- the candidate manifest is schema-compatible and identifies `local-windows-licensed-unity`;
- manifest `releaseEvidenceEligible=true`, `readyForDeviceEvidence=true`, `verified=false`, and verdict remains `READY_FOR_PHYSICAL_DEVICE_EVIDENCE`;
- manifest Git SHA is a full exact SHA;
- manifest package id matches the requested package;
- manifest APK SHA-256 matches the local APK bytes;
- the selected ADB target is a real connected `device` state;
- `pm path` resolves to one standalone installed APK rather than an ambiguous split-package set;
- the installed `base.apk` bytes hash to the **same SHA-256** as the candidate APK file;
- the runtime report has the UPER-006 schema/evidence id, required metric/sample fields, and the same Unity version as the licensed candidate manifest.

The resulting envelope records candidate-manifest hash, Git SHA, local APK hash, installed APK path/hash, ADB serial, reported device/GPU/OS and runtime metrics. Its verdict intentionally remains `COLLECTED_NOT_VERIFIED`; provenance is not the same as acceptance.

`--git-sha` remains a legacy collection option for non-manifest diagnostics only. Evidence intended for P1 review should use `--candidate-manifest` so the licensed test/build chain is retained end-to-end.

## Remaining gates

A successful `experimental` run means a current unified test APK exists; it does **not** make it release evidence or physical-device verified.

A successful `production` licensed Windows workflow means only that the exact candidate is **ready for physical-device evidence**. It does not complete:

- `UVEH-012` driving feel;
- `URAC-012` lap/results/restart device verification;
- `UPER-006` device smoke/performance matrix;
- `UPER-009` visual gate;
- `UPER-010` verified APK publication.

Those tasks still require candidate-bound device/manual evidence before promotion.
