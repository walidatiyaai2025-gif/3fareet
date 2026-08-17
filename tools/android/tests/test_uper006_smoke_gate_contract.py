import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uper006SmokeGateContractTests(unittest.TestCase):
    def test_authoritative_release_wrapper_enforces_smoke_analyzer(self):
        wrapper = (REPO_ROOT / "tools/android/verify_release_with_production_art.py").read_text(encoding="utf-8")
        analyzer = (REPO_ROOT / "tools/android/analyze_device_smoke.py").read_text(encoding="utf-8")
        prepare = (REPO_ROOT / "tools/android/prepare_candidate_device.py").read_text(encoding="utf-8")
        workflow = (REPO_ROOT / ".github/workflows/p1-production-art-gate.yml").read_text(encoding="utf-8")

        self.assertIn("import analyze_device_smoke", wrapper)
        self.assertIn("requested_tier = performance_tier.lower()", wrapper)
        self.assertIn("analyze_device_smoke.analyze(session_dir, requested_tier)", wrapper)
        self.assertIn('smoke.get("verdict") == "PASSABLE_FOR_MANUAL_REVIEW"', wrapper)
        self.assertIn('smoke.get("sessionPerformanceTier")', wrapper)
        self.assertIn("smoke session performance tier does not match requested release tier", wrapper)
        self.assertIn("--performance-tier", wrapper)

        self.assertIn("performanceTier", analyzer)
        self.assertIn("sessionPerformanceTier", analyzer)
        self.assertIn("performanceTier mismatch", analyzer)
        self.assertIn("PASSABLE_FOR_MANUAL_REVIEW", analyzer)
        self.assertIn("restartPssGrowthPercent", analyzer)
        self.assertIn("SEVERE_THERMAL_STATUS = 3", analyzer)
        self.assertIn('session["performanceTier"] = tier', prepare)
        self.assertIn('"--performance-tier"', prepare)

        self.assertIn("tools/android/analyze_device_smoke.py", workflow)
        self.assertIn("test_analyze_device_smoke.py", workflow)


if __name__ == "__main__":
    unittest.main()
