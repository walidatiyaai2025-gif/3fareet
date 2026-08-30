# U3D — Unity AI-First 3D Closure Tasks

> This task file is the execution register for the autonomous 3D presentation program. It is intentionally split by ownership boundary so multiple Codex workers can run in parallel without inventing competing systems.

## Coordination rules

- One owner per Task ID.
- One active branch per Task ID.
- Read `AGENTS.md` and `docs/AI_AUTONOMOUS_3D_EXECUTION_PLAN.md` before starting.
- Fetch LIVE state and current active PRs first.
- Do not restart already verified work.
- Use Module Lock for bootstrap, player-state, race-session, route-data, input, save-schema, render-pipeline and Android-build shared files.
- `DONE` is implementation complete; `VERIFIED` requires evidence appropriate to the task.

| ID | Priority | Task | Primary owner | Status | Depends on |
|---|---|---|---|---|---|
| U3D-GOV-001 | P0 | Reconcile canonical Unity runtime branch/head with main and document it | Team Lead / Integrator | READY | — |
| U3D-GOV-002 | P0 | Reconcile stale Flutter-era gameplay architecture statements with Unity runtime truth | Team Lead / Architect | READY | U3D-GOV-001 |
| U3D-AUT-001 | P0 | Create/verify Unity project validation automation | Build/Tools | READY | U3D-GOV-001 |
| U3D-AUT-002 | P0 | Create/verify batchmode Android build entry point | Build/Tools | READY | U3D-GOV-001 |
| U3D-AUT-003 | P1 | Automate representative screenshot capture | Build/Tools + QA | TODO | U3D-CAM-003 |
| U3D-AUT-004 | P1 | Create Blender availability probe + headless runner/fallback contract | Tools / Technical Art | TODO | — |
| U3D-ASSET-001 | P0 | Generate original hero vehicle asset pipeline | Technical Art | READY | U3D-AUT-004 |
| U3D-ASSET-002 | P1 | Generate original traffic vehicle family | Technical Art | TODO | U3D-AUT-004 |
| U3D-ASSET-003 | P0 | Generate Cairo facade modular kit | Technical Art | READY | U3D-AUT-004 |
| U3D-ASSET-004 | P1 | Generate street prop / roadside kit | Technical Art | TODO | U3D-AUT-004 |
| U3D-ASSET-005 | P1 | Automate import settings / materials / LOD / colliders / prefabs | Technical Art + Tools | TODO | U3D-ASSET-001 |
| U3D-CAM-001 | P0 | Audit current chase camera and remove flat/rigid presentation causes | Camera / Gameplay | READY | U3D-GOV-001 |
| U3D-CAM-002 | P0 | Implement/tune perspective chase rig, damping, offsets and camera collision | Camera / Gameplay | READY | U3D-CAM-001 |
| U3D-CAM-003 | P0 | Implement/tune speed FOV, accel/brake/drift/collision feedback | Camera / Gameplay | TODO | U3D-CAM-002 |
| U3D-CAM-004 | P1 | Add accessibility/reduced-shake and hard bounds | Camera / Gameplay | TODO | U3D-CAM-003 |
| U3D-VEH-001 | P0 | Audit current controller for sliding/sprite-like vehicle presentation | Vehicle / Physics | READY | U3D-GOV-001 |
| U3D-VEH-002 | P0 | Integrate visible wheel steering/rotation and suspension response | Vehicle / Physics | READY | U3D-VEH-001 |
| U3D-VEH-003 | P0 | Tune body roll/pitch, traction, handbrake and drift mass cues | Vehicle / Physics | TODO | U3D-VEH-002 |
| U3D-VEH-004 | P1 | Add anti-flip, recovery and collision stabilization | Vehicle / Physics | TODO | U3D-VEH-003 |
| U3D-VEH-005 | P1 | Integrate vehicle light/VFX hooks | Vehicle / Physics + VFX | TODO | U3D-VEH-002 |
| U3D-ROAD-001 | P0 | Audit authored road geometry for flat strip presentation | World / Road | READY | U3D-GOV-001 |
| U3D-ROAD-002 | P0 | Enforce curb/sidewalk/gutter vertical separation | World / Road | READY | U3D-ROAD-001 |
| U3D-ROAD-003 | P0 | Add asphalt roughness/normal/decal/patch/manhole/drain variation | World / Road + Lighting | TODO | U3D-ROAD-002 |
| U3D-ROAD-004 | P1 | Improve intersections, markings, crosswalks and road edge variety | World / Road | TODO | U3D-ROAD-002 |
| U3D-WLD-001 | P0 | Audit Cairo route for empty voids and weak parallax | World | READY | U3D-GOV-001 |
| U3D-WLD-002 | P0 | Build foreground roadside density pass | World | READY | U3D-WLD-001 |
| U3D-WLD-003 | P0 | Build midground building/shop/intersection density pass | World | TODO | U3D-ASSET-003 |
| U3D-WLD-004 | P0 | Build background skyline/side-street continuation/haze geometry | World | TODO | U3D-WLD-001 |
| U3D-WLD-005 | P1 | Add deterministic facade/shop/balcony/roof/sign variation | World | TODO | U3D-ASSET-003 |
| U3D-LGT-001 | P0 | Audit current Moon Light / ambient stack for flat illumination | Lighting / Technical Art | READY | U3D-GOV-001 |
| U3D-LGT-002 | P0 | Build street/shop/window/headlight practical-light hierarchy | Lighting / Technical Art | READY | U3D-LGT-001 |
| U3D-LGT-003 | P0 | Improve contact/vehicle/environment shadow grounding within mobile budget | Lighting / Technical Art | TODO | U3D-LGT-001 |
| U3D-LGT-004 | P1 | Add restrained tonemapping/color/bloom/fog/AA quality tiers | Lighting / Technical Art | TODO | U3D-LGT-002 |
| U3D-TRF-001 | P0 | Add parked-vehicle scale cues around vertical slice | Traffic / World | READY | U3D-ASSET-002 |
| U3D-TRF-002 | P0 | Add pooled ambient traffic lane following | Traffic / AI | TODO | U3D-ASSET-002 |
| U3D-TRF-003 | P1 | Add distance-based traffic simulation reduction | Traffic / AI | TODO | U3D-TRF-002 |
| U3D-TRF-004 | P1 | Integrate rival behavior without coupling to ambient traffic | Race AI | TODO | U3D-TRF-002 |
| U3D-VFX-001 | P0 | Integrate tire smoke + bounded skid marks | VFX | READY | U3D-VEH-003 |
| U3D-VFX-002 | P1 | Integrate collision sparks/dust/road feedback | VFX | TODO | U3D-VEH-005 |
| U3D-VFX-003 | P1 | Reconcile Drift/Nitro signature VFX with art direction and performance tiers | VFX | TODO | U3D-LGT-004 |
| U3D-UI-001 | P1 | Audit touch HUD for visual obstruction and safe areas | UI / Input | TODO | U3D-CAM-002 |
| U3D-UI-002 | P1 | Polish original race HUD without copying reference UI | UI / UX | TODO | U3D-UI-001 |
| U3D-UI-003 | P1 | Polish 3D garage presentation around hero vehicle | UI / Garage | TODO | U3D-ASSET-001 |
| U3D-AUD-001 | P1 | Complete engine/skid/impact/wind/traffic/city audio hooks | Audio | TODO | U3D-VEH-003 |
| U3D-QA-001 | P0 | Persist real-device diagnostic baseline for HONOR ALI-NX1 | QA | READY | — |
| U3D-QA-002 | P0 | Add camera/track/traffic/player/FPS runtime diagnostic markers | QA + Runtime | TODO | U3D-QA-001 |
| U3D-QA-003 | P0 | Create G1 camera + grounding visual acceptance evidence | QA / Visual | TODO | U3D-CAM-003,U3D-VEH-003 |
| U3D-QA-004 | P0 | Create G2 dense Cairo vertical-slice visual acceptance evidence | QA / Visual | TODO | U3D-WLD-004,U3D-ROAD-003,U3D-LGT-003,U3D-TRF-001 |
| U3D-QA-005 | P1 | Verify complete event loop on Android | QA / Gameplay | TODO | U3D-QA-004 |
| U3D-QA-006 | P0 | Verify frame pacing/quality tiers on Adreno-710-class target | QA / Performance | TODO | U3D-LGT-004,U3D-TRF-003 |
| U3D-REL-001 | P0 | Build exact-SHA Android candidate and archive build metadata | Release | TODO | U3D-QA-005,U3D-QA-006 |
| U3D-REL-002 | P0 | Real-device smoke + final visual review + verified APK promotion | Release / Owner / QA | TODO | U3D-REL-001 |

## Closure order

The program should converge through these gates:

1. **G0 Live reconciliation** — U3D-GOV-001/002
2. **G1 Camera + grounding** — U3D-CAM + U3D-VEH + U3D-QA-003
3. **G2 Dense Cairo slice** — U3D-ROAD + U3D-WLD + U3D-LGT + parked/traffic cues + U3D-QA-004
4. **G3 Premium moving frame** — materials/VFX/UI refinement and screenshot review
5. **G4 Playable event loop** — integrated race lifecycle
6. **G5 Android release evidence** — performance, diagnostics, exact-SHA APK, real-device verification

Do not let lower-priority feature expansion bypass an open P0 visual/runtime gate.
