# READY-5 — systems/UI implementation batch

Branch: `agent/ready-5-art-audio-ui`
Base: `agent/ready-5-visual-ui`

## Five READY tasks implemented

1. `UVEH-010` — camera collision/obstruction: `CameraObstructionPass` runs after the chase camera and sphere-casts from the vehicle focus to the desired camera position, ignoring the hero hierarchy.
2. `UUI-005` — RTL/localization framework: `RuntimeLocalization` provides English/Arabic runtime tables, system-language detection, explicit locale switching and RTL state.
3. `UUI-002` — production race HUD: `ProductionRaceHud` adds a SafeArea-aware uGUI overlay for position, race time, speed and Spirit energy. Unity UI `2.0.0` is declared in Packages.
4. `UPER-002` — profiler baseline capture: `PerformanceBaselineCapture` samples 300 debug frames and reports average FPS, CPU/GPU frame timings, peak reserved memory and device/GPU identity.
5. `U3D-007` — new Input System foundation: package `com.unity.inputsystem` `1.17.0` plus `ProductionInputMap` with rebindable Keyboard/Gamepad/Touch actions for steer, throttle, brake, drift, nitro and start.

## Validation truth

Implementation is complete enough to move these tasks from `READY` to `IN REVIEW`, not `VERIFIED`.

Still required before promotion/merge:
- exact-head Unity package resolution/import/compile;
- execute relevant automated tests;
- collect an actual UPER-002 baseline on the target workload;
- device visual review for UUI-002 and Arabic shaping/font verification for UUI-005;
- camera obstruction verification against representative Cairo geometry;
- commit Unity-generated `.meta` files for the five new C# assets. Connector attempts to create those metadata files were blocked, so this is recorded as an explicit merge blocker rather than hidden.

## Task-state delta

Before this batch: `IN REVIEW 20 | READY 25 | TODO 14 | BLOCKED 6`.
After implementation: `IN REVIEW 25 | READY 20 | TODO 14 | BLOCKED 6`.
Total remains `65`.
