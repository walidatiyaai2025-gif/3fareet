#!/usr/bin/env python3
"""Author the UART-005 Cairo street-kit mobile LOD1/LOD2 source set.

This tool is deterministic and source-only. It writes tracked OBJ/MTL candidates plus
MOBILE_LOD_MANIFEST.json. It does not claim licensed Unity import, runtime verification,
physical-device performance, or owner/Art Director acceptance.
"""
from __future__ import annotations

import hashlib
import json
import math
from dataclasses import dataclass
from pathlib import Path


ROOT = Path("docs/assets/02_tracks_environments/cairo_street_kit/source")
MANIFEST = Path("docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_MANIFEST.json")


@dataclass(frozen=True)
class Spec:
    key: str
    base: str
    kind: str
    material: str
    texture: str
    lod0_triangles: int


SPECS = (
    Spec("facade-a", "SM_Env_CairoFacade_A", "facade_a", "Facade_Surface", "T_Env_CairoFacade_Surface_BC.png", 204),
    Spec("facade-b", "SM_Env_CairoFacade_B", "facade_b", "Facade_B_Surface", "T_Env_CairoFacade_B_BC.png", 324),
    Spec("facade-c", "SM_Env_CairoFacade_C", "facade_c", "Facade_C_Surface", "T_Env_CairoFacade_C_BC.png", 300),
    Spec("awning-a", "SM_Env_CairoAwning_A", "awning", "Awning_Surface", "T_Env_CairoAwning_Surface_BC.png", 96),
    Spec("awning-b", "SM_Env_CairoAwning_B", "awning_b", "Awning_B_Surface", "T_Env_CairoAwning_B_BC.png", 132),
    Spec("lamp-a", "SM_Prop_CairoLamp_A", "lamp", "Lamp_Surface", "T_Prop_CairoLamp_Surface_BC.png", 112),
    Spec("barrier-a", "SM_Prop_CairoBarrier_A", "barrier", "Barrier_Surface", "T_Prop_CairoBarrier_Surface_BC.png", 92),
    Spec("sign-a", "SM_Prop_CairoSign_A", "sign", "Sign_Surface", "T_Prop_CairoSign_A_BC.png", 120),
    Spec("planter-a", "SM_Prop_CairoPlanter_A", "planter", "Planter_Surface", "T_Prop_CairoPlanter_A_BC.png", 300),
    Spec("crates-a", "SM_Prop_CairoCrateStack_A", "crates", "Crate_Surface", "T_Prop_CairoCrateStack_A_BC.png", 324),
    Spec("cafe-a", "SM_Prop_CairoCafeTable_A", "cafe", "Cafe_Surface", "T_Prop_CairoCafeTable_A_BC.png", 552),
)


class Mesh:
    def __init__(self, name: str):
        self.name = name
        self.vertices: list[tuple[float, float, float]] = []
        self.faces: list[tuple[int, int, int]] = []

    def v(self, x: float, y: float, z: float) -> int:
        self.vertices.append((x, y, z))
        return len(self.vertices)

    def box(self, cx: float, cy: float, cz: float, sx: float, sy: float, sz: float) -> None:
        x0, x1 = cx - sx / 2, cx + sx / 2
        y0, y1 = cy - sy / 2, cy + sy / 2
        z0, z1 = cz - sz / 2, cz + sz / 2
        a, b, c, d, e, f, g, h = [
            self.v(*p)
            for p in (
                (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
                (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1),
            )
        ]
        self.faces += [
            (a, b, c), (a, c, d), (e, g, f), (e, h, g),
            (a, e, f), (a, f, b), (b, f, g), (b, g, c),
            (c, g, h), (c, h, d), (d, h, e), (d, e, a),
        ]

    def frustum(self, cx: float, cz: float, y0: float, y1: float, r0: float, r1: float, segments: int) -> None:
        bot: list[int] = []
        top: list[int] = []
        for i in range(segments):
            angle = 2 * math.pi * i / segments
            bot.append(self.v(cx + r0 * math.cos(angle), y0, cz + r0 * math.sin(angle)))
            top.append(self.v(cx + r1 * math.cos(angle), y1, cz + r1 * math.sin(angle)))
        bc = self.v(cx, y0, cz)
        tc = self.v(cx, y1, cz)
        for i in range(segments):
            j = (i + 1) % segments
            self.faces += [
                (bot[i], bot[j], top[j]),
                (bot[i], top[j], top[i]),
                (bc, bot[j], bot[i]),
                (tc, top[i], top[j]),
            ]

    def cyl(self, cx: float, cz: float, y0: float, y1: float, radius: float, segments: int) -> None:
        self.frustum(cx, cz, y0, y1, radius, radius, segments)


def build_mesh(kind: str, lod: int, name: str) -> Mesh:
    mesh = Mesh(name)
    if kind.startswith("facade"):
        mesh.box(3, 2.5, -.16, 6, 5, .32)
        if lod == 1:
            mesh.box(.22, 2.5, -.38, .22, 5, .14)
            mesh.box(5.78, 2.5, -.38, .22, 5, .14)
            mesh.box(3, 4.78, -.43, 5.7, .20, .16)
            if kind == "facade_a":
                for x in (1.2, 3.0, 4.8):
                    mesh.box(x, 3.35, -.40, .88, 1.20, .12)
                mesh.box(3, 1.05, -.42, 1.15, 1.85, .14)
            elif kind == "facade_b":
                for x in (1.35, 3.0, 4.65):
                    mesh.box(x, 3.30, -.40, 1.0, 1.25, .12)
                mesh.box(3, 2.55, -.50, 4.8, .12, .22)
                mesh.box(3, 1.10, -.42, 1.35, 1.95, .14)
            else:
                for x in (1.0, 2.35, 3.7, 5.0):
                    mesh.box(x, 3.50, -.40, .72, 1.30, .12)
                mesh.box(3, 2.36, -.46, 5.4, .12, .18)
                mesh.box(3, 1.05, -.43, .82, 1.90, .13)
        else:
            mesh.box(3, 4.78, -.42, 5.7, .18, .14)
            mesh.box(3, 1.05, -.42, 1.10, 1.90, .12)
    elif kind in ("awning", "awning_b"):
        mesh.box(1.5, 1.18, .75, 3.0, .16, 1.50)
        if lod == 1:
            mesh.box(.08, .62, .75, .12, 1.20, 1.45)
            mesh.box(2.92, .62, .75, .12, 1.20, 1.45)
            mesh.box(1.5, 1.05, .04, 3.0, .15, .08)
    elif kind == "lamp":
        # LOD0 is 112 triangles. Keep LOD1 strictly below that; the previous
        # 8-segment cylinders produced 112 triangles and broke the monotonic gate.
        segments = 6 if lod == 1 else 5
        mesh.cyl(0, 0, 0, .38, .25, segments)
        mesh.cyl(0, 0, .32, 2.55, .06, segments)
        mesh.box(0, 2.55, 0, .72, .08, .08)
        if lod == 1:
            mesh.box(-.34, 2.50, 0, .20, .16, .18)
            mesh.box(.34, 2.50, 0, .20, .16, .18)
            mesh.box(0, 2.76, 0, .08, .32, .08)
    elif kind == "barrier":
        mesh.box(0, .33, 0, 2.0, .55, .48)
        if lod == 1:
            mesh.box(-.72, .07, 0, .34, .14, .56)
            mesh.box(.72, .07, 0, .34, .14, .56)
    elif kind == "sign":
        mesh.box(.08, -1.28, 1.25, .12, .92, 1.52)
        if lod == 1:
            mesh.box(.08, .02, .55, .14, .14, 1.10)
            mesh.box(.08, -.58, 1.10, .12, .90, .12)
    elif kind == "planter":
        mesh.frustum(0, 0, 0, .68, .42, .51, 10 if lod == 1 else 8)
        if lod == 1:
            mesh.cyl(0, 0, .62, .74, .53, 10)
            mesh.box(-.18, 1.06, 0, .36, .60, .18)
            mesh.box(.18, 1.12, 0, .36, .64, .18)
            mesh.box(0, 1.30, .12, .28, .54, .20)
    elif kind == "crates":
        if lod == 1:
            for args in (
                (0, .28, 0, .92, .54, .70),
                (.12, .83, .05, .86, .50, .66),
                (-.08, 1.34, -.04, .78, .46, .60),
            ):
                mesh.box(*args)
        else:
            mesh.box(0, .30, 0, .92, .58, .70)
            mesh.box(.08, .88, .02, .82, .54, .64)
    elif kind == "cafe":
        mesh.cyl(0, 0, 0, .08, .46, 10 if lod == 1 else 8)
        mesh.cyl(0, 0, .06, .76, .08, 8)
        mesh.cyl(0, 0, .70, .82, .56, 12 if lod == 1 else 8)
        if lod == 1:
            for x in (-.76, .76):
                mesh.cyl(x, 0, 0, .46, .06, 8)
                mesh.cyl(x, 0, .42, .51, .28, 10)
    else:
        raise ValueError(f"unsupported mobile LOD kind: {kind}")
    return mesh


def _sub(a, b):
    return (a[0] - b[0], a[1] - b[1], a[2] - b[2])


def _cross(a, b):
    return (
        a[1] * b[2] - a[2] * b[1],
        a[2] * b[0] - a[0] * b[2],
        a[0] * b[1] - a[1] * b[0],
    )


def _normalize(v):
    length = math.sqrt(v[0] * v[0] + v[1] * v[1] + v[2] * v[2])
    if length <= 1e-12:
        return (0.0, 1.0, 0.0)
    return (v[0] / length, v[1] / length, v[2] / length)


def build_normals(mesh: Mesh):
    accum = [[0.0, 0.0, 0.0] for _ in mesh.vertices]
    for face in mesh.faces:
        origin = mesh.vertices[face[0] - 1]
        b = mesh.vertices[face[1] - 1]
        c = mesh.vertices[face[2] - 1]
        weighted = _cross(_sub(b, origin), _sub(c, origin))
        for index in face:
            bucket = accum[index - 1]
            bucket[0] += weighted[0]
            bucket[1] += weighted[1]
            bucket[2] += weighted[2]
    return tuple(_normalize(value) for value in accum)


def build_uvs(mesh: Mesh):
    mins = [min(vertex[axis] for vertex in mesh.vertices) for axis in range(3)]
    maxs = [max(vertex[axis] for vertex in mesh.vertices) for axis in range(3)]
    extents = [maxs[axis] - mins[axis] for axis in range(3)]
    u_axis, v_axis = sorted(range(3), key=lambda axis: extents[axis], reverse=True)[:2]
    if extents[u_axis] <= 1e-12 or extents[v_axis] <= 1e-12:
        raise ValueError(f"{mesh.name}: cannot build planar UVs from degenerate extents")
    return tuple(
        (
            (vertex[u_axis] - mins[u_axis]) / extents[u_axis],
            (vertex[v_axis] - mins[v_axis]) / extents[v_axis],
        )
        for vertex in mesh.vertices
    )


def write_obj(path: Path, mesh: Mesh, material_file: str, material_name: str) -> None:
    uvs = build_uvs(mesh)
    normals = build_normals(mesh)
    lines = [
        "# UART-005 deterministic mobile LOD authored source",
        f"mtllib {material_file}",
        f"o {mesh.name}",
        "s 1",
    ]
    lines += [f"v {x:.5f} {y:.5f} {z:.5f}" for x, y, z in mesh.vertices]
    lines += [f"vt {u:.6f} {v:.6f}" for u, v in uvs]
    lines += [f"vn {x:.6f} {y:.6f} {z:.6f}" for x, y, z in normals]
    lines += [f"usemtl {material_name}"]
    lines += [f"f {a}/{a}/{a} {b}/{b}/{b} {c}/{c}/{c}" for a, b, c in mesh.faces]
    path.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def validate_obj(path: Path, *, vertices: int, triangles: int, material_file: str, material_name: str) -> None:
    text = path.read_text(encoding="utf-8")
    lines = text.splitlines()
    if sum(line.startswith("v ") for line in lines) != vertices:
        raise ValueError(f"{path.name}: vertex count mismatch")
    if sum(line.startswith("vt ") for line in lines) != vertices:
        raise ValueError(f"{path.name}: incomplete UV0 stream")
    if sum(line.startswith("vn ") for line in lines) != vertices:
        raise ValueError(f"{path.name}: incomplete normal stream")
    if sum(line.startswith("f ") for line in lines) != triangles:
        raise ValueError(f"{path.name}: triangle count mismatch")
    if f"mtllib {material_file}" not in lines:
        raise ValueError(f"{path.name}: missing material dependency")
    if f"usemtl {material_name}" not in lines:
        raise ValueError(f"{path.name}: missing material binding")
    for line in lines:
        if not line.startswith("f "):
            continue
        for token in line.split()[1:]:
            fields = token.split("/")
            if len(fields) != 3 or not all(fields) or len(set(fields)) != 1:
                raise ValueError(f"{path.name}: invalid deterministic v/vt/vn face token {token!r}")


def main() -> int:
    ROOT.mkdir(parents=True, exist_ok=True)
    records = []
    digests: set[str] = set()

    for spec in SPECS:
        mobile_mtl = f"{spec.base}_MobileLOD.mtl"
        lod_records = []
        for lod in (1, 2):
            name = f"{spec.base}_LOD{lod}"
            mesh = build_mesh(spec.kind, lod, name)
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
            if digest in digests:
                raise ValueError(f"{spec.key}: duplicate mobile LOD source hash detected")
            digests.add(digest)
            lod_records.append(
                {
                    "lod": lod,
                    "model": obj.name,
                    "vertices": len(mesh.vertices),
                    "triangles": len(mesh.faces),
                    "sha256": digest,
                }
            )

        (ROOT / mobile_mtl).write_text(
            f"newmtl {spec.material}\n"
            "Ka 0.050000 0.050000 0.050000\n"
            "Kd 0.600000 0.600000 0.600000\n"
            "Ks 0.160000 0.160000 0.160000\n"
            "Ns 28.000000\n"
            "illum 2\n"
            f"map_Kd {spec.texture}\n",
            encoding="utf-8",
            newline="\n",
        )

        if not (spec.lod0_triangles > lod_records[0]["triangles"] > lod_records[1]["triangles"] > 0):
            raise ValueError(
                f"{spec.key}: LOD triangle monotonicity failed: "
                f"{spec.lod0_triangles} -> {lod_records[0]['triangles']} -> {lod_records[1]['triangles']}"
            )

        records.append(
            {
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
        )

    if len(records) != 11 or len(digests) != 22:
        raise ValueError(f"mobile LOD coverage mismatch: modules={len(records)} distinctSources={len(digests)}")

    manifest = {
        "taskId": "UART-005",
        "scope": "mobile-lod-source-authoring",
        "reviewState": "BLOCKED",
        "sourceQuality": "authored-source-candidate",
        "moduleCoverage": "11/11",
        "distinctLodSourceAssets": 22,
        "sameMeshLodReuseAllowed": False,
        "runtimeLodIntegrationImplemented": True,
        "runtimeLodIntegrationVerified": False,
        "modules": records,
        "acceptancePending": [
            "stage and import all 22 distinct mobile LOD source assets in licensed Unity",
            "verify runtime LODGroup binding uses the correct source-backed LOD0/LOD1/LOD2 renderers",
            "verify transition distances and no same-mesh reuse on the exact Android candidate",
            "physical-device performance review for transitions and scene density",
            "owner/Art Director Visual Gate acceptance",
        ],
    }
    MANIFEST.write_text(json.dumps(manifest, indent=2) + "\n", encoding="utf-8", newline="\n")

    for record in records:
        print(
            "AFAREET_UART005_LOD_SOURCE_OK "
            f"key={record['key']} triangles="
            f"{record['lod0Triangles']}/{record['lod1']['triangles']}/{record['lod2']['triangles']}"
        )
    print("AFAREET_UART005_LOD_AUTHOR_OK modules=11 distinctSources=22 runtimeVerified=false")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
