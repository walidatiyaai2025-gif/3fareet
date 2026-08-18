import importlib.util
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/validate_uart004_rival_production_handoff.py"
POLICY_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs"
SPEC = importlib.util.spec_from_file_location("validate_uart004_rival_production_handoff", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class RivalProductionHandoffTests(unittest.TestCase):
    def setUp(self):
        self.policy = MODULE.parse_policy(POLICY_PATH)

    @staticmethod
    def write_mtl(root: Path, name: str, *, texture_mapped: bool = True) -> Path:
        texture = root / "rival_albedo.png"
        texture.write_bytes(b"texture-fixture")
        lines = []
        for lod in range(3):
            lines.append(f"newmtl Mat_LOD{lod}")
            lines.append("Kd 1 1 1")
            if texture_mapped:
                lines.append("map_Kd rival_albedo.png")
        path = root / name
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        return path

    def write_obj(
        self,
        root: Path,
        variant: int,
        *,
        triangles=None,
        file_name=None,
        unique_comment=True,
        face_token="1/1/1 2/2/1 3/3/1",
        include_unclassified_face=False,
        texture_mapped=True,
        shared_mtl_name=None,
    ) -> Path:
        triangle_counts = triangles or self.policy["MinimumTriangles"]
        name = file_name or self.policy["sourceFileNames"][variant]
        mtl_name = shared_mtl_name or f"rival_{variant + 1}.mtl"
        self.write_mtl(root, mtl_name, texture_mapped=texture_mapped)
        lines = []
        if unique_comment:
            lines.append(f"# variant {variant + 1}")
        lines.extend(
            (
                f"mtllib {mtl_name}",
                "v 0 0 0",
                "v 1 0 0",
                "v 0 1 0",
                "vt 0 0",
                "vt 1 0",
                "vt 0 1",
                "vn 0 0 1",
            )
        )
        if include_unclassified_face:
            lines.extend(("o Extra_Unclassified", "usemtl Mat_LOD0", f"f {face_token}"))
        for lod, count in enumerate(triangle_counts):
            lines.append(f"o Rival_{variant + 1:02}_LOD{lod}")
            lines.append(f"usemtl Mat_LOD{lod}")
            lines.extend([f"f {face_token}"] * count)
        path = root / name
        path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        return path

    def test_policy_drives_exact_production_exchange_names_and_triangle_bands(self):
        self.assertEqual(
            self.policy["sourceFileNames"],
            [
                "Rival_01_WedgeCoupe_Production.obj",
                "Rival_02_FastbackMuscle_Production.obj",
                "Rival_03_CompactPrototype_Production.obj",
            ],
        )
        self.assertEqual(self.policy["MinimumTriangles"], [1800, 800, 350])
        self.assertEqual(self.policy["MaximumTriangles"], [16000, 8000, 4000])
        self.assertEqual(
            self.policy["productionSourceRoot"],
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/",
        )

    def test_exact_three_source_handoff_passes_technical_checks_without_acceptance_promotion(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            paths = [self.write_obj(root, variant) for variant in range(3)]
            report = MODULE.validate_handoff(paths, POLICY_PATH)

        self.assertEqual(report["verdict"], "TECHNICAL_HANDOFF_PASS_NOT_PRODUCTION_ACCEPTANCE")
        self.assertTrue(report["technicalPreflightPassed"])
        self.assertEqual(report["distinctSourceHashes"], 3)
        for variant in report["variants"]:
            self.assertEqual(len(variant["lods"]), 3)
            self.assertTrue(variant["materialLibraries"])
        for key in (
            "productionGate",
            "visualAcceptance",
            "ownerApproval",
            "provenanceAccepted",
            "licensedUnityImportVerified",
            "physicalDeviceVerified",
            "verified",
        ):
            self.assertFalse(report[key], key)

    def test_review_filename_or_wrong_exchange_name_is_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            wrong = self.write_obj(root, 0, file_name="Rival_01_WedgeCoupe.obj")
            with self.assertRaises(MODULE.HandoffError):
                MODULE.parse_obj(wrong, self.policy, 0)

    def test_three_variants_must_not_reuse_identical_obj_bytes(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            self.write_mtl(root, "shared.mtl")
            paths = [
                self.write_obj(
                    root,
                    variant,
                    unique_comment=False,
                    shared_mtl_name="shared.mtl",
                )
                for variant in range(3)
            ]
            # Object names differ by variant in normal fixtures, so rewrite them to the same
            # authored LOD labels while retaining the three required exchange filenames.
            for path in paths:
                text = path.read_text(encoding="utf-8")
                for variant_number in (1, 2, 3):
                    text = text.replace(f"Rival_{variant_number:02}_LOD", "Shared_Rival_LOD")
                path.write_text(text, encoding="utf-8")
            with self.assertRaisesRegex(MODULE.HandoffError, "reuses identical OBJ bytes"):
                MODULE.validate_handoff(paths, POLICY_PATH)

    def test_triangle_band_is_enforced_from_policy(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            below = list(self.policy["MinimumTriangles"])
            below[0] -= 1
            path = self.write_obj(root, 0, triangles=below)
            with self.assertRaisesRegex(MODULE.HandoffError, "LOD0 triangle count"):
                MODULE.parse_obj(path, self.policy, 0)

    def test_faces_require_explicit_lod_uv_normal_and_material_signatures(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            missing_uv = self.write_obj(root, 0, face_token="1//1 2//1 3//1")
            with self.assertRaisesRegex(MODULE.HandoffError, "without both vt and vn indices"):
                MODULE.parse_obj(missing_uv, self.policy, 0)

            unclassified = self.write_obj(root, 0, include_unclassified_face=True)
            with self.assertRaisesRegex(MODULE.HandoffError, "faces outside explicit"):
                MODULE.parse_obj(unclassified, self.policy, 0)

    def test_every_used_material_must_resolve_to_an_existing_texture_map(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            path = self.write_obj(root, 0, texture_mapped=False)
            parsed = MODULE.parse_obj(path, self.policy, 0)
            with self.assertRaisesRegex(MODULE.HandoffError, "not texture-mapped"):
                MODULE.validate_mtl_and_textures(path, parsed)

    def test_source_contract_never_claims_production_or_owner_verification(self):
        source = MODULE_PATH.read_text(encoding="utf-8")
        for required in (
            '"productionGate": False',
            '"visualAcceptance": False',
            '"ownerApproval": False',
            '"provenanceAccepted": False',
            '"licensedUnityImportVerified": False',
            '"physicalDeviceVerified": False',
            '"verified": False',
            "TECHNICAL_SOURCE_PREFLIGHT_ONLY_LICENSE_VISUAL_UNITY_DEVICE_OWNER_GATES_REQUIRED",
        ):
            self.assertIn(required, source)
        for forbidden in (
            '"productionGate": True',
            '"ownerApproval": True',
            '"verified": True',
        ):
            self.assertNotIn(forbidden, source)


if __name__ == "__main__":
    unittest.main()
