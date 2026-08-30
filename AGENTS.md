# 3FAREET — Repository Constitution for Codex / AI Workers

**Status:** MANDATORY  
**Applies to:** every AI worker, Codex session, developer, reviewer, task branch, PR, build and release  
**Owner intent:** multiple workers must converge on one original premium 3D racing game instead of independently inventing different directions.

## 1. Mission

Build **3FAREET** as an original third-person 3D mobile street-racing game with a distinctive Cairo-at-night identity.

The visual target is the **feeling** of a modern premium mobile racer: convincing perspective depth, vehicle weight, dense city context, active traffic, strong lighting hierarchy and speed feedback. Reference screenshots are inspiration only; do not copy maps, branded cars, logos, UI, textures, models or proprietary assets.

## 2. Canonical runtime path

For current 3D gameplay/rendering work, **Unity is the canonical game-runtime path**.

Known live Unity runtime markers include systems such as:
- `AfareetBootstrap`
- `CairoTrackBuilder`
- `CairoCareerTrackBuilder`
- `CairoAuthoredStreetKit`
- Garage session runtime
- authored Cairo route / road / curb systems
- Android JSONL diagnostics beginning with `AFAREET_`

Do **not** add new core racing, rendering, camera, traffic, world, vehicle or VFX implementation to the legacy Flutter/Flame prototype unless a task explicitly targets legacy compatibility, launcher/shell behavior or migration support.

Backend remains a separate later concern: **HTTPS API → Laravel → MySQL**. No game client may connect directly to MySQL.

## 3. AI-first / no-manual-production rule

The owner should not be required to model, texture, arrange scenes, drag references, place lights, wire GameObjects or repeatedly operate Blender/Unity by hand.

Workers must prefer automation:
- C# runtime code
- Unity Editor tooling
- Unity batchmode / command-line builds
- deterministic procedural generation
- ScriptableObjects/data-driven configuration
- Blender Python
- Blender headless CLI when available
- generated meshes/materials/textures where practical
- import/prefab/LOD/collider automation
- validation scripts
- automated screenshot/build/diagnostic capture where possible

If a normal workflow says “open Unity and drag X onto Y”, first try to automate it.

## 4. Source-of-truth order

Before coding, read in this order:

1. `AGENTS.md`
2. `docs/PROJECT_STATUS.md`
3. `docs/MASTER_DEVELOPMENT_PLAN.md`
4. `docs/AI_AUTONOMOUS_3D_EXECUTION_PLAN.md`
5. `docs/ART_DIRECTION.md`
6. `docs/TASK_REGISTER.md`
7. the relevant `docs/tasks/*.md`
8. existing implementation/tests/diagnostics

If documents conflict, do **not** silently choose one. Reconcile the conflict in the same PR or raise a focused governance task.

## 5. Highest-priority product problem

The project may technically contain 3D meshes and still **feel flat / 2D**. The highest visual priority is therefore **perceptual 3D**, not merely increasing asset count.

Prioritize in this order unless live evidence shows a different blocker:

1. chase camera / perspective
2. vehicle grounding and mass
3. road / curb / sidewalk height separation
4. lighting and contact shadows
5. foreground / midground / background depth
6. city density and visual continuation
7. traffic / parked vehicles / scale cues
8. materials and decals
9. restrained post-processing
10. UI polish

Do not hide a weak 3D scene behind a polished HUD.

## 6. Visual invariants

The following are mandatory acceptance principles:

- Third-person **Perspective** chase camera.
- Dynamic speed FOV and damped camera motion.
- Vehicle wheels visibly steer/rotate and suspension/body response is readable.
- The vehicle must look attached to the road, not like a sprite sliding over a plane.
- Real curb/sidewalk elevation and road-surface variation.
- Foreground, midground and background must be visible in normal driving views.
- Empty voids behind the playable road are not acceptable near the player.
- Cairo-inspired urban density: facades, balconies, shopfronts, roofs, AC units, signage, poles, wires, barriers and roadside props.
- Night lighting must have bright / medium / dark regions, not flat global illumination.
- Traffic and parked vehicles must establish scale.
- Bloom/neon must be restrained; no washed-out cyberpunk look.
- The final presentation must be recognizably original **3FAREET**.

## 7. Target hardware / performance

Primary real-device target currently includes:

- HONOR ALI-NX1
- Android 15 / API 35
- Adreno 710
- approximately 11 GB RAM
- 2652×1200 display

Target **60 FPS where realistically possible** with stable frame pacing. Build scalable quality tiers and optimize realtime lights, shadows, particles, traffic, overdraw, texture memory and draw calls.

## 8. Multi-worker coordination

Every meaningful implementation task must have a Task ID.

Branch convention:
- `feature/<TASK-ID>-short-name`
- `fix/<TASK-ID>-short-name`
- `infra/<TASK-ID>-short-name`
- `docs/<TASK-ID>-short-name`

Rules:
- one clear owner per task;
- one focused responsibility per worker;
- fetch/reconcile live state before work;
- do not restart already verified work;
- do not duplicate another active worker’s module;
- avoid parallel edits to the same core files/interfaces;
- use Module Lock for shared architecture;
- shared interface changes should be isolated before dependent parallel work;
- keep PR scope bounded and evidence-driven;
- update `docs/PROJECT_STATUS.md` in the same PR when project truth changes.

Task state:
`TODO → READY → IN PROGRESS → BLOCKED/IN REVIEW → DONE → VERIFIED`

`DONE` means implementation exists.  
`VERIFIED` requires the appropriate build/test/device/visual evidence.

## 9. Recommended parallel workstreams

Workers should parallelize by ownership boundary, not by arbitrary file count:

- CAMERA / presentation depth
- VEHICLE / physics / grounding
- WORLD / road / Cairo modular environment
- LIGHTING / materials / post-processing
- TRAFFIC / rivals
- VFX / drift / nitro
- UI / touch controls / garage presentation
- AUDIO
- AUTOMATION / Blender / Unity Editor pipelines
- QA / Android / diagnostics / performance

Dependencies and task IDs are defined in `docs/AI_AUTONOMOUS_3D_EXECUTION_PLAN.md` and the task register.

## 10. Asset and IP rule

Prefer, in order:

1. original procedural assets;
2. original Blender-scripted assets;
3. programmatically constructed Unity assets;
4. verified permissive / CC0 resources when genuinely useful.

Never extract or copy proprietary assets from another game. Do not reproduce real vehicle branding or protected logos. Preserve provenance for external assets.

## 11. Diagnostics and evidence

Maintain searchable JSONL diagnostics using `AFAREET_` markers.

Important runtime evidence includes:
- build identity / exact SHA
- device / GPU / memory
- quality tier
- scene
- player spawn
- selected vehicle
- camera status
- route/track status
- traffic status
- race lifecycle
- FPS/frame-time samples
- recoveries/exceptions

A successful compile is not visual acceptance.

## 12. Definition of 3D vertical-slice acceptance

Do not claim the 3D presentation slice `VERIFIED` until representative Android screenshots and runtime evidence show all of the following:

- immediately reads as a third-person 3D racing game;
- convincing chase-camera perspective;
- player vehicle has visible mass and ground contact;
- foreground / midground / background depth;
- dense Cairo-inspired surroundings;
- road is not a flat primitive strip;
- night lighting has depth and contact shadows;
- traffic or parked cars establish scale;
- no obvious near-camera primitive prototype look;
- original 3FAREET identity;
- reasonable performance on target-class Android hardware.

## 13. Autonomous completion behavior

Do not stop merely because one subtask compiled or one script was added.

Within the assigned task boundary:
1. inspect live state;
2. implement;
3. validate;
4. fix failures;
5. capture evidence;
6. update project truth;
7. prepare a focused PR.

Stop only when the assigned scope is terminal or a genuine external blocker cannot be resolved by code, automation or available tooling.
