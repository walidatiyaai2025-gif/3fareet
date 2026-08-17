#!/usr/bin/env python3
"""Export a P1 physical-device session as a sanitized lineage-bound review bundle.

The generic exporter remains authoritative for checkpoint sanitization, privacy and candidate
binding. This wrapper first validates the Step 11 P1 lineage files inside the raw session, then
runs the generic export and adds only a sanitized SHA/fingerprint summary to the public bundle.
Raw P1 source/staging manifests are never copied into the review bundle.
"""

from __future__ import annotations

import argparse
import json
import shutil
import sys
from pathlib import Path
from typing import Any, Optional, Sequence

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import export_device_evidence
import prepare_p1_candidate_device

P1_REVIEW_LINEAGE_FILE = "p1-review-lineage.json"
P1_REVIEW_STATE = "SANITIZED_P1_REVIEW_LINEAGE"
P1_REVIEW_PROFILE = "p1-final-gate-lineage-v1"
EXPECTED_TASKS = list(prepare_p1_candidate_device.EXPECTED_TASKS)
REQUIRED_SESSION_FILES = {
    "p1Manifest": prepare_p1_candidate_device.BOUND_P1_MANIFEST_FILE,
    "stagingReport": prepare_p1_candidate_device.BOUND_STAGING_REPORT_FILE,
    "stagingLineage": prepare_p1_candidate_device.BOUND_LINEAGE_REPORT_FILE,
    "candidateManifest": prepare_p1_candidate_device.prepare_candidate_device.BOUND_MANIFEST_FILE,
}


class P1EvidenceExportError(RuntimeError):
    pass


def _read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file() or path.is_symlink():
        raise P1EvidenceExportError(f"{label} is missing or is not a regular file: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1EvidenceExportError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1EvidenceExportError(f"{label} root must be a JSON object")
    return payload


def _require_false(payload: dict[str, Any], key: str, label: str) -> None:
    if payload.get(key) is not False:
        raise P1EvidenceExportError(f"{label}.{key} must remain JSON false")


def _require_true(payload: dict[str, Any], key: str, label: str) -> None:
    if payload.get(key) is not True:
        raise P1EvidenceExportError(f"{label}.{key} must be JSON true")


def _sha40(value: Any, label: str) -> str:
    try:
        return prepare_p1_candidate_device._sha40(value, label)
    except prepare_p1_candidate_device.P1CandidatePrepareError as exc:
        raise P1EvidenceExportError(str(exc)) from exc


def _sha256(value: Any, label: str) -> str:
    try:
        return prepare_p1_candidate_device._sha256(value, label)
    except prepare_p1_candidate_device.P1CandidatePrepareError as exc:
        raise P1EvidenceExportError(str(exc)) from exc


def _require_tasks(value: Any, label: str) -> list[str]:
    if not isinstance(value, list) or value != EXPECTED_TASKS:
        raise P1EvidenceExportError(f"{label} must equal the ordered six-task P1 scope: {EXPECTED_TASKS}")
    return list(value)


def validate_p1_session(session_dir: Path) -> dict[str, Any]:
    session_dir = session_dir.expanduser().resolve()
    session = _read_json(session_dir / export_device_evidence.SESSION_FILE, "P1 device session")
    p1 = session.get("p1Lineage")
    if not isinstance(p1, dict):
        raise P1EvidenceExportError(
            "Device session has no p1Lineage binding; prepare it with prepare_p1_candidate_device.py before P1 export"
        )
    if p1.get("schemaVersion") != 1 or p1.get("state") != "P1_LINEAGE_BOUND_FOR_PHYSICAL_DEVICE_EVIDENCE":
        raise P1EvidenceExportError(f"Unsupported session.p1Lineage schema/state: {p1.get('schemaVersion')!r}/{p1.get('state')!r}")
    _require_true(p1, "readyForCheckpointCapture", "session.p1Lineage")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(p1, key, "session.p1Lineage")
    _require_tasks(p1.get("coveredTasks"), "session.p1Lineage.coveredTasks")

    staging_source_sha = _sha40(p1.get("stagingSourceGitSha"), "session.p1Lineage.stagingSourceGitSha")
    candidate_sha = _sha40(p1.get("candidateGitSha"), "session.p1Lineage.candidateGitSha")
    direct_parent_sha = _sha40(p1.get("directParentGitSha"), "session.p1Lineage.directParentGitSha")
    apk_sha = _sha256(p1.get("apkSha256"), "session.p1Lineage.apkSha256")
    if candidate_sha == staging_source_sha or direct_parent_sha != staging_source_sha:
        raise P1EvidenceExportError(
            f"P1 lineage relation is invalid: stagingSource={staging_source_sha} directParent={direct_parent_sha} candidate={candidate_sha}"
        )

    candidate = session.get("candidate")
    if not isinstance(candidate, dict):
        raise P1EvidenceExportError("Device session is missing generic candidate context")
    if _sha40(candidate.get("gitSha"), "session.candidate.gitSha") != candidate_sha:
        raise P1EvidenceExportError("Generic session candidate Git SHA does not match p1Lineage")
    if _sha256(candidate.get("apkSha256"), "session.candidate.apkSha256") != apk_sha:
        raise P1EvidenceExportError("Generic session candidate APK SHA-256 does not match p1Lineage")
    if candidate.get("candidateType") != prepare_p1_candidate_device.prepare_candidate_device.LOCAL_CANDIDATE_TYPE:
        raise P1EvidenceExportError("P1 review requires the local licensed-Windows candidate type")
    _require_false(candidate, "verified", "session.candidate")

    session_apk = session.get("apk")
    if not isinstance(session_apk, dict) or _sha256(session_apk.get("sha256"), "session.apk.sha256") != apk_sha:
        raise P1EvidenceExportError("Session APK metadata does not match p1Lineage")

    performance_tier = str(session.get("performanceTier") or "").strip().lower()
    if performance_tier not in {"low", "mid", "high"}:
        raise P1EvidenceExportError(f"session.performanceTier must be low/mid/high, found {performance_tier!r}")

    files = p1.get("files")
    if not isinstance(files, dict):
        raise P1EvidenceExportError("session.p1Lineage.files must be a JSON object")
    file_hashes: dict[str, str] = {}
    for key, expected_name in REQUIRED_SESSION_FILES.items():
        record = files.get(key)
        if not isinstance(record, dict):
            raise P1EvidenceExportError(f"session.p1Lineage.files.{key} is missing")
        if record.get("fileName") != expected_name:
            raise P1EvidenceExportError(
                f"session.p1Lineage.files.{key}.fileName mismatch: expected={expected_name!r} actual={record.get('fileName')!r}"
            )
        declared_hash = _sha256(record.get("sha256"), f"session.p1Lineage.files.{key}.sha256")
        path = session_dir / expected_name
        if not path.is_file() or path.is_symlink() or path.stat().st_size <= 0:
            raise P1EvidenceExportError(f"Bound P1 provenance file is missing/invalid: {path}")
        actual_hash = export_device_evidence.sha256_file(path)
        if actual_hash != declared_hash:
            raise P1EvidenceExportError(
                f"Bound P1 provenance SHA mismatch for {expected_name}: session={declared_hash} actual={actual_hash}"
            )
        file_hashes[key] = actual_hash

    candidate_manifest_hash = _sha256(
        candidate.get("manifest", {}).get("sha256") if isinstance(candidate.get("manifest"), dict) else None,
        "session.candidate.manifest.sha256",
    )
    if file_hashes["candidateManifest"] != candidate_manifest_hash:
        raise P1EvidenceExportError("P1 bound generic candidate-manifest hash differs from session.candidate.manifest.sha256")

    # Re-validate the bound P1 JSON semantics so matching session hashes cannot mask
    # a jointly-tampered provenance set.
    envelope = _read_json(session_dir / REQUIRED_SESSION_FILES["p1Manifest"], "Bound P1 staged-candidate manifest")
    if envelope.get("schemaVersion") != 1 or envelope.get("candidateType") != prepare_p1_candidate_device.P1_CANDIDATE_TYPE:
        raise P1EvidenceExportError("Bound P1 staged-candidate manifest has an unsupported schema/candidateType")
    if envelope.get("verdict") != prepare_p1_candidate_device.P1_VERDICT:
        raise P1EvidenceExportError("Bound P1 staged-candidate manifest has an unexpected verdict")
    _require_true(envelope, "readyForDeviceEvidence", "boundP1Manifest")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(envelope, key, "boundP1Manifest")
    _require_tasks(envelope.get("coveredTasks"), "boundP1Manifest.coveredTasks")
    if _sha40(envelope.get("stagingSourceGitSha"), "boundP1Manifest.stagingSourceGitSha") != staging_source_sha:
        raise P1EvidenceExportError("Bound P1 manifest staging-source SHA differs from session.p1Lineage")
    if _sha40(envelope.get("candidateGitSha"), "boundP1Manifest.candidateGitSha") != candidate_sha:
        raise P1EvidenceExportError("Bound P1 manifest candidate SHA differs from session.p1Lineage")
    if _sha40(envelope.get("directParentGitSha"), "boundP1Manifest.directParentGitSha") != direct_parent_sha:
        raise P1EvidenceExportError("Bound P1 manifest direct-parent SHA differs from session.p1Lineage")
    if _sha256(envelope.get("apkSha256"), "boundP1Manifest.apkSha256") != apk_sha:
        raise P1EvidenceExportError("Bound P1 manifest APK SHA differs from session.p1Lineage")

    for envelope_key, session_key in (
        ("stagingReport", "stagingReport"),
        ("stagingLineage", "stagingLineage"),
        ("localCandidateManifest", "candidateManifest"),
    ):
        record = envelope.get(envelope_key)
        if not isinstance(record, dict):
            raise P1EvidenceExportError(f"Bound P1 manifest is missing {envelope_key}")
        if _sha256(record.get("sha256"), f"boundP1Manifest.{envelope_key}.sha256") != file_hashes[session_key]:
            raise P1EvidenceExportError(f"Bound P1 manifest {envelope_key} hash differs from the session-bound bytes")

    staging = _read_json(session_dir / REQUIRED_SESSION_FILES["stagingReport"], "Bound P1 staging report")
    if staging.get("schemaVersion") != 2 or staging.get("state") != prepare_p1_candidate_device.STAGING_STATE:
        raise P1EvidenceExportError("Bound P1 staging report has an unsupported schema/state")
    if _sha40(staging.get("gitSha"), "boundStaging.gitSha") != staging_source_sha:
        raise P1EvidenceExportError("Bound staging report Git SHA differs from session.p1Lineage")
    _require_tasks(staging.get("coveredTasks"), "boundStaging.coveredTasks")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible", "candidateBuildStarted"):
        _require_false(staging, key, "boundStaging")

    lineage = _read_json(session_dir / REQUIRED_SESSION_FILES["stagingLineage"], "Bound P1 staging lineage")
    if lineage.get("schemaVersion") != 1 or lineage.get("state") != prepare_p1_candidate_device.P1_LINEAGE_STATE:
        raise P1EvidenceExportError("Bound P1 staging lineage has an unsupported schema/state")
    if _sha40(lineage.get("stagingSourceGitSha"), "boundLineage.stagingSourceGitSha") != staging_source_sha:
        raise P1EvidenceExportError("Bound lineage staging-source SHA differs from session.p1Lineage")
    if _sha40(lineage.get("candidateGitSha"), "boundLineage.candidateGitSha") != candidate_sha:
        raise P1EvidenceExportError("Bound lineage candidate SHA differs from session.p1Lineage")
    if _sha40(lineage.get("directParentGitSha"), "boundLineage.directParentGitSha") != direct_parent_sha:
        raise P1EvidenceExportError("Bound lineage direct-parent SHA differs from session.p1Lineage")
    if _sha256(lineage.get("stagingReportSha256"), "boundLineage.stagingReportSha256") != file_hashes["stagingReport"]:
        raise P1EvidenceExportError("Bound lineage staging-report hash differs from the exact session-bound staging report")
    _require_tasks(lineage.get("coveredTasks"), "boundLineage.coveredTasks")
    _require_true(lineage, "readyForLicensedCandidateTests", "boundLineage")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(lineage, key, "boundLineage")

    return {
        "stagingSourceGitSha": staging_source_sha,
        "candidateGitSha": candidate_sha,
        "directParentGitSha": direct_parent_sha,
        "apkSha256": apk_sha,
        "performanceTier": performance_tier,
        "coveredTasks": list(EXPECTED_TASKS),
        "sourceFileHashes": file_hashes,
    }


def _write_p1_summary(output_dir: Path, review_manifest: dict[str, Any], p1: dict[str, Any]) -> dict[str, Any]:
    summary = {
        "schemaVersion": 1,
        "state": P1_REVIEW_STATE,
        "reviewProfile": P1_REVIEW_PROFILE,
        "verdict": export_device_evidence.EXPECTED_REVIEW_VERDICT,
        "stagingSourceGitSha": p1["stagingSourceGitSha"],
        "candidateGitSha": p1["candidateGitSha"],
        "directParentGitSha": p1["directParentGitSha"],
        "apkSha256": p1["apkSha256"],
        "deviceSerialSha256": review_manifest["deviceSerialSha256"],
        "performanceTier": p1["performanceTier"],
        "coveredTasks": list(EXPECTED_TASKS),
        "sourceArtifactDigests": dict(p1["sourceFileHashes"]),
        "checkpointCount": review_manifest["checkpointCount"],
        "contentReviewVerdict": review_manifest["verdict"],
        "manualReviewRequired": True,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "privacy": {
            "rawP1SessionIncluded": False,
            "rawP1SourceArtifactsIncluded": False,
            "localPathsIncluded": False,
        },
    }
    target = output_dir / P1_REVIEW_LINEAGE_FILE
    export_device_evidence.write_json(target, summary)
    return summary


def export_p1_bundle(session_dir: Path, output_dir: Path, *, force: bool = False) -> dict[str, Any]:
    session_dir = session_dir.expanduser().resolve()
    output_dir = output_dir.expanduser().resolve()
    p1 = validate_p1_session(session_dir)

    created_output = False
    try:
        review_manifest = export_device_evidence.export_bundle(session_dir, output_dir, force=force)
        created_output = True
        candidate = review_manifest.get("candidate")
        if not isinstance(candidate, dict):
            raise P1EvidenceExportError("Generic review manifest is missing candidate binding")
        if _sha40(candidate.get("gitSha"), "review.candidate.gitSha") != p1["candidateGitSha"]:
            raise P1EvidenceExportError("Generic review candidate Git SHA differs from P1 lineage")
        if _sha256(candidate.get("apkSha256"), "review.candidate.apkSha256") != p1["apkSha256"]:
            raise P1EvidenceExportError("Generic review candidate APK SHA differs from P1 lineage")
        if candidate.get("candidateType") != prepare_p1_candidate_device.prepare_candidate_device.LOCAL_CANDIDATE_TYPE:
            raise P1EvidenceExportError("P1 review bundle requires local licensed-Windows candidate evidence")

        summary = _write_p1_summary(output_dir, review_manifest, p1)
        relative = P1_REVIEW_LINEAGE_FILE
        record = export_device_evidence._content_record(output_dir / relative)
        content_files = dict(review_manifest["contentFiles"])
        if relative in content_files:
            raise P1EvidenceExportError(f"P1 review lineage path collides with generic content: {relative}")
        content_files[relative] = record
        copied_files = sorted([*review_manifest["copiedFiles"], relative])
        content_set_sha = export_device_evidence._content_set_sha256(content_files)

        review_manifest["reviewProfile"] = P1_REVIEW_PROFILE
        review_manifest["copiedFiles"] = copied_files
        review_manifest["contentFiles"] = content_files
        review_manifest["contentSetSha256"] = content_set_sha
        review_manifest["p1Lineage"] = {
            "schemaVersion": 1,
            "state": "P1_REVIEW_LINEAGE_ATTACHED",
            "fileName": relative,
            "sha256": record["sha256"],
            "stagingSourceGitSha": p1["stagingSourceGitSha"],
            "candidateGitSha": p1["candidateGitSha"],
            "directParentGitSha": p1["directParentGitSha"],
            "apkSha256": p1["apkSha256"],
            "performanceTier": p1["performanceTier"],
            "coveredTasks": list(EXPECTED_TASKS),
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
        }
        excluded = list(review_manifest.get("excludedByPolicy") or [])
        for name in (
            prepare_p1_candidate_device.BOUND_P1_MANIFEST_FILE,
            prepare_p1_candidate_device.BOUND_STAGING_REPORT_FILE,
            prepare_p1_candidate_device.BOUND_LINEAGE_REPORT_FILE,
        ):
            if name not in excluded:
                excluded.append(name)
        review_manifest["excludedByPolicy"] = excluded
        privacy = review_manifest.get("privacy")
        if not isinstance(privacy, dict):
            raise P1EvidenceExportError("Generic review manifest is missing privacy contract")
        privacy.update(
            {
                "rawP1SessionIncluded": False,
                "rawP1SourceArtifactsIncluded": False,
                "sanitizedP1LineageIncluded": True,
            }
        )
        export_device_evidence.write_json(output_dir / export_device_evidence.REVIEW_MANIFEST_FILE, review_manifest)

        # Generic integrity verification remains applicable after adding the P1 summary
        # because it is added to the same contentFiles/contentSet contract.
        import verify_device_review_bundle

        generic_verified = verify_device_review_bundle.verify_bundle(
            output_dir,
            expected_git_sha=p1["candidateGitSha"],
            expected_apk_sha=p1["apkSha256"],
        )
        if generic_verified["contentSetSha256"] != content_set_sha:
            raise P1EvidenceExportError("Generic verification returned a different contentSetSha256 after P1 augmentation")
        if summary["deviceSerialSha256"] != generic_verified["deviceSerialSha256"]:
            raise P1EvidenceExportError("Sanitized P1 summary device hash differs from generic verified bundle")
        return review_manifest
    except Exception:
        if created_output and output_dir.is_dir():
            shutil.rmtree(output_dir, ignore_errors=True)
        raise


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--session", required=True, help="Raw Step 11 P1 physical-device evidence session")
    parser.add_argument("--output", required=True, help="Destination sanitized P1 review bundle")
    parser.add_argument("--force", action="store_true", help="Replace an existing output directory")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        manifest = export_p1_bundle(Path(args.session), Path(args.output), force=args.force)
        red_flags = int(manifest.get("automatedRedFlagCount", 0))
        p1 = manifest["p1Lineage"]
        print(
            "AFAREET_P1_DEVICE_REVIEW_BUNDLE_EXPORTED "
            f"stagingSourceGitSha={p1['stagingSourceGitSha']} candidateGitSha={p1['candidateGitSha']} "
            f"apkSha256={p1['apkSha256']} checkpoints={manifest['checkpointCount']} "
            f"contentSetSha256={manifest['contentSetSha256']} tasks=6 verdict={manifest['verdict']} verified=false"
        )
        return 2 if red_flags > 0 else 0
    except (
        P1EvidenceExportError,
        export_device_evidence.EvidenceExportError,
        OSError,
        ValueError,
    ) as exc:
        print(f"AFAREET_P1_DEVICE_REVIEW_EXPORT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
