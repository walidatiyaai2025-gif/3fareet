import hashlib
import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source"
MANIFEST = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/ROADSIDE_CLUTTER_MANIFEST.json"


class Uart005RoadsideClutterSourceTests(unittest.TestCase):
    def test_manifest_stays_blocked_and_runtime_unverified(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("UART-005", manifest["taskId"])
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertEqual("3/3", manifest["sourceDeliveryProgress"])
        self.assertTrue(manifest["runtimeIntegrationImplemented"])
        self.assertFalse(manifest["runtimeIntegrated"])
        self.assertFalse(manifest["runtimeIntegrationVerified"])
        self.assertEqual(3, len(manifest["modules"]))

    def test_all_three_clutter_sources_have_valid_tracked_surface_chains(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        digests = set()
        for module in manifest["modules"]:
            with self.subTest(model=module["model"]):
                obj_path = SOURCE_ROOT / module["model"]
                mtl_path = SOURCE_ROOT / module["material"]
                texture_path = SOURCE_ROOT / module["texture"]
                self.assertTrue(obj_path.is_file(), obj_path)
                self.assertTrue(mtl_path.is_file(), mtl_path)
                self.assertTrue(texture_path.is_file(), texture_path)

                obj = obj_path.read_text(encoding="utf-8")
                mtl = mtl_path.read_text(encoding="utf-8")
                self.assertIn(f"mtllib {module['material']}", obj)
                self.assertIn(f"usemtl {module['materialName']}", obj)
                self.assertIn(f"newmtl {module['materialName']}", mtl)
                self.assertIn(f"map_Kd {module['texture']}", mtl)
                self.assertEqual(bytes((137, 80, 78, 71, 13, 10, 26, 10)), texture_path.read_bytes()[:8])

                lines = obj.splitlines()
                vertex_count = sum(line.startswith("v ") for line in lines)
                uv_count = sum(line.startswith("vt ") for line in lines)
                normal_count = sum(line.startswith("vn ") for line in lines)
                triangle_count = sum(max(0, len(line.split()) - 3) for line in lines if line.startswith("f "))
                self.assertEqual(module["currentVertices"], vertex_count)
                self.assertEqual(module["currentTriangles"], triangle_count)
                self.assertGreaterEqual(vertex_count, module["productionMinVertices"])
                self.assertGreaterEqual(triangle_count, module["productionMinTriangles"])
                self.assertGreater(uv_count, 0)
                self.assertGreater(normal_count, 0)

                for face in (line for line in lines if line.startswith("f ")):
                    for token in face.split()[1:]:
                        fields = token.split("/")
                        self.assertEqual(3, len(fields), token)
                        self.assertTrue(all(fields), token)
                        vi, ti, ni = map(int, fields)
                        self.assertGreaterEqual(vi, 1); self.assertLessEqual(vi, vertex_count)
                        self.assertGreaterEqual(ti, 1); self.assertLessEqual(ti, uv_count)
                        self.assertGreaterEqual(ni, 1); self.assertLessEqual(ni, normal_count)

                digests.add(hashlib.sha256(obj_path.read_bytes()).hexdigest())

        self.assertEqual(3, len(digests), "clutter sources must be geometrically distinct")

    def test_clutter_scope_only_lists_real_acceptance_work_as_pending(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        pending = "\n".join(manifest["acceptancePending"])
        self.assertNotIn("stage all three clutter sources", pending)
        self.assertNotIn("deterministic runtime placement without primitive fallback", pending)
        self.assertNotIn("mobile LOD authoring", pending)
        self.assertIn("licensed Unity compile/import/render verification", pending)
        self.assertIn("exact runtime proof", pending)
        self.assertIn("physical-device performance review", pending)
        self.assertIn("owner/Art Director Visual Gate acceptance", pending)


if __name__ == "__main__":
    unittest.main()
