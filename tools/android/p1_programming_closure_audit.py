#!/usr/bin/env python3
"""Fail-closed audit for the U-P1 programming-closure phase.

This does NOT mark tasks DONE/VERIFIED. It only proves that the active Unity register
contains no explicit TODO/READY programming queue and that every BLOCKED task is one
of the known external asset/device/owner/publication evidence gates.
"""

from __future__ import annotations

import argparse
import json
import re
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


REPO_ROOT = Path(__file__).resolve().parents[2]
REGISTER = REPO_ROOT / "docs/tasks/06-UNITY-3D-MIGRATION.md"
ASSET_POLICY = REPO_ROOT / "EXTERNAL_ASSET_REQUESTS.txt"

KNOWN_EXTERNAL_BLOCKERS = {
    "UART-003": "external authored Hero source + licensed binding + owner acceptance",
    "UART-004": "licensed Rival production binding/runtime/owner proof",
    "UART-005": "licensed Cairo street-kit runtime/device/owner proof",
    "UART-006": "licensed landmark runtime/device/owner proof",
    "UART-007": "licensed dressing runtime/device/owner proof",
    "URAC-011": "exact-candidate authored runtime/device/owner proof",
    "UVEH-012": "physical-device driving-feel acceptance",
    "URAC-012": "physical-device lap/results/restart verification",
    "UPER-006": "fresh Android smoke/profiler/performance matrix",
    "UPER-009": "owner/Art Director Visual Gate",
    "UPER-010": "manual publication approval",
}

ACTIVE_STATUSES = {"TODO", "READY", "IN PROGRESS", "BLOCKED", "IN REVIEW", "DONE", "VERIFIED"}
ROW_RE = re.compile(r"^\|\s*([A-Z][A-Z0-9-]+)\s*\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|\s*([^|]+)\|")
AGGREGATE_RE = re.compile(
    r"U-P1 aggregate:\*\*\s*`IN REVIEW\s+(\d+)\s*\|\s*READY\s+(\d+)\s*\|\s*TODO\s+(\d+)\s*\|\s*BLOCKED\s+(\d+)\s*=\s*(\d+)`"
)


@dataclass(frozen=True)
class TaskRow:
    task_id: str
    priority: str
    task: str
    owner: str
    status: str


def parse_rows(text: str) -> list[TaskRow]:
    rows: list[TaskRow] = []
    for raw in text.splitlines():
        match = ROW_RE.match(raw.strip())
        if not match:
            continue
        task_id, priority, task, owner, status = (part.strip() for part in match.groups())
        if status not in ACTIVE_STATUSES:
            continue
        rows.append(TaskRow(task_id, priority, task, owner, status))
    return rows


def duplicates(values: Iterable[str]) -> list[str]:
    seen: set[str] = set()
    dupes: set[str] = set()
    for value in values:
        if value in seen:
            dupes.add(value)
        seen.add(value)
    return sorted(dupes)


def audit(register_text: str, asset_policy_text: str) -> dict:
    rows = parse_rows(register_text)
    if not rows:
        raise RuntimeError("PROGRAMMING_CLOSURE_BLOCKED reason=no-active-task-rows")

    duplicate_ids = duplicates(row.task_id for row in rows)
    if duplicate_ids:
        raise RuntimeError("PROGRAMMING_CLOSURE_BLOCKED reason=duplicate-task-ids ids=" + ",".join(duplicate_ids))

    explicit_programming_queue = [row.task_id for row in rows if row.status in {"TODO", "READY", "IN PROGRESS"}]
    if explicit_programming_queue:
        raise RuntimeError(
            "PROGRAMMING_CLOSURE_BLOCKED reason=explicit-programming-queue ids=" + ",".join(explicit_programming_queue)
        )

    blocked = {row.task_id for row in rows if row.status == "BLOCKED"}
    expected_blocked = set(KNOWN_EXTERNAL_BLOCKERS)
    unexpected_blocked = sorted(blocked - expected_blocked)
    missing_known_blockers = sorted(expected_blocked - blocked)
    if unexpected_blocked:
        raise RuntimeError("PROGRAMMING_CLOSURE_BLOCKED reason=unexpected-blocked-task ids=" + ",".join(unexpected_blocked))
    if missing_known_blockers:
        raise RuntimeError("PROGRAMMING_CLOSURE_BLOCKED reason=known-blocker-set-drift missing=" + ",".join(missing_known_blockers))

    aggregate_match = AGGREGATE_RE.search(register_text)
    if not aggregate_match:
        raise RuntimeError("PROGRAMMING_CLOSURE_BLOCKED reason=aggregate-missing")
    in_review, ready, todo, aggregate_blocked, total = map(int, aggregate_match.groups())
    if ready != 0 or todo != 0:
        raise RuntimeError(f"PROGRAMMING_CLOSURE_BLOCKED reason=aggregate-programming-queue ready={ready} todo={todo}")
    if aggregate_blocked != len(expected_blocked):
        raise RuntimeError(
            f"PROGRAMMING_CLOSURE_BLOCKED reason=aggregate-blocker-count expected={len(expected_blocked)} actual={aggregate_blocked}"
        )
    if total != len(rows):
        raise RuntimeError(f"PROGRAMMING_CLOSURE_BLOCKED reason=aggregate-total-mismatch aggregate={total} parsed={len(rows)}")

    for required in (
        "EXTERNAL ASSET REQUEST POLICY & ACTIVE REQUESTS",
        "POLICY — MANDATORY FOR EVERY PROGRAMMER / AI AGENT",
        "Programming first.",
        "Prompts must be ready to copy/paste",
    ):
        if required not in asset_policy_text:
            raise RuntimeError("PROGRAMMING_CLOSURE_BLOCKED reason=asset-policy-contract-missing token=" + required)

    return {
        "status": "PROGRAMMING_CLOSURE_QUEUE_CLEAR",
        "task_total": len(rows),
        "in_review": in_review,
        "blocked_external_only": len(blocked),
        "explicit_programming_queue": 0,
        "external_blockers": [
            {"task_id": task_id, "reason": KNOWN_EXTERNAL_BLOCKERS[task_id]}
            for task_id in sorted(KNOWN_EXTERNAL_BLOCKERS)
        ],
        "verified_unchanged": True,
        "p1_status_promoted": False,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Audit U-P1 programming closure without promoting P1 status")
    parser.add_argument("--json", action="store_true", help="emit machine-readable JSON")
    args = parser.parse_args()

    result = audit(REGISTER.read_text(encoding="utf-8"), ASSET_POLICY.read_text(encoding="utf-8"))
    if args.json:
        print(json.dumps(result, indent=2, sort_keys=True))
    else:
        print(
            "PROGRAMMING_CLOSURE_QUEUE_CLEAR "
            f"tasks={result['task_total']} inReview={result['in_review']} "
            f"blockedExternalOnly={result['blocked_external_only']} programmingQueue=0 "
            "verifiedUnchanged=true p1StatusPromoted=false canonicalAssetLedger=EXTERNAL_ASSET_REQUESTS.txt"
        )
        for blocker in result["external_blockers"]:
            print(f"PROGRAMMING_CLOSURE_EXTERNAL_BLOCKER {blocker['task_id']} reason={blocker['reason']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
