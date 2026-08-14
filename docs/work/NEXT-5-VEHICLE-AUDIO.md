# NEXT-5 — Vehicle Feel + Audio Runtime

State: IN REVIEW

## Tasks advanced

### UVEH-003 — Grip / lateral-slip / drift tuning assets
- `ArcadeCarConfig` now owns normal and drift grip-vs-speed curves.
- `ArcadeCarController` evaluates those curves against normalized current speed.
- Surface grip multiplier remains composed with the tuning profile.
- This removes fixed-grip-only behavior from the production controller.

### UVEH-007 — Nitro acceleration / consumption integration
- Nitro is now a stateful burst rather than a raw held-button boolean.
- Config owns minimum activation energy, cooldown, consumption/recharge and force-vs-speed curve.
- Releasing or exhausting a burst starts the cooldown.
- Public `NitroCooldownRemaining` is exposed for UI/VFX diagnostics.

### UVEH-008 — Drift energy charge rules
- Public `DriftEnergy` meter is implemented.
- Charge requires drift input + minimum steer + minimum speed + grounded state.
- Charge is capped, decay is bounded, and a post-drift re-entry guard prevents rapid charge spam.
- Thresholds/rates are all config-owned.

### UAUD-001 — Engine speed layers
- Shared low/high engine loop clips are created once and reused across cars.
- Layer pitch/volume respond to speed and throttle.
- Sources use 3D attenuation and bounded Doppler.
- Runtime baseline is implemented; final imported production engine assets and device listening remain pending.

### UAUD-002 — Drift / Nitro / collision SFX
- Drift loop responds to actual `IsDrifting` state.
- Nitro emits a one-shot on burst entry.
- Collision emits a strength-scaled one-shot above a small impact threshold.
- Clips are shared and generated once; no per-event clip creation occurs.
- Final mix and production sound replacement remain device/audio-review gates.

## Static hygiene
- `VehicleAudioPass.cs.meta` is committed.
- AudioSource setup does not assign `Component.name`, avoiding accidental Player/Rival GameObject renaming.
- Branch is stacked on `agent/next-5-suspension-surface-race`.

## Validation truth
These five tasks move from READY to IN REVIEW, not VERIFIED.

Still required before promotion:
1. Unity 6000.5.8f1 exact-head import/compile.
2. EditMode coverage for Nitro activation/cooldown and Drift Energy guards.
3. PlayMode regression for car reset + trail/audio state.
4. Real-device feel pass for grip/Nitro/Drift tuning.
5. Device listening/mix review and replacement with approved production audio assets where required.

## Operational ledger
Before: `IN REVIEW 50 | READY 7 | TODO 2 | BLOCKED 6`.
After: `IN REVIEW 55 | READY 2 | TODO 2 | BLOCKED 6`.
Total: `65`.
