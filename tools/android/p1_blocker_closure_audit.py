#!/usr/bin/env python3
"""Read-only evidence inventory audit for the authoritative U-P1 blockers.

The operational ledger remains GitHub Issue #90. This tool consumes a caller-supplied
snapshot of that issue body, proves that the fixed 65-task / 11-blocker contract has not
drifted, and inventories explicit evidence files for each blocked task.

Evidence presence is deliberately weaker than acceptance. Even a complete inventory is
only ready for human closure review; this tool never changes task state, grants owner or
runtime acceptance, publishes a release, or marks anything VERIFIED.
"""
from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any, Sequence

EXPECTED_REGISTER_SIZE = 65
EXPECTED_IN_REVIEW = 54
EXPECTED_BLOCKED = 11
EXPECTED_TASK_IDS = (
    "UART-003",
    "UART-004",
    "UART-005",
    "UART-006",
    "UART-007",
    "URAC-011",
    "UVEH-012",
    "URAC-012",
    "UPER-006",
    "UPER-009",
    "UPER-010",
)

REQUIRED_EVIDENCE = {
    "UART-003": (
        "hero-production-source",
        "licensed-runtime-proof",
        "owner-visual-acceptance",
    ),
    "UART-004": (
        "licensed-runtime-proof",
        "owner-visual-acceptance",
    ),
    "UART-005": (
        "licensed-runtime-proof",
        "physical-device-proof",
        "owner-visual-acceptance",
    ),
    "UART-006": (
        "licensed-runtime-proof",
        "physical-device-proof",
        "owner-visual-acceptance",
    ),
    "UART-007": (
        "licensed-runtime-proof",
        "physical-device-proof",
        "owner-visual-acceptance",
    ),
    "URAC-011": (
        "exact-candidate-runtime-proof",
        "physical-device-proof",
        "owner-visual-acceptance",
    ),
    "UVEH-012": ("physical-device-driving-feel-acceptance",),
    "URAC-012": ("physical-device-lap-results-restart-proof",),
    "UPER-006": ("android-smoke-performance-matrix",),
    "UPER-009": ("owner-art-director-visual-acceptance",),
    "UPER-010": ("manual-publication-approval",),
}

AGGREGATE_RE = re.compile(
    r"`?IN REVIEW\s+(\d+)\s*\|\s*READY\s+(\d+)\s*\|\s*TODO\s+(\d+)\s*\|\s*BLOCKED\s+(\d+)\s*=\s*(\d+)`?",
    flags=re.IGNORECASE,
)
BLOCKER_HEADING_RE = re.compile(r"^##\s+Blocked tasks\b", flags=re.IGNORECASE)
BLOCKER_RE = re.compile(r"^\s*\d+\.\s+([A-Z]+-\d+)\s+[—-]\s+(.+?)\s*$")
SHA256_RE = re.compile(r"^[0-9a-f]{64}$", flags=re.IGNORECASE)


class P1ClosureAuditError(RuntimeError):
    pass


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _read_text(path: Path, label: str) -> str:
    if not path.is_file():
        raise P1ClosureAuditError(f"{label} is missing: {path}")
    try:
        return path.read_text(encoding="utf-8-sig")
    except OSError as exc:
        raise P1ClosureAuditError(f"cannot read {label}: {path}: {exc}") from exc


def parse_ledger(text: str) -> dict[str, Any]:
    aggregate_match = AGGREGATE_RE.search(text)
    if aggregate_match is None:
        raise P1ClosureAuditError("Issue #90 aggregate state line is missing or malformed")

    in_review, ready, todo, blocked, total = (int(value) for value in aggregate_match.groups())
    if (in_review, ready, todo, blocked, total) != (
        EXPECTED_IN_REVIEW,
        0,
        0,
        EXPECTED_BLOCKED,
        EXPECTED_REGISTER_SIZE,
    ):
        raise P1ClosureAuditError(
            "Issue #90 fixed-register aggregate drifted: "
            f"IN REVIEW={in_review} READY={ready} TODO={todo} BLOCKED={blocked} TOTAL={total}"
        )

    lines = text.splitlines()
    start = next((index for index, line in enumerate(lines) if BLOCKER_HEADING_RE.search(line)), None)
    if start is None:
        raise P1ClosureAuditError("Issue #90 blocked-task section is missing")

    blockers: list[dict[str, str]] = []
    seen: set[str] = set()
    for line in lines[start + 1 :]:
        if line.startswith("## ") and blockers:
            break
        match = BLOCKER_RE.match(line)
        if match is None:
            continue
        task_id, description = match.groups()
        if task_id in seen:
            raise P1ClosureAuditError(f"Issue #90 repeats blocked task {task_id}")
        seen.add(task_id)
        blockers.append({"taskId": task_id, "description": description.strip()})

    task_ids = tuple(item["taskId"] for item in blockers)
    if len(blockers) != EXPECTED_BLOCKED:
        raise P1ClosureAuditError(
            f"Issue #90 must contain exactly {EXPECTED_BLOCKED} blocked tasks, found {len(blockers)}"
        )
    if task_ids != EXPECTED_TASK_IDS:
        raise P1ClosureAuditError(
            "Issue #90 blocked-task identity/order drifted; conscious contract update required: "
            f"found={list(task_ids)} expected={list(EXPECTED_TASK_IDS)}"
        )

    return {
        "aggregate": {
            "inReview": in_review,
            "ready": ready,
            "todo": todo,
            "blocked": blocked,
            "total": total,
        },
        "blockers": blockers,
    }


def _read_json(path: Path, label: str) -> dict[str, Any]:
    if not path.is_file():
        raise P1ClosureAuditError(f"{label} is missing: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1ClosureAuditError(f"{label} is not valid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1ClosureAuditError(f"{label} root must be a JSON object")
    return payload


def load_evidence_index(index_path: Path | None) -> dict[str, list[dict[str, str]]]:
    if index_path is None:
        return {}
    payload = _read_json(index_path, "closure evidence index")
    if payload.get("schemaVersion") != 1 or payload.get("state") != "P1_BLOCKER_CLOSURE_EVIDENCE_INDEX":
        raise P1ClosureAuditError("closure evidence index schema/state mismatch")
    tasks = payload.get("tasks")
    if not isinstance(tasks, dict):
        raise P1ClosureAuditError("closure evidence index tasks must be an object")

    normalized: dict[str, list[dict[str, str]]] = {}
    for task_id, raw_items in tasks.items():
        if task_id not in EXPECTED_TASK_IDS:
            raise P1ClosureAuditError(f"closure evidence index contains unknown task: {task_id}")
        if not isinstance(raw_items, list):
            raise P1ClosureAuditError(f"closure evidence for {task_id} must be a list")
        seen_types: set[str] = set()
        items: list[dict[str, str]] = []
        for raw in raw_items:
            if not isinstance(raw, dict):
                raise P1ClosureAuditError(f"closure evidence record for {task_id} must be an object")
            evidence_type = str(raw.get("type") or "").strip()
            relative_path = str(raw.get("path") or "").strip()
            declared_sha = str(raw.get("sha256") or "").strip().lower()
            if evidence_type not in REQUIRED_EVIDENCE[task_id]:
                raise P1ClosureAuditError(
                    f"unsupported evidence type for {task_id}: {evidence_type!r}; "
                    f"expected one of {list(REQUIRED_EVIDENCE[task_id])}"
                )
            if evidence_type in seen_types:
                raise P1ClosureAuditError(f"duplicate evidence type for {task_id}: {evidence_type}")
            if not relative_path:
                raise P1ClosureAuditError(f"evidence path is missing for {task_id}/{evidence_type}")
            if not SHA256_RE.fullmatch(declared_sha):
                raise P1ClosureAuditError(f"invalid SHA-256 for {task_id}/{evidence_type}")
            seen_types.add(evidence_type)
            items.append({"type": evidence_type, "path": relative_path, "sha256": declared_sha})
        normalized[task_id] = items
    return normalized


def _resolve_evidence_path(root: Path, relative_path: str) -> Path:
    raw = Path(relative_path)
    if raw.is_absolute():
        raise P1ClosureAuditError(f"evidence path must be relative to --evidence-root: {relative_path}")
    resolved_root = root.expanduser().resolve()
    candidate = (resolved_root / raw).resolve()
    try:
        candidate.relative_to(resolved_root)
    except ValueError as exc:
        raise P1ClosureAuditError(f"evidence path escapes --evidence-root: {relative_path}") from exc
    return candidate


def audit(
    ledger_path: Path,
    *,
    evidence_index_path: Path | None = None,
    evidence_root: Path | None = None,
) -> dict[str, Any]:
    ledger_bytes = ledger_path.read_bytes() if ledger_path.is_file() else b""
    if not ledger_bytes:
        raise P1ClosureAuditError(f"Issue #90 ledger snapshot is missing or empty: {ledger_path}")
    try:
        ledger_text = ledger_bytes.decode("utf-8-sig")
    except UnicodeDecodeError as exc:
        raise P1ClosureAuditError(f"Issue #90 ledger snapshot is not UTF-8: {ledger_path}") from exc

    ledger = parse_ledger(ledger_text)
    index = load_evidence_index(evidence_index_path)
    root = (evidence_root or (evidence_index_path.parent if evidence_index_path else ledger_path.parent)).expanduser().resolve()

    task_results: list[dict[str, Any]] = []
    complete_count = 0
    for ledger_task in ledger["blockers"]:
        task_id = ledger_task["taskId"]
        required = list(REQUIRED_EVIDENCE[task_id])
        declared_records = index.get(task_id, [])
        present_types: list[str] = []
        evidence_records: list[dict[str, str]] = []

        for record in declared_records:
            path = _resolve_evidence_path(root, record["path"])
            if not path.is_file() or path.stat().st_size <= 0:
                raise P1ClosureAuditError(
                    f"declared evidence is missing or empty for {task_id}/{record['type']}: {path}"
                )
            actual_sha = sha256_file(path)
            if actual_sha.lower() != record["sha256"].lower():
                raise P1ClosureAuditError(
                    f"evidence SHA-256 mismatch for {task_id}/{record['type']}: "
                    f"declared={record['sha256']} actual={actual_sha}"
                )
            present_types.append(record["type"])
            evidence_records.append(
                {
                    "type": record["type"],
                    "path": record["path"],
                    "sha256": actual_sha,
                }
            )

        missing = [item for item in required if item not in present_types]
        inventory_complete = not missing
        if inventory_complete:
            complete_count += 1
        task_results.append(
            {
                "taskId": task_id,
                "ledgerDescription": ledger_task["description"],
                "requiredEvidenceTypes": required,
                "presentEvidenceTypes": present_types,
                "missingEvidenceTypes": missing,
                "evidence": evidence_records,
                "inventoryCompleteForHumanReview": inventory_complete,
                "verified": False,
                "taskStateMutationPerformed": False,
            }
        )

    missing_task_ids = [
        item["taskId"] for item in task_results if not item["inventoryCompleteForHumanReview"]
    ]
    return {
        "schemaVersion": 1,
        "state": "P1_BLOCKER_CLOSURE_AUDIT",
        "verdict": (
            "P1_BLOCKER_EVIDENCE_INVENTORY_COMPLETE_FOR_HUMAN_REVIEW"
            if not missing_task_ids
            else "P1_BLOCKER_EVIDENCE_MISSING"
        ),
        "ledger": {
            "issueNumber": 90,
            "sha256": sha256_bytes(ledger_bytes),
            **ledger["aggregate"],
        },
        "summary": {
            "blockerCount": len(task_results),
            "inventoryCompleteCount": complete_count,
            "missingEvidenceTaskCount": len(missing_task_ids),
            "missingEvidenceTaskIds": missing_task_ids,
        },
        "tasks": task_results,
        "humanClosureReviewRequired": True,
        "taskStateMutationPerformed": False,
        "publicationPerformed": False,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--ledger", required=True, help="UTF-8 snapshot of authoritative GitHub Issue #90 body")
    parser.add_argument("--evidence-index", help="Optional P1_BLOCKER_CLOSURE_EVIDENCE_INDEX JSON")
    parser.add_argument(
        "--evidence-root",
        help="Root for paths declared by --evidence-index; defaults to the index directory (or ledger directory)",
    )
    parser.add_argument("--output", help="Optional JSON audit output; existing files are never overwritten")
    parser.add_argument(
        "--require-complete",
        action="store_true",
        help="Return nonzero when any authoritative blocker still lacks required evidence inventory",
    )
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = audit(
            Path(args.ledger).expanduser().resolve(),
            evidence_index_path=(Path(args.evidence_index).expanduser().resolve() if args.evidence_index else None),
            evidence_root=(Path(args.evidence_root).expanduser().resolve() if args.evidence_root else None),
        )
        rendered = json.dumps(result, indent=2, sort_keys=True) + "\n"
        if args.output:
            output = Path(args.output).expanduser().resolve()
            if output.exists():
                raise P1ClosureAuditError(f"refusing to overwrite existing closure audit: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(rendered, encoding="utf-8")
        else:
            sys.stdout.write(rendered)

        print(
            "AFAREET_P1_BLOCKER_CLOSURE_AUDIT "
            f"blockers={result['summary']['blockerCount']} "
            f"inventoryComplete={result['summary']['inventoryCompleteCount']} "
            f"missing={result['summary']['missingEvidenceTaskCount']} "
            "taskStateMutationPerformed=false publicationPerformed=false verified=false",
            file=sys.stderr,
        )
        if args.require_complete and result["summary"]["missingEvidenceTaskCount"]:
            return 2
        return 0
    except (OSError, P1ClosureAuditError) as exc:
        print(f"AFAREET_P1_BLOCKER_CLOSURE_AUDIT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
