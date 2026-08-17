#!/usr/bin/env python3
"""Prepare P1 physical-device evidence from a fully bound staged-candidate lineage.

This is the authoritative P1 wrapper around prepare_candidate_device.py. Before ADB is
allowed to start it validates the Step 10 P1 envelope, exact staging report bytes, exact
staging-lineage bytes, READY-handoff authorization fingerprints, generic local-candidate
manifest, and APK bytes as one chain. After the generic device session is prepared, it
copies the P1 provenance records into the session and records their hashes. It never
marks runtime/device/owner gates VERIFIED.
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

import prepare_candidate_device

P1_CANDIDATE_TYPE = "p1-staged-local-windows-licensed-unity"
P1_VERDICT = "P1_STAGED_CANDIDATE_READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
P1_LINEAGE_STATE = "STAGING_PARENT_BOUND_TO_CANDIDATE"
STAGING_STATE = "STAGED_FOR_COMMIT_NOT_CANDIDATE"
EXPECTED_TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")

BOUND_P1_MANIFEST_FILE = "p1-staged-candidate-manifest.json"
BOUND_STAGING_REPORT_FILE = "p1-staging-handoff.json"
BOUND_LINEAGE_REPORT_FILE = "p1-staging-lineage.json"


class P1CandidatePrepareError(RuntimeError):
    pass


def _read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file():
        raise P1CandidatePrepareError(f"{label} is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1CandidatePrepareError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1CandidatePrepareError(f"{label} root must be a JSON object")
    return payload


def _sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise P1CandidatePrepareError(f"{label} must be a full 40-character Git SHA, found {value!r}")
    return text


def _sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise P1CandidatePrepareError(f"{label} must be a SHA-256 hex digest, found {value!r}")
    return text


def _require_false(payload: dict[str, Any], key: str, label: str) -> None:
    if payload.get(key) is not False:
        raise P1CandidatePrepareError(f"{label}.{key} must remain JSON false")


def _require_true(payload: dict[str, Any], key: str, label: str) -> None:
    if payload.get(key) is not True:
        raise P1CandidatePrepareError(f"{label}.{key} must be JSON true")


def _require_exact_tasks(value: Any, label: str) -> list[str]:
    if not isinstance(value, list) or value != EXPECTED_TASKS:
        raise P1CandidatePrepareError(
            f"{label} must contain exactly the ordered six-task P1 visual/runtime scope: {EXPECTED_TASKS}"
        )
    return list(value)


def _record(payload: dict[str, Any], key: str, label: str) -> dict[str, Any]:
    value = payload.get(key)
    if not isinstance(value, dict):
        raise P1CandidatePrepareError(f"{label}.{key} must be a JSON object")
    return value


def _authorization(payload: dict[str, Any], label: str, expected_source_sha: str) -> dict[str, str]:
    record = _record(payload, "stagingAuthorization", label)
    source_sha = _sha40(record.get("authorizationSourceGitSha"), f"{label}.stagingAuthorization.authorizationSourceGitSha")
    if source_sha != expected_source_sha:
        raise P1CandidatePrepareError(
            f"{label}.stagingAuthorization source SHA mismatch: expected={expected_source_sha} actual={source_sha}"
        )
    return {
        "authorizationSourceGitSha": source_sha,
        "handoffPacketSha256": _sha256(
            record.get("handoffPacketSha256"), f"{label}.stagingAuthorization.handoffPacketSha256"
        ),
        "nativeHandoffVerificationSha256": _sha256(
            record.get("nativeHandoffVerificationSha256"),
            f"{label}.stagingAuthorization.nativeHandoffVerificationSha256",
        ),
        "operatorChainSha256": _sha256(
            record.get("operatorChainSha256"), f"{label}.stagingAuthorization.operatorChainSha256"
        ),
    }


def _resolve_record_path(
    record: dict[str, Any],
    override: Optional[Path],
    envelope_path: Path,
    label: str,
) -> Path:
    if override is not None:
        path = override.expanduser().resolve()
    else:
        raw = str(record.get("path") or "").strip()
        if not raw:
            raise P1CandidatePrepareError(f"{label}.path is missing; pass the matching override for a moved bundle")
        candidate = Path(raw).expanduser()
        path = candidate.resolve() if candidate.is_absolute() else (envelope_path.parent / candidate).resolve()
    if not path.is_file() or path.stat().st_size <= 0:
        raise P1CandidatePrepareError(f"{label} file is missing or empty: {path}")
    expected = _sha256(record.get("sha256"), f"{label}.sha256")
    actual = prepare_candidate_device.sha256_file(path)
    if actual != expected:
        raise P1CandidatePrepareError(f"{label} SHA-256 mismatch: envelope={expected} actual={actual}")
    return path


def validate_p1_chain(
    p1_manifest_path: Path,
    *,
    candidate_manifest_override: Optional[Path] = None,
    staging_report_override: Optional[Path] = None,
    staging_lineage_override: Optional[Path] = None,
    apk_override: Optional[Path] = None,
) -> dict[str, Any]:
    p1_manifest_path = p1_manifest_path.expanduser().resolve()
    envelope = _read_json(p1_manifest_path, "P1 staged candidate manifest")

    if envelope.get("schemaVersion") != 1:
        raise P1CandidatePrepareError(f"Unsupported P1 staged candidate schemaVersion: {envelope.get('schemaVersion')!r}")
    if envelope.get("candidateType") != P1_CANDIDATE_TYPE:
        raise P1CandidatePrepareError(f"Unexpected P1 candidateType: {envelope.get('candidateType')!r}")
    if envelope.get("verdict") != P1_VERDICT:
        raise P1CandidatePrepareError(f"Unexpected P1 candidate verdict: {envelope.get('verdict')!r}")
    _require_true(envelope, "readyForDeviceEvidence", "p1Manifest")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(envelope, key, "p1Manifest")
    _require_exact_tasks(envelope.get("coveredTasks"), "p1Manifest.coveredTasks")

    staging_source_sha = _sha40(envelope.get("stagingSourceGitSha"), "p1Manifest.stagingSourceGitSha")
    candidate_sha = _sha40(envelope.get("candidateGitSha"), "p1Manifest.candidateGitSha")
    direct_parent_sha = _sha40(envelope.get("directParentGitSha"), "p1Manifest.directParentGitSha")
    if candidate_sha == staging_source_sha:
        raise P1CandidatePrepareError("P1 candidate SHA must differ from the staging-source SHA")
    if direct_parent_sha != staging_source_sha:
        raise P1CandidatePrepareError(
            f"P1 direct-parent lineage mismatch: stagingSource={staging_source_sha} directParent={direct_parent_sha}"
        )
    envelope_apk_sha = _sha256(envelope.get("apkSha256"), "p1Manifest.apkSha256")
    envelope_authorization = _authorization(envelope, "p1Manifest", staging_source_sha)

    staging_record = _record(envelope, "stagingReport", "p1Manifest")
    if staging_record.get("schemaVersion") != 3:
        raise P1CandidatePrepareError("p1Manifest.stagingReport.schemaVersion must be 3")
    lineage_record = _record(envelope, "stagingLineage", "p1Manifest")
    if lineage_record.get("state") != P1_LINEAGE_STATE:
        raise P1CandidatePrepareError(
            f"Unexpected p1Manifest.stagingLineage.state: {lineage_record.get('state')!r}"
        )
    candidate_record = _record(envelope, "localCandidateManifest", "p1Manifest")

    staging_report_path = _resolve_record_path(
        staging_record, staging_report_override, p1_manifest_path, "p1Manifest.stagingReport"
    )
    lineage_path = _resolve_record_path(
        lineage_record, staging_lineage_override, p1_manifest_path, "p1Manifest.stagingLineage"
    )
    candidate_manifest_path = _resolve_record_path(
        candidate_record, candidate_manifest_override, p1_manifest_path, "p1Manifest.localCandidateManifest"
    )

    staging = _read_json(staging_report_path, "P1 staging report")
    if staging.get("schemaVersion") != 3 or staging.get("state") != STAGING_STATE:
        raise P1CandidatePrepareError("P1 staging report must be schema 3 in STAGED_FOR_COMMIT_NOT_CANDIDATE state")
    if _sha40(staging.get("gitSha"), "stagingReport.gitSha") != staging_source_sha:
        raise P1CandidatePrepareError("P1 staging report Git SHA does not match the staged-candidate envelope")
    if _sha40(staging.get("authorizationSourceGitSha"), "stagingReport.authorizationSourceGitSha") != staging_source_sha:
        raise P1CandidatePrepareError("P1 staging report authorizationSourceGitSha does not match the staging source SHA")
    staging_authorization = {
        "authorizationSourceGitSha": staging_source_sha,
        "handoffPacketSha256": _sha256(staging.get("handoffPacketSha256"), "stagingReport.handoffPacketSha256"),
        "nativeHandoffVerificationSha256": _sha256(
            staging.get("nativeHandoffVerificationSha256"), "stagingReport.nativeHandoffVerificationSha256"
        ),
        "operatorChainSha256": _sha256(staging.get("operatorChainSha256"), "stagingReport.operatorChainSha256"),
    }
    if staging_authorization != envelope_authorization:
        raise P1CandidatePrepareError("P1 staging report authorization fingerprints do not match the staged-candidate envelope")
    _require_exact_tasks(staging.get("coveredTasks"), "stagingReport.coveredTasks")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible", "candidateBuildStarted"):
        _require_false(staging, key, "stagingReport")
    task_evidence = staging.get("taskEvidence")
    if not isinstance(task_evidence, list) or len(task_evidence) != len(EXPECTED_TASKS):
        raise P1CandidatePrepareError("P1 staging report must contain exactly six taskEvidence records")
    seen: set[str] = set()
    for item in task_evidence:
        if not isinstance(item, dict):
            raise P1CandidatePrepareError("P1 staging taskEvidence entries must be JSON objects")
        task_id = str(item.get("taskId") or "")
        if task_id not in EXPECTED_TASKS or task_id in seen:
            raise P1CandidatePrepareError(f"Invalid or duplicate P1 staging taskEvidence taskId: {task_id!r}")
        seen.add(task_id)
        for key in ("verified", "runtimeVerified", "ownerAccepted"):
            _require_false(item, key, f"stagingReport.taskEvidence[{task_id}]")
    if seen != set(EXPECTED_TASKS):
        raise P1CandidatePrepareError("P1 staging taskEvidence scope is incomplete")

    lineage = _read_json(lineage_path, "P1 staging lineage report")
    if lineage.get("schemaVersion") != 1 or lineage.get("state") != P1_LINEAGE_STATE:
        raise P1CandidatePrepareError("P1 staging lineage report has an unsupported schema/state")
    if lineage.get("stagingReportSchemaVersion") != 3:
        raise P1CandidatePrepareError("P1 staging lineage must bind stagingReportSchemaVersion=3")
    if _sha40(lineage.get("stagingSourceGitSha"), "lineage.stagingSourceGitSha") != staging_source_sha:
        raise P1CandidatePrepareError("Lineage staging-source SHA does not match the P1 envelope")
    if _sha40(lineage.get("candidateGitSha"), "lineage.candidateGitSha") != candidate_sha:
        raise P1CandidatePrepareError("Lineage candidate SHA does not match the P1 envelope")
    if _sha40(lineage.get("directParentGitSha"), "lineage.directParentGitSha") != direct_parent_sha:
        raise P1CandidatePrepareError("Lineage direct-parent SHA does not match the P1 envelope")
    if _sha256(lineage.get("stagingReportSha256"), "lineage.stagingReportSha256") != prepare_candidate_device.sha256_file(staging_report_path):
        raise P1CandidatePrepareError("Lineage stagingReportSha256 does not match the exact staging-report bytes")
    lineage_authorization = _authorization(lineage, "lineage", staging_source_sha)
    if lineage_authorization != envelope_authorization:
        raise P1CandidatePrepareError("P1 staging lineage authorization fingerprints do not match the staged-candidate envelope")
    _require_exact_tasks(lineage.get("coveredTasks"), "lineage.coveredTasks")
    _require_true(lineage, "readyForLicensedCandidateTests", "lineage")
    for key in ("verified", "runtimeVerified", "ownerAccepted", "publicationEligible"):
        _require_false(lineage, key, "lineage")

    generic_manifest = prepare_candidate_device.read_json(candidate_manifest_path)
    candidate = prepare_candidate_device.resolve_candidate(
        generic_manifest,
        candidate_manifest_path,
        apk_override,
    )
    if candidate["candidateType"] != prepare_candidate_device.LOCAL_CANDIDATE_TYPE:
        raise P1CandidatePrepareError(
            f"P1 staged device evidence requires a local licensed-Windows candidate, found {candidate['candidateType']!r}"
        )
    if candidate["gitSha"] != candidate_sha:
        raise P1CandidatePrepareError(
            f"Generic candidate SHA does not match P1 lineage: generic={candidate['gitSha']} p1={candidate_sha}"
        )
    if candidate["apkSha256"] != envelope_apk_sha:
        raise P1CandidatePrepareError(
            f"Generic candidate APK SHA-256 does not match P1 envelope: generic={candidate['apkSha256']} p1={envelope_apk_sha}"
        )

    return {
        "p1ManifestPath": p1_manifest_path,
        "p1ManifestSha256": prepare_candidate_device.sha256_file(p1_manifest_path),
        "stagingReportPath": staging_report_path,
        "stagingReportSha256": prepare_candidate_device.sha256_file(staging_report_path),
        "lineagePath": lineage_path,
        "lineageSha256": prepare_candidate_device.sha256_file(lineage_path),
        "candidateManifestPath": candidate_manifest_path,
        "candidateManifestSha256": prepare_candidate_device.sha256_file(candidate_manifest_path),
        "candidate": candidate,
        "stagingSourceGitSha": staging_source_sha,
        "candidateGitSha": candidate_sha,
        "directParentGitSha": direct_parent_sha,
        "apkSha256": envelope_apk_sha,
        "stagingAuthorization": dict(envelope_authorization),
        "coveredTasks": list(EXPECTED_TASKS),
    }


def bind_p1_chain_to_session(chain: dict[str, Any], output_dir: Path, device_evidence: Any) -> dict[str, Any]:
    output_dir = output_dir.expanduser().resolve()
    session_path = output_dir / prepare_candidate_device.SESSION_FILE
    session = _read_json(session_path, "Prepared device evidence session")
    candidate_context = session.get("candidate")
    if not isinstance(candidate_context, dict):
        raise P1CandidatePrepareError("Prepared device session is missing generic candidate context")
    if str(candidate_context.get("gitSha") or "").lower() != chain["candidateGitSha"]:
        raise P1CandidatePrepareError("Prepared session candidate SHA does not match P1 lineage")
    if str(candidate_context.get("apkSha256") or "").lower() != chain["apkSha256"]:
        raise P1CandidatePrepareError("Prepared session APK SHA-256 does not match P1 lineage")
    if candidate_context.get("verified") is not False:
        raise P1CandidatePrepareError("Prepared generic candidate context must remain unverified")

    bound_files = (
        ("p1Manifest", chain["p1ManifestPath"], BOUND_P1_MANIFEST_FILE, chain["p1ManifestSha256"]),
        ("stagingReport", chain["stagingReportPath"], BOUND_STAGING_REPORT_FILE, chain["stagingReportSha256"]),
        ("stagingLineage", chain["lineagePath"], BOUND_LINEAGE_REPORT_FILE, chain["lineageSha256"]),
    )
    file_context: dict[str, Any] = {}
    for key, source, file_name, expected_hash in bound_files:
        destination = output_dir / file_name
        destination.write_bytes(Path(source).read_bytes())
        actual_hash = prepare_candidate_device.sha256_file(destination)
        if actual_hash != expected_hash:
            raise P1CandidatePrepareError(
                f"Bound P1 session file hash mismatch for {file_name}: expected={expected_hash} actual={actual_hash}"
            )
        file_context[key] = {"fileName": file_name, "sha256": actual_hash}

    bound_generic = output_dir / prepare_candidate_device.BOUND_MANIFEST_FILE
    if not bound_generic.is_file():
        raise P1CandidatePrepareError("Prepared session is missing the generic bound candidate manifest")
    bound_generic_hash = prepare_candidate_device.sha256_file(bound_generic)
    if bound_generic_hash != chain["candidateManifestSha256"]:
        raise P1CandidatePrepareError("Bound generic candidate manifest hash does not match P1 lineage")
    file_context["candidateManifest"] = {
        "fileName": prepare_candidate_device.BOUND_MANIFEST_FILE,
        "sha256": bound_generic_hash,
    }

    context = {
        "schemaVersion": 1,
        "state": "P1_LINEAGE_BOUND_FOR_PHYSICAL_DEVICE_EVIDENCE",
        "stagingSourceGitSha": chain["stagingSourceGitSha"],
        "candidateGitSha": chain["candidateGitSha"],
        "directParentGitSha": chain["directParentGitSha"],
        "apkSha256": chain["apkSha256"],
        "stagingAuthorization": dict(chain["stagingAuthorization"]),
        "coveredTasks": list(EXPECTED_TASKS),
        "files": file_context,
        "readyForCheckpointCapture": True,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "nextAction": "Capture the required physical-device checkpoints on this exact APK; this lineage and READY-handoff authorization binding is not gate approval.",
    }
    session["p1Lineage"] = context
    device_evidence.write_json(session_path, session)
    return context


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--p1-candidate-manifest", required=True, help="Step 10 p1-staged-candidate-manifest.json")
    parser.add_argument("--candidate-manifest", help="Override local-candidate-manifest.json path for a moved bundle")
    parser.add_argument("--staging-report", help="Override schema-v3 staging report path for a moved bundle")
    parser.add_argument("--staging-lineage", help="Override staging-lineage report path for a moved bundle")
    parser.add_argument("--apk", help="Optional APK path override for a moved bundle; bytes must match exactly")
    parser.add_argument("--output", required=True, help="Output directory for the physical-device evidence session")
    parser.add_argument("--serial", help="ADB serial when more than one authorized device is connected")
    parser.add_argument(
        "--performance-tier",
        required=True,
        choices=("low", "mid", "high"),
        help="Bind the device session to the UPER-001 capability tier before checkpoint capture",
    )
    parser.add_argument("--allow-emulator", action="store_true", help="Harness debugging only; cannot satisfy P1 gates")
    parser.add_argument("--force", action="store_true", help="Replace an existing evidence session")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        chain = validate_p1_chain(
            Path(args.p1_candidate_manifest),
            candidate_manifest_override=Path(args.candidate_manifest) if args.candidate_manifest else None,
            staging_report_override=Path(args.staging_report) if args.staging_report else None,
            staging_lineage_override=Path(args.staging_lineage) if args.staging_lineage else None,
            apk_override=Path(args.apk) if args.apk else None,
        )
        print(
            "AFAREET_P1_DEVICE_LINEAGE_PRECHECK_OK "
            f"stagingSourceGitSha={chain['stagingSourceGitSha']} candidateGitSha={chain['candidateGitSha']} "
            f"apkSha256={chain['apkSha256']} packetSha256={chain['stagingAuthorization']['handoffPacketSha256']} "
            "tasks=6 verified=false"
        )

        child_args = [
            "--candidate-manifest",
            str(chain["candidateManifestPath"]),
            "--apk",
            str(chain["candidate"]["apkPath"]),
            "--output",
            args.output,
            "--performance-tier",
            args.performance_tier,
        ]
        if args.serial:
            child_args.extend(["--serial", args.serial])
        if args.allow_emulator:
            child_args.append("--allow-emulator")
        if args.force:
            child_args.append("--force")
        prepare_code = int(prepare_candidate_device.main(child_args))
        if prepare_code != 0:
            return prepare_code

        import device_evidence

        context = bind_p1_chain_to_session(chain, Path(args.output), device_evidence)
        print(
            "AFAREET_P1_DEVICE_SESSION_BOUND "
            f"candidateGitSha={context['candidateGitSha']} apkSha256={context['apkSha256']} "
            f"packetSha256={context['stagingAuthorization']['handoffPacketSha256']} "
            f"tasks=6 readyForCheckpointCapture=true verified=false session={Path(args.output).expanduser().resolve()}"
        )
        return 0
    except (
        P1CandidatePrepareError,
        prepare_candidate_device.CandidatePrepareError,
        OSError,
        ValueError,
    ) as exc:
        print(f"AFAREET_P1_DEVICE_PREPARE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
