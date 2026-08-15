#!/usr/bin/env python3
"""Fail-closed preflight for manual publication of a verified 3Fareet Unity APK.

This tool never creates a Git tag, GitHub Release, asset upload, or VERIFIED
pointer. It proves that one exact candidate APK, candidate-bound physical-device
session, content-addressed review bundle, and schema-v2 human approvals all
refer to the same evidence chain and that the final-five readiness evaluator
reaches READY_FOR_RELEASE_REVIEW.

A successful result means only ELIGIBLE_FOR_MANUAL_PUBLICATION. The release
owner must still follow docs/RELEASE_POLICY.md and perform publication as an
explicit human action.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import sys
from pathlib import Path
from typing import Any

import p1_gate_readiness
import prepare_candidate_device

SCHEMA_VERSION = 1
STATE = "PUBLICATION_PREFLIGHT_PASSED"
VERDICT = "ELIGIBLE_FOR_MANUAL_PUBLICATION"
FINAL_GATE = "UPER-010"


class PublicationPreflightError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise PublicationPreflightError(message)


def verify_publication(
    *,
    candidate_manifest_path: Path,
    apk_path: Path | None,
    session_dir: Path,
    review_bundle_dir: Path,
    approvals_path: Path,
    spec_path: Path,
) -> dict[str, Any]:
    candidate_manifest_path = candidate_manifest_path.expanduser().resolve()
    session_dir = session_dir.expanduser().resolve()
    review_bundle_dir = review_bundle_dir.expanduser().resolve()
    approvals_path = approvals_path.expanduser().resolve()
    spec_path = spec_path.expanduser().resolve()
    apk_override = apk_path.expanduser().resolve() if apk_path is not None else None

    candidate_manifest = prepare_candidate_device.read_json(candidate_manifest_path)
    candidate = prepare_candidate_device.resolve_candidate(
        candidate_manifest,
        candidate_manifest_path,
        apk_override,
    )

    index = p1_gate_readiness.read_json(session_dir / p1_gate_readiness.INDEX_FILE)
    session_candidate, binding_error = p1_gate_readiness.load_bound_candidate(session_dir, index)
    _require(binding_error is None, f"candidate-bound device session is invalid: {binding_error}")
    _require(isinstance(session_candidate, dict), "device session is missing validated candidate provenance")

    candidate_type = str(candidate["candidateType"])
    git_sha = str(candidate["gitSha"])
    apk_sha = str(candidate["apkSha256"])
    _require(session_candidate.get("candidateType") == candidate_type, "session candidateType does not match publication candidate")
    _require(str(session_candidate.get("gitSha") or "").lower() == git_sha, "session candidate Git SHA does not match publication candidate")
    _require(str(session_candidate.get("apkSha256") or "").lower() == apk_sha, "session candidate APK SHA does not match publication candidate")
    _require(str(index.get("apkSha256") or "").lower() == apk_sha, "evidence-index APK SHA does not match publication candidate")

    session_manifest = session_candidate.get("manifest")
    _require(isinstance(session_manifest, dict), "session candidate manifest binding is missing")
    source_manifest_sha = sha256_file(candidate_manifest_path)
    _require(
        str(session_manifest.get("sha256") or "").lower() == source_manifest_sha,
        "publication candidate manifest bytes do not match the manifest bound into the physical-device session",
    )

    review = p1_gate_readiness.verify_review_bundle(
        review_bundle_dir,
        expected_git_sha=git_sha,
        expected_apk_sha=apk_sha,
    )
    review_content_sha = str(review.get("contentSetSha256") or "").strip().lower()
    _require(bool(p1_gate_readiness.SHA256_RE.fullmatch(review_content_sha)), "verified review bundle has invalid contentSetSha256")
    _require(review.get("verified") is False, "review verifier must not self-assert VERIFIED state")
    _require(review.get("verdict") == "MANUAL_REVIEW_REQUIRED", "review bundle must remain MANUAL_REVIEW_REQUIRED before release preflight")

    approvals = p1_gate_readiness.load_approvals(approvals_path)
    _require(isinstance(approvals, dict), "schema-v2 manual approvals file is required")

    evaluation_index = dict(index)
    evaluation_index["candidate"] = session_candidate
    evaluation_index["reviewBundleBound"] = True
    evaluation_index["reviewContentSetSha256"] = review_content_sha
    spec = p1_gate_readiness.load_spec(spec_path)
    readiness = p1_gate_readiness.evaluate(spec, evaluation_index, approvals)

    _require(readiness.get("candidateBound") is True, "readiness candidate binding is not valid")
    _require(readiness.get("reviewBundleBound") is True, "readiness review bundle binding is not valid")
    _require(readiness.get("physicalDevice") is True, "release publication requires physical-device evidence")
    _require(int(readiness.get("automatedRedFlagCount", -1)) == 0, "release publication is blocked by automated crash/ANR/native-fatal red flags")
    _require(readiness.get("allEvidenceReady") is True, "not all four manual P1 evidence gates are ready")
    _require(readiness.get("releaseReviewReady") is True, "final-five readiness has not reached READY_FOR_RELEASE_REVIEW")
    _require(readiness.get("verified") is False, "readiness evaluator must not self-assert VERIFIED state")
    _require(str(readiness.get("candidateGitSha") or "").lower() == git_sha, "readiness Git SHA does not match publication candidate")
    _require(str(readiness.get("apkSha256") or "").lower() == apk_sha, "readiness APK SHA does not match publication candidate")
    _require(str(readiness.get("reviewContentSetSha256") or "").lower() == review_content_sha, "readiness review-content SHA does not match verified review bundle")

    gates = readiness.get("gates")
    _require(isinstance(gates, dict), "readiness gate records are missing")
    reviewers: dict[str, str] = {}
    for task_id in spec["gates"]:
        gate = gates.get(task_id)
        _require(isinstance(gate, dict), f"readiness gate record is missing: {task_id}")
        _require(gate.get("manualApproved") is True, f"manual approval is missing for {task_id}")
        reviewer = str(gate.get("approvalDetail") or "").strip()
        _require(bool(reviewer), f"approval reviewer is missing for {task_id}")
        reviewers[task_id] = reviewer

    final = gates[FINAL_GATE]
    _require(final.get("status") == "READY_FOR_RELEASE_REVIEW", "UPER-010 has not reached READY_FOR_RELEASE_REVIEW")

    exact_apk = Path(candidate["apkPath"]).resolve()
    actual_apk_sha = sha256_file(exact_apk)
    _require(actual_apk_sha == apk_sha, "APK bytes changed after candidate validation")

    return {
        "schemaVersion": SCHEMA_VERSION,
        "state": STATE,
        "verdict": VERDICT,
        "eligibleForManualPublication": True,
        "verified": False,
        "candidate": {
            "candidateType": candidate_type,
            "gitSha": git_sha,
            "candidateManifestFile": candidate_manifest_path.name,
            "candidateManifestSha256": source_manifest_sha,
            "apkFileName": exact_apk.name,
            "apkSizeBytes": exact_apk.stat().st_size,
            "apkSha256": actual_apk_sha,
        },
        "evidence": {
            "reviewContentSetSha256": review_content_sha,
            "deviceSerialSha256": review.get("deviceSerialSha256"),
            "checkpointCount": review.get("checkpointCount"),
            "approvalsFileName": approvals_path.name,
            "approvalsSha256": sha256_file(approvals_path),
            "reviewers": reviewers,
        },
        "releaseGate": {
            "taskId": FINAL_GATE,
            "status": "READY_FOR_RELEASE_REVIEW",
        },
        "notes": [
            "This preflight does not publish, tag, upload, rename, or mark the APK VERIFIED.",
            "The release owner must still follow docs/RELEASE_POLICY.md and record the final publication evidence.",
        ],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Verify one exact candidate/evidence/approval chain is eligible for manual 3Fareet Unity release publication."
    )
    parser.add_argument("--candidate-manifest", required=True, help="Exact local/CI candidate manifest used for device evidence.")
    parser.add_argument("--apk", help="Optional exact APK override if the candidate bundle moved workstations.")
    parser.add_argument("--session", required=True, help="Candidate-bound physical-device evidence session directory.")
    parser.add_argument("--review-bundle", required=True, help="Content-addressed sanitized review bundle.")
    parser.add_argument("--approvals", required=True, help="Schema-v2 manual approvals bound to the same review content set.")
    parser.add_argument("--spec", default=str(p1_gate_readiness.DEFAULT_SPEC), help="P1 gate specification JSON.")
    parser.add_argument("--output", help="Optional publication-preflight JSON output path.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_publication(
            candidate_manifest_path=Path(args.candidate_manifest),
            apk_path=Path(args.apk) if args.apk else None,
            session_dir=Path(args.session),
            review_bundle_dir=Path(args.review_bundle),
            approvals_path=Path(args.approvals),
            spec_path=Path(args.spec),
        )
        if args.output:
            output = Path(args.output).expanduser().resolve()
            output.parent.mkdir(parents=True, exist_ok=True)
            if output.exists():
                raise PublicationPreflightError(f"refusing to overwrite existing publication preflight: {output}")
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        else:
            output = None
        print(
            "AFAREET_RELEASE_PUBLICATION_PREFLIGHT_OK "
            f"gitSha={result['candidate']['gitSha']} apkSha256={result['candidate']['apkSha256']} "
            f"reviewContentSetSha256={result['evidence']['reviewContentSetSha256']} "
            f"verdict={result['verdict']} verified=false"
            + (f" output={output}" if output else "")
        )
        return 0
    except (
        PublicationPreflightError,
        prepare_candidate_device.CandidatePrepareError,
        RuntimeError,
        OSError,
        ValueError,
        json.JSONDecodeError,
    ) as exc:
        print(f"AFAREET_RELEASE_PUBLICATION_PREFLIGHT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())