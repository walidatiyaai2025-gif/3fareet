# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-13 01:20 (Asia/Kuwait)  
**Overall status:** 🟡 **50-TASK BATCH — TASKS 1→40 IMPLEMENTED / STACKED REVIEW**

> أي تغيير مادي يجب أن يحدّث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Core gameplay | 🟢 Verified base | Existing verified gameplay/camera/AI/UI foundation preserved |
| Garage | 🟡 In review | GAR-001→014 implemented across stacked PRs |
| Career | 🟡 In review | CAR-001→015 implemented with offline save/migration and Chapter 1 progression |
| Asset pipeline | 🟡 In review | ART-001→010 + ART-012→014 implemented; ART-011 remains separate team audio work |
| Performance foundation | 🟡 In review | PER-001→009 implemented: device/frame/memory/texture/VFX budgets, release-disabled overlay, explicit CI analysis/test steps |
| Release QA | ⚪ Next | PER-010→019 remain as tasks 41–50 |
| Premium visual gate | 🔴 Open | VIS screenshot/device approval still required |
| Verified Release APK | 🔴 Open | Real-device smoke evidence still required |

## 50-task batch progress

- Tasks 1–10: `GAR-012→014 + CAR-001→007` — implemented in PR #33.
- Tasks 11–20: `CAR-008→015 + ART-001→002` — implemented.
- Tasks 21–30: `ART-003→010 + ART-012→013` — implemented.
- Tasks 31–40: `ART-014 + PER-001→009` — implemented.
- Tasks 41–50: `PER-010→019` — next and final batch.

## Current engineering evidence

- `docs/MISSED_ASSET_PRIORITY.md` links asset priority to delivery phases without overriding `MISSED_ASSETS.md` ownership locks.
- `docs/PERFORMANCE_BUDGETS.md` defines target device, frame-time, memory, texture and VFX budgets.
- `lib/game/ui/debug_overlay.dart` is explicitly disabled in release mode.
- `.github/workflows/flutter-prototype-ci.yml` separates dependencies, format/analyze, asset validation, full tests and widget/game integration tests.
- `docs/work/ART-014-PER-009.md` records scope/evidence.

## Architecture locks

- Performance budgets are targets, not claims of real-device compliance.
- Low/mid/high hardware profiles remain unverified until measured on real devices.
- Existing CI is hardened rather than duplicated.
- ART-011 remains untouched to avoid collision with the active audio workstream.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct MySQL client access is prohibited.

## Remaining gates

1. Final tasks 41–50: `PER-010→019`.
2. Green CI for stacked PRs and ordered predecessor merge.
3. VIS-001→014 screenshot/device gate.
4. CAM-012 / VEH-017 / RAC-017 real-device verification.
5. Release APK smoke test on real Android hardware.

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
