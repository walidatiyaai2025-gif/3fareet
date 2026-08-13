## Task / Scope
- Task IDs:
- Owner:
- Reviewer:
- Module Lock row/link:
- Summary:

## Change classification
- [ ] Unity production (`unity_game/`)
- [ ] Flutter legacy (`lib/`, `test/`, root assets/tooling)
- [ ] Art/asset source or export
- [ ] Docs/governance
- [ ] Architecture/shared contract (requires 2 reviewers)

## Validation
- [ ] Build / tests required for this change passed.
- [ ] No generated cache/build/APK/secrets were committed.
- [ ] Acceptance Criteria for every Task ID are satisfied.
- [ ] I updated project/task status when the change affects a tracked task or milestone.
- [ ] I added screenshots/video/log/device evidence appropriate to the task.

## Evidence
- Unity/Flutter version:
- Commands/checks:
- Device + OS/API (if applicable):
- Screenshots/video/logs:

## Shared-contract coordination
- [ ] This PR does not change a shared contract; or Tech Lead coordination is linked.
- [ ] Active Module Locks in `docs/MODULE_OWNERSHIP.md` are accurate.

## Asset coordination — mandatory when this PR touches assets
- [ ] I checked `docs/MISSED_ASSETS.md` before starting work.
- [ ] The asset has an AST-ID / registry entry and no conflicting active Owner.
- [ ] I updated Owner / Status / Target path / Branch-PR in `docs/MISSED_ASSETS.md` in this same PR.
- [ ] I did not overwrite a `reference` file or another designer's active `source` silently.
- [ ] Editable source is kept in the appropriate `source/` path and integration-ready files in `exports/` (or `exports_candidate/` until approved).
- [ ] If the asset is `CLAIMED`, `IN_PROGRESS`, `REVIEW`, `READY`, or `INTEGRATING` by another Owner, Team Lead approval is documented before parallel/replacement work.

> Asset PRs missing the registry update are not Ready for Review.
