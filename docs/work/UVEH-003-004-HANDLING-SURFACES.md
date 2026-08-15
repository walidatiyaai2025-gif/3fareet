# UVEH-003 / UVEH-004 — production handling and ground surfaces

## State
- UVEH-003: IN REVIEW
- UVEH-004: IN REVIEW
- Tracking issue: #86
- Parent PR: #82 (`agent/UVEH-007-008-spirit-energy`)
- Branch: `agent/UVEH-003-004-handling-surfaces`

## Team boundary
PR #65 was reviewed before implementation. Its additive support-model files are not copied, renamed or modified here. This slice owns the production integration contract on the latest vehicle controller instead of creating add/add conflicts with that support branch.

## UVEH-003 — grip / slip / traction
- `VehicleHandlingPolicy` provides deterministic throttle traction limiting after a configurable lateral-slip threshold.
- Drift grip is a continuous blend driven by drift request, steer magnitude and real local lateral slip instead of a binary grip switch.
- Effective grip applies the active surface multiplier after the road/drift blend.
- `ArcadeCarConfig` owns traction threshold/strength and drift-grip blend thresholds; no new production handling tuning is hidden in `FixedUpdate`.
- `ArcadeCarController` consumes the policy using local Rigidbody velocity before lateral correction.

## UVEH-004 — ground / surface behavior
- `ArcadeGroundSurfaceSensor` performs a non-alloc downward physics probe before vehicle physics via `DefaultExecutionOrder(-200)`.
- The probe ignores colliders belonging to the car hierarchy.
- Existing Cairo runtime names classify deterministically: `Road`/`Asphalt`/`Rune` → Asphalt; `Desert`/`Sand`/`Ground` → OffRoad.
- `ArcadeSurfaceMarker` explicitly supports future Boost/Slippery patches without name coupling.
- Unknown physical ground is conservatively treated as OffRoad until explicitly marked.
- OffRoad/Boost/Slippery grip, acceleration and max-speed multipliers are data-driven in `ArcadeCarConfig` and the production config asset.
- Grounded state gates drive force, brake, steering, drift-energy charging and lateral-grip correction, preventing the previous mid-air traction correction behavior.

## Tests committed
`VehicleHandlingSurfaceTests.cs` contains EditMode coverage for:
1. traction unchanged below slip threshold;
2. traction reduction above threshold;
3. drift blend request/steer/slip guards and full blend;
4. effective grip + surface multiplier composition;
5. OffRoad reduction and Boost acceleration/max-speed profile behavior;
6. invalid traction/surface config rejection;
7. existing Cairo Road/Desert name classification;
8. explicit marker override;
9. a real physics probe that ignores the vehicle collider and finds the Desert collider below.

## Scope guard
No changes to PR #82 audio files or `VehicleSpiritPolicy`; no Race/AI/UI/World generator/Packages/ProjectSettings/build/release/art files are changed.

## Validation truth
- Exact-head Unity 6000.5.8f1 import/compile: NOT EXECUTED in this connector session.
- EditMode tests: committed, NOT EXECUTED.
- Real-device handling/surface feel: NOT EXECUTED.
- VERIFIED: No.

## Remaining QA
1. Import/compile exact PR head in Unity 6000.5.8f1.
2. Run the complete existing EditMode suite plus `VehicleHandlingSurfaceTests`.
3. Verify the runtime Cairo road reports Asphalt and the surrounding desert reports OffRoad on a physical Android candidate.
4. Confirm ordinary steering below slip threshold does not over-trigger traction limiting.
5. Confirm drift grip transitions smoothly and does not snap at drift-button edges.
6. Confirm airborne state preserves momentum without grounded steering/grip corrections.
7. Tune only the config asset from measured device feel evidence before VERIFIED promotion.
