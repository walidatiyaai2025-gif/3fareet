# Instructions for AI programmers

These instructions apply to the entire repository.

## Before changing files

1. Read `CONTRIBUTING.md`, `docs/ONBOARDING.md`, `docs/PROJECT_STATUS.md`,
   `docs/MODULE_OWNERSHIP.md`, and `docs/TEAM_WORKFLOW.md`.
2. Claim a Task ID and Module Lock before implementation.
3. Work on a dedicated branch and never overwrite another owner's active scope.
4. The production client is `unity_game/`; Flutter is a legacy reference unless a
   `FLT-*` task explicitly says otherwise.

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
