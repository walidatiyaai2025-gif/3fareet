import importlib.util
import sys
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/p1_programming_closure_audit.py"

spec = importlib.util.spec_from_file_location("p1_programming_closure_audit", MODULE_PATH)
module = importlib.util.module_from_spec(spec)
assert spec.loader is not None
sys.modules[spec.name] = module
spec.loader.exec_module(module)


class ProgrammingClosureAuditTests(unittest.TestCase):
    def setUp(self):
        self.register_text = module.REGISTER.read_text(encoding="utf-8")
        self.policy_text = module.ASSET_POLICY.read_text(encoding="utf-8")

    def test_live_register_reports_no_explicit_programming_queue(self):
        result = module.audit(self.register_text, self.policy_text)
        self.assertEqual("PROGRAMMING_CLOSURE_QUEUE_CLEAR", result["status"])
        self.assertEqual(65, result["task_total"])
        self.assertEqual(54, result["in_review"])
        self.assertEqual(11, result["blocked_external_only"])
        self.assertEqual(0, result["explicit_programming_queue"])
        self.assertTrue(result["verified_unchanged"])
        self.assertFalse(result["p1_status_promoted"])

    def test_known_external_blocker_set_is_exact_and_stable(self):
        self.assertEqual(
            {
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
            },
            set(module.KNOWN_EXTERNAL_BLOCKERS),
        )

    def test_audit_fails_if_todo_programming_queue_returns(self):
        mutated = self.register_text.replace(
            "| U3D-001 | P0 | إنشاء Unity project مستقل داخل المستودع | Principal Mobile Game Architect | IN REVIEW |",
            "| U3D-001 | P0 | إنشاء Unity project مستقل داخل المستودع | Principal Mobile Game Architect | TODO |",
            1,
        )
        with self.assertRaisesRegex(RuntimeError, "explicit-programming-queue"):
            module.audit(mutated, self.policy_text)

    def test_audit_fails_if_unknown_blocker_appears(self):
        mutated = self.register_text.replace(
            "| U3D-001 | P0 | إنشاء Unity project مستقل داخل المستودع | Principal Mobile Game Architect | IN REVIEW |",
            "| U3D-001 | P0 | إنشاء Unity project مستقل داخل المستودع | Principal Mobile Game Architect | BLOCKED |",
            1,
        )
        with self.assertRaisesRegex(RuntimeError, "unexpected-blocked-task"):
            module.audit(mutated, self.policy_text)

    def test_source_never_mutates_task_or_issue_state(self):
        source = MODULE_PATH.read_text(encoding="utf-8")
        for forbidden in (
            "update_issue(",
            "state=\"closed\"",
            "VERIFIED =",
            "mark_verified",
        ):
            self.assertNotIn(forbidden, source)


if __name__ == "__main__":
    unittest.main()
