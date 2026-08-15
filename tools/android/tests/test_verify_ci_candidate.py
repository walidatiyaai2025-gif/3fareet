import hashlib
import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "verify_ci_candidate.py"
SPEC = importlib.util.spec_from_file_location("verify_ci_candidate", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
VERIFY = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VERIFY)

CiCandidateError = VERIFY.CiCandidateError
verify_ci_candidate = VERIFY.verify_ci_candidate


def metadata_for(apk: Path):
    data = apk.read_bytes()
    return {
        "schemaVersion": 1,
        "source": "github-actions-unity-production-ci",
        "artifact": "afareet-unity3d-debug.apk",
        "packageId": "com.fiftysolutions.afareetunity3d",
        "minSdk": 26,
        "abi": "arm64-v8a",
        "sha256": hashlib.sha256(data).hexdigest(),
        "sizeBytes": len(data),
        "gitSha": "a" * 40,
        "runId": "12345",
        "runAttempt": "1",
        "repository": "walidatiyaai2025-gif/3fareet",
        "workflow": "Unity Production CI",
        "eventName": "pull_request",
        "ref": "refs/pull/108/merge",
    }


class VerifyCiCandidateTests(unittest.TestCase):
    def make_apk(self, directory: str, content: bytes = b"ci-apk") -> Path:
        apk = Path(directory) / "afareet-unity3d-debug.apk"
        apk.write_bytes(content)
        return apk

    def test_valid_ci_candidate_is_device_ready_but_not_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = verify_ci_candidate(metadata_for(apk), apk)
            self.assertEqual(manifest["candidateType"], "github-actions-unity-ci")
            self.assertTrue(manifest["readyForDeviceEvidence"])
            self.assertFalse(manifest["verified"])
            self.assertEqual(manifest["githubRun"]["runId"], "12345")
            self.assertEqual(manifest["githubRun"]["repository"], "walidatiyaai2025-gif/3fareet")
            self.assertEqual(manifest["githubRun"]["workflow"], "Unity Production CI")

    def test_rejects_wrong_source_or_missing_run_identity(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            metadata = metadata_for(apk)
            metadata["source"] = "unknown"
            with self.assertRaisesRegex(CiCandidateError, "Unsupported CI metadata source"):
                verify_ci_candidate(metadata, apk)

            metadata = metadata_for(apk)
            metadata["runId"] = ""
            with self.assertRaisesRegex(CiCandidateError, "runId"):
                verify_ci_candidate(metadata, apk)

    def test_rejects_wrong_repository_workflow_event_or_ref(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)

            metadata = metadata_for(apk)
            metadata["repository"] = "other/repo"
            with self.assertRaisesRegex(CiCandidateError, "Unexpected GitHub repository"):
                verify_ci_candidate(metadata, apk)

            metadata = metadata_for(apk)
            metadata["workflow"] = "Other Workflow"
            with self.assertRaisesRegex(CiCandidateError, "Unexpected GitHub workflow"):
                verify_ci_candidate(metadata, apk)

            metadata = metadata_for(apk)
            metadata["eventName"] = "schedule"
            with self.assertRaisesRegex(CiCandidateError, "Unexpected GitHub eventName"):
                verify_ci_candidate(metadata, apk)

            metadata = metadata_for(apk)
            metadata["ref"] = ""
            with self.assertRaisesRegex(CiCandidateError, "refs/\*"):
                verify_ci_candidate(metadata, apk)

    def test_rejects_git_sha_mismatch_shape(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            metadata = metadata_for(apk)
            metadata["gitSha"] = "short"
            with self.assertRaisesRegex(CiCandidateError, "40-character SHA"):
                verify_ci_candidate(metadata, apk)

    def test_rejects_apk_hash_or_size_mismatch(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            metadata = metadata_for(apk)
            metadata["sha256"] = "0" * 64
            with self.assertRaisesRegex(CiCandidateError, "SHA-256 mismatch"):
                verify_ci_candidate(metadata, apk)

            metadata = metadata_for(apk)
            metadata["sizeBytes"] += 1
            with self.assertRaisesRegex(CiCandidateError, "size mismatch"):
                verify_ci_candidate(metadata, apk)


if __name__ == "__main__":
    unittest.main()
