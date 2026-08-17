# Unity Production + Flutter Legacy Release Policy

## Artifact identity

- Unity Android debug: `afareet-unity3d-debug.apk`.
- Unity release candidate: `afareet-unity3d-v<version>-rc<build>.apk`.
- Unity verified release: `afareet-unity3d-v<version>-verified.apk`.
- Flutter reference: `afareet-flutter-debug.apk` أو `afareet-flutter-release-skeleton.apk`؛ لا يوضع في مجلد Verified المنتج.

## Tag format

`unity-prototype-vMAJOR.MINOR.PATCH+BUILD`

Example: `unity-prototype-v0.1.0+1`.

## P1 authoritative override

For the current Unity P1 closure, `tools/android/p1_operator_release_chain.json` is the machine-readable operator sequence. The P1 wrappers are authoritative because they preserve the licensed-staging SHA -> reviewed candidate SHA -> exact APK -> physical-device session -> sanitized review -> human-approval lineage.

**The generic `tools/android/verify_release_publication.py` is not sufficient to satisfy P1 publication readiness.** It remains a compatibility/helper path for non-P1 or legacy evidence flows only. A P1 candidate must use `tools/android/verify_p1_release_publication.py` after the complete P1 lineage chain.

The P1 operator chain must not be shortened by substituting generic helpers for these P1 boundaries:

1. Hero source intake and six-task visual-source readiness.
2. Licensed staging readiness and native Windows Hero preflight.
3. Native handoff-packet verification, then licensed Unity staging, followed by explicit human review and commit of the approved staging delta.
4. P1 staged-candidate lineage verification and candidate generation.
5. P1 physical-device session binding and the fixed 16-checkpoint capture.
6. P1 sanitized review export + P1 review-lineage verification.
7. P1 lineage-bound five-gate manual approval readiness.
8. P1 publication preflight.
9. Explicit release-owner publication action and post-publication evidence recording.

No automated stage may publish, tag, upload a release asset, update Last Verified, or self-assert VERIFIED.

## P1 licensed Unity staging entrypoint

For P1 closure, Stage 5 must be invoked through the authoritative native wrapper. The wrapper validates the schema-v2 licensed handoff packet against the current clean, exact, non-synthetic Git HEAD before the low-level Unity staging implementation is allowed to run:

```powershell
pwsh -File tools/android/run_p1_licensed_staging_windows.ps1 `
  -HeroSource "Assets/Afareet/ArtSource/Vehicles/HeroCar/AfareetKing.fbx" `
  -HandoffPacket "artifacts/production-staging/p1-licensed-handoff-packet.json"
```

The required packet must already be `READY_FOR_LICENSED_OPERATOR_HANDOFF` and must bind all of the following to the current workstation checkout:

- exact 40-character source Git SHA, with observed SHA equal to expected SHA and current `HEAD`;
- non-synthetic checkout identity (`refs/pull/.../merge` is never sufficient);
- exact tracked Hero source under `unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/`;
- all six visual/runtime source tasks source-ready with zero source blockers;
- licensed-staging readiness with zero blocked checks;
- the exact SHA-256 of `tools/android/p1_operator_release_chain.json`;
- all runtime/device/human/publication/verification flags still false.

`tools/android/verify_p1_licensed_handoff_packet_windows.ps1` performs the native verification and writes `artifacts/production-staging/p1-native-handoff-verification.json` before Unity staging starts.

`tools/android/stage_production_candidate_windows.ps1` is the **low-level implementation detail** used by the authoritative wrapper. It must not be invoked directly as the P1 closure entrypoint. Direct invocation bypasses the required packet-to-HEAD binding and is not acceptable P1 release evidence even if the underlying Unity staging later succeeds.

## Required gates before a prototype tag

1. PR merged to `main` with all required checks Green.
2. Unity headless compile/tests Green on the exact release commit.
3. Unity Android build/candidate integrity Green on that same commit and APK SHA-256.
4. Candidate-bound physical-device checkpoints complete with zero automated Fatal/ANR/native-fatal red flags.
5. Privacy-safe content-addressed review bundle verifies for the exact candidate Git SHA + APK SHA-256.
6. Gameplay/QA/Art approvals and the explicit `UPER-010` release-owner approval match that exact review bundle and candidate lineage.
7. For **P1**, `tools/android/verify_p1_release_publication.py` returns `P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION` for the exact candidate/APK/session/P1-review/P1-approvals chain. Generic `verify_release_publication.py` alone cannot satisfy this gate.
8. `docs/PROJECT_STATUS.md` updated with the release state.
9. Only then may the release owner create the GitHub Release/tag and upload the exact tested APK as `afareet-unity3d-last-verified.apk`.
10. Update `docs/releases/LAST_VERIFIED_APK.md` with release/asset links, commit, APK SHA-256, review `contentSetSha256`, device and evidence.
11. Never move the pointer for a merely built, CI-only, emulator-only, stale-evidence, failed-preflight or unapproved candidate.

Flutter checks تبقى مطلوبة فقط إذا لمس PR مسار Flutter. CI artifacts هي preview/build evidence وليست تلقائيًا Verified APKs.

## P1 publication preflight

The P1 publication preflight is intentionally a **read-only eligibility check**. It never creates tags/releases, uploads assets, renames APKs, updates the Last Verified pointer, or sets `verified=true`.

Run it only after the exact licensed-Windows P1 candidate has completed the P1 lineage-bound physical-device/manual-review flow:

```bash
python3 tools/android/verify_p1_release_publication.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/p1-lineage-approvals.json \
  --output evidence/p1-publication-preflight.json
```

The P1 preflight independently rechecks:

- exact local licensed-Windows candidate manifest and APK bytes/size/SHA-256;
- candidate-manifest bytes equal the manifest bound into the P1 physical-device session;
- staging-source SHA -> direct-parent candidate SHA lineage;
- exact six visual/runtime task scope;
- P1 sanitized review bundle and its lineage fingerprint;
- P1 source-artifact digests and performance tier;
- fixed 16-checkpoint physical-device evidence with zero automated red flags;
- all five explicit human approval records, including final `UPER-010`;
- final readiness is `READY_FOR_RELEASE_REVIEW` while automation remains `verified=false`, `runtimeVerified=false`, `ownerAccepted=false`, and `publicationPerformed=false`.

A successful marker is:

`AFAREET_P1_RELEASE_PUBLICATION_PREFLIGHT_OK ... verdict=P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION publicationPerformed=false verified=false`

A successful P1 preflight is **not publication** and is **not VERIFIED**. The release owner must still perform the publication action explicitly and record the resulting release evidence.

If any input changes after review—staging lineage, candidate manifest, APK bytes, device evidence, review bundle, approval fingerprint, performance tier, or reviewer decision—the P1 preflight must be rerun and must fail until the chain is consistent again.

## Generic publication preflight (non-P1 compatibility)

`tools/android/verify_release_publication.py` remains available for legacy/non-P1 flows. It is also read-only and cannot publish or self-verify. It must not be used as a substitute for the authoritative P1 chain above.

## Last Verified invariants

- Build success = `Latest Built`, not `Verified`.
- Candidate-ready = eligible for physical-device evidence, not `Verified`.
- Evidence-ready = eligible for manual review, not `Verified`.
- `READY_FOR_RELEASE_REVIEW` + successful P1 publication preflight = eligible for the release owner to publish manually, not automatic `Verified` state.
- Real-device/manual approvals + exact binary hash + successful publication + recorded release evidence are required before the repository pointer can say `Verified`.
- The GitHub Release asset is the distributed binary; APK files remain excluded from Git commits.
- `docs/releases/LAST_VERIFIED_APK.md` is the repository pointer and must always distinguish Unity from Flutter.
- Keep the previous verified GitHub Release available for rollback.
