# U-P1 side-branch integration recovery

## Purpose
Restore reconciled U-P1 deliverables that were counted IN REVIEW on owning side PRs but were absent from the current production tree.

## Production base
- parent: PR #97 / `agent/U3D-012-android-ci-artifact`
- exact base: `9685f8a05517a683011dbcf810c5cbc396da32bd`
- tracker: Issue #101

## Exact owning sources ported
- PR #51 / UPER-001: Android device-tier/performance budget contract.
- PR #52 / UART-001: human + machine-readable 3D asset pipeline contract.
- PR #53 / U3D-010: PlayMode test assembly + race-start tests.
- PR #58 / URAC-006: track boundary/off-road policy, runtime, tests.
- PR #60 / URAC-008: corner speed/racing-line policies + tests.
- PR #61 / URAC-010: rival motion/reset guards + tests.
- PR #84 / UVEH-002: WheelCollider suspension prototype + tests/evidence.

## Integration rule
Production/test/evidence files are ported with the exact Git blobs from their owning PR heads, preserving Unity `.meta` GUIDs. Stale `docs/PROJECT_STATUS.md`, `docs/MODULE_OWNERSHIP.md`, and `docs/tasks/06-UNITY-3D-MIGRATION.md` snapshots from side branches are deliberately excluded.

Current production `AiRacer`, `RaceDirector`, vehicle controller/config and other newer integration files are not replaced by older side-branch versions.

## State guard
This recovery does not add tasks or change the operational task count. It makes the production tree match work already counted IN REVIEW. Exact Unity compile/tests remain blocked by missing Actions licensing secrets (Issue #98); no VERIFIED claim is made.
