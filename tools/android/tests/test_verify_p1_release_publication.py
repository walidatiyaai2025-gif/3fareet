import hashlib
import importlib.util
import json
import shutil
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


GATE_FIXTURE = load("p1_gate_fixture_for_publication", TESTS / "test_p1_lineage_gate_readiness.py")
MODULE = load("verify_p1_release_publication", TOOLS / "verify_p1_release_publication.py")

STAGING_SHA = GATE_FIXTURE.STAGING_SHA
CANDIDATE_SHA = GATE_FIXTURE.CANDIDATE_SHA
FINAL_GATES = ["UVEH-012", "URAC-012", "UPER-006", "UPER-009", "UPER-010"]
APK_BYTES = b"afareet-p1-publication-fixture-apk-v1"
APK_SHA = hashlib.sha256(APK_BYTES).hexdigest()


def write_json(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def build_publication_chain(root: Path) -> dict[str, Path]:
    # Reuse the complete 16-checkpoint P1 fixture, but bind it to real APK bytes so the
    # publication preflight can re-hash the exact artifact instead of relying on metadata only.
    GATE_FIXTURE.APK_SHA = APK_SHA
    GATE_FIXTURE.FIXTURE.APK_SHA = APK_SHA
    session = GATE_FIXTURE.complete_checkpoint_session(root)

    apk = root / "afareet-unity3d-debug.apk"
    apk.write_bytes(APK_BYTES)

    bound_candidate = session / "candidate-manifest.json"
    candidate_payload = json.loads(bound_candidate.read_text(encoding="utf-8"))
    candidate_payload["apk"]["path"] = str(apk)
    candidate_payload["apk"]["fileName"] = apk.name
    candidate_payload["apk"]["sizeBytes"] = apk.stat().st_size
    candidate_payload["apk"]["sha256"] = APK_SHA
    write_json(bound_candidate, candidate_payload)
    candidate_manifest_sha = hashlib.sha256(bound_candidate.read_bytes()).hexdigest()

    p1_manifest = session / "p1-staged-candidate-manifest.json"
    p1_payload = json.loads(p1_manifest.read_text(encoding="utf-8"))
    p1_payload["apkSha256"] = APK_SHA
    p1_payload["localCandidateManifest"]["path"] = str(bound_candidate)
    p1_payload["localCandidateManifest"]["sha256"] = candidate_manifest_sha
    write_json(p1_manifest, p1_payload)
    p1_manifest_sha = hashlib.sha256(p1_manifest.read_bytes()).hexdigest()

    session_path = session / "session.json"
    session_payload = json.loads(session_path.read_text(encoding="utf-8"))
    session_payload["apk"]["sha256"] = APK_SHA
    session_payload["candidate"]["apkSha256"] = APK_SHA
    session_payload["candidate"]["manifest"]["sha256"] = candidate_manifest_sha
    session_payload["p1Lineage"]["apkSha256"] = APK_SHA
    session_payload["p1Lineage"]["files"]["candidateManifest"]["sha256"] = candidate_manifest_sha
    session_payload["p1Lineage"]["files"]["p1Manifest"]["sha256"] = p1_manifest_sha
    write_json(session_path, session_payload)

    index_path = session / "evidence-index.json"
    index = json.loads(index_path.read_text(encoding="utf-8"))
    index["apkSha256"] = APK_SHA
    write_json(index_path, index)
    for checkpoint_path in (session / "checkpoints").glob("*/checkpoint.json"):
        checkpoint = json.loads(checkpoint_path.read_text(encoding="utf-8"))
        checkpoint["apkSha256"] = APK_SHA
        write_json(checkpoint_path, checkpoint)

    external_candidate = root / "publication-candidate-manifest.json"
    shutil.copy2(bound_candidate, external_candidate)

    review = GATE_FIXTURE.make_p1_review(root, session)
    spec = MODULE.p1_gate_readiness.load_spec(TOOLS / "p1_gate_spec.json")
    binding = MODULE.p1_lineage_gate_readiness._bind_p1_review(session, review, spec)
    approvals = MODULE.p1_lineage_gate_readiness._approval_template(spec, binding)
    for task_id in approvals["approvals"]:
        approvals["approvals"][task_id] = {"approved": True, "reviewer": f"Reviewer {task_id}"}
    approvals_path = root / "p1-lineage-approvals.json"
    write_json(approvals_path, approvals)

    return {
        "session": session,
        "review": review,
        "candidate": external_candidate,
        "apk": apk,
        "approvals": approvals_path,
    }


class VerifyP1ReleasePublicationTests(unittest.TestCase):
    def test_complete_lineage_bound_chain_is_eligible_but_never_published_or_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            result = MODULE.verify_p1_publication(
                candidate_manifest_path=chain["candidate"],
                apk_path=chain["apk"],
                session_dir=chain["session"],
                review_bundle_dir=chain["review"],
                approvals_path=chain["approvals"],
                spec_path=TOOLS / "p1_gate_spec.json",
            )
            self.assertEqual("P1_PUBLICATION_PREFLIGHT_PASSED", result["state"])
            self.assertEqual("P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION", result["verdict"])
            self.assertTrue(result["eligibleForExplicitManualPublication"])
            self.assertFalse(result["publicationPerformed"])
            self.assertFalse(result["verified"])
            self.assertEqual(CANDIDATE_SHA, result["candidate"]["gitSha"])
            self.assertEqual(APK_SHA, result["candidate"]["apkSha256"])
            self.assertEqual(STAGING_SHA, result["p1Lineage"]["stagingSourceGitSha"])
            self.assertEqual(STAGING_SHA, result["p1Lineage"]["directParentGitSha"])
            self.assertEqual(CANDIDATE_SHA, result["p1Lineage"]["candidateGitSha"])
            self.assertEqual(16, result["evidence"]["checkpointCount"])
            self.assertEqual(set(FINAL_GATES), set(result["evidence"]["reviewers"]))
            self.assertEqual("READY_FOR_RELEASE_REVIEW", result["releaseGate"]["status"])

    def test_generic_review_bundle_cannot_enter_p1_publication_preflight(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            generic_review = root / "generic-review"
            GATE_FIXTURE.FIXTURE.EXPORT.export_device_evidence.export_bundle(chain["session"], generic_review)
            with self.assertRaisesRegex(
                MODULE.p1_lineage_gate_readiness.P1LineageGateError,
                "P1 review bundle verification failed",
            ):
                MODULE.verify_p1_publication(
                    candidate_manifest_path=chain["candidate"],
                    apk_path=chain["apk"],
                    session_dir=chain["session"],
                    review_bundle_dir=generic_review,
                    approvals_path=chain["approvals"],
                    spec_path=TOOLS / "p1_gate_spec.json",
                )

    def test_one_missing_manual_approval_blocks_publication_preflight(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            approvals = json.loads(chain["approvals"].read_text(encoding="utf-8"))
            approvals["approvals"]["UPER-010"] = {"approved": False, "reviewer": ""}
            write_json(chain["approvals"], approvals)
            with self.assertRaisesRegex(MODULE.P1PublicationPreflightError, "READY_FOR_RELEASE_REVIEW"):
                MODULE.verify_p1_publication(
                    candidate_manifest_path=chain["candidate"],
                    apk_path=chain["apk"],
                    session_dir=chain["session"],
                    review_bundle_dir=chain["review"],
                    approvals_path=chain["approvals"],
                    spec_path=TOOLS / "p1_gate_spec.json",
                )

    def test_lineage_fingerprint_tamper_in_approvals_blocks_preflight(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            approvals = json.loads(chain["approvals"].read_text(encoding="utf-8"))
            approvals["p1ReviewLineageSha256"] = "f" * 64
            write_json(chain["approvals"], approvals)
            with self.assertRaises(MODULE.p1_lineage_gate_readiness.P1LineageGateError):
                MODULE.verify_p1_publication(
                    candidate_manifest_path=chain["candidate"],
                    apk_path=chain["apk"],
                    session_dir=chain["session"],
                    review_bundle_dir=chain["review"],
                    approvals_path=chain["approvals"],
                    spec_path=TOOLS / "p1_gate_spec.json",
                )

    def test_publication_candidate_manifest_bytes_must_match_session_bound_copy(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            payload = json.loads(chain["candidate"].read_text(encoding="utf-8"))
            payload["fixtureOnlyMutation"] = True
            write_json(chain["candidate"], payload)
            with self.assertRaisesRegex(MODULE.P1PublicationPreflightError, "candidate-manifest bytes"):
                MODULE.verify_p1_publication(
                    candidate_manifest_path=chain["candidate"],
                    apk_path=chain["apk"],
                    session_dir=chain["session"],
                    review_bundle_dir=chain["review"],
                    approvals_path=chain["approvals"],
                    spec_path=TOOLS / "p1_gate_spec.json",
                )

    def test_apk_tamper_is_rejected_before_publication_readiness(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            chain["apk"].write_bytes(APK_BYTES + b"tamper")
            with self.assertRaises(MODULE.prepare_candidate_device.CandidatePrepareError):
                MODULE.verify_p1_publication(
                    candidate_manifest_path=chain["candidate"],
                    apk_path=chain["apk"],
                    session_dir=chain["session"],
                    review_bundle_dir=chain["review"],
                    approvals_path=chain["approvals"],
                    spec_path=TOOLS / "p1_gate_spec.json",
                )

    def test_generic_schema_v2_approvals_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            generic = {
                "schemaVersion": 2,
                "gitSha": CANDIDATE_SHA,
                "apkSha256": APK_SHA,
                "reviewContentSetSha256": "d" * 64,
                "approvals": {task: {"approved": True, "reviewer": "Reviewer"} for task in FINAL_GATES},
            }
            write_json(chain["approvals"], generic)
            with self.assertRaisesRegex(MODULE.p1_lineage_gate_readiness.P1LineageGateError, "schemaVersion"):
                MODULE.verify_p1_publication(
                    candidate_manifest_path=chain["candidate"],
                    apk_path=chain["apk"],
                    session_dir=chain["session"],
                    review_bundle_dir=chain["review"],
                    approvals_path=chain["approvals"],
                    spec_path=TOOLS / "p1_gate_spec.json",
                )

    def test_cli_output_refuses_overwrite_and_never_claims_publication_performed(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            chain = build_publication_chain(root)
            output = root / "publication-preflight.json"
            args = [
                "--candidate-manifest", str(chain["candidate"]),
                "--apk", str(chain["apk"]),
                "--session", str(chain["session"]),
                "--review-bundle", str(chain["review"]),
                "--approvals", str(chain["approvals"]),
                "--output", str(output),
            ]
            self.assertEqual(0, MODULE.main(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertFalse(payload["publicationPerformed"])
            self.assertFalse(payload["verified"])
            self.assertEqual(2, MODULE.main(args))


if __name__ == "__main__":
    unittest.main()
