# Unity Production + Flutter Legacy Release Policy

## Artifact identity

- Unity Android debug: `afareet-unity3d-debug.apk`.
- Unity release candidate: `afareet-unity3d-v<version>-rc<build>.apk`.
- Unity verified release: `afareet-unity3d-v<version>-verified.apk`.
- Flutter reference: `afareet-flutter-debug.apk` أو `afareet-flutter-release-skeleton.apk`؛ لا يوضع في مجلد Verified المنتج.

## Tag format

`unity-prototype-vMAJOR.MINOR.PATCH+BUILD`

Example: `unity-prototype-v0.1.0+1`.

## Required gates before a prototype tag

1. PR merged to `main` with all required checks Green.
2. Unity headless compile/tests Green on the exact release commit.
3. Unity Android build/candidate integrity Green on that same commit and APK SHA-256.
4. Candidate-bound physical-device checkpoints complete with zero automated Fatal/ANR/native-fatal red flags.
5. Privacy-safe content-addressed review bundle verifies for the exact candidate Git SHA + APK SHA-256.
6. Schema-v2 Gameplay/QA/Art approvals and the explicit `UPER-010` release-owner approval match that exact review bundle `contentSetSha256`.
7. `tools/android/verify_release_publication.py` returns `ELIGIBLE_FOR_MANUAL_PUBLICATION` for the exact candidate/APK/session/review/approvals chain.
8. `docs/PROJECT_STATUS.md` updated with the release state.
9. Only then create the GitHub Release/tag and upload the exact tested APK as `afareet-unity3d-last-verified.apk`.
10. Update `docs/releases/LAST_VERIFIED_APK.md` with release/asset links, commit, APK SHA-256, review `contentSetSha256`, device and evidence.
11. Never move the pointer for a merely built, CI-only, emulator-only, stale-evidence, failed-preflight or unapproved candidate.

Flutter checks تبقى مطلوبة فقط إذا لمس PR مسار Flutter. CI artifacts هي preview/build evidence وليست تلقائيًا Verified APKs.

## Verified publication preflight

The publication preflight is intentionally a **read-only eligibility check**. It never creates tags/releases, uploads assets, renames APKs, updates the Last Verified pointer, or sets `verified=true`.

Run it only after the exact candidate has completed the final-five physical-device/manual-review flow:

```bash
python3 tools/android/verify_release_publication.py \
  --candidate-manifest /path/to/local-or-ci-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/manual-approvals.json \
  --output evidence/publication-preflight.json
```

The preflight independently rechecks:

- candidate manifest contract and exact APK bytes/size/SHA-256;
- candidate manifest bytes equal the manifest bound into the device session;
- device session candidate Git/APK identity;
- content-addressed review bundle for that same Git/APK candidate;
- schema-v2 approval binding to the verified review `contentSetSha256`;
- physical-device evidence, zero automated red flags and all four manual gates;
- explicit `UPER-010` release-owner approval;
- final readiness is `READY_FOR_RELEASE_REVIEW` while all automation remains `verified=false`.

A successful marker is:

`AFAREET_RELEASE_PUBLICATION_PREFLIGHT_OK ... verdict=ELIGIBLE_FOR_MANUAL_PUBLICATION verified=false`

If any input changes after review—candidate manifest, APK bytes, evidence bundle, approval fingerprint or reviewer decision—the preflight must be rerun and must fail until the chain is consistent again.

## Last Verified invariants

- Build success = `Latest Built`, not `Verified`.
- Candidate-ready = eligible for physical-device evidence, not `Verified`.
- Evidence-ready = eligible for manual review, not `Verified`.
- `READY_FOR_RELEASE_REVIEW` + successful publication preflight = eligible for the release owner to publish manually, not automatic `Verified` state.
- Real-device/manual approvals + exact binary hash + successful publication + recorded release evidence are required before the repository pointer can say `Verified`.
- The GitHub Release asset is the distributed binary; APK files remain excluded from Git commits.
- `docs/releases/LAST_VERIFIED_APK.md` is the repository pointer and must always distinguish Unity from Flutter.
- Keep the previous verified GitHub Release available for rollback.
