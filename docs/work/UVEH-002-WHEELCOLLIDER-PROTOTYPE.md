# UVEH-002 — WheelCollider suspension prototype

## State
- Task: `UVEH-002`
- Parent ADR: `docs/adr/0002-unity-suspension-wheelcollider.md`
- Parent PR: #66 (`agent/unity-support-10tasks`)
- Issue: #83
- Branch: `agent/UVEH-002-wheelcollider-prototype`
- Status: IN REVIEW

## Goal
Complete the prototype half of the accepted P1 suspension ADR without modifying `ArcadeCarController` or changing the existing Rigidbody driving contract.

## Implemented
- `WheelSuspensionMath` derives per-wheel spring and damper coefficients from Rigidbody mass, supported wheel count, natural frequency and damping ratio.
- `WheelColliderSuspensionPrototype` is an opt-in component that accepts explicit WheelColliders or auto-discovers child WheelColliders.
- The prototype validates positive mass, unique/non-null wheel references, frequency, damping ratio, suspension distance, target position and force/damping distances before applying changes.
- Applying prototype tuning updates only WheelCollider suspension parameters: spring, damper, target position, suspension distance, force application point and wheel damping rate.
- No throttle, steering, Nitro/Drift, Race/AI, camera, audio or build contract is changed.

## Tests committed
`WheelColliderSuspensionPrototypeTests.cs` contains 6 EditMode tests covering:
1. spring-rate frequency-squared scaling;
2. damper scaling with damping ratio;
3. invalid math inputs;
4. child WheelCollider auto-discovery and applied tuning;
5. duplicate wheel rejection;
6. missing-wheel rejection.

## Acceptance mapping
- Decision: WheelCollider baseline is documented in ADR 0002.
- Prototype: committed as an additive vehicle component.
- Tunability: all prototype values are serialized/configurable and applied deterministically.
- Measurement hook: natural frequency/damping inputs define the measurable baseline; physical Android contact/oscillation/penetration checks remain device evidence gates.

## Validation truth
- Exact-head Unity 6000.5.8f1 import/compile: NOT EXECUTED in this connector session.
- EditMode tests: committed, NOT EXECUTED.
- Android/device suspension measurements: NOT EXECUTED.
- VERIFIED: No.

## Remaining QA
1. Import/compile the exact PR head in Unity 6000.5.8f1.
2. Run `Afareet.EditModeTests` including the suspension tests.
3. Add the prototype component to the P1 Hero Car candidate and assign/discover four WheelColliders.
4. Record grounded-wheel stability, landing oscillation, visible penetration and 30/45/60 FPS behavior on Android before promotion to VERIFIED.
