#!/usr/bin/env python3
"""Evaluate P1 final-five readiness with mandatory authorization-bound review lineage.

The existing p1_gate_readiness.py remains the generic evidence/approval evaluator. This wrapper
adds the P1 provenance boundary introduced by Steps 9–21: the raw physical-device session must
still contain the exact staged-candidate lineage, the sanitized review bundle must pass the P1
review-lineage verifier, and manual approvals must be pinned to both the generic review content
set and the exact staging authorization fingerprints.

No command in this module marks an APK VERIFIED or makes publication eligible.
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

import export_p1_device_evidence
import p1_gate_readiness
import verify_p1_device_review_bundle

P1_APPROVALS_SCHEMA_VERSION = 2
P1_APPROVAL_PROFILE = "p1-lineage-manual-approvals-v2"
OUTPUT_SCHEMA_VERSION = 1
EXPECTED_VISUAL_TASKS = list(verify_p1_device_review_bundle.EXPECTED_TASKS)
AUTHORIZATION_KEYS = (
    "authorizationSourceGitSha",
    "handoffPacketSha256",
    "nativeHandoffVerificationSha256",
    "operatorChainSha256",
)
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")


class P1LineageGateError(RuntimeError):
    pass


def _read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file():
        raise P1LineageGateError(f"{label} is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1LineageGateError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1LineageGateError(f"{label} root must be a JSON object")
    return payload


def _sha40(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA40_RE.fullmatch(text):
        raise P1LineageGateError(f"{label} must be a full 40-character Git SHA, found {value!r}")
    return text


def _sha256(value: Any, label: str) -> str:
    text = str(value or "").strip().lower()
    if not SHA256_RE.fullmatch(text):
        raise P1LineageGateError(f"{label} must be a SHA-256 hex digest, found {value!r}")
    return text


def _authorization(value: Any, label: str, expected_source_sha: str) -> dict[str, str]:
    if not isinstance(value, dict) or set(value) != set(AUTHORIZATION_KEYS):
        raise P1LineageGateError(
            f"{label} must contain exactly the four staging authorization fingerprints"
        )
    source_sha = _sha40(
        value.get("authorizationSourceGitSha"), f"{label}.authorizationSourceGitSha"
    )
    if source_sha != expected_source_sha:
        raise P1LineageGateError(
            f"{label}.authorizationSourceGitSha mismatch: expected={expected_source_sha} actual={source_sha}"
        )
    return {
        "authorizationSourceGitSha": source_sha,
        "handoffPacketSha256": _sha256(
            value.get("handoffPacketSha256"), f"{label}.handoffPacketSha256"
        ),
        "nativeHandoffVerificationSha256": _sha256(
            value.get("nativeHandoffVerificationSha256"),
            f"{label}.nativeHandoffVerificationSha256",
        ),
        "operatorChainSha256": _sha256(
            value.get("operatorChainSha256"), f"{label}.operatorChainSha256"
        ),
    }


def _require_false(payload: dict[str, Any], key: str, label: str) -> None:
    if payload.get(key) is not False:
        raise P1LineageGateError(f"{label}.{key} must remain JSON false")


def _require_exact_visual_tasks(value: Any, label: str) -> list[str]:
    if not isinstance(value, list) or value != EXPECTED_VISUAL_TASKS:
        raise P1LineageGateError(
            f"{label} must equal the ordered six-task P1 visual/runtime scope: {EXPECTED_VISUAL_TASKS}"
        )
    return list(value)


def _bind_p1_review(
    session_dir: Path,
    review_bundle: Path,
    spec: dict[str, Any],
) -> dict[str, Any]:
    session_dir = session_dir.expanduser().resolve()
    review_bundle = review_bundle.expanduser().resolve()

    index = p1_gate_readiness.read_json(session_dir / p1_gate_readiness.INDEX_FILE)
    candidate, binding_error = p1_gate_readiness.load_bound_candidate(session_dir, index)
    if binding_error:
        raise P1LineageGateError(f"candidate binding invalid: {binding_error}")
    if not isinstance(candidate, dict):
        raise P1LineageGateError("candidate provenance is missing from device session")

    candidate_sha = _sha40(candidate.get("gitSha"), "session.candidate.gitSha")
    apk_sha = _sha256(index.get("apkSha256"), "evidenceIndex.apkSha256")

    try:
        raw_p1 = export_p1_device_evidence.validate_p1_session(session_dir)
    except Exception as exc:
        raise P1LineageGateError(f"raw P1 session lineage validation failed: {exc}") from exc

    try:
        review = verify_p1_device_review_bundle.verify_p1_bundle(
            review_bundle,
            expected_git_sha=candidate_sha,
            expected_apk_sha=apk_sha,
            expected_staging_source_sha=raw_p1["stagingSourceGitSha"],
        )
    except Exception as exc:
        raise P1LineageGateError(f"P1 review bundle verification failed: {exc}") from exc

    if review.get("verified") is not False:
        raise P1LineageGateError("P1 review verifier must not self-assert VERIFIED state")
    for key in ("runtimeVerified", "ownerAccepted", "publicationEligible"):
        if review.get(key) is not False:
            raise P1LineageGateError(f"P1 review verifier must keep {key}=false")

    staging_source_sha = _sha40(review.get("stagingSourceGitSha"), "p1Review.stagingSourceGitSha")
    review_candidate_sha = _sha40(review.get("candidateGitSha"), "p1Review.candidateGitSha")
    direct_parent_sha = _sha40(review.get("directParentGitSha"), "p1Review.directParentGitSha")
    review_apk_sha = _sha256(review.get("apkSha256"), "p1Review.apkSha256")
    lineage_sha = _sha256(review.get("p1ReviewLineageSha256"), "p1Review.p1ReviewLineageSha256")
    content_set_sha = _sha256(review.get("contentSetSha256"), "p1Review.contentSetSha256")

    if review_candidate_sha != candidate_sha:
        raise P1LineageGateError("P1 review candidate SHA differs from device-session candidate")
    if review_apk_sha != apk_sha:
        raise P1LineageGateError("P1 review APK SHA differs from device evidence")
    if direct_parent_sha != staging_source_sha:
        raise P1LineageGateError("P1 review direct parent must equal staging-source SHA")
    if staging_source_sha != _sha40(raw_p1.get("stagingSourceGitSha"), "rawP1.stagingSourceGitSha"):
        raise P1LineageGateError("P1 review staging-source SHA differs from raw session lineage")
    if candidate_sha != _sha40(raw_p1.get("candidateGitSha"), "rawP1.candidateGitSha"):
        raise P1LineageGateError("P1 review candidate SHA differs from raw session lineage")
    if direct_parent_sha != _sha40(raw_p1.get("directParentGitSha"), "rawP1.directParentGitSha"):
        raise P1LineageGateError("P1 review direct-parent SHA differs from raw session lineage")
    if apk_sha != _sha256(raw_p1.get("apkSha256"), "rawP1.apkSha256"):
        raise P1LineageGateError("P1 review APK SHA differs from raw session lineage")

    review_authorization = _authorization(
        review.get("stagingAuthorization"), "p1Review.stagingAuthorization", staging_source_sha
    )
    raw_authorization = _authorization(
        raw_p1.get("stagingAuthorization"), "rawP1.stagingAuthorization", staging_source_sha
    )
    if review_authorization != raw_authorization:
        raise P1LineageGateError(
            "P1 review staging authorization differs from the raw physical-device session lineage"
        )

    review_tier = str(review.get("performanceTier") or "").strip().lower()
    raw_tier = str(raw_p1.get("performanceTier") or "").strip().lower()
    if review_tier not in {"low", "mid", "high"} or review_tier != raw_tier:
        raise P1LineageGateError(
            f"P1 review performance tier does not match raw session: review={review_tier!r} raw={raw_tier!r}"
        )

    _require_exact_visual_tasks(review.get("coveredTasks"), "p1Review.coveredTasks")
    _require_exact_visual_tasks(raw_p1.get("coveredTasks"), "rawP1.coveredTasks")

    review_digests = review.get("sourceArtifactDigests")
    raw_digests = raw_p1.get("sourceFileHashes")
    if not isinstance(review_digests, dict) or not isinstance(raw_digests, dict):
        raise P1LineageGateError("P1 review/raw-session source artifact digests are missing")
    expected_digest_keys = {"p1Manifest", "stagingReport", "stagingLineage", "candidateManifest"}
    if set(review_digests) != expected_digest_keys or set(raw_digests) != expected_digest_keys:
        raise P1LineageGateError("P1 source artifact digests must contain exactly four provenance records")
    normalized_digests = {
        key: _sha256(review_digests.get(key), f"p1Review.sourceArtifactDigests.{key}")
        for key in sorted(expected_digest_keys)
    }
    normalized_raw_digests = {
        key: _sha256(raw_digests.get(key), f"rawP1.sourceFileHashes.{key}")
        for key in sorted(expected_digest_keys)
    }
    if normalized_digests != normalized_raw_digests:
        raise P1LineageGateError("P1 review source-artifact digests differ from raw session lineage bytes")

    evaluation_index = dict(index)
    evaluation_index["candidate"] = candidate
    evaluation_index["reviewBundleBound"] = True
    evaluation_index["reviewContentSetSha256"] = content_set_sha

    evidence_only = p1_gate_readiness.evaluate(spec, evaluation_index, approvals=None)

    return {
        "sessionDir": session_dir,
        "reviewBundle": review_bundle,
        "index": index,
        "candidate": candidate,
        "evaluationIndex": evaluation_index,
        "evidenceOnly": evidence_only,
        "stagingSourceGitSha": staging_source_sha,
        "candidateGitSha": candidate_sha,
        "directParentGitSha": direct_parent_sha,
        "apkSha256": apk_sha,
        "reviewContentSetSha256": content_set_sha,
        "p1ReviewLineageSha256": lineage_sha,
        "performanceTier": review_tier,
        "coveredVisualRuntimeTasks": list(EXPECTED_VISUAL_TASKS),
        "sourceArtifactDigests": normalized_digests,
        "stagingAuthorization": dict(review_authorization),
    }


def load_p1_approvals(path: Path | None, spec: dict[str, Any]) -> dict[str, Any] | None:
    if path is None:
        return None
    approvals = _read_json(path, "P1 manual approvals")
    if approvals.get("schemaVersion") != P1_APPROVALS_SCHEMA_VERSION:
        raise P1LineageGateError(
            f"Unsupported P1 manual approvals schemaVersion: {approvals.get('schemaVersion')!r}"
        )
    if approvals.get("approvalProfile") != P1_APPROVAL_PROFILE:
        raise P1LineageGateError(
            f"Unsupported P1 manual approval profile: {approvals.get('approvalProfile')!r}"
        )
    _require_false(approvals, "verified", "p1Approvals")
    _require_false(approvals, "publicationEligible", "p1Approvals")
    _require_exact_visual_tasks(
        approvals.get("coveredVisualRuntimeTasks"), "p1Approvals.coveredVisualRuntimeTasks"
    )
    approval_staging_sha = _sha40(
        approvals.get("stagingSourceGitSha"), "p1Approvals.stagingSourceGitSha"
    )
    _authorization(
        approvals.get("stagingAuthorization"),
        "p1Approvals.stagingAuthorization",
        approval_staging_sha,
    )
    records = approvals.get("approvals")
    if not isinstance(records, dict) or set(records) != set(spec["gates"]):
        raise P1LineageGateError(
            "P1 manual approvals must contain exactly the five gate IDs from p1_gate_spec.json"
        )
    for task_id, record in records.items():
        if not isinstance(record, dict):
            raise P1LineageGateError(f"P1 approval record for {task_id} must be a JSON object")
        if record.get("approved") not in {True, False}:
            raise P1LineageGateError(f"P1 approval record for {task_id}.approved must be JSON boolean")
        reviewer = str(record.get("reviewer") or "").strip()
        if record.get("approved") is True and not reviewer:
            raise P1LineageGateError(f"P1 approved record for {task_id} requires a reviewer")
    return approvals


def _bind_approvals(
    approvals: dict[str, Any] | None,
    binding: dict[str, Any],
) -> dict[str, Any] | None:
    if approvals is None:
        return None

    checks = (
        ("candidateGitSha", binding["candidateGitSha"], _sha40),
        ("apkSha256", binding["apkSha256"], _sha256),
        ("reviewContentSetSha256", binding["reviewContentSetSha256"], _sha256),
        ("stagingSourceGitSha", binding["stagingSourceGitSha"], _sha40),
        ("p1ReviewLineageSha256", binding["p1ReviewLineageSha256"], _sha256),
    )
    for key, expected, normalizer in checks:
        actual = normalizer(approvals.get(key), f"p1Approvals.{key}")
        if actual != expected:
            raise P1LineageGateError(
                f"P1 manual approval fingerprint mismatch for {key}: expected={expected} actual={actual}"
            )

    approval_authorization = _authorization(
        approvals.get("stagingAuthorization"),
        "p1Approvals.stagingAuthorization",
        binding["stagingSourceGitSha"],
    )
    if approval_authorization != binding["stagingAuthorization"]:
        raise P1LineageGateError(
            "P1 manual approval stagingAuthorization fingerprint mismatch"
        )

    tier = str(approvals.get("performanceTier") or "").strip().lower()
    if tier != binding["performanceTier"]:
        raise P1LineageGateError(
            f"P1 manual approval performanceTier mismatch: expected={binding['performanceTier']} actual={tier!r}"
        )

    digests = approvals.get("sourceArtifactDigests")
    if not isinstance(digests, dict) or set(digests) != set(binding["sourceArtifactDigests"]):
        raise P1LineageGateError("P1 manual approvals sourceArtifactDigests scope is invalid")
    for key, expected in binding["sourceArtifactDigests"].items():
        actual = _sha256(digests.get(key), f"p1Approvals.sourceArtifactDigests.{key}")
        if actual != expected:
            raise P1LineageGateError(
                f"P1 manual approval source artifact digest mismatch for {key}"
            )

    return {
        "schemaVersion": p1_gate_readiness.APPROVALS_SCHEMA_VERSION,
        "gitSha": binding["candidateGitSha"],
        "apkSha256": binding["apkSha256"],
        "reviewContentSetSha256": binding["reviewContentSetSha256"],
        "approvals": approvals["approvals"],
    }


def evaluate_p1(
    spec: dict[str, Any],
    binding: dict[str, Any],
    approvals: dict[str, Any] | None,
) -> dict[str, Any]:
    generic_approvals = _bind_approvals(approvals, binding)
    generic = p1_gate_readiness.evaluate(
        spec,
        binding["evaluationIndex"],
        approvals=generic_approvals,
    )
    return {
        "schemaVersion": OUTPUT_SCHEMA_VERSION,
        "verdict": "MANUAL_REVIEW_REQUIRED",
        "reviewProfile": verify_p1_device_review_bundle.P1_REVIEW_PROFILE,
        "approvalProfile": P1_APPROVAL_PROFILE,
        "stagingSourceGitSha": binding["stagingSourceGitSha"],
        "candidateGitSha": binding["candidateGitSha"],
        "directParentGitSha": binding["directParentGitSha"],
        "apkSha256": binding["apkSha256"],
        "reviewContentSetSha256": binding["reviewContentSetSha256"],
        "p1ReviewLineageSha256": binding["p1ReviewLineageSha256"],
        "performanceTier": binding["performanceTier"],
        "coveredVisualRuntimeTasks": list(binding["coveredVisualRuntimeTasks"]),
        "sourceArtifactDigests": dict(binding["sourceArtifactDigests"]),
        "stagingAuthorization": dict(binding["stagingAuthorization"]),
        "candidateBound": generic["candidateBound"],
        "p1ReviewBundleBound": True,
        "physicalDevice": generic["physicalDevice"],
        "automatedRedFlagCount": generic["automatedRedFlagCount"],
        "capturedCheckpoints": generic["capturedCheckpoints"],
        "gates": generic["gates"],
        "allEvidenceReady": generic["allEvidenceReady"],
        "releaseReviewReady": generic["releaseReviewReady"],
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "notes": [
            "P1 gate readiness requires a P1-verified authorization-bound sanitized review bundle; a generic review bundle is insufficient.",
            "Manual approval records are pinned to candidate/APK/content-set, staging-source SHA, P1 review-lineage SHA, performance tier, four source-artifact digests and all four staging-authorization fingerprints.",
            "releaseReviewReady is still only readiness for the final human/repository publication decision; this wrapper never marks the APK VERIFIED or publication eligible.",
        ],
    }


def _approval_template(spec: dict[str, Any], binding: dict[str, Any]) -> dict[str, Any]:
    evidence = binding["evidenceOnly"]
    if not evidence["allEvidenceReady"]:
        blockers: list[str] = []
        for task_id, gate in evidence["gates"].items():
            if task_id == p1_gate_readiness.FINAL_GATE:
                continue
            for blocker in gate.get("blockers", []):
                text = str(blocker)
                if text and text not in blockers:
                    blockers.append(text)
        detail = "; ".join(blockers) if blockers else "required physical-device evidence is incomplete"
        raise P1LineageGateError(
            "P1 approval template requires complete clean evidence for the first four manual gates: "
            + detail
        )

    return {
        "schemaVersion": P1_APPROVALS_SCHEMA_VERSION,
        "approvalProfile": P1_APPROVAL_PROFILE,
        "candidateGitSha": binding["candidateGitSha"],
        "apkSha256": binding["apkSha256"],
        "reviewContentSetSha256": binding["reviewContentSetSha256"],
        "stagingSourceGitSha": binding["stagingSourceGitSha"],
        "p1ReviewLineageSha256": binding["p1ReviewLineageSha256"],
        "performanceTier": binding["performanceTier"],
        "coveredVisualRuntimeTasks": list(binding["coveredVisualRuntimeTasks"]),
        "sourceArtifactDigests": dict(binding["sourceArtifactDigests"]),
        "stagingAuthorization": dict(binding["stagingAuthorization"]),
        "verified": False,
        "publicationEligible": False,
        "approvals": {
            task_id: {"approved": False, "reviewer": ""}
            for task_id in spec["gates"]
        },
    }


def command_validate(args: argparse.Namespace) -> int:
    spec = p1_gate_readiness.load_spec(Path(args.spec))
    binding = _bind_p1_review(Path(args.session), Path(args.review_bundle), spec)
    approvals = load_p1_approvals(Path(args.approvals).expanduser().resolve(), spec) if args.approvals else None
    result = evaluate_p1(spec, binding, approvals)
    output = (
        Path(args.output).expanduser().resolve()
        if args.output
        else Path(args.session).expanduser().resolve() / "p1-lineage-gate-readiness.json"
    )
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        "AFAREET_P1_LINEAGE_GATE_READINESS "
        f"candidateGitSha={result['candidateGitSha']} stagingSourceGitSha={result['stagingSourceGitSha']} "
        f"p1ReviewBound=true authorizationBound=true evidenceReady={str(result['allEvidenceReady']).lower()} "
        f"releaseReviewReady={str(result['releaseReviewReady']).lower()} verified=false output={output}"
    )
    return 0 if result["releaseReviewReady"] else 2


def command_approval_template(args: argparse.Namespace) -> int:
    spec = p1_gate_readiness.load_spec(Path(args.spec))
    binding = _bind_p1_review(Path(args.session), Path(args.review_bundle), spec)
    payload = _approval_template(spec, binding)
    output = Path(args.output).expanduser().resolve()
    if output.exists():
        raise P1LineageGateError(
            f"refusing to overwrite existing P1 manual approval file: {output}. Preserve reviewer decisions and choose a new path."
        )
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        "AFAREET_P1_LINEAGE_APPROVAL_TEMPLATE "
        f"candidateGitSha={payload['candidateGitSha']} stagingSourceGitSha={payload['stagingSourceGitSha']} "
        f"reviewContentSetSha256={payload['reviewContentSetSha256']} "
        f"p1ReviewLineageSha256={payload['p1ReviewLineageSha256']} authorizationBound=true "
        f"approvals=0/{len(spec['gates'])} verified=false output={output}"
    )
    return 0


def command_plan(args: argparse.Namespace) -> int:
    spec = p1_gate_readiness.load_spec(Path(args.spec))
    payload = {
        "schemaVersion": 1,
        "approvalProfile": P1_APPROVAL_PROFILE,
        "genericGateSpec": spec,
        "requiredP1Bindings": [
            "candidateGitSha",
            "apkSha256",
            "reviewContentSetSha256",
            "stagingSourceGitSha",
            "p1ReviewLineageSha256",
            "performanceTier",
            "sourceArtifactDigests",
            "stagingAuthorization",
        ],
        "coveredVisualRuntimeTasks": list(EXPECTED_VISUAL_TASKS),
        "verified": False,
        "publicationEligible": False,
    }
    print(json.dumps(payload, indent=2, sort_keys=True))
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Evaluate P1 final-five readiness with mandatory authorization-bound staged-candidate review lineage."
    )
    parser.add_argument("--spec", default=str(p1_gate_readiness.DEFAULT_SPEC))
    sub = parser.add_subparsers(dest="command", required=True)

    plan = sub.add_parser("plan", help="Print the P1 authorization-bound final-five plan.")
    plan.set_defaults(func=command_plan)

    validate = sub.add_parser(
        "validate",
        help="Evaluate a P1 physical-device session, P1 review bundle and optional authorization-bound approvals.",
    )
    validate.add_argument("--session", required=True)
    validate.add_argument("--review-bundle", required=True)
    validate.add_argument("--approvals")
    validate.add_argument("--output")
    validate.set_defaults(func=command_validate)

    template = sub.add_parser(
        "approval-template",
        help="Create a new all-false P1 approval template pinned to the exact authorization-bound review.",
    )
    template.add_argument("--session", required=True)
    template.add_argument("--review-bundle", required=True)
    template.add_argument("--output", required=True)
    template.set_defaults(func=command_approval_template)
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        return int(args.func(args))
    except (
        P1LineageGateError,
        RuntimeError,
        ValueError,
        json.JSONDecodeError,
        OSError,
    ) as exc:
        print(f"AFAREET_P1_LINEAGE_GATE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
