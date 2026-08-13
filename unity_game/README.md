# 3fareet Unity 3D

Playable mobile-first 3D vertical slice for **Afareet Asphalt**.

## Open

1. Install Unity Hub and Unity `6000.5.8f1` with Android Build Support.
2. Add this `unity_game` folder as a project.
3. Open it and press Play from any empty scene.

The runtime bootstrap creates the complete prototype automatically: Cairo oval
track, stylized player car, three AI rivals, chase camera, lights, HUD, touch
controls, drift and nitro feedback.

## Controls

- `W` / Up: accelerate
- `S` / Down: brake/reverse
- `A D` / Left Right: steer
- `Space`: drift
- `Left Shift`: nitro
- `R`: reset car

Landscape orientation is enforced at runtime. Touch controls appear on mobile.

## Verified build

- Unity `6000.5.8f1`: script compilation passed.
- Windows x64 development player: built successfully.
- Headless runtime smoke test: no exceptions.
- Android: pending installation of the Android Build Support module.
