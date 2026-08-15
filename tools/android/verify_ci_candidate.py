#!/usr/bin/env python3
"""Create a device-evidence candidate manifest from verified GitHub Unity CI APK metadata.

The Android CI job can only run after Unity tests succeed. This gate binds the
workflow-produced artifact metadata to the exact APK bytes and expected GitHub
repository/workflow identity before physical-device QA. It does not contact
GitHub and never self-asserts VERIFIED state.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

EXPECTED_SOURCE = "github-actions-unity-production-ci"
EXPECTED_ARTIFACT = "afareet-unity3d-debug.apk"
EXPECTED_PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
EXPECTED_MIN_SDK = 26
EXPECTED_ABI = "arm64-v8a"
EXPECTED_CANDIDATE_TYPE = "github-actions-unity-ci"
EXPECTED_REPOSITORY = "walidatiyaai2025-gif/3fareet"
EXPECTED_WORKFLOW = "Unity Production CI"
ALLOWED_EVENTS = {"pull_request", "push", "workflow_dispatch"}
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
DIGITS_RE = re.compile(r"^[1-9][0-9]*$")


class CiCandidateError(RuntimeError):
    pass


def utc_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat()


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise CiCandidateError(f"CI artifact metadata is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise CiCandidateError(f"CI artifact metadata is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise CiCandidateError("CI artifact metadata root must be a JSON object")
    return payload


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _full_sha(value: Any) -> str:
    sha = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(sha):
        raise CiCandidateError(f"gitSha must be a full 40-character SHA, found {value!r}")
    return sha


def _positive_id(value: Any, label: str) -> str:
    text = str(value or "").strip()
    if not DIGITS_RE.fullmatch(text):
        raise CiCandidateError(f"{label} must be a positive integer string, found {value!r}")
    return text


def verify_ci_candidate(metadata: dict[str, Any], apk_path: Path) -> dict[str, Any]:
    if metadata.get("source") != EXPECTED_SOURCE:
        raise CiCandidateError(f"Unsupported CI metadata source: {metadata.get('source')!r}")
    if metadata.get("artifact") != EXPECTED_ARTIFACT:
        raise CiCandidateError(f"Unexpected artifact name: {metadata.get('artifact')!r}")
    if metadata.get("packageId") != EXPECTED_PACKAGE_ID:
        raise CiCandidateError(f"Unexpected packageId: {metadata.get('packageId')!r}")
    try:
        min_sdk = int(metadata.get("minSdk", -1))
    except (TypeError, ValueError) as exc:
        raise CiCandidateError("minSdk is invalid") from exc
    if min_sdk != EXPECTED_MIN_SDK:
        raise CiCandidateError(f"Unexpected minSdk: {min_sdk}")
    if metadata.get("abi") != EXPECTED_ABI:
        raise CiCandidateError(f"Unexpected ABI: {metadata.get('abi')!r}")

    git_sha = _full_sha(metadata.get("gitSha"))
    run_id = _positive_id(metadata.get("runId"), "runId")
    run_attempt = _positive_id(metadata.get("runAttempt"), "runAttempt")
    repository = str(metadata.get("repository") or "").strip()
    workflow = str(metadata.get("workflow") or "").strip()
    event_name = str(metadata.get("eventName") or "").strip()
    ref = str(metadata.get("ref") or "").strip()
    if repository != EXPECTED_REPOSITORY:
        raise CiCandidateError(f"Unexpected GitHub repository: {repository!r}")
    if workflow != EXPECTED_WORKFLOW:
        raise CiCandidateError(f"Unexpected GitHub workflow: {workflow!r}")
    if event_name not in ALLOWED_EVENTS:
        raise CiCandidateError(f"Unexpected GitHub eventName: {event_name!r}")
    if not ref.startswith("refs/"):
        raise CiCandidateError(f"GitHub ref must be a non-empty refs/* value, found {ref!r}")

    if not apk_path.is_file() or apk_path.stat().st_size <= 0:
        raise CiCandidateError(f"APK is missing or empty: {apk_path}")
    if apk_path.name != EXPECTED_ARTIFACT:
        raise CiCandidateError(f"APK filename must be {EXPECTED_ARTIFACT}: {apk_path.name}")

    declared_hash = str(metadata.get("sha256") or "").strip().lower()
    if not SHA256_RE.fullmatch(declared_hash):
        raise CiCandidateError(f"Metadata SHA-256 is invalid: {declared_hash!r}")
    actual_hash = sha256_file(apk_path)
    if actual_hash != declared_hash:
        raise CiCandidateError(f"APK SHA-256 mismatch: metadata={declared_hash} actual={actual_hash}")

    try:
        declared_size = int(metadata.get("sizeBytes", -1))
    except (TypeError, ValueError) as exc:
        raise CiCandidateError("sizeBytes is invalid") from exc
    actual_size = apk_path.stat().st_size
    if declared_size <= 0 or actual_size != declared_size:
        raise CiCandidateError(f"APK size mismatch: metadata={declared_size} actual={actual_size}")

    return {
        "schemaVersion": 1,
        "generatedAtUtc": utc_now(),
        "candidateType": EXPECTED_CANDIDATE_TYPE,
        "gitSha": git_sha,
        "packageId": EXPECTED_PACKAGE_ID,
        "minSdk": EXPECTED_MIN_SDK,
        "abi": EXPECTED_ABI,
        "githubRun": {
            "runId": run_id,
            "runAttempt": run_attempt,
            "repository": repository,
            "workflow": workflow,
            "eventName": event_name,
            "ref": ref,
        },
        "apk": {
            "path": str(apk_path.resolve()),
            "fileName": apk_path.name,
            "sizeBytes": actual_size,
            "sha256": actual_hash,
        },
        "releaseEvidenceEligible": True,
        "readyForDeviceEvidence": True,
        "verified": False,
        "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        "notes": [
            "This manifest binds Unity Production CI artifact metadata to the exact APK bytes and expected workflow identity.",
            "It does not independently query GitHub to attest run conclusion after the bundle is downloaded.",
            "It does not replace physical-device, performance, visual, or human approval gates.",
        ],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Bind Unity Production CI artifact metadata to an exact APK candidate.")
    parser.add_argument("--artifact-metadata", required=True, help="Path to CI artifacts/android/artifact-metadata.json")
    parser.add_argument("--apk", required=True, help="Path to the matching downloaded APK")
    parser.add_argument("--output", required=True, help="Output path for ci-candidate-manifest.json")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        metadata_path = Path(args.artifact_metadata).expanduser().resolve()
        apk_path = Path(args.apk).expanduser().resolve()
        output_path = Path(args.output).expanduser().resolve()
        manifest = verify_ci_candidate(read_json(metadata_path), apk_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_CI_CANDIDATE_READY "
            f"gitSha={manifest['gitSha']} runId={manifest['githubRun']['runId']} "
            f"apkSha256={manifest['apk']['sha256']} output={output_path}"
        )
        return 0
    except (CiCandidateError, OSError, ValueError) as exc:
        print(f"AFAREET_CI_CANDIDATE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
