import hashlib
import json
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
MANIFEST = REPO_ROOT / "docs/assets/01_vehicles/rival_cars_production/SOURCE_CANDIDATES.json"
ASSET_ROOT = REPO_ROOT / "unity_game/Assets/Afareet/ArtSource/Vehicles/Rivals"
BANDS = ((1800, 16000), (800, 8000), (350, 4000))

def parse_obj(path: Path):
    lines = path.read_text(encoding="utf-8").splitlines()
    vertices = [line for line in lines if line.startswith("v ")]
    uvs = [line for line in lines if line.startswith("vt ")]
    normals = [line for line in lines if line.startswith("vn ")]
    lod_faces = {0: [], 1: [], 2: []}
    active = None
    for line in lines:
        if line.startswith("o "):
            active = next((lod for lod in range(3) if line.endswith(f"_LOD{lod}")), None)
        elif line.startswith("f ") and active is not None:
            lod_faces[active].append(line)
    return lines, vertices, uvs, normals, lod_faces

class Uart004StaticSourceCandidateTests(unittest.TestCase):
    def test_three_distinct_static_sources_exist_under_assets_and_remain_unaccepted(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        self.assertEqual("BLOCKED", manifest["reviewState"])
        self.assertEqual("3/3", manifest["sourceDeliveryProgress"])
        self.assertEqual("0/3", manifest["productionPrefabsBound"])
        self.assertFalse(manifest["licensedUnityImportVerified"])
        self.assertFalse(manifest["runtimeVisualVerified"])
        self.assertEqual(3, len(manifest["sources"]))
        paths = [entry["sourceAssetId"] for entry in manifest["sources"]]
        self.assertEqual(3, len(set(paths)))
        for source in paths:
            self.assertTrue(source.startswith("Assets/Afareet/ArtSource/Vehicles/Rivals/"), source)
            self.assertTrue(source.endswith(".obj"), source)
            self.assertNotIn("/Generated/", source)

    def test_all_lods_have_uv_normals_material_texture_and_budgeted_topology(self):
        manifest = json.loads(MANIFEST.read_text(encoding="utf-8"))
        digests = set()
        for entry in manifest["sources"]:
            obj = REPO_ROOT / "unity_game" / entry["sourceAssetId"]
            mtl = obj.parent / entry["material"]
            texture = obj.parent / entry["texture"]
            self.assertTrue(obj.is_file(), obj)
            self.assertTrue(mtl.is_file(), mtl)
            self.assertTrue(texture.is_file(), texture)
            self.assertEqual(bytes((137,80,78,71,13,10,26,10)), texture.read_bytes()[:8])
            digest = hashlib.sha256(obj.read_bytes()).hexdigest()
            self.assertEqual(entry["sha256"], digest)
            digests.add(digest)

            lines, vertices, uvs, normals, lod_faces = parse_obj(obj)
            self.assertEqual(len(vertices), len(uvs), obj.name)
            self.assertEqual(len(vertices), len(normals), obj.name)
            self.assertIn(f"mtllib {entry['material']}", lines)
            mtl_text = mtl.read_text(encoding="utf-8")
            self.assertGreaterEqual(mtl_text.count(f"map_Kd {entry['texture']}"), 4)

            measured = []
            for lod in range(3):
                faces = lod_faces[lod]
                measured.append(len(faces))
                low, high = BANDS[lod]
                self.assertGreaterEqual(len(faces), low, (obj.name, lod, len(faces)))
                self.assertLessEqual(len(faces), high, (obj.name, lod, len(faces)))
                for face in faces:
                    for token in face.split()[1:]:
                        fields = token.split("/")
                        self.assertEqual(3, len(fields), token)
                        self.assertTrue(all(fields), token)
                        vi, ti, ni = map(int, fields)
                        self.assertGreaterEqual(vi, 1); self.assertLessEqual(vi, len(vertices))
                        self.assertGreaterEqual(ti, 1); self.assertLessEqual(ti, len(uvs))
                        self.assertGreaterEqual(ni, 1); self.assertLessEqual(ni, len(normals))
            self.assertEqual(entry["lodTriangles"], measured)
        self.assertEqual(3, len(digests))

if __name__ == "__main__":
    unittest.main()
