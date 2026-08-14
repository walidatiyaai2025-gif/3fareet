# ADR 0003 — Keep Built-in/custom rendering for U-P1

- Status: Accepted for U-P1
- Task: UART-008
- Issue: #85
- Decision date: 2026-08-14

## Context

The task register calls UART-008 “Mobile URP materials and lighting setup”, but the production Unity client is not a URP project:

- `Packages/manifest.json` has no Universal Render Pipeline dependency.
- `Afareet/RuntimeLit` is a Built-in Forward shader using UnityCG/Lighting includes.
- The Cairo street-kit shader delivered by UART-005 targets the same current pipeline.
- UART-003 Hero production materials bind `Afareet/RuntimeLit`.
- Runtime fog, moon light and camera setup are Built-in APIs.

A late P1 URP migration would therefore be an architecture migration, not a quality-setting task. It would require package/pipeline assets, shader/material conversion, lighting regression, build-size review and Android performance evidence across multiple active workstreams.

## Decision

**Keep Built-in/custom rendering for the U-P1 vertical slice.**

UART-008 is interpreted for P1 as: implement production mobile material/lighting quality tiers for the pipeline the product actually uses.

URP migration is deferred to a separate post-P1 task and must have its own architecture, shader migration and regression plan.

## P1 quality contract

Data-driven Low/Mid/High profiles control:

- target FPS;
- dynamic render-buffer scale;
- pixel-light budget;
- shadow distance, hard/soft mode, cascades and resolution;
- MSAA;
- anisotropic filtering;
- LOD bias / maximum LOD level;
- shader maximum LOD;
- soft particles;
- realtime reflection probes.

The support values from PR #66 are retained as the baseline for FPS/render scale/light count/shadow distance: 30/0.80/2/35m, 45/0.90/4/55m, 60/1.00/6/75m.

## Runtime selection

Auto selection uses known system memory, graphics memory and shader-level thresholds. Unknown or mixed capability falls back conservatively to Mid rather than guessing High. QA can force `low`, `mid` or `high` through the dedicated PlayerPrefs override without changing production config.

## Consequences

### Positive
- No package or ProjectSettings migration during P1.
- Existing shaders, street assets and Hero materials remain compatible.
- Quality tiers can be validated independently on Android devices.
- The rendering module is isolated and self-booting; gameplay/bootstrap ownership is unchanged.

### Tradeoff
- P1 does not gain URP-specific renderer features.
- Any future URP move still requires explicit shader/material migration and visual/performance regression.

## Verification gate

This ADR resolves the architecture blocker only. UART-008 remains IN REVIEW until exact-head Unity compile/tests and Android Low/Mid/High visual/performance evidence exist. It is not VERIFIED by this document alone.
