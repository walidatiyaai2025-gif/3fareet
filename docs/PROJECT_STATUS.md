# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Purpose:** الصفحة التنفيذية السريعة لمعرفة وضع المشروع لحظة بلحظة  
**Last updated:** 2026-08-13 01:26 (Asia/Kuwait)  
**Overall status:** 🟡 **50/50 TASKS IMPLEMENTED — STACKED REVIEW / CI + DEVICE VERIFICATION PENDING**

> أي تغيير مادي يجب أن يحدّث هذه الصفحة في نفس الـPR.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Core gameplay | 🟢 Verified base | Existing verified gameplay/camera/AI/UI foundation preserved |
| Garage | 🟡 In review | GAR-001→014 implemented across stacked PRs |
| Career | 🟡 In review | CAR-001→015 implemented with offline save/migration and Chapter 1 progression |
| Asset pipeline | 🟡 In review | ART-001→010 + ART-012→014 implemented; ART-011 remains separate team audio work |
| Performance foundation | 🟡 In review | PER-001→009 implemented |
| Release QA | 🟡 In review | PER-010→019 implemented; device profiles still require physical-device evidence |
| Premium visual gate | 🔴 Open | VIS screenshot/device approval still required |
| Verified Release APK | 🔴 Open | Real-device smoke evidence still required |

## 50-task batch — implementation complete

- Tasks 1–10: `GAR-012→014 + CAR-001→007` — PR #33.
- Tasks 11–20: `CAR-008→015 + ART-001→002` — PR #34.
- Tasks 21–30: `ART-003→010 + ART-012→013` — PR #35.
- Tasks 31–40: `ART-014 + PER-001→009` — PR #36.
- Tasks 41–50: `PER-010→019` — current release-QA branch.

Implementation count: **50/50**.
Verification count: intentionally lower until stacked CI and device-only gates pass.

## Final release-QA evidence

- Android debug + release-skeleton build steps remain explicit in `.github/workflows/flutter-prototype-ci.yml`.
- APK artifacts are uploaded with 14-day retention and run-specific artifact names.
- `tool/version_stamp.sh` emits commit SHA / short SHA / CI run number build metadata.
- `test/prototype_acceptance_test.dart` adds automated foundation smoke acceptance.
- `docs/RELEASE_QA.md` defines smoke, acceptance, regression and low/mid/high device profiles.
- `docs/work/PER-010-019.md` records final-batch scope and verification boundary.

## Verification boundary

- `IN REVIEW` means implementation exists but is not yet promoted to VERIFIED.
- PER-017→019 cannot become VERIFIED from documentation/configuration alone; representative low/mid/high physical-device measurements are required.
- A Release APK is not called Verified until installed and smoke-tested on real Android hardware.
- ART-011 remains untouched to avoid collision with the active audio workstream.
- Backend path remains `Flutter/Flame → HTTPS API → Laravel → MySQL`; direct MySQL client access is prohibited.

## Remaining gates after this 50-task implementation batch

1. Retarget/merge stacked PRs in order and obtain Green CI evidence.
2. Fix any genuine analyzer/test/build failure surfaced by CI.
3. VIS-001→014 screenshot/device visual gate.
4. CAM-012 / VEH-017 / RAC-017 real-device verification.
5. Low/mid/high device performance measurements for PER-017→019.
6. Real-device Release APK smoke test and Last Verified APK publication.

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR.
