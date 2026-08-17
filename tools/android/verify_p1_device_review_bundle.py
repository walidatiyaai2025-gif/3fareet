#!/usr/bin/env python3
"""Verify a sanitized P1 device review bundle and its authorization-bound lineage.

The generic review-bundle verifier remains authoritative for content SHA-256, checkpoint/device
binding and privacy-safe file-set integrity. This P1 verifier additionally requires the exact
staging authorization fingerprints introduced by Step 20. It never grants approval.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any, Optional, Sequence

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import verify_device_review_bundle

P1_REVIEW_LINEAGE_FILE = "p1-review-lineage.json"
P1_REVIEW_STATE = "SANITIZED_P1_REVIEW_LINEAGE"
P1_REVIEW_PROFILE = "p1-final-gate-lineage-v2"
EXPECTED_TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
AUTHORIZATION_KEYS = (
    "authorizationSourceGitSha",
    "handoffPacketSha256",
    "nativeHandoffVerificationSha256",
    "operatorChainSha256",
)


class P1ReviewBundleVerificationError(RuntimeError):
    pass


def _read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file() or path.is_symlink():
        raise P1ReviewBundleVerificationError(f"{label} is missing or is not a regular file: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1ReviewBundleVerificationError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1ReviewBundleVerificationError(f"{label} root must be a JSON object")
    return payload


def _sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise P1ReviewBundleVerificationError(f"{label} must be a full 40-character Git SHA, found {value!r}")
    return text


def _sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise P1ReviewBundleVerificationError(f"{label} must be a SHA-256 hex digest, found {value!r}")
    return text


def _authorization(value: Any, label: str) -> dict[str, str]:
    if not isinstance(value, dict) or set(value) != set(AUTHORIZATION_KEYS):
        raise P1ReviewBundleVerificationError(
            f"{label} must contain exactly the four staging authorization fingerprints"
        )
    return {
        "authorizationSourceGitSha": _sha40(
            value.get("authorizationSourceGitSha"), f"{label}.authorizationSourceGitSha"
        ),
        "handoffPacketSha256": _sha256(
            value.get("handoffPacketSha256"), f"{label}.handoffPacketSha256"
        ),
        "nativeHandoffVerificationSha256": _sha256(
            value.get("nativeHandoffVerificationSha256"),
            f"{label}.nativeHandoffVerificationSha256",
        ),
        "operatorChainSha256": _sha256(
            value.get("operatorChainSha256"), f"{label}.operatorChainSha256"
        ),
    }


def _require_false(payload: dict[str, Any], key: str, label: str) -> None:
    if payload.get(key) is not False:
        raise P1ReviewBundleVerificationError(f"{label}.{key} must remain JSON false")


def _require_tasks(value: Any, label: str) -> list[str]:
    if not isinstance(value, list) or value != EXPECTED_TASKS:
        raise P1ReviewBundleVerificationError(f"{label} must equal the ordered six-task P1 scope: {EXPECTED_TASKS}")
    return list(value)


def verify_p1_bundle(
    bundle_dir: Path,
    *,
    expected_git_sha: str | None = None,
    expected_apk_sha: str | None = None,
    expected_staging_source_sha: str | None = None,
) -> dict[str, Any]:
    root = bundle_dir.expanduser().resolve()
    generic = verify_device_review_bundle.verify_bundle(
        root,
        expected_git_sha=expected_git_sha,
        expected_apk_sha=expected_apk_sha,
    )

    manifest = _read_json(root / verify_device_review_bundle.REVIEW_MANIFEST_FILE, "Review manifest")
    if manifest.get("reviewProfile") != P1_REVIEW_PROFILE:
        raise P1ReviewBundleVerificationError(f"Unexpected P1 reviewProfile: {manifest.get('reviewProfile')!r}")
    binding = manifest.get("p1Lineage")
    if not isinstance(binding, dict):
        raise P1ReviewBundleVerificationError("Review manifest is missing p1Lineage binding")
    if binding.get("schemaVersion") != 2 or binding.get("state") != "P1_REVIEW_LINEAGE_ATTACHED":
        raise P1ReviewBundleVerificationError("Review manifest p1Lineage must be schema 2 in attached state")
    if binding.get("fileName") != P1_REVIEW_LINEAGE_FILE:
        raise P1ReviewBundleVerificationError("Review manifest p1Lineage.fileName is not the canonical sanitized lineage file")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(binding, key, "reviewManifest.p1Lineage")
    _require_tasks(binding.get("coveredTasks"), "reviewManifest.p1Lineage.coveredTasks")
    binding_authorization = _authorization(
        binding.get("stagingAuthorization"), "reviewManifest.p1Lineage.stagingAuthorization"
    )

    summary_path = root / P1_REVIEW_LINEAGE_FILE
    summary_hash = verify_device_review_bundle.sha256_file(summary_path)
    if summary_hash != _sha256(binding.get("sha256"), "reviewManifest.p1Lineage.sha256"):
        raise P1ReviewBundleVerificationError("Sanitized P1 lineage SHA-256 differs from review-manifest binding")
    content_files = manifest.get("contentFiles")
    if not isinstance(content_files, dict) or P1_REVIEW_LINEAGE_FILE not in content_files:
        raise P1ReviewBundleVerificationError("Sanitized P1 lineage is not part of review contentFiles")
    content_record = content_files[P1_REVIEW_LINEAGE_FILE]
    if not isinstance(content_record, dict) or _sha256(content_record.get("sha256"), "contentFiles.p1Review.sha256") != summary_hash:
        raise P1ReviewBundleVerificationError("Review contentFiles does not bind the sanitized P1 lineage bytes")

    summary = _read_json(summary_path, "Sanitized P1 review lineage")
    if summary.get("schemaVersion") != 2 or summary.get("state") != P1_REVIEW_STATE:
        raise P1ReviewBundleVerificationError("Sanitized P1 review lineage must be schema 2 in the canonical state")
    if summary.get("reviewProfile") != P1_REVIEW_PROFILE:
        raise P1ReviewBundleVerificationError("Sanitized P1 review lineage profile mismatch")
    if summary.get("verdict") != verify_device_review_bundle.EXPECTED_REVIEW_VERDICT:
        raise P1ReviewBundleVerificationError("Sanitized P1 review lineage must remain MANUAL_REVIEW_REQUIRED")
    if summary.get("manualReviewRequired") is not True:
        raise P1ReviewBundleVerificationError("Sanitized P1 review lineage must require manual review")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(summary, key, "p1ReviewLineage")
    _require_tasks(summary.get("coveredTasks"), "p1ReviewLineage.coveredTasks")
    summary_authorization = _authorization(
        summary.get("stagingAuthorization"), "p1ReviewLineage.stagingAuthorization"
    )
    if summary_authorization != binding_authorization:
        raise P1ReviewBundleVerificationError(
            "P1 staging authorization differs between review manifest and sanitized lineage"
        )

    staging_source_sha = _sha40(summary.get("stagingSourceGitSha"), "p1ReviewLineage.stagingSourceGitSha")
    candidate_sha = _sha40(summary.get("candidateGitSha"), "p1ReviewLineage.candidateGitSha")
    direct_parent_sha = _sha40(summary.get("directParentGitSha"), "p1ReviewLineage.directParentGitSha")
    apk_sha = _sha256(summary.get("apkSha256"), "p1ReviewLineage.apkSha256")
    if summary_authorization["authorizationSourceGitSha"] != staging_source_sha:
        raise P1ReviewBundleVerificationError(
            "P1 review authorization source SHA differs from staging-source SHA"
        )
    if candidate_sha == staging_source_sha:
        raise P1ReviewBundleVerificationError("Sanitized P1 lineage candidate SHA must differ from staging-source SHA")
    if direct_parent_sha != staging_source_sha:
        raise P1ReviewBundleVerificationError("Sanitized P1 lineage direct parent must equal staging-source SHA")
    if candidate_sha != generic["gitSha"] or candidate_sha != _sha40(binding.get("candidateGitSha"), "reviewManifest.p1Lineage.candidateGitSha"):
        raise P1ReviewBundleVerificationError("P1 review candidate SHA does not match the generic verified review candidate")
    if apk_sha != generic["apkSha256"] or apk_sha != _sha256(binding.get("apkSha256"), "reviewManifest.p1Lineage.apkSha256"):
        raise P1ReviewBundleVerificationError("P1 review APK SHA does not match the generic verified review candidate")
    if staging_source_sha != _sha40(binding.get("stagingSourceGitSha"), "reviewManifest.p1Lineage.stagingSourceGitSha"):
        raise P1ReviewBundleVerificationError("P1 review staging-source SHA differs between manifest and sanitized lineage")
    if direct_parent_sha != _sha40(binding.get("directParentGitSha"), "reviewManifest.p1Lineage.directParentGitSha"):
        raise P1ReviewBundleVerificationError("P1 review direct-parent SHA differs between manifest and sanitized lineage")

    if expected_staging_source_sha is not None and _sha40(expected_staging_source_sha, "--expected-staging-source-sha") != staging_source_sha:
        raise P1ReviewBundleVerificationError(
            f"P1 staging-source SHA mismatch: expected={expected_staging_source_sha.lower()} actual={staging_source_sha}"
        )

    tier = str(summary.get("performanceTier") or "").strip().lower()
    if tier not in {"low", "mid", "high"}:
        raise P1ReviewBundleVerificationError(f"Invalid P1 performanceTier: {tier!r}")
    if tier != str(binding.get("performanceTier") or "").strip().lower():
        raise P1ReviewBundleVerificationError("P1 performanceTier differs between review manifest and sanitized lineage")
    if _sha256(summary.get("deviceSerialSha256"), "p1ReviewLineage.deviceSerialSha256") != generic["deviceSerialSha256"]:
        raise P1ReviewBundleVerificationError("P1 review device hash differs from generic verified bundle")
    if summary.get("checkpointCount") != generic["checkpointCount"]:
        raise P1ReviewBundleVerificationError("P1 checkpointCount differs from generic verified bundle")
    if summary.get("contentReviewVerdict") != generic["verdict"]:
        raise P1ReviewBundleVerificationError("P1 contentReviewVerdict differs from generic verified bundle")

    source_digests = summary.get("sourceArtifactDigests")
    if not isinstance(source_digests, dict) or set(source_digests) != {
        "p1Manifest",
        "stagingReport",
        "stagingLineage",
        "candidateManifest",
    }:
        raise P1ReviewBundleVerificationError("P1 sourceArtifactDigests must contain exactly four provenance digests")
    normalized_digests = {key: _sha256(value, f"p1ReviewLineage.sourceArtifactDigests.{key}") for key, value in source_digests.items()}
    candidate_manifest_sha = _sha256(
        manifest.get("candidate", {}).get("candidateManifestSha256")
        if isinstance(manifest.get("candidate"), dict)
        else None,
        "reviewManifest.candidate.candidateManifestSha256",
    )
    if normalized_digests["candidateManifest"] != candidate_manifest_sha:
        raise P1ReviewBundleVerificationError("P1 source candidate-manifest digest differs from generic review candidate binding")

    privacy = summary.get("privacy")
    if not isinstance(privacy, dict):
        raise P1ReviewBundleVerificationError("Sanitized P1 lineage is missing privacy contract")
    for key in ("rawP1SessionIncluded", "rawP1SourceArtifactsIncluded", "localPathsIncluded"):
        if privacy.get(key) is not False:
            raise P1ReviewBundleVerificationError(f"P1 privacy flag {key} must be false")
    if privacy.get("authorizationContainsOnlyDigests") is not True:
        raise P1ReviewBundleVerificationError("P1 review authorization must declare digest-only privacy")
    manifest_privacy = manifest.get("privacy")
    if not isinstance(manifest_privacy, dict):
        raise P1ReviewBundleVerificationError("Review manifest is missing privacy contract")
    if manifest_privacy.get("rawP1SessionIncluded") is not False or manifest_privacy.get("rawP1SourceArtifactsIncluded") is not False:
        raise P1ReviewBundleVerificationError("Review manifest must exclude raw P1 session/source artifacts")
    if manifest_privacy.get("sanitizedP1LineageIncluded") is not True:
        raise P1ReviewBundleVerificationError("Review manifest must declare sanitizedP1LineageIncluded=true")
    if manifest_privacy.get("authorizationContainsOnlyDigests") is not True:
        raise P1ReviewBundleVerificationError("Review manifest must declare digest-only authorization binding")

    return {
        "stagingSourceGitSha": staging_source_sha,
        "candidateGitSha": candidate_sha,
        "directParentGitSha": direct_parent_sha,
        "apkSha256": apk_sha,
        "performanceTier": tier,
        "coveredTasks": list(EXPECTED_TASKS),
        "checkpointCount": generic["checkpointCount"],
        "contentSetSha256": generic["contentSetSha256"],
        "p1ReviewLineageSha256": summary_hash,
        "sourceArtifactDigests": normalized_digests,
        "stagingAuthorization": dict(summary_authorization),
        "verdict": generic["verdict"],
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--bundle", required=True, help="Sanitized P1 review bundle directory")
    parser.add_argument("--expected-git-sha", help="Optional exact candidate Git SHA")
    parser.add_argument("--expected-apk-sha", help="Optional exact candidate APK SHA-256")
    parser.add_argument("--expected-staging-source-sha", help="Optional exact staging-source Git SHA")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_p1_bundle(
            Path(args.bundle),
            expected_git_sha=args.expected_git_sha,
            expected_apk_sha=args.expected_apk_sha,
            expected_staging_source_sha=args.expected_staging_source_sha,
        )
        print(
            "AFAREET_P1_DEVICE_REVIEW_BUNDLE_VERIFIED "
            f"stagingSourceGitSha={result['stagingSourceGitSha']} candidateGitSha={result['candidateGitSha']} "
            f"apkSha256={result['apkSha256']} checkpoints={result['checkpointCount']} "
            f"contentSetSha256={result['contentSetSha256']} tasks=6 authorizationBound=true "
            f"verdict={result['verdict']} verified=false"
        )
        return 0
    except (
        P1ReviewBundleVerificationError,
        verify_device_review_bundle.ReviewBundleVerificationError,
        OSError,
        ValueError,
    ) as exc:
        print(f"AFAREET_P1_DEVICE_REVIEW_VERIFY_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
