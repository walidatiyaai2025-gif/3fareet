#!/usr/bin/env python3
"""Verify integrity and candidate binding of a sanitized device review bundle.

This verifier is intentionally offline and uses only the Python standard
library. It proves that the review files still match the SHA-256/size records
created by export_device_evidence.py and that the bundle remains bound to one
candidate/device evidence index. It does not approve any manual P1 gate.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path, PurePosixPath
from typing import Any

PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
REVIEW_MANIFEST_FILE = "review-manifest.json"
INDEX_FILE = "evidence-index.json"
EXPECTED_SCHEMA_VERSION = 2
EXPECTED_STATE = "SANITIZED_REVIEW_BUNDLE"
EXPECTED_REVIEW_VERDICT = "MANUAL_REVIEW_REQUIRED"
EXPECTED_CANDIDATE_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
ALLOWED_CANDIDATE_TYPES = {
    "local-windows-licensed-unity",
    "github-actions-unity-ci",
}
SAFE_CHECKPOINT_PAYLOAD_FILES = (
    "screen.png",
    "meminfo.txt",
    "gfxinfo.txt",
    "thermalservice.txt",
    "battery.txt",
)
FORBIDDEN_BASENAMES = {
    "session.json",
    "candidate-manifest.json",
    "package-dump.txt",
    "logcat.txt",
    "activity.txt",
}
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")


class ReviewBundleVerificationError(RuntimeError):
    pass


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file() or path.is_symlink():
        raise ReviewBundleVerificationError(f"Required regular JSON file is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise ReviewBundleVerificationError(f"Invalid JSON file {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise ReviewBundleVerificationError(f"JSON root must be an object: {path}")
    return payload


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def require_sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise ReviewBundleVerificationError(f"{label} must be a full SHA-256 value, found {value!r}")
    return text


def require_sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise ReviewBundleVerificationError(f"{label} must be a full 40-character Git SHA, found {value!r}")
    return text


def safe_relative_path(value: Any) -> str:
    text = str(value or "").strip().replace("\\", "/")
    path = PurePosixPath(text)
    if (
        not text
        or path.is_absolute()
        or text.startswith("/")
        or any(part in {"", ".", ".."} for part in path.parts)
        or path.name in FORBIDDEN_BASENAMES
        or text == REVIEW_MANIFEST_FILE
    ):
        raise ReviewBundleVerificationError(f"Unsafe or forbidden review-bundle path: {value!r}")
    return path.as_posix()


def content_set_sha256(records: dict[str, dict[str, Any]]) -> str:
    canonical = json.dumps(records, sort_keys=True, separators=(",", ":"), ensure_ascii=True)
    return hashlib.sha256(canonical.encode("utf-8")).hexdigest()


def _validate_candidate(manifest: dict[str, Any]) -> tuple[str, str]:
    candidate = manifest.get("candidate")
    if not isinstance(candidate, dict):
        raise ReviewBundleVerificationError("Review manifest is missing candidate binding")

    candidate_type = str(candidate.get("candidateType") or "").strip()
    if candidate_type not in ALLOWED_CANDIDATE_TYPES:
        raise ReviewBundleVerificationError(f"Unsupported review candidateType: {candidate_type!r}")
    git_sha = require_sha40(candidate.get("gitSha"), "candidate.gitSha")
    apk_sha = require_sha256(candidate.get("apkSha256"), "candidate.apkSha256")
    require_sha256(candidate.get("candidateManifestSha256"), "candidate.candidateManifestSha256")

    if candidate.get("releaseEvidenceEligible") is not True:
        raise ReviewBundleVerificationError("Candidate must remain releaseEvidenceEligible=true")
    if candidate.get("readyForDeviceEvidence") is not True:
        raise ReviewBundleVerificationError("Candidate must remain readyForDeviceEvidence=true")
    if candidate.get("verified") is not False:
        raise ReviewBundleVerificationError("Candidate must remain verified=false before manual approval")
    if candidate.get("verdict") != EXPECTED_CANDIDATE_VERDICT:
        raise ReviewBundleVerificationError(f"Unexpected candidate verdict: {candidate.get('verdict')!r}")
    return git_sha, apk_sha


def _validate_privacy(manifest: dict[str, Any]) -> None:
    privacy = manifest.get("privacy")
    if not isinstance(privacy, dict):
        raise ReviewBundleVerificationError("Review manifest is missing privacy contract")
    required_false = (
        "rawAdbSerialIncluded",
        "rawSessionIncluded",
        "rawLogcatIncluded",
        "rawActivityDumpIncluded",
        "candidateSourceManifestIncluded",
    )
    for key in required_false:
        if privacy.get(key) is not False:
            raise ReviewBundleVerificationError(f"Privacy flag {key} must be JSON boolean false")


def _validate_content_files(root: Path, manifest: dict[str, Any]) -> dict[str, dict[str, Any]]:
    records_raw = manifest.get("contentFiles")
    if not isinstance(records_raw, dict) or not records_raw:
        raise ReviewBundleVerificationError("Review manifest must contain non-empty contentFiles")

    records: dict[str, dict[str, Any]] = {}
    for raw_path, raw_record in records_raw.items():
        relative = safe_relative_path(raw_path)
        if relative != raw_path:
            raise ReviewBundleVerificationError(
                f"contentFiles path must already be canonical POSIX form: {raw_path!r}"
            )
        if not isinstance(raw_record, dict):
            raise ReviewBundleVerificationError(f"contentFiles[{relative!r}] must be an object")
        expected_sha = require_sha256(raw_record.get("sha256"), f"contentFiles[{relative}].sha256")
        size = raw_record.get("sizeBytes")
        if not isinstance(size, int) or isinstance(size, bool) or size <= 0:
            raise ReviewBundleVerificationError(f"contentFiles[{relative}].sizeBytes must be a positive integer")

        path = root / Path(relative)
        if path.is_symlink() or not path.is_file():
            raise ReviewBundleVerificationError(f"Review content file is missing or is a symlink: {relative}")
        actual_size = path.stat().st_size
        if actual_size != size:
            raise ReviewBundleVerificationError(
                f"Review content size mismatch for {relative}: manifest={size} actual={actual_size}"
            )
        actual_sha = sha256_file(path)
        if actual_sha != expected_sha:
            raise ReviewBundleVerificationError(
                f"Review content SHA-256 mismatch for {relative}: manifest={expected_sha} actual={actual_sha}"
            )
        records[relative] = {"sha256": expected_sha, "sizeBytes": size}

    copied_files = manifest.get("copiedFiles")
    if not isinstance(copied_files, list) or copied_files != sorted(records):
        raise ReviewBundleVerificationError("copiedFiles must exactly equal the sorted contentFiles path set")

    actual_files: set[str] = set()
    for path in root.rglob("*"):
        if path.is_symlink():
            raise ReviewBundleVerificationError(f"Review bundle contains a symlink: {path.relative_to(root)}")
        if path.is_file():
            relative = path.relative_to(root).as_posix()
            if relative != REVIEW_MANIFEST_FILE:
                actual_files.add(relative)
    if actual_files != set(records):
        missing = sorted(set(records) - actual_files)
        unexpected = sorted(actual_files - set(records))
        raise ReviewBundleVerificationError(
            f"Review bundle file-set mismatch: missing={missing} unexpected={unexpected}"
        )

    declared_set_sha = require_sha256(manifest.get("contentSetSha256"), "contentSetSha256")
    actual_set_sha = content_set_sha256(records)
    if actual_set_sha != declared_set_sha:
        raise ReviewBundleVerificationError(
            f"contentSetSha256 mismatch: manifest={declared_set_sha} actual={actual_set_sha}"
        )
    return records


def _validate_index_and_checkpoints(
    root: Path,
    manifest: dict[str, Any],
    *,
    git_sha: str,
    apk_sha: str,
) -> None:
    if manifest.get("packageId") != PACKAGE_ID:
        raise ReviewBundleVerificationError("Review manifest packageId does not match production package")
    if manifest.get("verdict") != EXPECTED_REVIEW_VERDICT:
        raise ReviewBundleVerificationError(f"Unexpected review verdict: {manifest.get('verdict')!r}")

    device_serial_hash = require_sha256(manifest.get("deviceSerialSha256"), "deviceSerialSha256")
    device = manifest.get("device")
    if not isinstance(device, dict) or device.get("isEmulator") is not False:
        raise ReviewBundleVerificationError("Review bundle must describe a physical Android device")

    labels = manifest.get("checkpoints")
    if not isinstance(labels, list) or not labels or not all(isinstance(label, str) and label for label in labels):
        raise ReviewBundleVerificationError("Review manifest checkpoints must be a non-empty string list")
    if len(labels) != len(set(labels)):
        raise ReviewBundleVerificationError("Review manifest checkpoints contain duplicates")
    if manifest.get("checkpointCount") != len(labels):
        raise ReviewBundleVerificationError("Review manifest checkpointCount does not match checkpoints")

    index = read_json(root / INDEX_FILE)
    if index.get("packageId") != PACKAGE_ID:
        raise ReviewBundleVerificationError("Evidence index packageId does not match production package")
    if require_sha256(index.get("apkSha256"), "evidence-index.apkSha256") != apk_sha:
        raise ReviewBundleVerificationError("Evidence index APK SHA does not match review candidate")
    if require_sha256(index.get("deviceSerialSha256"), "evidence-index.deviceSerialSha256") != device_serial_hash:
        raise ReviewBundleVerificationError("Evidence index device hash does not match review manifest")
    if index.get("verdict") != EXPECTED_REVIEW_VERDICT:
        raise ReviewBundleVerificationError("Evidence index must remain MANUAL_REVIEW_REQUIRED")
    if index.get("checkpoints") != labels or index.get("checkpointCount") != len(labels):
        raise ReviewBundleVerificationError("Evidence index checkpoints do not match review manifest")

    for label in labels:
        checkpoint = read_json(root / "checkpoints" / label / "checkpoint.json")
        if checkpoint.get("label") != label:
            raise ReviewBundleVerificationError(f"Checkpoint label mismatch: expected={label!r}")
        if require_sha256(checkpoint.get("apkSha256"), f"checkpoint {label} apkSha256") != apk_sha:
            raise ReviewBundleVerificationError(f"Checkpoint {label} APK SHA does not match candidate")
        if (
            require_sha256(checkpoint.get("deviceSerialSha256"), f"checkpoint {label} deviceSerialSha256")
            != device_serial_hash
        ):
            raise ReviewBundleVerificationError(f"Checkpoint {label} device hash does not match manifest")
        if checkpoint.get("manualReviewRequired") is not True:
            raise ReviewBundleVerificationError(f"Checkpoint {label} must remain manualReviewRequired=true")
        if checkpoint.get("files") != list(SAFE_CHECKPOINT_PAYLOAD_FILES):
            raise ReviewBundleVerificationError(f"Checkpoint {label} files do not match sanitized payload contract")
        if checkpoint.get("excludedByPolicy") != ["logcat.txt", "activity.txt"]:
            raise ReviewBundleVerificationError(f"Checkpoint {label} excludedByPolicy contract changed")

    # Git SHA is not repeated by the evidence index; the review manifest is the
    # candidate binding source after its content-file set has been verified.
    if not SHA40_RE.fullmatch(git_sha):
        raise ReviewBundleVerificationError("Internal candidate Git SHA validation failed")


def verify_bundle(
    bundle_dir: Path,
    *,
    expected_git_sha: str | None = None,
    expected_apk_sha: str | None = None,
) -> dict[str, Any]:
    root = bundle_dir.expanduser().resolve()
    if not root.is_dir():
        raise ReviewBundleVerificationError(f"Review bundle directory is missing: {root}")

    manifest = read_json(root / REVIEW_MANIFEST_FILE)
    if manifest.get("schemaVersion") != EXPECTED_SCHEMA_VERSION:
        raise ReviewBundleVerificationError(
            f"Unsupported review manifest schemaVersion: {manifest.get('schemaVersion')!r}"
        )
    if manifest.get("state") != EXPECTED_STATE:
        raise ReviewBundleVerificationError(f"Unexpected review manifest state: {manifest.get('state')!r}")

    git_sha, apk_sha = _validate_candidate(manifest)
    _validate_privacy(manifest)
    records = _validate_content_files(root, manifest)
    _validate_index_and_checkpoints(root, manifest, git_sha=git_sha, apk_sha=apk_sha)

    if expected_git_sha is not None and require_sha40(expected_git_sha, "--expected-git-sha") != git_sha:
        raise ReviewBundleVerificationError(
            f"Review candidate Git SHA mismatch: expected={expected_git_sha.lower()} actual={git_sha}"
        )
    if expected_apk_sha is not None and require_sha256(expected_apk_sha, "--expected-apk-sha") != apk_sha:
        raise ReviewBundleVerificationError(
            f"Review candidate APK SHA mismatch: expected={expected_apk_sha.lower()} actual={apk_sha}"
        )

    return {
        "gitSha": git_sha,
        "apkSha256": apk_sha,
        "deviceSerialSha256": manifest["deviceSerialSha256"],
        "checkpointCount": manifest["checkpointCount"],
        "contentFileCount": len(records),
        "contentSetSha256": manifest["contentSetSha256"],
        "verdict": manifest["verdict"],
        "verified": False,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Verify SHA-256 integrity and candidate binding of a sanitized 3Fareet device review bundle."
    )
    parser.add_argument("--bundle", required=True, help="Sanitized review bundle directory.")
    parser.add_argument("--expected-git-sha", help="Optional exact candidate Git SHA expected by the reviewer.")
    parser.add_argument("--expected-apk-sha", help="Optional exact candidate APK SHA-256 expected by the reviewer.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_bundle(
            Path(args.bundle),
            expected_git_sha=args.expected_git_sha,
            expected_apk_sha=args.expected_apk_sha,
        )
        print(
            "AFAREET_DEVICE_REVIEW_BUNDLE_VERIFIED "
            f"gitSha={result['gitSha']} apkSha256={result['apkSha256']} "
            f"checkpoints={result['checkpointCount']} files={result['contentFileCount']} "
            f"contentSetSha256={result['contentSetSha256']} "
            f"verdict={result['verdict']} verified=false"
        )
        return 0
    except (ReviewBundleVerificationError, OSError, ValueError) as exc:
        print(f"AFAREET_DEVICE_REVIEW_VERIFY_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
