import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LANDMARK_ROOT = REPO_ROOT / "docs/assets/03_props_architecture/cairo_landmarks"
SOURCE_ROOT = LANDMARK_ROOT / "source"
MANIFEST = LANDMARK_ROOT / "ASSET_MANIFEST.json"
STAGER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkAssetStager.cs"
PREPROCESSOR = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkBuildPreprocessor.cs"
ANDROID_GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkBuildGate.cs"
ADAPTER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredLandmarkKit.cs"
RUNTIME_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoLandmarkRuntimePass.cs"
TRACK_PYRAMID_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackPyramidAuthoredReplacementPass.cs"


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
        self.assertEqual("replacement-implemented-unverified", manifest["runtimeReplacementStatus"]["trackBuilderPyramids"])
        self.assertFalse(manifest["runtimeReplacementStatus"]["primitiveTrackPyramidFallbackInPlayer"])
        for module in manifest["modules"]:
            self.assertGreater(module["productionMinVertices"], 0)
            self.assertGreater(module["productionMinTriangles"], 0)

    def test_stager_and_build_preprocessor_package_source_and_material_dependencies(self):
        stager = STAGER.read_text(encoding="utf-8")
        for name in MODELS:
            self.assertIn(f'"{name}"', stager)
        for required in ('case ".mtl"', 'case ".png"', 'case ".jpg"', 'case ".tga"', "RemoveStaleStageableFiles"):
            self.assertIn(required, stager)
        self.assertIn("Resources.Load<GameObject>(resourcePath)", stager)
        self.assertIn("companions=mtl-textures", stager)

        preprocessor = PREPROCESSOR.read_text(encoding="utf-8")
        self.assertIn("IPreprocessBuildWithReport", preprocessor)
        self.assertIn("P1ProductionLandmarkAssetStager.StageTrackedSourcesOrThrow();", preprocessor)

    def test_runtime_adapter_never_constructs_primitive_geometry_and_preserves_player_materials(self):
        adapter = ADAPTER.read_text(encoding="utf-8")
        self.assertNotIn("GameObject.CreatePrimitive", adapter)
        for method in ("TryBuildMinarets", "TryBuildDomeGate", "TryBuildPyramidPair", "TryBuildBridgeGantry"):
            self.assertIn(method, adapter)
        self.assertIn("AFAREET_UART006_AUTHORED_LANDMARKS_ACTIVE", adapter)
        self.assertIn("ApplyMaterialsForEditorPreview", adapter)
        self.assertIn("if (!Application.isEditor)", adapter)
        self.assertIn("player-preserves-source-materials=true", adapter)
        self.assertNotIn("private static void ApplyMaterials(GameObject", adapter)

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

    def test_trackbuilder_duplicate_pyramids_have_authored_fail_closed_replacement(self):
        replacement = TRACK_PYRAMID_PASS.read_text(encoding="utf-8")
        self.assertNotIn("GameObject.CreatePrimitive", replacement)
        self.assertIn("SM_Landmark_GizaPyramid_A", replacement)
        self.assertIn('candidate.name == "Giza Spirit Pyramid"', replacement)
        self.assertIn('candidate.name == "Pyramid Spirit Crown"', replacement)
        self.assertIn("AFAREET_UART006_TRACK_PYRAMIDS_REPLACED", replacement)
        self.assertIn("AFAREET_UART006_PLAYER_PRIMITIVE_TRACK_PYRAMID_FALLBACK_DISABLED", replacement)

    def test_android_gate_requires_production_state_geometry_and_real_surface_authoring(self):
        gate = ANDROID_GATE.read_text(encoding="utf-8")
        for required in (
            "IPreprocessBuildWithReport",
            "BuildTarget.Android",
            'ProductionReadyState = "PRODUCTION_READY"',
            'ProductionQuality = "authored-production"',
            "runtimeIntegrationVerified",
            "proceduralFallbackAllowedInCandidate",
            "AFAREET_P1_PRODUCTION_LANDMARK_GATE_BLOCKED",
            "productionMinVertices",
            "productionMinTriangles",
            "TextureCoordinates",
            "Normals",
            "FacesWithUvAndNormal",
            'line.StartsWith("vt ", StringComparison.Ordinal)',
            'line.StartsWith("vn ", StringComparison.Ordinal)',
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
            "authored-surface rejected",
            "imported production surface rejected",
        ):
            self.assertIn(required, gate)


if __name__ == "__main__":
    unittest.main()
