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

    def test_apk_hash_is_bound_into_nonverified_envelope(self):
        report = self.report()
        with tempfile.TemporaryDirectory() as directory:
            apk = Path(directory) / "candidate.apk"
            apk.write_bytes(b"exact apk fixture")
            digest = MODULE.sha256_file(apk)
            envelope = MODULE.build_envelope(
                report=report,
                git_sha="1" * 40,
                apk_path=apk,
                apk_sha256=digest,
                device_serial="SERIAL123",
                package=MODULE.DEFAULT_PACKAGE,
            )

        self.assertEqual(envelope["schemaVersion"], 1)
        self.assertEqual(envelope["evidenceId"], "UPER-006")
        self.assertEqual(envelope["verdict"], "COLLECTED_NOT_VERIFIED")
        self.assertEqual(envelope["candidate"]["gitSha"], "1" * 40)
        self.assertEqual(envelope["candidate"]["apkSha256"], digest)
        self.assertEqual(envelope["device"]["adbSerial"], "SERIAL123")
        self.assertIn("does not satisfy physical-device acceptance", envelope["verificationBoundary"])

    def test_runtime_report_roundtrips_as_json(self):
        payload = json.dumps(self.report())
        decoded = json.loads(payload)
        MODULE.validate_runtime_report(decoded)


if __name__ == "__main__":
    unittest.main()
