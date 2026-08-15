import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source"


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
        return sum(line.startswith("v ") for line in lines), sum(line.startswith("f ") for line in lines)

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
        self.assertTrue(payload.startswith(b"\x89PNG\r\n\x1a\n"), texture)
        for face in (line for line in obj.splitlines() if line.startswith("f ")):
            for token in face.split()[1:]:
                fields = token.split("/")
                self.assertEqual(3, len(fields), token)
                self.assertTrue(fields[1] and fields[2], token)

    def test_all_six_modules_have_tracked_surface_authoring(self):
        modules = (
            ("SM_Track_CairoRoad_A.obj", "SM_Track_CairoRoad_A.mtl", "Road_Surface", "T_Track_CairoRoad_Surface_BC.png"),
            ("SM_Track_CairoCurb_A.obj", "SM_Track_CairoCurb_A.mtl", "Curb_Surface", "T_Track_CairoCurb_Surface_BC.png"),
            ("SM_Env_CairoAwning_A.obj", "SM_Env_CairoAwning_A.mtl", "Awning_Surface", "T_Env_CairoAwning_Surface_BC.png"),
            ("SM_Env_CairoFacade_A.obj", "SM_Env_CairoFacade_A.mtl", "Facade_Surface", "T_Env_CairoFacade_Surface_BC.png"),
            ("SM_Prop_CairoLamp_A.obj", "SM_Prop_CairoLamp_A.mtl", "Lamp_Surface", "T_Prop_CairoLamp_Surface_BC.png"),
            ("SM_Prop_CairoBarrier_A.obj", "SM_Prop_CairoBarrier_A.mtl", "Barrier_Surface", "T_Prop_CairoBarrier_Surface_BC.png"),
        )
        for module in modules:
            with self.subTest(model=module[0]):
                self._assert_surface_chain(*module)

    def test_geometry_floors_and_key_runtime_envelopes(self):
        for model, min_v, min_t in (
            ("SM_Prop_CairoLamp_A.obj", 64, 96),
            ("SM_Prop_CairoBarrier_A.obj", 48, 72),
        ):
            v, t = self._counts(model)
            self.assertGreaterEqual(v, min_v)
            self.assertGreaterEqual(t, min_t)

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

        stager = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldAssetStager.cs")
        for required in ('case ".obj"', 'case ".mtl"', 'case ".png"', "RemoveStaleStageableFiles", "companions=mtl-textures"):
            self.assertIn(required, stager)

    def test_player_preserves_imported_source_materials(self):
        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        self.assertIn("ApplyNamedMaterialsForEditorPreview", runtime)
        self.assertIn("ApplyMaterialForEditorPreview", runtime)
        self.assertIn("if (!Application.isEditor)", runtime)
        self.assertIn("player-preserves-source-materials=true", runtime)
        self.assertNotIn("private static void ApplyNamedMaterials(", runtime)
        self.assertNotIn("private static void ApplyMaterial(GameObject", runtime)

    def test_manifest_stays_blocked_after_source_surface_completion(self):
        manifest = self._read("docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json")
        self.assertIn('"reviewState": "BLOCKED"', manifest)
        self.assertIn('"sourceQuality": "authored-source-candidate"', manifest)
        self.assertIn('"runtimeIntegrated": false', manifest)
        self.assertIn('"runtimeIntegrationVerified": false', manifest)
        self.assertIn('"status": "replacement-required"', manifest)
        self.assertEqual(6, manifest.count('"surfaceAuthoring": "tracked-uv-normal-mtl-texture-candidate"'))


if __name__ == "__main__":
    unittest.main()
