import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
TOOL_PATH = REPO_ROOT / "tools/android/surface_obj_candidate.py"
SPEC = importlib.util.spec_from_file_location("surface_obj_candidate", TOOL_PATH)
TOOL = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
sys.modules[SPEC.name] = TOOL
SPEC.loader.exec_module(TOOL)


SIMPLE_OBJ = """# unsurfaced candidate
o TestCandidate
v -1 0 -1
v 1 0 -1
v 1 2 1
v -1 2 1
g Body
f 1 2 3
f 1 3 4
"""


class SurfaceObjCandidateTests(unittest.TestCase):
    def test_surface_author_preserves_geometry_and_face_topology(self):
        before = TOOL.parse_obj(SIMPLE_OBJ)
        authored = TOOL.surface_author(
            SIMPLE_OBJ,
            material_file="SM_Test.mtl",
            material_name="Test_Surface",
        )
        after = TOOL.parse_obj(authored)

        self.assertEqual(before.vertices, after.vertices)
        self.assertEqual(
            tuple(face.vertex_indices for face in before.faces),
            tuple(face.vertex_indices for face in after.faces),
        )
        self.assertEqual(2, TOOL.triangle_count(after.faces))
        self.assertIn("mtllib SM_Test.mtl", authored)
        self.assertIn("usemtl Test_Surface", authored)
        self.assertEqual(4, sum(line.startswith("vt ") for line in authored.splitlines()))
        self.assertEqual(4, sum(line.startswith("vn ") for line in authored.splitlines()))
        self.assertIn("f 1/1/1 2/2/2 3/3/3", authored)
        self.assertIn("f 1/1/1 3/3/3 4/4/4", authored)
        TOOL.assert_surface_contract(
            authored,
            material_file="SM_Test.mtl",
            material_name="Test_Surface",
        )

    def test_surface_author_refuses_to_overwrite_existing_authored_streams(self):
        already_authored = SIMPLE_OBJ.replace(
            "g Body",
            "vt 0 0\nvn 0 1 0\ng Body",
        )
        with self.assertRaisesRegex(TOOL.SurfaceAuthoringError, "refusing to overwrite"):
            TOOL.surface_author(
                already_authored,
                material_file="SM_Test.mtl",
                material_name="Test_Surface",
            )

    def test_surface_author_rejects_negative_indices_to_keep_rewrite_deterministic(self):
        negative = SIMPLE_OBJ.replace("f 1 2 3", "f -4 -3 -2")
        with self.assertRaisesRegex(TOOL.SurfaceAuthoringError, "negative/zero"):
            TOOL.surface_author(
                negative,
                material_file="SM_Test.mtl",
                material_name="Test_Surface",
            )

    def test_cli_write_and_check_round_trip(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "SM_Test.obj"
            path.write_text(SIMPLE_OBJ, encoding="utf-8")
            result = TOOL.main(
                [
                    str(path),
                    "--material-file",
                    "SM_Test.mtl",
                    "--material",
                    "Test_Surface",
                    "--write",
                ]
            )
            self.assertEqual(0, result)
            result = TOOL.main(
                [
                    str(path),
                    "--material-file",
                    "SM_Test.mtl",
                    "--material",
                    "Test_Surface",
                    "--check",
                ]
            )
            self.assertEqual(0, result)

    def test_current_dome_and_bridge_are_surfaced_without_topology_loss(self):
        source_root = REPO_ROOT / "docs/assets/03_props_architecture/cairo_landmarks/source"
        cases = (
            ("SM_Landmark_DomeGate_A.obj", "SM_Landmark_DomeGate_A.mtl", "DomeGate_Stone", 474, 738),
            ("SM_Landmark_BridgeGantry_A.obj", "SM_Landmark_BridgeGantry_A.mtl", "BridgeGantry_Metal", 248, 328),
        )
        for name, mtl_file, material, expected_vertices, expected_triangles in cases:
            text = (source_root / name).read_text(encoding="utf-8")
            TOOL.assert_surface_contract(text, material_file=mtl_file, material_name=material)
            geometry = TOOL.parse_obj(text)
            self.assertEqual(expected_vertices, len(geometry.vertices), name)
            self.assertEqual(expected_triangles, TOOL.triangle_count(geometry.faces), name)


if __name__ == "__main__":
    unittest.main()
