#!/usr/bin/env python3
"""Prepare physical-device evidence only from an integrity-checked candidate.

This wrapper accepts candidate manifests produced by either the licensed-Windows
local path or the GitHub Unity Production CI artifact-binding path. It fails
closed if candidate provenance or APK bytes do not match the supported contract,
then hands the exact APK to the existing ADB evidence collector. After ADB
prepare succeeds it persists the validated candidate provenance and an exact
copy of the source manifest into the evidence session. It never marks a
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
LOCAL_CANDIDATE_TYPE = "local-windows-licensed-unity"
CI_CANDIDATE_TYPE = "github-actions-unity-ci"
ALLOWED_CANDIDATE_TYPES = {LOCAL_CANDIDATE_TYPE, CI_CANDIDATE_TYPE}
EXPECTED_CI_REPOSITORY = "walidatiyaai2025-gif/3fareet"
EXPECTED_CI_WORKFLOW = "Unity Production CI"
ALLOWED_CI_EVENTS = {"pull_request", "push", "workflow_dispatch"}
EXPECTED_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
SESSION_FILE = "session.json"
BOUND_MANIFEST_FILE = "candidate-manifest.json"
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
DIGITS_RE = re.compile(r"^[1-9][0-9]*$")


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


def _positive_id(value: Any, label: str) -> str:
    text = str(value or "").strip()
    if not DIGITS_RE.fullmatch(text):
        raise CandidatePrepareError(f"{label} must be a positive integer string, found {value!r}")
    return text


def _validate_ci_provenance(manifest: dict[str, Any]) -> dict[str, str]:
    github_run = manifest.get("githubRun")
    if not isinstance(github_run, dict):
        raise CandidatePrepareError("GitHub CI candidate is missing githubRun provenance")
    run_id = _positive_id(github_run.get("runId"), "githubRun.runId")
    run_attempt = _positive_id(github_run.get("runAttempt"), "githubRun.runAttempt")
    repository = str(github_run.get("repository") or "").strip()
    workflow = str(github_run.get("workflow") or "").strip()
    event_name = str(github_run.get("eventName") or "").strip()
    ref = str(github_run.get("ref") or "").strip()
    if repository != EXPECTED_CI_REPOSITORY:
        raise CandidatePrepareError(f"Unexpected GitHub candidate repository: {repository!r}")
    if workflow != EXPECTED_CI_WORKFLOW:
        raise CandidatePrepareError(f"Unexpected GitHub candidate workflow: {workflow!r}")
    if event_name not in ALLOWED_CI_EVENTS:
        raise CandidatePrepareError(f"Unexpected GitHub candidate eventName: {event_name!r}")
    if not ref.startswith("refs/"):
        raise CandidatePrepareError(f"GitHub candidate ref must be refs/*, found {ref!r}")
    return {
        "runId": run_id,
        "runAttempt": run_attempt,
        "repository": repository,
        "workflow": workflow,
        "eventName": event_name,
        "ref": ref,
    }


def resolve_candidate(
    manifest: dict[str, Any],
    manifest_path: Path,
    apk_override: Path | None = None,
) -> dict[str, Any]:
    candidate_type = str(manifest.get("candidateType") or "").strip()
    if candidate_type not in ALLOWED_CANDIDATE_TYPES:
        raise CandidatePrepareError(f"Unsupported candidateType: {candidate_type!r}")
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

    github_run = _validate_ci_provenance(manifest) if candidate_type == CI_CANDIDATE_TYPE else None

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
        "candidateType": candidate_type,
        "githubRun": github_run,
    }


def build_session_candidate_context(candidate: dict[str, Any], manifest_path: Path) -> dict[str, Any]:
    context: dict[str, Any] = {
        "schemaVersion": 1,
        "candidateType": candidate["candidateType"],
        "gitSha": candidate["gitSha"],
        "apkSha256": candidate["apkSha256"],
        "releaseEvidenceEligible": True,
        "readyForDeviceEvidence": True,
        "verified": False,
        "verdict": EXPECTED_VERDICT,
        "manifest": {
            "fileName": BOUND_MANIFEST_FILE,
            "sourceFileName": manifest_path.name,
            "sha256": sha256_file(manifest_path),
        },
    }
    if candidate.get("githubRun") is not None:
        context["githubRun"] = dict(candidate["githubRun"])
    return context


def bind_candidate_to_session(
    candidate: dict[str, Any],
    manifest_path: Path,
    output_dir: Path,
    device_evidence: Any,
) -> dict[str, Any]:
    session_path = output_dir / SESSION_FILE
    if not session_path.is_file():
        raise CandidatePrepareError(f"Device evidence prepare did not produce {session_path}")
    try:
        session = json.loads(session_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise CandidatePrepareError(f"Device evidence session is not valid JSON: {session_path}: {exc}") from exc
    if not isinstance(session, dict):
        raise CandidatePrepareError("Device evidence session root must be a JSON object")
    if session.get("packageId") != EXPECTED_PACKAGE_ID:
        raise CandidatePrepareError(f"Prepared session packageId mismatch: {session.get('packageId')!r}")
    session_apk = session.get("apk")
    if not isinstance(session_apk, dict):
        raise CandidatePrepareError("Prepared session is missing APK metadata")
    if str(session_apk.get("sha256") or "").strip().lower() != candidate["apkSha256"]:
        raise CandidatePrepareError("Prepared session APK SHA-256 does not match validated candidate")
    try:
        session_size = int(session_apk.get("sizeBytes", -1))
    except (TypeError, ValueError) as exc:
        raise CandidatePrepareError("Prepared session APK sizeBytes is invalid") from exc
    if session_size != candidate["sizeBytes"]:
        raise CandidatePrepareError("Prepared session APK size does not match validated candidate")

    manifest_bytes = manifest_path.read_bytes()
    bound_manifest = output_dir / BOUND_MANIFEST_FILE
    bound_manifest.write_bytes(manifest_bytes)
    context = build_session_candidate_context(candidate, manifest_path)
    if sha256_file(bound_manifest) != context["manifest"]["sha256"]:
        raise CandidatePrepareError("Copied candidate manifest SHA-256 does not match source manifest")

    session["candidate"] = context
    device_evidence.write_json(session_path, session)
    return context


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Validate an exact local/CI Unity candidate, then prepare its physical-device evidence session."
    )
    parser.add_argument("--candidate-manifest", required=True, help="Path to a local or GitHub-CI candidate manifest")
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
            f"candidateType={candidate['candidateType']} gitSha={candidate['gitSha']} "
            f"apkSha256={candidate['apkSha256']} apk={candidate['apkPath']}"
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
        prepare_code = int(device_evidence.main(child_args))
        if prepare_code != 0:
            return prepare_code

        output_dir = Path(args.output).expanduser().resolve()
        context = bind_candidate_to_session(candidate, manifest_path, output_dir, device_evidence)
        print(
            "AFAREET_CANDIDATE_SESSION_BOUND "
            f"candidateType={context['candidateType']} gitSha={context['gitSha']} "
            f"apkSha256={context['apkSha256']} manifestSha256={context['manifest']['sha256']} "
            f"session={output_dir}"
        )
        return 0
    except (CandidatePrepareError, OSError, ValueError) as exc:
        print(f"AFAREET_CANDIDATE_DEVICE_PREPARE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
