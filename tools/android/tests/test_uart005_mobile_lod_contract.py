import hashlib
import json
import unittest
from pathlib import Path

REPO_ROOT=Path(__file__).resolve().parents[3]
ROOT=REPO_ROOT/'docs/assets/02_tracks_environments/cairo_street_kit/source'
MANIFEST=REPO_ROOT/'docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_MANIFEST.json'
STATUS=REPO_ROOT/'docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_RUNTIME_STATUS.json'

class Uart005MobileLodContractTests(unittest.TestCase):
    def _read(self,p): return (REPO_ROOT/p).read_text(encoding='utf-8')

    def test_11_modules_have_22_distinct_monotonic_lod_sources(self):
        m=json.loads(MANIFEST.read_text())
        self.assertEqual('BLOCKED',m['reviewState']); self.assertEqual('11/11',m['moduleCoverage'])
        self.assertEqual(22,m['distinctLodSourceAssets']); self.assertFalse(m['sameMeshLodReuseAllowed'])
        digests=set()
        for mod in m['modules']:
            self.assertGreater(mod['lod0Triangles'],mod['lod1']['triangles'])
            self.assertGreater(mod['lod1']['triangles'],mod['lod2']['triangles'])
            for key in ('lod1','lod2'):
                p=ROOT/mod[key]['model']; self.assertTrue(p.is_file(),p)
                text=p.read_text(); lines=text.splitlines()
                v=sum(x.startswith('v ') for x in lines); t=sum(max(0,len(x.split())-3) for x in lines if x.startswith('f '))
                self.assertEqual(mod[key]['vertices'],v); self.assertEqual(mod[key]['triangles'],t)
                self.assertIn(f"mtllib {mod['sharedMobileMaterial']}",text)
                self.assertIn(f"usemtl {mod['materialName']}",text)
                self.assertIn('\nvt ','\n'+text); self.assertIn('\nvn ','\n'+text)
                digests.add(hashlib.sha256(p.read_bytes()).hexdigest())
        self.assertEqual(22,len(digests))

    def test_runtime_pass_rejects_fake_same_mesh_and_builds_three_level_groups(self):
        text=self._read('unity_game/Assets/Afareet/Scripts/World/CairoStreetKitMobileLodRuntimePass.cs')
        for s in ('_LOD1','_LOD2','LODGroup','fake same-mesh LOD reuse rejected','lod0Triangles > lod1Triangles','lod1Triangles > lod2Triangles','new LOD(.56f','new LOD(.27f','new LOD(.08f','AFAREET_UART005_MOBILE_LOD_ACTIVE','sameMeshReuse=false','FindObjectsByType<Transform>'):
            self.assertIn(s,text)
        for s in ('GameObject.CreatePrimitive','new Mesh(','RecalculateNormals'):
            self.assertNotIn(s,text)

    def test_runtime_pass_hardens_cache_retry_existing_groups_and_surface_validation(self):
        text=self._read('unity_game/Assets/Afareet/Scripts/World/CairoStreetKitMobileLodRuntimePass.cs')
        for s in (
            'RetryDelaySeconds = 5.0f',
            'retryAfterByInstance',
            'resourceCache',
            'LoadCached',
            'TryValidateExistingGroup',
            'ExpectedLodLevels',
            'ValidateRendererSet',
            'mesh.uv',
            'mesh.normals',
            'material.mainTexture',
            'MeshesOverlap',
            'RejectSecondaryLodColliders',
            'UnityEngine.Object.Destroy(group)',
            'bindingFailureLogged',
            'AFAREET_UART005_MOBILE_LOD_EXISTING_BLOCKED',
            'rendererSurfaceValidation=true',
            'resourceCache=true',
        ):
            self.assertIn(s,text)

    def test_android_gate_requires_distinct_source_paths_and_monotonic_topology(self):
        text=self._read('unity_game/Assets/Afareet/Editor/P1ProductionMobileLodBuildGate.cs')
        for s in ('IPreprocessBuildWithReport','BuildTarget.Android','StageTrackedSourcesOrThrow','AssetDatabase.GetAssetPath','mesh.uv','mesh.normals','material.mainTexture','triangleCounts[0] > triangleCounts[1]','triangleCounts[1] > triangleCounts[2]','fake same-source LOD reuse rejected','AFAREET_UART005_MOBILE_LOD_GATE_OK','AFAREET_UART005_MOBILE_LOD_GATE_BLOCKED'):
            self.assertIn(s,text)

    def test_android_gate_rejects_secondary_colliders_untracked_paths_and_mesh_reuse(self):
        text=self._read('unity_game/Assets/Afareet/Editor/P1ProductionMobileLodBuildGate.cs')
        for s in (
            'GetComponentsInChildren<Collider>(true)',
            'secondary mobile LOD must not introduce colliders',
            'mesh.vertexCount <= 0',
            'meshTriangles <= 0',
            'mesh is not backed by a tracked asset',
            'expectedSuffix',
            '_LOD{lod}.obj',
            'MeshesOverlap',
            'fake same-mesh LOD reuse rejected across imported levels',
            'exactSourceSuffix=true',
            'secondaryColliders=false',
        ):
            self.assertIn(s,text)

    def test_runtime_status_remains_unverified(self):
        s=json.loads(STATUS.read_text())
        self.assertEqual('BLOCKED',s['reviewState']); self.assertEqual('11/11',s['moduleCoverage'])
        self.assertEqual(22,s['distinctLodSourceAssets']); self.assertTrue(s['runtimeLodIntegrationImplemented'])
        self.assertFalse(s['runtimeLodIntegrationVerified']); self.assertFalse(s['sameMeshLodReuseAllowed'])

if __name__=='__main__': unittest.main()
