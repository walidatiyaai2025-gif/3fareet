#!/usr/bin/env python3
"""Inspect the exact Afareet King GLB refinement companion against Hero mobile LOD budgets.

This is a refinement diagnostic only. It does not inspect the authoritative FBX as Unity
would import it and it never promotes UART-003, production art, visual acceptance or
verification state.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import struct
import sys
from pathlib import Path
from typing import Any

GLB_MAGIC = b"glTF"
GLB_VERSION = 2
JSON_CHUNK_TYPE = 0x4E4F534A
EXPECTED_CLASSIFICATION = "REFINEMENT_CANDIDATE"
EXPECTED_GLB_ROLE = "INSPECTION_COMPANION"
BASIS = "GLB_COMPANION_ACCESSOR_COUNTS_MATCHING_UNITY_STAGER_NAME_CLASSIFICATION"


class DiagnosticError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise DiagnosticError(message)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _load_json(path: Path, label: str) -> dict[str, Any]:
    _require(path.is_file(), f"{label} does not exist: {path}")
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise DiagnosticError(f"{label} is not valid JSON: {exc}") from exc
    _require(isinstance(payload, dict), f"{label} root must be a JSON object")
    return payload


def validate_companion_identity(glb_path: Path, receipt: dict[str, Any]) -> dict[str, Any]:
    _require(receipt.get("classification") == EXPECTED_CLASSIFICATION, "receipt must remain REFINEMENT_CANDIDATE")
    for key in ("productionGate", "visualAcceptance", "ownerApproval", "verified"):
        _require(receipt.get(key) is False, f"receipt.{key} must be JSON false")

    files = receipt.get("files")
    _require(isinstance(files, dict), "receipt.files must be an object")
    record = files.get("glb")
    _require(isinstance(record, dict), "receipt.files.glb must be an object")
    _require(record.get("role") == EXPECTED_GLB_ROLE, f"receipt.files.glb.role must be {EXPECTED_GLB_ROLE}")
    _require(glb_path.is_file(), f"GLB companion does not exist: {glb_path}")
    _require(glb_path.name == record.get("fileName"), "GLB companion file name does not match receipt")
    actual_size = glb_path.stat().st_size
    _require(actual_size == record.get("sizeBytes"), "GLB companion size does not match receipt")
    actual_sha = sha256_file(glb_path)
    _require(actual_sha == record.get("sha256"), "GLB companion SHA-256 does not match receipt")
    return {
        "fileName": glb_path.name,
        "sizeBytes": actual_size,
        "sha256": actual_sha,
    }


def read_glb_json(path: Path) -> dict[str, Any]:
    data = path.read_bytes()
    _require(len(data) >= 20, "GLB is too small")
    magic, version, declared_length = struct.unpack_from("<4sII", data, 0)
    _require(magic == GLB_MAGIC, "GLB magic is invalid")
    _require(version == GLB_VERSION, f"GLB version must be {GLB_VERSION}")
    _require(declared_length == len(data), "GLB declared length does not match file length")

    offset = 12
    json_payload = None
    while offset < len(data):
        _require(offset + 8 <= len(data), "GLB chunk header is truncated")
        chunk_length, chunk_type = struct.unpack_from("<II", data, offset)
        offset += 8
        _require(offset + chunk_length <= len(data), "GLB chunk exceeds declared file length")
        chunk = data[offset : offset + chunk_length]
        offset += chunk_length
        if chunk_type == JSON_CHUNK_TYPE and json_payload is None:
            json_payload = chunk

    _require(offset == len(data), "GLB contains trailing bytes")
    _require(json_payload is not None, "GLB is missing JSON chunk")
    try:
        payload = json.loads(json_payload.decode("utf-8").rstrip(" \t\r\n\0"))
    except (UnicodeDecodeError, json.JSONDecodeError) as exc:
        raise DiagnosticError(f"GLB JSON chunk is invalid: {exc}") from exc
    _require(isinstance(payload, dict), "GLB JSON root must be an object")
    return payload


def parse_policy(policy_path: Path) -> dict[str, list[int]]:
    _require(policy_path.is_file(), f"HeroCarLodPolicy.cs does not exist: {policy_path}")
    text = policy_path.read_text(encoding="utf-8")
    result: dict[str, list[int]] = {}
    for name in ("MinimumVertices", "VertexBudgets", "MinimumTriangles", "TriangleBudgets"):
        match = re.search(rf"{name}\s*=\s*\{{\s*([^}}]+)\}}", text)
        _require(match is not None, f"cannot parse HeroCarLodPolicy.{name}")
        values = [int(value.strip()) for value in match.group(1).split(",")]
        _require(len(values) == 3, f"HeroCarLodPolicy.{name} must contain exactly 3 values")
        result[name] = values
    return result


def classify_lod(name: str) -> int:
    normalized = name or ""
    if "_LOD2" in normalized.upper():
        return 2
    if "_LOD1" in normalized.upper():
        return 1
    return 0


def _accessor_count(accessors: list[Any], index: Any, label: str) -> int:
    _require(isinstance(index, int) and not isinstance(index, bool), f"{label} accessor index must be an integer")
    _require(0 <= index < len(accessors), f"{label} accessor index is out of range")
    accessor = accessors[index]
    _require(isinstance(accessor, dict), f"{label} accessor must be an object")
    count = accessor.get("count")
    _require(isinstance(count, int) and not isinstance(count, bool) and count >= 0, f"{label} accessor count is invalid")
    return count


def _primitive_triangle_count(primitive: dict[str, Any], accessors: list[Any], position_count: int) -> int:
    mode = primitive.get("mode", 4)
    _require(isinstance(mode, int) and not isinstance(mode, bool), "primitive mode must be an integer")
    indices = primitive.get("indices")
    element_count = _accessor_count(accessors, indices, "indices") if indices is not None else position_count
    if mode == 4:
        _require(element_count % 3 == 0, "TRIANGLES primitive element count must be divisible by 3")
        return element_count // 3
    if mode in (5, 6):
        return max(0, element_count - 2)
    raise DiagnosticError(f"unsupported non-triangle GLB primitive mode: {mode}")


def inspect_glb(payload: dict[str, Any], policy: dict[str, list[int]]) -> dict[str, Any]:
    nodes = payload.get("nodes")
    meshes = payload.get("meshes")
    accessors = payload.get("accessors")
    _require(isinstance(nodes, list), "GLB nodes must be an array")
    _require(isinstance(meshes, list), "GLB meshes must be an array")
    _require(isinstance(accessors, list), "GLB accessors must be an array")

    totals = {
        lod: {"rendererNodes": 0, "vertices": 0, "triangles": 0}
        for lod in range(3)
    }
    for node_index, node in enumerate(nodes):
        _require(isinstance(node, dict), f"node {node_index} must be an object")
        mesh_index = node.get("mesh")
        if mesh_index is None:
            continue
        _require(isinstance(mesh_index, int) and not isinstance(mesh_index, bool), f"node {node_index} mesh index must be integer")
        _require(0 <= mesh_index < len(meshes), f"node {node_index} mesh index is out of range")
        mesh = meshes[mesh_index]
        _require(isinstance(mesh, dict), f"mesh {mesh_index} must be an object")
        primitives = mesh.get("primitives")
        _require(isinstance(primitives, list) and primitives, f"mesh {mesh_index} must contain primitives")

        lod = classify_lod(str(node.get("name", "")))
        node_vertices = 0
        node_triangles = 0
        seen_position_accessors: set[int] = set()
        for primitive_index, primitive in enumerate(primitives):
            _require(isinstance(primitive, dict), f"mesh {mesh_index} primitive {primitive_index} must be an object")
            attributes = primitive.get("attributes")
            _require(isinstance(attributes, dict), f"mesh {mesh_index} primitive {primitive_index} attributes must be an object")
            position_accessor = attributes.get("POSITION")
            _require(position_accessor is not None, f"mesh {mesh_index} primitive {primitive_index} is missing POSITION")
            position_count = _accessor_count(accessors, position_accessor, "POSITION")
            if position_accessor not in seen_position_accessors:
                node_vertices += position_count
                seen_position_accessors.add(position_accessor)
            node_triangles += _primitive_triangle_count(primitive, accessors, position_count)

        totals[lod]["rendererNodes"] += 1
        totals[lod]["vertices"] += node_vertices
        totals[lod]["triangles"] += node_triangles

    lod_results = []
    all_within_budget = True
    for lod in range(3):
        item = totals[lod]
        min_vertices = policy["MinimumVertices"][lod]
        max_vertices = policy["VertexBudgets"][lod]
        min_triangles = policy["MinimumTriangles"][lod]
        max_triangles = policy["TriangleBudgets"][lod]
        within = (
            min_vertices <= item["vertices"] <= max_vertices
            and min_triangles <= item["triangles"] <= max_triangles
        )
        all_within_budget &= within
        lod_results.append({
            "lod": lod,
            **item,
            "minimumVertices": min_vertices,
            "vertexBudget": max_vertices,
            "minimumTriangles": min_triangles,
            "triangleBudget": max_triangles,
            "verticesOverBudgetBy": max(0, item["vertices"] - max_vertices),
            "trianglesOverBudgetBy": max(0, item["triangles"] - max_triangles),
            "withinPolicyRange": within,
        })

    return {
        "basis": BASIS,
        "lods": lod_results,
        "mobileBudgetReady": all_within_budget,
        "verdict": "REFINEMENT_COMPANION_WITHIN_BUDGET" if all_within_budget else "REFINEMENT_COMPANION_OVER_BUDGET",
    }


def build_result(identity: dict[str, Any], diagnostic: dict[str, Any]) -> dict[str, Any]:
    return {
        "schemaVersion": 1,
        "task": "UART-003",
        "classification": EXPECTED_CLASSIFICATION,
        "glbCompanion": identity,
        **diagnostic,
        "authoritativeFbxUnityInspectionRequired": True,
        "productionGate": False,
        "visualAcceptance": False,
        "ownerApproval": False,
        "verified": False,
    }


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--glb", type=Path, required=True, help="Exact AfareetKing_Hero.glb companion")
    parser.add_argument(
        "--receipt",
        type=Path,
        default=Path("tools/android/hero_refinement_handoff_receipt.json"),
    )
    parser.add_argument(
        "--policy",
        type=Path,
        default=Path("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs"),
    )
    parser.add_argument("--output", type=Path, help="Optional JSON diagnostic; existing files are never overwritten")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(sys.argv[1:] if argv is None else argv)
    try:
        receipt = _load_json(args.receipt, "handoff receipt")
        identity = validate_companion_identity(args.glb, receipt)
        policy = parse_policy(args.policy)
        diagnostic = inspect_glb(read_glb_json(args.glb), policy)
        result = build_result(identity, diagnostic)
        if args.output:
            output = args.output.resolve()
            _require(not output.exists(), f"refusing to overwrite existing diagnostic: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_HERO_REFINEMENT_GLB_DIAGNOSTIC "
            f"verdict={result['verdict']} mobileBudgetReady={str(result['mobileBudgetReady']).lower()} "
            "authoritativeFbxUnityInspectionRequired=true productionGate=false verified=false"
        )
        return 0
    except (DiagnosticError, OSError, ValueError, struct.error) as exc:
        print(f"AFAREET_HERO_REFINEMENT_GLB_DIAGNOSTIC_BLOCKED error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
