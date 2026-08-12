# Asset Budgets and Import Rules

## ART-003 — Texture budgets

- Hero vehicle exterior: up to 2K albedo/normal/ORM per primary material set.
- Secondary vehicle materials: 1K preferred.
- Environment hero props: 1K–2K; background props: 512–1K.
- UI atlases: use tightly packed WebP/PNG and avoid duplicate source images.
- Mobile runtime should prefer compressed GPU-friendly textures where supported.

## ART-004 — Polygon / LOD budgets

- Vehicle LOD0: <= 80k triangles.
- Vehicle LOD1: <= 40k triangles.
- Vehicle LOD2: <= 18k triangles.
- Hero environment prop: <= 25k triangles.
- Background prop: <= 8k triangles.
- Collision meshes must be simpler than render meshes.

## ART-005 — Collider rules

- Never use full render geometry as collision by default.
- Prefer primitive/convex colliders for cars and props.
- Track collision must avoid tiny disconnected faces and self-intersections.
- Decorative geometry is non-collidable unless gameplay requires it.

## ART-006 — Pivot / scale / orientation

- 1 Flutter/Flame world unit represents 1 meter unless a scene contract overrides it.
- Vehicle forward axis: +Y in exported gameplay metadata; conversion adapters may remap renderer axes.
- Vehicle pivot: ground contact center between axles.
- Environment prop pivot: logical placement base.
- Apply transforms before export and keep scale uniform.

## ART-007 — Car import checklist

1. Stable lowercase asset ID.
2. Correct pivot/orientation/scale.
3. LOD0/1/2 within triangle budgets.
4. Simplified collision proxy present.
5. Material/texture names follow `ASSET_PIPELINE.md`.
6. Required preview render available or safe fallback configured.
7. No embedded absolute file paths.
8. Runtime smoke load succeeds before promotion.

## ART-008 — Track import checklist

1. Track ID and scene version are explicit.
2. Start grid, checkpoints, finish and respawn anchors exist.
3. Collision mesh is watertight enough for race surface use.
4. Out-of-bounds zones are explicit.
5. Decoration does not alter gameplay collision accidentally.
6. LOD/visibility groups exist for heavy districts.
7. Lighting probes/look-dev tier metadata are documented.
8. Track can load in prototype without missing-asset exceptions.

## ART-009 — Environment prop checklist

- Stable prop ID and category.
- Correct pivot and physical scale.
- Collision required only for gameplay-relevant props.
- Texture and triangle budgets respected.
- Reusable props avoid baked scene-specific transforms.
- Decorative emissive materials respect performance tier caps.

## ART-010 — VFX atlas / pooling rules

- Reuse atlases for drift smoke, sparks and nitro families.
- No per-frame texture allocation.
- Repeated transient effects must be pooled.
- Pool capacity is bounded and degrades by dropping non-critical effects rather than allocating indefinitely.
- Low tier reduces particles first, then atlas animation rate if needed.

## ART-012 — Placeholder labeling

Every placeholder asset must include `_placeholder_` in its filename and must never be silently promoted to production. UI may show a fallback frame, but source-of-truth metadata must retain placeholder state.

## ART-013 — Validation checklist

Validation rejects or flags:

- spaces/uppercase/ambiguous names;
- files outside approved runtime folders;
- placeholder assets without explicit placeholder naming;
- oversized texture declarations;
- missing LOD/collider metadata for required asset classes;
- duplicate logical asset IDs;
- absolute local paths in metadata.
