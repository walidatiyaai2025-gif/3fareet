import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE = REPO_ROOT / "docs/assets/01_vehicles/rival_cars_production/RIVAL_DESIGN_PROFILES.json"
MIN_TRIANGLES = (1800, 800, 350)
MAX_TRIANGLES = (16000, 8000, 4000)


class Uart004RivalMeshBudgetTests(unittest.TestCase):
    def test_profile_topology_generates_all_three_lods_inside_production_bands(self):
        pack = json.loads(SOURCE.read_text(encoding="utf-8"))
        self.assertEqual(len(pack["variants"]), 3)
        for variant_index in range(3):
            extra_box_triangles = 84 if variant_index == 0 else 108
            for lod, topology in enumerate(pack["lodTopology"]):
                longitudinal = topology["longitudinalSegments"]
                radial = topology["bodyRadialSegments"]
                wheel = topology["wheelSegments"]
                body = 2 * longitudinal * radial
                cabin = 2 * max(6, longitudinal // 3) * max(6, radial // 2)
                wheels = 16 * wheel
                triangles = body + cabin + wheels + extra_box_triangles
                self.assertGreaterEqual(triangles, MIN_TRIANGLES[lod], (variant_index, lod, triangles))
                self.assertLessEqual(triangles, MAX_TRIANGLES[lod], (variant_index, lod, triangles))

    def test_variants_are_dimensionally_distinct(self):
        pack = json.loads(SOURCE.read_text(encoding="utf-8"))
        signatures = {
            (v["length"], v["width"], v["bodyHeight"], v["roofHeight"], v["rearWingWidth"])
            for v in pack["variants"]
        }
        self.assertEqual(len(signatures), 3)


if __name__ == "__main__":
    unittest.main()
