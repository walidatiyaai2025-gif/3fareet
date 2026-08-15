# Last Verified Unity APK

> This file is the single source of truth. A successful build is not verification.

## Current status

**No Unity APK has completed the required real-device verification and manual publication preflight yet.**

The current Unity APKs are `Latest Built` / candidate artifacts only. Do not publish, link,
rename, or record one as `Last Verified APK` until every gate below is complete on one exact
candidate/evidence chain.

## Required record for the first verified APK

Replace the status above with all of these fields:

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
- Review bundle `contentSetSha256`:
- Manual approvals file SHA-256:
- Publication preflight result / evidence URL:
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

## Promotion procedure

1. Produce an integrity-checked release candidate from the reviewed exact commit.
2. Bind that exact APK to a physical-device session and capture every checkpoint from `tools/android/p1_gate_spec.json`.
3. Finish the evidence session and confirm zero automated Fatal/ANR/native-fatal red flags.
4. Export the privacy-safe content-addressed review bundle and verify it offline for the exact Git SHA + APK SHA-256.
5. Generate a schema-v2 approval template from the verified evidence; Gameplay/QA/Art reviewers approve the exact bundle they inspected.
6. Obtain explicit `UPER-010` release-owner approval in the same schema-v2 file.
7. Run the fail-closed publication preflight:

```bash
python3 tools/android/verify_release_publication.py \
  --candidate-manifest /path/to/local-or-ci-candidate-manifest.json \
  --apk /path/to/afareet-unity3d-debug.apk \
  --session evidence/p1-device \
  --review-bundle evidence/p1-review \
  --approvals evidence/manual-approvals.json \
  --output evidence/publication-preflight.json
```

8. Require `ELIGIBLE_FOR_MANUAL_PUBLICATION` with `verified=false`. Any other result blocks publication.
9. Only the release owner creates the GitHub Release tag such as `unity-verified-v0.1.0-build.1`.
10. Upload the **same tested APK bytes** as `afareet-unity3d-last-verified.apk`; record the pre-rename SHA-256 and confirm the uploaded bytes retain that SHA-256.
11. Update this file and `docs/PROJECT_STATUS.md` in a reviewed PR with all candidate/evidence/reviewer/publication identifiers above.
12. Keep the previous verified release published for rollback.

If any gate fails, evidence changes, the candidate changes, or publication preflight fails, record the candidate only as Built/Failed/Review evidence without changing this pointer.
