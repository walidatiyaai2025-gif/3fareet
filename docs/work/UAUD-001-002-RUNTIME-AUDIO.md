# UAUD-001 / UAUD-002 Runtime Audio Slice

## State
- UAUD-001: IN REVIEW
- UAUD-002: IN REVIEW
- Tracking issue: #77
- Parent/base: PR #76 (`agent/final-todo-plus-ready-5`)
- Branch: `agent/UAUD-001-002-runtime-audio`

## Implemented
- Two-layer procedural engine presentation with speed/throttle driven pitch and gain.
- Duplicate-safe runtime bootstrap attaches audio rigs to player and rival cars without modifying gameplay code.
- Player/rival spatial mix policy with bounded rolloff.
- Drift squeal loop driven by `IsDrifting`.
- Nitro loop plus transition burst driven by `NitroActive`/energy.
- Collision one-shot driven by Unity `OnCollisionEnter`, minimum impact threshold and cooldown.
- Static reusable generated clips; no clip allocation occurs in per-frame update paths.
- Unity `.meta` files committed for both new C# assets.

## Scope guard
No changes to `ArcadeCarController`, `CairoMusicLifecycle`, Race/AI, World, ProjectSettings, Packages, Android/release or art assets.

## Validation truth
Static implementation review only in this connector session. Exact-head Unity 6000.5.8f1 import/compile, automated tests and physical-device listening were not executed here, therefore neither task is VERIFIED.

## Remaining QA
1. Unity import/compile exact PR head with zero C# errors.
2. Listen to idle/acceleration/high-speed transitions and tune engine layer balance.
3. Verify 3 rival spatial attenuation in a race.
4. Verify drift start/stop has no clicks or stuck loop.
5. Verify nitro transition and collision cooldown under repeated impacts.
6. Replace procedural fallback clips with approved authored assets later without changing the telemetry/event contract.
