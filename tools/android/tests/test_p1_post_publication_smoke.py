import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1]
if str(TOOLS) not in sys.path:
    sys.path.insert(0, str(TOOLS))

import prepare_p1_post_publication_smoke as PREPARE
import verify_p1_post_publication_smoke as VERIFY


STAGING_SHA = "a" * 40
CANDIDATE_SHA = "b" * 40
DEVICE_SHA = "d" * 64
AUTHORIZATION = {
    "authorizationSourceGitSha": STAGING_SHA,
    "handoffPacketSha256": "1" * 64,
    "nativeHandoffVerificationSha256": "2" * 64,
    "operatorChainSha256": "3" * 64,
}
APK_BYTES = b"published-afareet-apk-bytes\n"
APK_SHA = hashlib.sha256(APK_BYTES).hexdigest()


def write_json(path: Path, payload) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


class FakeDeviceEvidence:
    @staticmethod
    def write_json(path: Path, payload) -> None:
        write_json(path, payload)


def make_reconciliation(root: Path) -> Path:
    path = root / "p1-publication-receipt-reconciled.json"
    write_json(
        path,
        {
            "schemaVersion": 1,
            "state": "P1_HUMAN_PUBLICATION_RECEIPT_RECONCILED",
            "receiptProfile": "p1-manual-publication-receipt-v1",
            "humanPublicationRecorded": True,
            "publicationPerformedByTool": False,
            "verified": False,
            "runtimeVerified": False,
            "ownerAccepted": False,
            "publicationEligible": False,
            "publicationPreflightSha256": "4" * 64,
            "candidateGitSha": CANDIDATE_SHA,
            "apkSha256": APK_SHA,
            "publishedApkSha256": APK_SHA,
            "stagingSourceGitSha": STAGING_SHA,
            "reviewContentSetSha256": "5" * 64,
            "p1ReviewLineageSha256": "6" * 64,
            "stagingAuthorization": dict(AUTHORIZATION),
            "releaseOwner": "Release Owner",
            "publishedAtUtc": "2026-08-17T06:00:00Z",
            "gitTag": "unity-verified-v0.1.0-build.1",
            "githubReleaseUrl": "https://github.com/example/afareet/releases/tag/unity-verified-v0.1.0-build.1",
            "apkAssetUrl": "https://github.com/example/afareet/releases/download/unity-verified-v0.1.0-build.1/afareet-unity3d-last-verified.apk",
            "postPublicationSmokeRequired": True,
            "lastVerifiedPointerUpdateRequiresReviewedEvidence": True,
        },
    )
    return path


def make_apk(root: Path) -> Path:
    path = root / "afareet-unity3d-last-verified.apk"
    path.write_bytes(APK_BYTES)
    return path


def make_prepared_session(root: Path, reconciliation: dict, apk: dict, *, emulator: bool = False) -> Path:
    session_dir = root / "post-publication-session"
    session_dir.mkdir(parents=True, exist_ok=True)
    write_json(
        session_dir / "session.json",
        {
            "schemaVersion": 1,
            "createdAtUtc": "2026-08-17T06:05:00Z",
            "state": "PREPARED",
            "verdict": "MANUAL_REVIEW_REQUIRED",
            "packageId": PREPARE.EXPECTED_PACKAGE_ID,
            "apk": {
                "path": str(apk["path"]),
                "fileName": apk["path"].name,
                "sizeBytes": apk["sizeBytes"],
                "sha256": apk["sha256"],
            },
            "device": {
                "serialSha256": DEVICE_SHA,
                "isEmulator": emulator,
                "manufacturer": "Test",
                "model": "Physical Phone",
            },
            "checkpointCount": 0,
        },
    )
    if not emulator:
        PREPARE.bind_session(reconciliation, apk, session_dir, "mid", FakeDeviceEvidence)
    return session_dir


def write_checkpoint(session_dir: Path, label: str, captured_at: str, *, pss_mib: int, p95: float, p99: float, thermal: int = 1, red_flags: int = 0) -> None:
    directory = session_dir / "checkpoints" / label
    directory.mkdir(parents=True, exist_ok=True)
    write_json(
        directory / "checkpoint.json",
        {
            "schemaVersion": 1,
            "label": label,
            "capturedAtUtc": captured_at,
            "apkSha256": APK_SHA,
            "deviceSerialSha256": DEVICE_SHA,
            "automatedRedFlags": [] if red_flags == 0 else ["synthetic fatal"],
            "automatedRedFlagCount": red_flags,
            "manualReviewRequired": True,
            "files": list(VERIFY.CHECKPOINT_FILES),
        },
    )
    (directory / "screen.png").write_bytes(b"\x89PNG\r\n\x1a\nsynthetic")
    (directory / "logcat.txt").write_text("", encoding="utf-8")
    (directory / "meminfo.txt").write_text(f"TOTAL PSS: {pss_mib * 1024}\n", encoding="utf-8")
    (directory / "gfxinfo.txt").write_text(
        f"95th percentile: {p95}ms\n99th percentile: {p99}ms\nJanky frames: 2 (1.0%)\n",
        encoding="utf-8",
    )
    (directory / "thermalservice.txt").write_text(f"Thermal Status: {thermal}\n", encoding="utf-8")
    (directory / "battery.txt").write_text("level: 80\nUSB powered: false\n", encoding="utf-8")
    (directory / "activity.txt").write_text("ResumedActivity afareet\n", encoding="utf-8")


def finish_clean_session(session_dir: Path) -> None:
    write_checkpoint(session_dir, "smoke-cold-start", "2026-08-17T06:10:00Z", pss_mib=500, p95=20, p99=25)
    write_checkpoint(session_dir, "smoke-warm-race", "2026-08-17T06:20:00Z", pss_mib=700, p95=15, p99=20)
    write_checkpoint(session_dir, "smoke-after-restarts", "2026-08-17T06:25:00Z", pss_mib=728, p95=16, p99=21)
    session = json.loads((session_dir / "session.json").read_text(encoding="utf-8"))
    session.update(
        {
            "state": "EVIDENCE_COLLECTED",
            "verdict": "MANUAL_REVIEW_REQUIRED",
            "finishedAtUtc": "2026-08-17T06:30:00Z",
            "checkpointCount": 3,
            "automatedRedFlagCount": 0,
        }
    )
    write_json(session_dir / "session.json", session)
    write_json(
        session_dir / "evidence-index.json",
        {
            "schemaVersion": 1,
            "generatedAtUtc": "2026-08-17T06:30:00Z",
            "state": "EVIDENCE_COLLECTED",
            "verdict": "MANUAL_REVIEW_REQUIRED",
            "packageId": PREPARE.EXPECTED_PACKAGE_ID,
            "apkSha256": APK_SHA,
            "deviceSerialSha256": DEVICE_SHA,
            "device": {"isEmulator": False, "model": "Physical Phone"},
            "checkpointCount": 3,
            "checkpoints": list(VERIFY.EXPECTED_CHECKPOINTS),
            "automatedRedFlagCount": 0,
            "automatedRedFlags": [],
        },
    )


def make_clean_fixture(root: Path):
    reconciliation_path = make_reconciliation(root)
    reconciliation = PREPARE.validate_reconciliation(reconciliation_path)
    apk_path = make_apk(root)
    apk = PREPARE.validate_published_apk(apk_path, reconciliation["publishedApkSha256"])
    session_dir = make_prepared_session(root, reconciliation, apk)
    finish_clean_session(session_dir)
    return reconciliation_path, apk_path, session_dir


class P1PostPublicationSmokeTests(unittest.TestCase):
    def test_prepare_binds_exact_receipt_apk_tier_and_keeps_all_promotion_flags_false(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            reconciliation_path = make_reconciliation(root)
            reconciliation = PREPARE.validate_reconciliation(reconciliation_path)
            apk = PREPARE.validate_published_apk(make_apk(root), reconciliation["publishedApkSha256"])
            session_dir = make_prepared_session(root, reconciliation, apk)
            session = json.loads((session_dir / "session.json").read_text(encoding="utf-8"))
            context = session["p1PostPublication"]
            self.assertEqual("mid", session["performanceTier"])
            self.assertEqual(PREPARE.SESSION_PROFILE, context["profile"])
            self.assertEqual(reconciliation["reconciliationSha256"], context["publicationReceiptReconciliationSha256"])
            self.assertEqual(AUTHORIZATION, context["stagingAuthorization"])
            self.assertTrue(context["humanPublicationRecorded"])
            self.assertFalse(context["publicationPerformedByTool"])
            self.assertFalse(context["verified"])
            self.assertFalse(context["runtimeVerified"])
            self.assertFalse(context["ownerAccepted"])
            self.assertFalse(context["publicationEligible"])

    def test_prepare_rejects_wrong_published_bytes_and_emulator_session(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            reconciliation_path = make_reconciliation(root)
            reconciliation = PREPARE.validate_reconciliation(reconciliation_path)
            apk_path = make_apk(root)
            apk_path.write_bytes(b"tampered")
            with self.assertRaisesRegex(PREPARE.P1PostPublicationPrepareError, "SHA-256 mismatch"):
                PREPARE.validate_published_apk(apk_path, reconciliation["publishedApkSha256"])

            apk_path.write_bytes(APK_BYTES)
            apk = PREPARE.validate_published_apk(apk_path, reconciliation["publishedApkSha256"])
            session_dir = make_prepared_session(root, reconciliation, apk, emulator=True)
            with self.assertRaisesRegex(PREPARE.P1PostPublicationPrepareError, "physical Android device"):
                PREPARE.bind_session(reconciliation, apk, session_dir, "mid", FakeDeviceEvidence)

    def test_valid_finished_smoke_is_only_passable_for_human_closure_review(self):
        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            result = VERIFY.reconcile(reconciliation_path, session_dir)
            self.assertEqual(VERIFY.RESULT_STATE, result["state"])
            self.assertEqual(VERIFY.RESULT_VERDICT, result["verdict"])
            self.assertTrue(result["humanPublicationRecorded"])
            self.assertTrue(result["postPublicationSmokeObserved"])
            self.assertTrue(result["humanClosureReviewRequired"])
            self.assertFalse(result["publicationPerformedByTool"])
            self.assertFalse(result["verified"])
            self.assertFalse(result["runtimeVerified"])
            self.assertFalse(result["ownerAccepted"])
            self.assertFalse(result["publicationEligible"])
            self.assertFalse(result["lastVerifiedPointerUpdatePerformedByTool"])
            self.assertEqual("MID", result["performanceTier"])
            self.assertEqual(26, result["evidenceFileCount"])
            self.assertEqual("PASSABLE_FOR_MANUAL_REVIEW", result["smokeMetrics"]["verdict"])
            self.assertEqual([], result["smokeMetrics"]["blockers"])

    def test_prepublication_session_and_checkpoint_evidence_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            session_path = session_dir / "session.json"
            session = json.loads(session_path.read_text(encoding="utf-8"))
            session["createdAtUtc"] = "2026-08-17T05:59:59Z"
            write_json(session_path, session)
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "timestamps"):
                VERIFY.reconcile(reconciliation_path, session_dir)

        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            checkpoint = session_dir / "checkpoints" / "smoke-cold-start" / "checkpoint.json"
            payload = json.loads(checkpoint.read_text(encoding="utf-8"))
            payload["capturedAtUtc"] = "2026-08-17T05:59:59Z"
            write_json(checkpoint, payload)
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "predates"):
                VERIFY.reconcile(reconciliation_path, session_dir)

    def test_receipt_binding_and_exact_smoke_set_are_fail_closed(self):
        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            session_path = session_dir / "session.json"
            session = json.loads(session_path.read_text(encoding="utf-8"))
            session["p1PostPublication"]["publicationReceiptReconciliationSha256"] = "f" * 64
            write_json(session_path, session)
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "binding differs"):
                VERIFY.reconcile(reconciliation_path, session_dir)

        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            extra = session_dir / "checkpoints" / "unrelated-old-evidence"
            extra.mkdir(parents=True)
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "unexpected checkpoint"):
                VERIFY.reconcile(reconciliation_path, session_dir)

    def test_red_flags_missing_evidence_and_budget_failure_are_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            index_path = session_dir / "evidence-index.json"
            index = json.loads(index_path.read_text(encoding="utf-8"))
            index["automatedRedFlagCount"] = 1
            index["automatedRedFlags"] = [{"checkpoint": "smoke-warm-race", "finding": "fatal"}]
            write_json(index_path, index)
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "red flags"):
                VERIFY.reconcile(reconciliation_path, session_dir)

        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            (session_dir / "checkpoints" / "smoke-cold-start" / "screen.png").unlink()
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "screen.png"):
                VERIFY.reconcile(reconciliation_path, session_dir)

        with tempfile.TemporaryDirectory() as tmp:
            reconciliation_path, _, session_dir = make_clean_fixture(Path(tmp))
            gfx = session_dir / "checkpoints" / "smoke-warm-race" / "gfxinfo.txt"
            gfx.write_text("95th percentile: 15ms\n99th percentile: 99ms\nJanky frames: 2 (1.0%)\n", encoding="utf-8")
            with self.assertRaisesRegex(VERIFY.P1PostPublicationSmokeError, "not passable"):
                VERIFY.reconcile(reconciliation_path, session_dir)

    def test_cli_output_is_evidence_only_and_refuses_overwrite(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            reconciliation_path, _, session_dir = make_clean_fixture(root)
            output = root / "p1-post-publication-smoke-reconciled.json"
            args = [
                "--receipt-reconciliation",
                str(reconciliation_path),
                "--session",
                str(session_dir),
                "--output",
                str(output),
            ]
            self.assertEqual(0, VERIFY.main(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(payload["humanClosureReviewRequired"])
            self.assertFalse(payload["verified"])
            self.assertEqual(2, VERIFY.main(args))

    def test_tools_contain_no_release_or_pointer_mutation_commands(self):
        for path in (
            TOOLS / "prepare_p1_post_publication_smoke.py",
            TOOLS / "verify_p1_post_publication_smoke.py",
        ):
            text = path.read_text(encoding="utf-8")
            for forbidden in (
                "git push",
                "git tag",
                "gh release create",
                "gh release upload",
                "LAST_VERIFIED_APK.md",
                "PROJECT_STATUS.md",
            ):
                self.assertNotIn(forbidden, text)


if __name__ == "__main__":
    unittest.main()
