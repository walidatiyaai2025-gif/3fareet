# UVEH-002 — WheelCollider suspension prototype

## State
- Task: `UVEH-002`
- Parent ADR: `docs/adr/0002-unity-suspension-wheelcollider.md`
- Parent PR: #66 (`agent/unity-support-10tasks`)
- Status: implementation in progress on a child branch

## Goal
Complete the prototype half of the accepted P1 suspension ADR without modifying `ArcadeCarController` or changing the existing Rigidbody driving contract.

## Prototype contract
- four-wheel (or N-wheel) WheelCollider rig remains opt-in and additive;
- spring rate derives from Rigidbody mass, wheel count and target natural frequency;
- damper rate derives from the same sprung mass plus damping ratio;
- suspension distance, target position and force application distance remain tunable;
- invalid mass, wheel count, frequency or damping values are rejected deterministically;
- applying prototype tuning never changes throttle, steering, race, camera, audio or build systems.

## Validation truth
No Unity execution is claimed until the exact child-branch head is imported and the EditMode tests run.
