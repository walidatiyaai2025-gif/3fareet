#!/usr/bin/env python3
"""Reproduce the complete UART-005 Cairo mobile LOD source set (13 modules / 26 sources).

The original author_uart005_mobile_lods.py owns the first 11 repeated environment/prop
families. Road and curb were added later by the runtime hardening pass, so invoking the
original author alone would rewrite MOBILE_LOD_MANIFEST.json back to 11/11. This wrapper
is the canonical complete authoring entry point: it runs the existing deterministic core,
then deterministically authors road/curb LOD1+LOD2, and finally emits the exact 13/13
manifest without making any licensed-Unity, device, visual-acceptance, or verification claim.
"""
from __future__ import annotations

import contextlib
import hashlib
import io
import json
from dataclasses import dataclass

from author_uart005_mobile_lods import MANIFEST, ROOT, Mesh, main as author_core, validate_obj, write_obj


@dataclass(frozen=True)
class ExtensionSpec:
    key: str
    base: str
    material: str
    texture: str
    lod0_triangles: int


EXTENSIONS = (
    ExtensionSpec(
        "road-a",
        "SM_Track_CairoRoad_A",
        "Road_Surface",
        "T_Track_CairoRoad_Surface_BC.png",
        150,
    ),
    ExtensionSpec(
        "curb-a",
        "SM_Track_CairoCurb_A",
        "Curb_Surface",
        "T_Track_CairoCurb_Surface_BC.png",
        120,
    ),
)


def _build_extension_mesh(key: str, lod: int, name: str) -> Mesh:
    mesh = Mesh(name)
    if key == "road-a":
        if lod == 1:
            mesh.box(0, 0, 0, 13.6, 0.10, 10)
            mesh.box(-6.85, 0.035, 0, 0.30, 0.16, 10)
            mesh.box(6.85, 0.035, 0, 0.30, 0.16, 10)
        elif lod == 2:
            mesh.box(0, 0, 0, 14.0, 0.08, 10)
        else:
            raise ValueError(f"road-a: unsupported LOD {lod}")
        return mesh

    if key == "curb-a":
        if lod == 1:
            mesh.box(0, 0.15, 0, 0.56, 0.30, 10)
            mesh.box(0, 0.34, 0, 0.62, 0.08, 10)
        elif lod == 2:
            mesh.box(0, 0.16, 0, 0.60, 0.32, 10)
        else:
            raise ValueError(f"curb-a: unsupported LOD {lod}")
        return mesh

    raise ValueError(f"unsupported UART-005 mobile LOD extension: {key}")


def _write_mobile_material(path, material: str, texture: str) -> None:
    path.write_text(
        f"newmtl {material}\n"
        "Ka 0.050000 0.050000 0.050000\n"
        "Kd 0.600000 0.600000 0.600000\n"
        "Ks 0.160000 0.160000 0.160000\n"
        "Ns 28.000000\n"
        "illum 2\n"
        f"map_Kd {texture}\n",
        encoding="utf-8",
        newline="\n",
    )


def _author_extension(spec: ExtensionSpec, seen_digests: set[str]) -> dict:
    mobile_mtl = f"{spec.base}_MobileLOD.mtl"
    lod_records = []

    for lod in (1, 2):
        name = f"{spec.base}_LOD{lod}"
        mesh = _build_extension_mesh(spec.key, lod, name)
        obj = ROOT / f"{name}.obj"
        write_obj(obj, mesh, mobile_mtl, spec.material)
        validate_obj(
            obj,
            vertices=len(mesh.vertices),
            triangles=len(mesh.faces),
            material_file=mobile_mtl,
            material_name=spec.material,
        )
        digest = hashlib.sha256(obj.read_bytes()).hexdigest()
        if digest in seen_digests:
            raise ValueError(f"{spec.key}: duplicate mobile LOD source hash detected")
        seen_digests.add(digest)
        lod_records.append(
            {
                "lod": lod,
                "model": obj.name,
                "vertices": len(mesh.vertices),
                "triangles": len(mesh.faces),
                "sha256": digest,
            }
        )

    _write_mobile_material(ROOT / mobile_mtl, spec.material, spec.texture)

    if not (spec.lod0_triangles > lod_records[0]["triangles"] > lod_records[1]["triangles"] > 0):
        raise ValueError(
            f"{spec.key}: LOD triangle monotonicity failed: "
            f"{spec.lod0_triangles} -> {lod_records[0]['triangles']} -> {lod_records[1]['triangles']}"
        )

    return {
        "key": spec.key,
        "lod0Model": f"{spec.base}.obj",
        "lod0Triangles": spec.lod0_triangles,
        "sharedMobileMaterial": mobile_mtl,
        "texture": spec.texture,
        "materialName": spec.material,
        "lod1": lod_records[0],
        "lod2": lod_records[1],
        "sourceState": "STATIC_DISTINCT_MOBILE_LOD_CANDIDATE_PENDING_LICENSED_UNITY_PROOF",
    }


def main() -> int:
    # The core script intentionally still owns its original 11 families. Suppress its
    # terminal 11/11 success marker here so the canonical complete command exposes only
    # the final 13/13 truth to operators and CI logs.
    core_output = io.StringIO()
    with contextlib.redirect_stdout(core_output):
        core_result = author_core()
    if core_result != 0:
        raise RuntimeError(f"UART-005 core mobile LOD authoring failed: {core_output.getvalue()}")

    manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
    records = list(manifest["modules"])
    if len(records) != 11:
        raise ValueError(f"core mobile LOD authoring drifted: expected 11 modules, got {len(records)}")

    seen_digests = {
        record[lod_key]["sha256"]
        for record in records
        for lod_key in ("lod1", "lod2")
    }
    if len(seen_digests) != 22:
        raise ValueError(f"core mobile LOD digest coverage drifted: expected 22, got {len(seen_digests)}")

    for spec in EXTENSIONS:
        records.append(_author_extension(spec, seen_digests))

    keys = [record["key"] for record in records]
    if len(records) != 13 or len(set(keys)) != 13:
        raise ValueError(f"complete mobile LOD module coverage mismatch: modules={len(records)} uniqueKeys={len(set(keys))}")
    if len(seen_digests) != 26:
        raise ValueError(f"complete mobile LOD source coverage mismatch: distinctSources={len(seen_digests)}")
    if {"road-a", "curb-a"} - set(keys):
        raise ValueError("complete mobile LOD manifest is missing road-a or curb-a")

    manifest.update(
        {
            "moduleCoverage": "13/13",
            "distinctLodSourceAssets": 26,
            "sameMeshLodReuseAllowed": False,
            "runtimeLodIntegrationImplemented": True,
            "runtimeLodIntegrationVerified": False,
            "modules": records,
            "acceptancePending": [
                "stage and import all 26 distinct mobile LOD source assets in licensed Unity",
                "verify runtime LODGroup binding uses the correct source-backed LOD0/LOD1/LOD2 renderers including repeated road and curb segments",
                "verify transition distances and no same-mesh reuse on the exact Android candidate",
                "physical-device performance review for transitions and scene density",
                "owner/Art Director Visual Gate acceptance",
            ],
        }
    )
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")

    for record in records[-2:]:
        print(
            "AFAREET_UART005_LOD_EXTENSION_SOURCE_OK "
            f"key={record['key']} triangles="
            f"{record['lod0Triangles']}/{record['lod1']['triangles']}/{record['lod2']['triangles']}"
        )
    print("AFAREET_UART005_LOD_AUTHOR_COMPLETE_OK modules=13 distinctSources=26 runtimeVerified=false")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
