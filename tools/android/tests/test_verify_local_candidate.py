import hashlib
import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "verify_local_candidate.py"
SPEC = importlib.util.spec_from_file_location("verify_local_candidate", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
VERIFY = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(VERIFY)

CandidateError = VERIFY.CandidateError
verify_candidate = VERIFY.verify_candidate
SHA = "a" * 40


def base_test_metadata():
    return {
        "source": "local-windows-licensed-unity-tests",
        "unityVersion": "6000.5.8f1",
        "gitSha": SHA,
        "gitBranch": "agent/unblock-final-5",
        "dirtyTree": False,
        "releaseEvidenceEligible": True,
        "editMode": {
            "result": "Passed",
            "total": 12,
            "passed": 12,
            "failed": 0,
            "skipped": 0,
        },
        "playMode": {
            "result": "Passed",
            "total": 5,
            "passed": 5,
            "failed": 0,
            "skipped": 0,
        },
    }


def build_metadata_for(apk: Path):
    data = apk.read_bytes()
    return {
        "artifact": "afareet-unity3d-debug.apk",
        "source": "local-windows-licensed-unity",
        "unityVersion": "6000.5.8f1",
        "packageId": "com.fiftysolutions.afareetunity3d",
        "minSdk": 26,
        "abi": "arm64-v8a",
        "sha256": hashlib.sha256(data).hexdigest(),
        "sizeBytes": len(data),
        "gitSha": SHA,
        "gitBranch": "agent/unblock-final-5",
        "gitDirty": False,
        "releaseEvidenceEligible": True,
    }


class VerifyLocalCandidateTests(unittest.TestCase):
    def make_apk(self, directory: str, content: bytes = b"fake-apk-content") -> Path:
        apk = Path(directory) / "afareet-unity3d-debug.apk"
        apk.write_bytes(content)
        return apk

    def test_valid_same_sha_candidate_is_ready_but_never_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = verify_candidate(base_test_metadata(), build_metadata_for(apk), apk)
            self.assertTrue(manifest["releaseEvidenceEligible"])
            self.assertTrue(manifest["readyForDeviceEvidence"])
            self.assertFalse(manifest["verified"])
            self.assertEqual(manifest["gitSha"], SHA)
            self.assertEqual(manifest["apk"]["sha256"], hashlib.sha256(apk.read_bytes()).hexdigest())

    def test_rejects_test_build_git_sha_mismatch(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            tests = base_test_metadata()
            build = build_metadata_for(apk)
            build["gitSha"] = "b" * 40
            with self.assertRaisesRegex(CandidateError, "Git SHA mismatch"):
                verify_candidate(tests, build, apk)

    def test_rejects_dirty_or_ineligible_evidence(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            tests = base_test_metadata()
            tests["dirtyTree"] = True
            tests["releaseEvidenceEligible"] = False
            with self.assertRaises(CandidateError):
                verify_candidate(tests, build_metadata_for(apk), apk)

    def test_rejects_apk_hash_mismatch(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            build = build_metadata_for(apk)
            build["sha256"] = "0" * 64
            with self.assertRaisesRegex(CandidateError, "APK SHA-256 mismatch"):
                verify_candidate(base_test_metadata(), build, apk)

    def test_rejects_zero_or_failed_unity_tests(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            tests = base_test_metadata()
            tests["editMode"]["total"] = 0
            tests["editMode"]["passed"] = 0
            with self.assertRaisesRegex(CandidateError, "executed zero tests"):
                verify_candidate(tests, build_metadata_for(apk), apk)

            tests = base_test_metadata()
            tests["playMode"]["result"] = "Failed"
            tests["playMode"]["failed"] = 1
            tests["playMode"]["passed"] = 4
            with self.assertRaisesRegex(CandidateError, "failed tests"):
                verify_candidate(tests, build_metadata_for(apk), apk)


if __name__ == "__main__":
    unittest.main()
