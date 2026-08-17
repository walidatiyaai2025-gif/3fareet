#!/usr/bin/env python3
"""Read-only source-readiness audit for the six P1 visual/runtime blockers.

This audit deliberately separates source readiness from licensed Unity/runtime/device/owner
verification. A SOURCE_READY result means the tracked authored inputs and fail-closed runtime
contracts are structurally present for the next licensed staging step. It never marks a U-P1 task
VERIFIED, accepted, publication eligible, or complete.
"""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from pathlib import Path
from typing import Any, Dict, Iterable, List, Optional, Sequence, Tuple

SCRIPT_DIR = Path(__file__).resolve().parent
if str(SCRIPT_DIR) not in sys.path:
    sys.path.insert(0, str(SCRIPT_DIR))

import validate_hero_asset_intake

TASK_IDS = ("UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011")
READY_STATES = {"SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF", "UNITY_INSPECTION_REQUIRED"}

RIVAL_ROOT = "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals"
RIVAL_VARIANTS = (
    ("Rival_01_WedgeCoupe.obj", "Rival_01_WedgeCoupe.mtl", "T_Rival_01_BC.png"),
    ("Rival_02_FastbackMuscle.obj", "Rival_02_FastbackMuscle.mtl", "T_Rival_02_BC.png"),
    ("Rival_03_CompactPrototype.obj", "Rival_03_CompactPrototype.mtl", "T_Rival_03_BC.png"),
)

MANIFESTS = {
    "UART-005": "docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json",
    "UART-006": "docs/assets/03_props_architecture/cairo_landmarks/ASSET_MANIFEST.json",
    "UART-007": "docs/assets/02_tracks_environments/cairo_track_dressing/ASSET_MANIFEST.json",
}

URAC011_FILES = (
    "unity_game/Assets/Afareet/Resources/Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1.json",
    "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs",
    "unity_game/Assets/Afareet/Scripts/World/CairoVerticalSliceLayout.cs",
    "unity_game/Assets/Afareet/Editor/CairoVerticalSliceLayoutBuildGate.cs",
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


def _tracked(repo_root: Path, relative_path: str) -> bool:
    code, _, _ = _run_git(repo_root, ["ls-files", "--error-unmatch", "--", relative_path])
    return code == 0


def _normalize_hero(hero_source: Optional[str]) -> Optional[str]:
    value = (hero_source or "").strip().replace("\\", "/")
    while value.startswith("./"):
        value = value[2:]
    if value.startswith("Assets/"):
        value = "unity_game/" + value
    return value or None


def _task(task_id: str, state: str, checks: List[Dict[str, Any]], detail: str) -> Dict[str, Any]:
    blocked = [item for item in checks if item["status"] != "PASS"]
    if blocked:
        state = "BLOCKED"
    return {
        "taskId": task_id,
        "state": state,
        "sourceReady": state in READY_STATES,
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "detail": detail,
        "blockedCheckIds": [item["id"] for item in blocked],
        "checks": checks,
    }


def _check(checks: List[Dict[str, Any]], check_id: str, ok: bool, detail: str) -> bool:
    checks.append({"id": check_id, "status": "PASS" if ok else "BLOCKED", "detail": detail})
    return ok


def _tracked_file(checks: List[Dict[str, Any]], repo_root: Path, task_id: str, relative_path: str) -> bool:
    path = repo_root / relative_path
    exists = path.is_file()
    tracked = exists and _tracked(repo_root, relative_path)
    return _check(
        checks,
        f"{task_id}:TRACKED:{relative_path}",
        tracked,
        "tracked" if tracked else "missing or untracked",
    )


def _read_json(path: Path) -> Dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(payload, dict):
        raise ValueError(f"JSON root must be an object: {path}")
    return payload


def _validate_obj_surface_chain(
    repo_root: Path,
    source_root: str,
    model_name: str,
    checks: List[Dict[str, Any]],
    task_id: str,
) -> bool:
    obj_relative = f"{source_root.rstrip('/')}/{model_name}"
    ok = _tracked_file(checks, repo_root, task_id, obj_relative)
    if not ok:
        return False

    obj_path = repo_root / obj_relative
    text = obj_path.read_text(encoding="utf-8", errors="replace")
    structural = "\nvt " in "\n" + text and "\nvn " in "\n" + text and "\nusemtl " in "\n" + text
    _check(checks, f"{task_id}:SURFACE:{model_name}", structural, "uv+normal+material streams" if structural else "OBJ surface stream incomplete")

    mtllibs: List[str] = []
    for raw in text.splitlines():
        line = raw.strip()
        if line.lower().startswith("mtllib "):
            mtllibs.extend(part for part in line.split()[1:] if part)
    _check(checks, f"{task_id}:MTLLIB:{model_name}", bool(mtllibs), ",".join(mtllibs) if mtllibs else "missing mtllib")

    texture_count = 0
    for mtl_name in mtllibs:
        mtl_relative = f"{source_root.rstrip('/')}/{mtl_name}"
        if not _tracked_file(checks, repo_root, task_id, mtl_relative):
            continue
        mtl_path = repo_root / mtl_relative
        for raw in mtl_path.read_text(encoding="utf-8", errors="replace").splitlines():
            line = raw.strip()
            lower = line.lower()
            if lower.startswith("map_kd ") or lower.startswith("map_basecolor ") or lower.startswith("map_base_color "):
                texture_name = line.split(maxsplit=1)[1].strip()
                texture_relative = f"{source_root.rstrip('/')}/{texture_name}"
                if _tracked_file(checks, repo_root, task_id, texture_relative):
                    texture_count += 1
    _check(checks, f"{task_id}:TEXTURE:{model_name}", texture_count > 0, f"tracked base-color mappings={texture_count}")
    return structural and bool(mtllibs) and texture_count > 0


def _audit_hero(repo_root: Path, hero_source: Optional[str]) -> Dict[str, Any]:
    checks: List[Dict[str, Any]] = []
    normalized = _normalize_hero(hero_source)
    if not normalized:
        _check(checks, "UART-003:SOURCE", False, "real externally-authored Hero source not supplied")
        return _task("UART-003", "BLOCKED", checks, "real Afareet King production source is still required")

    hero_path = repo_root / normalized
    canonical_root = "unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/"
    _check(checks, "UART-003:CANONICAL_ROOT", normalized.startswith(canonical_root), normalized)
    _check(checks, "UART-003:TRACKED", hero_path.is_file() and _tracked(repo_root, normalized), "tracked Hero source" if hero_path.is_file() and _tracked(repo_root, normalized) else "missing or untracked Hero source")

    if any(item["status"] != "PASS" for item in checks):
        return _task("UART-003", "BLOCKED", checks, "Hero source path/tracking preconditions failed")

    try:
        intake = validate_hero_asset_intake.validate_intake(repo_root, hero_path)
    except (validate_hero_asset_intake.HeroAssetIntakeError, OSError, ValueError) as exc:
        _check(checks, "UART-003:INTAKE", False, str(exc))
        return _task("UART-003", "BLOCKED", checks, "Hero intake rejected the supplied source")

    verdict = str(intake.get("verdict") or "")
    allowed = verdict in {"READY_FOR_LICENSED_UNITY_IMPORT", "UNITY_INSPECTION_REQUIRED"}
    _check(checks, "UART-003:INTAKE", allowed, verdict or "unexpected Hero intake verdict")
    state = "SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF" if verdict == "READY_FOR_LICENSED_UNITY_IMPORT" else "UNITY_INSPECTION_REQUIRED"
    return _task("UART-003", state, checks, "source intake only; licensed binding/render and owner proof remain pending")


def _audit_rivals(repo_root: Path) -> Dict[str, Any]:
    checks: List[Dict[str, Any]] = []
    obj_hashes = set()
    for obj_name, mtl_name, texture_name in RIVAL_VARIANTS:
        for filename in (obj_name, mtl_name, texture_name):
            _tracked_file(checks, repo_root, "UART-004", f"{RIVAL_ROOT}/{filename}")
        obj_path = repo_root / RIVAL_ROOT / obj_name
        if obj_path.is_file():
            text = obj_path.read_text(encoding="utf-8", errors="replace")
            for suffix in ("_LOD0", "_LOD1", "_LOD2"):
                _check(checks, f"UART-004:{obj_name}:{suffix}", suffix in text, "LOD group present" if suffix in text else "LOD group missing")
            _check(checks, f"UART-004:{obj_name}:UV", "\nvt " in "\n" + text, "UV stream present")
            _check(checks, f"UART-004:{obj_name}:NORMAL", "\nvn " in "\n" + text, "normal stream present")
            code, digest, _ = _run_git(repo_root, ["hash-object", "--", f"{RIVAL_ROOT}/{obj_name}"])
            if code == 0 and digest:
                obj_hashes.add(digest)
    _check(checks, "UART-004:DISTINCT_SOURCES", len(obj_hashes) == 3, f"distinct tracked OBJ hashes={len(obj_hashes)}/3")
    return _task(
        "UART-004",
        "SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF",
        checks,
        "three authored Rival source chains are present; licensed prefab/runtime/owner proof remains pending",
    )


def _audit_manifest_task(repo_root: Path, task_id: str, relative_manifest: str) -> Dict[str, Any]:
    checks: List[Dict[str, Any]] = []
    if not _tracked_file(checks, repo_root, task_id, relative_manifest):
        return _task(task_id, "BLOCKED", checks, "production source manifest missing")

    try:
        manifest = _read_json(repo_root / relative_manifest)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        _check(checks, f"{task_id}:MANIFEST_JSON", False, str(exc))
        return _task(task_id, "BLOCKED", checks, "production source manifest unreadable")

    _check(checks, f"{task_id}:TASK_ID", manifest.get("taskId") == task_id, str(manifest.get("taskId")))
    _check(checks, f"{task_id}:REVIEW_BLOCKED", manifest.get("reviewState") == "BLOCKED", "source ledger remains fail-closed")
    _check(checks, f"{task_id}:SOURCE_QUALITY", manifest.get("sourceQuality") == "authored-source-candidate", str(manifest.get("sourceQuality")))
    _check(checks, f"{task_id}:INTEGRATION_IMPLEMENTED", manifest.get("runtimeIntegrationImplemented") is True, str(manifest.get("runtimeIntegrationImplemented")))
    _check(checks, f"{task_id}:NOT_RUNTIME_VERIFIED", manifest.get("runtimeIntegrationVerified") is False, "runtime verification remains pending")
    _check(checks, f"{task_id}:NO_PROCEDURAL_CANDIDATE", manifest.get("proceduralFallbackAllowedInCandidate") is False, "candidate fallback disabled")

    source_root = str(manifest.get("sourceRoot") or "").strip().replace("\\", "/")
    modules = manifest.get("modules")
    _check(checks, f"{task_id}:SOURCE_ROOT", bool(source_root), source_root or "missing sourceRoot")
    _check(checks, f"{task_id}:MODULES", isinstance(modules, list) and bool(modules), f"modules={len(modules) if isinstance(modules, list) else 0}")

    if source_root and isinstance(modules, list):
        for module in modules:
            if not isinstance(module, dict):
                _check(checks, f"{task_id}:MODULE_OBJECT", False, "module entry is not an object")
                continue
            model = str(module.get("model") or "")
            if not model:
                _check(checks, f"{task_id}:MODULE_MODEL", False, "module missing model")
                continue
            _validate_obj_surface_chain(repo_root, source_root, model, checks, task_id)
            current_v = int(module.get("currentVertices") or 0)
            current_t = int(module.get("currentTriangles") or 0)
            min_v = int(module.get("productionMinVertices") or 0)
            min_t = int(module.get("productionMinTriangles") or 0)
            _check(checks, f"{task_id}:BUDGET:{model}", min_v > 0 and min_t > 0 and current_v >= min_v and current_t >= min_t, f"vertices={current_v}/{min_v}+ triangles={current_t}/{min_t}+")
            _check(checks, f"{task_id}:AUTHORING:{model}", module.get("surfaceAuthoring") == "tracked-uv-normal-mtl-texture-candidate", str(module.get("surfaceAuthoring")))

    pending = manifest.get("acceptancePending")
    _check(checks, f"{task_id}:ACCEPTANCE_PENDING", isinstance(pending, list) and bool(pending), "licensed/device/owner acceptance explicitly remains pending")
    return _task(
        task_id,
        "SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF",
        checks,
        "authored source package and fail-closed integration contract are present; runtime/device/owner acceptance remains pending",
    )


def _audit_urac011(repo_root: Path) -> Dict[str, Any]:
    checks: List[Dict[str, Any]] = []
    for relative in URAC011_FILES:
        _tracked_file(checks, repo_root, "URAC-011", relative)
    if any(item["status"] != "PASS" for item in checks):
        return _task("URAC-011", "BLOCKED", checks, "authored vertical-slice contract files are incomplete")

    layout_path = repo_root / URAC011_FILES[0]
    try:
        layout = _read_json(layout_path)
    except (OSError, ValueError, json.JSONDecodeError) as exc:
        _check(checks, "URAC-011:LAYOUT_JSON", False, str(exc))
        return _task("URAC-011", "BLOCKED", checks, "authored vertical-slice layout is unreadable")

    points = layout.get("points")
    sectors = {str(item.get("sector")) for item in points if isinstance(item, dict)} if isinstance(points, list) else set()
    _check(checks, "URAC-011:SCHEMA", layout.get("schemaVersion") == 1, str(layout.get("schemaVersion")))
    _check(checks, "URAC-011:LAYOUT_ID", layout.get("layoutId") == "cairo-night-vertical-slice-v1", str(layout.get("layoutId")))
    _check(checks, "URAC-011:AUTHORED", layout.get("authoringState") == "AUTHORED_LAYOUT", str(layout.get("authoringState")))
    _check(checks, "URAC-011:CLOSED_LOOP", layout.get("closedLoop") is True, str(layout.get("closedLoop")))
    _check(checks, "URAC-011:CONTROL_POINTS", isinstance(points, list) and len(points) == 24, f"points={len(points) if isinstance(points, list) else 0}")
    _check(checks, "URAC-011:SAMPLES", layout.get("samplesPerControlPoint") == 3, str(layout.get("samplesPerControlPoint")))
    _check(checks, "URAC-011:SECTORS", len(sectors) >= 6, f"sectors={len(sectors)}")

    builder = (repo_root / URAC011_FILES[1]).read_text(encoding="utf-8", errors="replace")
    runtime = (repo_root / URAC011_FILES[2]).read_text(encoding="utf-8", errors="replace")
    gate = (repo_root / URAC011_FILES[3]).read_text(encoding="utf-8", errors="replace")
    _check(checks, "URAC-011:PLAYER_FAIL_CLOSED", "ellipse-fallback-disabled" in builder and "AFAREET_URAC011_PLAYER_LAYOUT_REQUIRED" in builder, "Player requires authored layout")
    _check(checks, "URAC-011:RUNTIME_72", "RuntimeSegmentCount = RequiredControlPoints * SamplesPerControlPoint" in runtime, "runtime samples authored route")
    _check(checks, "URAC-011:ANDROID_GATE", "AFAREET_URAC011_VERTICAL_SLICE_GATE_OK" in gate and "BuildTarget.Android" in gate, "Android build gate present")
    return _task(
        "URAC-011",
        "SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF",
        checks,
        "authored layout/runtime gate is source-ready; exact-candidate runtime/device/owner proof remains pending",
    )


def audit_visual_sources(repo_root: Path, hero_source: Optional[str] = None) -> Dict[str, Any]:
    repo_root = repo_root.resolve()
    tasks = [
        _audit_hero(repo_root, hero_source),
        _audit_rivals(repo_root),
        _audit_manifest_task(repo_root, "UART-005", MANIFESTS["UART-005"]),
        _audit_manifest_task(repo_root, "UART-006", MANIFESTS["UART-006"]),
        _audit_manifest_task(repo_root, "UART-007", MANIFESTS["UART-007"]),
        _audit_urac011(repo_root),
    ]
    blocked = [item for item in tasks if item["state"] == "BLOCKED"]
    ready = [item for item in tasks if item["state"] in READY_STATES]
    state = "READY_FOR_LICENSED_VISUAL_STAGING" if not blocked and len(ready) == len(TASK_IDS) else "BLOCKED"
    return {
        "schemaVersion": 1,
        "scope": list(TASK_IDS),
        "state": state,
        "readyForLicensedVisualStaging": state == "READY_FOR_LICENSED_VISUAL_STAGING",
        "sourceReadyCount": len(ready),
        "blockedCount": len(blocked),
        "verified": False,
        "runtimeVerified": False,
        "ownerAccepted": False,
        "publicationEligible": False,
        "tasks": tasks,
        "blockedTaskIds": [item["taskId"] for item in blocked],
        "nextAction": (
            "Run the licensed staging/runtime proof chain; do not treat source readiness as UART/URAC verification."
            if state == "READY_FOR_LICENSED_VISUAL_STAGING"
            else "Resolve every BLOCKED source task first; current results do not authorize licensed candidate publication."
        ),
    }


def _write_report(repo_root: Path, output: Path, report: Dict[str, Any]) -> None:
    repo_root = repo_root.resolve()
    output = output.resolve()
    artifact_root = (repo_root / "artifacts").resolve()
    try:
        output.relative_to(artifact_root)
    except ValueError as exc:
        raise ValueError("--output must stay under <repo>/artifacts/") from exc
    output.parent.mkdir(parents=True, exist_ok=True)
    if output.exists():
        raise ValueError(f"refusing to overwrite existing visual readiness report: {output}")
    output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def parse_args(argv: Optional[Sequence[str]] = None) -> argparse.Namespace:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--hero-source", default=None)
    parser.add_argument("--output", default=None)
    parser.add_argument("--allow-blocked", action="store_true")
    return parser.parse_args(argv)


def main(argv: Optional[Sequence[str]] = None) -> int:
    args = parse_args(argv)
    repo_root = Path(args.repo_root).resolve()
    report = audit_visual_sources(repo_root, hero_source=args.hero_source)
    print(json.dumps(report, indent=2, sort_keys=True))
    if args.output:
        try:
            _write_report(repo_root, Path(args.output), report)
        except ValueError as exc:
            print(f"AFAREET_P1_VISUAL_SOURCE_READINESS_ERROR {exc}", file=sys.stderr)
            return 2
    if report["state"] == "BLOCKED" and not args.allow_blocked:
        return 3
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
