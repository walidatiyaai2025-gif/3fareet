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
| 3D assets | `unity_game/Assets/Afareet/Art/` (planned), `docs/assets/` | Art Director | Technical Artist |
| Branding | `assets/branding/`, `unity_game/Assets/Afareet/Branding/` | Art Director | UI/UX Lead |
| Flutter legacy | `lib/`, `test/`, root `assets/`, `tool/` | Flutter Maintainer | Unity Tech Lead |
| Project governance | `docs/`, `.github/` | Team Lead | Product Owner |
| Backend | future `backend/`, API contracts | Backend Lead | Unity Tech Lead |

## Active work board

| Task | Owner | Branch/PR | Locked paths/contracts | Since | Status |
|---|---|---|---|---|---|
| U3D-001→U3D-012 bootstrap batch | Principal Mobile Game Architect | `agent/unity-3d-prototype` / PR #49 | `unity_game/`, build identity, migration docs | 2026-08-13 | IN REVIEW |
| U3D-006 + U3D-008 + UPER-004 + UPER-005 | Principal Mobile Game Architect | `agent/unity-3d-prototype` / PR #49 | Unity asmdefs, Vehicle/Camera config contracts, tests, Android toolchain and debug APK | 2026-08-13 | IN REVIEW |
| UVEH input stabilization + UART-002/AST-062 | Principal Mobile Game Architect | `agent/unity-3d-prototype` / PR #49 | `ArcadeCarController`, `PrototypeHud`, hero blockout/reference and Android verification | 2026-08-13 | IN PROGRESS |
| URAC-002 | GPT-5.6 Sol (Race/QA Agent) | `agent/URAC-002-checkpoint-validation` / PR #54 | Shared carve-out: new Race checkpoint-validation files, new EditMode checkpoint tests, URAC-002 status/evidence rows | 2026-08-13 | IN REVIEW |
| URAC-003 | GPT-5.6 Sol (Race/QA Agent) | `agent/URAC-003-one-lap-state` / PR #55 | Shared carve-out: new one-lap state/runtime file, new EditMode lifecycle tests, URAC-003 status/evidence rows; depends on PR #54 | 2026-08-13 | IN REVIEW |
| URAC-004 | GPT-5.6 Sol (Race/QA Agent) | `agent/URAC-004-race-ranking` / PR #56 | Shared carve-out: new deterministic ranking file, new EditMode ranking tests, URAC-004 status/evidence rows; depends on PR #55 | 2026-08-13 | IN REVIEW |
| URAC-005 | GPT-5.6 Sol (Race/QA Agent) | `agent/URAC-005-race-round-flow` / PR #57 | Shared carve-out: new countdown/results/restart state+controller, new EditMode flow tests, URAC-005 status/evidence rows; depends on PR #56 | 2026-08-13 | IN REVIEW |
| URAC-006 | GPT-5.6 Sol (Race/QA Agent) | `agent/URAC-006-track-bounds` | Shared carve-out: new deterministic road-corridor policy, solid track-edge colliders, off-road monitor and EditMode tests; depends on PR #57 | 2026-08-13 | IN REVIEW |

## Lock procedure

1. أضف صفًا قبل تعديل الملفات.
2. لا تستخدم `TBD` كـOwner لمهمة `IN PROGRESS`.
3. إذا احتجت Path محجوزًا، اتفق مع Owner ودوّن Shared Lock أو قسّم Contract.
4. احذف الصف بعد الدمج، وانقل النتيجة إلى Task status/evidence.
5. Lock أقدم من 3 أيام بلا تحديث يراجعه Team Lead؛ لا يزال فعالًا حتى إلغائه صراحة.
