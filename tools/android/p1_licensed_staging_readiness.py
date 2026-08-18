#!/usr/bin/env python3
"""Fail-closed readiness audit for the P1 licensed production-art staging handoff.

This command is read-only with respect to tracked source. It proves source/handoff readiness
only; it never runs Unity, stages assets, builds an APK, or promotes verification state.
"""

from __future__ import annotations

import argparse
import importlib.util
import json
import subprocess
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence, Tuple

SUPPORTED_HERO_EXTENSIONS = {".fbx", ".obj", ".blend", ".glb", ".gltf"}
FORBIDDEN_HERO_TOKENS = (
    "generated", "placeholder", "legacyprocedural", "preview", "refinement",
    "refinementcandidates", "blockout", "review", "reviewpackaging",
)

HERO_HANDOFF_VALIDATOR_FILE = "tools/android/validate_uart003_hero_production_handoff.py"
HERO_NATIVE_PREFLIGHT_FILE = "tools/android/hero_production_handoff_preflight_windows.ps1"
RIVAL_PRODUCTION_ROOT = "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production"
RIVAL_OBJ_FILES = (
    f"{RIVAL_PRODUCTION_ROOT}/Rival_01_WedgeCoupe_Production.obj",
    f"{RIVAL_PRODUCTION_ROOT}/Rival_02_FastbackMuscle_Production.obj",
    f"{RIVAL_PRODUCTION_ROOT}/Rival_03_CompactPrototype_Production.obj",
)
RIVAL_REQUIRED_FILES = tuple(path for obj in RIVAL_OBJ_FILES for path in (obj, obj + ".meta"))
RIVAL_POLICY_FILE = "unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs"
RIVAL_HANDOFF_VALIDATOR_FILE = "tools/android/validate_uart004_rival_production_handoff.py"
RIVAL_NATIVE_PREFLIGHT_FILE = "tools/android/rival_production_handoff_preflight_windows.ps1"

HANDOFF_REQUIRED_FILES = (
    "unity_game/Assets/Afareet/Editor/P1ProductionCandidateStagingHandoff.cs",
    "unity_game/Assets/Afareet/Editor/HeroCarProductionPrefabStager.cs",
    "unity_game/Assets/Afareet/Editor/RivalProductionPrefabStager.cs",
    "unity_game/Assets/Afareet/Editor/P1ProductionWorldAssetStager.cs",
    "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkAssetStager.cs",
    "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingAssetStager.cs",
    HERO_HANDOFF_VALIDATOR_FILE,
    HERO_NATIVE_PREFLIGHT_FILE,
    RIVAL_POLICY_FILE,
    RIVAL_HANDOFF_VALIDATOR_FILE,
    RIVAL_NATIVE_PREFLIGHT_FILE,
    "tools/android/stage_production_candidate_windows.ps1",
    "tools/android/run_local_candidate_windows.ps1",
    ".github/workflows/unity-licensed-windows-candidate.yml",
)

WORLD_REQUIRED_FILES = (
    "docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json",
    "docs/assets/03_props_architecture/cairo_landmarks/ASSET_MANIFEST.json",
    "docs/assets/02_tracks_environments/cairo_track_dressing/ASSET_MANIFEST.json",
)


def _run_git(repo_root: Path, args: Sequence[str]) -> Tuple[int, str, str]:
    process = subprocess.run(
        ["git", "-C", str(repo_root), *args], stdout=subprocess.PIPE, stderr=subprocess.PIPE,
        text=True, encoding="utf-8", errors="replace", check=False,
    )
    return process.returncode, process.stdout.strip(), process.stderr.strip()


def _check(checks: List[Dict[str, Any]], check_id: str, ok: bool, detail: str) -> bool:
    checks.append({"id": check_id, "status": "PASS" if ok else "BLOCKED", "detail": detail})
    return ok


def _tracked(repo_root: Path, relative_path: str) -> bool:
    code, _, _ = _run_git(repo_root, ["ls-files", "--error-unmatch", "--", relative_path])
    return code == 0


def _nonempty_file(repo_root: Path, relative_path: str) -> bool:
    path = repo_root / relative_path
    return path.is_file() and path.stat().st_size > 0


def _tracked_nonempty(repo_root: Path, relative_path: str) -> bool:
    return _nonempty_file(repo_root, relative_path) and _tracked(repo_root, relative_path)


def _normalize_hero_path(hero_source: str) -> str:
    value = (hero_source or "").strip().replace("\\", "/")
    if value.startswith("Assets/"):
        value = "unity_game/" + value
    while value.startswith("./"):
        value = value[2:]
    return value


def _load_sibling(module_name: str, file_name: str):
    module_path = Path(__file__).resolve().with_name(file_name)
    spec = importlib.util.spec_from_file_location(module_name, module_path)
    if spec is None or spec.loader is None:
        raise ImportError(f"cannot load preflight module: {module_path}")
    module = importlib.util.module_from_spec(spec)
    previous_module = sys.modules.get(module_name)
    sys.modules[module_name] = module
    try:
        spec.loader.exec_module(module)
    except BaseException:
        if previous_module is None:
            sys.modules.pop(module_name, None)
        else:
            sys.modules[module_name] = previous_module
        raise
    return module


def _load_hero_handoff_validator():
    return _load_sibling("afareet_uart003_hero_handoff_validator", "validate_uart003_hero_production_handoff.py")


def _load_rival_handoff_validator():
    return _load_sibling("afareet_uart004_rival_handoff_validator", "validate_uart004_rival_production_handoff.py")


def _repo_relative(repo_root: Path, path: Path) -> str:
    try:
        return path.resolve().relative_to(repo_root.resolve()).as_posix()
    except ValueError as exc:
        raise ValueError(f"dependency resolved outside the exact Git worktree: {path}") from exc


def _audit_rival_handoff(repo_root: Path, checks: List[Dict[str, Any]]) -> None:
    base_ready = all(_tracked_nonempty(repo_root, path) for path in RIVAL_REQUIRED_FILES)
    policy_ready = _tracked_nonempty(repo_root, RIVAL_POLICY_FILE)
    validator_ready = _tracked_nonempty(repo_root, RIVAL_HANDOFF_VALIDATOR_FILE)
    if not (base_ready and policy_ready and validator_ready):
        _check(checks, "UART-004_RIVAL_HANDOFF_STRUCTURE", False,
               "exact three tracked Rival OBJ/.meta files plus tracked policy and technical validator are required")
        return
    validator = _load_rival_handoff_validator()
    try:
        report = validator.validate_handoff([repo_root / path for path in RIVAL_OBJ_FILES], repo_root / RIVAL_POLICY_FILE)
    except (validator.HandoffError, OSError, UnicodeError, ValueError) as exc:
        _check(checks, "UART-004_RIVAL_HANDOFF_STRUCTURE", False, str(exc))
        return
    _check(checks, "UART-004_RIVAL_HANDOFF_STRUCTURE",
           bool(report.get("technicalPreflightPassed")) and report.get("distinctSourceHashes") == 3,
           "three distinct policy-compliant OBJ/MTL/texture handoffs")

    dependency_files: set[str] = set()
    package_root = repo_root / RIVAL_PRODUCTION_ROOT
    for variant in report.get("variants", []):
        for library in variant.get("materialLibraries", []):
            name = str(library.get("fileName") or "").replace("\\", "/")
            if not name:
                continue
            mtl_path = (package_root / name).resolve()
            mtl_relative = _repo_relative(repo_root, mtl_path)
            dependency_files.update((mtl_relative, mtl_relative + ".meta"))
            for texture_ref in library.get("textures", []):
                texture_path = (mtl_path.parent / str(texture_ref).replace("\\", "/")).resolve()
                texture_relative = _repo_relative(repo_root, texture_path)
                dependency_files.update((texture_relative, texture_relative + ".meta"))
    if not dependency_files:
        _check(checks, "UART-004_RIVAL_DEPENDENCY_SET", False, "technical handoff exposed no MTL/texture dependencies")
        return
    _check(checks, "UART-004_RIVAL_DEPENDENCY_SET", True, f"dependencyFiles={len(dependency_files)} packageLocal=true")
    for path in sorted(dependency_files):
        ok = _tracked_nonempty(repo_root, path)
        _check(checks, f"RIVAL_DEP:{path}", ok,
               "tracked-nonempty" if ok else "missing, empty or untracked Rival MTL/texture dependency or Unity metadata")


def _audit_hero_handoff(repo_root: Path, hero_source: str, checks: List[Dict[str, Any]]) -> None:
    if not _tracked_nonempty(repo_root, HERO_HANDOFF_VALIDATOR_FILE):
        _check(checks, "UART-003_HERO_HANDOFF_PREFLIGHT", False, "Hero technical handoff validator is missing, empty or untracked")
        return
    validator = _load_hero_handoff_validator()
    try:
        report = validator.validate_intake(repo_root, hero_source)
    except (validator.HeroHandoffError, OSError, UnicodeError, ValueError, subprocess.SubprocessError) as exc:
        _check(checks, "UART-003_HERO_HANDOFF_PREFLIGHT", False, str(exc))
        return
    eligible = bool(report.get("preUnitySourceEligible")) and report.get("verdict") in {
        "READY_FOR_LICENSED_UNITY_IMPORT", "UNITY_INSPECTION_REQUIRED"
    }
    detail = (
        f"verdict={report.get('verdict')} sourceInspection={report.get('sourceInspection')} "
        f"unityInspectionRequired={str(bool(report.get('unityInspectionRequired'))).lower()} verified=false"
    )
    _check(checks, "UART-003_HERO_HANDOFF_PREFLIGHT", eligible, detail)


def audit(repo_root: Path, hero_source: Optional[str] = None, require_clean: bool = True) -> Dict[str, Any]:
    repo_root = repo_root.resolve()
    checks: List[Dict[str, Any]] = []

    code, top_level, git_error = _run_git(repo_root, ["rev-parse", "--show-toplevel"])
    is_git_root = code == 0 and Path(top_level).resolve() == repo_root
    _check(checks, "GIT_ROOT", is_git_root,
           "repo-root-is-exact-git-toplevel" if is_git_root else f"expected exact Git root; git={git_error or top_level or '<none>'}")
    code, git_sha, _ = _run_git(repo_root, ["rev-parse", "HEAD"])
    valid_sha = code == 0 and len(git_sha) == 40 and all(c in "0123456789abcdefABCDEF" for c in git_sha)
    _check(checks, "GIT_SHA", valid_sha, git_sha if valid_sha else "unable to resolve full 40-character HEAD")
    code, status, status_error = _run_git(repo_root, ["status", "--porcelain"])
    clean = code == 0 and status == ""
    if require_clean:
        _check(checks, "CLEAN_TREE", clean,
               "clean" if clean else f"working tree is dirty or unreadable: {status or status_error or '<unknown>'}")
    else:
        _check(checks, "CLEAN_TREE", code == 0, f"clean={clean}; debug override does not make staging release-eligible")

    for path in HANDOFF_REQUIRED_FILES:
        ok = _tracked_nonempty(repo_root, path)
        _check(checks, f"HANDOFF:{path}", ok,
               "tracked-nonempty" if ok else "missing, empty or untracked staging/candidate handoff dependency")
    for path in WORLD_REQUIRED_FILES:
        ok = _tracked_nonempty(repo_root, path)
        _check(checks, f"WORLD:{path}", ok,
               "tracked-nonempty" if ok else "missing, empty or untracked production-world manifest")
    for path in RIVAL_REQUIRED_FILES:
        ok = _tracked_nonempty(repo_root, path)
        _check(checks, f"RIVAL:{path}", ok,
               "tracked-nonempty" if ok else "missing, empty or untracked UART-004 isolated production source/Unity metadata")
    _audit_rival_handoff(repo_root, checks)

    normalized_hero = _normalize_hero_path(hero_source or "")
    hero_supplied = bool(normalized_hero)
    _check(checks, "UART-003_HERO_SOURCE_SUPPLIED", hero_supplied,
           normalized_hero if hero_supplied else "real externally-authored Hero production source not supplied")

    hero_basic_ready = False
    if hero_supplied:
        lower = normalized_hero.lower()
        under_assets = normalized_hero.startswith("unity_game/Assets/")
        no_traversal = "../" not in normalized_hero
        vehicle_role = "/vehicles/" in lower
        extension_ok = Path(normalized_hero).suffix.lower() in SUPPORTED_HERO_EXTENSIONS
        forbidden = [token for token in FORBIDDEN_HERO_TOKENS if f"/{token}/" in f"/{lower.strip('/')}/"]
        exists = _nonempty_file(repo_root, normalized_hero)
        tracked = exists and _tracked(repo_root, normalized_hero)
        hero_meta = normalized_hero + ".meta"
        hero_meta_ok = _tracked_nonempty(repo_root, hero_meta)
        not_rival = "/rivals/" not in lower
        _check(checks, "UART-003_HERO_UNITY_ASSET_PATH", under_assets,
               normalized_hero if under_assets else "Hero must resolve under unity_game/Assets/")
        _check(checks, "UART-003_HERO_NO_TRAVERSAL", no_traversal,
               "no traversal" if no_traversal else "Hero source path cannot contain ../ traversal")
        _check(checks, "UART-003_HERO_VEHICLE_ROLE", vehicle_role,
               "vehicle role path" if vehicle_role else "Hero production source must resolve under a /Vehicles/ role path")
        _check(checks, "UART-003_HERO_SUPPORTED_FORMAT", extension_ok, Path(normalized_hero).suffix.lower() or "missing extension")
        _check(checks, "UART-003_HERO_NOT_NONPRODUCTION_PATH", not forbidden,
               "authored-production-source-path" if not forbidden else "forbidden source token(s): " + ",".join(forbidden))
        _check(checks, "UART-003_HERO_EXISTS", exists, normalized_hero if exists else "Hero file is missing or empty")
        _check(checks, "UART-003_HERO_TRACKED_BY_HEAD", tracked,
               "tracked by Git" if tracked else "Hero must be non-empty and committed before licensed staging")
        _check(checks, "UART-003_HERO_META_TRACKED_BY_HEAD", hero_meta_ok,
               "tracked non-empty Unity metadata" if hero_meta_ok else "Hero .meta must be non-empty and committed before licensed staging")
        _check(checks, "UART-003_HERO_NOT_RIVAL_SOURCE", not_rival,
               "separate Hero source" if not_rival else "Rival source cannot be reused as the Hero production source")
        hero_basic_ready = all((under_assets, no_traversal, vehicle_role, extension_ok, not forbidden, exists, tracked, hero_meta_ok, not_rival))
        if hero_basic_ready:
            _audit_hero_handoff(repo_root, normalized_hero, checks)
        else:
            _check(checks, "UART-003_HERO_HANDOFF_PREFLIGHT", False,
                   "basic Hero source identity/tracking checks must pass before technical handoff validation")

    blocked = [item for item in checks if item["status"] != "PASS"]
    state = "READY_FOR_LICENSED_STAGING" if not blocked else "BLOCKED"
    return {
        "schemaVersion": 2,
        "state": state,
        "gitSha": git_sha.lower() if valid_sha else None,
        "heroSource": normalized_hero or None,
        "readyForLicensedStaging": state == "READY_FOR_LICENSED_STAGING",
        "candidateBuildStarted": False,
        "publicationEligible": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "verified": False,
        "blockedCheckIds": [item["id"] for item in blocked],
        "checks": checks,
        "nextAction": (
            "Run tools/android/stage_production_candidate_windows.ps1 with the preflighted tracked Hero source plus three isolated Rival production sources and tracked package-local dependencies on licensed Unity 6000.5.8f1; opaque Hero formats still require Unity importer inspection; review and commit staging output before candidate tests/build."
            if state == "READY_FOR_LICENSED_STAGING"
            else "Resolve every BLOCKED external-source/handoff/dependency check; do not run candidate build or claim UART/UPER verification from this audit."
        ),
    }


def _write_report(repo_root: Path, output: Path, report: Dict[str, Any]) -> None:
    repo_root = repo_root.resolve()
    output = output.resolve()
    artifact_root = (repo_root / "artifacts").resolve()
    try:
        output.relative_to(artifact_root)
    except ValueError as exc:
        raise ValueError("--output must be under <repo>/artifacts/ so the readiness report cannot dirty tracked source") from exc
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--hero-source", default=None, help="Unity Assets/... or repo-relative unity_game/Assets/... production model")
    parser.add_argument("--output", default=None, help="Optional JSON path under <repo>/artifacts/")
    parser.add_argument("--allow-blocked", action="store_true", help="Return exit code 0 for reporting even when state is BLOCKED")
    parser.add_argument("--allow-dirty", action="store_true", help="Debug inspection only; never makes staging release-eligible")
    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    repo_root = Path(args.repo_root).resolve()
    report = audit(repo_root, hero_source=args.hero_source, require_clean=not args.allow_dirty)
    print(json.dumps(report, indent=2, sort_keys=True))
    if args.output:
        try:
            _write_report(repo_root, Path(args.output), report)
        except ValueError as exc:
            print(f"AFAREET_P1_STAGING_READINESS_ERROR {exc}", file=sys.stderr)
            return 2
    if report["state"] == "BLOCKED" and not args.allow_blocked:
        return 3
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
