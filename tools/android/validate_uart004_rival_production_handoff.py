#!/usr/bin/env python3
"""Validate a three-Rival production OBJ handoff before licensed Unity staging.

This is a technical source preflight only. A passing report never proves original authorship,
license rights, visual acceptance, Unity import/runtime binding, Android rendering, device proof,
or UART-004 / UPER-009 completion.
"""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import sys
from pathlib import Path
from typing import Any

EXPECTED_TASK = "UART-004"
EXPECTED_VARIANTS = 3
BOUNDARY = "TECHNICAL_SOURCE_PREFLIGHT_ONLY_LICENSE_VISUAL_UNITY_DEVICE_OWNER_GATES_REQUIRED"
PASS_VERDICT = "TECHNICAL_HANDOFF_PASS_NOT_PRODUCTION_ACCEPTANCE"
BLOCKED_VERDICT = "TECHNICAL_HANDOFF_BLOCKED"
TEXTURE_DIRECTIVES = (
    "map_ka", "map_kd", "map_ks", "map_ke", "map_ns", "map_d", "map_bump",
    "bump", "disp", "decal", "norm", "map_pr", "map_pm",
)


class HandoffError(RuntimeError):
    pass


def _require(condition: bool, message: str) -> None:
    if not condition:
        raise HandoffError(message)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def parse_policy(policy_path: Path) -> dict[str, Any]:
    _require(policy_path.is_file(), f"RivalProductionPolicy.cs does not exist: {policy_path}")
    text = policy_path.read_text(encoding="utf-8")
    root_match = re.search(r'ProductionSourceRoot\s*=\s*"([^"]+)"', text)
    _require(root_match is not None, "cannot parse RivalProductionPolicy.ProductionSourceRoot")
    source_names = re.findall(r'ProductionSourceRoot\s*\+\s*"([^"]+_Production\.obj)"', text)
    _require(len(source_names) == EXPECTED_VARIANTS, "RivalProductionPolicy must define exactly three production OBJ exchanges")
    _require(len(set(source_names)) == EXPECTED_VARIANTS, "Rival production OBJ exchange names must be distinct")

    bands: dict[str, list[int]] = {}
    for name in ("MinimumTriangles", "MaximumTriangles"):
        match = re.search(rf"{name}\s*=\s*\{{\s*([^}}]+)\}}", text)
        _require(match is not None, f"cannot parse RivalProductionPolicy.{name}")
        values = [int(value.strip()) for value in match.group(1).split(",")]
        _require(len(values) == EXPECTED_VARIANTS, f"RivalProductionPolicy.{name} must contain exactly three values")
        bands[name] = values

    return {
        "productionSourceRoot": root_match.group(1),
        "sourceFileNames": source_names,
        **bands,
    }


def resolve_lod_from_name(name: str, lod_count: int = EXPECTED_VARIANTS) -> int:
    if not name or not name.strip():
        return -1
    upper = name.upper()
    for lod in range(lod_count):
        token = f"_LOD{lod}"
        search_from = 0
        while search_from < len(upper):
            index = upper.find(token, search_from)
            if index < 0:
                break
            suffix_end = index + len(token)
            if suffix_end == len(upper) or not upper[suffix_end].isdigit():
                return lod
            search_from = suffix_end
    return -1


def _face_has_uv_and_normal(token: str) -> bool:
    fields = token.split("/")
    return len(fields) >= 3 and bool(fields[0]) and bool(fields[1]) and bool(fields[2])


def parse_obj(path: Path, policy: dict[str, Any], variant: int) -> dict[str, Any]:
    _require(path.is_file(), f"Rival {variant + 1} OBJ does not exist: {path}")
    expected_name = policy["sourceFileNames"][variant]
    _require(path.name == expected_name, f"Rival {variant + 1} file name must be {expected_name}, got {path.name}")

    lod_count = len(policy["MinimumTriangles"])
    triangles = [0] * lod_count
    object_names: list[str | None] = [None] * lod_count
    material_names: list[list[str]] = [[] for _ in range(lod_count)]
    mtllibs: list[str] = []
    seen_objects = [False] * lod_count
    current_lod = -1
    current_material = ""
    source_vertices = source_uvs = source_normals = face_count = 0
    unclassified_faces = faces_missing_uv_or_normal = faces_without_material = 0

    for line_number, raw in enumerate(path.read_text(encoding="utf-8", errors="strict").splitlines(), start=1):
        line = raw.strip()
        if not line or line.startswith("#"):
            continue
        if line.startswith("v "):
            source_vertices += 1
            continue
        if line.startswith("vt "):
            source_uvs += 1
            continue
        if line.startswith("vn "):
            source_normals += 1
            continue
        if line.startswith("mtllib "):
            library = line[len("mtllib ") :].strip()
            if library and library not in mtllibs:
                mtllibs.append(library)
            continue
        if line.startswith("o "):
            current_object = line[2:].strip()
            current_lod = resolve_lod_from_name(current_object, lod_count)
            current_material = ""
            if current_lod >= 0:
                _require(not seen_objects[current_lod], f"Rival {variant + 1} defines more than one authored object for LOD{current_lod}")
                seen_objects[current_lod] = True
                object_names[current_lod] = current_object
            continue
        if line.startswith("usemtl "):
            current_material = line[len("usemtl ") :].strip()
            if current_lod >= 0 and current_material and current_material not in material_names[current_lod]:
                material_names[current_lod].append(current_material)
            continue
        if not line.startswith("f "):
            continue

        face_count += 1
        vertices = line.split()[1:]
        _require(len(vertices) >= 3, f"Rival {variant + 1} invalid face at line {line_number}")
        if current_lod < 0:
            unclassified_faces += 1
            continue
        if not current_material:
            faces_without_material += 1
        if any(not _face_has_uv_and_normal(token) for token in vertices):
            faces_missing_uv_or_normal += 1
        triangles[current_lod] += len(vertices) - 2

    _require(source_vertices > 0, f"Rival {variant + 1} source has no vertices")
    _require(source_uvs > 0, f"Rival {variant + 1} source has no UV coordinates")
    _require(source_normals > 0, f"Rival {variant + 1} source has no normals")
    _require(face_count > 0, f"Rival {variant + 1} source has no faces")
    _require(unclassified_faces == 0, f"Rival {variant + 1} contains {unclassified_faces} faces outside explicit _LOD0/_LOD1/_LOD2 objects")
    _require(faces_missing_uv_or_normal == 0, f"Rival {variant + 1} contains {faces_missing_uv_or_normal} faces without both vt and vn indices")
    _require(faces_without_material == 0, f"Rival {variant + 1} contains {faces_without_material} faces before a usemtl assignment")
    _require(mtllibs, f"Rival {variant + 1} must declare at least one mtllib")

    lods = []
    for lod in range(lod_count):
        _require(seen_objects[lod], f"Rival {variant + 1} is missing an authored _LOD{lod} object")
        _require(triangles[lod] > 0, f"Rival {variant + 1} LOD{lod} has no triangle signature")
        _require(material_names[lod], f"Rival {variant + 1} LOD{lod} has no usemtl signature")
        minimum = policy["MinimumTriangles"][lod]
        maximum = policy["MaximumTriangles"][lod]
        _require(minimum <= triangles[lod] <= maximum,
                 f"Rival {variant + 1} LOD{lod} triangle count {triangles[lod]} is outside [{minimum}, {maximum}]")
        for other in range(lod):
            _require(triangles[other] != triangles[lod],
                     f"Rival {variant + 1} LOD{other}/LOD{lod} triangle signatures are ambiguous at {triangles[lod]}")
        lods.append({
            "lod": lod,
            "objectName": object_names[lod],
            "triangles": triangles[lod],
            "minimumTriangles": minimum,
            "maximumTriangles": maximum,
            "materials": material_names[lod],
        })

    return {
        "variant": variant + 1,
        "fileName": path.name,
        "sizeBytes": path.stat().st_size,
        "sha256": sha256_file(path),
        "sourceVertices": source_vertices,
        "sourceUvCoordinates": source_uvs,
        "sourceNormals": source_normals,
        "mtllibs": mtllibs,
        "lods": lods,
    }


def _parse_texture_reference(line: str) -> str:
    parts = line.split()
    return parts[-1] if len(parts) >= 2 else ""


def _resolve_local_dependency(package_root: Path, base_dir: Path, reference: str, *, label: str) -> Path:
    """Resolve a package-local dependency using the format's actual reference base.

    OBJ mtllib references are relative to the OBJ/package directory. Texture-map references
    are relative to the MTL directory. In both cases the final resolved file must remain inside
    the submitted package root.
    """
    _require(bool(reference and reference.strip()), f"{label} reference is empty")
    normalized = reference.strip().replace("\\", "/")
    _require(not normalized.startswith("/"), f"{label} must be package-relative: {reference}")
    _require(re.match(r"^[A-Za-z]:/", normalized) is None, f"{label} must not use a drive-qualified path: {reference}")

    package_root = package_root.resolve()
    base_dir = base_dir.resolve()
    _require(base_dir == package_root or package_root in base_dir.parents,
             f"{label} base directory escapes the handoff package root: {base_dir}")
    resolved = (base_dir / normalized).resolve()
    _require(resolved == package_root or package_root in resolved.parents,
             f"{label} escapes the handoff package root: {reference}")
    return resolved


def validate_mtl_and_textures(obj_path: Path, obj_result: dict[str, Any]) -> list[dict[str, Any]]:
    required_materials = {material for lod in obj_result["lods"] for material in lod["materials"]}
    material_maps: dict[str, set[str]] = {name: set() for name in required_materials}
    library_reports = []
    package_root = obj_path.parent.resolve()

    for library in obj_result["mtllibs"]:
        mtl_path = _resolve_local_dependency(package_root, obj_path.parent, library, label=f"{obj_path.name} mtllib")
        _require(mtl_path.is_file(), f"{obj_path.name} references missing MTL: {library}")
        current_material = ""
        textures: set[str] = set()
        for raw in mtl_path.read_text(encoding="utf-8", errors="strict").splitlines():
            line = raw.strip()
            if not line or line.startswith("#"):
                continue
            lower = line.lower()
            if lower.startswith("newmtl "):
                current_material = line[len("newmtl ") :].strip()
                continue
            directive = lower.split(maxsplit=1)[0]
            if directive not in TEXTURE_DIRECTIVES or not current_material:
                continue
            texture_ref = _parse_texture_reference(line)
            _require(texture_ref, f"{mtl_path.name} has an empty texture reference for {current_material}")
            texture_path = _resolve_local_dependency(package_root, mtl_path.parent, texture_ref, label=f"{mtl_path.name} texture")
            _require(texture_path.is_file(), f"{mtl_path.name} material {current_material} references missing texture: {texture_ref}")
            textures.add(texture_ref)
            if current_material in material_maps:
                material_maps[current_material].add(texture_ref)
        library_reports.append({
            "fileName": str(mtl_path.relative_to(package_root)).replace("\\", "/"),
            "sha256": sha256_file(mtl_path),
            "textures": sorted(textures),
        })

    missing = sorted(name for name, maps in material_maps.items() if not maps)
    _require(not missing, f"{obj_path.name} materials are not texture-mapped in supplied MTL files: {', '.join(missing)}")
    return library_reports


def validate_handoff(paths: list[Path], policy_path: Path) -> dict[str, Any]:
    _require(len(paths) == EXPECTED_VARIANTS, "exactly three Rival OBJ paths are required")
    policy = parse_policy(policy_path)
    variants = []
    hashes: set[str] = set()

    for variant, path in enumerate(paths):
        result = parse_obj(path.resolve(), policy, variant)
        _require(result["sha256"] not in hashes, f"Rival {variant + 1} reuses identical OBJ bytes from another variant")
        hashes.add(result["sha256"])
        result["materialLibraries"] = validate_mtl_and_textures(path.resolve(), result)
        variants.append(result)

    return {
        "schemaVersion": 2,
        "task": EXPECTED_TASK,
        "verdict": PASS_VERDICT,
        "technicalPreflightPassed": True,
        "productionSourceRoot": policy["productionSourceRoot"],
        "variants": variants,
        "distinctSourceHashes": len(hashes),
        "dependenciesPackageLocal": True,
        "productionGate": False,
        "visualAcceptance": False,
        "ownerApproval": False,
        "provenanceAccepted": False,
        "licensedUnityImportVerified": False,
        "physicalDeviceVerified": False,
        "verified": False,
        "boundary": BOUNDARY,
    }


def default_policy_path() -> Path:
    return Path(__file__).resolve().parents[2] / "unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs"


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--rival-01", type=Path, required=True)
    parser.add_argument("--rival-02", type=Path, required=True)
    parser.add_argument("--rival-03", type=Path, required=True)
    parser.add_argument("--policy", type=Path, default=default_policy_path())
    parser.add_argument("--output", type=Path, help="Optional JSON report; existing files are never overwritten")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(sys.argv[1:] if argv is None else argv)
    try:
        report = validate_handoff([args.rival_01, args.rival_02, args.rival_03], args.policy)
        if args.output:
            output = args.output.resolve()
            _require(not output.exists(), f"refusing to overwrite existing report: {output}")
            output.parent.mkdir(parents=True, exist_ok=True)
            output.write_text(json.dumps(report, indent=2, sort_keys=True) + "\n", encoding="utf-8")
        print(
            "AFAREET_UART004_RIVAL_HANDOFF_OK "
            f"verdict={PASS_VERDICT} distinctSources={report['distinctSourceHashes']} "
            "dependenciesPackageLocal=true productionGate=false verified=false"
        )
        return 0
    except (HandoffError, OSError, UnicodeError, ValueError) as exc:
        print(f"AFAREET_UART004_RIVAL_HANDOFF_BLOCKED verdict={BLOCKED_VERDICT} error={exc}", file=sys.stderr)
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
