import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/validate_hero_refinement_handoff.py"
RECEIPT_PATH = REPO_ROOT / "tools/android/hero_refinement_handoff_receipt.json"
MANIFEST_PATH = REPO_ROOT / "tools/android/hero_refinement_candidate_manifest.json"
SPEC = importlib.util.spec_from_file_location("validate_hero_refinement_handoff", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class ValidateHeroRefinementHandoffTests(unittest.TestCase):
    @staticmethod
    def file_record(path: Path, role: str):
        payload = path.read_bytes()
        return {
            "fileName": path.name,
            "sizeBytes": len(payload),
            "sha256": hashlib.sha256(payload).hexdigest(),
            "role": role,
        }

    def build_fixture(self, directory: Path):
        fbx = directory / "AfareetKing_Hero.fbx"
        glb = directory / "AfareetKing_Hero.glb"
        blend = directory / "AfareetKing_Hero.blend"
        fbx.write_bytes(b"fbx-fixture")
        glb.write_bytes(b"glb-fixture")
        blend.write_bytes(b"blend-fixture")

        fbx_record = self.file_record(fbx, MODULE.EXPECTED_FBX_ROLE)
        receipt = {
            "schemaVersion": 1,
            "task": "UART-003",
            "classification": "REFINEMENT_CANDIDATE",
            "origin": "EXTERNAL_USER_HANDOFF",
            "files": {
                "fbx": fbx_record,
                "glb": self.file_record(glb, MODULE.EXPECTED_GLB_ROLE),
                "blend": self.file_record(blend, MODULE.EXPECTED_BLEND_ROLE),
            },
            "productionGate": False,
            "visualAcceptance": False,
            "ownerApproval": False,
            "verified": False,
            "inspectionBoundary": MODULE.EXPECTED_BOUNDARY,
        }
        manifest = {
            "schemaVersion": 1,
            "classification": "REFINEMENT_CANDIDATE",
            "sourceFileName": fbx_record["fileName"],
            "sha256": fbx_record["sha256"],
            "sizeBytes": fbx_record["sizeBytes"],
            "productionGate": False,
            "visualAcceptance": False,
        }
        return fbx, glb, blend, receipt, manifest

    def test_repository_receipt_matches_pinned_refinement_manifest(self):
        receipt = json.loads(RECEIPT_PATH.read_text(encoding="utf-8"))
        manifest = json.loads(MANIFEST_PATH.read_text(encoding="utf-8"))
        records = MODULE.validate_receipt(receipt, manifest)
        self.assertEqual(records["fbx"]["sha256"], manifest["sha256"])
        self.assertFalse(receipt["productionGate"])
        self.assertFalse(receipt["verified"])

    def test_exact_three_file_handoff_is_accepted_without_production_promotion(self):
        with tempfile.TemporaryDirectory() as directory:
            fbx, glb, blend, receipt, manifest = self.build_fixture(Path(directory))
            records = MODULE.validate_receipt(receipt, manifest)
            verified_files = {
                "fbx": MODULE.verify_exact_file(fbx, records["fbx"], label="FBX"),
                "glb": MODULE.verify_exact_file(glb, records["glb"], label="GLB"),
                "blend": MODULE.verify_exact_file(blend, records["blend"], label="BLEND"),
            }
            result = MODULE.build_result(receipt, verified_files)

        self.assertEqual(result["verdict"], "REFINEMENT_HANDOFF_MATCH_NOT_PRODUCTION")
        self.assertTrue(result["handoffByteIdentityMatch"])
        self.assertFalse(result["productionGate"])
        self.assertFalse(result["visualAcceptance"])
        self.assertFalse(result["ownerApproval"])
        self.assertFalse(result["verified"])

    def test_receipt_cannot_escape_refinement_classification_or_acceptance_boundary(self):
        with tempfile.TemporaryDirectory() as directory:
            _, _, _, receipt, manifest = self.build_fixture(Path(directory))
            for key, value in (
                ("classification", "PRODUCTION"),
                ("productionGate", True),
                ("visualAcceptance", True),
                ("ownerApproval", True),
                ("verified", True),
            ):
                mutated = dict(receipt)
                mutated[key] = value
                with self.subTest(key=key):
                    with self.assertRaises(MODULE.HandoffError):
                        MODULE.validate_receipt(mutated, manifest)

    def test_fbx_identity_must_match_existing_refinement_manifest(self):
        with tempfile.TemporaryDirectory() as directory:
            _, _, _, receipt, manifest = self.build_fixture(Path(directory))
            manifest["sha256"] = "0" * 64
            with self.assertRaises(MODULE.HandoffError):
                MODULE.validate_receipt(receipt, manifest)

    def test_exact_file_rejects_name_size_and_hash_mismatch(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            fbx, _, _, receipt, manifest = self.build_fixture(root)
            record = MODULE.validate_receipt(receipt, manifest)["fbx"]

            wrong_name = root / "renamed.fbx"
            wrong_name.write_bytes(fbx.read_bytes())
            with self.assertRaises(MODULE.HandoffError):
                MODULE.verify_exact_file(wrong_name, record, label="FBX")

            fbx.write_bytes(b"different")
            with self.assertRaises(MODULE.HandoffError):
                MODULE.verify_exact_file(fbx, record, label="FBX")

    def test_output_result_remains_collection_only(self):
        receipt = {
            "inspectionBoundary": MODULE.EXPECTED_BOUNDARY,
        }
        result = MODULE.build_result(receipt, {"fbx": {}, "glb": {}, "blend": {}})
        self.assertEqual(result["classification"], "REFINEMENT_CANDIDATE")
        self.assertEqual(result["verdict"], MODULE.VERDICT)
        self.assertFalse(result["verified"])
        self.assertFalse(result["productionGate"])


if __name__ == "__main__":
    unittest.main()
