# Last Verified Unity APK

> This file is the single source of truth. A successful build is not verification.

## Current status

**No Unity APK has completed the required real-device verification, authorization-bound manual review, human publication, and post-publication closure yet.**

The current Unity APKs are `Latest Built` / candidate artifacts only. Do not publish, link,
rename, or record one as `Last Verified APK` until every gate below is complete on one exact
P1 candidate/evidence/authorization chain.

## Required record for the first verified APK

Replace the status above with all of these fields only after the human publication and required
post-publication smoke/closure evidence are complete and reviewed:

- Status: `DEVICE VERIFIED`
- Product: `Unity 3D` (never confuse with Flutter)
- Version name / version code:
- Git tag:
- Commit SHA:
- GitHub Release URL:
- Direct APK asset URL:
- APK filename: `afareet-unity3d-last-verified.apk`
- APK SHA-256:
- Candidate manifest SHA-256:
- P1 review profile: `p1-final-gate-lineage-v2`
- Review bundle `contentSetSha256`:
- P1 review-lineage SHA-256:
- P1 approval profile: `p1-lineage-manual-approvals-v2`
- Manual approvals file SHA-256:
- Staging authorization source Git SHA:
- Handoff packet SHA-256:
- Native handoff-verification SHA-256:
- Operator-chain SHA-256:
- P1 publication-preflight SHA-256:
- P1 human publication-receipt SHA-256:
- Publication receipt reconciliation evidence URL:
- Unity version:
- Package ID:
- Build type and ABI:
- Verification date/time and timezone:
- Tester:
- Device model:
- Android version / API:
- Smoke/performance result:
- Gameplay/race approval reviewers:
- Visual approval reviewer:
- Release owner:
- Screenshot/video/review-bundle evidence URLs:
- Known issues:

## P1 promotion procedure

1. Produce the integrity-checked **local licensed-Windows P1 candidate** from the reviewed exact commit and preserve the exact candidate manifest + APK SHA-256.
2. Bind that exact P1 staged candidate to a physical-device session and capture every checkpoint from `tools/android/p1_gate_spec.json`.
3. Finish the physical-device evidence session and confirm zero automated Fatal/ANR/native-fatal red flags.
4. Export the P1 privacy-safe review bundle with `tools/android/export_p1_device_evidence.py` and verify it with `tools/android/verify_p1_device_review_bundle.py`. Require review profile `p1-final-gate-lineage-v2`; the review must carry the same four staging-authorization fingerprints as the raw P1 session while exposing no raw local paths/source artifacts.
5. Generate the P1 manual approval template with `tools/android/p1_lineage_gate_readiness.py approval-template`. Require approval profile `p1-lineage-manual-approvals-v2`; Gameplay/QA/Art reviewers approve the exact candidate/APK/review/source-digest/staging-authorization chain they inspected.
6. Obtain explicit `UPER-010` release-owner approval in that same P1 approval file, then run `tools/android/p1_lineage_gate_readiness.py validate` and require `READY_FOR_RELEASE_REVIEW` while `verified=false` and `publicationEligible=false`.
7. Run the authoritative fail-closed P1 publication preflight:

```bash
python3 tools/android/verify_p1_release_publication.py \
  --candidate-manifest /path/to/local-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/p1-lineage-approvals.json \
  --output evidence/p1-publication-preflight.json
```

8. Require `P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION` with `publicationPerformed=false` and `verified=false`. Generic `verify_release_publication.py` / generic approval files are **not sufficient for P1**.
9. Only the release owner performs the publication action: create the GitHub Release/tag (for example `unity-verified-v0.1.0-build.1`) and upload the **same tested APK bytes** as `afareet-unity3d-last-verified.apk`. Confirm the published asset retains the exact tested APK SHA-256.
10. The release owner records a human publication receipt JSON using receipt profile `p1-manual-publication-receipt-v1`. It must bind the exact P1 preflight SHA-256, candidate Git SHA, APK SHA-256, review content-set SHA-256, P1 review-lineage SHA-256, all four staging-authorization fingerprints, release owner, publication timestamp, Git tag, release URL, asset URL, and published APK SHA-256. The receipt may record `publicationPerformed=true` as a **human attestation**, but it must keep `verified=false`.
11. Reconcile that receipt without mutating release state:

```bash
python3 tools/android/verify_p1_publication_receipt.py \
  --preflight evidence/p1-publication-preflight.json \
  --receipt evidence/p1-publication-receipt.json \
  --output evidence/p1-publication-receipt-reconciled.json
```

Require `AFAREET_P1_PUBLICATION_RECEIPT_RECONCILED ... humanPublicationRecorded=true publicationPerformedByTool=false verified=false`. This validator does not create tags/releases, upload assets, or update repository pointers.
12. Run and review the required **post-publication physical-device smoke/performance closure** on the published bytes. A reconciled publication receipt alone is not `DEVICE VERIFIED`.
13. Only after publication + reconciled receipt + post-publication smoke/closure are reviewed, update this file and `docs/PROJECT_STATUS.md` in a reviewed PR with all candidate/evidence/authorization/reviewer/publication identifiers above.
14. Keep the previous verified release published for rollback.

If any gate fails, evidence changes, authorization fingerprints change, the candidate changes, publication preflight fails, the published asset hash differs, receipt reconciliation fails, or post-publication smoke fails, record the artifact only as Built/Failed/Review/Publication evidence without changing this pointer to `DEVICE VERIFIED`.

## Non-P1 compatibility

`tools/android/verify_release_publication.py` remains available for legacy/non-P1 compatibility only. It must never substitute for the authorization-bound P1 path above.
