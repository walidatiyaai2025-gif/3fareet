#!/usr/bin/env python3
"""Topology-preserving OBJ surface authoring helper for UART-006 candidates.

The tool intentionally does not add/remove/reorder geometry vertices or faces. It
adds one UV and one accumulated normal per geometric vertex and rewrites face
references to v/vt/vn using the same positive vertex index for all three
streams. A single tracked material dependency can then be attached without
weakening the Android production-art gate.

This is source-preparation tooling only. Running it does not make an asset
PRODUCTION_READY, runtime-verified, owner-approved, or publication-eligible.
"""

from __future__ import annotations

import argparse
import math
import sys
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable, Sequence


class SurfaceAuthoringError(ValueError):
    pass


@dataclass(frozen=True)
class Face:
    line_index: int
    vertex_indices: tuple[int, ...]


@dataclass(frozen=True)
class ObjGeometry:
    lines: tuple[str, ...]
    vertices: tuple[tuple[float, float, float], ...]
    faces: tuple[Face, ...]


def _parse_vertex_index(token: str, vertex_count: int) -> int:
    raw = token.split("/", 1)[0]
    if not raw:
        raise SurfaceAuthoringError(f"face token has no vertex index: {token!r}")
    try:
        index = int(raw)
    except ValueError as exc:
        raise SurfaceAuthoringError(f"invalid face vertex index: {token!r}") from exc
    if index <= 0:
        raise SurfaceAuthoringError(
            "negative/zero OBJ vertex indices are deliberately unsupported so the "
            "rewritten source stays deterministic"
        )
    if index > vertex_count:
        raise SurfaceAuthoringError(
            f"face references vertex {index}, but only {vertex_count} vertices exist"
        )
    return index


def parse_obj(text: str) -> ObjGeometry:
    lines = tuple(text.splitlines())
    vertices: list[tuple[float, float, float]] = []
    raw_faces: list[tuple[int, tuple[str, ...]]] = []

    for line_index, raw in enumerate(lines):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("v "):
            fields = line.split()
            if len(fields) < 4:
                raise SurfaceAuthoringError(f"invalid vertex line: {raw!r}")
            try:
                vertices.append(tuple(float(value) for value in fields[1:4]))
            except ValueError as exc:
                raise SurfaceAuthoringError(f"invalid vertex coordinates: {raw!r}") from exc
        elif line.startswith("f "):
            tokens = tuple(line.split()[1:])
            if len(tokens) < 3:
                raise SurfaceAuthoringError(f"face has fewer than 3 vertices: {raw!r}")
            raw_faces.append((line_index, tokens))

    if not vertices:
        raise SurfaceAuthoringError("OBJ contains no geometry vertices")
    if not raw_faces:
        raise SurfaceAuthoringError("OBJ contains no faces")

    faces = tuple(
        Face(
            line_index=line_index,
            vertex_indices=tuple(
                _parse_vertex_index(token, len(vertices)) for token in tokens
            ),
        )
        for line_index, tokens in raw_faces
    )
    return ObjGeometry(lines=lines, vertices=tuple(vertices), faces=faces)


def triangle_count(faces: Iterable[Face]) -> int:
    return sum(max(0, len(face.vertex_indices) - 2) for face in faces)


def _sub(a: Sequence[float], b: Sequence[float]) -> tuple[float, float, float]:
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _cross(a: Sequence[float], b: Sequence[float]) -> tuple[float, float, float]:
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def _normalize(v: Sequence[float]) -> tuple[float, float, float]:
    length = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])
    if length <= 1e-12:
        return (0.0, 1.0, 0.0)
    return (v[0] / length, v[1] / length, v[2] / length)


def build_vertex_normals(geometry: ObjGeometry) -> tuple[tuple[float, float, float], ...]:
    accum = [[0.0, 0.0, 0.0] for _ in geometry.vertices]

    for face in geometry.faces:
        indices = face.vertex_indices
        origin = geometry.vertices[indices[0] - 1]
        for offset in range(1, len(indices) - 1):
            b = geometry.vertices[indices[offset] - 1]
            c = geometry.vertices[indices[offset + 1] - 1]
            weighted = _cross(_sub(b, origin), _sub(c, origin))
            for index in (indices[0], indices[offset], indices[offset + 1]):
                bucket = accum[index - 1]
                bucket[0] += weighted[0]
                bucket[1] += weighted[1]
                bucket[2] += weighted[2]

    return tuple(_normalize(value) for value in accum)


def build_planar_uvs(geometry: ObjGeometry) -> tuple[tuple[float, float], ...]:
    mins = [min(vertex[axis] for vertex in geometry.vertices) for axis in range(3)]
    maxs = [max(vertex[axis] for vertex in geometry.vertices) for axis in range(3)]
    extents = [maxs[axis] - mins[axis] for axis in range(3)]
    axes = sorted(range(3), key=lambda axis: extents[axis], reverse=True)[:2]
    u_axis, v_axis = axes
    u_extent = extents[u_axis]
    v_extent = extents[v_axis]
    if u_extent <= 1e-12 or v_extent <= 1e-12:
        raise SurfaceAuthoringError("OBJ does not have two non-zero projection extents")

    return tuple(
        (
            (vertex[u_axis] - mins[u_axis]) / u_extent,
            (vertex[v_axis] - mins[v_axis]) / v_extent,
        )
        for vertex in geometry.vertices
    )


def surface_author(
    text: str,
    *,
    material_file: str,
    material_name: str,
) -> str:
    if not material_file or Path(material_file).name != material_file:
        raise SurfaceAuthoringError("material file must be a simple tracked companion filename")
    if not material_name or any(char.isspace() for char in material_name):
        raise SurfaceAuthoringError("material name must be a non-empty token without whitespace")

    geometry = parse_obj(text)
    if any(line.strip().startswith(("vt ", "vn ")) for line in geometry.lines):
        raise SurfaceAuthoringError(
            "OBJ already contains UV/normal streams; refusing to overwrite authored surface data"
        )

    uvs = build_planar_uvs(geometry)
    normals = build_vertex_normals(geometry)
    rewritten_faces = {
        face.line_index: "f "
        + " ".join(f"{index}/{index}/{index}" for index in face.vertex_indices)
        for face in geometry.faces
    }

    output: list[str] = [
        "# UART-006 topology-preserving candidate surface authoring",
        f"mtllib {material_file}",
    ]

    inserted_surface_streams = False
    inserted_material = False
    for line_index, raw in enumerate(geometry.lines):
        stripped = raw.strip()
        if stripped.startswith("mtllib ") or stripped.startswith("usemtl "):
            continue
        if not inserted_surface_streams and stripped.startswith("f "):
            for u, v in uvs:
                output.append(f"vt {u:.6f} {v:.6f}")
            for x, y, z in normals:
                output.append(f"vn {x:.6f} {y:.6f} {z:.6f}")
            inserted_surface_streams = True
        if stripped.startswith("f ") and not inserted_material:
            output.append(f"usemtl {material_name}")
            inserted_material = True
        output.append(rewritten_faces.get(line_index, raw))

    if not inserted_surface_streams or not inserted_material:
        raise SurfaceAuthoringError("internal error: surface declarations were not inserted")

    result = "\n".join(output).rstrip() + "\n"
    authored = parse_obj(result)
    if len(authored.vertices) != len(geometry.vertices):
        raise SurfaceAuthoringError("vertex count changed during surface authoring")
    if tuple(face.vertex_indices for face in authored.faces) != tuple(
        face.vertex_indices for face in geometry.faces
    ):
        raise SurfaceAuthoringError("face topology changed during surface authoring")
    if triangle_count(authored.faces) != triangle_count(geometry.faces):
        raise SurfaceAuthoringError("triangle count changed during surface authoring")
    return result


def assert_surface_contract(text: str, *, material_file: str, material_name: str) -> None:
    geometry = parse_obj(text)
    lines = text.splitlines()
    if f"mtllib {material_file}" not in lines:
        raise SurfaceAuthoringError(f"missing mtllib {material_file}")
    if f"usemtl {material_name}" not in lines:
        raise SurfaceAuthoringError(f"missing usemtl {material_name}")
    vt_count = sum(line.startswith("vt ") for line in lines)
    vn_count = sum(line.startswith("vn ") for line in lines)
    if vt_count < len(geometry.vertices) or vn_count < len(geometry.vertices):
        raise SurfaceAuthoringError(
            f"surface streams incomplete: vertices={len(geometry.vertices)} vt={vt_count} vn={vn_count}"
        )
    for face in geometry.faces:
        for token in lines[face.line_index + 2].split()[1:]:
            # This positional check is intentionally not used for correctness because
            # two declaration lines are injected at the file head. The authoritative
            # token validation below scans every face line directly.
            _ = token
    for raw in lines:
        if not raw.startswith("f "):
            continue
        for token in raw.split()[1:]:
            fields = token.split("/")
            if len(fields) != 3 or not all(fields):
                raise SurfaceAuthoringError(f"face lacks v/vt/vn reference: {token!r}")


def _build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(
        description="Add deterministic UV0/normals/material references to an OBJ without changing its geometry topology."
    )
    parser.add_argument("obj", type=Path)
    parser.add_argument("--material-file", required=True)
    parser.add_argument("--material", required=True)
    mode = parser.add_mutually_exclusive_group(required=True)
    mode.add_argument("--write", action="store_true", help="rewrite the OBJ in place")
    mode.add_argument("--check", action="store_true", help="validate an already surfaced OBJ")
    return parser


def main(argv: Sequence[str] | None = None) -> int:
    args = _build_parser().parse_args(argv)
    try:
        text = args.obj.read_text(encoding="utf-8")
        if args.write:
            authored = surface_author(
                text,
                material_file=args.material_file,
                material_name=args.material,
            )
            args.obj.write_text(authored, encoding="utf-8", newline="\n")
            before = parse_obj(text)
            after = parse_obj(authored)
            print(
                "AFAREET_UART006_SURFACE_AUTHOR_OK "
                f"path={args.obj} vertices={len(after.vertices)} "
                f"triangles={triangle_count(after.faces)} "
                f"topologyPreserved={tuple(f.vertex_indices for f in before.faces) == tuple(f.vertex_indices for f in after.faces)}"
            )
        else:
            assert_surface_contract(
                text,
                material_file=args.material_file,
                material_name=args.material,
            )
            geometry = parse_obj(text)
            print(
                "AFAREET_UART006_SURFACE_CHECK_OK "
                f"path={args.obj} vertices={len(geometry.vertices)} triangles={triangle_count(geometry.faces)}"
            )
    except (OSError, SurfaceAuthoringError) as exc:
        print(f"AFAREET_UART006_SURFACE_BLOCKED reason={exc}", file=sys.stderr)
        return 2
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
