# Module Ownership & Active Locks

**Owner:** Team Lead  
**قاعدة:** Role ownership للمراجعة، وActive Lock للحجز المؤقت فقط.

## Ownership map

| Module | Paths | Primary reviewer | Backup reviewer |
|---|---|---|---|
| Unity core/bootstrap | `unity_game/Assets/Afareet/Scripts/Core/` | Unity Tech Lead | Gameplay Lead |
| Vehicle physics | `unity_game/Assets/Afareet/Scripts/Vehicle/` | Gameplay Lead | Unity Tech Lead |
| Race & AI | `unity_game/Assets/Afareet/Scripts/Race/` | Gameplay Lead | AI Engineer |
| World/Track | `unity_game/Assets/Afareet/Scripts/World/`, `Assets/Scenes/` | Level Lead | Unity Tech Lead |
| Unity UI | `unity_game/Assets/Afareet/Scripts/UI/` | UI/UX Lead | Unity Tech Lead |
| Unity editor/build | `unity_game/Assets/Afareet/Editor/`, `ProjectSettings/`, `Packages/` | Unity Tech Lead | QA/Release Lead |
| 3D assets | `unity_game/Assets/Afareet/Art/`, `docs/assets/` | Art Director | Technical Artist |
| Branding | `assets/branding/`, `unity_game/Assets/Afareet/Branding/` | Art Director | UI/UX Lead |
| Flutter legacy | `lib/`, `test/`, root `assets/`, `tool/` | Flutter Maintainer | Unity Tech Lead |
| Project governance | `docs/`, `.github/` | Team Lead | Product Owner |
| Backend | future `backend/`, API contracts | Backend Lead | Unity Tech Lead |

## Active work board

_No active scoped Module Locks are recorded after merge of DOC-P1-STATUS / PR #270._

### Reconciled legacy locks

The previous active-board rows dated `2026-08-13` (`agent/unity-3d-prototype` / PR #49) were reviewed by the Team Lead during DOC-P1-STATUS reconciliation and are **retired as active locks**. Their historical work is already represented by the later Unity convergence line and fixed U-P1 operational ledger; retaining them as live locks would falsely block current ownership decisions.

DOC-P1-STATUS / #269 was merged through PR #270 on 2026-08-29 and its temporary documentation lock is released.

Current convergence coordination lives in Issue #90 and PR #144. A convergence branch is not a blanket lock on every `unity_game/` path: any new source change still requires its own Task ID, human Owner and scoped Module Lock before modification.

## Lock procedure

1. أضف صفًا قبل تعديل الملفات.
2. لا تستخدم `TBD` كـOwner لمهمة `IN PROGRESS`.
3. إذا احتجت Path محجوزًا، اتفق مع Owner ودوّن Shared Lock أو قسّم Contract.
4. احذف الصف بعد الدمج، وانقل النتيجة إلى Task status/evidence.
5. Lock أقدم من 3 أيام بلا تحديث يراجعه Team Lead؛ لا يزال فعالًا حتى إلغائه صراحة.

## Release/evidence boundary

Module ownership does not grant authority to self-verify runtime, device, Visual Gate or publication evidence. `Built`, source-contract green, or static CI success remains distinct from `Device Verified` and from explicit release-owner approval.
