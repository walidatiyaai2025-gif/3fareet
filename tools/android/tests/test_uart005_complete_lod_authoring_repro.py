import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / 'tools/android/author_uart005_mobile_lods_complete.py'
COMMITTED_MANIFEST = REPO_ROOT / 'docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_MANIFEST.json'
RELATIVE_MANIFEST = Path('docs/assets/02_tracks_environments/cairo_street_kit/MOBILE_LOD_MANIFEST.json')


class Uart005CompleteLodAuthoringReproTests(unittest.TestCase):
    def test_complete_authoring_reproduces_committed_13_module_manifest(self):
        with tempfile.TemporaryDirectory() as temp_dir:
            completed = subprocess.run(
                [sys.executable, str(SCRIPT)],
                cwd=temp_dir,
                check=True,
                capture_output=True,
                text=True,
            )
            generated_manifest_path = Path(temp_dir) / RELATIVE_MANIFEST
            self.assertTrue(generated_manifest_path.is_file())

            generated = json.loads(generated_manifest_path.read_text(encoding='utf-8'))
            committed = json.loads(COMMITTED_MANIFEST.read_text(encoding='utf-8'))
            self.assertEqual(committed, generated)

            self.assertEqual('13/13', generated['moduleCoverage'])
            self.assertEqual(26, generated['distinctLodSourceAssets'])
            self.assertEqual(13, len(generated['modules']))
            self.assertFalse(generated['sameMeshLodReuseAllowed'])
            self.assertFalse(generated['runtimeLodIntegrationVerified'])
            self.assertIn('AFAREET_UART005_LOD_AUTHOR_COMPLETE_OK modules=13 distinctSources=26 runtimeVerified=false', completed.stdout)
            self.assertNotIn('modules=11 distinctSources=22 runtimeVerified=false', completed.stdout)

    def test_complete_authoring_reproduces_exact_road_curb_source_hashes(self):
        committed = json.loads(COMMITTED_MANIFEST.read_text(encoding='utf-8'))
        expected = {record['key']: record for record in committed['modules'] if record['key'] in {'road-a', 'curb-a'}}
        self.assertEqual({'road-a', 'curb-a'}, set(expected))

        with tempfile.TemporaryDirectory() as temp_dir:
            subprocess.run(
                [sys.executable, str(SCRIPT)],
                cwd=temp_dir,
                check=True,
                capture_output=True,
                text=True,
            )
            generated = json.loads((Path(temp_dir) / RELATIVE_MANIFEST).read_text(encoding='utf-8'))
            actual = {record['key']: record for record in generated['modules'] if record['key'] in {'road-a', 'curb-a'}}

        for key in ('road-a', 'curb-a'):
            self.assertEqual(expected[key]['lod1']['sha256'], actual[key]['lod1']['sha256'])
            self.assertEqual(expected[key]['lod2']['sha256'], actual[key]['lod2']['sha256'])
            self.assertGreater(actual[key]['lod0Triangles'], actual[key]['lod1']['triangles'])
            self.assertGreater(actual[key]['lod1']['triangles'], actual[key]['lod2']['triangles'])


if __name__ == '__main__':
    unittest.main()
