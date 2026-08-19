import json
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source"
MANIFEST = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json"

SURFACED = (
    ("SM_Track_CairoRoad_A.obj", "SM_Track_CairoRoad_A.mtl", "Road_Surface", "T_Track_CairoRoad_Surface_BC.png"),
    ("SM_Track_CairoCurb_A.obj", "SM_Track_CairoCurb_A.mtl", "Curb_Surface", "T_Track_CairoCurb_Surface_BC.png"),
    ("SM_Env_CairoAwning_A.obj", "SM_Env_CairoAwning_A.mtl", "Awning_Surface", "T_Env_CairoAwning_Surface_BC.png"),
    ("SM_Env_CairoAwning_B.obj", "SM_Env_CairoAwning_B.mtl", "Awning_B_Surface", "T_Env_CairoAwning_B_BC.png"),
    ("SM_Env_CairoFacade_A.obj", "SM_Env_CairoFacade_A.mtl", "Facade_Surface", "T_Env_CairoFacade_Surface_BC.png"),
    ("SM_Env_CairoFacade_B.obj", "SM_Env_CairoFacade_B.mtl", "Facade_B_Surface", "T_Env_CairoFacade_B_BC.png"),
    ("SM_Env_CairoFacade_C.obj", "SM_Env_CairoFacade_C.mtl", "Facade_C_Surface", "T_Env_CairoFacade_C_BC.png"),
    ("SM_Prop_CairoLamp_A.obj", "SM_Prop_CairoLamp_A.mtl", "Lamp_Surface", "T_Prop_CairoLamp_Surface_BC.png"),
    ("SM_Prop_CairoBarrier_A.obj", "SM_Prop_CairoBarrier_A.mtl", "Barrier_Surface", "T_Prop_CairoBarrier_Surface_BC.png"),
    ("SM_Prop_CairoSign_A.obj", "SM_Prop_CairoSign_A.mtl", "Sign_Surface", "T_Prop_CairoSign_A_BC.png"),
)


class Uart005ProductionSurfaceContractTests(unittest.TestCase):
    def _read(self, relative):
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def _bounds(self, model):
        vertices = []
        for line in (SOURCE_ROOT / model).read_text(encoding="utf-8").splitlines():
            if line.startswith("v "):
                _, x, y, z = line.split()
                vertices.append((float(x), float(y), float(z)))
        self.assertTrue(vertices, model)
        return (
            min(v[0] for v in vertices), max(v[0] for v in vertices),
            min(v[1] for v in vertices), max(v[1] for v in vertices),
            min(v[2] for v in vertices), max(v[2] for v in vertices),
        )

    def _counts(self, model):
        lines = (SOURCE_ROOT / model).read_text(encoding="utf-8").splitlines()
        vertices = sum(line.startswith("v ") for line in lines)
        triangles = sum(max(0, len(line.split()) - 3) for line in lines if line.startswith("f "))
        return vertices, triangles

    def _assert_surface_chain(self, model, mtl_file, material, texture):
        obj = (SOURCE_ROOT / model).read_text(encoding="utf-8")
        mtl = (SOURCE_ROOT / mtl_file).read_text(encoding="utf-8")
        self.assertIn(f"mtllib {mtl_file}", obj)
        self.assertIn(f"usemtl {material}", obj)
        self.assertIn("\nvt ", "\n" + obj)
        self.assertIn("\nvn ", "\n" + obj)
        self.assertIn(f"newmtl {material}", mtl)
        self.assertIn(f"map_Kd {texture}", mtl)

        payload = (SOURCE_ROOT / texture).read_bytes()
        self.assertGreater(len(payload), 32)
        self.assertEqual(bytes((137, 80, 78, 71, 13, 10, 26, 10)), payload[:8], texture)

        lines = obj.splitlines()
        vertex_count = sum(line.startswith("v ") for line in lines)
        uv_count = sum(line.startswith("vt ") for line in lines)
        normal_count = sum(line.startswith("vn ") for line in lines)
        # OBJ surface pools may be intentionally deduplicated: one UV/normal can be
        # referenced by many vertices. Completeness is proven by every face token
        # carrying in-range v/vt/vn indices rather than by requiring 1:1 pool sizes.
        self.assertGreater(uv_count, 0, model)
        self.assertGreater(normal_count, 0, model)

        for face in (line for line in lines if line.startswith("f ")):
            for token in face.split()[1:]:
                fields = token.split("/")
                self.assertEqual(3, len(fields), token)
                self.assertTrue(all(fields), token)
                vi, ti, ni = map(int, fields)
                self.assertGreaterEqual(vi, 1, token)
                self.assertLessEqual(vi, vertex_count, token)
                self.assertGreaterEqual(ti, 1, token)
                self.assertLessEqual(ti, uv_count, token)
                self.assertGreaterEqual(ni, 1, token)
                self.assertLessEqual(ni, normal_count, token)

    def test_all_ten_modules_have_tracked_surface_authoring(self):
        for module in SURFACED:
            with self.subTest(model=module[0]):
                self._assert_surface_chain(*module)

    def test_all_ten_sources_clear_manifest_anti_blockout_floors(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual(10, len(manifest["modules"]))
        for module in manifest["modules"]:
            with self.subTest(model=module["model"]):
                vertices, triangles = self._counts(module["model"])
                self.assertEqual(module["currentVertices"], vertices)
                self.assertEqual(module["currentTriangles"], triangles)
                self.assertGreaterEqual(vertices, module["productionMinVertices"])
                self.assertGreaterEqual(triangles, module["productionMinTriangles"])

    def test_geometry_floors_and_key_runtime_envelopes(self):
        road = self._bounds("SM_Track_CairoRoad_A.obj")
        curb = self._bounds("SM_Track_CairoCurb_A.obj")
        lamp = self._bounds("SM_Prop_CairoLamp_A.obj")
        barrier = self._bounds("SM_Prop_CairoBarrier_A.obj")
        self.assertEqual((-7.0, 7.0), (round(road[0], 4), round(road[1], 4)))
        self.assertLessEqual(road[3], 0.22)
        self.assertEqual((-5.0, 5.0), (round(road[4], 4), round(road[5], 4)))
        self.assertGreaterEqual(curb[0], -0.49)
        self.assertLessEqual(curb[1], 0.49)
        self.assertLessEqual(curb[3], 0.43)
        self.assertLessEqual(lamp[1] - lamp[0], 1.0)
        self.assertGreaterEqual(lamp[2], 0.0)
        self.assertLessEqual(lamp[3], 3.05)
        self.assertLessEqual(lamp[5] - lamp[4], 0.40)
        self.assertAlmostEqual(-1.0, barrier[0], places=4)
        self.assertAlmostEqual(1.0, barrier[1], places=4)
        self.assertLessEqual(barrier[3], 0.65)
        self.assertFalse((SOURCE_ROOT / "T_Prop_CairoBarrier_Surface_BC.png.base64.tmp").exists())

    def test_authored_awning_placement_contract_is_preserved(self):
        awning = self._bounds("SM_Env_CairoAwning_A.obj")
        facade = self._bounds("SM_Env_CairoFacade_A.obj")
        self.assertEqual((0.0, 3.0), (round(awning[0], 4), round(awning[1], 4)))
        self.assertEqual((0.0, 1.5), (round(awning[4], 4), round(awning[5], 4)))
        self.assertLess(facade[4], 0.0)
        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        for required in (
            "AuthoredAwningWidth = 3f",
            "placedAwningWidth * .5f",
            "-width * .5f - .06f",
            "canopy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);",
            "new Vector3(awningScaleX, 1f, .78f)",
        ):
            self.assertIn(required, runtime)

    def test_facade_awning_and_sign_expansion_is_distinct_and_registered(self):
        names = (
            "SM_Env_CairoFacade_A.obj",
            "SM_Env_CairoFacade_B.obj",
            "SM_Env_CairoFacade_C.obj",
            "SM_Env_CairoAwning_A.obj",
            "SM_Env_CairoAwning_B.obj",
            "SM_Prop_CairoSign_A.obj",
        )
        payloads = [(SOURCE_ROOT / name).read_bytes() for name in names]
        self.assertEqual(len(payloads), len(set(payloads)))

        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("10/10", manifest["sourceSurfaceProgress"])
        self.assertEqual("3/3", manifest["sourceVariantCoverage"]["facades"])
        self.assertEqual("2/2", manifest["sourceVariantCoverage"]["awnings"])
        self.assertEqual("1/1", manifest["sourceVariantCoverage"]["hangingSigns"])
        expansion = "\n".join(manifest["requiredProductionExpansion"])
        self.assertNotIn("at least three facade/building variants", expansion)
        self.assertNotIn("storefront/sign/awning variants", expansion)
        self.assertNotIn("additional roadside clutter", expansion)
        self.assertNotIn("mobile LOD setup", expansion)
        self.assertIn("normal/ORM", expansion)
        self.assertIn("landmark and skyline replacement", expansion)
        self.assertEqual(
            "implemented-unverified",
            manifest["runtimeReplacementStatus"]["authoredRoadsideClutter"],
        )
        self.assertEqual(
            "implemented-unverified",
            manifest["runtimeReplacementStatus"]["mobileLod13ModulePath"],
        )

    def test_runtime_adapter_uses_all_authored_sources_and_stable_building_variation(self):
        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        for required in (
            "SM_Env_CairoFacade_A",
            "SM_Env_CairoFacade_B",
            "SM_Env_CairoFacade_C",
            "SM_Env_CairoAwning_A",
            "SM_Env_CairoAwning_B",
            "SM_Prop_CairoLamp_A",
            "SM_Prop_CairoBarrier_A",
            "SM_Prop_CairoSign_A",
            "SM_Track_CairoRoad_A",
            "SM_Track_CairoCurb_A",
            "StableVariantIndex",
            "FacadePaths.Length",
            "AwningPaths.Length",
            "AFAREET_UART005_BUILDING_VARIANTS_ACTIVE",
            "facades=3 awnings=2 signs=1",
            "selection=stable-position-hash",
            "playerMaterials=source-authored",
        ):
            self.assertIn(required, runtime)
        self.assertIn("if (facade == null) Missing(facadePath);", runtime)
        self.assertIn("if (awning == null) Missing(awningPath);", runtime)
        self.assertIn("if (sign == null) Missing(SignPath);", runtime)
        self.assertIn("if (!Application.isEditor)", runtime)

    def test_fail_closed_android_surface_and_dependency_contracts_remain(self):
        gate = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldBuildGate.cs")
        for required in (
            "runtimeIntegrationVerified", "TextureCoordinates", "Normals", "FacesWithUvAndNormal",
            'line.StartsWith("vt ", StringComparison.Ordinal)', 'line.StartsWith("vn ", StringComparison.Ordinal)',
            "mesh.uv", "mesh.normals", "material.mainTexture",
            "authored-surface rejected", "imported production surface rejected",
        ):
            self.assertIn(required, gate)

        policy = self._read("unity_game/Assets/Afareet/Editor/ObjProductionMaterialDependencyPolicy.cs")
        for required in (
            'line.StartsWith("mtllib ", StringComparison.Ordinal)',
            'line.StartsWith("usemtl ", StringComparison.Ordinal)',
            'line.StartsWith("newmtl ", StringComparison.Ordinal)',
            "OBJ usemtl is not defined by tracked MTL files",
            "OBJ production material has no tracked texture map",
            "Material dependency escapes tracked source root",
            "Texture dependency escapes tracked source root",
        ):
            self.assertIn(required, policy)

        material_gate = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldMaterialDependencyGate.cs")
        for required in (
            "IPreprocessBuildWithReport", "BuildTarget.Android", '"PRODUCTION_READY"', '"authored-production"',
            "ObjProductionMaterialDependencyPolicy.ValidateOrThrow",
            "AFAREET_UART005_MATERIAL_DEPENDENCY_GATE_BLOCKED",
        ):
            self.assertIn(required, material_gate)

    def test_stager_tracks_all_ten_models_and_surface_companions(self):
        stager = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldAssetStager.cs")
        for model, _, _, _ in SURFACED:
            self.assertIn(f'"{model}"', stager)
        for required in (
            'case ".obj"', 'case ".mtl"', 'case ".png"',
            "RemoveStaleStageableFiles", "Resources.Load<GameObject>",
            "companions=mtl-textures", "models={Models.Length}",
        ):
            self.assertIn(required, stager)

    def test_player_preserves_imported_source_materials(self):
        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        self.assertIn("ApplyNamedMaterialsForEditorPreview", runtime)
        self.assertIn("ApplyMaterialForEditorPreview", runtime)
        self.assertIn("if (!Application.isEditor)", runtime)
        self.assertIn("player-preserves-source-materials=true", runtime)
        self.assertNotIn("private static void ApplyNamedMaterials(", runtime)
        self.assertNotIn("private static void ApplyMaterial(GameObject", runtime)

    def test_runtime_player_fallbacks_for_replaced_geometry_are_disabled(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs")
        for required in (
            "TryCreateRoadSegment",
            "TryCreateBuilding",
            "TryCreateLamp",
            "AFAREET_UART005_PLAYER_PRIMITIVE_ROAD_FALLBACK_DISABLED",
            "AFAREET_UART005_PLAYER_PRIMITIVE_RAIL_FALLBACK_DISABLED",
            "AFAREET_UART005_PLAYER_PRIMITIVE_BUILDING_FALLBACK_DISABLED",
            "AFAREET_UART005_PLAYER_PRIMITIVE_LAMP_FALLBACK_DISABLED",
            "if (!Application.isEditor)",
        ):
            self.assertIn(required, text)

    def test_manifest_stays_blocked_after_ten_source_surface_completion(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertEqual("authored-source-candidate", manifest["sourceQuality"])
        self.assertFalse(manifest["runtimeIntegrated"])
        self.assertFalse(manifest["runtimeIntegrationVerified"])
        self.assertEqual("replacement-required", manifest["atlas"]["status"])
        self.assertEqual(
            10,
            sum(
                module.get("surfaceAuthoring") == "tracked-uv-normal-mtl-texture-candidate"
                for module in manifest["modules"]
            ),
        )


if __name__ == "__main__":
    unittest.main()
