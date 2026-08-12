# Missed Asset Priority by Delivery Phase

Source of truth for individual asset state remains `docs/MISSED_ASSETS.md`. This document defines ART-014 phase ordering only; it does not replace ownership locks.

## P1 Prototype gate — P0 assets first

Assets marked P0 in `MISSED_ASSETS.md` block one or more of: gameplay readability, driving feel, visual review, or performance validation. They are scheduled before P1/P2 assets unless a Team Lead explicitly records an exception.

Priority groups:

1. Prototype Hero Car + collision/rig essentials.
2. Cairo Fantasy Prototype Track + collision + safe respawn data.
3. P0 drift/nitro VFX and prototype HUD/Spirit meter.
4. P0 engine/drift/nitro audio.
5. P0 road/asphalt textures and technical race markers.

## Core Alpha — P1 assets

P1 assets start once their dependent P0 gate is testable. They cover garage vehicle variants, Cairo environment packs, power-up VFX, core UI, and secondary audio/animation.

## Career / Beta / Online — P2 assets

P2 assets are deferred until the corresponding system is active: career districts, garage polish, ranks/pass art, store screenshots and other non-prototype deliverables.

## Scheduling rule

When an asset is claimed, copy its `Priority` and `Phase` from `MISSED_ASSETS.md` into the work issue/PR. Never promote a lower-priority cosmetic asset ahead of a blocking P0 asset merely because it is easier to produce.
