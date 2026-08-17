# Instructions for AI programmers

These instructions apply to the entire repository.

## Before changing files

1. Read `CONTRIBUTING.md`, `docs/ONBOARDING.md`, `docs/PROJECT_STATUS.md`,
   `docs/MODULE_OWNERSHIP.md`, `docs/TEAM_WORKFLOW.md`, and
   `EXTERNAL_ASSET_REQUESTS.txt`.
2. Claim a Task ID and Module Lock before implementation.
3. Work on a dedicated branch and never overwrite another owner's active scope.
4. The production client is `unity_game/`; Flutter is a legacy reference unless a
   `FLT-*` task explicitly says otherwise.

## External asset request policy

- `EXTERNAL_ASSET_REQUESTS.txt` at repository root is the mandatory source of
  truth for assets or source material that cannot be completed legitimately by
  repository programming alone.
- Never silently substitute a primitive, generated mesh, procedural placeholder,
  preview, review candidate, unknown-license download, or AI approximation for a
  missing production asset.
- A DEBUG / PREVIEW / REFINEMENT / REVIEW fallback may exist only when explicitly
  classified and must never satisfy a production or P1 visual gate.
- If implementation reaches a genuine external dependency, add or update the
  corresponding request in `EXTERNAL_ASSET_REQUESTS.txt` in the same branch/PR.
  Include the asset name, blocking task, named creation tool, helper script or
  workflow, copy-ready creation prompt, output path/format, technical constraints,
  acceptance criteria, and provenance/license requirement.
- If engineering can provide validation, export, naming, packaging, provenance,
  import, LOD or build tooling around the asset, implement that tooling in the
  repository. Do not use tooling to misclassify generated content as externally
  authored production art.
- Do not turn programming work such as camera logic, race state, HUD behavior,
  import/staging gates, provenance checks, track geometry math, performance
  instrumentation, evidence tooling or CI into external-asset requests.
- Current Product Owner priority: close programming, automation, validation and
  test gaps before visual polish. Resume polish-focused work only when the Product
  Owner explicitly requests it.

## Android release truth

- `Latest Built APK` means build success only.
- `Last Verified APK` means the exact APK passed the real-device checklist in
  `docs/SMOKE_TEST_CHECKLIST.md` and has recorded evidence.
- Never describe an emulator-only, CI-only, or locally built APK as verified.
- Never replace the Last Verified pointer with an unverified newer build.
- Verified APK binaries are GitHub Release assets, not Git commits.
- The single source of truth is `docs/releases/LAST_VERIFIED_APK.md`.
- Every release-affecting PR must complete the Last Verified section in the PR
  template and update the pointer only after real-device approval.

## Required handoff

Report Task ID, branch, changed files, tests, build result, artifact status
(`Built` or `Device Verified`), commit SHA, APK SHA-256, remaining risks, and PR.
