import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock

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
        self.apk_sha = "a" * 64
        self.git_sha = "c" * 40
        self.review_sha = "d" * 64
        self.candidate = {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": self.git_sha,
            "apkSha256": self.apk_sha,
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "manifest": {
                "fileName": "candidate-manifest.json",
                "sourceFileName": "local-candidate-manifest.json",
                "sha256": "b" * 64,
            },
        }
        self.index = {
            "schemaVersion": 1,
            "apkSha256": self.apk_sha,
            "candidate": dict(self.candidate),
            "device": {"isEmulator": False},
            "automatedRedFlagCount": 0,
            "checkpoints": list(self.labels),
            "reviewBundleBound": True,
            "reviewContentSetSha256": self.review_sha,
        }

    def approvals(
        self,
        *,
        apk_sha=None,
        git_sha=None,
        review_sha=None,
        schema_version=module.APPROVALS_SCHEMA_VERSION,
    ):
        return {
            "schemaVersion": schema_version,
            "gitSha": git_sha or self.git_sha,
            "apkSha256": apk_sha or self.apk_sha,
            "reviewContentSetSha256": review_sha or self.review_sha,
            "approvals": {
                task_id: {"approved": True, "reviewer": "qa-owner"}
                for task_id in self.spec["gates"]
            },
        }

    def source_manifest(self):
        return {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": self.git_sha,
            "packageId": "com.fiftysolutions.afareetunity3d",
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "apk": {
                "fileName": "afareet-unity3d-debug.apk",
                "sha256": self.apk_sha,
                "sizeBytes": 123,
            },
        }

    def _write_bound_session(self, session: Path) -> None:
        source_manifest = self.source_manifest()
        manifest_path = session / module.BOUND_MANIFEST_FILE
        manifest_path.write_text(
            json.dumps(source_manifest, sort_keys=True) + "\n", encoding="utf-8"
        )
        manifest_sha = hashlib.sha256(manifest_path.read_bytes()).hexdigest()
        candidate = dict(self.candidate)
        candidate["manifest"] = dict(candidate["manifest"])
        candidate["manifest"]["sha256"] = manifest_sha

        index_for_file = dict(self.index)
        index_for_file.pop("candidate")
        index_for_file.pop("reviewBundleBound")
        index_for_file.pop("reviewContentSetSha256")
        (session / module.INDEX_FILE).write_text(
            json.dumps(index_for_file), encoding="utf-8"
        )
        (session / module.SESSION_FILE).write_text(
            json.dumps({"apk": {"sha256": self.apk_sha}, "candidate": candidate}),
            encoding="utf-8",
        )

    def test_spec_contains_exactly_five_gates(self):
        self.assertEqual(5, len(self.spec["gates"]))
        self.assertIn("UPER-010", self.spec["gates"])

    def test_complete_capture_without_approvals_stays_manual(self):
        result = module.evaluate(self.spec, self.index)
        self.assertTrue(result["candidateBound"])
        self.assertTrue(result["allEvidenceReady"])
        self.assertFalse(result["releaseReviewReady"])
        self.assertFalse(result["verified"])
        for task_id in ("UVEH-012", "URAC-012", "UPER-006", "UPER-009"):
            self.assertEqual(
                "EVIDENCE_READY_FOR_MANUAL_REVIEW",
                result["gates"][task_id]["status"],
            )

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

    def test_approval_apk_sha_must_match_session(self):
        result = module.evaluate(self.spec, self.index, self.approvals(apk_sha="e" * 64))
        self.assertFalse(result["gates"]["UVEH-012"]["manualApproved"])
        self.assertIn("APK SHA", result["gates"]["UVEH-012"]["approvalDetail"])
        self.assertFalse(result["releaseReviewReady"])

    def test_approval_git_sha_must_match_candidate(self):
        result = module.evaluate(self.spec, self.index, self.approvals(git_sha="e" * 40))
        self.assertFalse(result["gates"]["UVEH-012"]["manualApproved"])
        self.assertIn("Git SHA", result["gates"]["UVEH-012"]["approvalDetail"])
        self.assertFalse(result["releaseReviewReady"])

    def test_approval_review_fingerprint_must_match_verified_bundle(self):
        result = module.evaluate(self.spec, self.index, self.approvals(review_sha="e" * 64))
        self.assertFalse(result["gates"]["UVEH-012"]["manualApproved"])
        self.assertIn("review-content SHA", result["gates"]["UVEH-012"]["approvalDetail"])
        self.assertFalse(result["releaseReviewReady"])

    def test_approval_cannot_pass_without_verified_review_bundle_binding(self):
        index = dict(self.index)
        index["reviewBundleBound"] = False
        index.pop("reviewContentSetSha256")
        result = module.evaluate(self.spec, index, self.approvals())
        self.assertTrue(result["allEvidenceReady"])
        self.assertFalse(result["reviewBundleBound"])
        self.assertFalse(result["gates"]["UVEH-012"]["manualApproved"])
        self.assertFalse(result["releaseReviewReady"])

    def test_direct_apk_session_cannot_satisfy_final_gates(self):
        index = dict(self.index)
        index.pop("candidate")
        result = module.evaluate(self.spec, index, self.approvals())
        self.assertFalse(result["candidateBound"])
        self.assertFalse(result["allEvidenceReady"])
        self.assertFalse(result["releaseReviewReady"])
        blockers = result["gates"]["UPER-010"]["blockers"]
        self.assertTrue(any("candidate provenance is missing" in item for item in blockers))

    def test_candidate_apk_sha_mismatch_blocks_release_review(self):
        index = dict(self.index)
        bad_candidate = dict(self.candidate)
        bad_candidate["apkSha256"] = "e" * 64
        index["candidate"] = bad_candidate
        result = module.evaluate(self.spec, index, self.approvals())
        self.assertFalse(result["candidateBound"])
        self.assertFalse(result["releaseReviewReady"])
        self.assertTrue(any("does not match evidence index" in item for item in result["gates"]["UPER-010"]["blockers"]))

    def test_all_schema_v2_approvals_make_release_review_ready_but_never_verified(self):
        result = module.evaluate(self.spec, self.index, self.approvals())
        self.assertTrue(result["candidateBound"])
        self.assertTrue(result["reviewBundleBound"])
        self.assertEqual(self.review_sha, result["reviewContentSetSha256"])
        self.assertTrue(result["allEvidenceReady"])
        self.assertTrue(result["releaseReviewReady"])
        self.assertEqual("READY_FOR_RELEASE_REVIEW", result["gates"]["UPER-010"]["status"])
        self.assertFalse(result["verified"])

    def test_legacy_schema_v1_approval_file_is_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            path = Path(temp) / "approvals.json"
            legacy = {
                "schemaVersion": 1,
                "apkSha256": self.apk_sha,
                "approvals": {},
            }
            path.write_text(json.dumps(legacy), encoding="utf-8")
            with self.assertRaisesRegex(RuntimeError, "schemaVersion 1 is no longer accepted"):
                module.load_approvals(path)

    def test_validate_binds_verified_review_bundle_to_schema_v2_approvals(self):
        with tempfile.TemporaryDirectory() as temp:
            session = Path(temp) / "session"
            session.mkdir()
            self._write_bound_session(session)
            approvals_path = session / "approvals.json"
            approvals_path.write_text(json.dumps(self.approvals()), encoding="utf-8")
            output = session / "readiness.json"
            args = type("Args", (), {
                "spec": str(SPEC_PATH),
                "session": str(session),
                "review_bundle": str(Path(temp) / "review"),
                "approvals": str(approvals_path),
                "output": str(output),
            })()

            review_result = {
                "gitSha": self.git_sha,
                "apkSha256": self.apk_sha,
                "contentSetSha256": self.review_sha,
                "verdict": "MANUAL_REVIEW_REQUIRED",
                "verified": False,
            }
            with mock.patch.object(module, "verify_review_bundle", return_value=review_result):
                self.assertEqual(0, module.command_validate(args))

            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(payload["candidateBound"])
            self.assertTrue(payload["reviewBundleBound"])
            self.assertEqual(self.review_sha, payload["reviewContentSetSha256"])
            self.assertTrue(payload["releaseReviewReady"])
            self.assertFalse(payload["verified"])

    def test_review_bundle_verifier_failure_blocks_approvals_but_preserves_evidence_readiness(self):
        with tempfile.TemporaryDirectory() as temp:
            session = Path(temp) / "session"
            session.mkdir()
            self._write_bound_session(session)
            approvals_path = session / "approvals.json"
            approvals_path.write_text(json.dumps(self.approvals()), encoding="utf-8")
            output = session / "readiness.json"
            args = type("Args", (), {
                "spec": str(SPEC_PATH),
                "session": str(session),
                "review_bundle": str(Path(temp) / "review"),
                "approvals": str(approvals_path),
                "output": str(output),
            })()

            with mock.patch.object(
                module,
                "verify_review_bundle",
                side_effect=RuntimeError("review bundle verification failed: tampered"),
            ):
                self.assertEqual(2, module.command_validate(args))

            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(payload["candidateBound"])
            self.assertTrue(payload["allEvidenceReady"])
            self.assertFalse(payload["reviewBundleBound"])
            self.assertFalse(payload["releaseReviewReady"])
            self.assertIn("tampered", payload["reviewBundleBindingError"])

    def test_tampered_bound_manifest_blocks_release_review(self):
        with tempfile.TemporaryDirectory() as temp:
            session = Path(temp)
            self._write_bound_session(session)
            manifest_path = session / module.BOUND_MANIFEST_FILE
            manifest_path.write_text('{"tampered": true}\n', encoding="utf-8")
            output = session / "readiness.json"
            args = type("Args", (), {
                "spec": str(SPEC_PATH),
                "session": str(session),
                "review_bundle": None,
                "approvals": None,
                "output": str(output),
            })()
            self.assertEqual(2, module.command_validate(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertFalse(payload["candidateBound"])
            self.assertFalse(payload["releaseReviewReady"])


if __name__ == "__main__":
    unittest.main()