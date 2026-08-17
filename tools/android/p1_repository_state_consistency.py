#!/usr/bin/env python3
"""Fail-closed consistency check between Issue #90 and in-repository U-P1 status sources.

This tool is read-only with respect to project/task/release state. It verifies that the
fixed 65-task Unity P1 register and PROJECT_STATUS snapshot expose the same aggregate and
the same 11 blockers as the caller-supplied authoritative Issue #90 body.
"""
from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any, Sequence

EXPECTED_REGISTER_SIZE = 65
EXPECTED_IN_REVIEW = 54
EXPECTED_BLOCKED = 11
EXPECTED_BLOCKER_IDS = (
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
EXPECTED_REGISTER_TASK_IDS = tuple(
    [f"U3D-{i:03d}" for i in range(1, 13)]
    + [f"UVEH-{i:03d}" for i in range(1, 13)]
    + [f"URAC-{i:03d}" for i in range(1, 13)]
    + [f"UART-{i:03d}" for i in range(1, 9)]
    + [f"UVFX-{i:03d}" for i in range(1, 4)]
    + [f"UUI-{i:03d}" for i in range(1, 6)]
    + [f"UAUD-{i:03d}" for i in range(1, 4)]
    + [f"UPER-{i:03d}" for i in range(1, 11)]
)

AGGREGATE_RE = re.compile(
    r"`?IN REVIEW\s+(\d+)\s*\|\s*READY\s+(\d+)\s*\|\s*TODO\s+(\d+)\s*\|\s*BLOCKED\s+(\d+)\s*=\s*(\d+)`?",
    flags=re.IGNORECASE,
)
ISSUE_BLOCKER_HEADING_RE = re.compile(r"^##\s+Blocked tasks\b", flags=re.IGNORECASE)
PROJECT_BLOCKER_HEADING_RE = re.compile(r"^##\s+Authoritative P1 blockers\b", flags=re.IGNORECASE)
BLOCKER_RE = re.compile(r"^\s*\d+\.\s+([A-Z]+-\d+)\s+[—-]\s+(.+?)\s*$")
TASK_ID_RE = re.compile(r"^(?:U3D|UVEH|URAC|UART|UVFX|UUI|UAUD|UPER)-\d{3}$")
ALLOWED_OPERATIONAL_STATES = {"IN REVIEW", "BLOCKED"}


class P1RepositoryStateError(RuntimeError):
    pass


def _read_text(path: Path, label: str) -> str:
    if not path.is_file():
        raise P1RepositoryStateError(f"{label} is missing: {path}")
    try:
        return path.read_text(encoding="utf-8-sig")
    except OSError as exc:
        raise P1RepositoryStateError(f"cannot read {label}: {path}: {exc}") from exc


def _parse_aggregate(text: str, label: str) -> dict[str, int]:
    matches = [tuple(int(value) for value in match.groups()) for match in AGGREGATE_RE.finditer(text)]
    if not matches:
        raise P1RepositoryStateError(f"{label} aggregate state is missing or malformed")
    unique = set(matches)
    if len(unique) != 1:
        raise P1RepositoryStateError(f"{label} contains conflicting aggregate states: {matches}")
    in_review, ready, todo, blocked, total = matches[0]
    return {
        "inReview": in_review,
        "ready": ready,
        "todo": todo,
        "blocked": blocked,
        "total": total,
    }


def _parse_blockers(text: str, label: str, heading_re: re.Pattern[str]) -> list[dict[str, str]]:
    lines = text.splitlines()
    start = next((index for index, line in enumerate(lines) if heading_re.search(line)), None)
    if start is None:
        raise P1RepositoryStateError(f"{label} blocker section is missing")

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
            raise P1RepositoryStateError(f"{label} repeats blocker {task_id}")
        seen.add(task_id)
        blockers.append({"taskId": task_id, "description": description.strip()})

    if not blockers:
        raise P1RepositoryStateError(f"{label} blocker section contains no tasks")
    return blockers


def parse_authoritative_ledger(text: str) -> dict[str, Any]:
    aggregate = _parse_aggregate(text, "Issue #90")
    expected = {
        "inReview": EXPECTED_IN_REVIEW,
        "ready": 0,
        "todo": 0,
        "blocked": EXPECTED_BLOCKED,
        "total": EXPECTED_REGISTER_SIZE,
    }
    if aggregate != expected:
        raise P1RepositoryStateError(f"Issue #90 fixed-register aggregate drifted: {aggregate}")

    blockers = _parse_blockers(text, "Issue #90", ISSUE_BLOCKER_HEADING_RE)
    task_ids = tuple(item["taskId"] for item in blockers)
    if task_ids != EXPECTED_BLOCKER_IDS:
        raise P1RepositoryStateError(
            "Issue #90 blocker identity/order drifted; conscious contract update required: "
            f"found={list(task_ids)} expected={list(EXPECTED_BLOCKER_IDS)}"
        )
    return {"aggregate": aggregate, "blockers": blockers}


def parse_project_status(text: str) -> dict[str, Any]:
    return {
        "aggregate": _parse_aggregate(text, "PROJECT_STATUS.md"),
        "blockers": _parse_blockers(text, "PROJECT_STATUS.md", PROJECT_BLOCKER_HEADING_RE),
    }


def parse_task_register(text: str) -> dict[str, Any]:
    task_ids: list[str] = []
    statuses: dict[str, str] = {}
    for line in text.splitlines():
        if not line.lstrip().startswith("|"):
            continue
        cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
        if len(cells) < 5:
            continue
        task_id = cells[0]
        if TASK_ID_RE.fullmatch(task_id) is None:
            continue
        if task_id in statuses:
            raise P1RepositoryStateError(f"Unity task register repeats task {task_id}")
        status = cells[4].upper()
        task_ids.append(task_id)
        statuses[task_id] = status

    if tuple(task_ids) != EXPECTED_REGISTER_TASK_IDS:
        raise P1RepositoryStateError(
            "Unity task register identity/order drifted; "
            f"found={task_ids} expected={list(EXPECTED_REGISTER_TASK_IDS)}"
        )

    invalid = {task_id: state for task_id, state in statuses.items() if state not in ALLOWED_OPERATIONAL_STATES}
    if invalid:
        raise P1RepositoryStateError(
            "Unity task register contains non-operational P1 states; expected only IN REVIEW/BLOCKED: "
            f"{invalid}"
        )

    blocked_ids = [task_id for task_id in task_ids if statuses[task_id] == "BLOCKED"]
    in_review = sum(1 for state in statuses.values() if state == "IN REVIEW")
    aggregate = {
        "inReview": in_review,
        "ready": 0,
        "todo": 0,
        "blocked": len(blocked_ids),
        "total": len(task_ids),
    }
    return {"aggregate": aggregate, "blockedTaskIds": blocked_ids, "statuses": statuses}


def verify_consistency(
    ledger_text: str,
    project_status_text: str,
    task_register_text: str,
) -> dict[str, Any]:
    ledger = parse_authoritative_ledger(ledger_text)
    project = parse_project_status(project_status_text)
    task_register = parse_task_register(task_register_text)

    if project["aggregate"] != ledger["aggregate"]:
        raise P1RepositoryStateError(
            f"PROJECT_STATUS.md aggregate disagrees with Issue #90: "
            f"project={project['aggregate']} ledger={ledger['aggregate']}"
        )
    project_blockers = tuple(item["taskId"] for item in project["blockers"])
    ledger_blockers = tuple(item["taskId"] for item in ledger["blockers"])
    if project_blockers != ledger_blockers:
        raise P1RepositoryStateError(
            "PROJECT_STATUS.md blocker identity/order disagrees with Issue #90: "
            f"project={list(project_blockers)} ledger={list(ledger_blockers)}"
        )

    if task_register["aggregate"] != ledger["aggregate"]:
        raise P1RepositoryStateError(
            f"Unity task register aggregate disagrees with Issue #90: "
            f"register={task_register['aggregate']} ledger={ledger['aggregate']}"
        )
    if set(task_register["blockedTaskIds"]) != set(ledger_blockers):
        raise P1RepositoryStateError(
            "Unity task register blocker set disagrees with Issue #90: "
            f"register={task_register['blockedTaskIds']} ledger={list(ledger_blockers)}"
        )

    return {
        "schemaVersion": 1,
        "state": "P1_REPOSITORY_STATE_CONSISTENT",
        "ledger": {"issueNumber": 90, **ledger["aggregate"]},
        "projectStatus": {
            **project["aggregate"],
            "blockedTaskIds": list(project_blockers),
        },
        "taskRegister": {
            **task_register["aggregate"],
            "blockedTaskIds": task_register["blockedTaskIds"],
        },
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
    parser.add_argument("--project-status", required=True, help="Path to docs/PROJECT_STATUS.md")
    parser.add_argument("--task-register", required=True, help="Path to docs/tasks/06-UNITY-3D-MIGRATION.md")
    parser.add_argument("--output", help="Optional JSON output; existing files are never overwritten")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_consistency(
            _read_text(Path(args.ledger).expanduser().resolve(), "Issue #90 snapshot"),
            _read_text(Path(args.project_status).expanduser().resolve(), "PROJECT_STATUS.md"),
            _read_text(Path(args.task_register).expanduser().resolve(), "Unity task register"),
        )
        rendered = json.dumps(result, indent=2, sort_keys=True) + "\n"
        if args.output:
            output = Path(args.output).expanduser().resolve()
            if output.exists():
                raise P1RepositoryStateError(f"refusing to overwrite existing consistency report: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(rendered, encoding="utf-8")
        else:
            sys.stdout.write(rendered)

        print(
            "AFAREET_P1_REPOSITORY_STATE_OK "
            f"inReview={result['ledger']['inReview']} blocked={result['ledger']['blocked']} "
            f"total={result['ledger']['total']} taskStateMutationPerformed=false "
            "publicationPerformed=false verified=false",
            file=sys.stderr,
        )
        return 0
    except (OSError, P1RepositoryStateError) as exc:
        print(f"AFAREET_P1_REPOSITORY_STATE_ERROR: {exc}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main())
