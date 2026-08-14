#!/usr/bin/env python3
"""Evaluate readiness for the five remaining 3Fareet P1 Android gates.

The evaluator is deliberately conservative. It can prove that required evidence
exists and that explicit human approvals are present, but it never marks an APK
VERIFIED or publishes a release.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

INDEX_FILE = "evidence-index.json"
DEFAULT_SPEC = Path(__file__).with_name("p1_gate_spec.json")
FINAL_GATE = "UPER-010"


def read_json(path: Path) -> dict[str, Any]:
    if not path.is_file():
        raise RuntimeError(f"Required JSON file is missing: {path}")
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise RuntimeError(f"JSON root must be an object: {path}")
    return payload


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
    if approvals.get("schemaVersion") != 1:
        raise RuntimeError("Unsupported manual approvals schemaVersion.")
    return approvals


def approval_for(approvals: dict[str, Any] | None, task_id: str, apk_sha: str) -> tuple[bool, str]:
    if approvals is None:
        return False, "manual approval file not supplied"
    if approvals.get("apkSha256") != apk_sha:
        return False, "manual approval APK SHA does not match evidence session"
    records = approvals.get("approvals", {})
    record = records.get(task_id) if isinstance(records, dict) else None
    if not isinstance(record, dict) or record.get("approved") is not True:
        return False, "explicit approved=true record is missing"
    reviewer = str(record.get("reviewer", "")).strip()
    if not reviewer:
        return False, "approval reviewer is missing"
    return True, reviewer


def evaluate(spec: dict[str, Any], index: dict[str, Any], approvals: dict[str, Any] | None = None) -> dict[str, Any]:
    apk_sha = str(index.get("apkSha256", "")).strip()
    checkpoints = {str(item) for item in index.get("checkpoints", [])}
    red_flag_count = int(index.get("automatedRedFlagCount", 0) or 0)
    device = index.get("device", {}) if isinstance(index.get("device"), dict) else {}
    is_emulator = bool(device.get("isEmulator", False))

    common_blockers: list[str] = []
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
        approved, approval_detail = approval_for(approvals, task_id, apk_sha)
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
        task_id for task_id in dependencies
        if not result_gates.get(task_id, {}).get("manualApproved", False)
    ]
    final_approved, final_approval_detail = approval_for(approvals, FINAL_GATE, apk_sha)
    final_blockers = list(common_blockers)
    if dependency_failures:
        final_blockers.append("manual approvals still required: " + ", ".join(dependency_failures))
    if not final_approved:
        final_blockers.append("UPER-010 release approval is not explicitly approved for this APK SHA")

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
        "schemaVersion": 1,
        "verdict": "MANUAL_REVIEW_REQUIRED",
        "apkSha256": apk_sha,
        "physicalDevice": not is_emulator,
        "automatedRedFlagCount": red_flag_count,
        "capturedCheckpoints": sorted(checkpoints),
        "gates": result_gates,
        "allEvidenceReady": all(result_gates[item]["evidenceReady"] for item in dependencies),
        "releaseReviewReady": release_review_ready,
        "verified": False,
        "notes": [
            "This evaluator never marks an APK VERIFIED.",
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
    approvals = load_approvals(Path(args.approvals).expanduser().resolve() if args.approvals else None)
    result = evaluate(spec, index, approvals)
    output = Path(args.output).expanduser().resolve() if args.output else session / "p1-gate-readiness.json"
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    print(
        "AFAREET_P1_GATE_READINESS "
        f"evidenceReady={str(result['allEvidenceReady']).lower()} "
        f"releaseReviewReady={str(result['releaseReviewReady']).lower()} "
        f"output={output}"
    )
    return 0 if result["releaseReviewReady"] else 2


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Evaluate 3Fareet P1 final-five Android gate readiness.")
    parser.add_argument("--spec", default=str(DEFAULT_SPEC), help="Path to p1_gate_spec.json")
    sub = parser.add_subparsers(dest="command", required=True)

    plan = sub.add_parser("plan", help="Print the declarative five-gate evidence plan.")
    plan.set_defaults(func=command_plan)

    validate = sub.add_parser("validate", help="Evaluate a device evidence session and optional manual approvals.")
    validate.add_argument("--session", required=True, help="Evidence session directory containing evidence-index.json")
    validate.add_argument("--approvals", help="Optional manual approvals JSON pinned to the same APK SHA")
    validate.add_argument("--output", help="Optional readiness JSON output path")
    validate.set_defaults(func=command_validate)
    return parser


def main() -> int:
    parser = build_parser()
    args = parser.parse_args()
    try:
        return int(args.func(args))
    except (RuntimeError, ValueError, json.JSONDecodeError) as exc:
        print(f"ERROR: {exc}", file=__import__("sys").stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
