import importlib.util
import json
import struct
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/inspect_hero_refinement_glb.py"
SPEC = importlib.util.spec_from_file_location("inspect_hero_refinement_glb", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class InspectHeroRefinementGlbTests(unittest.TestCase):
    @staticmethod
    def write_glb(path: Path, payload: dict):
        encoded = json.dumps(payload, separators=(",", ":")).encode("utf-8")
        encoded += b" " * ((4 - len(encoded) % 4) % 4)
        total = 12 + 8 + len(encoded)
        path.write_bytes(
            struct.pack("<4sII", b"glTF", 2, total)
            + struct.pack("<II", len(encoded), MODULE.JSON_CHUNK_TYPE)
            + encoded
        )

    @staticmethod
    def payload():
        return {
            "asset": {"version": "2.0"},
            "nodes": [
                {"name": "Wheel_FL", "mesh": 0},
                {"name": "Body_LOD1", "mesh": 1},
                {"name": "Body_LOD2", "mesh": 2},
            ],
            "meshes": [
                {"primitives": [{"attributes": {"POSITION": 0}, "indices": 1}]},
                {"primitives": [{"attributes": {"POSITION": 2}, "indices": 3}]},
                {"primitives": [{"attributes": {"POSITION": 4}, "indices": 5}]},
            ],
            "accessors": [
                {"count": 12},
                {"count": 18},
                {"count": 8},
                {"count": 9},
                {"count": 6},
                {"count": 6},
            ],
        }

    @staticmethod
    def policy(path: Path, budgets=(20, 10, 8), tri_budgets=(10, 6, 4)):
        path.write_text(
            "\n".join(
                (
                    "public static readonly int[] MinimumVertices = { 1, 1, 1 };",
                    f"public static readonly int[] VertexBudgets = {{ {budgets[0]}, {budgets[1]}, {budgets[2]} }};",
                    "public static readonly int[] MinimumTriangles = { 1, 1, 1 };",
                    f"public static readonly int[] TriangleBudgets = {{ {tri_budgets[0]}, {tri_budgets[1]}, {tri_budgets[2]} }};",
                )
            ),
            encoding="utf-8",
        )

    def test_classification_matches_unity_refinement_stager_default(self):
        self.assertEqual(MODULE.classify_lod("Wheel_FL"), 0)
        self.assertEqual(MODULE.classify_lod("Body_LOD0"), 0)
        self.assertEqual(MODULE.classify_lod("Body_LOD1"), 1)
        self.assertEqual(MODULE.classify_lod("Body_LOD2"), 2)
        self.assertEqual(MODULE.classify_lod("part_lod2_extra"), 2)

    def test_glb_counts_nodes_vertices_and_triangles_by_stager_classification(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            glb = root / "fixture.glb"
            policy_path = root / "HeroCarLodPolicy.cs"
            self.write_glb(glb, self.payload())
            self.policy(policy_path)
            diagnostic = MODULE.inspect_glb(MODULE.read_glb_json(glb), MODULE.parse_policy(policy_path))

        self.assertTrue(diagnostic["mobileBudgetReady"])
        self.assertEqual(
            [(item["rendererNodes"], item["vertices"], item["triangles"]) for item in diagnostic["lods"]],
            [(1, 12, 6), (1, 8, 3), (1, 6, 2)],
        )

    def test_over_budget_companion_is_reported_without_production_promotion(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            glb = root / "fixture.glb"
            policy_path = root / "HeroCarLodPolicy.cs"
            self.write_glb(glb, self.payload())
            self.policy(policy_path, budgets=(10, 7, 5), tri_budgets=(5, 2, 1))
            diagnostic = MODULE.inspect_glb(MODULE.read_glb_json(glb), MODULE.parse_policy(policy_path))
            result = MODULE.build_result(
                {"fileName": glb.name, "sizeBytes": glb.stat().st_size, "sha256": MODULE.sha256_file(glb)},
                diagnostic,
            )

        self.assertEqual(result["verdict"], "REFINEMENT_COMPANION_OVER_BUDGET")
        self.assertFalse(result["mobileBudgetReady"])
        self.assertTrue(result["authoritativeFbxUnityInspectionRequired"])
        self.assertFalse(result["productionGate"])
        self.assertFalse(result["visualAcceptance"])
        self.assertFalse(result["ownerApproval"])
        self.assertFalse(result["verified"])

    def test_companion_identity_must_match_exact_receipt_hash_and_nonproduction_flags(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            glb = root / "AfareetKing_Hero.glb"
            self.write_glb(glb, self.payload())
            receipt = {
                "classification": "REFINEMENT_CANDIDATE",
                "files": {
                    "glb": {
                        "fileName": glb.name,
                        "sizeBytes": glb.stat().st_size,
                        "sha256": MODULE.sha256_file(glb),
                        "role": "INSPECTION_COMPANION",
                    }
                },
                "productionGate": False,
                "visualAcceptance": False,
                "ownerApproval": False,
                "verified": False,
            }
            identity = MODULE.validate_companion_identity(glb, receipt)
            self.assertEqual(identity["sha256"], receipt["files"]["glb"]["sha256"])

            receipt["files"]["glb"]["sha256"] = "0" * 64
            with self.assertRaises(MODULE.DiagnosticError):
                MODULE.validate_companion_identity(glb, receipt)

    def test_malformed_or_nontriangle_glb_fails_closed(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            malformed = root / "bad.glb"
            malformed.write_bytes(b"not-a-glb")
            with self.assertRaises(MODULE.DiagnosticError):
                MODULE.read_glb_json(malformed)

            payload = self.payload()
            payload["meshes"][0]["primitives"][0]["mode"] = 1
            with self.assertRaises(MODULE.DiagnosticError):
                MODULE.inspect_glb(
                    payload,
                    {
                        "MinimumVertices": [1, 1, 1],
                        "VertexBudgets": [100, 100, 100],
                        "MinimumTriangles": [1, 1, 1],
                        "TriangleBudgets": [100, 100, 100],
                    },
                )


if __name__ == "__main__":
    unittest.main()
