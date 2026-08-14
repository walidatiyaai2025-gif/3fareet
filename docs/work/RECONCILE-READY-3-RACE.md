# Reconcile READY Race Tasks — URAC-006 / URAC-008 / URAC-010

## Purpose
The running 65-task operational ledger on PR #79 still counts three Race/AI tasks as READY even though their owning team PRs already implement them and explicitly mark them IN REVIEW. This document corrects the ledger only; it does not duplicate production code.

## Base
- Parent reconciliation PR: #79
- Parent branch: `agent/reconcile-ready-5-race-ci`
- Parent head at branch creation: `302e9dcae9512f5d24018e77aa73843236db8a9c`
- Child branch: `agent/reconcile-ready-3-race`

## Reconciled tasks

### URAC-006 — track bounds / barriers / off-road
Owner PR: #58 (`agent/URAC-006-track-bounds`)

Existing implementation evidence:
- deterministic nearest-segment track-boundary sampling;
- runtime solid edge-collider construction;
- explicit leave/re-enter off-road state events;
- committed EditMode coverage for classification, colliders, events and invalid tracks.

State correction: `READY → IN REVIEW`.

### URAC-008 — AI racing line and braking zones
Owner PR: #60 (`agent/URAC-008-ai-speed-planner`)

Existing implementation evidence:
- deterministic curvature severity / target-speed policy;
- braking demand when overspeeding upcoming corners;
- racing-line lookahead that varies with upcoming curvature;
- nitro suppression when turn/braking demand makes boost unsafe for the racing policy;
- committed EditMode coverage for straights, 90-degree turns, braking and lookahead.

State correction: `READY → IN REVIEW`.

### URAC-010 — AI stuck recovery / lifecycle safeguards
Owner PR: #61 (`agent/URAC-010-ai-recovery`)

Existing implementation evidence:
- deterministic rival lifecycle/recovery safeguards;
- committed EditMode coverage;
- production gameplay files outside the owned slice remain unchanged.

State correction: `READY → IN REVIEW`.

## Team coordination
A reconciliation review comment was posted on PRs #58, #60 and #61 before this ledger correction. No code from those branches is copied into this branch.

## Validation truth
All three tasks remain IN REVIEW, not VERIFIED. Their owning PRs still require exact-head Unity 6000.5.8f1 import/compile and relevant test execution; task-specific device/runtime evidence remains separate.

## Operational ledger
PR #79 ledger before this correction:

`IN REVIEW 45 | READY 12 | TODO 2 | BLOCKED 6 = 65`

After reconciling these three already-implemented tasks:

`IN REVIEW 48 | READY 9 | TODO 2 | BLOCKED 6 = 65`

PR #78 is a sibling branch that independently moves `UAUD-001` and `UAUD-002` from READY to IN REVIEW. If both PR #78 and this reconciliation chain land, the aggregate project ledger becomes:

`IN REVIEW 50 | READY 7 | TODO 2 | BLOCKED 6 = 65`

No task is double-counted by this document.
