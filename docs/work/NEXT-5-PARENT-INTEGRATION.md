# NEXT-5 — Parent integration batch

State: IN REVIEW

This batch advances five remaining parent tasks without duplicating the additive support policies already present in team PRs.

## Tasks

1. `U3D-009` — structured runtime logging
   - typed channels for Core/Vehicle/Race/UI/Art/Audio/Performance/Release;
   - Info/Warning stripped from non-development players via `Conditional`;
   - Error remains available in release builds.

2. `UVEH-006` — last-valid-checkpoint reset
   - player tracks the nearest valid ordered waypoint while upright;
   - waypoint discovery is cached;
   - existing Reset action returns to the last valid checkpoint when available and clears rigidbody velocity.

3. `URAC-009` — seeded AI personality / avoidance / overtake
   - deterministic rival personality from rival index;
   - lane bias and overtake side are seeded;
   - non-alloc SphereCast avoidance detects cars ahead;
   - throttle/steering/nitro decisions react to traffic without per-frame hit-array allocation.

4. `UPER-003` — material/pooling audit implementation
   - `RuntimeMaterials` caches identical Lit and Trail materials;
   - duplicate runtime material allocations are removed for repeated palette combinations;
   - cache size is exposed for profiler evidence.

5. `UPER-008` — Android release APK/AAB pipeline
   - shared build preparation/helpers exposed inside the Editor assembly;
   - separate release APK and AAB entry points;
   - release builder refuses to run unless Unity custom signing is already configured;
   - signing secrets remain outside repository code and are owned by the UPER-007 process.

## Validation truth

Implementation is complete enough for `IN REVIEW`, not `VERIFIED`.

Required before promotion:
- Unity 6000.5.8f1 exact-head import/compile;
- relevant EditMode/PlayMode regression execution;
- profiler comparison for UPER-003;
- signed release APK/AAB execution for UPER-008;
- real-device validation remains separate.

## Hygiene blocker

GitHub write safety rejected `.meta` creation for `LastCheckpointTracker.cs` and `AfareetReleaseBuild.cs` during this session. Those metadata files must be committed before merge if Unity import has not already generated them on another synchronized branch.

## Operational ledger delta

Before: `IN REVIEW 30 | READY 20 | TODO 9 | BLOCKED 6`.

After this batch: `IN REVIEW 35 | READY 20 | TODO 4 | BLOCKED 6`.

Total remains `65`.
