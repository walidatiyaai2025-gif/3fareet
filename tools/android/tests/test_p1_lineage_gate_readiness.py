import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1]
TESTS = Path(__file__).resolve().parent


def load(name: str, path: Path):
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    sys.modules[name] = module
    spec.loader.exec_module(module)
    return module


FIXTURE = load("p1_review_fixture_for_gate", TESTS / "test_p1_review_lineage_binding.py")
MODULE = load("p1_lineage_gate_readiness", TOOLS / "p1_lineage_gate_readiness.py")

TASKS = ["UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"]
FINAL_GATES = ["UVEH-012", "URAC-012", "UPER-006", "UPER-009", "UPER-010"]
STAGING_SHA = FIXTURE.STAGING_SHA
CANDIDATE_SHA = FIXTURE.CANDIDATE_SHA
APK_SHA = FIXTURE.APK_SHA
SERIAL_SHA = FIXTURE.SERIAL_SHA
AUTHORIZATION = dict(FIXTURE.AUTHORIZATION)


def write_json(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def complete_checkpoint_session(root: Path) -> Path:
    session = FIXTURE.build_session(root)
    spec = json.loads((TOOLS / "p1_gate_spec.json").read_text(encoding="utf-8"))
    labels = []
    for task_id, gate in spec["gates"].items():
        if task_id == "UPER-010":
            continue
        labels.extend(gate["requiredCheckpoints"])
    assert len(labels) == 16 and len(set(labels)) == 16

    checkpoints_root = session / "checkpoints"
    if checkpoints_root.exists():
        for child in list(checkpoints_root.iterdir()):
            if child.is_dir():
                import shutil
                shutil.rmtree(child)

    for label in labels:
        checkpoint = checkpoints_root / label
        checkpoint.mkdir(parents=True, exist_ok=True)
        checkpoint_json = {
            "schemaVersion": 1,
            "label": label,
            "apkSha256": APK_SHA,
            "deviceSerialSha256": SERIAL_SHA,
            "automatedRedFlags": [],
            "automatedRedFlagCount": 0,
            "manualReviewRequired": True,
            "files": [
                "screen.png",
                "logcat.txt",
                "meminfo.txt",
                "gfxinfo.txt",
                "thermalservice.txt",
                "battery.txt",
                "activity.txt",
            ],
        }
        write_json(checkpoint / "checkpoint.json", checkpoint_json)
        (checkpoint / "screen.png").write_bytes(b"\x89PNG\r\n\x1a\nP1-GATE-" + label.encode("utf-8"))
        for name in ("meminfo.txt", "gfxinfo.txt", "thermalservice.txt", "battery.txt"):
            (checkpoint / name).write_text(f"safe {label} {name}\n", encoding="utf-8")
        (checkpoint / "logcat.txt").write_text(f"private {FIXTURE.SERIAL} {label}\n", encoding="utf-8")
        (checkpoint / "activity.txt").write_text(f"private {FIXTURE.SERIAL} {label}\n", encoding="utf-8")

    index_path = session / "evidence-index.json"
    index = json.loads(index_path.read_text(encoding="utf-8"))
    index["checkpointCount"] = 16
    index["checkpoints"] = labels
    index["automatedRedFlagCount"] = 0
    index["automatedRedFlags"] = []
    write_json(index_path, index)
    return session


def make_p1_review(root: Path, session: Path) -> Path:
    bundle = root / "p1-review"
    FIXTURE.EXPORT.export_p1_bundle(session, bundle)
    return bundle


def make_generic_review(root: Path, session: Path) -> Path:
    bundle = root / "generic-review"
    FIXTURE.EXPORT.export_device_evidence.export_bundle(session, bundle)
    return bundle


def make_approvals(module, spec, binding, *, approved: bool) -> dict:
    payload = module._approval_template(spec, binding)
    for task_id in payload["approvals"]:
        payload["approvals"][task_id] = {
            "approved": approved,
            "reviewer": "P1 Reviewer" if approved else "",
        }
    return payload


class P1LineageGateReadinessTests(unittest.TestCase):
    def test_generic_review_bundle_is_insufficient_for_p1_gate_binding(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            generic = make_generic_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "P1 review bundle verification failed"):
                MODULE._bind_p1_review(session, generic, spec)

    def test_complete_16_checkpoint_p1_review_creates_all_false_fingerprint_template(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)
            self.assertTrue(binding["evidenceOnly"]["allEvidenceReady"])
            self.assertEqual(16, len(binding["evidenceOnly"]["capturedCheckpoints"]))
            self.assertEqual(AUTHORIZATION, binding["stagingAuthorization"])

            payload = MODULE._approval_template(spec, binding)
            self.assertEqual(2, payload["schemaVersion"])
            self.assertEqual("p1-lineage-manual-approvals-v2", payload["approvalProfile"])
            self.assertEqual(CANDIDATE_SHA, payload["candidateGitSha"])
            self.assertEqual(STAGING_SHA, payload["stagingSourceGitSha"])
            self.assertEqual(APK_SHA, payload["apkSha256"])
            self.assertEqual(AUTHORIZATION, payload["stagingAuthorization"])
            self.assertRegex(payload["reviewContentSetSha256"], r"^[0-9a-f]{64}$")
            self.assertRegex(payload["p1ReviewLineageSha256"], r"^[0-9a-f]{64}$")
            self.assertEqual("mid", payload["performanceTier"])
            self.assertEqual(TASKS, payload["coveredVisualRuntimeTasks"])
            self.assertEqual(
                {"p1Manifest", "stagingReport", "stagingLineage", "candidateManifest"},
                set(payload["sourceArtifactDigests"]),
            )
            self.assertEqual(FINAL_GATES, list(payload["approvals"]))
            self.assertTrue(all(record == {"approved": False, "reviewer": ""} for record in payload["approvals"].values()))
            self.assertFalse(payload["verified"])
            self.assertFalse(payload["publicationEligible"])

    def test_no_approvals_is_evidence_ready_but_not_release_review_ready(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)
            result = MODULE.evaluate_p1(spec, binding, None)
            self.assertTrue(result["allEvidenceReady"])
            self.assertFalse(result["releaseReviewReady"])
            self.assertTrue(result["p1ReviewBundleBound"])
            self.assertEqual(AUTHORIZATION, result["stagingAuthorization"])
            for task_id in FINAL_GATES[:-1]:
                self.assertEqual("EVIDENCE_READY_FOR_MANUAL_REVIEW", result["gates"][task_id]["status"])
            self.assertEqual("BLOCKED_RELEASE_GATE", result["gates"]["UPER-010"]["status"])
            self.assertFalse(result["verified"])
            self.assertFalse(result["runtimeVerified"])
            self.assertFalse(result["ownerAccepted"])
            self.assertFalse(result["publicationEligible"])

    def test_all_five_exact_approvals_can_reach_release_review_ready_but_never_verify(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)
            approvals = make_approvals(MODULE, spec, binding, approved=True)
            loaded = MODULE.load_p1_approvals_from_payload(approvals, spec) if hasattr(MODULE, "load_p1_approvals_from_payload") else approvals
            result = MODULE.evaluate_p1(spec, binding, loaded)
            self.assertTrue(result["allEvidenceReady"])
            self.assertTrue(result["releaseReviewReady"])
            self.assertEqual(AUTHORIZATION, result["stagingAuthorization"])
            for task_id in FINAL_GATES[:-1]:
                self.assertEqual("MANUALLY_APPROVED", result["gates"][task_id]["status"])
                self.assertTrue(result["gates"][task_id]["manualApproved"])
            self.assertTrue(result["gates"]["UPER-010"]["manualApproved"])
            self.assertEqual("READY_FOR_RELEASE_REVIEW", result["gates"]["UPER-010"]["status"])
            self.assertFalse(result["verified"])
            self.assertFalse(result["publicationEligible"])

    def test_approval_staging_or_review_lineage_fingerprint_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)

            approvals = make_approvals(MODULE, spec, binding, approved=True)
            approvals["stagingSourceGitSha"] = "d" * 40
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "stagingSourceGitSha"):
                MODULE.evaluate_p1(spec, binding, approvals)

            approvals = make_approvals(MODULE, spec, binding, approved=True)
            approvals["p1ReviewLineageSha256"] = "e" * 64
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "p1ReviewLineageSha256"):
                MODULE.evaluate_p1(spec, binding, approvals)

    def test_approval_authorization_fingerprint_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)

            for key in ("handoffPacketSha256", "nativeHandoffVerificationSha256", "operatorChainSha256"):
                approvals = make_approvals(MODULE, spec, binding, approved=True)
                approvals["stagingAuthorization"][key] = "1" * 64
                with self.assertRaisesRegex(MODULE.P1LineageGateError, "stagingAuthorization"):
                    MODULE.evaluate_p1(spec, binding, approvals)

            approvals = make_approvals(MODULE, spec, binding, approved=True)
            approvals["stagingAuthorization"]["authorizationSourceGitSha"] = "d" * 40
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "authorizationSourceGitSha"):
                MODULE.evaluate_p1(spec, binding, approvals)

    def test_approval_performance_tier_and_source_digest_mismatch_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)

            approvals = make_approvals(MODULE, spec, binding, approved=True)
            approvals["performanceTier"] = "high"
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "performanceTier mismatch"):
                MODULE.evaluate_p1(spec, binding, approvals)

            approvals = make_approvals(MODULE, spec, binding, approved=True)
            approvals["sourceArtifactDigests"]["stagingReport"] = "f" * 64
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "source artifact digest mismatch"):
                MODULE.evaluate_p1(spec, binding, approvals)

    def test_generic_schema_v2_approval_file_without_p1_profile_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            generic_approvals = root / "generic-approvals.json"
            write_json(
                generic_approvals,
                {
                    "schemaVersion": 2,
                    "gitSha": CANDIDATE_SHA,
                    "apkSha256": APK_SHA,
                    "reviewContentSetSha256": "d" * 64,
                    "approvals": {task: {"approved": True, "reviewer": "Reviewer"} for task in FINAL_GATES},
                },
            )
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "approval profile"):
                MODULE.load_p1_approvals(generic_approvals, spec)

    def test_incomplete_checkpoint_evidence_cannot_create_p1_approval_template(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = FIXTURE.build_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)
            self.assertFalse(binding["evidenceOnly"]["allEvidenceReady"])
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "complete clean evidence"):
                MODULE._approval_template(spec, binding)

    def test_p1_approval_file_loader_requires_exact_five_records_and_never_self_verifies(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            session = complete_checkpoint_session(root)
            review = make_p1_review(root, session)
            spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
            binding = MODULE._bind_p1_review(session, review, spec)
            payload = make_approvals(MODULE, spec, binding, approved=False)
            path = root / "p1-approvals.json"
            write_json(path, payload)
            loaded = MODULE.load_p1_approvals(path, spec)
            self.assertEqual(payload, loaded)

            payload["verified"] = True
            write_json(path, payload)
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "verified"):
                MODULE.load_p1_approvals(path, spec)

            payload = make_approvals(MODULE, spec, binding, approved=False)
            payload["approvals"].pop("UPER-010")
            write_json(path, payload)
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "exactly the five"):
                MODULE.load_p1_approvals(path, spec)

            payload = make_approvals(MODULE, spec, binding, approved=False)
            payload.pop("stagingAuthorization")
            write_json(path, payload)
            with self.assertRaisesRegex(MODULE.P1LineageGateError, "stagingAuthorization"):
                MODULE.load_p1_approvals(path, spec)


if __name__ == "__main__":
    unittest.main()
