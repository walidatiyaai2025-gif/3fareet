#!/usr/bin/env python3
"""Build a read-only operator packet for the P1 licensed Unity handoff.

The packet composes the existing six-task visual-source audit, licensed-staging
readiness audit, and authoritative operator-chain identity. It is designed to tell
an operator exactly what is ready, what is blocked, and which Windows commands/artifacts
come next. It never runs Unity, builds an APK, publishes, or marks anything verified.
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

import p1_licensed_staging_readiness
import p1_visual_source_readiness

SCHEMA_VERSION = 1
CHAIN_FILE = SCRIPT_DIR / "p1_operator_release_chain.json"
EXPECTED_TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]
EXPECTED_UNITY_VERSION = "6000.5.8f1"


class P1LicensedHandoffPacketError(RuntimeError):
    pass


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _read_json(path: Path) -> dict[str, Any]:
    try:
        payload = json.loads(path.read_text(encoding="utf-8-sig"))
    except (OSError, json.JSONDecodeError) as exc:
        raise P1LicensedHandoffPacketError(f"invalid JSON: {path}: {exc}") from exc
    if not isinstance(payload, dict):
        raise P1LicensedHandoffPacketError(f"JSON root must be an object: {path}")
    return payload


def _normalize_repo_hero(value: Optional[str]) -> Optional[str]:
    text = (value or "").strip().replace("\\", "/")
    while text.startswith("./"):
        text = text[2:]
    if text.startswith("Assets/"):
        text = "unity_game/" + text
    return text or None


def _to_unity_asset_path(repo_relative: Optional[str]) -> Optional[str]:
    if not repo_relative:
        return None
    prefix = "unity_game/"
    if not repo_relative.startswith(prefix):
        return None
    value = repo_relative[len(prefix):]
    return value if value.startswith("Assets/") else None


def _load_operator_chain(chain_path: Path) -> tuple[dict[str, Any], str]:
    chain = _read_json(chain_path)
    if chain.get("schemaVersion") != 1 or chain.get("state") != "P1_AUTHORITATIVE_OPERATOR_CHAIN":
        raise P1LicensedHandoffPacketError("unsupported authoritative P1 operator-chain schema/state")
    if chain.get("authoritativeForP1") is not True:
        raise P1LicensedHandoffPacketError("operator chain must declare authoritativeForP1=true")
    if chain.get("genericPublicationVerifierSufficientForP1") is not False:
        raise P1LicensedHandoffPacketError("operator chain must keep generic publication verifier insufficient for P1")
    stages = chain.get("orderedStages")
    if not isinstance(stages, list) or len(stages) != 13:
        raise P1LicensedHandoffPacketError("operator chain must contain exactly 13 ordered P1 stages")
    return chain, sha256_file(chain_path)


def _command_packet(hero_repo_relative: Optional[str]) -> dict[str, Any]:
    unity_source = _to_unity_asset_path(hero_repo_relative)
    display_source = unity_source or "Assets/Afareet/ArtSource/Vehicles/HeroCar/<REAL_HERO_SOURCE.fbx>"
    return {
        "heroSourcePlaceholder": unity_source is None,
        "heroSourceUnityPath": unity_source,
        "portableAudit": (
            "python3 tools/android/p1_licensed_handoff_packet.py "
            f"--hero-source \"{display_source}\" --output artifacts/production-staging/p1-licensed-handoff-packet.json"
        ),
        "nativeHeroIntake": (
            "pwsh -File tools/android/validate_hero_asset_intake_windows.ps1 "
            f"-Source \"{display_source}\" -Output artifacts/production-staging/uart003-native-intake.json"
        ),
        "licensedUnityStaging": (
            "pwsh -File tools/android/stage_production_candidate_windows.ps1 "
            f"-HeroSource \"{display_source}\""
        ),
        "postStagingRule": (
            "Review the exact unity_game/Assets staging delta and commit only the approved staging output; "
            "then run tools/android/run_p1_staged_candidate_windows.ps1 from the new clean direct-child SHA."
        ),
    }


def build_packet(
    repo_root: Path,
    *,
    hero_source: Optional[str] = None,
    chain_path: Path = CHAIN_FILE,
) -> dict[str, Any]:
    repo_root = repo_root.expanduser().resolve()
    chain_path = chain_path.expanduser().resolve()
    chain, chain_sha = _load_operator_chain(chain_path)

    normalized_hero = _normalize_repo_hero(hero_source)
    visual = p1_visual_source_readiness.audit_visual_sources(repo_root, hero_source=normalized_hero)
    staging = p1_licensed_staging_readiness.audit(
        repo_root,
        hero_source=normalized_hero,
        require_clean=True,
    )

    tasks = []
    for item in visual.get("tasks", []):
        task_id = str(item.get("taskId") or "")
        if task_id not in EXPECTED_TASKS:
            raise P1LicensedHandoffPacketError(f"unexpected visual task in source audit: {task_id!r}")
        tasks.append(
            {
                "taskId": task_id,
                "sourceState": item.get("state"),
                "sourceReady": item.get("sourceReady") is True,
                "blockedCheckIds": list(item.get("blockedCheckIds") or []),
                "verified": False,
                "runtimeVerified": False,
                "ownerAccepted": False,
            }
        )
    if [item["taskId"] for item in tasks] != EXPECTED_TASKS:
        raise P1LicensedHandoffPacketError("visual source audit did not return the exact ordered six-task P1 scope")

    hero_task = tasks[0]
    if normalized_hero is None and hero_task["sourceReady"] is False:
        state = "BLOCKED_EXTERNAL_HERO_SOURCE"
    elif staging.get("readyForLicensedStaging") is True:
        state = "READY_FOR_LICENSED_OPERATOR_HANDOFF"
    else:
        state = "BLOCKED_PRELICENSED_HANDOFF"

    stages = chain["orderedStages"]
    selected_stage_ids = [
        "UART003_NATIVE_WINDOWS_INTAKE",
        "P1_LICENSED_UNITY_STAGING",
        "P1_REVIEW_AND_COMMIT_STAGING_DELTA",
        "P1_STAGED_CANDIDATE",
    ]
    stage_lookup = {str(stage.get("id")): stage for stage in stages if isinstance(stage, dict)}
    if any(stage_id not in stage_lookup for stage_id in selected_stage_ids):
        raise P1LicensedHandoffPacketError("operator chain is missing one or more licensed-handoff stages")

    return {
        "schemaVersion": SCHEMA_VERSION,
        "state": state,
        "gitSha": staging.get("gitSha"),
        "expectedUnityVersion": EXPECTED_UNITY_VERSION,
        "heroSource": normalized_hero,
        "fixedRegisterSize": chain.get("fixedRegisterSize"),
        "operatorChain": {
            "file": str(chain_path.relative_to(repo_root)).replace("\\", "/")
            if chain_path.is_relative_to(repo_root)
            else chain_path.name,
            "sha256": chain_sha,
            "stageCount": len(stages),
            "authoritativeForP1": True,
        },
        "visualSourceSummary": {
            "state": visual.get("state"),
            "sourceReadyCount": visual.get("sourceReadyCount"),
            "blockedCount": visual.get("blockedCount"),
            "blockedTaskIds": list(visual.get("blockedTaskIds") or []),
            "tasks": tasks,
        },
        "licensedStagingSummary": {
            "state": staging.get("state"),
            "readyForLicensedStaging": staging.get("readyForLicensedStaging") is True,
            "blockedCheckIds": list(staging.get("blockedCheckIds") or []),
        },
        "nextLicensedStages": [
            {
                "order": stage_lookup[stage_id].get("order"),
                "id": stage_id,
                "tool": stage_lookup[stage_id].get("tool"),
                "humanBoundary": stage_lookup[stage_id].get("humanBoundary") is True,
            }
            for stage_id in selected_stage_ids
        ],
        "commands": _command_packet(normalized_hero),
        "expectedArtifacts": [
            "artifacts/production-staging/uart003-native-intake.json",
            "artifacts/production-staging/p1-staging-handoff.json",
            "artifacts/production-staging/p1-staging-handoff.git-status.txt",
            "artifacts/production-staging/p1-staging-lineage.json",
            "artifacts/p1-staged-candidate-manifest.json",
        ],
        "licensedUnityExecuted": False,
        "candidateBuildStarted": False,
        "physicalDeviceEvidenceCaptured": False,
        "humanApprovalRecorded": False,
        "publicationEligible": False,
        "publicationPerformed": False,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "nextAction": (
            "Commit a real externally-authored Afareet King production source under the canonical HeroCar source root, then rerun this packet with --hero-source."
            if state == "BLOCKED_EXTERNAL_HERO_SOURCE"
            else staging.get("nextAction")
        ),
    }


def write_packet(repo_root: Path, output: Path, packet: dict[str, Any]) -> None:
    repo_root = repo_root.resolve()
    output = output.expanduser().resolve()
    artifact_root = (repo_root / "artifacts").resolve()
    try:
        output.relative_to(artifact_root)
    except ValueError as exc:
        raise P1LicensedHandoffPacketError("--output must stay under <repo>/artifacts/") from exc
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        raise P1LicensedHandoffPacketError(f"refusing to overwrite existing handoff packet: {output}")
    output.write_text(json.dumps(packet, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--hero-source", default=None, help="Assets/... or unity_game/Assets/... real production Hero source")
    parser.add_argument("--chain", default=str(CHAIN_FILE), help="Authoritative P1 operator-chain JSON")
    parser.add_argument("--output", default=None, help="Optional packet JSON under <repo>/artifacts/")
    parser.add_argument("--allow-blocked", action="store_true", help="Return 0 when producing an informational blocked packet")
    return parser


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = build_parser().parse_args(argv)
    repo_root = Path(args.repo_root).resolve()
    try:
        packet = build_packet(
            repo_root,
            hero_source=args.hero_source,
            chain_path=Path(args.chain),
        )
        if args.output:
            write_packet(repo_root, Path(args.output), packet)
        print(json.dumps(packet, indent=2, sort_keys=True))
    except (P1LicensedHandoffPacketError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"AFAREET_P1_LICENSED_HANDOFF_PACKET_ERROR: {exc}", file=sys.stderr)
        return 2

    if packet["state"].startswith("BLOCKED") and not args.allow_blocked:
        return 3
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
