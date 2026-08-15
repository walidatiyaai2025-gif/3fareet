#!/usr/bin/env python3
"""Populate schema-v2 P1 production-art artifact SHA-256 fingerprints.

The input manifest remains untouched. The output must live beside the input so
relative screenshot/video paths keep the same evidence root. Repository source
and runtime artifacts must already be tracked by, and byte-identical to, the
manifest's exact candidate Git commit. This helper does not grant acceptance,
publish, or mark any candidate VERIFIED.
"""

from __future__ import annotations

import argparse
import copy
import json
import sys
from pathlib import Path
from typing import Any

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import verify_p1_production_art as gate


class FingerprintManifestError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise FingerprintManifestError(message)


def _path_record(item: Any, label: str) -> dict[str, Any]:
    _require(isinstance(item, dict), f"{label} must be an object with a path")
    path = str(item.get("path") or "").strip()
    _require(bool(path), f"{label} path is empty")
    return item


def fingerprint_manifest(
    *,
    manifest_path: Path,
    repo_root: Path,
    spec_path: Path = gate.DEFAULT_SPEC,
) -> tuple[dict[str, Any], int]:
    manifest_path = manifest_path.expanduser().resolve()
    repo_root = repo_root.expanduser().resolve()
    spec_path = spec_path.expanduser().resolve()

    _require(manifest_path.is_file(), f"input manifest is missing: {manifest_path}")
    _require(repo_root.is_dir(), f"repository root does not exist: {repo_root}")

    manifest = gate.read_json(manifest_path)
    spec = gate.read_json(spec_path)
    _require(manifest.get("schemaVersion") == spec.get("schemaVersion") == 2, "production-art schemaVersion must be 2")
    _require(manifest.get("verified") is False, "fingerprinter refuses manifests that self-assert VERIFIED")

    candidate = manifest.get("candidate")
    _require(isinstance(candidate, dict), "candidate fingerprint is missing")
    git_sha = str(candidate.get("gitSha") or "").strip().lower()
    _require(bool(gate.SHA40_RE.fullmatch(git_sha)), "candidate Git SHA must be a full 40-hex SHA")

    required_tasks = spec.get("requiredTasks")
    _require(isinstance(required_tasks, list) and required_tasks, "production-art spec has no requiredTasks")
    assets = manifest.get("assets")
    _require(isinstance(assets, dict), "assets map is missing")

    result = copy.deepcopy(manifest)
    result_assets = result["assets"]
    fingerprint_count = 0
    manifest_dir = manifest_path.parent
    repo_artifacts: list[tuple[Path, str, str]] = []

    for task_id in required_tasks:
        task = result_assets.get(task_id)
        _require(isinstance(task, dict), f"required production-art task is missing: {task_id}")

        source_files = task.get("sourceFiles")
        runtime_assets = task.get("runtimeAssets")
        evidence = task.get("evidence")
        _require(isinstance(source_files, list) and source_files, f"{task_id} sourceFiles must be non-empty")
        _require(isinstance(runtime_assets, list) and runtime_assets, f"{task_id} runtimeAssets must be non-empty")
        _require(isinstance(evidence, list) and evidence, f"{task_id} visual evidence must be non-empty")

        for index, raw in enumerate(source_files):
            label = f"{task_id} sourceFiles[{index}]"
            item = _path_record(raw, label)
            path = gate._safe_repo_file(repo_root, str(item["path"]), label)
            digest = gate._sha256_file(path)
            item["sha256"] = digest
            repo_artifacts.append((path, digest, label))
            fingerprint_count += 1

        for index, raw in enumerate(runtime_assets):
            label = f"{task_id} runtimeAssets[{index}]"
            item = _path_record(raw, label)
            path = gate._safe_repo_file(repo_root, str(item["path"]), label)
            digest = gate._sha256_file(path)
            item["sha256"] = digest
            repo_artifacts.append((path, digest, label))
            fingerprint_count += 1

        for index, raw in enumerate(evidence):
            label = f"{task_id} evidence[{index}]"
            item = _path_record(raw, label)
            path = gate._safe_evidence_file(manifest_dir, str(item["path"]), label)
            item["sha256"] = gate._sha256_file(path)
            fingerprint_count += 1

    gate._verify_git_candidate_binding(repo_root, git_sha, repo_artifacts)
    return result, fingerprint_count


def write_fingerprinted_manifest(*, input_path: Path, output_path: Path, payload: dict[str, Any]) -> Path:
    input_path = input_path.expanduser().resolve()
    output_path = output_path.expanduser().resolve()
    _require(output_path != input_path, "fingerprinter never overwrites the input manifest")
    _require(output_path.parent == input_path.parent, "fingerprinted manifest must stay beside the input so evidence-relative paths remain stable")
    _require(not output_path.exists(), f"refusing to overwrite existing fingerprinted manifest: {output_path}")
    output_path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")
    return output_path


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Populate exact SHA-256 fingerprints for schema-v2 P1 production-art paths from the exact candidate Git worktree."
    )
    parser.add_argument("--manifest", required=True, help="Unfingerprinted schema-v2 production-art manifest template.")
    parser.add_argument("--repo-root", default=".", help="Exact candidate Git worktree root used to resolve source/runtime asset paths.")
    parser.add_argument("--spec", default=str(gate.DEFAULT_SPEC), help="Production-art gate specification JSON.")
    parser.add_argument("--output", required=True, help="New fingerprinted manifest path beside the input manifest.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    input_path = Path(args.manifest)
    try:
        payload, count = fingerprint_manifest(
            manifest_path=input_path,
            repo_root=Path(args.repo_root),
            spec_path=Path(args.spec),
        )
        output = write_fingerprinted_manifest(
            input_path=input_path,
            output_path=Path(args.output),
            payload=payload,
        )
        print(
            "AFAREET_PRODUCTION_ART_FINGERPRINT_OK "
            f"artifacts={count} gitCandidateBound=true schemaVersion=2 verified=false output={output}"
        )
        return 0
    except (
        FingerprintManifestError,
        gate.ProductionArtGateError,
        OSError,
        ValueError,
        json.JSONDecodeError,
    ) as exc:
        print(f"AFAREET_PRODUCTION_ART_FINGERPRINT_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
