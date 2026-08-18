import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
WORKFLOW = REPO_ROOT / ".github/workflows/p1-production-art-gate.yml"


class P1ProductionArtWorkflowCoverageContractTests(unittest.TestCase):
    def test_pr_and_main_push_triggers_cover_authoritative_art_and_publication_chain(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        required_paths = (
            "tools/android/fingerprint_p1_production_art_manifest.py",
            "tools/android/verify_p1_production_art.py",
            "tools/android/verify_release_with_production_art.py",
            "tools/android/verify_release_publication.py",
            "tools/android/p1_gate_readiness.py",
            "tools/android/p1_gate_spec.json",
            "tools/android/prepare_candidate_device.py",
            "tools/android/verify_device_review_bundle.py",
            "tools/android/p1_production_art_spec.json",
            "tools/android/tests/test_p1_production_art_source_policy_contract.py",
            "tools/android/tests/test_prepare_candidate_device.py",
            "tools/android/tests/test_verify_device_review_bundle.py",
            "tools/android/tests/test_p1_gate_readiness.py",
            "tools/android/tests/test_verify_release_publication.py",
            "tools/android/tests/test_verify_release_with_production_art.py",
            "tools/android/tests/test_p1_production_art_workflow_coverage_contract.py",
        )
        for relative in required_paths:
            self.assertEqual(
                2,
                text.count(f"- '{relative}'"),
                f"{relative} must trigger both pull_request and main push coverage",
            )

    def test_workflow_executes_source_policy_and_release_chain_regressions(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        for test_name in (
            "test_p1_production_art_source_policy_contract.py",
            "test_prepare_candidate_device.py",
            "test_verify_device_review_bundle.py",
            "test_p1_gate_readiness.py",
            "test_verify_release_publication.py",
            "test_verify_release_with_production_art.py",
            "test_verify_release_with_smoke_contract.py",
            "test_p1_production_art_workflow_coverage_contract.py",
        ):
            self.assertIn(
                f"python3 -m unittest discover -s tools/android/tests -p '{test_name}' -v",
                text,
            )

    def test_workflow_syntax_checks_transitive_release_sources(self):
        text = WORKFLOW.read_text(encoding="utf-8")
        for relative in (
            "tools/android/verify_release_publication.py",
            "tools/android/p1_gate_readiness.py",
            "tools/android/prepare_candidate_device.py",
            "tools/android/verify_device_review_bundle.py",
        ):
            self.assertIn(relative + " \\", text)

        for cli in (
            "verify_release_publication.py --help",
            "p1_gate_readiness.py --help",
            "prepare_candidate_device.py --help",
            "verify_device_review_bundle.py --help",
        ):
            self.assertIn(cli, text)


if __name__ == "__main__":
    unittest.main()
