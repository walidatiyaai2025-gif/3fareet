#!/usr/bin/env python3
"""Fail-closed integrity gate for a locally built 3Fareet Unity Android candidate.

The gate proves that local Unity EditMode/PlayMode evidence and the inspected APK
refer to the same clean Git commit and that the APK bytes match build metadata.
It never marks a candidate VERIFIED; physical-device/manual gates remain required.
"""

from __future__ import annotations

import argparse
import datetime as dt
import hashlib
import json
import re
from pathlib import Path
from typing import Any

EXPECTED_UNITY_VERSION = "6000.5.8f1"
EXPECTED_PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
EXPECTED_ABI = "arm64-v8a"
EXPECTED_MIN_SDK = 26
EXPECTED_ARTIFACT = "afareet-unity3d-debug.apk"
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
PASS_RESULTS = {"passed", "success"}


class CandidateError(RuntimeError):
    pass


def utc_now() -> str:
    return dt.datetime.now(dt.timezone.utc).replace(microsecond=0).isoformat()


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise CandidateError(f"Required metadata file is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise CandidateError(f"Could not read valid JSON from {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise CandidateError(f"Metadata root must be a JSON object: {path}")
    return payload


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def require_bool(payload: dict[str, Any], key: str, expected: bool, label: str) -> None:
    value = payload.get(key)
    if value is not expected:
        raise CandidateError(f"{label}.{key} must be {str(expected).lower()}, found {value!r}")


def normalized_sha(value: Any, label: str) -> str:
    sha = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(sha):
        raise CandidateError(f"{label} must contain a full 40-character Git SHA, found {value!r}")
    return sha


def verify_test_mode(mode: Any, label: str) -> dict[str, int | str]:
    if not isinstance(mode, dict):
        raise CandidateError(f"{label} test metadata is missing or invalid")
    result = str(mode.get("result") or "").strip()
    try:
        total = int(mode.get("total", 0))
        passed = int(mode.get("passed", 0))
        failed = int(mode.get("failed", 0))
        skipped = int(mode.get("skipped", 0))
    except (TypeError, ValueError) as exc:
        raise CandidateError(f"{label} counters are not integers") from exc
    if total <= 0:
        raise CandidateError(f"{label} executed zero tests")
    if passed <= 0:
        raise CandidateError(f"{label} contains no passing tests; all-skipped/non-executed evidence is not release eligible")
    if failed != 0:
        raise CandidateError(f"{label} contains failed tests: {failed}")
    if result.lower() not in PASS_RESULTS:
        raise CandidateError(f"{label} result is not passing: {result!r}")
    if passed < 0 or skipped < 0 or passed + failed + skipped > total:
        raise CandidateError(
            f"{label} counters are inconsistent: total={total} passed={passed} failed={failed} skipped={skipped}"
        )
    return {
        "result": result,
        "total": total,
        "passed": passed,
        "failed": failed,
        "skipped": skipped,
    }


def verify_candidate(
    test_metadata: dict[str, Any],
    build_metadata: dict[str, Any],
    apk_path: Path,
) -> dict[str, Any]:
    if not apk_path.is_file() or apk_path.stat().st_size <= 0:
        raise CandidateError(f"APK is missing or empty: {apk_path}")

    if test_metadata.get("source") != "local-windows-licensed-unity-tests":
        raise CandidateError("Test metadata source is not the supported local Unity test path")
    if build_metadata.get("source") != "local-windows-licensed-unity":
        raise CandidateError("Build metadata source is not a clean local Unity build")

    require_bool(test_metadata, "releaseEvidenceEligible", True, "testMetadata")
    require_bool(test_metadata, "dirtyTree", False, "testMetadata")
    require_bool(build_metadata, "releaseEvidenceEligible", True, "buildMetadata")
    require_bool(build_metadata, "gitDirty", False, "buildMetadata")

    test_sha = normalized_sha(test_metadata.get("gitSha"), "testMetadata.gitSha")
    build_sha = normalized_sha(build_metadata.get("gitSha"), "buildMetadata.gitSha")
    if test_sha != build_sha:
        raise CandidateError(f"Git SHA mismatch: tests={test_sha} build={build_sha}")

    test_unity = str(test_metadata.get("unityVersion") or "")
    build_unity = str(build_metadata.get("unityVersion") or "")
    if test_unity != EXPECTED_UNITY_VERSION or build_unity != EXPECTED_UNITY_VERSION:
        raise CandidateError(
            f"Unity version must be {EXPECTED_UNITY_VERSION}: tests={test_unity!r} build={build_unity!r}"
        )

    edit = verify_test_mode(test_metadata.get("editMode"), "EditMode")
    play = verify_test_mode(test_metadata.get("playMode"), "PlayMode")

    if build_metadata.get("artifact") != EXPECTED_ARTIFACT:
        raise CandidateError(f"Unexpected artifact name: {build_metadata.get('artifact')!r}")
    if build_metadata.get("packageId") != EXPECTED_PACKAGE_ID:
        raise CandidateError(f"Unexpected package id: {build_metadata.get('packageId')!r}")
    if int(build_metadata.get("minSdk", -1)) != EXPECTED_MIN_SDK:
        raise CandidateError(f"Unexpected minSdk: {build_metadata.get('minSdk')!r}")
    if build_metadata.get("abi") != EXPECTED_ABI:
        raise CandidateError(f"Unexpected ABI: {build_metadata.get('abi')!r}")

    declared_hash = str(build_metadata.get("sha256") or "").strip().lower()
    if not SHA256_RE.fullmatch(declared_hash):
        raise CandidateError(f"Build metadata SHA-256 is invalid: {declared_hash!r}")
    actual_hash = sha256_file(apk_path)
    if actual_hash != declared_hash:
        raise CandidateError(f"APK SHA-256 mismatch: metadata={declared_hash} actual={actual_hash}")

    actual_size = apk_path.stat().st_size
    try:
        declared_size = int(build_metadata.get("sizeBytes", -1))
    except (TypeError, ValueError) as exc:
        raise CandidateError("Build metadata sizeBytes is invalid") from exc
    if actual_size != declared_size:
        raise CandidateError(f"APK size mismatch: metadata={declared_size} actual={actual_size}")

    return {
        "schemaVersion": 1,
        "generatedAtUtc": utc_now(),
        "candidateType": "local-windows-licensed-unity",
        "gitSha": test_sha,
        "gitBranch": str(build_metadata.get("gitBranch") or test_metadata.get("gitBranch") or ""),
        "unityVersion": EXPECTED_UNITY_VERSION,
        "packageId": EXPECTED_PACKAGE_ID,
        "minSdk": EXPECTED_MIN_SDK,
        "abi": EXPECTED_ABI,
        "apk": {
            "path": str(apk_path.resolve()),
            "fileName": apk_path.name,
            "sizeBytes": actual_size,
            "sha256": actual_hash,
        },
        "unityTests": {
            "editMode": edit,
            "playMode": play,
        },
        "releaseEvidenceEligible": True,
        "readyForDeviceEvidence": True,
        "verified": False,
        "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        "notes": [
            "This manifest proves same-SHA local Unity test/build integrity only.",
            "It does not make GitHub Unity Production CI green.",
            "It does not replace physical-device, performance, visual, or human approval gates.",
        ],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Verify local Unity tests and Android APK form one exact-head candidate.")
    parser.add_argument("--test-metadata", required=True, help="Path to artifacts/unity-local-tests/test-metadata.json")
    parser.add_argument("--build-metadata", required=True, help="Path to artifacts/android-local/artifact-metadata.json")
    parser.add_argument("--apk", required=True, help="Path to the exact APK referenced by build metadata")
    parser.add_argument("--output", required=True, help="Output path for local-candidate-manifest.json")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        test_path = Path(args.test_metadata).expanduser().resolve()
        build_path = Path(args.build_metadata).expanduser().resolve()
        apk_path = Path(args.apk).expanduser().resolve()
        output_path = Path(args.output).expanduser().resolve()
        manifest = verify_candidate(read_json(test_path), read_json(build_path), apk_path)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_LOCAL_CANDIDATE_READY "
            f"gitSha={manifest['gitSha']} apkSha256={manifest['apk']['sha256']} output={output_path}"
        )
        return 0
    except (CandidateError, OSError, ValueError) as exc:
        print(f"AFAREET_LOCAL_CANDIDATE_ERROR: {exc}", file=__import__('sys').stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
