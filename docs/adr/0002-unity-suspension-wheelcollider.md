# ADR 0002 — P1 suspension model

Status: Accepted for P1 vertical slice  
Task: UVEH-002

## Decision

Use Unity `WheelCollider` as the P1 suspension baseline, wrapped behind the vehicle module so a custom suspension implementation can replace it later without changing race/UI contracts.

## Why

- per-wheel contact, spring and damper telemetry are available immediately;
- the current P1 target is one player car plus three rivals, so predictable tuning and iteration speed matter more than a bespoke solver;
- the wrapper boundary keeps gameplay code independent from the concrete suspension implementation;
- the decision is compatible with the existing Rigidbody vehicle architecture.

## P1 acceptance measurements

The baseline is accepted only when the exact Android candidate demonstrates: stable four-wheel contact on the Cairo vertical slice, no persistent oscillation after landing, no wheel penetration visible at normal camera distance, and repeatable steering/braking behavior at 30/45/60 FPS device tiers.

## Revisit trigger

Re-evaluate a custom raycast suspension only if profiler/device evidence shows WheelCollider CPU cost, determinism, or tuning limits block the P1 performance/feel gates.
