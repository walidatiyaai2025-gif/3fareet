import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
DRESSING_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_track_dressing"
SOURCE_ROOT = DRESSING_ROOT / "source"
MANIFEST = DRESSING_ROOT / "ASSET_MANIFEST.json"
STAGER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingAssetStager.cs"
PREPROCESSOR = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingBuildPreprocessor.cs"
ANDROID_GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingBuildGate.cs"
ADAPTER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredTrackDressing.cs"
RUNTIME_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredTrackDressingRuntimePass.cs"


EXPECTED_MODELS = {
    "SM_Track_FinishGate_A.obj",
    "SM_Track_SpiritRune_A.obj",
    "SM_Track_DesertGround_A.obj",
    "SM_Track_SectorBeacon_A.obj",
}


def obj_stats(path: Path):
    vertices = 0
    triangles = 0
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if line.startswith("v "):
            vertices += 1
        elif line.startswith("f "):
            corners = len(line.split()) - 1
            if corners >= 3:
                triangles += corners - 2
    return vertices, triangles


class CairoAuthoredTrackDressingContractTests(unittest.TestCase):
    def test_manifest_is_fail_closed_and_defines_four_authored_modules(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("UART-007", manifest["taskId"])
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertEqual("authored-source-candidate", manifest["sourceQuality"])
        self.assertTrue(manifest["runtimeIntegrationImplemented"])
        self.assertFalse(manifest["runtimeIntegrated"])
        self.assertFalse(manifest["runtimeIntegrationVerified"])
        self.assertFalse(manifest["proceduralFallbackAllowedInCandidate"])

        models = {module["model"] for module in manifest["modules"]}
        self.assertEqual(EXPECTED_MODELS, models)
        self.assertFalse(manifest["runtimeReplacementStatus"]["primitiveGroundFallbackInPlayer"])
        self.assertFalse(manifest["runtimeReplacementStatus"]["primitiveFinishFallbackInPlayer"])
        self.assertFalse(manifest["runtimeReplacementStatus"]["primitiveRuneFallbackInPlayer"])
        self.assertFalse(manifest["runtimeReplacementStatus"]["primitiveSectorBeaconFallbackInPlayer"])

    def test_sources_clear_manifest_anti_blockout_floors(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        for module in manifest["modules"]:
            source = SOURCE_ROOT / module["model"]
            self.assertTrue(source.is_file(), module["model"])
            vertices, triangles = obj_stats(source)
            self.assertGreaterEqual(vertices, module["productionMinVertices"], module["model"])
            self.assertGreaterEqual(triangles, module["productionMinTriangles"], module["model"])

    def test_stager_packages_models_and_material_dependencies_before_gate(self):
        stager = STAGER.read_text(encoding="utf-8")
        for model in EXPECTED_MODELS:
            self.assertIn(f'"{model}"', stager)
        for required in (
            'case ".mtl"',
            'case ".png"',
            'case ".jpg"',
            'case ".tga"',
            "RemoveStaleStageableFiles",
            "companions=mtl-textures",
        ):
            self.assertIn(required, stager)
        self.assertIn("Resources.Load<GameObject>(path)", stager)
        self.assertIn("ForceSynchronousImport", stager)

        preprocessor = PREPROCESSOR.read_text(encoding="utf-8")
        self.assertIn("IPreprocessBuildWithReport", preprocessor)
        self.assertIn("callbackOrder => -850", preprocessor)
        self.assertIn("P1ProductionTrackDressingAssetStager.StageTrackedSourcesOrThrow();", preprocessor)

        gate = ANDROID_GATE.read_text(encoding="utf-8")
        self.assertIn("callbackOrder => -840", gate)

    def test_runtime_adapter_uses_resources_never_primitives_and_preserves_player_materials(self):
        adapter = ADAPTER.read_text(encoding="utf-8")
        self.assertNotIn("GameObject.CreatePrimitive", adapter)
        for method in ("TryCreateGround", "TryCreateFinishGate", "TryCreateRoadRune", "TryCreateSectorBeacon"):
            self.assertIn(method, adapter)
        for resource in (
            "SM_Track_DesertGround_A",
            "SM_Track_FinishGate_A",
            "SM_Track_SpiritRune_A",
            "SM_Track_SectorBeacon_A",
        ):
            self.assertIn(resource, adapter)
        self.assertIn("AFAREET_UART007_AUTHORED_TRACK_DRESSING_ACTIVE", adapter)
        self.assertIn("ApplyByNameForEditorPreview", adapter)
        self.assertIn("ApplySectorBeaconMaterialsForEditorPreview", adapter)
        self.assertIn("if (!Application.isEditor)", adapter)
        self.assertIn("player-preserves-source-materials=true", adapter)
        self.assertNotIn("private static void ApplyByName(GameObject", adapter)

    def test_runtime_pass_replaces_legacy_visuals_and_player_is_fail_closed(self):
        runtime = RUNTIME_PASS.read_text(encoding="utf-8")
        self.assertNotIn("GameObject.CreatePrimitive", runtime)
        for legacy_name in (
            '"Desert Ground"',
            '"Finish Left"',
            '"Finish Right"',
            '"Finish Beam"',
            '"Finish Spirit Blade"',
            '"Finish Gold Blade"',
            '"Asphalt Spirit Rune"',
            '"Sector Beacon //"',
        ):
            self.assertIn(legacy_name, runtime)
        self.assertIn("SetRenderersEnabled", runtime)
        self.assertIn("AFAREET_UART007_PLAYER_PRIMITIVE_", runtime)
        self.assertIn("AFAREET_UART007_AUTHORED_TRACK_DRESSING_RUNTIME_OK", runtime)

    def test_android_gate_requires_real_production_promotion_verified_runtime_and_surface_authoring(self):
        gate = ANDROID_GATE.read_text(encoding="utf-8")
        for required in (
            "IPreprocessBuildWithReport",
            "BuildTarget.Android",
            'ProductionReadyState = "PRODUCTION_READY"',
            'ProductionQuality = "authored-production"',
            "runtimeIntegrationVerified",
            "proceduralFallbackAllowedInCandidate",
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
            "AFAREET_P1_PRODUCTION_TRACK_DRESSING_GATE_BLOCKED",
            "AFAREET_P1_PRODUCTION_TRACK_DRESSING_GATE_OK",
        ):
            self.assertIn(required, gate)


if __name__ == "__main__":
    unittest.main()
