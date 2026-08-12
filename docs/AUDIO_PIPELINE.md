# 3fareet — Audio Pipeline

**Document:** AFA-AUDIO-001  
**Status:** Prototype baseline  
**Owner:** Audio / Technical Artist

## Runtime folder structure

- `assets/audio/music/` — long-form music and ambience.
- `assets/audio/engine/` — idle/low/high RPM layers.
- `assets/audio/vehicle/` — tire, drift, suspension and collision-adjacent vehicle sounds.
- `assets/audio/sfx/` — nitro, impacts, power-ups and world effects.
- `assets/audio/ui/` — click/confirm/back/countdown/result stings.
- `assets/audio/voice/` — announcer/character voice only when explicitly approved.

## Naming convention

`<system>_<event>_<variant>_<nn>.<ext>` using lowercase snake_case.

Examples:
- `engine_loop_low_01.ogg`
- `vehicle_drift_skid_asphalt_01.ogg`
- `sfx_nitro_spirit_activate_01.ogg`
- `ui_confirm_01.ogg`
- `music_cairo_fantasy_race_01.mp3`

## Music rule

The owner-provided 2026-08-12 source is registered as `AUD-MUS-001` and targets:
`assets/audio/music/cairo_fantasy_race_theme_01.mp3`.

It is a 30.772 s stereo, 44.1 kHz music candidate with an estimated ~120 BPM pulse. It should be auditioned first as race music; if the mood is too restrained for gameplay, move its usage to Main Menu/Garage without renaming the source asset.

Before production use:
1. Confirm the project has usage rights for the source.
2. Define clean loop points or intro/loop/outro regions.
3. Normalize the mobile mix without clipping.
4. Test ducking against engine, drift and nitro layers.
5. Smoke-test on a real Android device.

## Prototype format guidance

- Keep provided masters unchanged outside the runtime folder.
- Music: compressed stereo asset suitable for Android preview builds.
- Engine/drift loops: loop-safe assets with no audible click at wrap.
- One-shot SFX: trim leading silence and preserve transient impact.
- Avoid baking excessive loudness into individual assets; the runtime mixer owns balance.

## AI-generated audio policy

AI-generated audio is allowed for prototype/original sound design only when the generation tool/license permits commercial game use. Every generated source must record:
- generation tool/service,
- prompt or design brief,
- generation date,
- license/usage evidence,
- edit history when materially modified.

Do not imitate a living artist, copyrighted game soundtrack, recognizable commercial engine recording, or copyrighted character voice.

## P1 audio priority

1. `Prototype engine loop` — P0.
2. `Tire skid/drift` — P0.
3. `Nitro activation / Nitro Spirit signature` — P0.
4. `Crash impacts pack` — P1.
5. `Countdown 3-2-1-Go` — P1.
6. `Race finish sting` — P1.
7. `UI click/confirm/back` — P1.

## AI-ready briefs

### Engine prototype
Original fictional arcade racing engine. Layered idle/low/high RPM character, aggressive but not supercar-specific, seamless looping, dry output, no music, no speech, no branded/recognizable real vehicle recording.

### Drift / tire skid
Original dry asphalt tire scrub and drift skid, seamless 2–4 second loop, intensity-friendly texture, minimal reverb, no engine, no crowd, designed to layer under arcade racing gameplay.

### Nitro Spirit
Original supernatural Egyptian-fantasy nitro activation: fast air displacement, rising spectral energy, short magical metallic shimmer, powerful sub hit, 0.8–1.4 seconds, no speech, unmistakable signature distinct from conventional sci-fi boost sounds.

### Crash pack
Original arcade vehicle impacts in light/medium/heavy variants, metal/body thump plus debris transient, short tails, no glass-only dominance, no recognizable samples.

### Countdown
Punchy original electronic-percussive countdown tones for 3, 2, 1 and a distinct GO hit; readable on phone speakers and compatible with later Arabic/English announcer layering.

### Finish sting
2–3 second premium Egyptian-fantasy racing success sting: modern electronic pulse plus subtle metallic/organic motif, strong transient, no melody copied from any existing work.
