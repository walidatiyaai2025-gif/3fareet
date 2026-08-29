import importlib.util
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SCRIPT = REPO_ROOT / "tools/blender/validate_afareet_king_production.py"
POLICY = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs"
SPEC = importlib.util.spec_from_file_location("validate_afareet_king_production", SCRIPT)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class HeroBlenderProductionPreflightTests(unittest.TestCase):
    @staticmethod
    def valid_totals():
        return {
            0: {
                "meshObjects": 5,
                "vertices": 4200,
                "triangles": 6500,
                "uv0MissingObjects": [],
                "unappliedScaleObjects": [],
            },
            1: {
                "meshObjects": 5,
                "vertices": 2200,
                "triangles": 3000,
                "uv0MissingObjects": [],
                "unappliedScaleObjects": [],
            },
            2: {
                "meshObjects": 5,
                "vertices": 1400,
                "triangles": 1700,
                "uv0MissingObjects": [],
                "unappliedScaleObjects": [],
            },
        }

    @staticmethod
    def valid_wheel_check():
        return {
            "required": ["FL", "FR", "RL", "RR"],
            "found": ["FL", "FR", "RL", "RR"],
            "unclassifiedMeshObjects": [],
            "errors": [],
            "passed": True,
        }

    def test_reads_current_authoritative_unity_dual_budget_policy(self):
        policy = MODULE.parse_policy(POLICY)
        self.assertEqual(policy["MinimumVertices"], [1500, 800, 500])
        self.assertEqual(policy["VertexBudgets"], [5000, 2800, 1800])
        self.assertEqual(policy["MinimumTriangles"], [3500, 1600, 900])
        self.assertEqual(policy["TriangleBudgets"], [7500, 4000, 2500])

    def test_triangle_pass_cannot_hide_vertex_budget_failure(self):
        policy = MODULE.parse_policy(POLICY)
        totals = self.valid_totals()
        totals[0]["vertices"] = 6162
        totals[0]["triangles"] = 6495
        result = MODULE.evaluate_lod_totals(totals, policy)
        self.assertFalse(result["technicalPreflightPassed"])
        lod0 = result["lods"][0]
        self.assertFalse(lod0["verticesWithinRange"])
        self.assertTrue(lod0["trianglesWithinRange"])
        self.assertIn("LOD0 vertex count 6162 is outside [1500, 5000]", result["errors"])

    def test_v6_brand_object_name_is_detected_without_relaxing_lod_rules(self):
        check = MODULE.detect_branding_stamp(
            ["AK_Body_LOD0", "AK_3FREET_HoodStamp_TextCurve.001", "AK_Wheel_FL_LOD0"]
        )
        self.assertTrue(check["passed"])
        self.assertEqual(check["requiredToken"], "3FREET")
        self.assertEqual(check["matchedObjects"], ["AK_3FREET_HoodStamp_TextCurve.001"])
        self.assertIsNone(MODULE.classify_lod("AK_3FREET_HoodStamp_TextCurve.001"))

    def test_missing_brand_stamp_blocks_report(self):
        policy = MODULE.parse_policy(POLICY)
        evaluation = MODULE.evaluate_lod_totals(self.valid_totals(), policy)
        report = MODULE.build_report(
            evaluation,
            source_file="AfareetKing_Production.blend",
            wheel_check=self.valid_wheel_check(),
            branding_check=MODULE.detect_branding_stamp(["AK_Body_LOD0"]),
        )
        self.assertFalse(report["technicalPreflightPassed"])
        self.assertEqual(report["verdict"], "TECHNICAL_PREFLIGHT_BLOCKED")
        self.assertFalse(report["brandingCheck"]["passed"])
        self.assertTrue(any("3FREET" in error for error in report["errors"]))

    def test_technical_pass_never_promotes_production_or_owner_acceptance(self):
        policy = MODULE.parse_policy(POLICY)
        evaluation = MODULE.evaluate_lod_totals(self.valid_totals(), policy)
        report = MODULE.build_report(
            evaluation,
            source_file="AfareetKing_Production.blend",
            wheel_check=self.valid_wheel_check(),
            branding_check=MODULE.detect_branding_stamp(["AK_3FREET_HoodStamp_LOD0"]),
        )
        self.assertTrue(report["technicalPreflightPassed"])
        self.assertEqual(report["schemaVersion"], 2)
        self.assertEqual(report["verdict"], "TECHNICAL_PREFLIGHT_PASS_NOT_PRODUCTION_ACCEPTANCE")
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

    def test_blender_execution_counts_evaluated_mesh_and_real_triangles(self):
        source = SCRIPT.read_text(encoding="utf-8")
        for required in (
            "evaluated_get(depsgraph)",
            "evaluated.to_mesh()",
            "mesh.calc_loop_triangles()",
            "len(mesh.vertices)",
            "len(mesh.loop_triangles)",
            "len(mesh.uv_layers)",
            "_scale_is_applied(obj.scale)",
            "evaluated.to_mesh_clear()",
            "brandingCheck",
            "TECHNICAL_PREFLIGHT_ONLY_OWNER_LICENSE_VISUAL_UNITY_DEVICE_GATES_REQUIRED",
        ):
            self.assertIn(required, source)
        self.assertNotIn('"productionGate": True', source)
        self.assertNotIn('"verified": True', source)


if __name__ == "__main__":
    unittest.main()
