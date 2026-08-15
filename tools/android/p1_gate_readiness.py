#!/usr/bin/env python3
"""Evaluate readiness for the five remaining 3Fareet P1 Android gates.

The evaluator is deliberately conservative. It can prove that required evidence
exists and that explicit human approvals are present, but it never marks an APK
VERIFIED or publishes a release. Final-five readiness requires a device session
bound to a validated local/CI candidate manifest. Manual approvals additionally
must be bound to the exact content-addressed sanitized review bundle that the
reviewer inspected.
"""

from __future__ import annotations

import argparse
import hashlib
import importlib.util
import json
import re
import sys
from pathlib import Path
from typing import Any

INDEX_FILE = "evidence-index.json"
SESSION_FILE = "session.json"
BOUND_MANIFEST_FILE = "candidate-manifest.json"
DEFAULT_SPEC = Path(__file__).with_name("p1_gate_spec.json")
REVIEW_VERIFIER = Path(__file__).with_name("verify_device_review_bundle.py")
FINAL_GATE = "UPER-010"
LOCAL_CANDIDATE_TYPE = "local-windows-licensed-unity"
CI_CANDIDATE_TYPE = "github-actions-unity-ci"
ALLOWED_CANDIDATE_TYPES = {LOCAL_CANDIDATE_TYPE, CI_CANDIDATE_TYPE}
EXPECTED_CI_REPOSITORY = "walidatiyaai2025-gif/3fareet"
EXPECTED_CI_WORKFLOW = "Unity Production CI"
ALLOWED_CI_EVENTS = {"pull_request", "push", "workflow_dispatch"}
EXPECTED_CANDIDATE_VERDICT = "READY_FOR_PHYSICAL_DEVICE_EVIDENCE"
APPROVALS_SCHEMA_VERSION = 2
OUTPUT_SCHEMA_VERSION = 2
SHA40_RE = re.compile(r"^[0-9a-f]{40}$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$")
DIGITS_RE = re.compile(r"^[1-9][0-9]*$")


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise RuntimeError(f"Required JSON file is missing: {path}")
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise RuntimeError(f"JSON root must be an object: {path}")
    return payload


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def load_spec(path: Path) -> dict[str, Any]:
    spec = read_json(path)
    if spec.get("schemaVersion") != 1:
        raise RuntimeError("Unsupported P1 gate spec schemaVersion.")
    gates = spec.get("gates")
    if not isinstance(gates, dict) or FINAL_GATE not in gates:
        raise RuntimeError("P1 gate spec must define the five gates including UPER-010.")
    if len(gates) != 5:
        raise RuntimeError(f"P1 gate spec must contain exactly five gates; found {len(gates)}.")
    return spec


def load_approvals(path: Path | None) -> dict[str, Any] | None:
    if path is None:
        return None
    approvals = read_json(path)
    version = approvals.get("schemaVersion")
    if version != APPROVALS_SCHEMA_VERSION:
        if version == 1:
            raise RuntimeError(
                "Manual approvals schemaVersion 1 is no longer accepted. "
                "Re-review the content-addressed review bundle and create schemaVersion 2 approvals "
                "pinned to gitSha, apkSha256 and reviewContentSetSha256."
            )
        raise RuntimeError(f"Unsupported manual approvals schemaVersion: {version!r}.")
    return approvals


def _normalized_sha40(value: Any) -> str:
    text = str(value or "").strip().lower()
    return text if SHA40_RE.fullmatch(text) else ""


def _normalized_sha256(value: Any) -> str:
    text = str(value or "").strip().lower()
    return text if SHA256_RE.fullmatch(text) else ""


def approval_for(
    approvals: dict[str, Any] | None,
    task_id: str,
    *,
    git_sha: str,
    apk_sha: str,
    review_content_set_sha: str,
    review_bundle_bound: bool,
) -> tuple[bool, str]:
    if approvals is None:
        return False, "manual approval file not supplied"
    if not review_bundle_bound or not SHA256_RE.fullmatch(review_content_set_sha):
        return False, "verified content-addressed review bundle is not bound to this evaluation"
    if _normalized_sha40(approvals.get("gitSha")) != git_sha:
        return False, "manual approval Git SHA does not match candidate"
    if _normalized_sha256(approvals.get("apkSha256")) != apk_sha:
        return False, "manual approval APK SHA does not match evidence session"
    if _normalized_sha256(approvals.get("reviewContentSetSha256")) != review_content_set_sha:
        return False, "manual approval review-content SHA does not match verified review bundle"

    records = approvals.get("approvals", {})
    record = records.get(task_id) if isinstance(records, dict) else None
    if not isinstance(record, dict) or record.get("approved") is not True:
        return False, "explicit approved=true record is missing"
    reviewer = str(record.get("reviewer", "")).strip()
    if not reviewer:
        return False, "approval reviewer is missing"
    return True, reviewer


def candidate_blockers(index: dict[str, Any]) -> list[str]:
    blockers: list[str] = []
    binding_error = str(index.get("candidateBindingError") or "").strip()
    if binding_error:
        blockers.append(f"candidate binding invalid: {binding_error}")

    candidate = index.get("candidate")
    if not isinstance(candidate, dict):
        blockers.append("candidate provenance is missing; direct APK evidence cannot satisfy P1 gates")
        return blockers

    candidate_type = str(candidate.get("candidateType") or "").strip()
    if candidate_type not in ALLOWED_CANDIDATE_TYPES:
        blockers.append(f"unsupported candidate type: {candidate_type!r}")

    git_sha = str(candidate.get("gitSha") or "").strip().lower()
    if not SHA40_RE.fullmatch(git_sha):
        blockers.append("candidate gitSha is not a full 40-character SHA")

    apk_sha = str(index.get("apkSha256") or "").strip().lower()
    candidate_apk_sha = str(candidate.get("apkSha256") or "").strip().lower()
    if not SHA256_RE.fullmatch(candidate_apk_sha):
        blockers.append("candidate APK SHA-256 is invalid")
    elif candidate_apk_sha != apk_sha:
        blockers.append("candidate APK SHA-256 does not match evidence index")

    if candidate.get("releaseEvidenceEligible") is not True:
        blockers.append("candidate is not release-evidence eligible")
    if candidate.get("readyForDeviceEvidence") is not True:
        blockers.append("candidate is not ready for physical-device evidence")
    if candidate.get("verified") is not False:
        blockers.append("candidate must not self-assert VERIFIED state")
    if candidate.get("verdict") != EXPECTED_CANDIDATE_VERDICT:
        blockers.append("candidate verdict is not READY_FOR_PHYSICAL_DEVICE_EVIDENCE")

    manifest = candidate.get("manifest")
    if not isinstance(manifest, dict):
        blockers.append("candidate manifest binding metadata is missing")
    else:
        if manifest.get("fileName") != BOUND_MANIFEST_FILE:
            blockers.append(f"candidate manifest filename must be {BOUND_MANIFEST_FILE}")
        manifest_sha = str(manifest.get("sha256") or "").strip().lower()
        if not SHA256_RE.fullmatch(manifest_sha):
            blockers.append("candidate manifest SHA-256 is invalid")

    if candidate_type == CI_CANDIDATE_TYPE:
        github_run = candidate.get("githubRun")
        if not isinstance(github_run, dict):
            blockers.append("GitHub CI candidate is missing githubRun provenance")
        else:
            run_id = str(github_run.get("runId") or "").strip()
            run_attempt = str(github_run.get("runAttempt") or "").strip()
            repository = str(github_run.get("repository") or "").strip()
            workflow = str(github_run.get("workflow") or "").strip()
            event_name = str(github_run.get("eventName") or "").strip()
            ref = str(github_run.get("ref") or "").strip()
            if not DIGITS_RE.fullmatch(run_id) or not DIGITS_RE.fullmatch(run_attempt):
                blockers.append("GitHub CI candidate runId/runAttempt is invalid")
            if repository != EXPECTED_CI_REPOSITORY:
                blockers.append("GitHub CI candidate repository provenance is invalid")
            if workflow != EXPECTED_CI_WORKFLOW:
                blockers.append("GitHub CI candidate workflow provenance is invalid")
            if event_name not in ALLOWED_CI_EVENTS:
                blockers.append("GitHub CI candidate event provenance is invalid")
            if not ref.startswith("refs/"):
                blockers.append("GitHub CI candidate ref provenance is invalid")

    return blockers


def load_bound_candidate(
    session_dir: Path, index: dict[str, Any]
) -> tuple[dict[str, Any] | None, str | None]:
    session_path = session_dir / SESSION_FILE
    if not session_path.is_file():
        return None, f"device evidence session is missing {SESSION_FILE}"
    try:
        session = read_json(session_path)
    except (RuntimeError, json.JSONDecodeError) as exc:
        return None, str(exc)

    candidate = session.get("candidate")
    if not isinstance(candidate, dict):
        return None, "device session is not bound to a validated candidate manifest"

    session_apk = session.get("apk")
    if not isinstance(session_apk, dict):
        return candidate, "device session APK metadata is missing"
    session_apk_sha = str(session_apk.get("sha256") or "").strip().lower()
    index_apk_sha = str(index.get("apkSha256") or "").strip().lower()
    if session_apk_sha != index_apk_sha:
        return candidate, "device session APK SHA-256 does not match evidence index"
    if str(candidate.get("apkSha256") or "").strip().lower() != index_apk_sha:
        return candidate, "candidate APK SHA-256 does not match device evidence"

    manifest_record = candidate.get("manifest")
    if not isinstance(manifest_record, dict):
        return candidate, "candidate manifest binding metadata is missing from session"
    manifest_name = str(manifest_record.get("fileName") or "").strip()
    if manifest_name != BOUND_MANIFEST_FILE:
        return candidate, f"bound candidate manifest must be named {BOUND_MANIFEST_FILE}"
    manifest_path = session_dir / BOUND_MANIFEST_FILE
    if not manifest_path.is_file():
        return candidate, f"bound candidate manifest is missing: {manifest_path}"
    declared_manifest_sha = str(manifest_record.get("sha256") or "").strip().lower()
    actual_manifest_sha = sha256_file(manifest_path)
    if declared_manifest_sha != actual_manifest_sha:
        return candidate, "bound candidate manifest SHA-256 does not match session metadata"

    try:
        source_manifest = read_json(manifest_path)
    except (RuntimeError, json.JSONDecodeError) as exc:
        return candidate, str(exc)
    if source_manifest.get("candidateType") != candidate.get("candidateType"):
        return candidate, "bound manifest candidateType does not match session candidate"
    source_git_sha = str(source_manifest.get("gitSha") or "").strip().lower()
    if source_git_sha != str(candidate.get("gitSha") or "").strip().lower():
        return candidate, "bound manifest gitSha does not match session candidate"
    source_apk = source_manifest.get("apk")
    if not isinstance(source_apk, dict):
        return candidate, "bound manifest is missing APK metadata"
    if str(source_apk.get("sha256") or "").strip().lower() != index_apk_sha:
        return candidate, "bound manifest APK SHA-256 does not match device evidence"
    if source_manifest.get("releaseEvidenceEligible") is not True:
        return candidate, "bound manifest is not release-evidence eligible"
    if source_manifest.get("readyForDeviceEvidence") is not True:
        return candidate, "bound manifest is not ready for device evidence"
    if source_manifest.get("verified") is not False:
        return candidate, "bound manifest self-asserts VERIFIED state"
    if source_manifest.get("verdict") != EXPECTED_CANDIDATE_VERDICT:
        return candidate, "bound manifest verdict is not device-evidence ready"

    return candidate, None


def _load_review_verifier() -> Any:
    if not REVIEW_VERIFIER.is_file():
        raise RuntimeError(f"Review bundle verifier is missing: {REVIEW_VERIFIER}")
    spec = importlib.util.spec_from_file_location("afareet_verify_device_review_bundle", REVIEW_VERIFIER)
    if spec is None or spec.loader is None:
        raise RuntimeError(f"Unable to load review bundle verifier: {REVIEW_VERIFIER}")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


def verify_review_bundle(
    bundle_dir: Path,
    *,
    expected_git_sha: str,
    expected_apk_sha: str,
) -> dict[str, Any]:
    verifier = _load_review_verifier()
    try:
        result = verifier.verify_bundle(
            bundle_dir,
            expected_git_sha=expected_git_sha,
            expected_apk_sha=expected_apk_sha,
        )
    except Exception as exc:
        raise RuntimeError(f"review bundle verification failed: {exc}") from exc
    if not isinstance(result, dict):
        raise RuntimeError("review bundle verifier returned an invalid result")
    content_set_sha = _normalized_sha256(result.get("contentSetSha256"))
    if not content_set_sha:
        raise RuntimeError("review bundle verifier did not return a valid contentSetSha256")
    if result.get("verified") is not False:
        raise RuntimeError("review bundle verifier must not self-assert VERIFIED state")
    if result.get("verdict") != "MANUAL_REVIEW_REQUIRED":
        raise RuntimeError("review bundle verifier verdict must remain MANUAL_REVIEW_REQUIRED")
    return result


def evaluate(
    spec: dict[str, Any],
    index: dict[str, Any],
    approvals: dict[str, Any] | None = None,
) -> dict[str, Any]:
    apk_sha = str(index.get("apkSha256", "")).strip().lower()
    candidate = index.get("candidate") if isinstance(index.get("candidate"), dict) else None
    git_sha = str(candidate.get("gitSha") or "").strip().lower() if candidate else ""
    checkpoints = {str(item) for item in index.get("checkpoints", [])}
    red_flag_count = int(index.get("automatedRedFlagCount", 0) or 0)
    device = index.get("device", {}) if isinstance(index.get("device"), dict) else {}
    is_emulator = bool(device.get("isEmulator", False))
    provenance_blockers = candidate_blockers(index)

    review_binding_error = str(index.get("reviewBundleBindingError") or "").strip()
    review_content_set_sha = _normalized_sha256(index.get("reviewContentSetSha256"))
    review_bundle_bound = bool(index.get("reviewBundleBound") is True and review_content_set_sha)
    if review_binding_error:
        review_bundle_bound = False

    common_blockers: list[str] = list(provenance_blockers)
    if not apk_sha:
        common_blockers.append("evidence index has no APK SHA-256")
    if is_emulator:
        common_blockers.append("physical Android device required; emulator evidence is not accepted")
    if red_flag_count > 0:
        common_blockers.append(f"automated crash/ANR/native-fatal red flags present: {red_flag_count}")

    result_gates: dict[str, Any] = {}
    for task_id, gate in spec["gates"].items():
        if task_id == FINAL_GATE:
            continue
        required = [str(item) for item in gate.get("requiredCheckpoints", [])]
        missing = [label for label in required if label not in checkpoints]
        approved, approval_detail = approval_for(
            approvals,
            task_id,
            git_sha=git_sha,
            apk_sha=apk_sha,
            review_content_set_sha=review_content_set_sha,
            review_bundle_bound=review_bundle_bound,
        )
        blockers = list(common_blockers)
        if missing:
            blockers.append("missing checkpoints: " + ", ".join(missing))

        evidence_ready = not blockers
        if not evidence_ready:
            status = "BLOCKED_EVIDENCE"
        elif approved:
            status = "MANUALLY_APPROVED"
        else:
            status = "EVIDENCE_READY_FOR_MANUAL_REVIEW"

        result_gates[task_id] = {
            "title": gate.get("title", task_id),
            "status": status,
            "evidenceReady": evidence_ready,
            "manualApproved": approved,
            "approvalDetail": approval_detail,
            "requiredCheckpoints": required,
            "missingCheckpoints": missing,
            "blockers": blockers,
        }

    final_gate = spec["gates"][FINAL_GATE]
    dependencies = [str(item) for item in final_gate.get("dependsOnManualApprovals", [])]
    dependency_failures = [
        task_id
        for task_id in dependencies
        if not result_gates.get(task_id, {}).get("manualApproved", False)
    ]
    final_approved, final_approval_detail = approval_for(
        approvals,
        FINAL_GATE,
        git_sha=git_sha,
        apk_sha=apk_sha,
        review_content_set_sha=review_content_set_sha,
        review_bundle_bound=review_bundle_bound,
    )
    final_blockers = list(common_blockers)
    if review_binding_error:
        final_blockers.append(review_binding_error)
    if approvals is not None and not review_bundle_bound:
        final_blockers.append(
            "manual approvals require a verified content-addressed review bundle bound to this candidate"
        )
    if dependency_failures:
        final_blockers.append("manual approvals still required: " + ", ".join(dependency_failures))
    if not final_approved:
        final_blockers.append(
            "UPER-010 release approval is not explicitly approved for this exact candidate/evidence fingerprint"
        )

    release_review_ready = not final_blockers
    result_gates[FINAL_GATE] = {
        "title": final_gate.get("title", FINAL_GATE),
        "status": "READY_FOR_RELEASE_REVIEW" if release_review_ready else "BLOCKED_RELEASE_GATE",
        "evidenceReady": all(result_gates[item]["evidenceReady"] for item in dependencies),
        "manualApproved": final_approved,
        "approvalDetail": final_approval_detail,
        "dependsOnManualApprovals": dependencies,
        "blockers": final_blockers,
    }

    return {
        "schemaVersion": OUTPUT_SCHEMA_VERSION,
        "verdict": "MANUAL_REVIEW_REQUIRED",
        "apkSha256": apk_sha,
        "candidateGitSha": git_sha,
        "candidateBound": not provenance_blockers,
        "candidate": candidate,
        "reviewBundleBound": review_bundle_bound,
        "reviewContentSetSha256": review_content_set_sha or None,
        "reviewBundleBindingError": review_binding_error or None,
        "physicalDevice": not is_emulator,
        "automatedRedFlagCount": red_flag_count,
        "capturedCheckpoints": sorted(checkpoints),
        "gates": result_gates,
        "allEvidenceReady": all(result_gates[item]["evidenceReady"] for item in dependencies),
        "releaseReviewReady": release_review_ready,
        "verified": False,
        "notes": [
            "This evaluator never marks an APK VERIFIED.",
            "Direct APK device sessions without candidate-manifest binding cannot satisfy final P1 gates.",
            "Manual approvals are accepted only when pinned to the exact candidate Git SHA, APK SHA-256 and verified review content-set SHA-256.",
            "UPER-010 still requires the repository release policy and human publication decision.",
        ],
    }


def command_plan(args: argparse.Namespace) -> int:
    spec = load_spec(Path(args.spec))
    print(json.dumps(spec, indent=2, sort_keys=True))
    return 0


def command_validate(args: argparse.Namespace) -> int:
    spec = load_spec(Path(args.spec))
    session = Path(args.session).expanduser().resolve()
    index = read_json(session / INDEX_FILE)
    candidate, binding_error = load_bound_candidate(session, index)
    evaluation_index = dict(index)
    if candidate is not None:
        evaluation_index["candidate"] = candidate
    if binding_error:
        evaluation_index["candidateBindingError"] = binding_error

    candidate_git_sha = (
        str(candidate.get("gitSha") or "").strip().lower()
        if isinstance(candidate, dict)
        else ""
    )
    apk_sha = str(index.get("apkSha256") or "").strip().lower()

    if args.review_bundle:
        try:
            review_result = verify_review_bundle(
                Path(args.review_bundle).expanduser().resolve(),
                expected_git_sha=candidate_git_sha,
                expected_apk_sha=apk_sha,
            )
            evaluation_index["reviewBundleBound"] = True
            evaluation_index["reviewContentSetSha256"] = review_result["contentSetSha256"]
        except RuntimeError as exc:
            evaluation_index["reviewBundleBound"] = False
            evaluation_index["reviewBundleBindingError"] = str(exc)

    approvals = load_approvals(
        Path(args.approvals).expanduser().resolve() if args.approvals else None
    )
    result = evaluate(spec, evaluation_index, approvals)
    output = (
        Path(args.output).expanduser().resolve()
        if args.output
        else session / "p1-gate-readiness.json"
    )
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        "AFAREET_P1_GATE_READINESS "
        f"candidateBound={str(result['candidateBound']).lower()} "
        f"reviewBundleBound={str(result['reviewBundleBound']).lower()} "
        f"evidenceReady={str(result['allEvidenceReady']).lower()} "
        f"releaseReviewReady={str(result['releaseReviewReady']).lower()} "
        f"output={output}"
    )
    return 0 if result["releaseReviewReady"] else 2


def command_approval_template(args: argparse.Namespace) -> int:
    spec = load_spec(Path(args.spec))
    session = Path(args.session).expanduser().resolve()
    index = read_json(session / INDEX_FILE)
    candidate, binding_error = load_bound_candidate(session, index)
    if binding_error:
        raise RuntimeError(f"candidate binding invalid: {binding_error}")
    if not isinstance(candidate, dict):
        raise RuntimeError("candidate provenance is missing")

    git_sha = _normalized_sha40(candidate.get("gitSha"))
    apk_sha = _normalized_sha256(index.get("apkSha256"))
    if not git_sha or not apk_sha:
        raise RuntimeError("candidate Git/APK SHA is invalid")

    review_result = verify_review_bundle(
        Path(args.review_bundle).expanduser().resolve(),
        expected_git_sha=git_sha,
        expected_apk_sha=apk_sha,
    )
    review_content_set_sha = _normalized_sha256(review_result.get("contentSetSha256"))
    if not review_content_set_sha:
        raise RuntimeError("verified review bundle has no valid contentSetSha256")

    evaluation_index = dict(index)
    evaluation_index["candidate"] = candidate
    evaluation_index["reviewBundleBound"] = True
    evaluation_index["reviewContentSetSha256"] = review_content_set_sha
    result = evaluate(spec, evaluation_index, None)
    if not result["allEvidenceReady"]:
        blockers: list[str] = []
        for task_id, gate in result["gates"].items():
            if task_id == FINAL_GATE:
                continue
            for blocker in gate.get("blockers", []):
                text = str(blocker)
                if text and text not in blockers:
                    blockers.append(text)
        detail = "; ".join(blockers) if blockers else "required physical-device evidence is incomplete"
        raise RuntimeError(
            "approval template requires complete clean evidence for the first four manual gates: "
            + detail
        )

    output = Path(args.output).expanduser().resolve()
    if output.exists():
        raise RuntimeError(
            f"refusing to overwrite existing manual approval file: {output}. "
            "Preserve reviewer decisions and choose a new path."
        )
    output.parent.mkdir(parents=True, exist_ok=True)
    payload = {
        "schemaVersion": APPROVALS_SCHEMA_VERSION,
        "gitSha": git_sha,
        "apkSha256": apk_sha,
        "reviewContentSetSha256": review_content_set_sha,
        "approvals": {
            task_id: {"approved": False, "reviewer": ""}
            for task_id in spec["gates"]
        },
    }
    output.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        "AFAREET_P1_APPROVAL_TEMPLATE "
        f"gitSha={git_sha} apkSha256={apk_sha} "
        f"reviewContentSetSha256={review_content_set_sha} "
        f"approvals=0/{len(spec['gates'])} output={output}"
    )
    return 0


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Evaluate 3Fareet P1 final-five Android gate readiness."
    )
    parser.add_argument("--spec", default=str(DEFAULT_SPEC), help="Path to p1_gate_spec.json")
    sub = parser.add_subparsers(dest="command", required=True)

    plan = sub.add_parser("plan", help="Print the declarative five-gate evidence plan.")
    plan.set_defaults(func=command_plan)

    validate = sub.add_parser(
        "validate",
        help="Evaluate a candidate-bound device evidence session and optional manual approvals.",
    )
    validate.add_argument(
        "--session",
        required=True,
        help="Evidence session directory containing session.json/evidence-index.json",
    )
    validate.add_argument(
        "--review-bundle",
        help="Optional sanitized content-addressed review bundle to verify and bind to manual approvals.",
    )
    validate.add_argument(
        "--approvals",
        help=(
            "Optional schema-v2 manual approvals JSON pinned to the same candidate Git SHA, "
            "APK SHA-256 and verified review content-set SHA-256."
        ),
    )
    validate.add_argument("--output", help="Optional readiness JSON output path")
    validate.set_defaults(func=command_validate)

    approval_template = sub.add_parser(
        "approval-template",
        help="Create a fail-closed schema-v2 approval template with every approval set to false.",
    )
    approval_template.add_argument(
        "--session",
        required=True,
        help="Complete candidate-bound physical-device evidence session.",
    )
    approval_template.add_argument(
        "--review-bundle",
        required=True,
        help="Sanitized content-addressed review bundle that verifies for the same candidate.",
    )
    approval_template.add_argument(
        "--output",
        required=True,
        help="New manual-approvals JSON path. Existing files are never overwritten.",
    )
    approval_template.set_defaults(func=command_approval_template)
    return parser


def main(argv: list[str] | None = None) -> int:
    parser = build_parser()
    args = parser.parse_args(argv)
    try:
        return int(args.func(args))
    except (RuntimeError, ValueError, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())