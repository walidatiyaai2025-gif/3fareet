#!/usr/bin/env python3
"""Technical preflight for an authored AFAREET KING Blender source.

Run inside Blender, for example:
  blender --background AfareetKing.blend --python tools/blender/validate_afareet_king_production.py -- --output hero-preflight.json

This validates technical handoff properties only. Passing never means the source is
owner-authored, licensed, visually accepted, production-gated, device-verified or approved
for UART-003 / UPER-009.
"""

from __future__ import annotations

import argparse
import json
import re
import sys
from pathlib import Path
from typing import Any, Iterable

EXPECTED_TASK = "UART-003"
EXPECTED_ASSET = "AFAREET KING"
EXPECTED_LODS = (0, 1, 2)
EXPECTED_BRAND_TOKEN = "3FREET"
BOUNDARY = "TECHNICAL_PREFLIGHT_ONLY_OWNER_LICENSE_VISUAL_UNITY_DEVICE_GATES_REQUIRED"


class PreflightError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise PreflightError(message)


def classify_lod(name: str) -> int | None:
    normalized = (name or "").upper()
    matches = [lod for lod in EXPECTED_LODS if f"_LOD{lod}" in normalized]
    if len(matches) != 1:
        return None
    return matches[0]


def detect_branding_stamp(object_names: Iterable[str]) -> dict[str, Any]:
    names = sorted({str(name) for name in object_names if str(name).strip()})
    matches = [name for name in names if EXPECTED_BRAND_TOKEN in name.upper()]
    errors = [] if matches else [f"required {EXPECTED_BRAND_TOKEN} branding stamp object was not found"]
    return {
        "requiredToken": EXPECTED_BRAND_TOKEN,
        "matchedObjects": matches,
        "passed": bool(matches),
        "errors": errors,
    }


def parse_policy(policy_path: Path) -> dict[str, list[int]]:
    _require(policy_path.is_file(), f"HeroCarLodPolicy.cs does not exist: {policy_path}")
    text = policy_path.read_text(encoding="utf-8")
    result: dict[str, list[int]] = {}
    for name in ("MinimumVertices", "VertexBudgets", "MinimumTriangles", "TriangleBudgets"):
        match = re.search(rf"{name}\s*=\s*\{{\s*([^}}]+)\}}", text)
        _require(match is not None, f"cannot parse HeroCarLodPolicy.{name}")
        values = [int(value.strip()) for value in match.group(1).split(",")]
        _require(len(values) == 3, f"HeroCarLodPolicy.{name} must contain exactly three values")
        result[name] = values
    return result


def evaluate_lod_totals(
    lod_totals: dict[int, dict[str, Any]],
    policy: dict[str, list[int]],
) -> dict[str, Any]:
    lod_results = []
    errors: list[str] = []

    for lod in EXPECTED_LODS:
        totals = lod_totals.get(lod)
        if totals is None:
            errors.append(f"LOD{lod} has no mesh objects")
            totals = {
                "meshObjects": 0,
                "vertices": 0,
                "triangles": 0,
                "uv0MissingObjects": [],
                "unappliedScaleObjects": [],
            }

        vertices = int(totals.get("vertices", 0))
        triangles = int(totals.get("triangles", 0))
        minimum_vertices = policy["MinimumVertices"][lod]
        vertex_budget = policy["VertexBudgets"][lod]
        minimum_triangles = policy["MinimumTriangles"][lod]
        triangle_budget = policy["TriangleBudgets"][lod]
        vertex_ok = minimum_vertices <= vertices <= vertex_budget
        triangle_ok = minimum_triangles <= triangles <= triangle_budget
        uv_missing = list(totals.get("uv0MissingObjects", []))
        scale_bad = list(totals.get("unappliedScaleObjects", []))
        mesh_objects = int(totals.get("meshObjects", 0))

        if mesh_objects <= 0:
            errors.append(f"LOD{lod} must contain at least one mesh object")
        if not vertex_ok:
            errors.append(f"LOD{lod} vertex count {vertices} is outside [{minimum_vertices}, {vertex_budget}]")
        if not triangle_ok:
            errors.append(f"LOD{lod} triangle count {triangles} is outside [{minimum_triangles}, {triangle_budget}]")
        if uv_missing:
            errors.append(f"LOD{lod} mesh objects missing UV0: {', '.join(sorted(uv_missing))}")
        if scale_bad:
            errors.append(f"LOD{lod} mesh objects have unapplied scale: {', '.join(sorted(scale_bad))}")

        lod_results.append(
            {
                "lod": lod,
                "meshObjects": mesh_objects,
                "vertices": vertices,
                "minimumVertices": minimum_vertices,
                "vertexBudget": vertex_budget,
                "verticesWithinRange": vertex_ok,
                "triangles": triangles,
                "minimumTriangles": minimum_triangles,
                "triangleBudget": triangle_budget,
                "trianglesWithinRange": triangle_ok,
                "uv0MissingObjects": sorted(uv_missing),
                "unappliedScaleObjects": sorted(scale_bad),
                "technicalRangePass": mesh_objects > 0 and vertex_ok and triangle_ok and not uv_missing and not scale_bad,
            }
        )

    return {
        "lods": lod_results,
        "errors": errors,
        "technicalPreflightPassed": len(errors) == 0,
    }


def build_report(
    evaluation: dict[str, Any],
    *,
    source_file: str,
    wheel_check: dict[str, Any],
    branding_check: dict[str, Any],
) -> dict[str, Any]:
    errors = list(evaluation.get("errors", []))
    if not wheel_check.get("passed", False):
        errors.extend(wheel_check.get("errors", []))
    if not branding_check.get("passed", False):
        errors.extend(branding_check.get("errors", []))

    passed = (
        bool(evaluation.get("technicalPreflightPassed"))
        and bool(wheel_check.get("passed"))
        and bool(branding_check.get("passed"))
    )
    return {
        "schemaVersion": 2,
        "task": EXPECTED_TASK,
        "asset": EXPECTED_ASSET,
        "sourceFile": source_file,
        "verdict": "TECHNICAL_PREFLIGHT_PASS_NOT_PRODUCTION_ACCEPTANCE" if passed else "TECHNICAL_PREFLIGHT_BLOCKED",
        "technicalPreflightPassed": passed,
        "lods": evaluation.get("lods", []),
        "wheelCheck": wheel_check,
        "brandingCheck": branding_check,
        "errors": errors,
        "productionGate": False,
        "visualAcceptance": False,
        "ownerApproval": False,
        "provenanceAccepted": False,
        "licensedUnityImportVerified": False,
        "physicalDeviceVerified": False,
        "verified": False,
        "boundary": BOUNDARY,
    }


def _scale_is_applied(scale: Iterable[float], epsilon: float = 0.0001) -> bool:
    values = tuple(float(value) for value in scale)
    return len(values) == 3 and all(abs(value - 1.0) <= epsilon for value in values)


def collect_blender_scene() -> tuple[dict[int, dict[str, Any]], dict[str, Any], dict[str, Any], str]:
    try:
        import bpy  # type: ignore
    except ImportError as exc:
        raise PreflightError("this command must run inside Blender with bpy available") from exc

    depsgraph = bpy.context.evaluated_depsgraph_get()
    scene_objects = list(bpy.context.scene.objects)
    branding_check = detect_branding_stamp(obj.name for obj in scene_objects)
    lod_totals = {
        lod: {
            "meshObjects": 0,
            "vertices": 0,
            "triangles": 0,
            "uv0MissingObjects": [],
            "unappliedScaleObjects": [],
        }
        for lod in EXPECTED_LODS
    }
    unclassified_meshes: list[str] = []
    lod0_wheels: set[str] = set()

    for obj in scene_objects:
        if obj.type != "MESH":
            continue
        lod = classify_lod(obj.name)
        if lod is None:
            unclassified_meshes.append(obj.name)
            continue

        evaluated = obj.evaluated_get(depsgraph)
        mesh = evaluated.to_mesh()
        try:
            mesh.calc_loop_triangles()
            totals = lod_totals[lod]
            totals["meshObjects"] += 1
            totals["vertices"] += len(mesh.vertices)
            totals["triangles"] += len(mesh.loop_triangles)
            if len(mesh.uv_layers) <= 0:
                totals["uv0MissingObjects"].append(obj.name)
            if not _scale_is_applied(obj.scale):
                totals["unappliedScaleObjects"].append(obj.name)
        finally:
            evaluated.to_mesh_clear()

        upper_name = obj.name.upper()
        if lod == 0 and "WHEEL" in upper_name:
            for wheel in ("FL", "FR", "RL", "RR"):
                if f"_{wheel}_" in upper_name or upper_name.endswith(f"_{wheel}_LOD0"):
                    lod0_wheels.add(wheel)

    wheel_errors = []
    missing_wheels = sorted(set(("FL", "FR", "RL", "RR")) - lod0_wheels)
    if missing_wheels:
        wheel_errors.append(f"LOD0 is missing named wheel assemblies: {', '.join(missing_wheels)}")
    if unclassified_meshes:
        wheel_errors.append(
            "mesh objects must carry exactly one _LOD0/_LOD1/_LOD2 marker: "
            + ", ".join(sorted(unclassified_meshes))
        )

    wheel_check = {
        "required": ["FL", "FR", "RL", "RR"],
        "found": sorted(lod0_wheels),
        "unclassifiedMeshObjects": sorted(unclassified_meshes),
        "errors": wheel_errors,
        "passed": not wheel_errors,
    }
    source_file = str(Path(bpy.data.filepath).resolve()) if bpy.data.filepath else "<unsaved>"
    return lod_totals, wheel_check, branding_check, source_file


def default_policy_path() -> Path:
    return Path(__file__).resolve().parents[2] / "unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs"


def parse_blender_args(argv: list[str]) -> argparse.Namespace:
    arguments = argv[argv.index("--") + 1 :] if "--" in argv else []
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--policy", type=Path, default=default_policy_path())
    parser.add_argument("--output", type=Path)
    return parser.parse_args(arguments)


def main(argv: list[str] | None = None) -> int:
    args = parse_blender_args(sys.argv if argv is None else argv)
    try:
        policy = parse_policy(args.policy)
        lod_totals, wheel_check, branding_check, source_file = collect_blender_scene()
        evaluation = evaluate_lod_totals(lod_totals, policy)
        report = build_report(
            evaluation,
            source_file=source_file,
            wheel_check=wheel_check,
            branding_check=branding_check,
        )
        if args.output:
            output = args.output.resolve()
            _require(not output.exists(), f"refusing to overwrite existing report: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(json.dumps(report, indent=2, sort_keys=True))
        return 0 if report["technicalPreflightPassed"] else 2
    except (PreflightError, OSError, ValueError) as exc:
        print(f"AFAREET_HERO_BLENDER_PREFLIGHT_BLOCKED error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
