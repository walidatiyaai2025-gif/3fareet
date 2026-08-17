import importlib.util
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]


def _load_module():
    path = TOOLS_DIR / "validate_hero_asset_intake.py"
    spec = importlib.util.spec_from_file_location("validate_hero_asset_intake", path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[spec.name] = module
    spec.loader.exec_module(module)
    return module


INTAKE = _load_module()


class HeroAssetIntakeTests(unittest.TestCase):
    def test_supported_formats_match_unity_production_source_contract(self):
        self.assertEqual({".obj", ".fbx", ".glb", ".gltf", ".blend"}, INTAKE.SUPPORTED_SUFFIXES)

    def test_obj_inspector_requires_three_named_lods_and_observes_uv_normals_materials(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            obj = root / "SM_Vehicle_AfareetKing.obj"
            obj.write_text(
                "\n".join(
                    [
                        "mtllib hero.mtl",
                        "v 0 0 0",
                        "v 1 0 0",
                        "v 0 1 0",
                        "vt 0 0",
                        "vt 1 0",
                        "vt 0 1",
                        "vn 0 0 1",
                        "o AfareetKing_LOD0",
                        "usemtl Hero",
                        "f 1/1/1 2/2/1 3/3/1",
                        "o AfareetKing_LOD1",
                        "usemtl Hero",
                        "f 1/1/1 2/2/1 3/3/1",
                        "o AfareetKing_LOD2",
                        "usemtl Hero",
                        "f 1/1/1 2/2/1 3/3/1",
                    ]
                )
                + "\n",
                encoding="utf-8",
            )
            stats, mtls = INTAKE.inspect_obj(obj)

        self.assertEqual([0, 1, 2], [item.lod for item in stats])
        self.assertTrue(all(item.has_complete_uv0 for item in stats))
        self.assertTrue(all(item.has_complete_normals for item in stats))
        self.assertTrue(all(item.material_names == ("Hero",) for item in stats))
        self.assertEqual((root / "hero.mtl",), mtls)

    def test_binary_source_never_claims_structural_or_production_art_approval(self):
        with tempfile.TemporaryDirectory() as tmp:
            repo = Path(tmp)
            source = repo / INTAKE.EXPECTED_ROOT / "AfareetKing.fbx"
            source.parent.mkdir(parents=True)
            source.write_bytes(b"not-a-real-fbx")
            with mock.patch.object(INTAKE, "_is_tracked", return_value=True):
                result = INTAKE.validate_intake(repo, source)

        self.assertEqual("UNITY_INSPECTION_REQUIRED", result["verdict"])
        self.assertEqual("BINARY_OR_DCC_SOURCE_NOT_INSPECTED", result["sourceInspection"])
        self.assertFalse(result["verified"])
        self.assertFalse(result["productionArtApproved"])

    def test_generated_preview_and_rival_paths_are_rejected(self):
        for forbidden in ("Generated", "Preview", "Blockout", "Rivals"):
            with self.subTest(forbidden=forbidden), tempfile.TemporaryDirectory() as tmp:
                repo = Path(tmp)
                source = repo / INTAKE.EXPECTED_ROOT / forbidden / "AfareetKing.obj"
                source.parent.mkdir(parents=True)
                source.write_text("# placeholder\n", encoding="utf-8")
                with mock.patch.object(INTAKE, "_is_tracked", return_value=True):
                    with self.assertRaisesRegex(INTAKE.HeroAssetIntakeError, "forbidden path segment"):
                        INTAKE.validate_intake(repo, source)

    def test_cli_requires_source_and_never_has_verified_switch(self):
        parser = INTAKE.build_parser()
        actions = {action.dest: action for action in parser._actions}
        self.assertTrue(actions["source"].required)
        self.assertNotIn("verified", actions)
        self.assertNotIn("approve", actions)


if __name__ == "__main__":
    unittest.main()
