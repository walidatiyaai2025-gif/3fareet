# Unity Android Device Tiers & Performance Budgets

**Task:** `UPER-001`  
**Owner:** GPT-5.6 Sol (Performance/QA Agent)  
**Engine:** Unity `6000.5.8f1`  
**Scope:** `unity_game/` Android vertical slice  
**Status:** IN PROGRESS  

## Purpose

This document is the performance contract for the first Unity Android vertical slice. It defines capability-based device tiers and measurable pass/fail budgets before art, VFX, AI, UI, and race systems are tuned independently.

These numbers are **budgets, not gameplay constants**. Runtime quality selection must be implemented later through Unity quality/config assets and ScriptableObjects where gameplay-facing tuning is involved; do not scatter these values through MonoBehaviours.

## Tier classification

A device is classified by the lowest capability it satisfies. RAM alone never promotes a device to a higher tier; sustained CPU/GPU behavior during the test loop is authoritative.

| Tier | Minimum capability band | Target use |
|---|---|---|
| **Low** | Android API 26+, ARM64, 4 GB physical RAM, Vulkan/OpenGLES3-class mobile GPU capable of sustaining the Low frame budget | Minimum supported production experience |
| **Mid** | ARM64, 6 GB+ RAM, modern mid-range mobile GPU/CPU that can sustain the Mid frame budget | Default quality target |
| **High** | ARM64, 8 GB+ RAM, upper-mid/high mobile GPU/CPU that can sustain the High frame budget | Highest P1 quality preset |

Do not maintain model-name allowlists as the source of truth. Named devices may be added to the QA matrix as evidence, but tier membership is decided by measured capability and sustained behavior.

## Frame-rate and frame-time budgets

The P1 vertical slice targets deterministic smoothness rather than the highest headline FPS.

| Metric | Low | Mid | High |
|---|---:|---:|---:|
| Target FPS | 30 | 60 | 60 |
| Frame-time target | 33.3 ms | 16.7 ms | 16.7 ms |
| P95 total frame time | <= 33.3 ms | <= 16.7 ms | <= 16.7 ms |
| P99 total frame time | <= 40 ms | <= 22 ms | <= 20 ms |
| Main-thread P95 | <= 24 ms | <= 11 ms | <= 10 ms |
| Render-thread P95 | <= 24 ms | <= 11 ms | <= 10 ms |
| GPU P95 | <= 28 ms | <= 13 ms | <= 12 ms |
| Sustained 1% low | >= 25 FPS | >= 50 FPS | >= 52 FPS |
| Single hitch gate | no frame > 150 ms during normal racing after warm-up | same | same |

A build fails the tier when either CPU or GPU repeatedly exceeds its budget even if the average FPS appears acceptable.

## Memory budgets

Measure Android process PSS/RSS externally and Unity memory internally. The larger of repeated samples is used for the report.

| Metric | Low | Mid | High |
|---|---:|---:|---:|
| Steady-state process PSS after warm-up | <= 650 MiB | <= 900 MiB | <= 1100 MiB |
| Peak process PSS during a 20-minute race loop | <= 850 MiB | <= 1150 MiB | <= 1400 MiB |
| Unity reserved memory steady-state | <= 500 MiB | <= 700 MiB | <= 850 MiB |
| Managed heap after forced GC in a stable scene | <= 120 MiB | <= 160 MiB | <= 200 MiB |
| Memory growth after 5 restart cycles | <= 5% from stabilized baseline | <= 5% | <= 5% |

Any monotonic growth across race restarts is treated as a leak candidate even when the absolute cap is not yet exceeded.

## Thermal and sustained-performance gate

Absolute surface temperature is not a portable pass/fail metric because sensors and chassis differ. Use Android thermal status when available plus sustained frame degradation.

Test sequence for every physical QA device:

1. Cold-launch the exact APK under test.
2. Run the game for 5 minutes to stabilize compilation, caches, and loading.
3. Run the representative race loop continuously for at least 20 additional minutes.
4. Capture frame timing, CPU/GPU timing, memory, battery state, and Android thermal status at regular intervals.
5. Compare the stabilized 5-minute window with the final 5-minute window.

Pass gates:

| Gate | Low | Mid | High |
|---|---:|---:|---:|
| Median FPS degradation, stabilized vs final window | <= 15% | <= 10% | <= 10% |
| Android thermal status | must not remain in SEVERE/CRITICAL/EMERGENCY/SHUTDOWN | same | same |
| Thermal-triggered crash/restart | 0 | 0 | 0 |
| Persistent input latency increase caused by throttling | none observable | none observable | none observable |

A transient MODERATE thermal state is reportable but not automatically a failure if sustained frame and input budgets remain inside limits.

## Quality-preset envelope

These are rendering envelopes for later `UART-008`/performance implementation, not direct ProjectSettings changes from this task.

| Feature | Low | Mid | High |
|---|---|---|---|
| Render scale starting envelope | 0.75-0.80 | 0.85-0.95 | 1.0 |
| FPS cap | 30 | 60 | 60 |
| Real-time shadow distance | short | medium | full P1 slice distance |
| Shadow cascades | 1 | up to 2 | up to 2 |
| Texture residency | reduced where visually safe | full P1 target | full P1 target |
| VFX density | reduced; preserve gameplay readability | standard | standard/high within GPU budget |
| Post-processing | minimal essentials | standard | full approved P1 stack within budget |

Visual features are removed or reduced in this order when a tier misses budget: expensive post effects -> non-gameplay particles -> shadow distance/resolution -> render scale. Gameplay readability, touch controls, START flow, race state, and collision fidelity must not be degraded to hide a performance problem.

## Representative P1 workload

Every performance capture must exercise the same minimum workload:

- player car plus 3 AI rivals;
- active Cairo vertical-slice track;
- race HUD visible;
- touch/tilt input path enabled;
- drift and nitro effects exercised repeatedly;
- representative collisions and recovery;
- race restart at least 5 times for leak detection;
- no Development Console or profiler overlay visible in the release-facing APK.

Until production art/audio is integrated, reports must label captures as **blockout baseline** so placeholder results are not mistaken for the Visual Gate performance result.

## Measurement protocol

### Unity-side capture

Use Unity Profiler/ProfilerRecorder or an equivalent development-only capture path to record at minimum:

- main thread frame time;
- render thread frame time;
- GPU frame time when supported;
- GC allocated per frame;
- total/used/reserved memory;
- batches/set-pass/draw calls when rendering work is under investigation.

Profiler instrumentation must not ship enabled in the final release APK.

### Android-side capture

Record the exact APK SHA-256 and device identity, then capture:

- process PSS from Android memory diagnostics;
- thermal status where supported;
- battery level before/after the sustained loop;
- device Android version and API level;
- screen resolution and refresh rate;
- whether the device was charging;
- ambient-test notes if conditions are abnormal.

Do not compare one charging device with one battery-powered device as if the thermal conditions were equivalent.

## Pass/fail rules

A tier result is **PASS** only when all applicable gates pass on the same APK and commit:

- no Unity/Android exceptions during the run;
- frame-time targets pass after warm-up;
- memory caps pass and no restart-growth pattern exceeds 5%;
- thermal sustained gate passes;
- controls and HUD remain responsive;
- visual correctness is not sacrificed by missing materials/shaders/assets.

`Build succeeded` is never a performance PASS. Emulator measurements are useful for automation but do not satisfy a physical-device performance gate.

## Evidence record template

Each device result should record:

| Field | Required value |
|---|---|
| Commit | full Git SHA |
| APK | artifact/release link |
| APK SHA-256 | full digest |
| Unity version | `6000.5.8f1` |
| Device | manufacturer + exact model |
| Android | version + API level |
| Tier under test | Low / Mid / High |
| Resolution / refresh | measured values |
| Charging | Yes / No |
| Test duration | warm-up + sustained duration |
| FPS/frame-time summary | median/P95/P99 + 1% low |
| CPU/GPU timing | P95 values |
| Memory | steady + peak PSS, Unity reserved |
| Thermal | status progression + FPS degradation |
| Exceptions | count / summary |
| Result | PASS / FAIL / BLOCKED |
| Evidence | profiler capture + screenshots/video/logs |

## Exit criteria for UPER-001

`UPER-001` is complete for review when:

- Low/Mid/High capability bands are defined;
- FPS and frame-time budgets are numeric and measurable;
- process/Unity memory budgets are numeric and measurable;
- thermal sustained-performance gates are explicit;
- the measurement protocol prevents average-FPS-only acceptance;
- later quality implementation is clearly separated from this documentation task;
- no existing Unity gameplay/build/Android files are changed.

Physical device captures belong to `UPER-002`, `UVEH-012`, `URAC-012`, and the release/device gates as applicable; they do not turn this planning document itself into a `Verified APK` claim.
