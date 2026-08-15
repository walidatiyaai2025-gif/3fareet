# PR #59 Visual QA Checklist

Target branch: `agent/UART-002-hero-car-art`

Do not call an APK visually verified until all checks below pass on the exact built commit.

## Hero Car
- Black/purple/gold hero identity is immediately readable from chase camera.
- Gold lip/side skirts, purple spirit eyes/runes, rear spoiler, rims and rear light details are visible.
- No geometry clipping through wheels/body at rest or during drift.

## HUD
- Position, race time, speed and Spirit Nitro meter are readable at phone scale.
- Nitro meter changes state clearly near high charge.
- Touch controls do not overlap core HUD information.

## Cairo Night Environment
- Neon rails alternate visibly and do not wash out the road.
- Cairo building silhouettes, roof crowns, pyramids and spirit crowns remain readable through fog.
- Road runes and roadside hazard markers are visible without obscuring racing line.

## VFX
- Drift effect appears only while drifting and remains readable behind the car.
- Nitro glow/trails are brighter than drift but do not hide the vehicle silhouette.
- VFX do not dominate the entire screen at normal chase distance.

## Camera Presentation
- FOV increases progressively with speed.
- Nitro adds a stronger FOV punch.
- High-speed nitro shake is subtle and does not harm steering readability.
- Camera returns smoothly after nitro/drift ends.

## Start / Finish Presentation
- Countdown pulse is visible for each countdown state.
- GO flash is visually stronger than countdown states.
- Start/finish gate, crowns, banners and roadside props are visible from grid position.

## Mobile Readability / Performance sanity
- No critical text is clipped at common 16:9 and 20:9 landscape ratios.
- No obvious Z-fighting or severe overdraw artifacts around neon props.
- No new visual element blocks track edges or safe driving line.

## Evidence required
Attach to Issue #62 or the build PR:
1. exact commit SHA;
2. APK artifact/release link;
3. screenshots: grid/start, mid-race, drift, nitro, finish gate;
4. device/emulator model and resolution;
5. notes for every failed visual check.
