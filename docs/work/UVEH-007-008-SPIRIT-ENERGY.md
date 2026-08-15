# UVEH-007 / UVEH-008 — Spirit Energy Integration

## State
- UVEH-007: IN REVIEW
- UVEH-008: IN REVIEW
- Tracking issue: #81
- Parent/base: PR #78 (`agent/UAUD-001-002-runtime-audio`)
- Base head selected: `b4adf0a9902fac54f48e6b96577abb814d224a2b`
- Branch: `agent/UVEH-007-008-spirit-energy`

## UVEH-007 — Nitro integration
- Nitro activation now requires configured minimum speed and minimum energy.
- Nitro activation is blocked while cooldown is active.
- Nitro force is scaled by a deterministic smooth speed curve rather than one constant force at every speed.
- Nitro energy consumption and recharge use a deterministic policy and remain clamped to `[0,1]`.
- Recharge is suppressed during cooldown so release/repress cannot immediately bypass the anti-spam window.
- Controller exposes `NitroEnergy`, `NitroCooldownRemaining`, `NitroReady` and `NitroActive` for HUD/presentation consumers.
- All tuning values live in `ArcadeCarConfig` and the production config asset.

## UVEH-008 — Drift energy
- Controller measures lateral slip from local Rigidbody velocity before lateral grip correction.
- Drift charge requires explicit drift input plus minimum steer, speed and slip thresholds.
- Valid drift slip charges `DriftEnergy`; invalid/non-drift state decays it.
- Charge gain scales with lateral slip up to a configured full-gain slip threshold.
- Controller exposes `DriftEnergy` and `DriftChargeActive` for later HUD/gameplay consumers.
- Abuse guards and gain/decay tuning are data-driven in `ArcadeCarConfig`.

## Deterministic policy
`VehicleSpiritPolicy` owns pure/testable rules for:
- Nitro force scaling.
- Cooldown progression.
- Nitro activation eligibility.
- Nitro consume/recharge state.
- Drift charge eligibility.
- Drift energy gain/decay.

The policy does not read Input, scenes, audio or race state.

## Automated coverage committed
`VehicleSpiritPolicyTests.cs` adds EditMode coverage for:
1. Nitro force curve progression/cap.
2. Cooldown activation blocking.
3. Nitro minimum energy/speed gating.
4. No recharge during blocked cooldown.
5. Recharge and clamp when allowed.
6. Drift steer/speed/slip eligibility guards.
7. Drift energy charging with eligible slip.
8. Drift energy decay when ineligible.
9. Valid production config defaults and ordered slip thresholds.

## Scope guard
This slice does not modify PR #78 audio presentation files, Race/AI, World, UI, ProjectSettings, Packages, Android/release, or art assets.

## Validation truth
The implementation and tests are committed, but Unity `6000.5.8f1` exact-head import/compile and EditMode execution were not available in this connector session. No Green or VERIFIED claim is made.

## Remaining QA
1. Import/compile exact PR head in Unity 6000.5.8f1.
2. Execute existing EditMode tests plus `VehicleSpiritPolicyTests`.
3. On device, verify Nitro release starts cooldown and held input cannot spam reactivation.
4. Verify Nitro force ramps naturally with speed and the meter/cooldown state remain readable.
5. Verify normal steering does not charge Drift Energy without sufficient lateral slip.
6. Tune speed/slip/gain thresholds from device feel evidence without moving them back into controller constants.
