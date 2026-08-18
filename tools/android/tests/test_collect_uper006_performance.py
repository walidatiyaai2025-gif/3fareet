import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
MODULE_PATH = REPO_ROOT / "tools/android/collect_uper006_performance.py"
SPEC = importlib.util.spec_from_file_location("collect_uper006_performance", MODULE_PATH)
MODULE = importlib.util.module_from_spec(SPEC)
assert SPEC.loader is not None
SPEC.loader.exec_module(MODULE)


class CollectUper006PerformanceTests(unittest.TestCase):
    def report(self):
        return {
            "schemaVersion": 1,
            "evidenceId": "UPER-006",
            "capturedUtc": "2026-08-18T00:00:00.0000000Z",
            "samples": 300,
            "validFrameTimingSamples": 280,
            "avgFps": 31.5,
            "avgFrameMs": 31.7,
            "p95FrameMs": 42.0,
            "worstFrameMs": 80.0,
            "avgCpuMs": 17.2,
            "avgGpuMs": 19.5,
            "peakReservedMb": 925.0,
            "deviceModel": "Test Phone",
            "deviceName": "device",
            "graphicsDeviceName": "Test GPU",
            "graphicsMemoryMb": 1024,
            "systemMemoryMb": 4096,
            "operatingSystem": "Android Test",
            "processorType": "ARM64",
            "processorCount": 8,
            "platform": "Android",
            "unityVersion": "6000.5.8f1",
            "appVersion": "0.1.0",
            "qualityLevel": "Low",
            "targetFrameRate": 30,
            "screenWidth": 1920,
            "screenHeight": 1080,
        }

    def candidate_manifest(self, apk_sha256: str):
        return {
            "schemaVersion": 1,
            "candidateType": "local-windows-licensed-unity",
            "gitSha": "1" * 40,
            "unityVersion": "6000.5.8f1",
            "packageId": MODULE.DEFAULT_PACKAGE,
            "apk": {
                "fileName": "afareet-unity3d-debug.apk",
                "sha256": apk_sha256,
            },
            "releaseEvidenceEligible": True,
            "readyForDeviceEvidence": True,
            "verified": False,
            "verdict": "READY_FOR_PHYSICAL_DEVICE_EVIDENCE",
        }

    def test_valid_runtime_report_is_accepted(self):
        report = self.report()
        self.assertIs(MODULE.validate_runtime_report(report), report)

    def test_wrong_schema_or_evidence_id_is_rejected(self):
        report = self.report()
        report["schemaVersion"] = 2
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_runtime_report(report)

        report = self.report()
        report["evidenceId"] = "OTHER"
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_runtime_report(report)

    def test_sample_and_timing_bounds_are_enforced(self):
        report = self.report()
        report["samples"] = 299
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_runtime_report(report)

        report = self.report()
        report["validFrameTimingSamples"] = 301
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_runtime_report(report)

    def test_nonfinite_metrics_and_invalid_percentile_order_are_rejected(self):
        report = self.report()
        report["avgFps"] = float("nan")
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_runtime_report(report)

        report = self.report()
        report["p95FrameMs"] = 81.0
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_runtime_report(report)

    def test_git_sha_requires_exact_full_fingerprint(self):
        sha = "a" * 40
        self.assertEqual(MODULE.validate_git_sha(sha.upper()), sha)
        for invalid in ("a" * 39, "a" * 41, "z" * 40, "main"):
            with self.assertRaises(MODULE.EvidenceError):
                MODULE.validate_git_sha(invalid)

    def test_candidate_manifest_binds_git_apk_package_and_device_gate(self):
        digest = "a" * 64
        manifest = self.candidate_manifest(digest)

        git_sha = MODULE.validate_candidate_manifest(
            manifest,
            local_apk_sha256=digest,
            package=MODULE.DEFAULT_PACKAGE,
        )

        self.assertEqual(git_sha, "1" * 40)

        manifest = self.candidate_manifest(digest)
        manifest["apk"]["sha256"] = "b" * 64
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_candidate_manifest(
                manifest,
                local_apk_sha256=digest,
                package=MODULE.DEFAULT_PACKAGE,
            )

        manifest = self.candidate_manifest(digest)
        manifest["verified"] = True
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.validate_candidate_manifest(
                manifest,
                local_apk_sha256=digest,
                package=MODULE.DEFAULT_PACKAGE,
            )

    def test_installed_apk_path_requires_one_standalone_apk(self):
        path = MODULE.parse_installed_apk_path(
            "package:/data/app/~~abc/com.fiftysolutions.afareetunity3d/base.apk\n",
            package=MODULE.DEFAULT_PACKAGE,
        )
        self.assertEqual(path, "/data/app/~~abc/com.fiftysolutions.afareetunity3d/base.apk")

        with self.assertRaises(MODULE.EvidenceError):
            MODULE.parse_installed_apk_path("", package=MODULE.DEFAULT_PACKAGE)

        with self.assertRaises(MODULE.EvidenceError):
            MODULE.parse_installed_apk_path(
                "package:/data/app/base.apk\npackage:/data/app/split_config.arm64_v8a.apk\n",
                package=MODULE.DEFAULT_PACKAGE,
            )

    def test_installed_apk_hash_uses_exact_binary_bytes(self):
        payload = b"installed exact apk bytes\x00\x01\xff"
        self.assertEqual(
            MODULE.sha256_bytes(payload),
            __import__("hashlib").sha256(payload).hexdigest(),
        )
        with self.assertRaises(MODULE.EvidenceError):
            MODULE.sha256_bytes(b"")

    def test_apk_hash_and_manifest_binding_are_recorded_in_nonverified_envelope(self):
        report = self.report()
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            apk = root / "candidate.apk"
            apk.write_bytes(b"exact apk fixture")
            manifest_path = root / "local-candidate-manifest.json"
            manifest_path.write_text("{}", encoding="utf-8")
            digest = MODULE.sha256_file(apk)
            envelope = MODULE.build_envelope(
                report=report,
                git_sha="1" * 40,
                apk_path=apk,
                apk_sha256=digest,
                device_serial="SERIAL123",
                package=MODULE.DEFAULT_PACKAGE,
                installed_apk_path="/data/app/example/base.apk",
                installed_apk_sha256=digest,
                candidate_manifest_path=manifest_path,
                candidate_manifest_sha256=MODULE.sha256_file(manifest_path),
                candidate_binding=MODULE.MANIFEST_BINDING,
            )

        self.assertEqual(envelope["schemaVersion"], 1)
        self.assertEqual(envelope["evidenceId"], "UPER-006")
        self.assertEqual(envelope["verdict"], "COLLECTED_NOT_VERIFIED")
        self.assertEqual(envelope["candidateBinding"]["mode"], "LICENSED_CANDIDATE_MANIFEST")
        self.assertEqual(envelope["candidate"]["gitSha"], "1" * 40)
        self.assertEqual(envelope["candidate"]["apkSha256"], digest)
        self.assertEqual(envelope["installedApk"]["sha256"], digest)
        self.assertIs(envelope["installedApk"]["matchesCandidate"], True)
        self.assertEqual(envelope["device"]["adbSerial"], "SERIAL123")
        self.assertIn("does not satisfy physical-device acceptance", envelope["verificationBoundary"])

    def test_runtime_report_roundtrips_as_json(self):
        payload = json.dumps(self.report())
        decoded = json.loads(payload)
        MODULE.validate_runtime_report(decoded)


if __name__ == "__main__":
    unittest.main()
