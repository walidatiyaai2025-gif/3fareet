import hashlib
import importlib.util
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "prepare_candidate_device.py"
SPEC = importlib.util.spec_from_file_location("prepare_candidate_device", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
PREPARE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(PREPARE)

SHA = "a" * 40


def make_manifest(apk: Path, candidate_type: str = "local-windows-licensed-unity"):
    payload = apk.read_bytes()
    return {
        "schemaVersion": 1,
        "candidateType": candidate_type,
        "gitSha": SHA,
        "packageId": "com.fiftysolutions.afareetunity3d",
        "releaseEvidenceEligible": True,
        "readyForDeviceEvidence": True,
        "verified": False,
        "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        "apk": {
            "path": str(apk),
            "fileName": "afareet-unity3d-debug.apk",
            "sizeBytes": len(payload),
            "sha256": hashlib.sha256(payload).hexdigest(),
        },
    }


class PrepareCandidateDeviceTests(unittest.TestCase):
    def make_apk(self, directory: str, payload: bytes = b"candidate-apk") -> Path:
        apk = Path(directory) / "afareet-unity3d-debug.apk"
        apk.write_bytes(payload)
        return apk

    def test_valid_candidate_resolves_exact_apk(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest_path = Path(tmp) / "local-candidate-manifest.json"
            candidate = PREPARE.resolve_candidate(make_manifest(apk), manifest_path)
            self.assertEqual(apk.resolve(), candidate["apkPath"])
            self.assertEqual(SHA, candidate["gitSha"])
            self.assertEqual("local-windows-licensed-unity", candidate["candidateType"])
            self.assertEqual(hashlib.sha256(apk.read_bytes()).hexdigest(), candidate["apkSha256"])

    def test_valid_github_ci_candidate_resolves_exact_apk(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp, b"github-ci-candidate")
            manifest_path = Path(tmp) / "ci-candidate-manifest.json"
            candidate = PREPARE.resolve_candidate(
                make_manifest(apk, "github-actions-unity-ci"),
                manifest_path,
            )
            self.assertEqual(apk.resolve(), candidate["apkPath"])
            self.assertEqual(SHA, candidate["gitSha"])
            self.assertEqual("github-actions-unity-ci", candidate["candidateType"])

    def test_rejects_unsupported_candidate_type(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = make_manifest(apk, "arbitrary-apk")
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "Unsupported candidateType"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

    def test_rejects_apk_hash_mismatch_before_adb(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = make_manifest(apk)
            manifest["apk"]["sha256"] = "0" * 64
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "SHA-256 mismatch"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

    def test_rejects_non_release_or_not_ready_candidate(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = make_manifest(apk)
            manifest["releaseEvidenceEligible"] = False
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "not release-evidence eligible"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

            manifest = make_manifest(apk)
            manifest["readyForDeviceEvidence"] = False
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "not ready"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

    def test_rejects_manifest_that_self_asserts_verified(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = make_manifest(apk)
            manifest["verified"] = True
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "must not self-assert VERIFIED"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

    def test_apk_override_allows_moved_bundle_but_still_checks_bytes(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            original = self.make_apk(tmp, b"portable-candidate")
            manifest = make_manifest(original)
            original.unlink()
            moved_dir = root / "moved"
            moved_dir.mkdir()
            moved = moved_dir / "afareet-unity3d-debug.apk"
            moved.write_bytes(b"portable-candidate")
            candidate = PREPARE.resolve_candidate(manifest, root / "manifest.json", moved)
            self.assertEqual(moved.resolve(), candidate["apkPath"])

            moved.write_bytes(b"different-bytes")
            with self.assertRaises(PREPARE.CandidatePrepareError):
                PREPARE.resolve_candidate(manifest, root / "manifest.json", moved)


if __name__ == "__main__":
    unittest.main()
