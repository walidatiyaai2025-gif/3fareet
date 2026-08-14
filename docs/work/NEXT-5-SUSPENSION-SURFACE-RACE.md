# NEXT-5 — Suspension, Surface, and Race Reconciliation

State: IN REVIEW

This batch advances five tasks without duplicating work already owned by team PRs.

## New parent implementations

### UVEH-002 — suspension decision + prototype

- decision recorded in `docs/adr/ADR-UVEH-002-SUSPENSION.md`;
- P1 keeps the current arcade Rigidbody model;
- custom four-ray suspension prototype is committed for measurement;
- prototype exposes grounded probe count, average compression and peak spring force;
- force application is disabled by default so this task cannot silently change production handling.

### UVEH-004 — ground detection and surface types

- every `ArcadeCarController` gets a `SurfaceResponseProbe`;
- probe is non-alloc and ignores the vehicle's own rigidbody colliders;
- `Road XX`/normal surfaces remain Asphalt;
- sand/desert/dirt/offroad/shoulder surfaces are OffRoad;
- OffRoad reduces acceleration, lateral grip and maximum forward speed;
- Nitro force is also surface-scaled so boost cannot fully bypass the off-road penalty.

## Existing team implementations reconciled

### URAC-006 — PR #58

Track bounds/barriers/off-road detection implementation already exists and is mergeable. Reconciliation review posted; stays IN REVIEW pending Unity execution.

### URAC-008 — PR #60

Curve-aware AI speed planning, braking-zone logic and racing-line lookahead already exist with committed tests. Reconciliation review posted; stays IN REVIEW pending Unity execution.

### URAC-010 — PR #61

Deterministic rival lifecycle/recovery safeguards already exist with committed tests. Reconciliation review posted; stays IN REVIEW pending Unity execution.

## Validation truth

No task in this batch is promoted to VERIFIED.

Required before promotion:

- Unity 6000.5.8f1 exact-head import/compile;
- EditMode coverage for suspension spring math and surface classification/integration;
- runtime measurement of the suspension prototype before enabling forces;
- race PR test execution once Unity CI licensing is available;
- relevant Android/device gates remain separate.

## Operational ledger delta

Before: `IN REVIEW 45 | READY 12 | TODO 2 | BLOCKED 6`.

After: `IN REVIEW 50 | READY 7 | TODO 2 | BLOCKED 6`.

Total remains `65`.
