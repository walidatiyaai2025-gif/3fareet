import hashlib
import importlib.util
import json
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
    manifest = {
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
    if candidate_type == "github-actions-unity-ci":
        manifest["githubRun"] = {
            "runId": "12345",
            "runAttempt": "1",
            "repository": "walidatiyaai2025-gif/3fareet",
            "workflow": "Unity Production CI",
            "eventName": "pull_request",
            "ref": "refs/pull/108/merge",
        }
    return manifest


class FakeDeviceEvidence:
    @staticmethod
    def write_json(path: Path, payload):
        path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


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
            self.assertIsNone(candidate["githubRun"])
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
            self.assertEqual("12345", candidate["githubRun"]["runId"])

    def test_bind_candidate_to_session_persists_manifest_provenance_and_performance_tier(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            apk = self.make_apk(tmp, b"bound-session-apk")
            source_manifest = make_manifest(apk, "github-actions-unity-ci")
            manifest_path = root / "ci-candidate-manifest.json"
            manifest_path.write_text(json.dumps(source_manifest, sort_keys=True) + "\n", encoding="utf-8")
            candidate = PREPARE.resolve_candidate(source_manifest, manifest_path)
            session_path = root / PREPARE.SESSION_FILE
            session_path.write_text(
                json.dumps({
                    "packageId": PREPARE.EXPECTED_PACKAGE_ID,
                    "apk": {
                        "sha256": candidate["apkSha256"],
                        "sizeBytes": candidate["sizeBytes"],
                    },
                }),
                encoding="utf-8",
            )

            context = PREPARE.bind_candidate_to_session(
                candidate,
                manifest_path,
                root,
                FakeDeviceEvidence,
                "mid",
            )
            saved_session = json.loads(session_path.read_text(encoding="utf-8"))
            bound_manifest = root / PREPARE.BOUND_MANIFEST_FILE
            self.assertTrue(bound_manifest.is_file())
            self.assertEqual(manifest_path.read_bytes(), bound_manifest.read_bytes())
            self.assertEqual(SHA, saved_session["candidate"]["gitSha"])
            self.assertEqual("github-actions-unity-ci", saved_session["candidate"]["candidateType"])
            self.assertEqual("12345", saved_session["candidate"]["githubRun"]["runId"])
            self.assertEqual("mid", saved_session["performanceTier"])
            self.assertEqual(hashlib.sha256(bound_manifest.read_bytes()).hexdigest(), context["manifest"]["sha256"])

    def test_bind_candidate_rejects_invalid_performance_tier(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            apk = self.make_apk(tmp)
            source_manifest = make_manifest(apk)
            manifest_path = root / "candidate.json"
            manifest_path.write_text(json.dumps(source_manifest), encoding="utf-8")
            candidate = PREPARE.resolve_candidate(source_manifest, manifest_path)
            session_path = root / PREPARE.SESSION_FILE
            session_path.write_text(
                json.dumps({
                    "packageId": PREPARE.EXPECTED_PACKAGE_ID,
                    "apk": {
                        "sha256": candidate["apkSha256"],
                        "sizeBytes": candidate["sizeBytes"],
                    },
                }),
                encoding="utf-8",
            )

            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "performance tier must be one of"):
                PREPARE.bind_candidate_to_session(
                    candidate,
                    manifest_path,
                    root,
                    FakeDeviceEvidence,
                    "ultra",
                )

    def test_candidate_device_parser_requires_performance_tier(self):
        parser = PREPARE.build_parser()
        with self.assertRaises(SystemExit):
            parser.parse_args(["--candidate-manifest", "candidate.json", "--output", "evidence"])
        args = parser.parse_args([
            "--candidate-manifest", "candidate.json",
            "--output", "evidence",
            "--performance-tier", "high",
        ])
        self.assertEqual("high", args.performance_tier)

    def test_rejects_ci_candidate_without_exact_github_provenance(self):
        with tempfile.TemporaryDirectory() as tmp:
            apk = self.make_apk(tmp)
            manifest = make_manifest(apk, "github-actions-unity-ci")
            manifest.pop("githubRun")
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "missing githubRun provenance"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

            manifest = make_manifest(apk, "github-actions-unity-ci")
            manifest["githubRun"]["repository"] = "other/repo"
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "Unexpected GitHub candidate repository"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

            manifest = make_manifest(apk, "github-actions-unity-ci")
            manifest["githubRun"]["workflow"] = "Other Workflow"
            with self.assertRaisesRegex(PREPARE.CandidatePrepareError, "Unexpected GitHub candidate workflow"):
                PREPARE.resolve_candidate(manifest, Path(tmp) / "manifest.json")

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
