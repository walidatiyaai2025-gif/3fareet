#!/usr/bin/env python3
"""Fail-closed readiness audit for the P1 licensed production-art staging handoff.

This command is intentionally read-only with respect to tracked repository content. It answers one
question: is this exact clean checkout structurally ready to run the licensed Unity staging handoff?
It does not run Unity, stage assets, build an APK, or promote any P1 verification state.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path
from typing import Any, Dict, List, Optional, Sequence, Tuple

SUPPORTED_HERO_EXTENSIONS = {".fbx", ".obj", ".blend", ".glb", ".gltf"}
FORBIDDEN_HERO_TOKENS = (
    "generated",
    "placeholder",
    "legacyprocedural",
    "preview",
    "refinement",
    "refinementcandidates",
    "blockout",
    "review",
    "reviewpackaging",
)

RIVAL_REQUIRED_FILES = (
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj.meta",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj.meta",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj",
    "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj.meta",
)

HANDOFF_REQUIRED_FILES = (
    "unity_game/Assets/Afareet/Editor/P1ProductionCandidateStagingHandoff.cs",
    "unity_game/Assets/Afareet/Editor/HeroCarProductionPrefabStager.cs",
    "unity_game/Assets/Afareet/Editor/RivalProductionPrefabStager.cs",
    "unity_game/Assets/Afareet/Editor/P1ProductionWorldAssetStager.cs",
    "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkAssetStager.cs",
    "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingAssetStager.cs",
    "unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs",
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
        ["git", "-C", str(repo_root), *args],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
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


def _normalize_hero_path(hero_source: str) -> str:
    value = (hero_source or "").strip().replace("\\", "/")
    if value.startswith("Assets/"):
        value = "unity_game/" + value
    while value.startswith("./"):
        value = value[2:]
    return value


def audit(repo_root: Path, hero_source: Optional[str] = None, require_clean: bool = True) -> Dict[str, Any]:
    repo_root = repo_root.resolve()
    checks: List[Dict[str, Any]] = []

    code, top_level, git_error = _run_git(repo_root, ["rev-parse", "--show-toplevel"])
    is_git_root = code == 0 and Path(top_level).resolve() == repo_root
    _check(
        checks,
        "GIT_ROOT",
        is_git_root,
        "repo-root-is-exact-git-toplevel" if is_git_root else f"expected exact Git root; git={git_error or top_level or '<none>'}",
    )

    code, git_sha, _ = _run_git(repo_root, ["rev-parse", "HEAD"])
    valid_sha = code == 0 and len(git_sha) == 40 and all(c in "0123456789abcdefABCDEF" for c in git_sha)
    _check(checks, "GIT_SHA", valid_sha, git_sha if valid_sha else "unable to resolve full 40-character HEAD")

    code, status, status_error = _run_git(repo_root, ["status", "--porcelain"])
    clean = code == 0 and status == ""
    if require_clean:
        _check(
            checks,
            "CLEAN_TREE",
            clean,
            "clean" if clean else f"working tree is dirty or unreadable: {status or status_error or '<unknown>'}",
        )
    else:
        _check(checks, "CLEAN_TREE", code == 0, f"clean={clean}; debug override does not make staging release-eligible")

    for path in HANDOFF_REQUIRED_FILES:
        exists = _nonempty_file(repo_root, path)
        tracked = exists and _tracked(repo_root, path)
        _check(
            checks,
            f"HANDOFF:{path}",
            exists and tracked,
            "tracked-nonempty" if exists and tracked else "missing, empty or untracked staging/candidate handoff dependency",
        )

    for path in WORLD_REQUIRED_FILES:
        exists = _nonempty_file(repo_root, path)
        tracked = exists and _tracked(repo_root, path)
        _check(
            checks,
            f"WORLD:{path}",
            exists and tracked,
            "tracked-nonempty" if exists and tracked else "missing, empty or untracked production-world manifest",
        )

    for path in RIVAL_REQUIRED_FILES:
        exists = _nonempty_file(repo_root, path)
        tracked = exists and _tracked(repo_root, path)
        _check(
            checks,
            f"RIVAL:{path}",
            exists and tracked,
            "tracked-nonempty" if exists and tracked else "missing, empty or untracked UART-004 isolated production source/Unity metadata",
        )

    normalized_hero = _normalize_hero_path(hero_source or "")
    hero_supplied = bool(normalized_hero)
    _check(
        checks,
        "UART-003_HERO_SOURCE_SUPPLIED",
        hero_supplied,
        normalized_hero if hero_supplied else "real externally-authored Hero production source not supplied",
    )

    if hero_supplied:
        lower = normalized_hero.lower()
        under_assets = normalized_hero.startswith("unity_game/Assets/")
        _check(
            checks,
            "UART-003_HERO_UNITY_ASSET_PATH",
            under_assets,
            normalized_hero if under_assets else "Hero must resolve under unity_game/Assets/ (or be passed as Assets/...)",
        )

        no_traversal = "../" not in normalized_hero
        _check(
            checks,
            "UART-003_HERO_NO_TRAVERSAL",
            no_traversal,
            "no traversal" if no_traversal else "Hero source path cannot contain ../ traversal",
        )

        vehicle_role = "/vehicles/" in lower
        _check(
            checks,
            "UART-003_HERO_VEHICLE_ROLE",
            vehicle_role,
            "vehicle role path" if vehicle_role else "Hero production source must resolve under a /Vehicles/ role path",
        )

        extension_ok = Path(normalized_hero).suffix.lower() in SUPPORTED_HERO_EXTENSIONS
        _check(
            checks,
            "UART-003_HERO_SUPPORTED_FORMAT",
            extension_ok,
            Path(normalized_hero).suffix.lower() or "missing extension",
        )

        forbidden = [token for token in FORBIDDEN_HERO_TOKENS if token in lower]
        _check(
            checks,
            "UART-003_HERO_NOT_NONPRODUCTION_PATH",
            not forbidden,
            "authored-production-source-path" if not forbidden else "forbidden source token(s): " + ",".join(forbidden),
        )

        exists = _nonempty_file(repo_root, normalized_hero)
        _check(checks, "UART-003_HERO_EXISTS", exists, normalized_hero if exists else "Hero file is missing or empty")

        tracked = exists and _tracked(repo_root, normalized_hero)
        _check(
            checks,
            "UART-003_HERO_TRACKED_BY_HEAD",
            tracked,
            "tracked by Git" if tracked else "Hero must be non-empty and committed before licensed staging starts",
        )

        hero_meta = normalized_hero + ".meta"
        hero_meta_ok = _nonempty_file(repo_root, hero_meta) and _tracked(repo_root, hero_meta)
        _check(
            checks,
            "UART-003_HERO_META_TRACKED_BY_HEAD",
            hero_meta_ok,
            "tracked non-empty Unity metadata" if hero_meta_ok else "Hero .meta must be non-empty and committed before licensed staging starts",
        )

        not_rival = "/rivals/" not in lower
        _check(
            checks,
            "UART-003_HERO_NOT_RIVAL_SOURCE",
            not_rival,
            "separate Hero source" if not_rival else "Rival source cannot be reused as the Hero production source",
        )

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
            "Run tools/android/stage_production_candidate_windows.ps1 with the tracked Hero + three isolated Rival production sources on licensed Unity 6000.5.8f1; review and commit staging output before candidate tests/build."
            if state == "READY_FOR_LICENSED_STAGING"
            else "Resolve every BLOCKED external-source/handoff check; do not run candidate build or claim UART/UPER verification from this audit."
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
    payload = json.dumps(report, indent=2, sort_keys=True)
    print(payload)
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
