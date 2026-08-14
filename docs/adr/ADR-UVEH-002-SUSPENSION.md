# ADR — UVEH-002 Suspension Decision

Status: IN REVIEW  
Task: UVEH-002  
Decision: keep the current arcade Rigidbody controller for the P1 vertical slice and prototype custom raycast suspension rather than introducing WheelCollider into the active production controller.

## Why

The current car already uses direct Rigidbody acceleration, steering and lateral-grip control. Replacing that model with WheelCollider during P1 would mix two vehicle models and create a high regression risk across drift, nitro, AI and camera work.

A custom raycast prototype is therefore the lower-risk evaluation path. It lets the team measure suspension compression, grounded wheel count and peak spring force while leaving production forces disabled by default.

## Prototype

`CustomSuspensionPrototype.cs` supplies four deterministic local probes and exposes:

- `GroundedProbeCount`;
- `AverageCompression01`;
- `PeakSpringForce`;
- pure `EvaluateSpringForce(...)` math for EditMode coverage.

`applyForces` is false by default. This means importing the prototype cannot silently change the active handling model.

## Measurement gate

Before any production adoption, capture the prototype on the same car/config over:

1. flat asphalt at 0 / 60 / 120 km/h;
2. curb/height transition;
3. drift entry and recovery;
4. hard landing after a small vertical displacement.

Record grounded probes, average compression, peak spring force and whether body oscillation settles without repeated bounce.

## Promotion rule

Only enable custom suspension in production if the measured prototype improves contact stability without degrading drift readability or introducing excessive oscillation. Otherwise keep the P1 Rigidbody baseline and revisit suspension post-vertical-slice.

No WheelCollider adoption is approved by this ADR.
