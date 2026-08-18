import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PLAN = REPO_ROOT / "docs/qa/P1_FINAL_5_GATE_PLAN.md"


class P1FinalGatePlanCurrentStateContractTests(unittest.TestCase):
    def test_plan_preserves_fixed_65_task_54_11_ledger(self):
        text = PLAN.read_text(encoding="utf-8")
        self.assertIn("IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65", text)
        self.assertNotIn("IN REVIEW 60 | READY 0 | TODO 0 | BLOCKED 5 = 65", text)
        self.assertIn("6 production-visual/runtime + final 5", text)

        for task_id in (
            "UART-003",
            "UART-004",
            "UART-005",
            "UART-006",
            "UART-007",
            "URAC-011",
            "UVEH-012",
            "URAC-012",
            "UPER-006",
            "UPER-009",
            "UPER-010",
        ):
            self.assertIn(task_id, text)

    def test_plan_uses_convergence_for_preintegration_licensed_candidate(self):
        text = PLAN.read_text(encoding="utf-8")
        self.assertIn("candidate_ref`: `agent/p1-remediation-convergence`", text)
        self.assertIn("git reset --hard origin/agent/p1-remediation-convergence", text)
        self.assertIn("before #144 is allowed to merge", text)
        self.assertIn("actual licensed job must execute", text)
        self.assertIn("licensed job `SKIPPED` is not licensed proof", text)
        self.assertNotIn("Enter the then-current reviewed full SHA of `agent/unblock-final-5`", text)
        self.assertNotIn("git reset --hard origin/agent/unblock-final-5", text)

    def test_plan_requires_production_art_chain_before_final_publication(self):
        text = PLAN.read_text(encoding="utf-8")
        for required in (
            "P1_PRODUCTION_ART_FINGERPRINTING.md",
            "P1_PRODUCTION_ART_GATE.md",
            "fingerprint_p1_production_art_manifest.py",
            "verify_p1_production_art.py",
            "verify_release_with_production_art.py",
            "ELIGIBLE_FOR_MANUAL_PUBLICATION_WITH_PRODUCTION_ART_AND_SMOKE_METRICS",
            "UPER-010 remains the release owner's final manual publication decision",
        ):
            self.assertIn(required, text)

        self.assertIn("Do **not** use `verify_release_publication.py` alone as the final P1 publication command", text)
        self.assertIn("verified=false", text)

    def test_plan_keeps_uart004_evidence_bound_to_exact_exchange_sources(self):
        text = PLAN.read_text(encoding="utf-8")
        self.assertIn("exact three UART-004 production exchange OBJ files", text)
        self.assertIn("exactly the three deterministic production exchange OBJ files", text)
        self.assertIn("all three production Rival prefabs", text)

    def test_plan_blocks_integration_until_same_candidate_clears_all_11(self):
        text = PLAN.read_text(encoding="utf-8")
        self.assertIn("same exact candidate", text)
        self.assertIn("all 11 blockers", text)
        self.assertIn("PR #144 may merge convergence into `agent/unblock-final-5`", text)
        self.assertIn("PR #112 may later advance toward `main`", text)
        self.assertIn("Do not merge #144/#112, publish/tag, or update Last Verified while any blocker remains", text)


if __name__ == "__main__":
    unittest.main()
