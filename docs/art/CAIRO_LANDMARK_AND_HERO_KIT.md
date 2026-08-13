# 3Fareet — Cairo Landmark + Hero Art Production Kit

## Hero Car silhouette kit

### Front signature
- Two cyan eyebrow blades above the existing purple spirit eyes.
- Eyebrows angle inward 8–12 degrees for an aggressive supernatural expression.
- Keep eyebrow width large enough to survive mobile downscale.
- No collider changes.

### Rear signature
- Twin vertical spirit fins outside the rear glass/spoiler supports.
- Left fin: electric purple. Right fin: cyan.
- One thin gold center spine above rear bumper centerline.
- Rear point lights: purple left / cyan right.
- Rear effect must remain readable in chase camera at 6–12 m.

### Material hierarchy
- Body: deep purple/near-black.
- Primary supernatural glow: purple.
- Navigation/readability glow: cyan.
- Premium accent: gold, max 10–15% of visible trim area.
- Avoid large white surfaces except headlight cores/fangs.

## Cairo landmark kit

Each landmark must read as a silhouette first and a detailed prop second.

### Landmark A — Spirit Minaret Cluster
- Three staggered minaret towers.
- Center tower tallest.
- Purple cap glow, cyan mid-band, dark stone body.
- Place outside collision corridor, visible from 120–180 m approach.

### Landmark B — Neon Dome Gate
- Low dome silhouette with twin side towers.
- Gold crown line only at the apex.
- Purple base lighting, cyan side markers.
- Use near a medium-speed bend so the silhouette stays readable.

### Landmark C — Pyramid Horizon Pair
- Two dark pyramid forms with subtle cyan rim light.
- Keep far enough away to read as skyline, not track geometry.
- Never use bright gold across the full pyramid surface.

### Landmark D — Cairo Bridge Gantry
- Wide overhead structural frame inspired by urban bridge steelwork.
- Purple left rail / cyan right rail.
- Small gold center mark for route confirmation.
- No hanging geometry below safe vehicle clearance.

## Track-sector rhythm

### Sector 1 — Start District
- Hero car + gold start crown.
- Sparse props.
- First landmark: Spirit Minaret Cluster.

### Sector 2 — Neon Market Straight
- Denser purple/cyan edge language.
- Low banner repetition.
- Neon Dome Gate at exit.

### Sector 3 — Desert Edge
- Reduce clutter.
- Pyramid Horizon Pair becomes main skyline read.
- Gold almost absent.

### Sector 4 — Return / Finish
- Reintroduce gold progressively.
- Cairo Bridge Gantry before finish.
- Finish crown becomes strongest gold focal point in the whole track.

## Road language
- Turn danger: purple + cyan edge blades.
- Confirmed racing line / major waypoint: small cyan rune.
- Premium moment / start / finish only: gold.
- Avoid random color switching that weakens navigation semantics.

## Mobile performance rules
- Prefer primitive or low-poly silhouettes.
- Decorative geometry must have colliders removed.
- Avoid per-prop dynamic lights where emissive materials are enough.
- Reserve real-time point lights for hero moments only.
- Keep repeated landmark pieces instancing-friendly.

## Acceptance views
1. Front 3/4 Hero Car at start line: cyan brows readable.
2. Rear 3/4 chase view: twin spirit fins + gold spine readable.
3. Sector 1 approach: minaret cluster visible before turn-in.
4. Sector 3 straight: pyramid pair readable without distracting from road.
5. Finish approach: bridge gantry leads eye to gold finish crown.

## Implementation order
1. Hero front/rear silhouette details.
2. Spirit Minaret Cluster.
3. Pyramid Horizon Pair.
4. Neon Dome Gate.
5. Cairo Bridge Gantry.
6. Sector color rhythm tuning.
7. Visual QA screenshots against mobile readability checklist.
