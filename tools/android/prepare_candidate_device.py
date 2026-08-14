#!/usr/bin/env python3
"""Prepare physical-device evidence only from an integrity-checked local candidate.

This wrapper bridges ``verify_local_candidate.py`` and ``device_evidence.py``.
It fails closed if the candidate manifest and the APK bytes do not match, then
hands the exact APK to the existing ADB evidence collector. It never marks a
candidate or device gate VERIFIED.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

EXPECTED_PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
EXPECTED_ARTIFACT = "afareet-unity3d-debug.apk"
EXPECTED_CANDIDATE_TYPE = "local-windows-licensed-unity"
EXPECTED_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")


class CandidatePrepareError(RuntimeError):
    pass


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise CandidatePrepareError(f"Candidate manifest is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise CandidatePrepareError(f"Candidate manifest is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise CandidatePrepareError("Candidate manifest root must be a JSON object")
    return payload


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def resolve_candidate(
    manifest: dict[str, Any],
    manifest_path: Path,
    apk_override: Path | None = None,
) -> dict[str, Any]:
    if manifest.get("candidateType") != EXPECTED_CANDIDATE_TYPE:
        raise CandidatePrepareError(f"Unsupported candidateType: {manifest.get('candidateType')!r}")
    if manifest.get("packageId") != EXPECTED_PACKAGE_ID:
        raise CandidatePrepareError(f"Unexpected packageId: {manifest.get('packageId')!r}")
    if manifest.get("releaseEvidenceEligible") is not True:
        raise CandidatePrepareError("Candidate is not release-evidence eligible")
    if manifest.get("readyForDeviceEvidence") is not True:
        raise CandidatePrepareError("Candidate is not ready for physical-device evidence")
    if manifest.get("verified") is not False:
        raise CandidatePrepareError("Candidate manifest must not self-assert VERIFIED state")
    if manifest.get("verdict") != EXPECTED_VERDICT:
        raise CandidatePrepareError(f"Unexpected candidate verdict: {manifest.get('verdict')!r}")

    git_sha = str(manifest.get("gitSha") or "").strip().lower()
    if not SHA40_RE.fullmatch(git_sha):
        raise CandidatePrepareError(f"Candidate gitSha is not a full 40-character SHA: {git_sha!r}")

    apk_record = manifest.get("apk")
    if not isinstance(apk_record, dict):
        raise CandidatePrepareError("Candidate manifest is missing apk metadata")

    file_name = str(apk_record.get("fileName") or "")
    if file_name != EXPECTED_ARTIFACT:
        raise CandidatePrepareError(f"Unexpected APK fileName: {file_name!r}")

    declared_hash = str(apk_record.get("sha256") or "").strip().lower()
    if not SHA256_RE.fullmatch(declared_hash):
        raise CandidatePrepareError(f"Candidate APK SHA-256 is invalid: {declared_hash!r}")

    try:
        declared_size = int(apk_record.get("sizeBytes", -1))
    except (TypeError, ValueError) as exc:
        raise CandidatePrepareError("Candidate APK sizeBytes is invalid") from exc
    if declared_size <= 0:
        raise CandidatePrepareError(f"Candidate APK sizeBytes must be positive: {declared_size}")

    if apk_override is not None:
        apk_path = apk_override.expanduser().resolve()
    else:
        raw_path = str(apk_record.get("path") or "").strip()
        if not raw_path:
            raise CandidatePrepareError("Candidate manifest APK path is missing; pass --apk when using a moved bundle")
        candidate_path = Path(raw_path).expanduser()
        apk_path = candidate_path.resolve() if candidate_path.is_absolute() else (manifest_path.parent / candidate_path).resolve()

    if not apk_path.is_file() or apk_path.stat().st_size <= 0:
        raise CandidatePrepareError(f"Candidate APK is missing or empty: {apk_path}")
    if apk_path.name != EXPECTED_ARTIFACT:
        raise CandidatePrepareError(f"Candidate APK filename must be {EXPECTED_ARTIFACT}: {apk_path.name}")

    actual_size = apk_path.stat().st_size
    if actual_size != declared_size:
        raise CandidatePrepareError(f"Candidate APK size mismatch: manifest={declared_size} actual={actual_size}")

    actual_hash = sha256_file(apk_path)
    if actual_hash != declared_hash:
        raise CandidatePrepareError(f"Candidate APK SHA-256 mismatch: manifest={declared_hash} actual={actual_hash}")

    return {
        "apkPath": apk_path,
        "apkSha256": actual_hash,
        "sizeBytes": actual_size,
        "gitSha": git_sha,
        "packageId": EXPECTED_PACKAGE_ID,
        "candidateType": EXPECTED_CANDIDATE_TYPE,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Validate an exact local Unity candidate, then prepare its physical-device evidence session."
    )
    parser.add_argument("--candidate-manifest", required=True, help="Path to local-candidate-manifest.json")
    parser.add_argument(
        "--apk",
        help="Optional APK path override for a moved evidence bundle. Bytes must still match the manifest exactly.",
    )
    parser.add_argument("--output", required=True, help="Output directory for the device evidence session.")
    parser.add_argument("--serial", help="ADB serial when more than one authorized device is connected.")
    parser.add_argument("--allow-emulator", action="store_true", help="Harness debugging only; cannot satisfy P1 gates.")
    parser.add_argument("--force", action="store_true", help="Replace an existing evidence session.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        manifest_path = Path(args.candidate_manifest).expanduser().resolve()
        apk_override = Path(args.apk) if args.apk else None
        candidate = resolve_candidate(read_json(manifest_path), manifest_path, apk_override)

        print(
            "AFAREET_CANDIDATE_DEVICE_PRECHECK_OK "
            f"gitSha={candidate['gitSha']} apkSha256={candidate['apkSha256']} apk={candidate['apkPath']}"
        )

        import device_evidence  # Imported only after the candidate integrity check passes.

        child_args = [
            "prepare",
            "--apk",
            str(candidate["apkPath"]),
            "--output",
            args.output,
        ]
        if args.serial:
            child_args.extend(["--serial", args.serial])
        if args.allow_emulator:
            child_args.append("--allow-emulator")
        if args.force:
            child_args.append("--force")
        return int(device_evidence.main(child_args))
    except (CandidatePrepareError, OSError, ValueError) as exc:
        print(f"AFAREET_CANDIDATE_DEVICE_PREPARE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
