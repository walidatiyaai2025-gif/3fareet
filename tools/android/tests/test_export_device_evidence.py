import hashlib
import importlib.util
import json
import tempfile
import unittest
from pathlib import Path


MODULE_PATH = Path(__file__).resolve().parents[1] / "export_device_evidence.py"
SPEC = importlib.util.spec_from_file_location("export_device_evidence", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
EXPORTER = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(EXPORTER)


class ExportDeviceEvidenceTests(unittest.TestCase):
    def _fixture(self, root: Path, *, red_flags: int = 0, emulator: bool = False) -> tuple[Path, str]:
        session_dir = root / "raw-session"
        checkpoint_dir = session_dir / "checkpoints" / "results"
        checkpoint_dir.mkdir(parents=True)

        serial = "ADB-SECRET-SERIAL-12345"
        serial_hash = hashlib.sha256(serial.encode("utf-8")).hexdigest()
        apk_sha = "a" * 64
        git_sha = "b" * 40

        bound_manifest = session_dir / EXPORTER.BOUND_CANDIDATE_MANIFEST_FILE
        bound_manifest_bytes = json.dumps(
            {"localPath": f"C:/private/{serial}/candidate.apk"}, sort_keys=True
        ).encode("utf-8")
        bound_manifest.write_bytes(bound_manifest_bytes)
        manifest_sha = hashlib.sha256(bound_manifest_bytes).hexdigest()

        session = {
            "packageId": EXPORTER.PACKAGE_ID,
            "apk": {"sha256": apk_sha},
            "device": {
                "serial": serial,
                "serialSha256": serial_hash,
            },
            "candidate": {
                "schemaVersion": 1,
                "candidateType": "local-windows-licensed-unity",
                "gitSha": git_sha,
                "apkSha256": apk_sha,
                "releaseEvidenceEligible": True,
                "readyForDeviceEvidence": True,
                "verified": False,
                "verdict": EXPORTER.EXPECTED_CANDIDATE_VERDICT,
                "manifest": {
                    "fileName": EXPORTER.BOUND_CANDIDATE_MANIFEST_FILE,
                    "sourceFileName": "local-candidate-manifest.json",
                    "sha256": manifest_sha,
                },
            },
        }
        EXPORTER.write_json(session_dir / EXPORTER.SESSION_FILE, session)
        (session_dir / "package-dump.txt").write_text(f"device={serial}\n", encoding="utf-8")

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
                "isEmulator": emulator,
            },
            "checkpointCount": 1,
            "checkpoints": ["results"],
            "automatedRedFlagCount": red_flags,
            "automatedRedFlags": ([{"checkpoint": "results", "finding": "fatal fixture"}] if red_flags else []),
            "manualReviewChecklist": [
                "UVEH-012: manual driving review",
                "URAC-012: manual race flow review",
                "UPER-006: manual performance review",
                "UPER-009: manual visual review",
                "UPER-010: publish only after approvals",
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
        (checkpoint_dir / "screen.png").write_bytes(b"\x89PNG\r\n\x1a\nfixture")
        for name in ("meminfo.txt", "gfxinfo.txt", "thermalservice.txt", "battery.txt"):
            (checkpoint_dir / name).write_text(f"safe {name}\n", encoding="utf-8")
        (checkpoint_dir / "logcat.txt").write_text(f"private serial {serial}\n", encoding="utf-8")
        (checkpoint_dir / "activity.txt").write_text(f"private serial {serial}\n", encoding="utf-8")
        return session_dir, serial

    def test_export_copies_only_sanitized_review_material(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, serial = self._fixture(root)
            output = root / "review-bundle"

            manifest = EXPORTER.export_bundle(session_dir, output)

            self.assertEqual("SANITIZED_REVIEW_BUNDLE", manifest["state"])
            self.assertEqual(EXPORTER.EXPECTED_REVIEW_VERDICT, manifest["verdict"])
            self.assertFalse(manifest["privacy"]["rawAdbSerialIncluded"])
            self.assertEqual("b" * 40, manifest["candidate"]["gitSha"])
            self.assertTrue((output / "evidence-index.json").is_file())
            self.assertTrue((output / "checkpoints" / "results" / "screen.png").is_file())
            self.assertTrue((output / "checkpoints" / "results" / "meminfo.txt").is_file())

            self.assertFalse((output / "session.json").exists())
            self.assertFalse((output / EXPORTER.BOUND_CANDIDATE_MANIFEST_FILE).exists())
            self.assertFalse((output / "package-dump.txt").exists())
            self.assertFalse((output / "checkpoints" / "results" / "logcat.txt").exists())
            self.assertFalse((output / "checkpoints" / "results" / "activity.txt").exists())

            checkpoint = json.loads(
                (output / "checkpoints" / "results" / "checkpoint.json").read_text(encoding="utf-8")
            )
            self.assertEqual(list(EXPORTER.SAFE_CHECKPOINT_PAYLOAD_FILES), checkpoint["files"])
            self.assertEqual(["logcat.txt", "activity.txt"], checkpoint["excludedByPolicy"])

            for path in output.rglob("*"):
                if path.is_file() and path.suffix.lower() in EXPORTER.TEXT_SUFFIXES:
                    self.assertNotIn(serial, path.read_text(encoding="utf-8", errors="replace"))

    def test_export_rejects_serial_hash_mismatch(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, _ = self._fixture(root)
            index_path = session_dir / EXPORTER.INDEX_FILE
            index = json.loads(index_path.read_text(encoding="utf-8"))
            index["deviceSerialSha256"] = "d" * 64
            EXPORTER.write_json(index_path, index)

            with self.assertRaisesRegex(EXPORTER.EvidenceExportError, "serial SHA-256 binding"):
                EXPORTER.export_bundle(session_dir, root / "review-bundle")

    def test_export_rejects_emulator_session(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, _ = self._fixture(root, emulator=True)

            with self.assertRaisesRegex(EXPORTER.EvidenceExportError, "physical-device evidence"):
                EXPORTER.export_bundle(session_dir, root / "review-bundle")

    def test_export_rejects_output_nested_inside_raw_session(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, _ = self._fixture(root)

            with self.assertRaisesRegex(EXPORTER.EvidenceExportError, "must not be inside"):
                EXPORTER.export_bundle(session_dir, session_dir / "public")

    def test_export_rejects_tampered_bound_candidate_manifest(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, _ = self._fixture(root)
            (session_dir / EXPORTER.BOUND_CANDIDATE_MANIFEST_FILE).write_text(
                '{"tampered": true}\n', encoding="utf-8"
            )

            with self.assertRaisesRegex(EXPORTER.EvidenceExportError, "manifest SHA mismatch"):
                EXPORTER.export_bundle(session_dir, root / "review-bundle")

    def test_export_rejects_unsupported_candidate_type(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, _ = self._fixture(root)
            session_path = session_dir / EXPORTER.SESSION_FILE
            session = json.loads(session_path.read_text(encoding="utf-8"))
            session["candidate"]["candidateType"] = "untrusted-candidate"
            EXPORTER.write_json(session_path, session)

            with self.assertRaisesRegex(EXPORTER.EvidenceExportError, "Unsupported candidateType"):
                EXPORTER.export_bundle(session_dir, root / "review-bundle")

    def test_cli_exports_bundle_but_returns_nonzero_when_red_flags_exist(self):
        with tempfile.TemporaryDirectory() as directory:
            root = Path(directory)
            session_dir, _ = self._fixture(root, red_flags=1)
            output = root / "review-bundle"

            code = EXPORTER.main(["--session", str(session_dir), "--output", str(output)])

            self.assertEqual(2, code)
            self.assertTrue((output / EXPORTER.REVIEW_MANIFEST_FILE).is_file())
            manifest = json.loads((output / EXPORTER.REVIEW_MANIFEST_FILE).read_text(encoding="utf-8"))
            self.assertEqual(1, manifest["automatedRedFlagCount"])
            self.assertEqual(EXPORTER.EXPECTED_REVIEW_VERDICT, manifest["verdict"])


if __name__ == "__main__":
    unittest.main()
