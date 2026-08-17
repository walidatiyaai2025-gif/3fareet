#!/usr/bin/env python3
"""Reconcile post-publication physical-device smoke with one exact P1 publication receipt.

The tool is read-only with respect to device, repository and release state. It validates a
finished dedicated smoke session, requires all evidence to be newer than the recorded human
publication, reuses ``analyze_device_smoke`` for UPER-006 observable budgets, and binds the
result to the exact publication receipt/APK/authorization lineage. Passing this validator is
only evidence for human closure review; it never marks the release VERIFIED or updates pointers.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path
from typing import Any, Optional, Sequence

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import analyze_device_smoke  # noqa: E402
import prepare_p1_post_publication_smoke as prepare  # noqa: E402

SCHEMA_VERSION = 1
RESULT_STATE = "P1_POST_PUBLICATION_SMOKE_RECONCILED"
RESULT_VERDICT = "P1_POST_PUBLICATION_SMOKE_PASSABLE_FOR_HUMAN_CLOSURE_REVIEW"
EXPECTED_CHECKPOINTS = tuple(analyze_device_smoke.REQUIRED_LABELS)
CHECKPOINT_FILES = (
    "screen.png",
    "logcat.txt",
    "meminfo.txt",
    "gfxinfo.txt",
    "thermalservice.txt",
    "battery.txt",
    "activity.txt",
)


class P1PostPublicationSmokeError(RuntimeError):
    pass


def _fail(message: str) -> None:
    raise P1PostPublicationSmokeError(message)


def _read_json(path: Path, label: str) -> dict[str, Any]:
    try:
        return prepare.read_json(path, label)
    except prepare.P1PostPublicationPrepareError as exc:
        raise P1PostPublicationSmokeError(str(exc)) from exc


def _sha256(value: Any, label: str) -> str:
    try:
        return prepare.normalize_sha256(value, label)
    except prepare.P1PostPublicationPrepareError as exc:
        raise P1PostPublicationSmokeError(str(exc)) from exc


def _timestamp(value: Any, label: str):
    try:
        return prepare.parse_timestamp(value, label)
    except prepare.P1PostPublicationPrepareError as exc:
        raise P1PostPublicationSmokeError(str(exc)) from exc


def _sha256_file(path: Path) -> str:
    return prepare.sha256_file(path)


def _require_regular(path: Path, label: str, *, nonempty: bool = False) -> None:
    if not path.is_file() or path.is_symlink():
        _fail(f"{label} is missing, symlinked, or not a regular file: {path}")
    if nonempty and path.stat().st_size <= 0:
        _fail(f"{label} must be non-empty: {path}")


def _context_expected(reconciliation: dict[str, Any]) -> dict[str, Any]:
    return {
        "schemaVersion": 1,
        "profile": prepare.SESSION_PROFILE,
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


def _validate_checkpoint(
    session_dir: Path,
    label: str,
    published_at,
    apk_sha: str,
    device_sha: str,
) -> list[dict[str, Any]]:
    checkpoint_dir = session_dir / "checkpoints" / label
    if not checkpoint_dir.is_dir() or checkpoint_dir.is_symlink():
        _fail(f"required post-publication smoke checkpoint is missing: {label}")
    metadata_path = checkpoint_dir / "checkpoint.json"
    metadata = _read_json(metadata_path, f"{label} checkpoint metadata")
    if metadata.get("schemaVersion") != 1:
        _fail(f"{label}: checkpoint schemaVersion must be 1")
    if metadata.get("label") != label:
        _fail(f"{label}: checkpoint metadata label mismatch")
    if _sha256(metadata.get("apkSha256"), f"{label}.apkSha256") != apk_sha:
        _fail(f"{label}: checkpoint APK SHA differs from published APK")
    if _sha256(metadata.get("deviceSerialSha256"), f"{label}.deviceSerialSha256") != device_sha:
        _fail(f"{label}: checkpoint device differs from post-publication session")
    if metadata.get("automatedRedFlagCount") != 0 or metadata.get("automatedRedFlags") not in ([], None):
        _fail(f"{label}: automated crash/ANR/native-fatal red flags are present")
    if metadata.get("manualReviewRequired") is not True:
        _fail(f"{label}: checkpoint must remain manual-review-required")
    captured_at = _timestamp(metadata.get("capturedAtUtc"), f"{label}.capturedAtUtc")
    if captured_at < published_at:
        _fail(f"{label}: checkpoint predates the recorded publication")
    files = metadata.get("files")
    if not isinstance(files, list) or files != list(CHECKPOINT_FILES):
        _fail(f"{label}: checkpoint evidence file contract differs from device_evidence collector")

    records: list[dict[str, Any]] = []
    for name in ("checkpoint.json", *CHECKPOINT_FILES):
        path = checkpoint_dir / name
        _require_regular(path, f"{label}/{name}", nonempty=name in {"checkpoint.json", "screen.png"})
        if name == "screen.png" and not path.read_bytes().startswith(b"\x89PNG\r\n\x1a\n"):
            _fail(f"{label}: screen.png does not have a valid PNG signature")
        records.append(
            {
                "path": f"checkpoints/{label}/{name}",
                "sizeBytes": path.stat().st_size,
                "sha256": _sha256_file(path),
            }
        )
    return records


def _content_set_sha(records: list[dict[str, Any]]) -> str:
    canonical = json.dumps(records, sort_keys=True, separators=(",", ":")).encode("utf-8")
    return hashlib.sha256(canonical).hexdigest()


def reconcile(reconciliation_path: Path, session_dir: Path) -> dict[str, Any]:
    reconciliation_path = reconciliation_path.expanduser().resolve()
    session_dir = session_dir.expanduser().resolve()
    try:
        reconciliation = prepare.validate_reconciliation(reconciliation_path)
    except prepare.P1PostPublicationPrepareError as exc:
        raise P1PostPublicationSmokeError(str(exc)) from exc

    session_path = session_dir / "session.json"
    index_path = session_dir / "evidence-index.json"
    session = _read_json(session_path, "post-publication device session")
    index = _read_json(index_path, "post-publication evidence index")

    if session.get("schemaVersion") != 1:
        _fail("post-publication device session schemaVersion must be 1")
    if session.get("state") != "EVIDENCE_COLLECTED" or session.get("verdict") != "MANUAL_REVIEW_REQUIRED":
        _fail("post-publication device session must be finished with MANUAL_REVIEW_REQUIRED verdict")
    if session.get("packageId") != prepare.EXPECTED_PACKAGE_ID:
        _fail("post-publication device session packageId mismatch")
    if session.get("checkpointCount") != len(EXPECTED_CHECKPOINTS):
        _fail("post-publication device session must contain exactly the three required smoke checkpoints")
    if session.get("automatedRedFlagCount") != 0:
        _fail("post-publication device session contains automated red flags")

    session_apk = session.get("apk")
    if not isinstance(session_apk, dict):
        _fail("post-publication device session is missing APK metadata")
    session_apk_sha = _sha256(session_apk.get("sha256"), "session.apk.sha256")
    if session_apk_sha != reconciliation["publishedApkSha256"]:
        _fail("post-publication device session APK differs from the published APK")

    device = session.get("device")
    if not isinstance(device, dict):
        _fail("post-publication device session is missing device metadata")
    if device.get("isEmulator") is not False:
        _fail("post-publication smoke requires a physical Android device")
    device_sha = _sha256(device.get("serialSha256"), "session.device.serialSha256")

    tier = str(session.get("performanceTier") or "").strip().lower()
    if tier not in prepare.PERFORMANCE_TIERS:
        _fail("post-publication device session has no valid performanceTier binding")
    context = session.get("p1PostPublication")
    if context != _context_expected(reconciliation):
        _fail("post-publication session receipt/authorization binding differs from the exact reconciliation")

    published_at = _timestamp(reconciliation["publishedAtUtc"], "publishedAtUtc")
    started_at = _timestamp(session.get("createdAtUtc"), "session.createdAtUtc")
    finished_at = _timestamp(session.get("finishedAtUtc"), "session.finishedAtUtc")
    if started_at < published_at or finished_at < published_at or finished_at < started_at:
        _fail("post-publication smoke session timestamps do not occur after publication in chronological order")

    if index.get("schemaVersion") != 1:
        _fail("post-publication evidence index schemaVersion must be 1")
    if index.get("state") != "EVIDENCE_COLLECTED" or index.get("verdict") != "MANUAL_REVIEW_REQUIRED":
        _fail("post-publication evidence index is not a finished manual-review evidence set")
    if index.get("packageId") != prepare.EXPECTED_PACKAGE_ID:
        _fail("post-publication evidence index packageId mismatch")
    if _sha256(index.get("apkSha256"), "evidence-index.apkSha256") != session_apk_sha:
        _fail("post-publication evidence index APK differs from session")
    if _sha256(index.get("deviceSerialSha256"), "evidence-index.deviceSerialSha256") != device_sha:
        _fail("post-publication evidence index device differs from session")
    index_device = index.get("device")
    if not isinstance(index_device, dict) or index_device.get("isEmulator") is not False:
        _fail("post-publication evidence index must identify a physical device")
    if index.get("checkpointCount") != len(EXPECTED_CHECKPOINTS):
        _fail("post-publication evidence index must contain exactly three checkpoints")
    checkpoint_names = index.get("checkpoints")
    if not isinstance(checkpoint_names, list) or len(checkpoint_names) != len(EXPECTED_CHECKPOINTS) or set(checkpoint_names) != set(EXPECTED_CHECKPOINTS):
        _fail("post-publication evidence index checkpoint set is not the exact smoke set")
    if index.get("automatedRedFlagCount") != 0 or index.get("automatedRedFlags") not in ([], None):
        _fail("post-publication evidence index contains automated red flags")
    if _timestamp(index.get("generatedAtUtc"), "evidence-index.generatedAtUtc") < published_at:
        _fail("post-publication evidence index predates publication")

    evidence_records = [
        {"path": "session.json", "sizeBytes": session_path.stat().st_size, "sha256": _sha256_file(session_path)},
        {"path": "evidence-index.json", "sizeBytes": index_path.stat().st_size, "sha256": _sha256_file(index_path)},
    ]
    actual_checkpoint_dirs = {
        path.name for path in (session_dir / "checkpoints").iterdir() if path.is_dir() and not path.is_symlink()
    }
    if actual_checkpoint_dirs != set(EXPECTED_CHECKPOINTS):
        _fail("post-publication smoke directory contains missing or unexpected checkpoint directories")
    for label in EXPECTED_CHECKPOINTS:
        evidence_records.extend(_validate_checkpoint(session_dir, label, published_at, session_apk_sha, device_sha))
    evidence_records.sort(key=lambda item: item["path"])

    try:
        metrics = analyze_device_smoke.analyze(session_dir, tier)
    except (OSError, RuntimeError, ValueError, json.JSONDecodeError) as exc:
        raise P1PostPublicationSmokeError(f"UPER-006 smoke analyzer failed: {exc}") from exc
    if metrics.get("schemaVersion") != 2 or metrics.get("taskId") != "UPER-006":
        _fail("UPER-006 smoke analyzer returned an unsupported contract")
    if metrics.get("verified") is not False:
        _fail("UPER-006 smoke analyzer must remain unverified")
    if metrics.get("verdict") != "PASSABLE_FOR_MANUAL_REVIEW" or metrics.get("blockers") != []:
        blockers = metrics.get("blockers") if isinstance(metrics.get("blockers"), list) else []
        _fail("post-publication smoke is not passable for manual review: " + "; ".join(map(str, blockers)))
    if str(metrics.get("tier") or "").lower() != tier:
        _fail("UPER-006 smoke analyzer tier differs from session binding")
    if _sha256(metrics.get("apkSha256"), "smokeMetrics.apkSha256") != session_apk_sha:
        _fail("UPER-006 smoke analyzer APK differs from published APK")
    if _sha256(metrics.get("deviceSerialSha256"), "smokeMetrics.deviceSerialSha256") != device_sha:
        _fail("UPER-006 smoke analyzer device differs from session")

    return {
        "schemaVersion": SCHEMA_VERSION,
        "state": RESULT_STATE,
        "verdict": RESULT_VERDICT,
        "humanPublicationRecorded": True,
        "postPublicationSmokeObserved": True,
        "publicationPerformedByTool": False,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "lastVerifiedPointerUpdatePerformedByTool": False,
        "humanClosureReviewRequired": True,
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
        "performanceTier": tier.upper(),
        "deviceSerialSha256": device_sha,
        "smokeStartedAtUtc": session.get("createdAtUtc"),
        "smokeFinishedAtUtc": session.get("finishedAtUtc"),
        "evidenceFileCount": len(evidence_records),
        "evidenceContentSetSha256": _content_set_sha(evidence_records),
        "smokeMetrics": metrics,
        "notes": [
            "The exact published APK completed the automated-observable post-publication smoke checks on a physical device.",
            "This result is only passable for human closure review; it does not grant VERIFIED state or update Last Verified pointers.",
        ],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--receipt-reconciliation", required=True, help="Exact Step-23 reconciliation JSON")
    parser.add_argument("--session", required=True, help="Finished dedicated post-publication device session")
    parser.add_argument("--output", help="Optional reconciliation output; existing files are never overwritten")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = reconcile(Path(args.receipt_reconciliation), Path(args.session))
        output: Path | None = None
        if args.output:
            output = Path(args.output).expanduser().resolve()
            if output.exists():
                raise P1PostPublicationSmokeError(f"refusing to overwrite existing smoke reconciliation: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_P1_POST_PUBLICATION_SMOKE_RECONCILED "
            f"verdict={result['verdict']} apkSha256={result['publishedApkSha256']} "
            f"evidenceContentSetSha256={result['evidenceContentSetSha256']} "
            "humanClosureReviewRequired=true publicationPerformedByTool=false verified=false"
            + (f" output={output}" if output else "")
        )
        return 0
    except (P1PostPublicationSmokeError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"AFAREET_P1_POST_PUBLICATION_SMOKE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
