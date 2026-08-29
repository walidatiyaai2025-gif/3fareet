# عفاريت الأسفلت — Project Status Dashboard

**Document:** AFA-STATUS-001  
**Last updated:** 2026-08-29 (Asia/Kuwait)  
**Overall status:** 🟡 **UNITY 3D SOURCE/CONTRACT CONVERGENCE ADVANCED — FINAL PRODUCTION ART + LICENSED UNITY + DEVICE/OWNER/RELEASE GATES BLOCKED**

> هذه هي الصفحة الأولى للفريق. أي PR يغيّر Task/Milestone/Blocker/Build/Asset يجب أن يحدّثها في نفس PR.
>
> **Operational authority:** Issue #90. Fixed U-P1 register: `IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`.
> `IN REVIEW` أو CI static/source success لا تعني `VERIFIED`.

## Executive snapshot

| Area | Status | Current reality |
|---|---|---|
| Product client | 🟢 Locked | Unity `6000.5.8f1` داخل `unity_game/`; Flutter/Flame legacy/reference only |
| Active convergence | 🟡 Draft | `agent/p1-remediation-convergence`; reconciliation source head `ad1532fe58df696c6a8e998bfbb2c4e5c2e78c5a`; PR #144 remains blocked from integration |
| Main integration | 🟡 Draft | PR #112 remains blocked from `main` while any P1 blocker remains |
| Source/contract CI | 🟢 Green | Android QA, Windows native candidate verifiers, P1 Production Art gate and observed post-P1 source/contracts are green on the current convergence line |
| Hosted Unity execution | 🔴 Credential blocked | Unity Production/Experimental static contracts pass, but license preflight has no configured Unity license/email/password/serial; Unity EditMode/PlayMode and hosted Android/Windows builds are skipped behind that gate |
| Licensed exact-SHA candidate | 🔴 Not executed on current head | The licensed-Windows workflow contract can pass on PR events while the actual exact-SHA Unity candidate job remains skipped until explicit supported execution |
| Core race/gameplay | 🟡 Source-complete / runtime-unverified | Race lifecycle, career/session, power-ups, input/recovery, AI and runtime hot-path hardening are integrated on convergence; final exact-candidate Unity/device proof still required |
| Hero production car | 🔴 BLOCKED | No acceptable externally-authored UART-003 production source has been promoted; generated/refinement candidates are explicitly non-production |
| Rival production cars | 🔴 BLOCKED | UART-004 requires acceptable external production package plus licensed prefab/runtime/owner proof |
| Cairo/world/landmarks/dressing | 🟡 Implemented / unverified | UART-005/006/007 source/runtime replacement paths and URAC-011 authored 24-control/72-segment layout exist; exact-candidate Player/device/owner proof is still blocked |
| Physical-device acceptance | 🔴 BLOCKED | UVEH-012, URAC-012 and UPER-006 require a new exact-candidate Android device evidence chain |
| Visual Gate | 🔴 BLOCKED | UPER-009 requires candidate-bound owner/Art Director acceptance |
| Publication | 🔴 BLOCKED | UPER-010 is the final explicit manual publication approval after all preceding gates |
| Last Verified Unity APK | 🔴 None | `releases/LAST_VERIFIED_APK.md` remains unchanged: no Unity APK has completed required real-device verification + manual publication preflight |
| Backend | 🔵 Deferred/Locked | Unity → HTTPS API → Laravel → MySQL; no direct DB |

## Current milestone — U-P1 Unity 3D Vertical Slice

**Gate target:** سباق 3D واحد كامل على Android به سيارة لاعب، 3 AI، Cairo premium look، Drift/Nitro، HUD/Touch، Audio، performance/device evidence، ثم owner/release approval على **نفس exact candidate**.

### Current engineering reality

The original prototype/blockout state is no longer an accurate description of the convergence branch. Subsequent focused work has materially closed source-level/runtime architecture, race flow, career persistence, player/AI power-ups, mobile input/recovery, authored Cairo layout/world paths, performance hot paths and fail-closed candidate/evidence tooling.

What remains is not a generic programming backlog. The fixed release boundary is now dominated by production-art intake and real execution/evidence:

1. acceptable external Hero and Rival production art;
2. licensed Unity staging/import on a clean exact convergence SHA;
3. licensed EditMode/PlayMode + exact Android ARM64 candidate build;
4. real Player proof of Hero/Rivals/Cairo/landmarks/dressing/layout;
5. physical-device driving/lap/performance evidence from 0/16;
6. owner/Art Director Visual Gate;
7. explicit final manual publication approval.

## Fixed P1 blockers — 11

| ID | Blocker | Closure evidence required |
|---|---|---|
| UART-003 | Hero production model | Acceptable external production source + licensed binding/render proof |
| UART-004 | Rival production package | Acceptable external package + licensed prefab/runtime/owner proof |
| UART-005 | Cairo street kit runtime acceptance | Licensed runtime/device/owner proof |
| UART-006 | Cairo landmarks runtime acceptance | Licensed landmark runtime/device/owner proof |
| UART-007 | Track dressing runtime acceptance | Licensed dressing runtime/device/owner proof |
| URAC-011 | Authored Cairo vertical-slice layout acceptance | Exact-candidate runtime/device/owner proof |
| UVEH-012 | Driving feel | Physical-device human acceptance on exact candidate |
| URAC-012 | Race lifecycle | Physical-device lap/results/restart verification |
| UPER-006 | Android performance | Exact-candidate smoke/profiler/performance matrix |
| UPER-009 | Visual Gate | Owner/Art Director approval of exact candidate evidence |
| UPER-010 | Publication | Final manual release-owner approval after all gates |

## CI / build truth

### Current convergence line

Source/static validation is healthy. On the current convergence head at reconciliation:

- Android QA Tools: success.
- Windows Native Candidate Verifiers: success.
- P1 Production Art Gate: success.
- Post-P1 career/power-up/vehicle/source contracts observed for the head: success.
- Unity Production CI static contract: success.
- Unity Experimental APK static contract: success.

### What is **not** proven

Hosted Unity license preflight currently fails because the repository Actions environment has no configured Unity credential set. Therefore hosted Unity tests and player builds behind that preflight are skipped. A green static workflow contract is not a licensed Unity run.

Historical experimental APKs and locally licensed checkpoints remain useful engineering evidence only. They cannot be reused as the first `Last Verified Unity APK`, because they are not the final exact candidate and did not complete the required physical-device/manual publication chain.

## Production-art truth

- Generated/procedural/refinement Hero assets are allowed only in their explicit non-production lanes.
- They cannot satisfy UART-003 or UPER-009 by relabelling.
- No acceptable production Hero source has been promoted on the current convergence line.
- UART-004 also remains blocked on acceptable external production source/package plus licensed binding/runtime/owner proof.
- Cairo street/landmark/dressing/layout remediation is implemented but remains unverified until the same production candidate proves it in Player/device evidence.

## Release truth

**Status:** 🔴 **NO UNITY DEVICE-VERIFIED RELEASE APK YET**

Authoritative pointer: [`releases/LAST_VERIFIED_APK.md`](releases/LAST_VERIFIED_APK.md).

Do not:

- rename an experimental/candidate APK to “Verified”;
- reuse evidence from a rejected or different SHA/APK;
- treat source/static CI as runtime proof;
- publish/tag/update Last Verified while any of the 11 blockers remains.

## Team entry points

- [Operational U-P1 ledger — Issue #90](https://github.com/walidatiyaai2025-gif/3fareet/issues/90)
- [Current convergence — PR #144](https://github.com/walidatiyaai2025-gif/3fareet/pull/144)
- [Main-target integration — PR #112](https://github.com/walidatiyaai2025-gif/3fareet/pull/112)
- [New contributor onboarding](ONBOARDING.md)
- [Team workflow and DoD](TEAM_WORKFLOW.md)
- [Module ownership and locks](MODULE_OWNERSHIP.md)
- [Active Unity task register](tasks/06-UNITY-3D-MIGRATION.md)
- [Release policy](RELEASE_POLICY.md)
- [Last Verified Unity APK](releases/LAST_VERIFIED_APK.md)

## Historical Flutter evidence

Flutter engineering remains historical/reference evidence only. It is not Unity product completion and must not be used to satisfy Unity P1 runtime/release gates.

## Source of truth hierarchy

1. **Issue #90** — live operational fixed-65 ledger and blocker state.
2. **`docs/releases/LAST_VERIFIED_APK.md`** — sole Verified Unity APK pointer.
3. **PR #144 / active convergence branch** — current implementation line.
4. **This dashboard + `tasks/06-UNITY-3D-MIGRATION.md`** — repository-facing status mirrors and must be kept reconciled.
5. Task-specific PR/evidence artifacts — implementation/evidence detail.

---

**Owner:** Team Lead / Project Manager  
**Update rule:** تحديث الحالة جزء من Definition of Done لنفس الـPR. No false completion claims.
