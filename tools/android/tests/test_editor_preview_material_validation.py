import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
FILES = [
    ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoStreetKitMobileLodRuntimePass.cs",
    ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoRoadCurbMobileLodRuntimePass.cs",
]

class EditorPreviewMaterialValidationContract(unittest.TestCase):
    def test_runtime_validation_never_uses_material_main_texture_getter(self):
        for path in FILES:
            source = path.read_text(encoding="utf-8-sig")
            self.assertNotIn("material.mainTexture", source, msg=path.name)
            self.assertNotIn(".mainTexture", source, msg=path.name)

    def test_editor_preview_skips_player_texture_binding_requirement(self):
        for path in FILES:
            source = path.read_text(encoding="utf-8-sig")
            self.assertIn("Application.isEditor", source, msg=path.name)
            self.assertIn("HasBoundTexture", source, msg=path.name)
            self.assertIn('material.HasProperty("_MainTex")', source, msg=path.name)
            self.assertIn('material.HasProperty("_BaseMap")', source, msg=path.name)

    def test_nonreadable_mesh_fix_is_preserved(self):
        for path in FILES:
            source = path.read_text(encoding="utf-8-sig")
            self.assertIn("VertexAttribute.TexCoord0", source, msg=path.name)
            self.assertIn("VertexAttribute.Normal", source, msg=path.name)
            self.assertNotIn("mesh.uv", source, msg=path.name)
            self.assertNotIn("mesh.normals", source, msg=path.name)

if __name__ == "__main__":
    unittest.main()
