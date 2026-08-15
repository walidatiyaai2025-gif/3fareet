# UART-007 — Track dressing / lighting vertical slice

State: `IN REVIEW`

The current Unity visual stack already implements the production-direction track dressing slice; this task was stale as `TODO` in the original register.

## Implemented evidence in the branch ancestry
- `CairoTrackBuilder.cs` — Cairo road, city blocks, pyramids, neon rails and start/finish treatment.
- `RoadsideArtPass.cs` — roadside spirit poles, blades, road runes and readable edge language.
- `CairoLandmarkRuntimePass.cs` — minaret, dome, pyramid and bridge landmark rhythm.
- `CairoSkylineDepthPass.cs` + crown accents — distant skyline depth and silhouette separation.
- `CairoStreetIdentityPass.cs` + `CornerChevronIdentityPass.cs` — sector/corner identity at racing speed.
- `StartLineLightPass.cs` — start-line purple/cyan framing lights.
- `CairoAmbientPulse.cs` — world-only ambient neon pulse, excluding player/rival lights.

## Acceptance mapping
- Track dressing exists throughout the one-lap Cairo vertical slice.
- Landmark silhouettes are distributed across multiple sectors.
- Purple/cyan road readability accents are present, with gold reserved for focal moments.
- Decorative geometry removes gameplay colliders.
- Mobile-density cleanup is present for skyline geometry.

## Pending validation
- Capture the required gameplay screenshots on the exact Unity head.
- Review screenshots against `docs/ART_DIRECTION.md`.
- Perform low/mid Android readability and performance review.

Implementation is complete enough for `TODO -> IN REVIEW`; it is not `VERIFIED` until screenshot/device Visual Gate evidence exists.
