import hashlib
import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS / "p1_blocker_closure_audit.py"
SPEC = importlib.util.spec_from_file_location("p1_blocker_closure_audit", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules["p1_blocker_closure_audit"] = MODULE
SPEC.loader.exec_module(MODULE)


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
8. URAC-012 — physical-device lap/results/restart verification; source/PlayMode regression present but licensed/device proof pending
9. UPER-006 — Android smoke/profiler/performance matrix
10. UPER-009 — owner/Art Director Visual Gate
11. UPER-010 — manual publication approval, last

## Historical rejected candidate
Do not reuse it.
"""


def write_text(path: Path, text: str) -> None:
    path.write_text(text, encoding="utf-8")


def write_json(path: Path, payload) -> None:
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


class P1BlockerClosureAuditTests(unittest.TestCase):
    def _ledger(self, root: Path, text: str = LEDGER) -> Path:
        path = root / "issue-90.md"
        write_text(path, text)
        return path

    def test_authoritative_ledger_parses_exact_fixed_register_and_blocker_order(self):
        parsed = MODULE.parse_ledger(LEDGER)
        self.assertEqual(54, parsed["aggregate"]["inReview"])
        self.assertEqual(11, parsed["aggregate"]["blocked"])
        self.assertEqual(65, parsed["aggregate"]["total"])
        self.assertEqual(list(MODULE.EXPECTED_TASK_IDS), [item["taskId"] for item in parsed["blockers"]])

    def test_aggregate_or_blocker_identity_drift_fails_closed(self):
        cases = (
            LEDGER.replace("BLOCKED 11 = 65", "BLOCKED 10 = 64"),
            LEDGER.replace("6. URAC-011", "6. URAC-099"),
            LEDGER.replace("11. UPER-010 — manual publication approval, last\n", ""),
        )
        for text in cases:
            with self.subTest(text=text[-120:]):
                with self.assertRaises(MODULE.P1ClosureAuditError):
                    MODULE.parse_ledger(text)

    def test_no_evidence_index_reports_all_eleven_missing_without_state_mutation(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            result = MODULE.audit(self._ledger(root))
            self.assertEqual("P1_BLOCKER_EVIDENCE_MISSING", result["verdict"])
            self.assertEqual(11, result["summary"]["blockerCount"])
            self.assertEqual(0, result["summary"]["inventoryCompleteCount"])
            self.assertEqual(11, result["summary"]["missingEvidenceTaskCount"])
            self.assertFalse(result["taskStateMutationPerformed"])
            self.assertFalse(result["publicationPerformed"])
            self.assertFalse(result["verified"])
            self.assertFalse(result["runtimeVerified"])
            self.assertFalse(result["ownerAccepted"])
            self.assertFalse(result["publicationEligible"])
            self.assertTrue(all(not item["inventoryCompleteForHumanReview"] for item in result["tasks"]))
            self.assertTrue(all(item["taskStateMutationPerformed"] is False for item in result["tasks"]))

    def test_complete_single_task_inventory_still_never_closes_or_verifies_task(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence_root = root / "evidence"
            evidence_root.mkdir()
            records = []
            for index, evidence_type in enumerate(MODULE.REQUIRED_EVIDENCE["UART-003"], start=1):
                evidence_path = evidence_root / f"uart003-{index}.bin"
                evidence_path.write_bytes(f"evidence-{index}".encode("utf-8"))
                records.append(
                    {
                        "type": evidence_type,
                        "path": evidence_path.name,
                        "sha256": sha256(evidence_path),
                    }
                )
            index_path = root / "index.json"
            write_json(
                index_path,
                {
                    "schemaVersion": 1,
                    "state": "P1_BLOCKER_CLOSURE_EVIDENCE_INDEX",
                    "tasks": {"UART-003": records},
                },
            )

            result = MODULE.audit(
                self._ledger(root),
                evidence_index_path=index_path,
                evidence_root=evidence_root,
            )
            hero = result["tasks"][0]
            self.assertTrue(hero["inventoryCompleteForHumanReview"])
            self.assertEqual([], hero["missingEvidenceTypes"])
            self.assertEqual(1, result["summary"]["inventoryCompleteCount"])
            self.assertEqual(10, result["summary"]["missingEvidenceTaskCount"])
            self.assertFalse(hero["verified"])
            self.assertFalse(hero["taskStateMutationPerformed"])
            self.assertTrue(result["humanClosureReviewRequired"])
            self.assertFalse(result["verified"])

    def test_declared_evidence_hash_and_path_are_integrity_checked(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            evidence_root = root / "evidence"
            evidence_root.mkdir()
            evidence = evidence_root / "drive.txt"
            write_text(evidence, "physical device review")
            index_path = root / "index.json"
            payload = {
                "schemaVersion": 1,
                "state": "P1_BLOCKER_CLOSURE_EVIDENCE_INDEX",
                "tasks": {
                    "UVEH-012": [
                        {
                            "type": "physical-device-driving-feel-acceptance",
                            "path": evidence.name,
                            "sha256": "f" * 64,
                        }
                    ]
                },
            }
            write_json(index_path, payload)
            with self.assertRaisesRegex(MODULE.P1ClosureAuditError, "SHA-256 mismatch"):
                MODULE.audit(self._ledger(root), evidence_index_path=index_path, evidence_root=evidence_root)

            payload["tasks"]["UVEH-012"][0]["sha256"] = sha256(evidence)
            payload["tasks"]["UVEH-012"][0]["path"] = "../drive.txt"
            write_json(index_path, payload)
            with self.assertRaisesRegex(MODULE.P1ClosureAuditError, "escapes"):
                MODULE.audit(self._ledger(root), evidence_index_path=index_path, evidence_root=evidence_root)

    def test_unknown_task_type_or_duplicate_type_is_rejected(self):
        cases = (
            {
                "schemaVersion": 1,
                "state": "P1_BLOCKER_CLOSURE_EVIDENCE_INDEX",
                "tasks": {"FAKE-001": []},
            },
            {
                "schemaVersion": 1,
                "state": "P1_BLOCKER_CLOSURE_EVIDENCE_INDEX",
                "tasks": {"UPER-006": [{"type": "wrong", "path": "x", "sha256": "a" * 64}]},
            },
            {
                "schemaVersion": 1,
                "state": "P1_BLOCKER_CLOSURE_EVIDENCE_INDEX",
                "tasks": {
                    "UPER-006": [
                        {"type": "android-smoke-performance-matrix", "path": "a", "sha256": "a" * 64},
                        {"type": "android-smoke-performance-matrix", "path": "b", "sha256": "b" * 64},
                    ]
                },
            },
        )
        for payload in cases:
            with self.subTest(payload=payload), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                index_path = root / "index.json"
                write_json(index_path, payload)
                with self.assertRaises(MODULE.P1ClosureAuditError):
                    MODULE.load_evidence_index(index_path)

    def test_cli_require_complete_is_nonzero_and_output_is_never_overwritten(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            ledger = self._ledger(root)
            output = root / "audit.json"
            args = ["--ledger", str(ledger), "--output", str(output), "--require-complete"]
            self.assertEqual(2, MODULE.main(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(11, payload["summary"]["missingEvidenceTaskCount"])
            self.assertFalse(payload["verified"])
            self.assertEqual(2, MODULE.main(args))

    def test_source_is_read_only_and_does_not_mutate_release_or_status_pointers(self):
        text = MODULE_PATH.read_text(encoding="utf-8")
        for forbidden in (
            "subprocess",
            "os.system",
            "git push",
            "git tag",
            "gh release create",
            "gh release upload",
            "LAST_VERIFIED_APK.md",
            "PROJECT_STATUS.md",
        ):
            self.assertNotIn(forbidden, text)


if __name__ == "__main__":
    unittest.main()
