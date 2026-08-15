import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]


def _load_module(name: str, filename: str):
    path = TOOLS_DIR / filename
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


PREFLIGHT = _load_module("verify_release_publication", "verify_release_publication.py")


class VerifyReleasePublicationTests(unittest.TestCase):
    def _fixture(self, root: Path):
        git_sha = "c" * 40
        apk_bytes = b"exact-unity-apk-bytes"
        apk_sha = hashlib.sha256(apk_bytes).hexdigest()
        review_sha = "d" * 64
        device_sha = "e" * 64

        apk = root / "afareet-unity3d-debug.apk"
        apk.write_bytes(apk_bytes)
        manifest = {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": git_sha,
            "packageId": "com.fiftysolutions.afareetunity3d",
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "apk": {
                "path": str(apk),
                "fileName": apk.name,
                "sizeBytes": len(apk_bytes),
                "sha256": apk_sha,
            },
        }
        manifest_path = root / "local-candidate-manifest.json"
        manifest_path.write_text(json.dumps(manifest, sort_keys=True) + "\n", encoding="utf-8")
        manifest_sha = PREFLIGHT.sha256_file(manifest_path)

        spec = PREFLIGHT.p1_gate_readiness.load_spec(PREFLIGHT.p1_gate_readiness.DEFAULT_SPEC)
        labels = []
        for task_id, gate in spec["gates"].items():
            if task_id != "UPER-010":
                labels.extend(gate["requiredCheckpoints"])

        session = root / "device-session"
        session.mkdir()
        (session / "candidate-manifest.json").write_bytes(manifest_path.read_bytes())
        session_candidate = {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": git_sha,
            "apkSha256": apk_sha,
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
            "manifest": {
                "fileName": "candidate-manifest.json",
                "sourceFileName": manifest_path.name,
                "sha256": manifest_sha,
            },
        }
        (session / "session.json").write_text(
            json.dumps({"apk": {"sha256": apk_sha}, "candidate": session_candidate}),
            encoding="utf-8",
        )
        (session / "evidence-index.json").write_text(
            json.dumps(
                {
                    "schemaVersion": 1,
                    "apkSha256": apk_sha,
                    "device": {"isEmulator": False},
                    "automatedRedFlagCount": 0,
                    "checkpoints": labels,
                }
            ),
            encoding="utf-8",
        )

        approvals = {
            "schemaVersion": 2,
            "gitSha": git_sha,
            "apkSha256": apk_sha,
            "reviewContentSetSha256": review_sha,
            "approvals": {
                task_id: {"approved": True, "reviewer": f"reviewer-{task_id.lower()}"}
                for task_id in spec["gates"]
            },
        }
        approvals_path = root / "manual-approvals.json"
        approvals_path.write_text(json.dumps(approvals, sort_keys=True) + "\n", encoding="utf-8")

        review_result = {
            "gitSha": git_sha,
            "apkSha256": apk_sha,
            "deviceSerialSha256": device_sha,
            "checkpointCount": len(labels),
            "contentSetSha256": review_sha,
            "verdict": "MANUAL_REVIEW_REQUIRED",
            "verified": False,
        }
        return {
            "git_sha": git_sha,
            "apk_sha": apk_sha,
            "review_sha": review_sha,
            "apk": apk,
            "manifest": manifest_path,
            "session": session,
            "review": root / "review-bundle",
            "approvals": approvals_path,
            "spec": PREFLIGHT.p1_gate_readiness.DEFAULT_SPEC,
            "review_result": review_result,
        }

    def _verify(self, fixture):
        with mock.patch.object(
            PREFLIGHT.p1_gate_readiness,
            "verify_review_bundle",
            return_value=fixture["review_result"],
        ):
            return PREFLIGHT.verify_publication(
                candidate_manifest_path=fixture["manifest"],
                apk_path=fixture["apk"],
                session_dir=fixture["session"],
                review_bundle_dir=fixture["review"],
                approvals_path=fixture["approvals"],
                spec_path=fixture["spec"],
            )

    def test_complete_exact_chain_is_eligible_but_never_verified(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = self._fixture(Path(directory))
            result = self._verify(fixture)

            self.assertTrue(result["eligibleForManualPublication"])
            self.assertFalse(result["verified"])
            self.assertEqual("ELIGIBLE_FOR_MANUAL_PUBLICATION", result["verdict"])
            self.assertEqual(fixture["git_sha"], result["candidate"]["gitSha"])
            self.assertEqual(fixture["apk_sha"], result["candidate"]["apkSha256"])
            self.assertEqual(fixture["review_sha"], result["evidence"]["reviewContentSetSha256"])
            self.assertEqual("READY_FOR_RELEASE_REVIEW", result["releaseGate"]["status"])
            self.assertEqual(5, len(result["evidence"]["reviewers"]))

    def test_missing_one_human_approval_blocks_publication(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = self._fixture(Path(directory))
            approvals = json.loads(fixture["approvals"].read_text(encoding="utf-8"))
            approvals["approvals"]["UPER-009"] = {"approved": False, "reviewer": ""}
            fixture["approvals"].write_text(json.dumps(approvals), encoding="utf-8")

            with self.assertRaisesRegex(PREFLIGHT.PublicationPreflightError, "READY_FOR_RELEASE_REVIEW"):
                self._verify(fixture)

    def test_candidate_manifest_bytes_must_match_device_bound_copy(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = self._fixture(Path(directory))
            manifest = json.loads(fixture["manifest"].read_text(encoding="utf-8"))
            manifest["notes"] = ["changed after device review"]
            fixture["manifest"].write_text(json.dumps(manifest, sort_keys=True) + "\n", encoding="utf-8")

            with self.assertRaisesRegex(PREFLIGHT.PublicationPreflightError, "manifest bytes do not match"):
                self._verify(fixture)

    def test_tampered_apk_is_rejected_before_readiness(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = self._fixture(Path(directory))
            fixture["apk"].write_bytes(fixture["apk"].read_bytes() + b"tamper")

            with self.assertRaisesRegex(
                PREFLIGHT.prepare_candidate_device.CandidatePrepareError,
                "size mismatch|SHA-256 mismatch",
            ):
                self._verify(fixture)

    def test_review_bundle_verification_failure_blocks_publication(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = self._fixture(Path(directory))
            with mock.patch.object(
                PREFLIGHT.p1_gate_readiness,
                "verify_review_bundle",
                side_effect=RuntimeError("review bundle verification failed: screenshot tampered"),
            ):
                with self.assertRaisesRegex(RuntimeError, "screenshot tampered"):
                    PREFLIGHT.verify_publication(
                        candidate_manifest_path=fixture["manifest"],
                        apk_path=fixture["apk"],
                        session_dir=fixture["session"],
                        review_bundle_dir=fixture["review"],
                        approvals_path=fixture["approvals"],
                        spec_path=fixture["spec"],
                    )

    def test_approval_fingerprint_mismatch_blocks_publication(self):
        with tempfile.TemporaryDirectory() as directory:
            fixture = self._fixture(Path(directory))
            approvals = json.loads(fixture["approvals"].read_text(encoding="utf-8"))
            approvals["reviewContentSetSha256"] = "f" * 64
            fixture["approvals"].write_text(json.dumps(approvals), encoding="utf-8")

            with self.assertRaisesRegex(PREFLIGHT.PublicationPreflightError, "READY_FOR_RELEASE_REVIEW"):
                self._verify(fixture)


if __name__ == "__main__":
    unittest.main()