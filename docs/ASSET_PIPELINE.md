# Unity Asset Pipeline & Naming

## Source vs game-ready

- Editable sources لا توضع عشوائيًا داخل Unity Runtime folders.
- Source: `docs/assets/<category>/source/` أو storage خارجي مسجل.
- Approved exports: `unity_game/Assets/Afareet/Art/<Category>/`.
- Concepts/references لا تدخل Build ولا تعامل كـproduction textures.

## Naming

| Type | Pattern | Example |
|---|---|---|
| Model | `SM_<Category>_<Name>_<Variant>` | `SM_Vehicle_TukTuk_Player` |
| Prefab | `PF_<Category>_<Name>` | `PF_Track_CairoGate` |
| Texture | `T_<Name>_<Map>` | `T_TukTuk_Body_BC` |
| Material | `M_<Name>_<Variant>` | `M_TukTuk_Player` |
| VFX | `VFX_<Name>` | `VFX_NitroSpirit` |
| Audio | `SFX_<Event>_<Variant>` | `SFX_Drift_Loop_01` |
| UI | `UI_<Screen>_<Element>` | `UI_Race_NitroIcon` |

Map suffixes: `BC`, `N`, `M`, `R`, `AO`, `E`, `ORM` حسب shader contract.

## Import baseline

- Unity unit = 1 meter؛ forward/pivot convention يثبت في `UART-001` قبل الإنتاج الكمي.
- Mesh collider ممنوع للمركبات الديناميكية إلا بقرار Technical Artist.
- كل Asset إنتاجي له AST/UART Task وOwner ومصدر ترخيص واضح.
- Textures power-of-two قدر الإمكان، والحدود/format/mipmaps حسب device budget.
- السيارة والبيئة الكبيرة تحتاج LODs قبل `VERIFIED`.
- Materials المشتركة تفضل على نسخ material لكل prefab.
- VFX يستخدم pooling ويقدم particle/overdraw evidence.

## Review package

Source link + export + Unity prefab + import screenshot + poly/texture/LOD report + license/source note + Art Director approval.
