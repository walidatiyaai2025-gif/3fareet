import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
STATUS_ROOT = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit"


def load_json(name: str) -> dict:
    return json.loads((STATUS_ROOT / name).read_text(encoding="utf-8"))


class Uart005StatusSourceConsistencyTests(unittest.TestCase):
    def test_all_status_sources_preserve_blocked_unverified_truth(self):
        asset = load_json("ASSET_MANIFEST.json")
        clutter = load_json("ROADSIDE_CLUTTER_MANIFEST.json")
        clutter_runtime = load_json("ROADSIDE_CLUTTER_RUNTIME_STATUS.json")
        mobile = load_json("MOBILE_LOD_MANIFEST.json")
        mobile_runtime = load_json("MOBILE_LOD_RUNTIME_STATUS.json")

        for document in (asset, clutter, clutter_runtime, mobile, mobile_runtime):
            self.assertEqual("UART-005", document["taskId"])
            self.assertEqual("BLOCKED", document["reviewState"])

        self.assertFalse(asset["runtimeIntegrationVerified"])
        self.assertFalse(clutter["runtimeIntegrationVerified"])
        self.assertFalse(clutter_runtime["runtimeIntegrationVerified"])
        self.assertFalse(mobile["runtimeLodIntegrationVerified"])
        self.assertFalse(mobile_runtime["runtimeLodIntegrationVerified"])

        self.assertTrue(asset["runtimeIntegrationImplemented"])
        self.assertTrue(clutter["runtimeIntegrationImplemented"])
        self.assertTrue(clutter_runtime["runtimeIntegrationImplemented"])
        self.assertTrue(mobile["runtimeLodIntegrationImplemented"])
        self.assertTrue(mobile_runtime["runtimeLodIntegrationImplemented"])

        self.assertFalse(asset["runtimeIntegrated"])
        self.assertFalse(clutter["runtimeIntegrated"])
        self.assertFalse(clutter_runtime["runtimeIntegrated"])

    def test_completed_source_and_lod_counts_are_consistent(self):
        asset = load_json("ASSET_MANIFEST.json")
        clutter = load_json("ROADSIDE_CLUTTER_MANIFEST.json")
        clutter_runtime = load_json("ROADSIDE_CLUTTER_RUNTIME_STATUS.json")
        mobile = load_json("MOBILE_LOD_MANIFEST.json")
        mobile_runtime = load_json("MOBILE_LOD_RUNTIME_STATUS.json")

        self.assertEqual("10/10", asset["sourceSurfaceProgress"])
        self.assertEqual("3/3", clutter["sourceDeliveryProgress"])
        self.assertEqual("3/3", clutter_runtime["sourceDeliveryProgress"])
        self.assertEqual("13/13", mobile["moduleCoverage"])
        self.assertEqual("13/13", mobile_runtime["moduleCoverage"])
        self.assertEqual(26, mobile["distinctLodSourceAssets"])
        self.assertEqual(26, mobile_runtime["distinctLodSourceAssets"])
        self.assertEqual(13, len(mobile["modules"]))

        keys = {module["key"] for module in mobile["modules"]}
        self.assertTrue({"planter-a", "crates-a", "cafe-a", "road-a", "curb-a"}.issubset(keys))

    def test_completed_clutter_and_lod_work_is_not_listed_as_missing_expansion(self):
        asset = load_json("ASSET_MANIFEST.json")
        clutter = load_json("ROADSIDE_CLUTTER_MANIFEST.json")
        clutter_runtime = load_json("ROADSIDE_CLUTTER_RUNTIME_STATUS.json")

        expansion = "\n".join(asset["requiredProductionExpansion"])
        self.assertNotIn("additional roadside clutter", expansion)
        self.assertNotIn("mobile LOD setup", expansion)
        self.assertIn("normal/ORM", expansion)
        self.assertIn("landmark and skyline replacement", expansion)

        self.assertEqual(
            "implemented-unverified",
            asset["runtimeReplacementStatus"]["authoredRoadsideClutter"],
        )
        self.assertEqual(
            "implemented-unverified",
            asset["runtimeReplacementStatus"]["mobileLod13ModulePath"],
        )

        for pending_list in (clutter["acceptancePending"], clutter_runtime["acceptancePending"]):
            pending = "\n".join(pending_list)
            self.assertNotIn("stage all three clutter sources", pending)
            self.assertNotIn("mobile LOD authoring", pending)
            self.assertIn("licensed Unity", pending)
            self.assertIn("physical-device performance review", pending)
            self.assertIn("owner/Art Director Visual Gate acceptance", pending)

    def test_readme_reports_current_not_historical_implementation_state(self):
        readme = (STATUS_ROOT / "README.md").read_text(encoding="utf-8")
        for required in (
            "10/10 surfaced",
            "Authored roadside clutter — 3/3",
            "Mobile LOD source/runtime path — 13/13",
            "26 distinct secondary LOD source assets",
            "Road A — 150 → 36 → 12 triangles",
            "Curb A — 120 → 24 → 12 triangles",
            "author_uart005_mobile_lods_complete.py",
            "runtimeIntegrated=false",
            "runtimeIntegrationVerified=false",
        ):
            self.assertIn(required, readme)

        for stale in (
            "lamp: 130 vertices / 236 triangles",
            "barrier: 48 vertices / 72 triangles",
            "UV/material authoring, runtime prefab replacement",
            "authored source pass in progress",
        ):
            self.assertNotIn(stale, readme)


if __name__ == "__main__":
    unittest.main()
