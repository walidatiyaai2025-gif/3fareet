import importlib.util
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]


def _load_module():
    path = TOOLS_DIR / "verify_release_with_production_art.py"
    spec = importlib.util.spec_from_file_location("verify_release_with_production_art", path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


WRAPPER = _load_module()


class ReleaseWithProductionArtTests(unittest.TestCase):
    def _kwargs(self):
        return {
            "candidate_manifest_path": Path("candidate.json"),
            "apk_path": Path("game.apk"),
            "session_dir": Path("session"),
            "review_bundle_dir": Path("review"),
            "approvals_path": Path("approvals.json"),
            "gate_spec_path": Path("p1_gate_spec.json"),
            "production_art_manifest_path": Path("p1-production-art.json"),
            "production_art_spec_path": Path("p1_production_art_spec.json"),
            "repo_root": Path("."),
        }

    def _candidate(self):
        return {
            "gitSha": "a" * 40,
            "apkSha256": "b" * 64,
        }

    def test_success_requires_both_gates_and_never_verifies(self):
        candidate = self._candidate()
        art = {
            "verdict": WRAPPER.verify_p1_production_art.PASS_VERDICT,
            "verified": False,
            "candidate": candidate,
            "acceptedTasks": ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"],
        }
        release = {
            "eligibleForManualPublication": True,
            "verified": False,
            "candidate": candidate,
            "verdict": "ELIGIBLE_FOR_MANUAL_PUBLICATION",
        }
        with mock.patch.object(WRAPPER.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(WRAPPER.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(WRAPPER.verify_p1_production_art, "verify_art_manifest", return_value=art), \
             mock.patch.object(WRAPPER.verify_release_publication, "verify_publication", return_value=release):
            result = WRAPPER.verify_release_with_art(**self._kwargs())

        self.assertTrue(result["eligibleForManualPublication"])
        self.assertFalse(result["verified"])
        self.assertEqual("ELIGIBLE_FOR_MANUAL_PUBLICATION_WITH_PRODUCTION_ART", result["verdict"])
        self.assertEqual(candidate, result["candidate"])

    def test_art_failure_blocks_before_release_preflight(self):
        candidate = self._candidate()
        with mock.patch.object(WRAPPER.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(WRAPPER.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(
                 WRAPPER.verify_p1_production_art,
                 "verify_art_manifest",
                 side_effect=WRAPPER.verify_p1_production_art.ProductionArtGateError("trackProcedural is active"),
             ), \
             mock.patch.object(WRAPPER.verify_release_publication, "verify_publication") as release_mock:
            with self.assertRaisesRegex(WRAPPER.verify_p1_production_art.ProductionArtGateError, "trackProcedural"):
                WRAPPER.verify_release_with_art(**self._kwargs())
            release_mock.assert_not_called()

    def test_release_fingerprint_must_match_art_candidate(self):
        candidate = self._candidate()
        art = {
            "verdict": WRAPPER.verify_p1_production_art.PASS_VERDICT,
            "verified": False,
            "candidate": candidate,
        }
        release = {
            "eligibleForManualPublication": True,
            "verified": False,
            "candidate": {"gitSha": "c" * 40, "apkSha256": candidate["apkSha256"]},
        }
        with mock.patch.object(WRAPPER.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(WRAPPER.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(WRAPPER.verify_p1_production_art, "verify_art_manifest", return_value=art), \
             mock.patch.object(WRAPPER.verify_release_publication, "verify_publication", return_value=release):
            with self.assertRaisesRegex(WRAPPER.ReleaseWithProductionArtError, "Git SHA does not match"):
                WRAPPER.verify_release_with_art(**self._kwargs())

    def test_release_preflight_not_eligible_is_rejected(self):
        candidate = self._candidate()
        art = {
            "verdict": WRAPPER.verify_p1_production_art.PASS_VERDICT,
            "verified": False,
            "candidate": candidate,
        }
        release = {
            "eligibleForManualPublication": False,
            "verified": False,
            "candidate": candidate,
        }
        with mock.patch.object(WRAPPER.prepare_candidate_device, "read_json", return_value={}), \
             mock.patch.object(WRAPPER.prepare_candidate_device, "resolve_candidate", return_value=candidate), \
             mock.patch.object(WRAPPER.verify_p1_production_art, "verify_art_manifest", return_value=art), \
             mock.patch.object(WRAPPER.verify_release_publication, "verify_publication", return_value=release):
            with self.assertRaisesRegex(WRAPPER.ReleaseWithProductionArtError, "not eligible"):
                WRAPPER.verify_release_with_art(**self._kwargs())


if __name__ == "__main__":
    unittest.main()
