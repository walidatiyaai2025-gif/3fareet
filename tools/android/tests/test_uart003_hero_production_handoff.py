import importlib.util
import shutil
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/validate_uart003_hero_production_handoff.py"
SPEC = importlib.util.spec_from_file_location("validate_uart003_hero_production_handoff", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
previous_module = sys.modules.get(SPEC.name)
sys.modules[SPEC.name] = MODULE
try:
    SPEC.loader.exec_module(MODULE)
except BaseException:
    if previous_module is None:
        sys.modules.pop(SPEC.name, None)
    else:
        sys.modules[SPEC.name] = previous_module
    raise

HERO_ROOT = Path("unity_game/Assets/Afareet/ArtSource/Vehicles/Hero")
POLICY = Path("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs")
LOD_VERTICES = (1500, 800, 500)
LOD_TRIANGLES = (3500, 1600, 900)


def git(root: Path, *args: str) -> None:
    subprocess.run(["git", "-C", str(root), *args], check=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, text=True)


def write_policy(root: Path) -> None:
    path = root / POLICY
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(
        """internal static class HeroCarLodPolicy {
internal static readonly int[] MinimumVertices = { 1500, 800, 500 };
internal static readonly int[] VertexBudgets = { 5000, 2800, 1800 };
internal static readonly int[] MinimumTriangles = { 3500, 1600, 900 };
internal static readonly int[] TriangleBudgets = { 7500, 4000, 2500 };
}\n""",
        encoding="utf-8",
    )


def write_valid_obj(root: Path, *, lod1_name="AFAREET_KING_LOD1") -> Path:
    package = root / HERO_ROOT
    materials = package / "materials"
    textures = package / "textures"
    materials.mkdir(parents=True, exist_ok=True)
    textures.mkdir(parents=True, exist_ok=True)
    obj = package / "AfareetKing_Production.obj"
    lines = ["mtllib materials/hero.mtl"]
    total_vertices = sum(LOD_VERTICES)
    for index in range(total_vertices):
        lines.append(f"v {index % 97} {(index // 97) % 31} {index % 13}")
    for index in range(total_vertices):
        lines.append(f"vt {(index % 101) / 100:.2f} {(index % 89) / 88:.2f}")
    lines.append("vn 0 1 0")
    start = 1
    names = ("AFAREET_KING_LOD0", lod1_name, "AFAREET_KING_LOD2")
    for lod, (vertices, triangles) in enumerate(zip(LOD_VERTICES, LOD_TRIANGLES)):
        lines.append(f"o {names[lod]}")
        lines.append(f"usemtl Mat_LOD{lod}")
        for face in range(triangles):
            a = start + (face % vertices)
            b = start + ((face + 1) % vertices)
            c = start + ((face + 2) % vertices)
            lines.append(f"f {a}/{a}/1 {b}/{b}/1 {c}/{c}/1")
        start += vertices
    obj.write_text("\n".join(lines) + "\n", encoding="utf-8")
    Path(str(obj) + ".meta").write_text("guid: heroobj\n", encoding="utf-8")

    mtl = materials / "hero.mtl"
    mtl.write_text(
        "\n".join(
            line
            for lod in range(3)
            for line in (f"newmtl Mat_LOD{lod}", "Kd 1 1 1", "map_Kd ../textures/hero.png")
        ) + "\n",
        encoding="utf-8",
    )
    Path(str(mtl) + ".meta").write_text("guid: heromtl\n", encoding="utf-8")
    texture = textures / "hero.png"
    texture.write_bytes(b"hero-texture")
    Path(str(texture) + ".meta").write_text("guid: herotexture\n", encoding="utf-8")
    return obj


def make_repo(*, obj=True, lod1_name="AFAREET_KING_LOD1") -> Path:
    root = Path(tempfile.mkdtemp(prefix="afareet-hero-handoff-"))
    git(root, "init")
    git(root, "config", "user.email", "ci@example.invalid")
    git(root, "config", "user.name", "CI Fixture")
    write_policy(root)
    if obj:
        write_valid_obj(root, lod1_name=lod1_name)
    else:
        hero = root / HERO_ROOT / "AfareetKing_Production.fbx"
        hero.parent.mkdir(parents=True, exist_ok=True)
        hero.write_bytes(b"opaque-fbx-fixture")
        Path(str(hero) + ".meta").write_text("guid: herofbx\n", encoding="utf-8")
    git(root, "add", ".")
    git(root, "commit", "-m", "fixture")
    return root


class HeroProductionHandoffTests(unittest.TestCase):
    def test_dynamic_module_is_registered_for_dataclass_introspection(self):
        self.assertIs(MODULE, sys.modules.get(SPEC.name))

    def test_valid_obj_passes_dual_budget_and_nested_dependency_preflight(self):
        root = make_repo()
        try:
            report = MODULE.validate_intake(root, (HERO_ROOT / "AfareetKing_Production.obj").as_posix())
            self.assertEqual("READY_FOR_LICENSED_UNITY_IMPORT", report["verdict"])
            self.assertEqual("OBJ_STRUCTURAL_PASS", report["sourceInspection"])
            self.assertFalse(report["unityInspectionRequired"])
            self.assertTrue(report["dependenciesPackageLocal"])
            self.assertTrue(report["dependenciesTrackedWithMeta"])
            self.assertEqual(list(LOD_VERTICES), [item["vertices"] for item in report["lods"]])
            self.assertEqual(list(LOD_TRIANGLES), [item["triangles"] for item in report["lods"]])
            self.assertIn((HERO_ROOT / "materials/hero.mtl.meta").as_posix(), report["dependencyFiles"])
            self.assertIn((HERO_ROOT / "textures/hero.png.meta").as_posix(), report["dependencyFiles"])
            for key in ("productionGate", "visualAcceptance", "ownerApproval", "provenanceAccepted", "licensedUnityImportVerified", "physicalDeviceVerified", "verified"):
                self.assertFalse(report[key], key)
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_lod10_does_not_impersonate_lod1(self):
        self.assertIsNone(MODULE.classify_lod("AFAREET_KING_LOD10"))
        root = make_repo(lod1_name="AFAREET_KING_LOD10")
        try:
            with self.assertRaisesRegex(MODULE.HeroHandoffError, "unclassified|missing an authored _LOD1"):
                MODULE.validate_intake(root, (HERO_ROOT / "AfareetKing_Production.obj").as_posix())
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_missing_texture_meta_blocks_obj_handoff(self):
        root = make_repo()
        try:
            (root / HERO_ROOT / "textures/hero.png.meta").unlink()
            with self.assertRaisesRegex(MODULE.HeroHandoffError, "Hero texture dependency Unity metadata is missing"):
                MODULE.validate_intake(root, (HERO_ROOT / "AfareetKing_Production.obj").as_posix())
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_refinement_path_is_rejected_even_when_tracked(self):
        root = make_repo(obj=False)
        try:
            source = root / HERO_ROOT / "Refinement/AfareetKing_Production.fbx"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_bytes(b"opaque")
            Path(str(source) + ".meta").write_text("guid: refinement\n", encoding="utf-8")
            git(root, "add", ".")
            git(root, "commit", "-m", "refinement")
            with self.assertRaisesRegex(MODULE.HeroHandoffError, "forbidden path segment: refinement"):
                MODULE.validate_intake(root, source.relative_to(root).as_posix())
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_opaque_fbx_is_source_eligible_but_requires_unity_inspection(self):
        root = make_repo(obj=False)
        try:
            report = MODULE.validate_intake(root, (HERO_ROOT / "AfareetKing_Production.fbx").as_posix())
            self.assertEqual("UNITY_INSPECTION_REQUIRED", report["verdict"])
            self.assertTrue(report["preUnitySourceEligible"])
            self.assertTrue(report["unityInspectionRequired"])
            self.assertFalse(report["verified"])
            self.assertFalse(report["licensedUnityImportVerified"])
        finally:
            shutil.rmtree(root, ignore_errors=True)

    def test_missing_source_meta_blocks_all_formats(self):
        root = make_repo(obj=False)
        try:
            (root / HERO_ROOT / "AfareetKing_Production.fbx.meta").unlink()
            with self.assertRaisesRegex(MODULE.HeroHandoffError, "Hero production source Unity metadata is missing"):
                MODULE.validate_intake(root, (HERO_ROOT / "AfareetKing_Production.fbx").as_posix())
        finally:
            shutil.rmtree(root, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
