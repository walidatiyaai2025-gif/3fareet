import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MANIFEST_PATH = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json"
ROAD_PATH = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Track_CairoRoad_A.obj"
STAGER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionWorldAssetStager.cs"
ADAPTER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs"
INSTALLER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredRoadInstaller.cs"


def obj_stats(path: Path):
    vertices = 0
    triangles = 0
    texcoords = 0
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if line.startswith("v "):
            vertices += 1
        elif line.startswith("vt "):
            texcoords += 1
        elif line.startswith("f "):
            count = len(line.split()) - 1
            if count >= 3:
                triangles += count - 2
    return vertices, triangles, texcoords


class CairoAuthoredRoadContractTests(unittest.TestCase):
    def test_authored_road_source_clears_registered_floor_and_has_uvs(self):
        manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
        module = next(item for item in manifest["modules"] if item["model"] == ROAD_PATH.name)
        vertices, triangles, texcoords = obj_stats(ROAD_PATH)
        self.assertGreaterEqual(vertices, module["productionMinVertices"])
        self.assertGreaterEqual(triangles, module["productionMinTriangles"])
        self.assertGreater(texcoords, 0)
        self.assertEqual(vertices, module["currentVertices"])
        self.assertEqual(triangles, module["currentTriangles"])

    def test_stager_packages_road_as_unity_resource(self):
        stager = STAGER_PATH.read_text(encoding="utf-8")
        self.assertIn('"SM_Track_CairoRoad_A.obj"', stager)
        self.assertIn("Resources.Load<GameObject>(resourcePath)", stager)

    def test_runtime_adapter_instantiates_authored_road_resource(self):
        adapter = ADAPTER_PATH.read_text(encoding="utf-8")
        self.assertIn('RoadPath = ResourceRoot + "/SM_Track_CairoRoad_A"', adapter)
        self.assertIn("TryCreateRoadSegment", adapter)
        self.assertIn("Object.Instantiate(source, parent, false)", adapter)
        self.assertNotIn("GameObject.CreatePrimitive", adapter)

    def test_player_road_visual_never_restores_primitive_renderer(self):
        installer = INSTALLER_PATH.read_text(encoding="utf-8")
        self.assertIn("primitiveRenderer.enabled = false;", installer)
        self.assertIn("AFAREET_UART005_PLAYER_PRIMITIVE_ROAD_FALLBACK_DISABLED", installer)
        self.assertIn("AFAREET_UART005_AUTHORED_ROAD_ACTIVE", installer)
        self.assertNotIn("GameObject.CreatePrimitive", installer)

    def test_visual_task_remains_blocked_until_real_render_acceptance(self):
        manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertFalse(manifest["runtimeIntegrated"])
        self.assertFalse(manifest["runtimeIntegrationVerified"])
        self.assertEqual("implemented-unverified", manifest["runtimeReplacementStatus"]["authoredRoadVisual"])
        self.assertTrue(manifest["runtimeReplacementStatus"]["trackLayoutStillProcedural"])
        self.assertTrue(manifest["runtimeReplacementStatus"]["landmarksStillProcedural"])


if __name__ == "__main__":
    unittest.main()
