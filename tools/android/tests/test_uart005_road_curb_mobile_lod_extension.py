import hashlib
import json
import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
SOURCE = REPO_ROOT / 'docs/assets/02_tracks_environments/cairo_street_kit/source'
MANIFEST = REPO_ROOT / 'docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_MANIFEST.json'
STATUS = REPO_ROOT / 'docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_RUNTIME_STATUS.json'


class Uart005RoadCurbMobileLodExtensionTests(unittest.TestCase):
    def _read(self, path):
        return (REPO_ROOT / path).read_text(encoding='utf-8')

    @staticmethod
    def _canonical_text_sha256(path):
        data = path.read_bytes()
        data = data.replace(b'\r\n', b'\n').replace(b'\r', b'\n')
        return hashlib.sha256(data).hexdigest()

    def test_manifest_covers_all_13_modules_and_26_distinct_sources(self):
        manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
        self.assertEqual('BLOCKED', manifest['reviewState'])
        self.assertEqual('13/13', manifest['moduleCoverage'])
        self.assertEqual(26, manifest['distinctLodSourceAssets'])
        self.assertFalse(manifest['sameMeshLodReuseAllowed'])
        self.assertEqual(13, len(manifest['modules']))
        self.assertEqual({'road-a', 'curb-a'}, {m['key'] for m in manifest['modules']} & {'road-a', 'curb-a'})

        digests = set()
        for module in manifest['modules']:
            self.assertGreater(module['lod0Triangles'], module['lod1']['triangles'])
            self.assertGreater(module['lod1']['triangles'], module['lod2']['triangles'])
            for key in ('lod1', 'lod2'):
                path = SOURCE / module[key]['model']
                self.assertTrue(path.is_file(), path)
                digest = self._canonical_text_sha256(path)
                self.assertEqual(module[key]['sha256'], digest)
                self.assertNotIn(digest, digests)
                digests.add(digest)
        self.assertEqual(26, len(digests))

    def test_road_and_curb_sources_have_complete_surface_streams(self):
        manifest = json.loads(MANIFEST.read_text(encoding='utf-8'))
        modules = {m['key']: m for m in manifest['modules']}
        for key in ('road-a', 'curb-a'):
            module = modules[key]
            material = SOURCE / module['sharedMobileMaterial']
            material_text = material.read_text(encoding='utf-8')
            self.assertIn(f"newmtl {module['materialName']}", material_text)
            self.assertIn(f"map_Kd {module['texture']}", material_text)
            for lod_key in ('lod1', 'lod2'):
                path = SOURCE / module[lod_key]['model']
                text = path.read_text(encoding='utf-8')
                lines = text.splitlines()
                vertices = sum(line.startswith('v ') for line in lines)
                uvs = sum(line.startswith('vt ') for line in lines)
                normals = sum(line.startswith('vn ') for line in lines)
                faces = sum(line.startswith('f ') for line in lines)
                self.assertEqual(module[lod_key]['vertices'], vertices)
                self.assertEqual(vertices, uvs)
                self.assertEqual(vertices, normals)
                self.assertEqual(module[lod_key]['triangles'], faces)
                self.assertIn(f"mtllib {module['sharedMobileMaterial']}", text)
                self.assertIn(f"usemtl {module['materialName']}", text)

    def test_runtime_pass_binds_controlled_road_and_curb_names_fail_closed(self):
        text = self._read('unity_game/Assets/Afareet/Scripts/World/CairoRoadCurbMobileLodRuntimePass.cs')
        for required in (
            'Authored Crowned Asphalt',
            'Authored Curb Right',
            'Authored Curb Left',
            'SM_Track_CairoRoad_A',
            'SM_Track_CairoCurb_A',
            '_LOD1', '_LOD2', 'LODGroup',
            'fake same-mesh road/curb LOD reuse rejected',
            't0 > t1 && t1 > t2 && t2 > 0',
            'mesh.HasVertexAttribute(VertexAttribute.TexCoord0)',
            'mesh.HasVertexAttribute(VertexAttribute.Normal)',
            'HasBoundTexture', 'Application.isEditor',
            'material.HasProperty("_MainTex")',
            'material.HasProperty("_BaseMap")',
            'secondary road/curb LOD must not introduce colliders',
            'AFAREET_UART005_ROAD_CURB_MOBILE_LOD_ACTIVE',
            'AFAREET_UART005_ROAD_CURB_MOBILE_LOD_BLOCKED',
        ):
            self.assertIn(required, text)
        for obsolete in ('mesh.uv', 'mesh.normals', 'material.mainTexture'):
            self.assertNotIn(obsolete, text)
        for forbidden in ('GameObject.CreatePrimitive', 'new Mesh(', 'RecalculateNormals'):
            self.assertNotIn(forbidden, text)

    def test_android_gate_requires_exact_tracked_road_and_curb_triplets(self):
        text = self._read('unity_game/Assets/Afareet/Editor/P1ProductionRoadCurbMobileLodBuildGate.cs')
        for required in (
            'IPreprocessBuildWithReport', 'BuildTarget.Android', 'StageTrackedSourcesOrThrow',
            'SM_Track_CairoRoad_A', 'SM_Track_CairoCurb_A',
            'AssetDatabase.GetAssetPath', 'expectedSuffix', '_LOD{lod}.obj',
            'mesh.uv', 'mesh.normals', 'material.mainTexture',
            'triangles[0] > triangles[1] && triangles[1] > triangles[2]',
            'fake same-source road/curb LOD reuse rejected',
            'fake same-mesh road/curb LOD reuse rejected',
            'AFAREET_UART005_ROAD_CURB_MOBILE_LOD_GATE_OK',
            'AFAREET_UART005_ROAD_CURB_MOBILE_LOD_GATE_BLOCKED',
        ):
            self.assertIn(required, text)

    def test_runtime_status_remains_blocked_and_unverified(self):
        status = json.loads(STATUS.read_text(encoding='utf-8'))
        self.assertEqual('BLOCKED', status['reviewState'])
        self.assertEqual('13/13', status['moduleCoverage'])
        self.assertEqual(26, status['distinctLodSourceAssets'])
        self.assertTrue(status['runtimeLodIntegrationImplemented'])
        self.assertFalse(status['runtimeLodIntegrationVerified'])
        self.assertFalse(status['sameMeshLodReuseAllowed'])


if __name__ == '__main__':
    unittest.main()
