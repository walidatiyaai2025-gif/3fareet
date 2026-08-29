import json
import math
import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
DRESSING_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_track_dressing"
SOURCE_ROOT = DRESSING_ROOT / "source"
MANIFEST = DRESSING_ROOT / "ASSET_MANIFEST.json"
STAGER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingAssetStager.cs"
PREPROCESSOR = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingBuildPreprocessor.cs"
ANDROID_GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingBuildGate.cs"
MATERIAL_POLICY = REPO_ROOT / "unity_game/Assets/Afareet/Editor/ObjProductionMaterialDependencyPolicy.cs"
MATERIAL_GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1ProductionTrackDressingMaterialDependencyGate.cs"
ADAPTER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredTrackDressing.cs"
RUNTIME_PASS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredTrackDressingRuntimePass.cs"
TRACK_BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"


EXPECTED_MODELS = {
    "SM_Track_FinishGate_A.obj",
    "SM_Track_SpiritRune_A.obj",
    "SM_Track_DesertGround_A.obj",
    "SM_Track_SectorBeacon_A.obj",
}

SURFACED = (
    ("SM_Track_FinishGate_A.obj", "SM_Track_FinishGate_A.mtl", "FinishGate_Metal", "T_Track_FinishGate_BC.png"),
    ("SM_Track_SpiritRune_A.obj", "SM_Track_SpiritRune_A.mtl", "SpiritRune_Surface", "T_Track_SpiritRune_BC.png"),
    ("SM_Track_DesertGround_A.obj", "SM_Track_DesertGround_A.mtl", "DesertGround_Sand", "T_Track_DesertGround_BC.png"),
    ("SM_Track_SectorBeacon_A.obj", "SM_Track_SectorBeacon_A.mtl", "SectorBeacon_Metal", "T_Track_SectorBeacon_BC.png"),
)


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

    def _assert_surface_chain(self, model, mtl_file, material, texture):
        obj=(SOURCE_ROOT/model).read_text(encoding="utf-8")
        mtl=(SOURCE_ROOT/mtl_file).read_text(encoding="utf-8")
        self.assertIn(f"mtllib {mtl_file}",obj)
        self.assertIn(f"usemtl {material}",obj)
        self.assertTrue(any(line.startswith("vt ") for line in obj.splitlines()), model)
        self.assertTrue(any(line.startswith("vn ") for line in obj.splitlines()), model)
        self.assertIn(f"newmtl {material}",mtl)
        self.assertIn(f"map_Kd {texture}",mtl)
        self.assertEqual(bytes((137,80,78,71,13,10,26,10)), (SOURCE_ROOT/texture).read_bytes()[:8], texture)
        vertices=sum(1 for line in obj.splitlines() if line.startswith("v "))
        for face in (line for line in obj.splitlines() if line.startswith("f ")):
            for token in face.split()[1:]:
                fields=token.split("/")
                self.assertEqual(3,len(fields),token)
                self.assertTrue(fields[1] and fields[2],token)
                self.assertGreaterEqual(int(fields[0]),1,token)
                self.assertLessEqual(int(fields[0]),vertices,token)

    def test_all_four_sources_have_tracked_surface_chains_and_valid_face_indices(self):
        manifest=json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("4/4",manifest["sourceSurfaceProgress"])
        self.assertEqual("malformed-arch-rebuilt-pylon-trim-preserved",manifest["finishGateTopologyRepair"])
        modules={m["model"]:m for m in manifest["modules"]}
        self.assertEqual(4,sum(m.get("surfaceAuthoring")=="tracked-uv-normal-mtl-texture-candidate" for m in modules.values()))
        for surface in SURFACED:
            with self.subTest(model=surface[0]):
                self._assert_surface_chain(*surface)
                v,t=obj_stats(SOURCE_ROOT/surface[0])
                self.assertEqual(v,modules[surface[0]]["currentVertices"])
                self.assertEqual(t,modules[surface[0]]["currentTriangles"])

    def test_sources_clear_manifest_anti_blockout_floors(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        for module in manifest["modules"]:
            source = SOURCE_ROOT / module["model"]
            self.assertTrue(source.is_file(), module["model"])
            vertices, triangles = obj_stats(source)
            self.assertGreaterEqual(vertices, module["productionMinVertices"], module["model"])
            self.assertGreaterEqual(triangles, module["productionMinTriangles"], module["model"])

    def test_sector_beacon_placement_clears_rail_and_nearest_right_building(self):
        adapter = ADAPTER.read_text(encoding="utf-8")
        track_builder = TRACK_BUILDER.read_text(encoding="utf-8")
        match = re.search(r"SectorBeaconLateralOffset\s*=\s*([0-9.]+)f", adapter)
        self.assertIsNotNone(match)
        beacon_offset = float(match.group(1))

        bounds = obj_vertex_bounds(SOURCE_ROOT / "SM_Track_SectorBeacon_A.obj")
        beacon_half_width = max(abs(bounds[0]), abs(bounds[1]))
        self.assertAlmostEqual(1.225, beacon_half_width, places=3)

        self.assertIn("RoadWidth = 14f", track_builder)
        self.assertIn("RoadWidth * .56f", track_builder)
        self.assertIn("RoadWidth + 8f", track_builder)
        self.assertIn("var width = 5f + (seed * 3 % 6);", track_builder)

        road_width = 14.0
        rail_offset = road_width * 0.56
        right_building_center = road_width + 8.0
        building_half_diagonal = 5.0 * math.sqrt(2.0) / 2.0
        minimum_building_inner_edge = right_building_center - building_half_diagonal

        rail_clearance = (beacon_offset - beacon_half_width) - rail_offset
        building_clearance = minimum_building_inner_edge - (beacon_offset + beacon_half_width)
        self.assertGreaterEqual(rail_clearance, 5.0)
        self.assertGreaterEqual(building_clearance, 2.0)
        self.assertIn("anchor.right * SectorBeaconLateralOffset", adapter)
        self.assertNotIn("anchor.right * 18f", adapter)

    def test_finish_gate_span_matches_fourteen_meter_road(self):
        bounds = obj_vertex_bounds(SOURCE_ROOT / "SM_Track_FinishGate_A.obj")
        half_span = max(abs(bounds[0]), abs(bounds[1]))
        self.assertGreaterEqual(half_span, 7.0)
        self.assertLessEqual(half_span, 8.5)

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

    def test_track_dressing_production_materials_require_tracked_obj_mtl_texture_chain(self):
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
            "AFAREET_UART007_MATERIAL_DEPENDENCY_GATE_BLOCKED",
            "AFAREET_UART007_MATERIAL_DEPENDENCY_GATE_OK",
            "obj-mtllib-usemtl-tracked-textures",
            "rootBound=true",
        ):
            self.assertIn(required, gate)


if __name__ == "__main__":
    unittest.main()
