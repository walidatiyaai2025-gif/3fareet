import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uart005ProductionSurfaceContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_current_road_source_is_still_geometry_candidate_not_surface_ready(self):
        road = self._read(
            "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Track_CairoRoad_A.obj"
        )
        self.assertIn("v ", road)
        self.assertIn("f ", road)
        self.assertNotIn("\nvt ", "\n" + road)
        self.assertNotIn("\nvn ", "\n" + road)

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

    def test_player_preserves_imported_source_materials(self):
        runtime = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredStreetKit.cs")
        self.assertIn("ApplyNamedMaterialsForEditorPreview", runtime)
        self.assertIn("ApplyMaterialForEditorPreview", runtime)
        self.assertIn("if (!Application.isEditor)", runtime)
        self.assertIn("player-preserves-source-materials=true", runtime)
        self.assertNotIn("private static void ApplyNamedMaterials(", runtime)
        self.assertNotIn("private static void ApplyMaterial(GameObject", runtime)

    def test_manifest_remains_blocked_until_surface_replacement_is_real(self):
        manifest = self._read("docs/assets/02_tracks_environments/cairo_street_kit/ASSET_MANIFEST.json")
        self.assertIn('"reviewState": "BLOCKED"', manifest)
        self.assertIn('"sourceQuality": "authored-source-candidate"', manifest)
        self.assertIn('"runtimeIntegrationVerified": false', manifest)
        self.assertIn('"status": "replacement-required"', manifest)


if __name__ == "__main__":
    unittest.main()
