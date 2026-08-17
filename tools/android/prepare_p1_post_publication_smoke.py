#!/usr/bin/env python3
"""Prepare a physical-device smoke session from one reconciled P1 publication.

The release action remains human-only. This wrapper validates the Step-23 publication
reconciliation and the exact downloaded published APK bytes, then delegates installation
and device discovery to the existing ``device_evidence`` collector. Before any smoke
checkpoint is captured it binds the session to one performance tier and the exact
publication/authorization lineage. It never publishes, approves, or marks anything VERIFIED.
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

EXPECTED_PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
RECEIPT_STATE = "P1_HUMAN_PUBLICATION_RECEIPT_RECONCILED"
RECEIPT_PROFILE = "p1-manual-publication-receipt-v1"
SESSION_PROFILE = "p1-post-publication-smoke-session-v1"
PERFORMANCE_TIERS = {"low", "mid", "high"}
AUTHORIZATION_KEYS = (
    "authorizationSourceGitSha",
    "handoffPacketSha256",
    "nativeHandoffVerificationSha256",
    "operatorChainSha256",
)
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")


class P1PostPublicationPrepareError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file() or path.is_symlink() or path.stat().st_size <= 0:
        raise P1PostPublicationPrepareError(f"{label} is missing, empty, symlinked, or not a regular file: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1PostPublicationPrepareError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1PostPublicationPrepareError(f"{label} root must be a JSON object")
    return payload


def require(condition: bool, message: str) -> None:
    if not condition:
        raise P1PostPublicationPrepareError(message)


def normalize_sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise P1PostPublicationPrepareError(f"{label} must be a full 40-character Git SHA")
    return text


def normalize_sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise P1PostPublicationPrepareError(f"{label} must be a SHA-256 hex digest")
    return text


def normalize_text(value: Any, label: str) -> str:
    text = str(value or "").strip()
    if not text:
        raise P1PostPublicationPrepareError(f"{label} must be non-empty")
    return text


def parse_timestamp(value: Any, label: str) -> datetime:
    text = normalize_text(value, label)
    normalized = text[:-1] + "+00:00" if text.endswith("Z") else text
    try:
        parsed = datetime.fromisoformat(normalized)
    except ValueError as exc:
        raise P1PostPublicationPrepareError(f"{label} must be an ISO-8601 timestamp with timezone") from exc
    if parsed.tzinfo is None or parsed.utcoffset() is None:
        raise P1PostPublicationPrepareError(f"{label} must include an explicit timezone")
    return parsed


def normalize_authorization(value: Any, source_sha: str) -> dict[str, str]:
    if not isinstance(value, dict) or set(value) != set(AUTHORIZATION_KEYS):
        raise P1PostPublicationPrepareError(
            "receipt reconciliation stagingAuthorization must contain exactly four fingerprints"
        )
    authorization = {
        "authorizationSourceGitSha": normalize_sha40(
            value.get("authorizationSourceGitSha"), "stagingAuthorization.authorizationSourceGitSha"
        ),
        "handoffPacketSha256": normalize_sha256(
            value.get("handoffPacketSha256"), "stagingAuthorization.handoffPacketSha256"
        ),
        "nativeHandoffVerificationSha256": normalize_sha256(
            value.get("nativeHandoffVerificationSha256"),
            "stagingAuthorization.nativeHandoffVerificationSha256",
        ),
        "operatorChainSha256": normalize_sha256(
            value.get("operatorChainSha256"), "stagingAuthorization.operatorChainSha256"
        ),
    }
    require(
        authorization["authorizationSourceGitSha"] == source_sha,
        "stagingAuthorization source Git SHA differs from stagingSourceGitSha",
    )
    return authorization


def validate_reconciliation(path: Path) -> dict[str, Any]:
    path = path.expanduser().resolve()
    payload = read_json(path, "P1 publication receipt reconciliation")
    require(payload.get("schemaVersion") == 1, "receipt reconciliation schemaVersion must be 1")
    require(payload.get("state") == RECEIPT_STATE, f"receipt reconciliation state must be {RECEIPT_STATE}")
    require(payload.get("receiptProfile") == RECEIPT_PROFILE, f"receipt profile must be {RECEIPT_PROFILE}")
    require(payload.get("humanPublicationRecorded") is True, "human publication must be recorded before smoke")
    require(payload.get("publicationPerformedByTool") is False, "receipt reconciliation tool must not claim publication")
    for field in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        require(payload.get(field) is False, f"receipt reconciliation {field} must remain false")
    require(payload.get("postPublicationSmokeRequired") is True, "receipt must require post-publication smoke")

    candidate_sha = normalize_sha40(payload.get("candidateGitSha"), "candidateGitSha")
    apk_sha = normalize_sha256(payload.get("apkSha256"), "apkSha256")
    published_apk_sha = normalize_sha256(payload.get("publishedApkSha256"), "publishedApkSha256")
    require(apk_sha == published_apk_sha, "published APK SHA-256 differs from tested candidate APK SHA-256")
    staging_sha = normalize_sha40(payload.get("stagingSourceGitSha"), "stagingSourceGitSha")
    authorization = normalize_authorization(payload.get("stagingAuthorization"), staging_sha)
    published_at_text = normalize_text(payload.get("publishedAtUtc"), "publishedAtUtc")
    parse_timestamp(published_at_text, "publishedAtUtc")

    return {
        "reconciliationSha256": sha256_file(path),
        "publicationPreflightSha256": normalize_sha256(
            payload.get("publicationPreflightSha256"), "publicationPreflightSha256"
        ),
        "candidateGitSha": candidate_sha,
        "apkSha256": apk_sha,
        "publishedApkSha256": published_apk_sha,
        "stagingSourceGitSha": staging_sha,
        "reviewContentSetSha256": normalize_sha256(
            payload.get("reviewContentSetSha256"), "reviewContentSetSha256"
        ),
        "p1ReviewLineageSha256": normalize_sha256(
            payload.get("p1ReviewLineageSha256"), "p1ReviewLineageSha256"
        ),
        "stagingAuthorization": authorization,
        "releaseOwner": normalize_text(payload.get("releaseOwner"), "releaseOwner"),
        "publishedAtUtc": published_at_text,
        "gitTag": normalize_text(payload.get("gitTag"), "gitTag"),
        "githubReleaseUrl": normalize_text(payload.get("githubReleaseUrl"), "githubReleaseUrl"),
        "apkAssetUrl": normalize_text(payload.get("apkAssetUrl"), "apkAssetUrl"),
    }


def validate_published_apk(path: Path, expected_sha: str) -> dict[str, Any]:
    path = path.expanduser().resolve()
    if not path.is_file() or path.is_symlink() or path.stat().st_size <= 0:
        raise P1PostPublicationPrepareError(f"published APK is missing, empty, symlinked, or not a regular file: {path}")
    actual_sha = sha256_file(path)
    if actual_sha != expected_sha:
        raise P1PostPublicationPrepareError(
            f"published APK SHA-256 mismatch: receipt={expected_sha} actual={actual_sha}"
        )
    return {"path": path, "sha256": actual_sha, "sizeBytes": path.stat().st_size}


def bind_session(
    reconciliation: dict[str, Any],
    apk: dict[str, Any],
    output_dir: Path,
    performance_tier: str,
    device_evidence: Any,
) -> dict[str, Any]:
    tier = str(performance_tier or "").strip().lower()
    if tier not in PERFORMANCE_TIERS:
        raise P1PostPublicationPrepareError(f"unsupported performance tier: {performance_tier!r}")

    session_path = output_dir / "session.json"
    session = read_json(session_path, "prepared device session")
    require(session.get("schemaVersion") == 1, "prepared device session schemaVersion must be 1")
    require(session.get("state") == "PREPARED", "prepared device session state must be PREPARED")
    require(session.get("verdict") == "MANUAL_REVIEW_REQUIRED", "prepared device session verdict is invalid")
    require(session.get("packageId") == EXPECTED_PACKAGE_ID, "prepared device session packageId mismatch")

    session_apk = session.get("apk")
    require(isinstance(session_apk, dict), "prepared device session is missing APK metadata")
    require(
        str(session_apk.get("sha256") or "").strip().lower() == apk["sha256"],
        "prepared device session APK SHA-256 differs from published APK",
    )
    try:
        session_size = int(session_apk.get("sizeBytes", -1))
    except (TypeError, ValueError) as exc:
        raise P1PostPublicationPrepareError("prepared device session APK sizeBytes is invalid") from exc
    require(session_size == apk["sizeBytes"], "prepared device session APK size differs from published APK")

    device = session.get("device")
    require(isinstance(device, dict), "prepared device session is missing device metadata")
    require(device.get("isEmulator") is False, "post-publication P1 smoke requires a physical Android device")
    normalize_sha256(device.get("serialSha256"), "device.serialSha256")
    require(
        parse_timestamp(session.get("createdAtUtc"), "session.createdAtUtc")
        >= parse_timestamp(reconciliation["publishedAtUtc"], "publishedAtUtc"),
        "post-publication device session was created before the recorded publication time",
    )

    context = {
        "schemaVersion": 1,
        "profile": SESSION_PROFILE,
        "publicationReceiptReconciliationSha256": reconciliation["reconciliationSha256"],
        "publicationPreflightSha256": reconciliation["publicationPreflightSha256"],
        "candidateGitSha": reconciliation["candidateGitSha"],
        "apkSha256": reconciliation["apkSha256"],
        "publishedApkSha256": reconciliation["publishedApkSha256"],
        "stagingSourceGitSha": reconciliation["stagingSourceGitSha"],
        "reviewContentSetSha256": reconciliation["reviewContentSetSha256"],
        "p1ReviewLineageSha256": reconciliation["p1ReviewLineageSha256"],
        "stagingAuthorization": dict(reconciliation["stagingAuthorization"]),
        "releaseOwner": reconciliation["releaseOwner"],
        "publishedAtUtc": reconciliation["publishedAtUtc"],
        "gitTag": reconciliation["gitTag"],
        "githubReleaseUrl": reconciliation["githubReleaseUrl"],
        "apkAssetUrl": reconciliation["apkAssetUrl"],
        "humanPublicationRecorded": True,
        "publicationPerformedByTool": False,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
    }
    session["performanceTier"] = tier
    session["p1PostPublication"] = context
    device_evidence.write_json(session_path, session)
    return context


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--receipt-reconciliation", required=True, help="Exact Step-23 reconciliation JSON")
    parser.add_argument("--apk", required=True, help="Downloaded published APK bytes; SHA must match the receipt")
    parser.add_argument("--output", required=True, help="Fresh output directory for post-publication device evidence")
    parser.add_argument("--serial", help="ADB serial when more than one authorized physical device is connected")
    parser.add_argument("--performance-tier", required=True, choices=("low", "mid", "high"))
    parser.add_argument("--force", action="store_true", help="Replace an existing evidence session")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        reconciliation_path = Path(args.receipt_reconciliation).expanduser().resolve()
        reconciliation = validate_reconciliation(reconciliation_path)
        apk = validate_published_apk(Path(args.apk), reconciliation["publishedApkSha256"])
        output_dir = Path(args.output).expanduser().resolve()

        print(
            "AFAREET_P1_POST_PUBLICATION_PRECHECK_OK "
            f"gitSha={reconciliation['candidateGitSha']} apkSha256={apk['sha256']} "
            f"performanceTier={args.performance_tier.upper()} receiptSha256={reconciliation['reconciliationSha256']}"
        )

        import device_evidence  # Reuse the established ADB collector only after all byte/receipt checks pass.

        child_args = ["prepare", "--apk", str(apk["path"]), "--output", str(output_dir)]
        if args.serial:
            child_args.extend(["--serial", args.serial])
        if args.force:
            child_args.append("--force")
        code = int(device_evidence.main(child_args))
        if code != 0:
            return code

        context = bind_session(reconciliation, apk, output_dir, args.performance_tier, device_evidence)
        print(
            "AFAREET_P1_POST_PUBLICATION_SESSION_BOUND "
            f"profile={context['profile']} apkSha256={context['publishedApkSha256']} "
            f"receiptSha256={context['publicationReceiptReconciliationSha256']} "
            "publicationPerformedByTool=false verified=false"
        )
        return 0
    except (P1PostPublicationPrepareError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"AFAREET_P1_POST_PUBLICATION_PREPARE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
