#!/usr/bin/env python3
"""Fail-closed pre-Unity technical intake for an UART-003 Hero production source.

OBJ sources are inspected structurally, including their package-local MTL/texture chain.
FBX/GLB/GLTF/BLEND sources remain explicitly UNITY_INSPECTION_REQUIRED because this
portable standard-library tool must not pretend to inspect opaque DCC/importer formats.

A successful result is source-readiness evidence only. It never proves provenance,
visual acceptance, licensed Unity runtime behavior, device evidence, owner approval,
or UART-003 / UPER-009 completion.
"""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any, Iterable

EXPECTED_TASK = "UART-003"
SUPPORTED_SUFFIXES = {".obj", ".fbx", ".glb", ".gltf", ".blend"}
FORBIDDEN_SEGMENTS = {
    "generated",
    "placeholder",
    "legacyprocedural",
    "preview",
    "refinement",
    "refinementcandidates",
    "blockout",
    "review",
    "reviewpackaging",
}
POLICY_PATH = Path("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs")
EXPECTED_LODS = (0, 1, 2)
BASE_COLOR_DIRECTIVES = {"map_kd", "map_basecolor", "map_base_color"}
BOUNDARY = "TECHNICAL_SOURCE_PREFLIGHT_ONLY_LICENSE_VISUAL_UNITY_DEVICE_OWNER_GATES_REQUIRED"


class HeroHandoffError(RuntimeError):
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
        raise HeroHandoffError(message)


def _run_git(repo: Path, args: list[str]) -> tuple[int, str]:
    process = subprocess.run(
        ["git", "-C", str(repo), *args],
        stdout=subprocess.PIPE,
        stderr=subprocess.PIPE,
        text=True,
        encoding="utf-8",
        errors="replace",
        check=False,
    )
    return process.returncode, process.stdout.strip()


def resolve_repo_root(path: Path) -> Path:
    code, output = _run_git(path, ["rev-parse", "--show-toplevel"])
    _require(code == 0 and bool(output), f"git worktree is required: {path}")
    root = Path(output).resolve()
    _require(root == path.resolve(), f"repo root must be the exact Git worktree root: {root}")
    return root


def normalize_source(source: str) -> str:
    normalized = (source or "").strip().replace("\\", "/")
    while normalized.startswith("./"):
        normalized = normalized[2:]
    if normalized.startswith("Assets/"):
        normalized = "unity_game/" + normalized
    return normalized


def _repo_relative(repo: Path, path: Path, *, label: str) -> str:
    try:
        return path.resolve().relative_to(repo.resolve()).as_posix()
    except ValueError as exc:
        raise HeroHandoffError(f"{label} escapes the Git worktree: {path}") from exc


def _is_tracked(repo: Path, relative_path: str) -> bool:
    code, _ = _run_git(repo, ["ls-files", "--error-unmatch", "--", relative_path])
    return code == 0


def _require_tracked_nonempty(repo: Path, path: Path, *, label: str) -> str:
    relative = _repo_relative(repo, path, label=label)
    _require(path.is_file(), f"{label} is missing: {relative}")
    _require(path.stat().st_size > 0, f"{label} is empty: {relative}")
    _require(_is_tracked(repo, relative), f"{label} is not tracked by Git: {relative}")
    return relative


def _require_file_with_meta(repo: Path, path: Path, *, label: str) -> tuple[str, str]:
    relative = _require_tracked_nonempty(repo, path, label=label)
    meta_path = Path(str(path) + ".meta")
    meta_relative = _require_tracked_nonempty(repo, meta_path, label=f"{label} Unity metadata")
    return relative, meta_relative


def parse_policy(repo: Path) -> dict[str, list[int]]:
    path = repo / POLICY_PATH
    _require_tracked_nonempty(repo, path, label="Hero LOD policy")
    text = path.read_text(encoding="utf-8")
    result: dict[str, list[int]] = {}
    for name in ("MinimumVertices", "VertexBudgets", "MinimumTriangles", "TriangleBudgets"):
        match = re.search(rf"{name}\s*=\s*\{{\s*([^}}]+)\}}", text)
        _require(match is not None, f"cannot parse HeroCarLodPolicy.{name}")
        values = [int(value.strip()) for value in match.group(1).split(",")]
        _require(len(values) == 3, f"HeroCarLodPolicy.{name} must contain exactly three values")
        result[name] = values
    return result


def validate_source_path(repo: Path, source: str) -> tuple[str, Path]:
    normalized = normalize_source(source)
    _require(normalized.startswith("unity_game/Assets/"), "Hero source must be a Unity Assets/ path")
    _require("../" not in normalized and not normalized.endswith("/.."), f"Hero source cannot contain traversal: {normalized}")
    lower = normalized.lower()
    _require("/vehicles/" in lower, f"Hero production source must resolve under a /Vehicles/ role path: {normalized}")
    _require("/rivals/" not in lower, f"Rival production art cannot be reused as the Hero source: {normalized}")
    parts = {part.lower() for part in Path(normalized).parts}
    forbidden = sorted(parts & FORBIDDEN_SEGMENTS)
    _require(not forbidden, f"Hero production source uses forbidden path segment: {forbidden[0] if forbidden else ''}")
    suffix = Path(normalized).suffix.lower()
    _require(suffix in SUPPORTED_SUFFIXES, f"unsupported Hero source format: {suffix or '<none>'}")
    absolute = (repo / normalized).resolve()
    _require_file_with_meta(repo, absolute, label="Hero production source")
    return normalized, absolute


def classify_lod(name: str) -> int | None:
    upper = (name or "").upper()
    matches = [lod for lod in EXPECTED_LODS if f"_LOD{lod}" in upper]
    return matches[0] if len(matches) == 1 else None


def _resolve_obj_index(token: str, count: int) -> int:
    try:
        value = int(token)
    except ValueError as exc:
        raise HeroHandoffError(f"invalid OBJ index: {token}") from exc
    _require(value != 0, "OBJ index 0 is invalid")
    resolved = value - 1 if value > 0 else count + value
    _require(0 <= resolved < count, f"OBJ index out of range: {token}")
    return resolved


def inspect_obj(path: Path) -> tuple[list[LodStats], tuple[str, ...]]:
    vertex_count = 0
    texcoord_count = 0
    normal_count = 0
    current_object = ""
    current_material = ""
    mtllibs: list[str] = []
    unclassified_faces = 0
    groups: dict[int, dict[str, Any]] = {
        lod: {
            "name": "",
            "vertices": set(),
            "triangles": 0,
            "uv_complete": True,
            "normal_complete": True,
            "materials": set(),
        }
        for lod in EXPECTED_LODS
    }

    for line_number, raw in enumerate(path.read_text(encoding="utf-8", errors="strict").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        parts = line.split()
        op = parts[0]
        if op == "v":
            _require(len(parts) >= 4, f"OBJ vertex requires xyz at line {line_number}")
            vertex_count += 1
            continue
        if op == "vt":
            _require(len(parts) >= 3, f"OBJ vt requires uv at line {line_number}")
            texcoord_count += 1
            continue
        if op == "vn":
            _require(len(parts) >= 4, f"OBJ vn requires xyz at line {line_number}")
            normal_count += 1
            continue
        if op in {"o", "g"}:
            current_object = " ".join(parts[1:]).strip()
            current_material = ""
            lod = classify_lod(current_object)
            if lod is not None:
                existing = str(groups[lod]["name"])
                _require(
                    not existing or existing == current_object,
                    f"OBJ must expose exactly one authored object/group for LOD{lod}",
                )
                groups[lod]["name"] = current_object
            continue
        if op == "usemtl":
            current_material = " ".join(parts[1:]).strip()
            continue
        if op == "mtllib":
            reference = line[len("mtllib") :].strip()
            _require(reference, f"empty mtllib at line {line_number}")
            if reference not in mtllibs:
                mtllibs.append(reference)
            continue
        if op != "f":
            continue

        lod = classify_lod(current_object)
        if lod is None:
            unclassified_faces += 1
            continue
        refs = parts[1:]
        _require(len(refs) >= 3, f"LOD{lod} OBJ face has fewer than three vertices at line {line_number}")
        _require(current_material, f"LOD{lod} face appears before usemtl at line {line_number}")
        group = groups[lod]
        for ref in refs:
            components = ref.split("/")
            _require(components and components[0], f"LOD{lod} face is missing vertex index")
            vertex_index = _resolve_obj_index(components[0], vertex_count)
            group["vertices"].add(vertex_index)
            has_uv = len(components) >= 2 and bool(components[1])
            has_normal = len(components) >= 3 and bool(components[2])
            if has_uv:
                _resolve_obj_index(components[1], texcoord_count)
            if has_normal:
                _resolve_obj_index(components[2], normal_count)
            group["uv_complete"] = bool(group["uv_complete"]) and has_uv
            group["normal_complete"] = bool(group["normal_complete"]) and has_normal
        group["triangles"] = int(group["triangles"]) + (len(refs) - 2)
        group["materials"].add(current_material)

    _require(unclassified_faces == 0, f"Hero OBJ contains {unclassified_faces} faces outside explicit _LOD0/_LOD1/_LOD2 objects")
    _require(mtllibs, "Hero OBJ must reference at least one MTL file")

    stats: list[LodStats] = []
    for lod in EXPECTED_LODS:
        group = groups[lod]
        _require(group["name"], f"Hero OBJ is missing an authored _LOD{lod} object/group")
        _require(int(group["triangles"]) > 0, f"Hero LOD{lod} has no faces")
        stats.append(
            LodStats(
                lod=lod,
                object_name=str(group["name"]),
                vertices=len(group["vertices"]),
                triangles=int(group["triangles"]),
                has_complete_uv0=bool(group["uv_complete"]),
                has_complete_normals=bool(group["normal_complete"]),
                material_names=tuple(sorted(group["materials"])),
            )
        )
    return stats, tuple(mtllibs)


def _resolve_package_dependency(package_root: Path, base_dir: Path, reference: str, *, label: str) -> Path:
    normalized = (reference or "").strip().replace("\\", "/")
    _require(normalized, f"{label} reference is empty")
    _require(not normalized.startswith("/"), f"{label} must be package-relative: {reference}")
    _require(re.match(r"^[A-Za-z]:/", normalized) is None, f"{label} cannot use a drive-qualified path: {reference}")
    package = package_root.resolve()
    base = base_dir.resolve()
    _require(base == package or package in base.parents, f"{label} base directory escapes the Hero package: {base}")
    resolved = (base / normalized).resolve()
    _require(resolved == package or package in resolved.parents, f"{label} escapes the Hero handoff package: {reference}")
    return resolved


def _texture_reference(line: str) -> str:
    parts = line.split()
    return parts[-1] if len(parts) >= 2 else ""


def validate_material_dependencies(
    repo: Path,
    source: Path,
    mtllibs: Iterable[str],
    used_materials: set[str],
) -> dict[str, Any]:
    package_root = source.parent.resolve()
    mapped_materials: dict[str, set[str]] = {name: set() for name in used_materials}
    mtl_reports: list[dict[str, Any]] = []
    dependency_files: set[str] = set()

    for reference in mtllibs:
        mtl = _resolve_package_dependency(package_root, source.parent, reference, label=f"{source.name} mtllib")
        mtl_relative, mtl_meta = _require_file_with_meta(repo, mtl, label="Hero MTL dependency")
        dependency_files.update((mtl_relative, mtl_meta))
        current_material = ""
        textures: set[str] = set()
        for raw in mtl.read_text(encoding="utf-8", errors="strict").splitlines():
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            lower = line.lower()
            if lower.startswith("newmtl "):
                current_material = line[len("newmtl ") :].strip()
                continue
            directive = lower.split(maxsplit=1)[0]
            if directive not in BASE_COLOR_DIRECTIVES:
                continue
            _require(current_material, f"{mtl_relative} contains a base-color map before newmtl")
            texture_ref = _texture_reference(line)
            _require(texture_ref, f"{mtl_relative} has an empty texture reference for {current_material}")
            texture = _resolve_package_dependency(package_root, mtl.parent, texture_ref, label=f"{mtl_relative} texture")
            texture_relative, texture_meta = _require_file_with_meta(repo, texture, label="Hero texture dependency")
            dependency_files.update((texture_relative, texture_meta))
            textures.add(texture_ref)
            if current_material in mapped_materials:
                mapped_materials[current_material].add(texture_ref)
        mtl_reports.append(
            {
                "fileName": mtl.relative_to(package_root).as_posix(),
                "textures": sorted(textures),
            }
        )

    missing = sorted(name for name, textures in mapped_materials.items() if not textures)
    _require(not missing, "Hero materials are not base-color texture-mapped by supplied MTL files: " + ", ".join(missing))
    return {
        "materialLibraries": mtl_reports,
        "dependencyFiles": sorted(dependency_files),
        "dependenciesPackageLocal": True,
        "dependenciesTrackedWithMeta": True,
    }


def validate_obj(repo: Path, source: Path) -> dict[str, Any]:
    policy = parse_policy(repo)
    stats, mtllibs = inspect_obj(source)
    used_materials = {material for item in stats for material in item.material_names}
    dependencies = validate_material_dependencies(repo, source, mtllibs, used_materials)

    for item in stats:
        lod = item.lod
        _require(item.has_complete_uv0, f"Hero LOD{lod} is missing complete UV0")
        _require(item.has_complete_normals, f"Hero LOD{lod} is missing authored normals")
        _require(item.material_names, f"Hero LOD{lod} has no material")
        minimum_vertices = policy["MinimumVertices"][lod]
        maximum_vertices = policy["VertexBudgets"][lod]
        minimum_triangles = policy["MinimumTriangles"][lod]
        maximum_triangles = policy["TriangleBudgets"][lod]
        _require(
            minimum_vertices <= item.vertices <= maximum_vertices,
            f"Hero LOD{lod} vertex count {item.vertices} is outside [{minimum_vertices}, {maximum_vertices}]",
        )
        _require(
            minimum_triangles <= item.triangles <= maximum_triangles,
            f"Hero LOD{lod} triangle count {item.triangles} is outside [{minimum_triangles}, {maximum_triangles}]",
        )

    _require(
        stats[0].triangles > stats[1].triangles > stats[2].triangles,
        "Hero triangle counts must decrease LOD0 > LOD1 > LOD2",
    )
    return {
        "sourceInspection": "OBJ_STRUCTURAL_PASS",
        "preUnitySourceEligible": True,
        "unityInspectionRequired": False,
        "lods": [asdict(item) for item in stats],
        **dependencies,
    }


def validate_intake(repo: Path, source: str) -> dict[str, Any]:
    repo = repo.resolve()
    normalized, absolute = validate_source_path(repo, source)
    suffix = absolute.suffix.lower()
    result: dict[str, Any] = {
        "schemaVersion": 2,
        "task": EXPECTED_TASK,
        "source": normalized,
        "extension": suffix,
        "productionGate": False,
        "visualAcceptance": False,
        "ownerApproval": False,
        "provenanceAccepted": False,
        "licensedUnityImportVerified": False,
        "physicalDeviceVerified": False,
        "verified": False,
        "boundary": BOUNDARY,
    }
    if suffix == ".obj":
        result.update(validate_obj(repo, absolute))
        result["verdict"] = "READY_FOR_LICENSED_UNITY_IMPORT"
    else:
        result.update(
            {
                "sourceInspection": "OPAQUE_SOURCE_UNITY_INSPECTION_REQUIRED",
                "preUnitySourceEligible": True,
                "unityInspectionRequired": True,
                "dependenciesPackageLocal": None,
                "dependenciesTrackedWithMeta": None,
                "verdict": "UNITY_INSPECTION_REQUIRED",
            }
        )
    return result


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--repo-root", default=str(Path(__file__).resolve().parents[2]))
    parser.add_argument("--source", required=True, help="Assets/... or unity_game/Assets/... Hero production source")
    parser.add_argument("--output", type=Path, help="Optional non-overwriting JSON report under <repo>/artifacts/")
    return parser


def _write_report(repo: Path, output: Path, report: dict[str, Any]) -> None:
    output = output.resolve()
    artifact_root = (repo / "artifacts").resolve()
    _require(artifact_root == output.parent or artifact_root in output.parents, "--output must stay under <repo>/artifacts/")
    _require(not output.exists(), f"refusing to overwrite existing report: {output}")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(sys.argv[1:] if argv is None else argv)
    try:
        repo = resolve_repo_root(Path(args.repo_root).expanduser().resolve())
        report = validate_intake(repo, args.source)
        if args.output:
            _write_report(repo, args.output, report)
        print(
            "AFAREET_UART003_HERO_HANDOFF_OK "
            f"verdict={report['verdict']} source={report['source']} "
            f"unityInspectionRequired={str(report['unityInspectionRequired']).lower()} verified=false"
        )
        return 0
    except (HeroHandoffError, OSError, UnicodeError, ValueError, subprocess.SubprocessError) as exc:
        print(f"AFAREET_UART003_HERO_HANDOFF_BLOCKED error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
