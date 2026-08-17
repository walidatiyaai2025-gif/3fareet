import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO / "tools" / "android" / "p1_visual_source_readiness.py"
SPEC = importlib.util.spec_from_file_location("p1_visual_source_readiness", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class P1VisualSourceReadinessTests(unittest.TestCase):
    def test_current_repo_has_five_source_ready_tasks_and_only_hero_source_blocked(self):
        report = MODULE.audit_visual_sources(REPO, hero_source=None)
        self.assertEqual("BLOCKED", report["state"])
        self.assertEqual(5, report["sourceReadyCount"])
        self.assertEqual(1, report["blockedCount"])
        self.assertEqual(["UART-003"], report["blockedTaskIds"])
        states = {item["taskId"]: item["state"] for item in report["tasks"]}
        for task_id in ("UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"):
            self.assertEqual("SOURCE_READY_FOR_LICENSED_RUNTIME_PROOF", states[task_id])

    def test_every_task_preserves_runtime_owner_and_verification_boundary(self):
        report = MODULE.audit_visual_sources(REPO, hero_source=None)
        self.assertFalse(report["verified"])
        self.assertFalse(report["runtimeVerified"])
        self.assertFalse(report["ownerAccepted"])
        self.assertFalse(report["publicationEligible"])
        for task in report["tasks"]:
            self.assertFalse(task["verified"])
            self.assertFalse(task["runtimeVerified"])
            self.assertFalse(task["ownerAccepted"])

    def test_scope_is_exactly_the_six_visual_runtime_blockers(self):
        report = MODULE.audit_visual_sources(REPO, hero_source=None)
        self.assertEqual(
            ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"],
            report["scope"],
        )

    def test_report_output_is_artifacts_only_and_never_overwrites(self):
        report = MODULE.audit_visual_sources(REPO, hero_source=None)
        with tempfile.TemporaryDirectory(dir=REPO / "artifacts") as temp:
            output = Path(temp) / "visual-source-readiness.json"
            MODULE._write_report(REPO, output, report)
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual("BLOCKED", payload["state"])
            with self.assertRaises(ValueError):
                MODULE._write_report(REPO, output, report)
        with self.assertRaises(ValueError):
            MODULE._write_report(REPO, REPO / "docs" / "visual-source-readiness.json", report)

    def test_cli_exposes_no_verification_or_approval_switch(self):
        text = MODULE_PATH.read_text(encoding="utf-8")
        self.assertNotIn("--verified", text)
        self.assertNotIn("--approve", text)
        self.assertNotIn("verified = True", text)
        self.assertNotIn("ownerAccepted = True", text)


if __name__ == "__main__":
    unittest.main()
