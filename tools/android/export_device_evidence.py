#!/usr/bin/env python3
"""Export a candidate-bound physical-device session as a privacy-safe review bundle.

The raw device-evidence session intentionally keeps the ADB serial and broad
local diagnostics so repeat captures can be pinned to the same device. Those
raw files are not appropriate for automatic publication from a public
repository. This exporter validates the session/candidate binding and copies
only the minimum review material that is designed to be shared.

It never decides gameplay feel, race correctness, visual quality, performance
acceptance, or release readiness. The exported verdict remains
MANUAL_REVIEW_REQUIRED.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import sys
from pathlib import Path
from typing import Any

PACKAGE_ID = "com.fiftysolutions.afareetunity3d"
SESSION_FILE = "session.json"
INDEX_FILE = "evidence-index.json"
REVIEW_MANIFEST_FILE = "review-manifest.json"
EXPECTED_CANDIDATE_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
EXPECTED_REVIEW_VERDICT = "MANUAL_REVIEW_REQUIRED"
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
SAFE_LABEL_RE = re.compile(r"^[A-Za-z0-9._-]{1,64}$")

# Raw logcat/activity dumps can contain unrelated system/app information. The
# session and candidate manifest can contain local paths or the raw ADB serial.
# None of them is copied by the default public-review export contract.
SAFE_CHECKPOINT_FILES = (
    "screen.png",
    "checkpoint.json",
    "meminfo.txt",
    "gfxinfo.txt",
    "thermalservice.txt",
    "battery.txt",
)
EXCLUDED_BY_POLICY = (
    SESSION_FILE,
    "candidate-manifest.json",
    "package-dump.txt",
    "checkpoints/*/logcat.txt",
    "checkpoints/*/activity.txt",
)
TEXT_SUFFIXES = {".json", ".txt", ".md", ".csv", ".xml"}


class EvidenceExportError(RuntimeError):
    pass


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise EvidenceExportError(f"Required JSON file is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise EvidenceExportError(f"Invalid JSON file {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise EvidenceExportError(f"JSON root must be an object: {path}")
    return payload


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def serial_sha256(serial: str) -> str:
    return hashlib.sha256(serial.encode("utf-8")).hexdigest()


def require_sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise EvidenceExportError(f"{label} must be a full SHA-256 value, found {value!r}")
    return text


def require_sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise EvidenceExportError(f"{label} must be a full 40-character Git SHA, found {value!r}")
    return text


def require_safe_label(value: Any) -> str:
    label = str(value or "").strip()
    if not SAFE_LABEL_RE.fullmatch(label) or label in {".", ".."}:
        raise EvidenceExportError(f"Unsafe checkpoint label in evidence index: {value!r}")
    return label


def _is_nested(path: Path, parent: Path) -> bool:
    try:
        path.relative_to(parent)
        return True
    except ValueError:
        return False


def _validate_candidate(session: dict[str, Any], apk_sha: str) -> dict[str, Any]:
    candidate = session.get("candidate")
    if not isinstance(candidate, dict):
        raise EvidenceExportError(
            "Device session is not bound to an integrity-checked Unity candidate; "
            "prepare it with prepare_candidate_device.py before export."
        )

    git_sha = require_sha40(candidate.get("gitSha"), "session.candidate.gitSha")
    candidate_apk_sha = require_sha256(candidate.get("apkSha256"), "session.candidate.apkSha256")
    if candidate_apk_sha != apk_sha:
        raise EvidenceExportError(
            f"Candidate/session APK SHA mismatch: candidate={candidate_apk_sha} session={apk_sha}"
        )
    if candidate.get("releaseEvidenceEligible") is not True:
        raise EvidenceExportError("Candidate is not release-evidence eligible")
    if candidate.get("readyForDeviceEvidence") is not True:
        raise EvidenceExportError("Candidate is not ready for physical-device evidence")
    if candidate.get("verified") is not False:
        raise EvidenceExportError("Candidate must remain verified=false before manual device approval")
    if candidate.get("verdict") != EXPECTED_CANDIDATE_VERDICT:
        raise EvidenceExportError(f"Unexpected candidate verdict: {candidate.get('verdict')!r}")

    manifest = candidate.get("manifest")
    if not isinstance(manifest, dict):
        raise EvidenceExportError("Candidate binding is missing manifest integrity metadata")
    manifest_sha = require_sha256(manifest.get("sha256"), "session.candidate.manifest.sha256")

    candidate_type = str(candidate.get("candidateType") or "").strip()
    if not candidate_type:
        raise EvidenceExportError("Candidate binding is missing candidateType")

    return {
        "candidateType": candidate_type,
        "gitSha": git_sha,
        "apkSha256": candidate_apk_sha,
        "candidateManifestSha256": manifest_sha,
        "releaseEvidenceEligible": True,
        "readyForDeviceEvidence": True,
        "verified": False,
        "verdict": EXPECTED_CANDIDATE_VERDICT,
    }


def _validate_session_and_index(
    session: dict[str, Any], index: dict[str, Any]
) -> tuple[str, str, dict[str, Any], dict[str, Any], list[str]]:
    if session.get("packageId") != PACKAGE_ID or index.get("packageId") != PACKAGE_ID:
        raise EvidenceExportError("Session/index packageId does not match the production Android package")

    session_apk = session.get("apk")
    if not isinstance(session_apk, dict):
        raise EvidenceExportError("Session is missing APK metadata")
    session_apk_sha = require_sha256(session_apk.get("sha256"), "session.apk.sha256")
    index_apk_sha = require_sha256(index.get("apkSha256"), "evidence-index.apkSha256")
    if session_apk_sha != index_apk_sha:
        raise EvidenceExportError(
            f"Session/index APK SHA mismatch: session={session_apk_sha} index={index_apk_sha}"
        )

    device = session.get("device")
    if not isinstance(device, dict):
        raise EvidenceExportError("Session is missing device metadata")
    raw_serial = str(device.get("serial") or "")
    if not raw_serial:
        raise EvidenceExportError("Session is missing the raw ADB serial required to validate the serial hash")

    declared_serial_hash = require_sha256(device.get("serialSha256"), "session.device.serialSha256")
    index_serial_hash = require_sha256(index.get("deviceSerialSha256"), "evidence-index.deviceSerialSha256")
    actual_serial_hash = serial_sha256(raw_serial)
    if declared_serial_hash != actual_serial_hash or index_serial_hash != actual_serial_hash:
        raise EvidenceExportError("Raw device serial does not match the session/index serial SHA-256 binding")

    index_device = index.get("device")
    if not isinstance(index_device, dict):
        raise EvidenceExportError("Evidence index is missing sanitized device metadata")
    if index_device.get("isEmulator") is not False:
        raise EvidenceExportError("P1 review bundles require physical-device evidence; emulator sessions are rejected")

    if index.get("verdict") != EXPECTED_REVIEW_VERDICT:
        raise EvidenceExportError(f"Unexpected evidence-index verdict: {index.get('verdict')!r}")

    checkpoints_raw = index.get("checkpoints")
    if not isinstance(checkpoints_raw, list) or not checkpoints_raw:
        raise EvidenceExportError("Evidence index must contain at least one checkpoint")
    labels = [require_safe_label(item) for item in checkpoints_raw]
    if len(set(labels)) != len(labels):
        raise EvidenceExportError("Evidence index contains duplicate checkpoint labels")
    if index.get("checkpointCount") != len(labels):
        raise EvidenceExportError(
            f"checkpointCount mismatch: declared={index.get('checkpointCount')!r} actual={len(labels)}"
        )

    candidate_summary = _validate_candidate(session, session_apk_sha)
    return raw_serial, actual_serial_hash, index_device, candidate_summary, labels


def _validate_checkpoint(
    checkpoint_dir: Path,
    label: str,
    apk_sha: str,
    device_serial_hash: str,
) -> dict[str, Any]:
    metadata = read_json(checkpoint_dir / "checkpoint.json")
    if metadata.get("label") != label:
        raise EvidenceExportError(f"Checkpoint metadata label mismatch for {label}")
    if require_sha256(metadata.get("apkSha256"), f"checkpoint {label} apkSha256") != apk_sha:
        raise EvidenceExportError(f"Checkpoint {label} APK SHA does not match the session")
    if (
        require_sha256(metadata.get("deviceSerialSha256"), f"checkpoint {label} deviceSerialSha256")
        != device_serial_hash
    ):
        raise EvidenceExportError(f"Checkpoint {label} device hash does not match the session")
    if metadata.get("manualReviewRequired") is not True:
        raise EvidenceExportError(f"Checkpoint {label} must remain manualReviewRequired=true")

    for name in SAFE_CHECKPOINT_FILES:
        path = checkpoint_dir / name
        if not path.is_file() or path.stat().st_size <= 0:
            raise EvidenceExportError(f"Checkpoint {label} is missing required review file: {name}")
    return metadata


def _assert_text_does_not_contain_serial(path: Path, raw_serial: str) -> None:
    if path.suffix.lower() not in TEXT_SUFFIXES:
        return
    text = path.read_text(encoding="utf-8", errors="replace")
    if raw_serial and raw_serial in text:
        raise EvidenceExportError(f"Sanitized review file contains raw ADB serial: {path}")


def export_bundle(session_dir: Path, output_dir: Path, *, force: bool = False) -> dict[str, Any]:
    session_dir = session_dir.expanduser().resolve()
    output_dir = output_dir.expanduser().resolve()
    if not session_dir.is_dir():
        raise EvidenceExportError(f"Device evidence session directory is missing: {session_dir}")
    if output_dir == session_dir or _is_nested(output_dir, session_dir):
        raise EvidenceExportError("Review bundle output must not be inside the raw evidence session directory")

    session = read_json(session_dir / SESSION_FILE)
    index = read_json(session_dir / INDEX_FILE)
    raw_serial, serial_hash, index_device, candidate, labels = _validate_session_and_index(session, index)
    apk_sha = candidate["apkSha256"]

    checkpoint_metadata: dict[str, dict[str, Any]] = {}
    for label in labels:
        checkpoint_metadata[label] = _validate_checkpoint(
            session_dir / "checkpoints" / label,
            label,
            apk_sha,
            serial_hash,
        )

    # Validate the already-sanitized index before creating/replacing output.
    _assert_text_does_not_contain_serial(session_dir / INDEX_FILE, raw_serial)

    if output_dir.exists():
        if not force:
            raise EvidenceExportError(f"Review bundle output already exists: {output_dir}; use --force to replace it")
        if not output_dir.is_dir():
            raise EvidenceExportError(f"Review bundle output is not a directory: {output_dir}")
        shutil.rmtree(output_dir)
    output_dir.mkdir(parents=True)

    copied_files: list[str] = []
    index_target = output_dir / INDEX_FILE
    shutil.copy2(session_dir / INDEX_FILE, index_target)
    copied_files.append(INDEX_FILE)

    for label in labels:
        source_dir = session_dir / "checkpoints" / label
        target_dir = output_dir / "checkpoints" / label
        target_dir.mkdir(parents=True)
        for name in SAFE_CHECKPOINT_FILES:
            source = source_dir / name
            target = target_dir / name
            shutil.copy2(source, target)
            relative = target.relative_to(output_dir).as_posix()
            copied_files.append(relative)
            _assert_text_does_not_contain_serial(target, raw_serial)

    review_manifest = {
        "schemaVersion": 1,
        "state": "SANITIZED_REVIEW_BUNDLE",
        "verdict": EXPECTED_REVIEW_VERDICT,
        "packageId": PACKAGE_ID,
        "candidate": candidate,
        "deviceSerialSha256": serial_hash,
        "device": index_device,
        "checkpointCount": len(labels),
        "checkpoints": labels,
        "automatedRedFlagCount": int(index.get("automatedRedFlagCount", 0)),
        "automatedRedFlags": index.get("automatedRedFlags", []),
        "manualReviewChecklist": index.get("manualReviewChecklist", []),
        "copiedFiles": sorted(copied_files),
        "excludedByPolicy": list(EXCLUDED_BY_POLICY),
        "privacy": {
            "rawAdbSerialIncluded": False,
            "rawSessionIncluded": False,
            "rawLogcatIncluded": False,
            "rawActivityDumpIncluded": False,
            "candidateSourceManifestIncluded": False,
        },
    }
    manifest_target = output_dir / REVIEW_MANIFEST_FILE
    write_json(manifest_target, review_manifest)
    _assert_text_does_not_contain_serial(manifest_target, raw_serial)

    # Final fail-closed scan of every copied text file and generated manifest.
    for path in output_dir.rglob("*"):
        if path.is_file():
            _assert_text_does_not_contain_serial(path, raw_serial)

    return review_manifest


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Export a candidate-bound device evidence session as a privacy-safe manual-review bundle."
    )
    parser.add_argument("--session", required=True, help="Raw device evidence session directory.")
    parser.add_argument("--output", required=True, help="Destination for the sanitized review bundle.")
    parser.add_argument("--force", action="store_true", help="Replace an existing output directory.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        manifest = export_bundle(Path(args.session), Path(args.output), force=args.force)
        red_flags = int(manifest.get("automatedRedFlagCount", 0))
        print(
            "AFAREET_DEVICE_REVIEW_BUNDLE_EXPORTED "
            f"gitSha={manifest['candidate']['gitSha']} apkSha256={manifest['candidate']['apkSha256']} "
            f"checkpoints={manifest['checkpointCount']} redFlags={red_flags} "
            f"verdict={manifest['verdict']} output={Path(args.output).expanduser().resolve()}"
        )
        return 2 if red_flags > 0 else 0
    except (EvidenceExportError, OSError, ValueError) as exc:
        print(f"AFAREET_DEVICE_REVIEW_EXPORT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
