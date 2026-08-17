import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


ROOT = Path(__file__).resolve().parents[3]
TOOLS = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS / "p1_repository_state_consistency.py"
SPEC = importlib.util.spec_from_file_location("p1_repository_state_consistency", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules["p1_repository_state_consistency"] = MODULE
SPEC.loader.exec_module(MODULE)

PROJECT_STATUS = ROOT / "docs" / "PROJECT_STATUS.md"
TASK_REGISTER = ROOT / "docs" / "tasks" / "06-UNITY-3D-MIGRATION.md"

LEDGER = """## Purpose
Operational source of truth for the fixed **65-task Unity U-P1 register**.

## Aggregate state
`IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65`

## Blocked tasks — unchanged 11
1. UART-003 — real Hero production model + licensed binding/render proof (#127)
2. UART-004 — licensed Rival production prefab binding/runtime/owner proof (#128)
3. UART-005 — licensed runtime/device/owner proof (#128)
4. UART-006 — licensed landmark runtime/device/owner proof (#128)
5. UART-007 — licensed dressing runtime/device/owner proof (#128)
6. URAC-011 — exact-candidate runtime/device/owner proof (#128)
7. UVEH-012 — real-device driving-feel acceptance
8. URAC-012 — physical-device lap/results/restart verification
9. UPER-006 — Android smoke/profiler/performance matrix
10. UPER-009 — owner/Art Director Visual Gate
11. UPER-010 — manual publication approval, last

## Guardrails
Do not self-verify.
"""


class P1RepositoryStateConsistencyTests(unittest.TestCase):
    def _project(self) -> str:
        return PROJECT_STATUS.read_text(encoding="utf-8")

    def _register(self) -> str:
        return TASK_REGISTER.read_text(encoding="utf-8")

    def test_repository_snapshot_matches_fixed_issue_90_contract(self):
        result = MODULE.verify_consistency(LEDGER, self._project(), self._register())
        self.assertEqual("P1_REPOSITORY_STATE_CONSISTENT", result["state"])
        self.assertEqual(54, result["ledger"]["inReview"])
        self.assertEqual(11, result["ledger"]["blocked"])
        self.assertEqual(65, result["ledger"]["total"])
        self.assertEqual(54, result["taskRegister"]["inReview"])
        self.assertEqual(11, result["taskRegister"]["blocked"])
        self.assertFalse(result["taskStateMutationPerformed"])
        self.assertFalse(result["publicationPerformed"])
        self.assertFalse(result["verified"])
        self.assertFalse(result["runtimeVerified"])
        self.assertFalse(result["ownerAccepted"])
        self.assertFalse(result["publicationEligible"])

    def test_project_status_aggregate_drift_fails_closed(self):
        drifted = self._project().replace(
            "IN REVIEW 54 | READY 0 | TODO 0 | BLOCKED 11 = 65",
            "IN REVIEW 53 | READY 1 | TODO 0 | BLOCKED 11 = 65",
            1,
        )
        with self.assertRaisesRegex(MODULE.P1RepositoryStateError, "PROJECT_STATUS.md aggregate"):
            MODULE.verify_consistency(LEDGER, drifted, self._register())

    def test_project_status_blocker_identity_drift_fails_closed(self):
        drifted = self._project().replace(
            "1. UART-003 — real Hero production model",
            "1. UART-099 — real Hero production model",
            1,
        )
        with self.assertRaisesRegex(MODULE.P1RepositoryStateError, "blocker identity/order"):
            MODULE.verify_consistency(LEDGER, drifted, self._register())

    def test_task_register_ready_or_todo_regression_fails_closed(self):
        drifted = self._register().replace("| IN REVIEW |", "| READY |", 1)
        with self.assertRaisesRegex(MODULE.P1RepositoryStateError, "non-operational P1 states"):
            MODULE.verify_consistency(LEDGER, self._project(), drifted)

    def test_task_register_blocker_set_drift_fails_closed(self):
        drifted = self._register().replace(
            "| UVEH-012 | P0 | Real-device driving feel pass | Gameplay Lead | BLOCKED |",
            "| UVEH-012 | P0 | Real-device driving feel pass | Gameplay Lead | IN REVIEW |",
            1,
        )
        with self.assertRaisesRegex(MODULE.P1RepositoryStateError, "aggregate disagrees"):
            MODULE.verify_consistency(LEDGER, self._project(), drifted)

    def test_cli_output_is_non_overwriting_and_never_promotes_state(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ledger = root / "issue-90.md"
            project = root / "PROJECT_STATUS.md"
            register = root / "06-UNITY-3D-MIGRATION.md"
            output = root / "result.json"
            ledger.write_text(LEDGER, encoding="utf-8")
            project.write_text(self._project(), encoding="utf-8")
            register.write_text(self._register(), encoding="utf-8")
            args = [
                "--ledger",
                str(ledger),
                "--project-status",
                str(project),
                "--task-register",
                str(register),
                "--output",
                str(output),
            ]
            self.assertEqual(0, MODULE.main(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertFalse(payload["taskStateMutationPerformed"])
            self.assertFalse(payload["publicationPerformed"])
            self.assertFalse(payload["verified"])
            self.assertEqual(1, MODULE.main(args))


if __name__ == "__main__":
    unittest.main()
