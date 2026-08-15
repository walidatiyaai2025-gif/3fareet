import json
import math
import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LANDMARK_ROOT = REPO_ROOT / "docs/assets/03_props_architecture/cairo_landmarks"
SOURCE_ROOT = LANDMARK_ROOT / "source"
MANIFEST = LANDMARK_ROOT / "ASSET_MANIFEST.json"
STAGER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkAssetStager.cs"
PREPROCESSOR = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkBuildPreprocessor.cs"
ANDROID_GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkBuildGate.cs"
MATERIAL_POLICY = REPO_ROOT / "unity_game/Assets/Afareet/Editor/ObjProductionMaterialDependencyPolicy.cs"
MATERIAL_GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionLandmarkMaterialDependencyGate.cs"
ADAPTER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredLandmarkKit.cs"
RUNTIME_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoLandmarkRuntimePass.cs"
TRACK_PYRAMID_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackPyramidAuthoredReplacementPass.cs"
TRACK_BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"


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


def obj_vertex_bounds(path: Path):
    vertices = []
    for raw in path.read_text(encoding="utf-8").splitlines():
        line = raw.strip()
        if not line.startswith("v "):
            continue
        fields = line.split()
        vertices.append(tuple(float(value) for value in fields[1:4]))
    if not vertices:
        raise AssertionError(f"OBJ has no vertices: {path}")
    return (
        min(v[0] for v in vertices), max(v[0] for v in vertices),
        min(v[1] for v in vertices), max(v[1] for v in vertices),
        min(v[2] for v in vertices), max(v[2] for v in vertices),
    )


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

    def test_dome_gate_clears_waypoint_36_right_building_footprint(self):
        bounds = obj_vertex_bounds(SOURCE_ROOT / "SM_Landmark_DomeGate_A.obj")
        dome_half_width = max(abs(bounds[0]), abs(bounds[1]))
        self.assertAlmostEqual(6.5, dome_half_width, places=3)

        adapter = ADAPTER.read_text(encoding="utf-8")
        track_builder = TRACK_BUILDER.read_text(encoding="utf-8")
        match = re.search(r"DomeGateLateralOffset\s*=\s*([0-9.]+)f", adapter)
        self.assertIsNotNone(match)
        dome_offset = float(match.group(1))

        self.assertIn("RoadWidth = 14f", track_builder)
        self.assertIn("RoadWidth + 8f", track_builder)
        self.assertIn("var width = 5f + (seed * 3 % 6);", track_builder)
        self.assertIn("seed * 31f", track_builder)

        building_center = 14.0 + 8.0
        building_width = 5.0 + ((36 * 3) % 6)
        building_half_diagonal = building_width * math.sqrt(2.0) / 2.0
        building_outer_edge = building_center + building_half_diagonal
        dome_inner_edge = dome_offset - dome_half_width
        clearance = dome_inner_edge - building_outer_edge

        self.assertGreaterEqual(clearance, 2.0)
        self.assertIn("anchor.right * DomeGateLateralOffset", adapter)
        self.assertNotIn("Root(\"AUTHORED Neon Dome Gate\", anchor, anchor.right * 24f)", adapter)

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

    def test_landmark_production_materials_require_tracked_obj_mtl_texture_chain(self):
        policy = MATERIAL_POLICY.read_text(encoding="utf-8")
        for required in (
            'line.StartsWith("mtllib ", StringComparison.Ordinal)',
            'line.StartsWith("usemtl ", StringComparison.Ordinal)',
            'line.StartsWith("newmtl ", StringComparison.Ordinal)',
            "Production OBJ has no mtllib declaration",
            "Production OBJ has no usemtl assignments",
            "OBJ usemtl is not defined by tracked MTL files",
            "OBJ production material has no tracked texture map",
            "Path.IsPathRooted(reference)",
            "dependency escapes tracked source root",
            "Tracked material dependency is missing",
            "Tracked texture dependency is missing",
        ):
            self.assertIn(required, policy)

        gate = MATERIAL_GATE.read_text(encoding="utf-8")
        for required in (
            "IPreprocessBuildWithReport",
            "BuildTarget.Android",
            '"PRODUCTION_READY"',
            '"authored-production"',
            "ObjProductionMaterialDependencyPolicy.ValidateOrThrow",
            "AFAREET_UART006_MATERIAL_DEPENDENCY_GATE_BLOCKED",
            "AFAREET_UART006_MATERIAL_DEPENDENCY_GATE_OK",
            "obj-mtllib-usemtl-tracked-textures",
            "rootBound=true",
        ):
            self.assertIn(required, gate)


if __name__ == "__main__":
    unittest.main()
