#!/usr/bin/env python3
"""Fail-closed P1 preflight for explicit manual publication of one exact Unity APK.

This is the P1 counterpart to verify_release_publication.py. It requires the Step 13
lineage-bound final-five readiness path, not merely a generic review bundle and generic
schema-v2 approvals. A successful result means only that one exact P1 evidence chain is
eligible for an explicit release-owner publication action. This tool never publishes,
tags, uploads, updates Last Verified, or marks the APK VERIFIED.
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

import p1_gate_readiness
import p1_lineage_gate_readiness
import prepare_candidate_device

SCHEMA_VERSION = 1
STATE = "P1_PUBLICATION_PREFLIGHT_PASSED"
VERDICT = "P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION"
FINAL_GATE = "UPER-010"


class P1PublicationPreflightError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise P1PublicationPreflightError(message)


def verify_p1_publication(
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
    candidate_type = str(candidate["candidateType"])
    git_sha = str(candidate["gitSha"])
    apk_sha = str(candidate["apkSha256"])
    _require(
        candidate_type == prepare_candidate_device.LOCAL_CANDIDATE_TYPE,
        f"P1 publication requires the local licensed-Windows candidate type, found {candidate_type!r}",
    )
    _require(candidate.get("verified") is False, "publication candidate must remain unverified before explicit publication")

    spec = p1_gate_readiness.load_spec(spec_path)
    binding = p1_lineage_gate_readiness._bind_p1_review(session_dir, review_bundle_dir, spec)
    _require(binding["candidateGitSha"] == git_sha, "P1 gate binding Git SHA does not match publication candidate")
    _require(binding["apkSha256"] == apk_sha, "P1 gate binding APK SHA does not match publication candidate")

    index = binding["index"]
    session_candidate = binding["candidate"]
    _require(isinstance(session_candidate, dict), "P1 device session is missing validated candidate provenance")
    _require(session_candidate.get("candidateType") == candidate_type, "session candidateType does not match publication candidate")
    _require(str(session_candidate.get("gitSha") or "").lower() == git_sha, "session Git SHA does not match publication candidate")
    _require(str(session_candidate.get("apkSha256") or "").lower() == apk_sha, "session APK SHA does not match publication candidate")
    _require(str(index.get("apkSha256") or "").lower() == apk_sha, "evidence-index APK SHA does not match publication candidate")

    session_manifest = session_candidate.get("manifest")
    _require(isinstance(session_manifest, dict), "session candidate-manifest binding is missing")
    source_manifest_sha = sha256_file(candidate_manifest_path)
    _require(
        str(session_manifest.get("sha256") or "").lower() == source_manifest_sha,
        "publication candidate-manifest bytes do not match the manifest bound into the P1 physical-device session",
    )

    approvals = p1_lineage_gate_readiness.load_p1_approvals(approvals_path, spec)
    _require(isinstance(approvals, dict), "P1 lineage-bound manual approvals file is required")
    readiness = p1_lineage_gate_readiness.evaluate_p1(spec, binding, approvals)

    _require(readiness.get("candidateBound") is True, "P1 readiness candidate binding is not valid")
    _require(readiness.get("p1ReviewBundleBound") is True, "P1 lineage-bound review bundle is not valid")
    _require(readiness.get("physicalDevice") is True, "P1 publication requires physical-device evidence")
    _require(int(readiness.get("automatedRedFlagCount", -1)) == 0, "P1 publication is blocked by automated red flags")
    _require(readiness.get("allEvidenceReady") is True, "not all four P1 evidence gates are ready")
    _require(readiness.get("releaseReviewReady") is True, "P1 final-five readiness has not reached READY_FOR_RELEASE_REVIEW")
    _require(readiness.get("verified") is False, "P1 readiness must not self-assert VERIFIED state")
    _require(readiness.get("runtimeVerified") is False, "P1 readiness must not self-assert runtime verification")
    _require(readiness.get("ownerAccepted") is False, "P1 readiness must not self-assert owner acceptance")
    _require(readiness.get("publicationEligible") is False, "P1 gate-readiness layer must not self-assert publication eligibility")

    _require(str(readiness.get("candidateGitSha") or "").lower() == git_sha, "P1 readiness Git SHA mismatch")
    _require(str(readiness.get("apkSha256") or "").lower() == apk_sha, "P1 readiness APK SHA mismatch")
    _require(
        str(readiness.get("stagingSourceGitSha") or "").lower() == binding["stagingSourceGitSha"],
        "P1 readiness staging-source SHA mismatch",
    )
    _require(
        str(readiness.get("p1ReviewLineageSha256") or "").lower() == binding["p1ReviewLineageSha256"],
        "P1 readiness review-lineage fingerprint mismatch",
    )
    _require(
        str(readiness.get("reviewContentSetSha256") or "").lower() == binding["reviewContentSetSha256"],
        "P1 readiness review-content fingerprint mismatch",
    )
    _require(
        str(readiness.get("performanceTier") or "").lower() == binding["performanceTier"],
        "P1 readiness performance tier mismatch",
    )
    _require(
        readiness.get("sourceArtifactDigests") == binding["sourceArtifactDigests"],
        "P1 readiness source-artifact digests mismatch",
    )

    gates = readiness.get("gates")
    _require(isinstance(gates, dict), "P1 readiness gate records are missing")
    reviewers: dict[str, str] = {}
    for task_id in spec["gates"]:
        gate = gates.get(task_id)
        _require(isinstance(gate, dict), f"P1 readiness gate record is missing: {task_id}")
        _require(gate.get("manualApproved") is True, f"manual approval is missing for {task_id}")
        reviewer = str(gate.get("approvalDetail") or "").strip()
        _require(bool(reviewer), f"approval reviewer is missing for {task_id}")
        reviewers[task_id] = reviewer

    final = gates.get(FINAL_GATE)
    _require(isinstance(final, dict), "UPER-010 readiness record is missing")
    _require(final.get("status") == "READY_FOR_RELEASE_REVIEW", "UPER-010 has not reached READY_FOR_RELEASE_REVIEW")

    exact_apk = Path(candidate["apkPath"]).resolve()
    actual_apk_sha = sha256_file(exact_apk)
    _require(actual_apk_sha == apk_sha, "APK bytes changed after P1 candidate validation")

    approvals_sha = sha256_file(approvals_path)
    return {
        "schemaVersion": SCHEMA_VERSION,
        "state": STATE,
        "verdict": VERDICT,
        "eligibleForExplicitManualPublication": True,
        "publicationPerformed": False,
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
        "p1Lineage": {
            "stagingSourceGitSha": binding["stagingSourceGitSha"],
            "directParentGitSha": binding["directParentGitSha"],
            "candidateGitSha": binding["candidateGitSha"],
            "reviewContentSetSha256": binding["reviewContentSetSha256"],
            "p1ReviewLineageSha256": binding["p1ReviewLineageSha256"],
            "performanceTier": binding["performanceTier"],
            "coveredVisualRuntimeTasks": list(binding["coveredVisualRuntimeTasks"]),
            "sourceArtifactDigests": dict(binding["sourceArtifactDigests"]),
        },
        "evidence": {
            "deviceSerialSha256": binding["evidenceOnly"].get("deviceSerialSha256"),
            "checkpointCount": len(binding["evidenceOnly"].get("capturedCheckpoints", [])),
            "approvalsFileName": approvals_path.name,
            "approvalsSha256": approvals_sha,
            "reviewers": reviewers,
        },
        "releaseGate": {
            "taskId": FINAL_GATE,
            "status": "READY_FOR_RELEASE_REVIEW",
        },
        "notes": [
            "This P1 preflight does not publish, tag, upload, rename, update Last Verified, or mark the APK VERIFIED.",
            "A release owner must still perform the explicit publication action and record post-publication evidence under docs/RELEASE_POLICY.md.",
        ],
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Verify one exact P1 lineage-bound candidate/evidence/approval chain is eligible for explicit manual publication."
    )
    parser.add_argument("--candidate-manifest", required=True, help="Exact local licensed-Windows candidate manifest used for device evidence.")
    parser.add_argument("--apk", help="Optional exact APK override if the candidate bundle moved workstations.")
    parser.add_argument("--session", required=True, help="Step 11 P1 candidate-bound physical-device evidence session directory.")
    parser.add_argument("--review-bundle", required=True, help="Step 12 P1 lineage-bound sanitized review bundle.")
    parser.add_argument("--approvals", required=True, help="Step 13 P1 lineage-bound manual approvals JSON.")
    parser.add_argument("--spec", default=str(p1_gate_readiness.DEFAULT_SPEC), help="P1 gate specification JSON.")
    parser.add_argument("--output", help="Optional P1 publication-preflight JSON output path.")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_p1_publication(
            candidate_manifest_path=Path(args.candidate_manifest),
            apk_path=Path(args.apk) if args.apk else None,
            session_dir=Path(args.session),
            review_bundle_dir=Path(args.review_bundle),
            approvals_path=Path(args.approvals),
            spec_path=Path(args.spec),
        )
        output: Path | None = None
        if args.output:
            output = Path(args.output).expanduser().resolve()
            output.parent.mkdir(parents=True, exist_ok=True)
            if output.exists():
                raise P1PublicationPreflightError(
                    f"refusing to overwrite existing P1 publication preflight: {output}"
                )
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_P1_RELEASE_PUBLICATION_PREFLIGHT_OK "
            f"stagingSourceGitSha={result['p1Lineage']['stagingSourceGitSha']} "
            f"gitSha={result['candidate']['gitSha']} apkSha256={result['candidate']['apkSha256']} "
            f"p1ReviewLineageSha256={result['p1Lineage']['p1ReviewLineageSha256']} "
            f"verdict={result['verdict']} publicationPerformed=false verified=false"
            + (f" output={output}" if output else "")
        )
        return 0
    except (
        P1PublicationPreflightError,
        p1_lineage_gate_readiness.P1LineageGateError,
        prepare_candidate_device.CandidatePrepareError,
        RuntimeError,
        OSError,
        ValueError,
        json.JSONDecodeError,
    ) as exc:
        print(f"AFAREET_P1_RELEASE_PUBLICATION_PREFLIGHT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
