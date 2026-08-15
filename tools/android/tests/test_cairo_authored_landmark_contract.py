import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LANDMARK_ROOT = REPO_ROOT / "docs/assets/03_props_architecture/cairo_landmarks"
SOURCE_ROOT = LANDMARK_ROOT / "source"
MANIFEST = LANDMARK_ROOT / "ASSET_MANIFEST.json"
STAGER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkAssetStager.cs"
PREPROCESSOR = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkBuildPreprocessor.cs"
ADAPTER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredLandmarkKit.cs"
RUNTIME_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoLandmarkRuntimePass.cs"


MODELS = {
    "SM_Landmark_GizaPyramid_A.obj": 40,
    "SM_Landmark_Minaret_A.obj": 100,
    "SM_Landmark_DomeGate_A.obj": 450,
    "SM_Landmark_BridgeGantry_A.obj": 200,
}


def vertex_count(path: Path) -> int:
    return sum(1 for line in path.read_text(encoding="utf-8").splitlines() if line.startswith("v "))


def triangle_count(path: Path) -> int:
    total = 0
    for line in path.read_text(encoding="utf-8").splitlines():
        if not line.startswith("f "):
            continue
        corners = len(line.split()) - 1
        if corners >= 3:
            total += corners - 2
    return total


class CairoAuthoredLandmarkContractTests(unittest.TestCase):
    def test_all_four_landmark_sources_are_authored_nontrivial_meshes(self):
        for name, minimum_vertices in MODELS.items():
            path = SOURCE_ROOT / name
            self.assertTrue(path.is_file(), name)
            self.assertGreaterEqual(vertex_count(path), minimum_vertices, name)
            self.assertGreater(triangle_count(path), 50, name)

    def test_manifest_stays_blocked_until_render_and_owner_acceptance(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("UART-006", manifest["taskId"])
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertTrue(manifest["runtimeIntegrationImplemented"])
        self.assertFalse(manifest["runtimeIntegrated"])
        self.assertFalse(manifest["runtimeIntegrationVerified"])
        self.assertFalse(manifest["proceduralFallbackAllowedInCandidate"])
        self.assertEqual(4, len(manifest["modules"]))

    def test_stager_and_build_preprocessor_package_all_tracked_sources(self):
        stager = STAGER.read_text(encoding="utf-8")
        for name in MODELS:
            self.assertIn(f'"{name}"', stager)
        self.assertIn("Resources.Load<GameObject>(resourcePath)", stager)
        preprocessor = PREPROCESSOR.read_text(encoding="utf-8")
        self.assertIn("IPreprocessBuildWithReport", preprocessor)
        self.assertIn("P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow();", preprocessor)

    def test_runtime_adapter_never_constructs_primitive_geometry(self):
        adapter = ADAPTER.read_text(encoding="utf-8")
        self.assertNotIn("GameObject.CreatePrimitive", adapter)
        for method in ("TryBuildMinarets", "TryBuildDomeGate", "TryBuildPyramidPair", "TryBuildBridgeGantry"):
            self.assertIn(method, adapter)
        self.assertIn("AFAREET_UART006_AUTHORED_LANDMARKS_ACTIVE", adapter)

    def test_player_path_is_fail_closed_and_primitives_are_editor_fallback_only(self):
        runtime = RUNTIME_PASS.read_text(encoding="utf-8")
        self.assertIn("CairoAuthoredLandmarkKit.TryBuildMinarets", runtime)
        self.assertIn("CairoAuthoredLandmarkKit.TryBuildDomeGate", runtime)
        self.assertIn("CairoAuthoredLandmarkKit.TryBuildPyramidPair", runtime)
        self.assertIn("CairoAuthoredLandmarkKit.TryBuildBridgeGantry", runtime)
        self.assertIn("if (Application.isEditor)", runtime)
        self.assertIn("AFAREET_UART006_PLAYER_PRIMITIVE_LANDMARK_FALLBACK_DISABLED", runtime)
        self.assertIn("AFAREET_UART006_PLAYER_AUTHORED_LANDMARK_PASS_ACTIVE", runtime)
        self.assertLess(runtime.index("if (Application.isEditor)"), runtime.index("GameObject.CreatePrimitive"))


if __name__ == "__main__":
    unittest.main()
