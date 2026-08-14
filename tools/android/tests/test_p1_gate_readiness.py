import importlib.util
import json
import tempfile
import unittest
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MODULE_PATH = ROOT / "p1_gate_readiness.py"
SPEC_PATH = ROOT / "p1_gate_spec.json"

spec = importlib.util.spec_from_file_location("p1_gate_readiness", MODULE_PATH)
module = importlib.util.module_from_spec(spec)
assert spec.loader is not None
spec.loader.exec_module(module)


class P1GateReadinessTests(unittest.TestCase):
    def setUp(self):
        self.spec = module.load_spec(SPEC_PATH)
        self.labels = []
        for task_id, gate in self.spec["gates"].items():
            if task_id != "UPER-010":
                self.labels.extend(gate["requiredCheckpoints"])
        self.index = {
            "schemaVersion": 1,
            "apkSha256": "a" * 64,
            "device": {"isEmulator": False},
            "automatedRedFlagCount": 0,
            "checkpoints": list(self.labels),
        }

    def approvals(self, apk_sha=None):
        return {
            "schemaVersion": 1,
            "apkSha256": apk_sha or self.index["apkSha256"],
            "approvals": {
                task_id: {"approved": True, "reviewer": "qa-owner"}
                for task_id in self.spec["gates"]
            },
        }

    def test_spec_contains_exactly_five_gates(self):
        self.assertEqual(5, len(self.spec["gates"]))
        self.assertIn("UPER-010", self.spec["gates"])

    def test_complete_capture_without_approvals_stays_manual(self):
        result = module.evaluate(self.spec, self.index)
        self.assertTrue(result["allEvidenceReady"])
        self.assertFalse(result["releaseReviewReady"])
        self.assertFalse(result["verified"])
        for task_id in ("UVEH-012", "URAC-012", "UPER-006", "UPER-009"):
            self.assertEqual("EVIDENCE_READY_FOR_MANUAL_REVIEW", result["gates"][task_id]["status"])

    def test_missing_checkpoint_blocks_owning_gate(self):
        index = dict(self.index)
        index["checkpoints"] = [label for label in self.labels if label != "drift"]
        result = module.evaluate(self.spec, index)
        self.assertFalse(result["gates"]["UVEH-012"]["evidenceReady"])
        self.assertIn("drift", result["gates"]["UVEH-012"]["missingCheckpoints"])
        self.assertFalse(result["allEvidenceReady"])

    def test_emulator_and_red_flags_block_evidence(self):
        index = dict(self.index)
        index["device"] = {"isEmulator": True}
        index["automatedRedFlagCount"] = 2
        result = module.evaluate(self.spec, index, self.approvals())
        self.assertFalse(result["allEvidenceReady"])
        self.assertFalse(result["releaseReviewReady"])
        self.assertTrue(any("emulator" in item.lower() for item in result["gates"]["UPER-006"]["blockers"]))
        self.assertTrue(any("red flags" in item.lower() for item in result["gates"]["UPER-006"]["blockers"]))

    def test_approval_sha_must_match_session(self):
        result = module.evaluate(self.spec, self.index, self.approvals(apk_sha="b" * 64))
        self.assertFalse(result["gates"]["UVEH-012"]["manualApproved"])
        self.assertFalse(result["releaseReviewReady"])

    def test_all_explicit_approvals_make_release_review_ready_but_never_verified(self):
        result = module.evaluate(self.spec, self.index, self.approvals())
        self.assertTrue(result["allEvidenceReady"])
        self.assertTrue(result["releaseReviewReady"])
        self.assertEqual("READY_FOR_RELEASE_REVIEW", result["gates"]["UPER-010"]["status"])
        self.assertFalse(result["verified"])

    def test_validate_writes_readiness_file(self):
        with tempfile.TemporaryDirectory() as temp:
            session = Path(temp)
            (session / "evidence-index.json").write_text(json.dumps(self.index), encoding="utf-8")
            approvals_path = session / "approvals.json"
            approvals_path.write_text(json.dumps(self.approvals()), encoding="utf-8")
            output = session / "readiness.json"
            args = type("Args", (), {
                "spec": str(SPEC_PATH),
                "session": str(session),
                "approvals": str(approvals_path),
                "output": str(output),
            })()
            self.assertEqual(0, module.command_validate(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(payload["releaseReviewReady"])
            self.assertFalse(payload["verified"])


if __name__ == "__main__":
    unittest.main()
