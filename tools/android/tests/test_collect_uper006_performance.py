import importlib.util
import json
import tempfile
import unittest
from pathlib import Path
from unittest import mock


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/collect_uper006_performance.py"
SPEC = importlib.util.spec_from_file_location("collect_uper006_performance", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class CollectUper006PerformanceTests(unittest.TestCase):
    def runtime_report(self):
        return {
            "schemaVersion": 1,
            "evidenceId": "UPER-006",
            "samples": 300,
            "validFrameTimingSamples": 300,
            "avgFps": 59.5,
            "avgFrameMs": 16.8,
            "p95FrameMs": 18.2,
            "worstFrameMs": 24.1,
            "avgCpuMs": 7.0,
            "avgGpuMs": 8.0,
            "peakReservedMb": 700.0,
            "capturedUtc": "2026-08-19T10:00:00Z",
            "deviceModel": "TEST PHONE",
            "graphicsDeviceName": "TEST GPU",
            "operatingSystem": "Android 15",
            "processorType": "TEST CPU",
            "platform": "Android",
            "unityVersion": "6000.5.8f1",
            "appVersion": "1.0",
            "qualityLevel": "Mobile",
            "graphicsMemoryMb": 2048,
            "systemMemoryMb": 8192,
            "processorCount": 8,
            "screenWidth": 2400,
            "screenHeight": 1080,
        }

    def make_candidate(self, root: Path):
        apk = root / "afareet-unity3d-debug.apk"
        apk.write_bytes(b"exact-candidate-apk-bytes")
        digest = MODULE.sha256_file(apk)
        manifest = {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": "a" * 40,
            "unityVersion": "6000.5.8f1",
            "packageId": MODULE.DEFAULT_PACKAGE,
            "apk": {
                "fileName": apk.name,
                "sizeBytes": apk.stat().st_size,
                "sha256": digest,
            },
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        }
        manifest_path = root / "local-candidate-manifest.json"
        manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        return apk, digest, manifest_path, manifest

    def test_candidate_manifest_binds_exact_sha_apk_size_and_unity(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            apk, digest, _, manifest = self.make_candidate(root)
            git_sha, unity = MODULE.validate_candidate_manifest(
                manifest,
                package=MODULE.DEFAULT_PACKAGE,
                local_apk_path=apk,
                local_apk_sha256=digest,
            )
            self.assertEqual(git_sha, "a" * 40)
            self.assertEqual(unity, "6000.5.8f1")

            manifest["apk"]["sha256"] = "b" * 64
            with self.assertRaisesRegex(MODULE.EvidenceError, "candidate APK hash mismatch"):
                MODULE.validate_candidate_manifest(
                    manifest,
                    package=MODULE.DEFAULT_PACKAGE,
                    local_apk_path=apk,
                    local_apk_sha256=digest,
                )

    def test_candidate_manifest_rejects_promotion_or_wrong_verdict(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            apk, digest, _, manifest = self.make_candidate(root)
            manifest["verified"] = True
            with self.assertRaisesRegex(MODULE.EvidenceError, "verified must remain"):
                MODULE.validate_candidate_manifest(
                    manifest, package=MODULE.DEFAULT_PACKAGE, local_apk_path=apk, local_apk_sha256=digest
                )
            manifest["verified"] = False
            manifest["verdict"] = "VERIFIED"
            with self.assertRaisesRegex(MODULE.EvidenceError, "verdict"):
                MODULE.validate_candidate_manifest(
                    manifest, package=MODULE.DEFAULT_PACKAGE, local_apk_path=apk, local_apk_sha256=digest
                )

    def test_installed_apk_path_requires_one_standalone_apk(self):
        path = MODULE.parse_installed_apk_path("package:/data/app/base.apk\n", package=MODULE.DEFAULT_PACKAGE)
        self.assertEqual(path, "/data/app/base.apk")
        with self.assertRaisesRegex(MODULE.EvidenceError, "split APKs"):
            MODULE.parse_installed_apk_path(
                "package:/data/app/base.apk\npackage:/data/app/split_config.arm64_v8a.apk\n",
                package=MODULE.DEFAULT_PACKAGE,
            )
        with self.assertRaisesRegex(MODULE.EvidenceError, "not installed"):
            MODULE.parse_installed_apk_path("", package=MODULE.DEFAULT_PACKAGE)

    def test_runtime_report_rejects_bad_metrics_and_insufficient_samples(self):
        report = self.runtime_report()
        self.assertIs(MODULE.validate_runtime_report(report), report)
        report["samples"] = 299
        with self.assertRaisesRegex(MODULE.EvidenceError, "samples"):
            MODULE.validate_runtime_report(report)
        report = self.runtime_report()
        report["p95FrameMs"] = 30.0
        with self.assertRaisesRegex(MODULE.EvidenceError, "p95FrameMs"):
            MODULE.validate_runtime_report(report)

    def test_envelope_is_explicitly_collected_not_verified(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            apk, digest, manifest_path, _ = self.make_candidate(root)
            envelope = MODULE.build_envelope(
                report=self.runtime_report(),
                git_sha="a" * 40,
                apk_path=apk,
                apk_sha256=digest,
                serial="SERIAL-001",
                package=MODULE.DEFAULT_PACKAGE,
                installed_apk_path="/data/app/base.apk",
                installed_apk_sha256=digest,
                candidate_manifest_path=manifest_path,
                candidate_manifest_sha256="c" * 64,
                binding_mode=MODULE.MANIFEST_BINDING,
            )
            self.assertEqual(envelope["schema"], "afareet-uper006-physical-device-evidence-v2")
            self.assertEqual(envelope["verdict"], "COLLECTED_NOT_VERIFIED")
            self.assertTrue(envelope["candidateArtifact"]["installedMatchesLocal"])
            self.assertFalse(envelope["acceptance"]["physicalDeviceVerified"])
            self.assertFalse(envelope["acceptance"]["performanceTargetAccepted"])
            self.assertFalse(envelope["acceptance"]["ownerApproval"])
            self.assertFalse(envelope["acceptance"]["upER006Verified"])

    def test_main_rejects_installed_apk_bytes_that_do_not_match_candidate(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            apk, _, manifest_path, _ = self.make_candidate(root)
            output = root / "evidence.json"
            with mock.patch.object(MODULE, "resolve_serial", return_value="SERIAL-001"), \
                 mock.patch.object(MODULE, "resolve_installed_apk_path", return_value="/data/app/base.apk"), \
                 mock.patch.object(MODULE, "hash_installed_apk", return_value="d" * 64):
                result = MODULE.main([
                    "--apk", str(apk),
                    "--candidate-manifest", str(manifest_path),
                    "--output", str(output),
                ])
            self.assertEqual(result, 2)
            self.assertFalse(output.exists())

    def test_main_writes_exact_bound_collection_with_matching_runtime(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            apk, digest, manifest_path, _ = self.make_candidate(root)
            output = root / "evidence.json"
            with mock.patch.object(MODULE, "resolve_serial", return_value="SERIAL-001"), \
                 mock.patch.object(MODULE, "resolve_installed_apk_path", return_value="/data/app/base.apk"), \
                 mock.patch.object(MODULE, "hash_installed_apk", return_value=digest), \
                 mock.patch.object(MODULE, "pull_runtime_report", return_value=self.runtime_report()):
                result = MODULE.main([
                    "--apk", str(apk),
                    "--candidate-manifest", str(manifest_path),
                    "--output", str(output),
                ])
            self.assertEqual(result, 0)
            evidence = json.loads(output.read_text(encoding="utf-8"))
            self.assertEqual(evidence["candidateBinding"]["gitSha"], "a" * 40)
            self.assertEqual(evidence["candidateArtifact"]["localApkSha256"], digest)
            self.assertEqual(evidence["candidateArtifact"]["installedApkSha256"], digest)
            self.assertEqual(evidence["device"]["adbSerial"], "SERIAL-001")
            self.assertEqual(evidence["verdict"], "COLLECTED_NOT_VERIFIED")


if __name__ == "__main__":
    unittest.main()
