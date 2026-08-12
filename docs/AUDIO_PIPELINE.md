# Afareet Asphalt — Audio Pipeline

## Prototype music identity
The prototype music direction is **Rap / Trap × Egyptian Shaabi / Mahraganat** with Cairo-night racing energy.

Core palette:
- punchy rap/trap kick + snare + hats;
- controlled 808 low end;
- darbuka/tabla/shaabi percussion;
- short original oriental motifs;
- no named-artist imitation and no third-party copyrighted vocals/lyrics.

## Runtime folders
- `assets/audio/embedded/` — connector-safe encoded preview assets used by prototype runtime.
- `assets/audio/music/` — music metadata and later mastered runtime files.
- future: `assets/audio/sfx/engine/`, `drift/`, `nitro/`, `impact/`, `ui/`.

## Naming
Use lowercase snake_case and semantic version-neutral names, for example:
- `cairo_rap_shaabi_race_theme_01.mp3`
- `engine_prototype_idle_loop_01.ogg`
- `drift_skid_asphalt_loop_01.ogg`
- `nitro_spirit_activation_01.wav`

## First APK implementation
`AUD-MUS-001` is the owner-provided source transformed into a Rap×Shaabi prototype direction. For the first APK a short MP3 preview is Base64-encoded as a tracked text asset, decoded in memory, and looped with `flame_audio`.

This is a packaging workaround for the prototype only. Before production replace it with a normal mastered audio binary and validate loop points, loudness, memory footprint, and Android device playback.

## Mix rules
- BGM must leave headroom for engine, drift and Nitro SFX.
- Prototype BGM runtime volume starts at `0.52`.
- Engine is the primary gameplay feedback layer once available.
- Nitro activation must cut through the mix without clipping.
- Avoid excessive sub-bass on low-tier phone speakers.

## P0 SFX backlog
1. Engine idle/low/high loop or layered RPM set.
2. Tire skid/drift loop with intensity response.
3. Nitro Spirit activation signature.

Then add crash impacts, countdown, finish sting, and UI click/confirm/back.

## AI generation briefs
### Engine
Original fictional compact sedan engine, arcade racing game, seamless RPM layers, clean mechanical midrange, no recognizable branded vehicle recording, dry source suitable for realtime mixing.

### Drift
Dry asphalt tire skid, seamless loop, three intensity layers, aggressive arcade character, minimal ambience, no music.

### Nitro Spirit
Original supernatural Egyptian-fantasy nitro activation: fast pressure rise, magical breath, metallic shimmer, short darbuka-like transient, powerful sub hit, 0.8–1.2 s, no voice or lyrics.

## Validation gate
An audio asset becomes `VERIFIED` only after:
- rights/source recorded;
- naming/path recorded;
- build succeeds;
- playback works on a real Android device;
- no clipping or obvious loop click;
- BGM/SFX balance passes gameplay smoke test.
