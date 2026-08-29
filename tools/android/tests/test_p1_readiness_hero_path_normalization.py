import importlib.util
import shutil
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
HELPER_PATH = Path(__file__).with_name("test_p1_licensed_staging_readiness.py")
SPEC = importlib.util.spec_from_file_location("p1_readiness_fixture", HELPER_PATH)
HELPER = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
previous_module = sys.modules.get(SPEC.name)
sys.modules[SPEC.name] = HELPER
try:
    SPEC.loader.exec_module(HELPER)
except BaseException:
    if previous_module is None:
        sys.modules.pop(SPEC.name, None)
    else:
        sys.modules[SPEC.name] = previous_module
    raise

MODULE = HELPER.MODULE
HERO_ASSET = "Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"
HERO_REPO_PATH = "unity_game/Assets/Afareet/ArtSource/Vehicles/Hero/AfareetKing_Production.fbx"


class P1ReadinessHeroPathNormalizationTests(unittest.TestCase):
    def test_dotted_assets_prefix_normalizes_identically_to_other_hero_gates(self):
        self.assertEqual(HERO_REPO_PATH, MODULE._normalize_hero_path("./" + HERO_ASSET))
        self.assertEqual(HERO_REPO_PATH, MODULE._normalize_hero_path("././" + HERO_ASSET))
        self.assertEqual(HERO_REPO_PATH, MODULE._normalize_hero_path(HERO_ASSET))
        self.assertEqual(HERO_REPO_PATH, MODULE._normalize_hero_path("./" + HERO_REPO_PATH))

    def test_complete_clean_fixture_accepts_dotted_assets_path_for_readiness(self):
        root = HELPER.make_fixture()
        try:
            report = MODULE.audit(root, hero_source="./" + HERO_ASSET)
            self.assertEqual("READY_FOR_LICENSED_STAGING", report["state"])
            self.assertTrue(report["readyForLicensedStaging"])
            self.assertEqual(HERO_REPO_PATH, report["heroSource"])
            self.assertEqual([], report["blockedCheckIds"])
            hero_check = next(
                item for item in report["checks"]
                if item["id"] == "UART-003_HERO_HANDOFF_PREFLIGHT"
            )
            self.assertEqual("PASS", hero_check["status"])
            self.assertIn("verdict=UNITY_INSPECTION_REQUIRED", hero_check["detail"])
            for key in (
                "candidateBuildStarted", "publicationEligible", "runtimeVerified",
                "ownerAccepted", "verified",
            ):
                self.assertFalse(report[key], key)
        finally:
            shutil.rmtree(root, ignore_errors=True)


if __name__ == "__main__":
    unittest.main()
