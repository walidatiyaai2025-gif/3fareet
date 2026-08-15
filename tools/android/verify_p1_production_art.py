#!/usr/bin/env python3
"""Fail-closed P1 production-art acceptance verifier for 3Fareet.

This tool is deliberately separate from subjective visual review. It proves that
an exact candidate fingerprint is bound to an owner-accepted production-art
manifest and that none of the accepted visual tasks are claiming a procedural
or blockout fallback as the production path.

Every reviewed source, runtime asset and screenshot/video is SHA-256 pinned so
changing bytes after review invalidates the manifest even when the path stays
unchanged. It never marks an APK VERIFIED.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

SCRIPT_DIR = Path(__file__).resolve().parent
DEFAULT_SPEC = SCRIPT_DIR / "p1_production_art_spec.json"
SHA40_RE = re.compile(r"^[0-9a-f]{40}$", re.IGNORECASE)
SHA256_RE = re.compile(r"^[0-9a-f]{64}$", re.IGNORECASE)
PASS_VERDICT = "PRODUCTION_ART_GATE_PASSED"


class ProductionArtGateError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise ProductionArtGateError(message)


def read_json(path: Path) -> dict[str, Any]:
    with path.open("r", encoding="utf-8") as handle:
        payload = json.load(handle)
    _require(isinstance(payload, dict), f"JSON root must be an object: {path}")
    return payload


def _sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _require_sha256(value: Any, label: str) -> str:
    digest = str(value or "").strip().lower()
    _require(bool(SHA256_RE.fullmatch(digest)), f"{label} sha256 must be 64 hex characters")
    return digest


def _safe_repo_file(repo_root: Path, relative_path: str, label: str) -> Path:
    _require(bool(relative_path.strip()), f"{label} path is empty")
    candidate = (repo_root / relative_path).resolve()
    try:
        candidate.relative_to(repo_root)
    except ValueError as exc:
        raise ProductionArtGateError(f"{label} escapes repository root: {relative_path}") from exc
    _require(candidate.is_file(), f"{label} file is missing: {relative_path}")
    return candidate


def _safe_evidence_file(manifest_dir: Path, relative_path: str, label: str) -> Path:
    _require(bool(relative_path.strip()), f"{label} path is empty")
    candidate = (manifest_dir / relative_path).resolve()
    try:
        candidate.relative_to(manifest_dir)
    except ValueError as exc:
        raise ProductionArtGateError(f"{label} escapes manifest directory: {relative_path}") from exc
    _require(candidate.is_file(), f"{label} file is missing: {relative_path}")
    return candidate


def _verify_file_fingerprint(path: Path, expected_sha256: Any, label: str) -> str:
    expected = _require_sha256(expected_sha256, label)
    actual = _sha256_file(path)
    _require(actual == expected, f"{label} SHA-256 mismatch")
    return actual


def _require_hashed_file_record(item: Any, label: str) -> dict[str, Any]:
    _require(isinstance(item, dict), f"{label} must be an object with path + sha256")
    _require(bool(str(item.get("path") or "").strip()), f"{label} path is empty")
    _require_sha256(item.get("sha256"), label)
    return item


def _reject_forbidden_source_segments(path: Path, repo_root: Path, forbidden_segments: set[str], label: str) -> None:
    relative = path.relative_to(repo_root)
    parts = {part.lower() for part in relative.parts}
    blocked = sorted(parts.intersection(forbidden_segments))
    _require(not blocked, f"{label} uses forbidden generated/preview/blockout source segment: {blocked[0]}")


def verify_art_manifest(
    *,
    manifest_path: Path,
    repo_root: Path,
    spec_path: Path = DEFAULT_SPEC,
    expected_git_sha: str | None = None,
    expected_apk_sha: str | None = None,
) -> dict[str, Any]:
    manifest_path = manifest_path.expanduser().resolve()
    repo_root = repo_root.expanduser().resolve()
    spec_path = spec_path.expanduser().resolve()
    _require(repo_root.is_dir(), f"repository root does not exist: {repo_root}")

    manifest = read_json(manifest_path)
    spec = read_json(spec_path)

    _require(manifest.get("schemaVersion") == spec.get("schemaVersion") == 2, "production-art schemaVersion must be 2")
    _require(manifest.get("visualGate") == spec.get("visualGate") == "UPER-009", "production-art manifest must target UPER-009")
    _require(manifest.get("verified") is False, "production-art manifest must never self-assert VERIFIED")
    _require(manifest.get("ownerAccepted") is True, "owner production-art acceptance is missing")

    candidate = manifest.get("candidate")
    _require(isinstance(candidate, dict), "candidate fingerprint is missing")
    git_sha = str(candidate.get("gitSha") or "").lower()
    apk_sha = str(candidate.get("apkSha256") or "").lower()
    _require(bool(SHA40_RE.fullmatch(git_sha)), "candidate Git SHA must be a full 40-hex SHA")
    _require(bool(SHA256_RE.fullmatch(apk_sha)), "candidate APK SHA-256 must be 64 hex characters")
    if expected_git_sha is not None:
        _require(git_sha == expected_git_sha.strip().lower(), "production-art Git SHA does not match expected candidate")
    if expected_apk_sha is not None:
        _require(apk_sha == expected_apk_sha.strip().lower(), "production-art APK SHA does not match expected candidate")

    fallback = manifest.get("fallbackState")
    _require(isinstance(fallback, dict), "fallbackState is missing")
    for key in spec.get("forbiddenActiveFallbacks", []):
        _require(fallback.get(key) is False, f"production-art gate blocked: {key} is active")

    assets = manifest.get("assets")
    _require(isinstance(assets, dict), "assets map is missing")
    required_tasks = spec.get("requiredTasks")
    _require(isinstance(required_tasks, list) and required_tasks, "production-art spec has no requiredTasks")

    accepted: list[str] = []
    evidence_count = 0
    fingerprint_count = 0
    manifest_dir = manifest_path.parent
    allowed_visual_evidence = set(spec.get("allowedVisualEvidenceKinds", ["screenshot", "video"]))
    allowed_source_suffixes = {str(x).lower() for x in spec.get("allowedAuthored3DSuffixes", [])}
    forbidden_source_segments = {str(x).strip().lower() for x in spec.get("forbiddenAuthoredSourcePathSegments", []) if str(x).strip()}
    seen_evidence_paths: set[Path] = set()

    for task_id in required_tasks:
        task = assets.get(task_id)
        _require(isinstance(task, dict), f"required production-art task is missing: {task_id}")
        _require(task.get("reviewState") == "ACCEPTED", f"{task_id} reviewState must be ACCEPTED")
        _require(task.get("quality") == "production", f"{task_id} is not production quality")
        _require(task.get("authored3D") is True, f"{task_id} is not marked as authored 3D")
        _require(task.get("runtimeActive") is True, f"{task_id} production asset is not active at runtime")
        _require(task.get("proceduralFallbackActive") is False, f"{task_id} is still using procedural/blockout fallback")
        _require(task.get("ownerAccepted") is True, f"{task_id} owner acceptance is missing")

        source_files = task.get("sourceFiles")
        runtime_assets = task.get("runtimeAssets")
        evidence = task.get("evidence")
        _require(isinstance(source_files, list) and source_files, f"{task_id} sourceFiles must be non-empty")
        _require(isinstance(runtime_assets, list) and runtime_assets, f"{task_id} runtimeAssets must be non-empty")
        _require(isinstance(evidence, list) and evidence, f"{task_id} visual evidence must be non-empty")

        authored_source_seen = False
        for index, raw in enumerate(source_files):
            label = f"{task_id} sourceFiles[{index}]"
            item = _require_hashed_file_record(raw, label)
            path = _safe_repo_file(repo_root, str(item["path"]), label)
            _verify_file_fingerprint(path, item.get("sha256"), label)
            _reject_forbidden_source_segments(path, repo_root, forbidden_source_segments, label)
            fingerprint_count += 1
            if path.suffix.lower() in allowed_source_suffixes:
                authored_source_seen = True
        _require(authored_source_seen, f"{task_id} has no authored 3D source file with an allowed suffix")

        for index, raw in enumerate(runtime_assets):
            label = f"{task_id} runtimeAssets[{index}]"
            item = _require_hashed_file_record(raw, label)
            path = _safe_repo_file(repo_root, str(item["path"]), label)
            _verify_file_fingerprint(path, item.get("sha256"), label)
            fingerprint_count += 1

        visual_evidence_seen = False
        for index, raw in enumerate(evidence):
            label = f"{task_id} evidence[{index}]"
            item = _require_hashed_file_record(raw, label)
            kind = str(item.get("kind") or "").lower()
            _require(kind in allowed_visual_evidence, f"{label} has unsupported kind: {kind}")
            path = _safe_evidence_file(manifest_dir, str(item["path"]), label)
            _require(path not in seen_evidence_paths, f"visual evidence file is reused across production-art tasks: {item['path']}")
            seen_evidence_paths.add(path)
            _verify_file_fingerprint(path, item.get("sha256"), label)
            visual_evidence_seen = True
            evidence_count += 1
            fingerprint_count += 1
        _require(visual_evidence_seen, f"{task_id} has no screenshot/video evidence")
        accepted.append(task_id)

    _require(set(accepted) == set(required_tasks), "not all required production-art tasks were accepted")

    return {
        "schemaVersion": 2,
        "verdict": PASS_VERDICT,
        "verified": False,
        "visualGate": "UPER-009",
        "candidate": {"gitSha": git_sha, "apkSha256": apk_sha},
        "acceptedTasks": accepted,
        "evidenceCount": evidence_count,
        "artifactFingerprintCount": fingerprint_count,
        "artifactFingerprintsVerified": True,
        "proceduralFallbackAccepted": False,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Verify exact-candidate P1 production-art acceptance without self-asserting VERIFIED state.")
    parser.add_argument("--manifest", required=True, help="Production-art acceptance manifest produced for one exact candidate.")
    parser.add_argument("--repo-root", default=".", help="Repository root used to resolve tracked source/runtime asset paths.")
    parser.add_argument("--spec", default=str(DEFAULT_SPEC), help="Production-art gate specification JSON.")
    parser.add_argument("--expected-git-sha", help="Optional exact 40-hex candidate Git SHA binding.")
    parser.add_argument("--expected-apk-sha", help="Optional exact 64-hex APK SHA-256 binding.")
    parser.add_argument("--output", help="Optional JSON result path; existing files are never overwritten.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        result = verify_art_manifest(
            manifest_path=Path(args.manifest),
            repo_root=Path(args.repo_root),
            spec_path=Path(args.spec),
            expected_git_sha=args.expected_git_sha,
            expected_apk_sha=args.expected_apk_sha,
        )
        output = None
        if args.output:
            output = Path(args.output).expanduser().resolve()
            output.parent.mkdir(parents=True, exist_ok=True)
            if output.exists():
                raise ProductionArtGateError(f"refusing to overwrite existing production-art result: {output}")
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_PRODUCTION_ART_GATE_OK "
            f"gitSha={result['candidate']['gitSha']} apkSha256={result['candidate']['apkSha256']} "
            f"tasks={len(result['acceptedTasks'])} evidence={result['evidenceCount']} "
            f"fingerprints={result['artifactFingerprintCount']} verdict={result['verdict']} verified=false"
            + (f" output={output}" if output else "")
        )
        return 0
    except (ProductionArtGateError, OSError, ValueError, json.JSONDecodeError) as exc:
        print(f"AFAREET_PRODUCTION_ART_GATE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
