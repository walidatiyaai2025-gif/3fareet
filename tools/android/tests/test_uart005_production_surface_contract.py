import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source"


class Uart005ProductionSurfaceContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def _obj_bounds(self, relative: str):
        vertices = []
        for raw in self._read(relative).splitlines():
            line = raw.strip()
            if not line.startswith("v "):
                continue
            fields = line.split()
            vertices.append(tuple(float(value) for value in fields[1:4]))
        self.assertTrue(vertices, relative)
        return (
            min(v[0] for v in vertices), max(v[0] for v in vertices),
            min(v[1] for v in vertices), max(v[1] for v in vertices),
            min(v[2] for v in vertices), max(v[2] for v in vertices),
        )

    def _assert_surface_chain(self, model: str, material_file: str, material: str, texture: str):
        obj = (SOURCE_ROOT / model).read_text(encoding="utf-8")
        mtl = (SOURCE_ROOT / material_file).read_text(encoding="utf-8")
        self.assertIn(f"mtllib {material_file}", obj)
        self.assertIn(f"usemtl {material}", obj)
        self.assertIn("\nvt ", "\n" + obj)
        self.assertIn("\nvn ", "\n" + obj)
        self.assertIn(f"newmtl {material}", mtl)
        self.assertIn(f"map_Kd {texture}", mtl)
        self.assertTrue((SOURCE_ROOT / texture).is_file())

        faces = [line.strip() for line in obj.splitlines() if line.strip().startswith("f ")]
        self.assertTrue(faces, model)
        for face in faces:
            for token in face.split()[1:]:
                indices = token.split("/")
                self.assertGreaterEqual(len(indices), 3, token)
                self.assertTrue(indices[1], token)
                self.assertTrue(indices[2], token)

    def test_road_curb_awning_and_facade_have_tracked_surface_authoring(self):
        self._assert_surface_chain(
            "SM_Track_CairoRoad_A.obj",
            "SM_Track_CairoRoad_A.mtl",
            "Road_Surface",
            "T_Track_CairoRoad_Surface_BC.png",
        )
        self._assert_surface_chain(
            "SM_Track_CairoCurb_A.obj",
            "SM_Track_CairoCurb_A.mtl",
            "Curb_Surface",
            "T_Track_CairoCurb_Surface_BC.png",
        )
        self._assert_surface_chain(
            "SM_Env_CairoAwning_A.obj",
            "SM_Env_CairoAwning_A.mtl",
            "Awning_Surface",
            "T_Env_CairoAwning_Surface_BC.png",
        )
        self._assert_surface_chain(
            "SM_Env_CairoFacade_A.obj",
            "SM_Env_CairoFacade_A.mtl",
            "Facade_Surface",
            "T_Env_CairoFacade_Surface_BC.png",
        )

    def test_road_and_curb_stay_inside_manifest_geometry_envelopes(self):
        road = self._obj_bounds(
            "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Track_CairoRoad_A.obj"
        )
        curb = self._obj_bounds(
            "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Track_CairoCurb_A.obj"
        )
        self.assertAlmostEqual(-7.0, road[0], places=4)
        self.assertAlmostEqual(7.0, road[1], places=4)
        self.assertGreaterEqual(road[2], 0.0)
        self.assertLessEqual(road[3], 0.22)
        self.assertAlmostEqual(-5.0, road[4], places=4)
        self.assertAlmostEqual(5.0, road[5], places=4)
        self.assertGreaterEqual(curb[0], -0.49)
        self.assertLessEqual(curb[1], 0.49)
        self.assertGreaterEqual(curb[2], 0.0)
        self.assertLessEqual(curb[3], 0.43)
        self.assertAlmostEqual(-5.0, curb[4], places=4)
        self.assertAlmostEqual(5.0, curb[5], places=4)

    def test_remaining_lamp_source_still_blocks_full_surface_promotion(self):
        lamp = self._read(
            "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Prop_CairoLamp_A.obj"
        )
        self.assertIn("v ", lamp)
        self.assertIn("f ", lamp)
        self.assertNotIn("\nvn ", "\n" + lamp)
        self.assertNotIn("\nmtllib ", "\n" + lamp)

    def test_authored_awning_projects_outward_from_front_facade_and_stays_centered(self):
        awning_path = "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Env_CairoAwning_A.obj"
        facade_path = "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Env_CairoFacade_A.obj"
        awning = self._obj_bounds(awning_path)
        facade = self._obj_bounds(facade_path)

        self.assertAlmostEqual(0.0, awning[4], places=4)
        self.assertAlmostEqual(1.5, awning[5], places=4)
        self.assertAlmostEqual(0.0, awning[0], places=4)
        self.assertAlmostEqual(3.0, awning[1], places=4)
        self.assertLess(facade[4], 0.0)

        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        self.assertIn("AuthoredAwningWidth = 3f", runtime)
        self.assertIn("placedAwningWidth * .5f", runtime)
        self.assertIn("-width * .5f - .06f", runtime)
        self.assertIn("canopy.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);", runtime)
        self.assertIn("new Vector3(awningScaleX, 1f, .78f)", runtime)
        self.assertNotIn("new Vector3(-Mathf.Min(width * .32f, 1.5f), 1.55f", runtime)

    def test_android_gate_requires_verified_runtime_and_authored_surface_data(self):
        gate = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldBuildGate.cs")
        for required in (
            "runtimeIntegrationVerified",
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

    def test_stager_copies_material_and_texture_companions(self):
        stager = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldAssetStager.cs")
        for required in (
            'case ".obj"',
            'case ".mtl"',
            'case ".png"',
            'case ".jpg"',
            'case ".tga"',
            "RemoveStaleStageableFiles",
            "companions=mtl-textures",
        ):
            self.assertIn(required, stager)

    def test_production_obj_material_dependencies_are_tracked_and_root_bound(self):
        policy = self._read("unity_game/Assets/Afareet/Editor/ObjProductionMaterialDependencyPolicy.cs")
        for required in (
            'line.StartsWith("mtllib ", StringComparison.Ordinal)',
            'line.StartsWith("usemtl ", StringComparison.Ordinal)',
            'line.StartsWith("newmtl ", StringComparison.Ordinal)',
            "Production OBJ has no mtllib declaration",
            "Production OBJ has no usemtl assignments",
            "OBJ usemtl is not defined by tracked MTL files",
            "OBJ production material has no tracked texture map",
            "Path.IsPathRooted(reference)",
            "Material dependency escapes tracked source root",
            "Texture dependency escapes tracked source root",
            "Tracked material dependency is missing",
            "Tracked texture dependency is missing",
            'case ".png"',
            'case ".jpg"',
            'case ".tga"',
        ):
            self.assertIn(required, policy)

    def test_android_build_pipeline_runs_material_dependency_gate_for_production_promotion(self):
        gate = self._read("unity_game/Assets/Afareet/Editor/P1ProductionWorldMaterialDependencyGate.cs")
        for required in (
            "IPreprocessBuildWithReport",
            "BuildTarget.Android",
            '"PRODUCTION_READY"',
            '"authored-production"',
            "ObjProductionMaterialDependencyPolicy.ValidateOrThrow",
            "AFAREET_UART005_MATERIAL_DEPENDENCY_GATE_BLOCKED",
            "AFAREET_UART005_MATERIAL_DEPENDENCY_GATE_OK",
            "obj-mtllib-usemtl-tracked-textures",
        ):
            self.assertIn(required, gate)

    def test_player_preserves_imported_source_materials(self):
        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        self.assertIn("ApplyNamedMaterialsForEditorPreview", runtime)
        self.assertIn("ApplyMaterialForEditorPreview", runtime)
        self.assertIn("if (!Application.isEditor)", runtime)
        self.assertIn("player-preserves-source-materials=true", runtime)
        self.assertNotIn("private static void ApplyNamedMaterials(", runtime)
        self.assertNotIn("private static void ApplyMaterial(GameObject", runtime)

    def test_manifest_remains_blocked_until_all_surface_replacement_is_real(self):
        manifest = self._read("docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json")
        self.assertIn('"reviewState": "BLOCKED"', manifest)
        self.assertIn('"sourceQuality": "authored-source-candidate"', manifest)
        self.assertIn('"runtimeIntegrationVerified": false', manifest)
        self.assertIn('"status": "replacement-required"', manifest)


if __name__ == "__main__":
    unittest.main()
