# 3FAREET — AI Autonomous 3D Execution Plan

**Document:** AFA-U3D-PLAN-001  
**Version:** 1.0  
**Date:** 2026-08-30  
**Status:** Controlled multi-worker execution plan  
**Scope:** Unity 3D runtime, automated asset pipeline, Android visual/runtime closure

## Purpose

This document converts the owner’s visual goal into a shared execution path that multiple Codex/AI workers can follow without drifting into different implementations.

The target is **not** to copy another racing game. The target is an original 3FAREET experience with comparable perceptual qualities: strong third-person depth, convincing vehicle mass, dense city context, premium night lighting, traffic, speed feedback and mobile polish.

## Canonical working model

The current 3D gameplay/runtime path is Unity. Legacy Flutter/Flame work may remain in the repository for historical/launcher/migration reasons, but **new 3D gameplay, world, camera, vehicle, traffic, VFX and rendering work belongs to Unity unless an explicit task says otherwise**.

Backend remains independent and later-gated:

`Game Client → HTTPS API → Laravel → MySQL`

No client-to-MySQL direct access.

## Owner interaction contract

The owner should not need to manually:
- model or texture assets;
- operate Blender for routine production;
- arrange Unity scenes;
- place lights/props by hand;
- drag component references;
- create prefabs manually;
- configure repetitive import settings;
- run repetitive build/QA operations.

Workers should automate these through Unity Editor scripts, C#, Blender Python, CLI/batchmode, generated data/assets, validators and CI/local build tooling.

## Primary acceptance problem

The project already demonstrates 3D runtime components, but the player-facing image can still read as flat. Therefore the first closure program is a **3D Presentation / Depth Vertical Slice**, not broad feature expansion.

### Global priority order

1. Camera perspective and motion
2. Vehicle grounding / mass / suspension feedback
3. Road / curb / sidewalk vertical separation
4. Lighting hierarchy / contact shadows
5. Foreground-midground-background composition
6. Dense Cairo modular world and side-street continuation
7. Traffic / parked vehicles / scale cues
8. Materials / decals / asphalt variation
9. Atmosphere / restrained post-processing
10. HUD/garage polish
11. Expansion into more events/content

---

# WORKSTREAMS

## U3D-CAM — Camera / Perceptual Depth

**Owner boundary:** chase camera and presentation feedback only. Avoid changing vehicle physics constants except through agreed interfaces.

Starting targets:
- Perspective projection
- normal FOV: 58–64°
- high-speed FOV: 68–74°
- distance: 5.5–7.0 m
- height: 2.6–3.2 m
- look target: ~1 m above vehicle center

Required behavior:
- position damping;
- rotation damping;
- speed-responsive FOV;
- acceleration lag;
- braking response;
- subtle steering offset/roll;
- drift-aware heading;
- camera collision and wall avoidance;
- collision/landing impulse;
- subtle high-speed shake;
- accessibility option to reduce/disable shake.

Acceptance: raw gameplay screenshot without HUD must immediately read as third-person racing.

## U3D-VEH — Vehicle / Physics / Grounding

**Owner boundary:** vehicle runtime, wheel/suspension/body presentation and arcade handling. Do not own environment lighting or world generation.

Required:
- original hero vehicle;
- wheel rotation;
- front-wheel steering visuals;
- suspension travel;
- readable body roll/pitch;
- acceleration squat / braking dive where appropriate;
- grip / traction / handbrake / drift;
- anti-flip/recovery;
- collision response;
- headlights / brake / reverse light hooks;
- tire smoke and skid-mark hooks;
- strong wheel-ground visual relationship.

Physics target: arcade-realistic, easy to learn, physically readable, not hardcore simulation.

## U3D-ASSET — AI / Blender Asset Factory

**Owner boundary:** original asset generation and import automation.

Preferred scripts:
- `create_hero_car.py`
- `create_traffic_cars.py`
- `create_cairo_buildings.py`
- `create_street_props.py`
- `create_roadside_kits.py`

Pipeline:

`Codex → Blender Python / procedural mesh → export → Unity import automation → materials/LODs/colliders/prefabs → validation`

Requirements:
- original designs;
- mobile-appropriate topology;
- reproducible seeds/configuration;
- LOD generation where useful;
- material naming conventions;
- automated import settings;
- provenance file for external permissive assets.

If Blender is unavailable, workers must provide a procedural/modular Unity fallback rather than blocking all progress.

## U3D-WLD — Cairo World / Road / Density

**Owner boundary:** track-side world, road construction, modular architecture and set dressing.

World must feel like a city around the player, not a track in empty space.

Required visual layers:

### Foreground
- curbs;
- sidewalks;
- poles;
- barriers;
- parked cars;
- bins;
- kiosks/props;
- nearby shopfront detail.

### Midground
- shops;
- apartment facades;
- traffic;
- intersections;
- side streets;
- walls/gates;
- underpass/flyover elements where appropriate.

### Background
- skyline;
- large structures;
- distant building silhouettes;
- bridge/flyover silhouettes;
- atmospheric haze;
- visually continued roads.

Minimum modular direction:
- 8+ facade families;
- several height/width variants;
- 5+ shopfront families;
- balcony variants;
- rooftop structures;
- AC units;
- satellite dishes;
- water tanks;
- awnings;
- Arabic-inspired fictional signs;
- utility poles/wires;
- walls/gates/stairs.

Avoid perfect repetitive building rows.

## U3D-ROAD — Road Surface / Vertical Separation

**Owner boundary:** drive surface and immediate road geometry/material system.

Required:
- asphalt with roughness/normal variation where practical;
- real curb height;
- sidewalk elevation;
- gutters/drains;
- lane markings;
- crosswalks;
- manholes;
- patches/cracks/stains;
- skid marks;
- road decals;
- intersection treatment.

Acceptance: road must not read as a single grey flat strip.

## U3D-LGT — Lighting / Materials / Atmosphere

**Owner boundary:** lighting stack, probes, material look-dev, post-process configuration.

Night stack:
- moon/directional base;
- street-light pools;
- warm shop practicals;
- cool ambient moonlight;
- emissive windows/signage;
- vehicle head/brake lights;
- reflection/light probes where beneficial;
- baked/mixed/faked lighting when cheaper on mobile.

Image hierarchy must contain bright, medium and dark regions.

Post-processing should be restrained:
- tonemapping;
- color grading;
- ambient occlusion if performant;
- subtle bloom;
- subtle vignette;
- fog/haze;
- anti-aliasing.

Avoid excessive neon/bloom.

## U3D-TRF — Traffic / Parked Cars / Rivals

**Owner boundary:** NPC vehicle simulation and pooling.

Original traffic archetypes:
- compact sedan;
- fictional taxi-inspired car;
- hatchback;
- van;
- fictional minibus-inspired vehicle;
- performance rival.

Required:
- lane following;
- speed variation;
- obstacle response;
- spawn/despawn pooling;
- distance-based simulation reduction;
- parked vehicles;
- rival race behavior separated from ambient traffic.

Traffic density must improve scale without destabilizing frame time.

## U3D-VFX — Drift / Nitro / Collision Feedback

**Owner boundary:** VFX and feedback hooks; do not rewrite core physics.

Required:
- tire smoke;
- skid marks;
- collision sparks where appropriate;
- dust/road particles;
- nitro/spirit feedback consistent with art direction;
- bounded particle counts and scalable tiers.

## U3D-UI — Mobile Controls / HUD / Garage

**Owner boundary:** UI and input presentation only.

Required touch controls:
- steer;
- throttle;
- brake/reverse;
- handbrake;
- nitro if active;
- reset;
- pause.

Required HUD:
- speed;
- gear;
- route/objective;
- position;
- timer/lap/event state;
- pause/results.

Garage:
- hero vehicle presentation;
- rotate/inspect;
- selection;
- stats;
- paint/customization hooks;
- event entry.

UI must not be used to conceal weak 3D presentation.

## U3D-AUD — Audio

Required categories:
- engine/RPM layers;
- tire skid;
- impact;
- wind;
- traffic;
- Cairo night ambience;
- UI/countdown/race feedback.

Use original/generated/permissively licensed sources and preserve provenance.

## U3D-AUT — Build / Editor / Pipeline Automation

Required automation where feasible:
- project validation;
- world generation;
- prefab generation;
- material/import setup;
- Android build;
- screenshot capture;
- diagnostics export;
- quality preset configuration.

Possible menu entry points may exist for debugging, but owner clicks are not part of the production definition of done.

## U3D-QA — Android / Diagnostics / Performance

Primary target device class:
- HONOR ALI-NX1
- Android 15 / API 35
- Adreno 710
- ~11 GB RAM
- 2652×1200

Required diagnostics:
- exact source/build identity;
- quality tier;
- active scene;
- vehicle;
- camera;
- route;
- traffic;
- race state;
- FPS/frame-time samples;
- exceptions/recoveries.

Maintain `AFAREET_` JSONL markers.

---

# PARALLELIZATION MODEL

## Safe parallel groups

These can usually proceed concurrently when their interfaces are stable:

**Group A**
- U3D-CAM
- U3D-ASSET
- U3D-LGT look-dev scaffolding
- U3D-AUT

**Group B**
- U3D-VEH
- U3D-WLD
- U3D-ROAD
- U3D-TRF

**Group C**
- U3D-VFX
- U3D-UI
- U3D-AUD
- U3D-QA

## Shared interfaces that require coordination

Module Lock or an interface task is required before parallel edits to:
- player vehicle state contract;
- camera target state contract;
- race session lifecycle;
- world/track route data;
- input contract;
- save schema;
- global bootstrap;
- Android build configuration;
- shared materials/render pipeline settings.

Workers should not “solve” conflicts by creating duplicate competing systems.

---

# VERTICAL SLICE GATES

## Gate G0 — Live-state reconciliation

Before new work:
- fetch current main and active Unity branches/PRs;
- identify canonical Unity head;
- inspect runtime/build evidence;
- reconcile stale Flutter-era status statements;
- identify currently active worker ownership.

## Gate G1 — Camera + vehicle grounding

Pass when:
- chase camera reads strongly 3D;
- vehicle is visually grounded;
- wheels/suspension/body response are readable;
- no severe camera clipping;
- stable Android runtime.

## Gate G2 — One dense Cairo street slice

Pass when one representative 20–30 second driving segment includes:
- foreground/midground/background depth;
- road elevation/detail;
- dense modular buildings;
- side-street visual continuation;
- parked vehicles/traffic;
- lighting hierarchy;
- no obvious near-camera primitive look.

## Gate G3 — Premium moving screenshot/video frame

Pass when representative frames, even with HUD hidden, visually read as a modern mobile 3D racer and retain original 3FAREET identity.

## Gate G4 — Playable event loop

`Garage → Drive → Event → Finish → Reward → Garage/Next Event`

At least one integrated race/event must complete without critical exceptions.

## Gate G5 — Android performance / release evidence

Pass when:
- installable APK is generated from exact source SHA;
- real-device runtime succeeds;
- diagnostics are captured;
- no critical exceptions/missing-script failures;
- frame pacing is reasonable for target hardware;
- visual acceptance evidence exists.

---

# QUALITY TIERS

At minimum:

### Low
- reduced shadows;
- reduced particles/traffic density;
- lower reflection/post effects;
- conservative texture/LOD ranges.

### Medium
- balanced mobile default.

### High
- improved shadows, reflections, traffic/VFX density when supported.

Ultra is optional and must not become a dependency for visual acceptance.

---

# ORIGINALITY / PROVENANCE

Reference games are allowed only as mood/quality comparison.

Forbidden:
- extracting assets from other games;
- cloning maps;
- copying proprietary UI;
- reproducing real manufacturer logos;
- importing unknown-license assets without provenance.

Preferred:
1. procedural original assets;
2. Blender-scripted original assets;
3. generated Unity modular assets;
4. verified CC0/permissive assets.

---

# FINAL 3D PRESENTATION DEFINITION OF DONE

The current 3D presentation program is not terminal until all are true:

- [ ] canonical Unity runtime/head identified and documented;
- [ ] autonomous asset/editor/build pipeline exists for repeated work;
- [ ] original hero vehicle exists;
- [ ] chase camera creates strong perspective depth;
- [ ] vehicle physics and visual grounding are convincing;
- [ ] road/curb/sidewalk are visibly three-dimensional;
- [ ] one dense Cairo-inspired vertical slice surrounds the player;
- [ ] foreground/midground/background depth is continuous;
- [ ] traffic and parked cars establish scale;
- [ ] lighting/shadows create form and depth;
- [ ] materials/road decals prevent flat prototype appearance;
- [ ] drift/nitro VFX are integrated and scalable;
- [ ] touch controls/HUD function;
- [ ] garage/event loop functions;
- [ ] Android APK generated from exact source identity;
- [ ] real-device diagnostics show no critical runtime failure;
- [ ] representative screenshots pass visual review;
- [ ] performance is reasonable on Adreno-710-class hardware;
- [ ] original 3FAREET identity is clear.

Until these gates are satisfied, workers should continue with the next legitimate task within their assigned workstream rather than treating “compiles” as project completion.
