# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-13 01:14 (Asia/Kuwait)  
**Overall status:** 🟡 **50-TASK BATCH — TASKS 1→30 IMPLEMENTED / STACKED REVIEW**

> أي تغيير مادي يجب أن يحدّث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Core gameplay | 🟢 Verified base | PRO / VEH / DRF / RAC / CAM / AI / UI core verified from predecessor batches |
| Power-ups | 🟡 In review | PWR-006→014 implemented in predecessor PR #9 |
| Garage | 🟡 In review | GAR-001→014 implemented across stacked PRs |
| Career | 🟡 In review | CAR-001→015 implemented, including save/migration and Chapter 1 progression |
| Asset pipeline | 🟡 In review | ART-001→010 + ART-012→013 implemented; ART-011 remains owned by separate audio workstream |
| Performance/CI | ⚪ Next | ART-014 + PER-001→019 remain in 50-task manifest |
| Premium visual gate | 🔴 Open | VIS screenshot/device approval still required |
| Verified Release APK | 🔴 Open | Real-device smoke evidence still required |

## 50-task batch progress

- Tasks 1–10: `GAR-012→014 + CAR-001→007` — implemented in PR #33.
- Tasks 11–20: `CAR-008→015 + ART-001→002` — implemented on `feature/CAR-008-ART-002-progression-assets`.
- Tasks 21–30: `ART-003→010 + ART-012→013` — implemented on `feature/ART-003-013-asset-pipeline`.
- Tasks 31–40: `ART-014 + PER-001→009` — next.
- Tasks 41–50: `PER-010→019` — queued after tasks 31–40.

## Current asset engineering evidence

- `docs/ASSET_PIPELINE.md`: folder and naming contract.
- `docs/ASSET_BUDGETS.md`: texture, polygon/LOD, collider, pivot/orientation and import rules.
- `tool/validate_assets.dart`: runtime folder / filename / placeholder validation.
- `test/asset_pipeline_rules_test.dart`: naming and placeholder rule tests.
- `docs/work/ART-003-013.md`: task evidence.

ART-011 is intentionally untouched to avoid collision with the Technical Audio / Integration workstream already marked IN REVIEW.

## Architecture locks

- Runtime asset IDs/paths are stable and lowercase snake_case.
- Render geometry is not accepted as default collision geometry.
- Vehicle and environment assets have explicit LOD/texture budgets.
- Repeated transient VFX use bounded pools rather than unbounded runtime allocation.
- Placeholder state is explicit in both path/name conventions and cannot silently become production.
- Career and Garage prototype persistence stay local and versioned; backend remains `Flutter/Flame → HTTPS API → Laravel → MySQL` with no direct MySQL client access.

## Remaining gates

1. Tasks 31–50 from the active manifest.
2. Green CI for stacked PRs and ordered predecessor merge.
3. VIS-001→014 screenshot/device gate.
4. CAM-012 / VEH-017 / RAC-017 real-device verification.
5. Release APK smoke test on real Android hardware.

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
