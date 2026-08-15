import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]


def _load_module(name: str, filename: str):
    path = TOOLS_DIR / filename
    spec = importlib.util.spec_from_file_location(name, path)
    assert spec is not None and spec.loader is not None
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


EXPORTER = _load_module("export_device_evidence_integrity_fixture", "export_device_evidence.py")
VERIFIER = _load_module("verify_device_review_bundle", "verify_device_review_bundle.py")


class VerifyDeviceReviewBundleTests(unittest.TestCase):
    def _raw_session(self, root: Path) -> tuple[Path, str, str]:
        session_dir = root / "raw"
        checkpoint_dir = session_dir / "checkpoints" / "results"
        checkpoint_dir.mkdir(parents=True)

        git_sha = "b" * 40
        apk_sha = "a" * 64
        serial = "ADB-PRIVATE-SERIAL-98765"
        serial_hash = hashlib.sha256(serial.encode("utf-8")).hexdigest()

        bound_manifest = session_dir / EXPORTER.BOUND_CANDIDATE_MANIFEST_FILE
        bound_manifest.write_text(
            json.dumps({"gitSha": git_sha, "apkSha256": apk_sha}, sort_keys=True) + "\n",
            encoding="utf-8",
        )
        manifest_sha = EXPORTER.sha256_file(bound_manifest)

        session = {
            "packageId": EXPORTER.PACKAGE_ID,
            "apk": {"sha256": apk_sha},
            "device": {
                "serial": serial,
                "serialSha256": serial_hash,
            },
            "candidate": {
                "candidateType": "local-windows-licensed-unity",
                "gitSha": git_sha,
                "apkSha256": apk_sha,
                "releaseEvidenceEligible": True,
                "readyForDeviceEvidence": True,
                "verified": False,
                "verdict": EXPORTER.EXPECTED_CANDIDATE_VERDICT,
                "manifest": {
                    "sha256": manifest_sha,
                },
            },
        }
        EXPORTER.write_json(session_dir / EXPORTER.SESSION_FILE, session)

        index = {
            "schemaVersion": 1,
            "state": "EVIDENCE_COLLECTED",
            "verdict": EXPORTER.EXPECTED_REVIEW_VERDICT,
            "packageId": EXPORTER.PACKAGE_ID,
            "apkSha256": apk_sha,
            "deviceSerialSha256": serial_hash,
            "device": {
                "manufacturer": "Acme",
                "model": "Physical Phone",
                "androidRelease": "16",
                "apiLevel": "36",
                "primaryAbi": "arm64-v8a",
                "isEmulator": False,
            },
            "checkpointCount": 1,
            "checkpoints": ["results"],
            "automatedRedFlagCount": 0,
            "automatedRedFlags": [],
            "manualReviewChecklist": [
                "UVEH-012 manual review",
                "URAC-012 manual review",
                "UPER-006 manual review",
                "UPER-009 manual review",
                "UPER-010 manual release approval",
            ],
        }
        EXPORTER.write_json(session_dir / EXPORTER.INDEX_FILE, index)

        checkpoint = {
            "schemaVersion": 1,
            "label": "results",
            "apkSha256": apk_sha,
            "deviceSerialSha256": serial_hash,
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
        EXPORTER.write_json(checkpoint_dir / "checkpoint.json", checkpoint)
        (checkpoint_dir / "screen.png").write_bytes(b"\x89PNG\r\n\x1a\nreview-fixture")
        for name in ("meminfo.txt", "gfxinfo.txt", "thermalservice.txt", "battery.txt"):
            (checkpoint_dir / name).write_text(f"safe {name}\n", encoding="utf-8")
        (checkpoint_dir / "logcat.txt").write_text(f"raw {serial}\n", encoding="utf-8")
        (checkpoint_dir / "activity.txt").write_text(f"raw {serial}\n", encoding="utf-8")
        return session_dir, git_sha, apk_sha

    def _bundle(self, root: Path) -> tuple[Path, str, str]:
        session_dir, git_sha, apk_sha = self._raw_session(root)
        bundle = root / "review"
        manifest = EXPORTER.export_bundle(session_dir, bundle)
        self.assertEqual(EXPORTER.REVIEW_MANIFEST_SCHEMA_VERSION, manifest["schemaVersion"])
        self.assertTrue(manifest["contentFiles"])
        self.assertRegex(manifest["contentSetSha256"], r"^[0-9a-f]{64}$")
        return bundle, git_sha, apk_sha

    def test_verifier_accepts_exact_exported_bundle(self):
        with tempfile.TemporaryDirectory() as directory:
            bundle, git_sha, apk_sha = self._bundle(Path(directory))

            result = VERIFIER.verify_bundle(
                bundle,
                expected_git_sha=git_sha,
                expected_apk_sha=apk_sha,
            )

            self.assertEqual(git_sha, result["gitSha"])
            self.assertEqual(apk_sha, result["apkSha256"])
            self.assertEqual(1, result["checkpointCount"])
            self.assertGreater(result["contentFileCount"], 1)
            self.assertEqual(EXPORTER.EXPECTED_REVIEW_VERDICT, result["verdict"])
            self.assertFalse(result["verified"])

    def test_verifier_rejects_tampered_screenshot(self):
        with tempfile.TemporaryDirectory() as directory:
            bundle, _, _ = self._bundle(Path(directory))
            screenshot = bundle / "checkpoints" / "results" / "screen.png"
            screenshot.write_bytes(screenshot.read_bytes() + b"tampered")

            with self.assertRaisesRegex(VERIFIER.ReviewBundleVerificationError, "size mismatch|SHA-256 mismatch"):
                VERIFIER.verify_bundle(bundle)

    def test_verifier_rejects_unexpected_raw_file(self):
        with tempfile.TemporaryDirectory() as directory:
            bundle, _, _ = self._bundle(Path(directory))
            (bundle / "session.json").write_text('{"raw": true}\n', encoding="utf-8")

            with self.assertRaisesRegex(VERIFIER.ReviewBundleVerificationError, "file-set mismatch"):
                VERIFIER.verify_bundle(bundle)

    def test_verifier_rejects_tampered_content_set_digest(self):
        with tempfile.TemporaryDirectory() as directory:
            bundle, _, _ = self._bundle(Path(directory))
            manifest_path = bundle / VERIFIER.REVIEW_MANIFEST_FILE
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            manifest["contentSetSha256"] = "d" * 64
            manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")

            with self.assertRaisesRegex(VERIFIER.ReviewBundleVerificationError, "contentSetSha256 mismatch"):
                VERIFIER.verify_bundle(bundle)

    def test_verifier_rejects_wrong_expected_candidate_sha(self):
        with tempfile.TemporaryDirectory() as directory:
            bundle, _, _ = self._bundle(Path(directory))

            with self.assertRaisesRegex(VERIFIER.ReviewBundleVerificationError, "candidate Git SHA mismatch"):
                VERIFIER.verify_bundle(bundle, expected_git_sha="c" * 40)

    def test_verifier_rejects_forbidden_manifest_content_path(self):
        with tempfile.TemporaryDirectory() as directory:
            bundle, _, _ = self._bundle(Path(directory))
            manifest_path = bundle / VERIFIER.REVIEW_MANIFEST_FILE
            manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
            first_record = next(iter(manifest["contentFiles"].values()))
            manifest["contentFiles"]["checkpoints/results/logcat.txt"] = first_record
            manifest["copiedFiles"] = sorted(manifest["contentFiles"])
            manifest["contentSetSha256"] = VERIFIER.content_set_sha256(manifest["contentFiles"])
            manifest_path.write_text(json.dumps(manifest, indent=2, sort_keys=True) + "\n", encoding="utf-8")

            with self.assertRaisesRegex(VERIFIER.ReviewBundleVerificationError, "forbidden review-bundle path"):
                VERIFIER.verify_bundle(bundle)


if __name__ == "__main__":
    unittest.main()
