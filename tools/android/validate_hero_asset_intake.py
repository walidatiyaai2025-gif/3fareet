#!/usr/bin/env python3
"""Fail-closed pre-Unity intake for the UART-003 production Hero asset.

This tool does not approve production art. It only rejects obviously invalid source
packages before licensed Unity staging. OBJ sources receive structural inspection;
FBX/GLB/GLTF/BLEND sources remain UNITY_INSPECTION_REQUIRED until Unity imports them.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import dataclass, asdict
from pathlib import Path
from typing import Iterable

SUPPORTED_SUFFIXES = {".obj", ".fbx", ".glb", ".gltf", ".blend"}
FORBIDDEN_SEGMENTS = {"generated", "preview", "blockout", "rivals"}
EXPECTED_ROOT = Path("unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar")
POLICY_PATH = Path("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs")


class HeroAssetIntakeError(RuntimeError):
    pass


@dataclass(frozen=True)
class LodStats:
    lod: int
    object_name: str
    vertices: int
    triangles: int
    has_complete_uv0: bool
    has_complete_normals: bool
    material_names: tuple[str, ...]


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise HeroAssetIntakeError(message)


def _repo_root(path: Path) -> Path:
    proc = subprocess.run(
        ["git", "-C", str(path), "rev-parse", "--show-toplevel"],
        text=True,
        capture_output=True,
        check=False,
    )
    _require(proc.returncode == 0, "git worktree is required for UART-003 intake")
    return Path(proc.stdout.strip()).resolve()


def _is_tracked(repo: Path, path: Path) -> bool:
    rel = path.resolve().relative_to(repo).as_posix()
    proc = subprocess.run(
        ["git", "-C", str(repo), "ls-files", "--error-unmatch", "--", rel],
        text=True,
        capture_output=True,
        check=False,
    )
    return proc.returncode == 0


def _parse_policy(repo: Path) -> dict[str, list[int]]:
    text = (repo / POLICY_PATH).read_text(encoding="utf-8")
    result: dict[str, list[int]] = {}
    for name in ("MinimumVertices", "VertexBudgets", "MinimumTriangles", "TriangleBudgets"):
        match = re.search(rf"{name}\s*=\s*\{{\s*([^}}]+)\}}", text)
        _require(match is not None, f"cannot parse HeroCarLodPolicy.{name}")
        values = [int(value.strip()) for value in match.group(1).split(",")]
        _require(len(values) == 3, f"HeroCarLodPolicy.{name} must contain exactly 3 values")
        result[name] = values
    return result


def _resolve_obj_index(token: str, count: int) -> int:
    value = int(token)
    _require(value != 0, "OBJ index 0 is invalid")
    resolved = value - 1 if value > 0 else count + value
    _require(0 <= resolved < count, f"OBJ index out of range: {token}")
    return resolved


def inspect_obj(path: Path) -> tuple[list[LodStats], tuple[Path, ...]]:
    vertices: list[tuple[float, float, float]] = []
    texcoords: list[tuple[float, ...]] = []
    normals: list[tuple[float, float, float]] = []
    current_object = ""
    current_material = ""
    mtllibs: list[str] = []
    groups: dict[int, dict[str, object]] = {
        lod: {
            "name": "",
            "vertices": set(),
            "triangles": 0,
            "uv_complete": True,
            "normal_complete": True,
            "materials": set(),
        }
        for lod in range(3)
    }

    def current_lod(name: str) -> int | None:
        match = re.search(r"_LOD([0-2])$", name, flags=re.IGNORECASE)
        return int(match.group(1)) if match else None

    for raw in path.read_text(encoding="utf-8", errors="strict").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split()
        op = parts[0]
        if op == "v":
            _require(len(parts) >= 4, "OBJ vertex requires xyz")
            vertices.append(tuple(float(x) for x in parts[1:4]))
        elif op == "vt":
            _require(len(parts) >= 3, "OBJ vt requires uv")
            texcoords.append(tuple(float(x) for x in parts[1:]))
        elif op == "vn":
            _require(len(parts) >= 4, "OBJ vn requires xyz")
            normals.append(tuple(float(x) for x in parts[1:4]))
        elif op in {"o", "g"}:
            current_object = " ".join(parts[1:]).strip()
            lod = current_lod(current_object)
            if lod is not None:
                existing = str(groups[lod]["name"])
                _require(not existing or existing == current_object, f"OBJ must expose exactly one object/group for LOD{lod}")
                groups[lod]["name"] = current_object
        elif op == "usemtl":
            current_material = " ".join(parts[1:]).strip()
        elif op == "mtllib":
            mtllibs.extend(parts[1:])
        elif op == "f":
            lod = current_lod(current_object)
            if lod is None:
                continue
            refs = parts[1:]
            _require(len(refs) >= 3, f"LOD{lod} OBJ face has fewer than 3 vertices")
            group = groups[lod]
            for ref in refs:
                comps = ref.split("/")
                _require(comps[0], f"LOD{lod} face is missing vertex index")
                vi = _resolve_obj_index(comps[0], len(vertices))
                group["vertices"].add(vi)
                has_uv = len(comps) >= 2 and bool(comps[1])
                has_normal = len(comps) >= 3 and bool(comps[2])
                if has_uv:
                    _resolve_obj_index(comps[1], len(texcoords))
                if has_normal:
                    _resolve_obj_index(comps[2], len(normals))
                group["uv_complete"] = bool(group["uv_complete"]) and has_uv
                group["normal_complete"] = bool(group["normal_complete"]) and has_normal
            group["triangles"] = int(group["triangles"]) + (len(refs) - 2)
            if current_material:
                group["materials"].add(current_material)

    stats: list[LodStats] = []
    for lod in range(3):
        group = groups[lod]
        name = str(group["name"])
        _require(name, f"OBJ is missing object/group suffix _LOD{lod}")
        _require(int(group["triangles"]) > 0, f"LOD{lod} has no faces")
        stats.append(
            LodStats(
                lod=lod,
                object_name=name,
                vertices=len(group["vertices"]),
                triangles=int(group["triangles"]),
                has_complete_uv0=bool(group["uv_complete"]),
                has_complete_normals=bool(group["normal_complete"]),
                material_names=tuple(sorted(group["materials"])),
            )
        )

    _require(mtllibs, "OBJ must reference at least one MTL file")
    mtl_paths = tuple((path.parent / name).resolve() for name in mtllibs)
    return stats, mtl_paths


def _texture_paths_from_mtl(path: Path) -> tuple[Path, ...]:
    _require(path.exists(), f"missing MTL file: {path}")
    textures: list[Path] = []
    for raw in path.read_text(encoding="utf-8", errors="strict").splitlines():
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split(maxsplit=1)
        if len(parts) == 2 and parts[0].lower() in {"map_kd", "map_basecolor", "map_base_color"}:
            textures.append((path.parent / parts[1].strip()).resolve())
    _require(textures, f"MTL has no base-color texture mapping: {path}")
    return tuple(textures)


def validate_obj(repo: Path, source: Path) -> dict[str, object]:
    policy = _parse_policy(repo)
    stats, mtls = inspect_obj(source)
    textures: set[Path] = set()
    for mtl in mtls:
        _require(mtl.exists(), f"referenced MTL does not exist: {mtl}")
        _require(_is_tracked(repo, mtl), f"referenced MTL is not tracked by Git: {mtl.relative_to(repo)}")
        textures.update(_texture_paths_from_mtl(mtl))
    for texture in textures:
        _require(texture.exists(), f"referenced texture does not exist: {texture}")
        _require(_is_tracked(repo, texture), f"referenced texture is not tracked by Git: {texture.relative_to(repo)}")

    for item in stats:
        lod = item.lod
        _require(item.has_complete_uv0, f"LOD{lod} is missing complete UV0 on one or more face vertices")
        _require(item.has_complete_normals, f"LOD{lod} is missing authored normals on one or more face vertices")
        _require(item.material_names, f"LOD{lod} does not use a material")
        _require(
            policy["MinimumVertices"][lod] <= item.vertices <= policy["VertexBudgets"][lod],
            f"LOD{lod} vertex count {item.vertices} is outside policy range "
            f"{policy['MinimumVertices'][lod]}..{policy['VertexBudgets'][lod]}",
        )
        _require(
            policy["MinimumTriangles"][lod] <= item.triangles <= policy["TriangleBudgets"][lod],
            f"LOD{lod} triangle count {item.triangles} is outside policy range "
            f"{policy['MinimumTriangles'][lod]}..{policy['TriangleBudgets'][lod]}",
        )

    _require(stats[0].triangles > stats[1].triangles > stats[2].triangles, "Hero triangle counts must decrease LOD0 > LOD1 > LOD2")
    return {
        "sourceInspection": "OBJ_STRUCTURAL_PASS",
        "lods": [asdict(item) for item in stats],
        "mtlFiles": sorted(path.relative_to(repo).as_posix() for path in mtls),
        "textureFiles": sorted(path.relative_to(repo).as_posix() for path in textures),
    }


def validate_intake(repo: Path, source: Path) -> dict[str, object]:
    repo = repo.resolve()
    source = source.resolve()
    _require(source.exists() and source.is_file(), f"Hero source does not exist: {source}")
    _require(source.suffix.lower() in SUPPORTED_SUFFIXES, f"unsupported Hero source format: {source.suffix}")
    rel = source.relative_to(repo)
    _require(EXPECTED_ROOT == rel.parent or EXPECTED_ROOT in rel.parents, f"Hero source must be under {EXPECTED_ROOT.as_posix()}")
    lowered = {part.lower() for part in rel.parts}
    forbidden = sorted(lowered & FORBIDDEN_SEGMENTS)
    _require(not forbidden, f"Hero production source uses forbidden path segment: {forbidden[0]}")
    _require(_is_tracked(repo, source), f"Hero source is not tracked by Git: {rel.as_posix()}")

    base: dict[str, object] = {
        "schemaVersion": 1,
        "task": "UART-003",
        "source": rel.as_posix(),
        "verified": False,
        "productionArtApproved": False,
    }
    if source.suffix.lower() == ".obj":
        base.update(validate_obj(repo, source))
        base["verdict"] = "READY_FOR_LICENSED_UNITY_IMPORT"
    else:
        base.update({
            "sourceInspection": "BINARY_OR_DCC_SOURCE_NOT_INSPECTED",
            "verdict": "UNITY_INSPECTION_REQUIRED",
        })
    return base


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Pre-Unity fail-closed intake for a UART-003 Hero source asset.")
    parser.add_argument("--source", required=True, help="Tracked Hero source under unity_game/Assets/Afareet/ArtSource/Vehicles/HeroCar/")
    parser.add_argument("--repo-root", default=".")
    parser.add_argument("--output", help="Optional JSON report; existing files are never overwritten.")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    try:
        repo = _repo_root(Path(args.repo_root).expanduser().resolve())
        result = validate_intake(repo, Path(args.source))
        output = None
        if args.output:
            output = Path(args.output).expanduser().resolve()
            _require(not output.exists(), f"refusing to overwrite existing intake report: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(result, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_UART003_INTAKE "
            f"verdict={result['verdict']} source={result['source']} verified=false"
            + (f" output={output}" if output else "")
        )
        return 0 if result["verdict"] == "READY_FOR_LICENSED_UNITY_IMPORT" else 3
    except (HeroAssetIntakeError, OSError, ValueError, subprocess.SubprocessError) as exc:
        print(f"AFAREET_UART003_INTAKE_ERROR: {exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
