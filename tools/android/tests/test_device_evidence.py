import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "device_evidence.py"
SPEC = importlib.util.spec_from_file_location("device_evidence", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
DEVICE_EVIDENCE = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(DEVICE_EVIDENCE)


class DeviceEvidenceTests(unittest.TestCase):
    def test_parse_adb_devices_keeps_authorized_device_metadata(self):
        output = """List of devices attached
ABC123\tdevice product:foo model:Pixel_8 device:husky transport_id:1
emulator-5554\toffline transport_id:2
"""
        devices = DEVICE_EVIDENCE.parse_adb_devices(output)
        self.assertEqual(2, len(devices))
        self.assertEqual("ABC123", devices[0]["serial"])
        self.assertEqual("device", devices[0]["state"])
        self.assertEqual("Pixel_8", devices[0]["model"])
        self.assertEqual("offline", devices[1]["state"])

    def test_sanitize_label_is_stable_and_rejects_empty(self):
        self.assertEqual("race-results-1", DEVICE_EVIDENCE.sanitize_label("  race results #1  "))
        self.assertEqual("start_screen", DEVICE_EVIDENCE.sanitize_label("start_screen"))
        with self.assertRaises(ValueError):
            DEVICE_EVIDENCE.sanitize_label("///")

    def test_sha256_file_matches_hashlib(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / "sample.apk"
            payload = b"afareet-apk-fixture"
            path.write_bytes(payload)
            self.assertEqual(hashlib.sha256(payload).hexdigest(), DEVICE_EVIDENCE.sha256_file(path))

    def test_scan_logcat_flags_fatal_anr_and_native_crash(self):
        text = "\n".join(
            [
                "I Unity : normal frame",
                "E AndroidRuntime: FATAL EXCEPTION: main",
                "E ActivityManager: ANR in com.fiftysolutions.afareetunity3d",
                "F libc : Fatal signal 11 (SIGSEGV), code 1",
            ]
        )
        findings = DEVICE_EVIDENCE.scan_logcat(text)
        self.assertEqual(3, len(findings))
        self.assertTrue(any("FATAL EXCEPTION" in item for item in findings))
        self.assertTrue(any("ANR in" in item for item in findings))
        self.assertTrue(any("SIGSEGV" in item for item in findings))

    def test_scan_logcat_does_not_turn_normal_unity_lines_into_failure(self):
        text = "\n".join(
            [
                "I Unity : AFAREET_BOOTSTRAP_READY",
                "I Unity : AFAREET_RENDER_TIER tier=mid",
                "D ActivityManager: Displayed com.fiftysolutions.afareetunity3d/.MainActivity",
            ]
        )
        self.assertEqual([], DEVICE_EVIDENCE.scan_logcat(text))

    def test_finish_keeps_manual_review_required(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session = {
                "packageId": DEVICE_EVIDENCE.PACKAGE_ID,
                "apk": {"sha256": "abc"},
                "device": {
                    "serial": "SERIAL",
                    "serialSha256": "serialhash",
                    "manufacturer": "Acme",
                    "model": "Phone",
                    "androidRelease": "16",
                    "apiLevel": "36",
                    "primaryAbi": "arm64-v8a",
                    "isEmulator": False,
                },
            }
            DEVICE_EVIDENCE.write_json(root / DEVICE_EVIDENCE.SESSION_FILE, session)
            checkpoints = root / "checkpoints" / "results"
            checkpoints.mkdir(parents=True)
            DEVICE_EVIDENCE.write_json(
                checkpoints / "checkpoint.json",
                {
                    "label": "results",
                    "automatedRedFlags": [],
                },
            )

            args = type("Args", (), {"session": str(root)})()
            code = DEVICE_EVIDENCE.command_finish(args)
            self.assertEqual(0, code)
            index = json.loads((root / DEVICE_EVIDENCE.INDEX_FILE).read_text(encoding="utf-8"))
            self.assertEqual("MANUAL_REVIEW_REQUIRED", index["verdict"])
            self.assertEqual(1, index["checkpointCount"])
            self.assertEqual(["results"], index["checkpoints"])


if __name__ == "__main__":
    unittest.main()
