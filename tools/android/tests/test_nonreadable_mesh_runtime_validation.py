import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
FILES = [
    ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoStreetKitMobileLodRuntimePass.cs",
    ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoRoadCurbMobileLodRuntimePass.cs",
]

class NonReadableMeshRuntimeValidationContract(unittest.TestCase):
    def test_runtime_lod_validation_uses_vertex_attribute_metadata(self):
        for path in FILES:
            source = path.read_text(encoding="utf-8-sig")
            self.assertIn("using UnityEngine.Rendering;", source, msg=path.name)
            self.assertIn("mesh.HasVertexAttribute(VertexAttribute.TexCoord0)", source, msg=path.name)
            self.assertIn("mesh.HasVertexAttribute(VertexAttribute.Normal)", source, msg=path.name)

    def test_runtime_lod_validation_does_not_read_mesh_uv_or_normals_arrays(self):
        for path in FILES:
            source = path.read_text(encoding="utf-8-sig")
            self.assertNotIn("mesh.uv", source, msg=path.name)
            self.assertNotIn("mesh.normals", source, msg=path.name)

if __name__ == "__main__":
    unittest.main()
