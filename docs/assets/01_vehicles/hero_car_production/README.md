# Afareet King — UART-003 production Hero Car

This folder is the editable/source-of-truth vehicle asset set for the P1 Hero Car. It replaces the acceptance gap where the runtime Hero was built only from Unity primitives.

## Source meshes

| LOD | Vertices | Triangles | Triangle cap | Screen-height transition |
|---|---:|---:|---:|---:|
| LOD0 | 274 | 476 | 600 | 0.18 |
| LOD1 | 194 | 332 | 400 | 0.07 |
| LOD2 | 104 | 180 | 220 | 0.01 / cull below |

All models use meters, +Y up, +Z forward and +X right. Names follow the UART-001 matching-base-name `_LOD#` rule.

## Visual language

The geometry is an original fictional low aggressive coupe for **3Fareet**. It preserves the established Afareet King identity: dark body, purple spirit accents, gold aero language, four exposed performance wheels, a strong hood/spirit signature and a rear-wing silhouette at near LODs.

The source is intentionally low-poly and mobile-first. It is not derived from a real production vehicle model and has no third-party model/license dependency.

## Texture budget

P1 deliberately uses no texture maps for this Hero asset. The generated Unity prefab binds `Afareet/RuntimeLit` color/emission materials, so Hero texture-map memory is **0 KB**. Texture authoring can be a later visual-quality pass only after Android performance and Visual Gate evidence establishes room in the budget.

## Runtime generation

`Afareet.Editor.HeroCarProductionAssetBuilder.BuildOrThrow()` parses these OBJ files, validates the exact mesh budgets, creates generated Mesh/Material assets and builds the `PF_Vehicle_AfareetKing_Production` prefab with a three-level Unity `LODGroup`.

`HeroCarProductionBuildPreprocessor` invokes the builder before every Unity Player build. In interactive Editor sessions, the builder also generates the prefab once after domain load when the generated prefab is absent. This keeps clean checkouts and CI builds deterministic without modifying the shared `AfareetBuild` implementation.

At runtime `HeroCarProductionVisualInstaller` finds only the player Hero, disables its existing visual-only `MeshRenderer` blockout, then attaches the generated production LOD prefab. Rigidbody, gameplay collider, controller, trails, lighting and VFX remain owned by the existing runtime. If the production prefab is unavailable or invalid, the installer restores the procedural renderers and the race remains playable.

Generated Unity assets are derived build inputs and are git-ignored; the OBJ source + manifest + builder are the versioned source of truth.
