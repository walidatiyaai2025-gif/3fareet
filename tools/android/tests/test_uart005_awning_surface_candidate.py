import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source"


class Uart005AwningSurfaceCandidateTests(unittest.TestCase):
    def _read(self, name: str) -> str:
        return (SOURCE_ROOT / name).read_text(encoding="utf-8")

    def test_awning_has_authored_surface_chain(self):
        obj = self._read("SM_Env_CairoAwning_A.obj")
        self.assertIn("mtllib SM_Env_CairoAwning_A.mtl", obj)
        self.assertIn("usemtl Awning_Surface", obj)
        self.assertIn("\nvt ", "\n" + obj)
        self.assertIn("\nvn ", "\n" + obj)

        vertices = [line for line in obj.splitlines() if line.startswith("v ")]
        faces = [line for line in obj.splitlines() if line.startswith("f ")]
        self.assertEqual(64, len(vertices))
        self.assertEqual(96, len(faces))
        for face in faces:
            for token in face.split()[1:]:
                indices = token.split("/")
                self.assertEqual(3, len(indices), token)
                self.assertTrue(indices[1], token)
                self.assertTrue(indices[2], token)

    def test_awning_material_dependency_is_tracked(self):
        mtl = self._read("SM_Env_CairoAwning_A.mtl")
        self.assertIn("newmtl Awning_Surface", mtl)
        self.assertIn("map_Kd T_Env_CairoAwning_Surface_BC.png", mtl)
        texture = SOURCE_ROOT / "T_Env_CairoAwning_Surface_BC.png"
        self.assertTrue(texture.is_file())
        self.assertGreater(texture.stat().st_size, 32)

    def test_awning_bounds_preserve_runtime_contract(self):
        vertices = []
        for raw in self._read("SM_Env_CairoAwning_A.obj").splitlines():
            if not raw.startswith("v "):
                continue
            _, x, y, z = raw.split()
            vertices.append((float(x), float(y), float(z)))
        xs = [v[0] for v in vertices]
        ys = [v[1] for v in vertices]
        zs = [v[2] for v in vertices]
        self.assertAlmostEqual(0.0, min(xs), places=4)
        self.assertAlmostEqual(3.0, max(xs), places=4)
        self.assertAlmostEqual(0.0, min(ys), places=4)
        self.assertLessEqual(max(ys), 1.3)
        self.assertAlmostEqual(0.0, min(zs), places=4)
        self.assertAlmostEqual(1.5, max(zs), places=4)

    def test_package_is_fully_surfaced_but_not_runtime_verified(self):
        for model in ("SM_Prop_CairoLamp_A.obj", "SM_Prop_CairoBarrier_A.obj"):
            obj = self._read(model)
            self.assertIn("\nvn ", "\n" + obj)
            self.assertIn("\nmtllib ", "\n" + obj)
        manifest = (SOURCE_ROOT.parent / "ASSET_MANIFEST.json").read_text(encoding="utf-8")
        self.assertIn('"reviewState": "BLOCKED"', manifest)
        self.assertIn('"runtimeIntegrated": false', manifest)
        self.assertIn('"runtimeIntegrationVerified": false', manifest)


if __name__ == "__main__":
    unittest.main()
