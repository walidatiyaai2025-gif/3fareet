# Unity Asset Pipeline & Naming

**Task:** `UART-001`  
**Engine:** Unity `6000.5.8f1`  
**Human-readable source of truth:** this document  
**Machine-readable contract:** [`assets/UNITY_ASSET_CONVENTION.json`](assets/UNITY_ASSET_CONVENTION.json)

This convention applies to new 3D production assets for the Unity client. It does not promote references or blockout art to production quality and does not override asset-specific tasks such as `UART-002`.

## Source vs game-ready lifecycle

Editable sources must not be dropped directly into Unity runtime folders.

| Stage | Location | Meaning |
|---|---|---|
| Reference | `docs/assets/<category>/references/` | Visual/reference material only; never imported automatically as production content |
| Source | `docs/assets/<category>/source/` | Editable DCC/source files or a manifest pointing to external source storage |
| Candidate | `docs/assets/<category>/exports_candidate/` | Game-ready export waiting for technical/art review |
| Approved export | `docs/assets/<category>/exports/` | Approved export ready to be copied/imported into the Unity Art tree |
| Unity runtime | `unity_game/Assets/Afareet/Art/<Category>/` | Production runtime asset after review/import |

`00_reference`, UI, audio, and marketing categories are not automatically eligible for 3D runtime export. The machine-readable contract records which source categories can map to Unity runtime categories.

## Coordinate, scale and transform contract

- Unity world unit = **1 meter**.
- Up = **+Y**.
- Forward = **+Z**.
- Right = **+X**.
- Export at scale `1.0` with authored transforms normalized/zeroed where practical.
- Do not compensate for incorrect DCC scale by hiding a corrective scale in a gameplay prefab.
- Asset-specific exceptions must be documented in that asset's Task/Evidence and reviewed by the Technical Artist.

### Pivot rules

- Static prop: pivot on the placement/contact plane at a stable logical origin.
- Modular environment: pivot on the authored modular snap corner/origin; grid size belongs to the environment asset task.
- Vehicle: this convention only fixes scale/axes. Wheel/chassis pivot details stay owned by the vehicle task (`UART-002`/successors).
- VFX: pivot at the gameplay attachment or emission origin.

These rules deliberately avoid rewriting the active Hero Car reference/blockout contract.

## Naming

Runtime asset names are case-sensitive, ASCII-only, and contain no spaces. Use Pascal-style name tokens separated by underscores.

| Type | Pattern | Example |
|---|---|---|
| Model / mesh | `SM_<Category>_<Name>[_<Variant>]` | `SM_Vehicle_TukTuk_Player` |
| Prefab | `PF_<Category>_<Name>[_<Variant>]` | `PF_Track_CairoGate` |
| Texture | `T_<Name>_<Map>` | `T_TukTuk_Body_BC` |
| Material | `M_<Name>[_<Variant>]` | `M_TukTuk_Player` |
| Collider helper | `COL_<Category>_<Name>` | `COL_Prop_ConcreteBarrier` |
| Socket / attachment | `SO_<Category>_<Name>` | `SO_Vehicle_Nitro` |
| Animation | `A_<Category>_<Name>[_<State>]` | `A_Spirit_Idle_Loop` |
| VFX | `VFX_<Name>[_<Variant>]` | `VFX_Nitro_SpiritTrail` |
| Audio | `SFX_<Event>_<Variant>` | `SFX_Drift_Loop_01` |
| UI | `UI_<Screen>_<Element>` | `UI_Race_NitroIcon` |

The exact validator regexes for 3D asset kinds live in `docs/assets/UNITY_ASSET_CONVENTION.json` so a later Editor/CI validator can consume one contract instead of duplicating naming logic.

### Texture map suffixes

- `BC` — Base Color, sRGB.
- `N` — Normal map, linear/NormalMap import.
- `M` — Metallic, linear.
- `R` — Roughness, linear.
- `AO` — Ambient Occlusion, linear.
- `E` — Emission color, sRGB.
- `ORM` — packed Occlusion/Roughness/Metallic, linear.

3D world textures use mipmaps unless the owning task documents a justified exception. Max texture size and compression are constrained by the current device/performance budgets; source resolution is not automatically a valid runtime resolution.

## LOD naming

Assets that require LODs keep the same base name and change only the suffix:

- `SM_Prop_CairoLamp_LOD0`
- `SM_Prop_CairoLamp_LOD1`
- `SM_Prop_CairoLamp_LOD2`

The accepted suffix is `_LOD0` through `_LOD3`. LOD count and triangle targets remain asset-specific and must be justified by the relevant art/performance task.

## Model import baseline

Unless an asset task explicitly records an exception:

- Model scale factor: `1.0`.
- Do not import DCC cameras.
- Do not import DCC lights.
- `Read/Write` disabled by default.
- Do not auto-generate colliders from the imported render mesh.
- Dynamic vehicle MeshCollider is not allowed by default.
- Do not bake gameplay tuning into imported object transforms.
- Shared materials are preferred over per-prefab material duplication.

Importer/project-setting implementation is a later Technical Art task; `UART-001` defines the contract only and does not modify the currently locked Unity tree.

## Required asset metadata

Every production candidate must have enough evidence to identify:

- Task/Asset ID.
- Owner.
- Source or license/provenance.
- Intended Unity runtime path.
- Review state (`candidate` / `approved` / `rejected`).

`docs/MISSED_ASSETS.md` remains the live coordination register for individual assets. Do not claim the same Asset ID in parallel.

## Review package

Before an asset can be treated as production-ready, provide the applicable subset of:

- source link or source manifest;
- exported model/texture files;
- intended Unity prefab/runtime path;
- import-settings screenshot;
- scale/pivot screenshot;
- triangle/poly report;
- texture size/map report;
- LOD report when required;
- license/source note;
- Art Director/Technical Artist review evidence.

References are never promoted automatically simply because they are present in `docs/assets/`.

## Validator contract

[`docs/assets/UNITY_ASSET_CONVENTION.json`](assets/UNITY_ASSET_CONVENTION.json) is intentionally data-only and contains:

- source/runtime category mapping;
- coordinate and pivot policy;
- regex naming rules and valid examples;
- LOD suffix rule;
- texture map/color-space rules;
- model import defaults;
- required metadata and forbidden reference promotion.

A future Unity Editor or CI validator should parse this file rather than hardcode a second set of rules. Changing the contract requires a reviewed task because artists and automation will both depend on it.
