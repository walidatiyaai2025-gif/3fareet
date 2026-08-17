#!/usr/bin/env python3
"""Reconcile a human P1 publication receipt against one exact P1 preflight.

This tool is intentionally post-publication and read-only with respect to repository/remote
release state. It validates a release owner's recorded publication claim against the exact
Step-22 P1 publication-preflight bytes and authorization-bound candidate lineage. It never
creates tags/releases, uploads assets, changes repository pointers, or marks an APK VERIFIED.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Any, Optional, Sequence

SCHEMA_VERSION = 1
RECEIPT_PROFILE = "p1-manual-publication-receipt-v1"
RESULT_STATE = "P1_HUMAN_PUBLICATION_RECEIPT_RECONCILED"
PREFLIGHT_STATE = "P1_PUBLICATION_PREFLIGHT_PASSED"
PREFLIGHT_VERDICT = "P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION"
AUTHORIZATION_KEYS = (
    "authorizationSourceGitSha",
    "handoffPacketSha256",
    "nativeHandoffVerificationSha256",
    "operatorChainSha256",
)
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")


class P1PublicationReceiptError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file() or path.is_symlink() or path.stat().st_size <= 0:
        raise P1PublicationReceiptError(f"{label} is missing, empty, or not a regular file: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1PublicationReceiptError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1PublicationReceiptError(f"{label} root must be a JSON object")
    return payload


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise P1PublicationReceiptError(message)


def _sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise P1PublicationReceiptError(f"{label} must be a full 40-character Git SHA, found {value!r}")
    return text


def _sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise P1PublicationReceiptError(f"{label} must be a SHA-256 hex digest, found {value!r}")
    return text


def _text(value: Any, label: str) -> str:
    text = str(value or "").strip()
    if not text:
        raise P1PublicationReceiptError(f"{label} must be non-empty")
    return text


def _https_url(value: Any, label: str) -> str:
    text = _text(value, label)
    if not text.startswith("https://"):
        raise P1PublicationReceiptError(f"{label} must be an https:// URL")
    return text


def _utc_timestamp(value: Any, label: str) -> str:
    text = _text(value, label)
    normalized = text[:-1] + "+00:00" if text.endswith("Z") else text
    try:
        parsed = datetime.fromisoformat(normalized)
    except ValueError as exc:
        raise P1PublicationReceiptError(f"{label} must be an ISO-8601 timestamp with timezone") from exc
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        raise P1PublicationReceiptError(f"{label} must include an explicit timezone")
    return text


def _authorization(value: Any, label: str, source_sha: str) -> dict[str, str]:
    if not isinstance(value, dict) or set(value) != set(AUTHORIZATION_KEYS):
        raise P1PublicationReceiptError(
            f"{label} must contain exactly the four staging authorization fingerprints"
        )
    normalized = {
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
    if normalized["authorizationSourceGitSha"] != source_sha:
        raise P1PublicationReceiptError(
            f"{label}.authorizationSourceGitSha differs from stagingSourceGitSha"
        )
    return normalized


def reconcile(preflight_path: Path, receipt_path: Path) -> dict[str, Any]:
    preflight_path = preflight_path.expanduser().resolve()
    receipt_path = receipt_path.expanduser().resolve()
    preflight = _read_json(preflight_path, "P1 publication preflight")
    receipt = _read_json(receipt_path, "Human P1 publication receipt")

    _require(preflight.get("schemaVersion") == 1, "P1 publication preflight schemaVersion must be 1")
    _require(preflight.get("state") == PREFLIGHT_STATE, f"P1 preflight state must be {PREFLIGHT_STATE}")
    _require(preflight.get("verdict") == PREFLIGHT_VERDICT, f"P1 preflight verdict must be {PREFLIGHT_VERDICT}")
    _require(
        preflight.get("eligibleForExplicitManualPublication") is True,
        "P1 preflight must explicitly allow manual publication",
    )
    _require(preflight.get("publicationPerformed") is False, "P1 preflight itself must not claim publication")
    _require(preflight.get("verified") is False, "P1 preflight must remain unverified")

    candidate = preflight.get("candidate")
    lineage = preflight.get("p1Lineage")
    evidence = preflight.get("evidence")
    _require(isinstance(candidate, dict), "P1 preflight candidate block is missing")
    _require(isinstance(lineage, dict), "P1 preflight p1Lineage block is missing")
    _require(isinstance(evidence, dict), "P1 preflight evidence block is missing")

    candidate_sha = _sha40(candidate.get("gitSha"), "preflight.candidate.gitSha")
    apk_sha = _sha256(candidate.get("apkSha256"), "preflight.candidate.apkSha256")
    staging_sha = _sha40(lineage.get("stagingSourceGitSha"), "preflight.p1Lineage.stagingSourceGitSha")
    review_content_sha = _sha256(
        lineage.get("reviewContentSetSha256"), "preflight.p1Lineage.reviewContentSetSha256"
    )
    review_lineage_sha = _sha256(
        lineage.get("p1ReviewLineageSha256"), "preflight.p1Lineage.p1ReviewLineageSha256"
    )
    preflight_authorization = _authorization(
        lineage.get("stagingAuthorization"), "preflight.p1Lineage.stagingAuthorization", staging_sha
    )
    preflight_sha = sha256_file(preflight_path)

    _require(receipt.get("schemaVersion") == SCHEMA_VERSION, "publication receipt schemaVersion must be 1")
    _require(receipt.get("receiptProfile") == RECEIPT_PROFILE, f"publication receipt profile must be {RECEIPT_PROFILE}")
    _require(receipt.get("publicationPerformed") is True, "receipt must explicitly record the human publication action")
    _require(receipt.get("verified") is False, "publication receipt must not self-assert VERIFIED state")

    receipt_preflight_sha = _sha256(
        receipt.get("publicationPreflightSha256"), "receipt.publicationPreflightSha256"
    )
    if receipt_preflight_sha != preflight_sha:
        raise P1PublicationReceiptError(
            f"publication preflight SHA-256 mismatch: receipt={receipt_preflight_sha} actual={preflight_sha}"
        )

    receipt_candidate_sha = _sha40(receipt.get("candidateGitSha"), "receipt.candidateGitSha")
    receipt_apk_sha = _sha256(receipt.get("apkSha256"), "receipt.apkSha256")
    receipt_published_apk_sha = _sha256(receipt.get("publishedApkSha256"), "receipt.publishedApkSha256")
    receipt_staging_sha = _sha40(receipt.get("stagingSourceGitSha"), "receipt.stagingSourceGitSha")
    receipt_review_content_sha = _sha256(
        receipt.get("reviewContentSetSha256"), "receipt.reviewContentSetSha256"
    )
    receipt_review_lineage_sha = _sha256(
        receipt.get("p1ReviewLineageSha256"), "receipt.p1ReviewLineageSha256"
    )
    receipt_authorization = _authorization(
        receipt.get("stagingAuthorization"), "receipt.stagingAuthorization", receipt_staging_sha
    )

    expected_pairs = (
        ("candidateGitSha", receipt_candidate_sha, candidate_sha),
        ("apkSha256", receipt_apk_sha, apk_sha),
        ("publishedApkSha256", receipt_published_apk_sha, apk_sha),
        ("stagingSourceGitSha", receipt_staging_sha, staging_sha),
        ("reviewContentSetSha256", receipt_review_content_sha, review_content_sha),
        ("p1ReviewLineageSha256", receipt_review_lineage_sha, review_lineage_sha),
    )
    for label, actual, expected in expected_pairs:
        if actual != expected:
            raise P1PublicationReceiptError(
                f"publication receipt {label} mismatch: receipt={actual} preflight={expected}"
            )
    if receipt_authorization != preflight_authorization:
        raise P1PublicationReceiptError("publication receipt stagingAuthorization differs from P1 preflight")

    release_owner = _text(receipt.get("releaseOwner"), "receipt.releaseOwner")
    published_at = _utc_timestamp(receipt.get("publishedAtUtc"), "receipt.publishedAtUtc")
    git_tag = _text(receipt.get("gitTag"), "receipt.gitTag")
    release_url = _https_url(receipt.get("githubReleaseUrl"), "receipt.githubReleaseUrl")
    asset_url = _https_url(receipt.get("apkAssetUrl"), "receipt.apkAssetUrl")

    return {
        "schemaVersion": 1,
        "state": RESULT_STATE,
        "receiptProfile": RECEIPT_PROFILE,
        "humanPublicationRecorded": True,
        "publicationPerformedByTool": False,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "publicationPreflightSha256": preflight_sha,
        "candidateGitSha": candidate_sha,
        "apkSha256": apk_sha,
        "stagingSourceGitSha": staging_sha,
        "reviewContentSetSha256": review_content_sha,
        "p1ReviewLineageSha256": review_lineage_sha,
        "stagingAuthorization": dict(preflight_authorization),
        "releaseOwner": release_owner,
        "publishedAtUtc": published_at,
        "gitTag": git_tag,
        "githubReleaseUrl": release_url,
        "apkAssetUrl": asset_url,
        "publishedApkSha256": apk_sha,
        "postPublicationSmokeRequired": True,
        "lastVerifiedPointerUpdateRequiresReviewedEvidence": True,
        "notes": [
            "This result reconciles a human-recorded publication claim to one exact P1 preflight; it does not independently perform publication.",
            "Post-publication smoke/performance evidence and reviewed Last Verified pointer updates remain required before repository status may say VERIFIED.",
        ],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--preflight", required=True, help="Exact Step-22 P1 publication-preflight JSON")
    parser.add_argument("--receipt", required=True, help="Human-produced P1 manual publication receipt JSON")
    parser.add_argument("--output", help="Optional reconciliation JSON output; never overwrites an existing file")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = reconcile(Path(args.preflight), Path(args.receipt))
        output: Path | None = None
        if args.output:
            output = Path(args.output).expanduser().resolve()
            if output.exists():
                raise P1PublicationReceiptError(f"refusing to overwrite existing reconciliation result: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_P1_PUBLICATION_RECEIPT_RECONCILED "
            f"gitSha={result['candidateGitSha']} apkSha256={result['apkSha256']} "
            f"preflightSha256={result['publicationPreflightSha256']} authorizationBound=true "
            "humanPublicationRecorded=true publicationPerformedByTool=false verified=false"
            + (f" output={output}" if output else "")
        )
        return 0
    except (P1PublicationReceiptError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"AFAREET_P1_PUBLICATION_RECEIPT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
