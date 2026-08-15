import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS_DIR / "verify_release_with_production_art.py"
SPEC = importlib.util.spec_from_file_location("verify_release_with_production_art", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class ReleaseWithProductionArtTests(unittest.TestCase):
    def test_combined_preflight_requires_matching_art_smoke_and_release_fingerprints(self):
        candidate = {"gitSha": "a" * 40, "apkSha256": "b" * 64}
        art = {"verdict": "PRODUCTION_ART_GATE_PASSED", "verified": False}
        smoke = {
            "verdict": "PASSABLE_FOR_MANUAL_REVIEW",
            "verified": False,
            "apkSha256": "b" * 64,
            "blockers": [],
        }
        release = {
            "eligibleForManualPublication": True,
            "verified": False,
            "candidate": candidate,
        }
        with mock.patch.object(MODULE.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(MODULE.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(MODULE.verify_p1_production_art, "verify_art_manifest", return_value=art), \
             mock.patch.object(MODULE.analyze_device_smoke, "analyze", return_value=smoke), \
             mock.patch.object(MODULE.verify_release_publication, "verify_publication", return_value=release):
            result = MODULE.verify_release_with_art(
                candidate_manifest_path=Path("candidate.json"),
                apk_path=None,
                session_dir=Path("session"),
                review_bundle_dir=Path("review"),
                approvals_path=Path("approvals.json"),
                gate_spec_path=Path("spec.json"),
                production_art_manifest_path=Path("art.json"),
                production_art_spec_path=Path("art-spec.json"),
                repo_root=Path("."),
                performance_tier="mid",
            )
        self.assertTrue(result["eligibleForManualPublication"])
        self.assertFalse(result["verified"])
        self.assertEqual("MID", result["performanceTier"])
        self.assertEqual("PASSABLE_FOR_MANUAL_REVIEW", result["uper006SmokeMetrics"]["verdict"])

    def test_smoke_apk_mismatch_is_rejected(self):
        candidate = {"gitSha": "a" * 40, "apkSha256": "b" * 64}
        art = {"verdict": "PRODUCTION_ART_GATE_PASSED", "verified": False}
        smoke = {
            "verdict": "PASSABLE_FOR_MANUAL_REVIEW",
            "verified": False,
            "apkSha256": "c" * 64,
            "blockers": [],
        }
        with mock.patch.object(MODULE.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(MODULE.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(MODULE.verify_p1_production_art, "verify_art_manifest", return_value=art), \
             mock.patch.object(MODULE.analyze_device_smoke, "analyze", return_value=smoke), \
             mock.patch.object(MODULE.verify_release_publication, "verify_publication") as release:
            with self.assertRaisesRegex(MODULE.ReleaseWithProductionArtError, "smoke APK SHA does not match"):
                MODULE.verify_release_with_art(
                    candidate_manifest_path=Path("candidate.json"),
                    apk_path=None,
                    session_dir=Path("session"),
                    review_bundle_dir=Path("review"),
                    approvals_path=Path("approvals.json"),
                    gate_spec_path=Path("spec.json"),
                    production_art_manifest_path=Path("art.json"),
                    production_art_spec_path=Path("art-spec.json"),
                    repo_root=Path("."),
                    performance_tier="low",
                )
            release.assert_not_called()


if __name__ == "__main__":
    unittest.main()
