import importlib.util
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS_DIR / "verify_release_with_production_art.py"
SPEC = importlib.util.spec_from_file_location("verify_release_with_production_art_smoke_contract", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class ReleaseWithSmokeContractTests(unittest.TestCase):
    def test_authoritative_wrapper_requires_performance_tier(self):
        parser = MODULE.build_parser()
        actions = {action.dest: action for action in parser._actions}
        self.assertIn("performance_tier", actions)
        self.assertTrue(actions["performance_tier"].required)
        self.assertEqual(("low", "mid", "high"), actions["performance_tier"].choices)

    def test_blocked_smoke_metrics_abort_before_publication_preflight(self):
        candidate = {"gitSha": "a" * 40, "apkSha256": "b" * 64}
        art = {"verdict": "PRODUCTION_ART_GATE_PASSED", "verified": False}
        smoke = {
            "verdict": "BLOCKED",
            "verified": False,
            "apkSha256": "b" * 64,
            "blockers": ["smoke-warm-race: frameP95Ms exceeds budget"],
        }
        with mock.patch.object(MODULE.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(MODULE.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(MODULE.verify_p1_production_art, "verify_art_manifest", return_value=art), \
             mock.patch.object(MODULE.analyze_device_smoke, "analyze", return_value=smoke), \
             mock.patch.object(MODULE.verify_release_publication, "verify_publication") as release:
            with self.assertRaisesRegex(MODULE.ReleaseWithProductionArtError, "UPER-006 Android-observable smoke metrics are blocked"):
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
                    performance_tier="mid",
                )
            release.assert_not_called()


if __name__ == "__main__":
    unittest.main()
