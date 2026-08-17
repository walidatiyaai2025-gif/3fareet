import hashlib
import importlib.util
import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS = Path(__file__).resolve().parents[1]
MODULE_PATH = TOOLS / "verify_p1_publication_receipt.py"
SPEC = importlib.util.spec_from_file_location("verify_p1_publication_receipt", MODULE_PATH)
assert SPEC is not None and SPEC.loader is not None
MODULE = importlib.util.module_from_spec(SPEC)
sys.modules["verify_p1_publication_receipt"] = MODULE
SPEC.loader.exec_module(MODULE)

STAGING_SHA = "a" * 40
CANDIDATE_SHA = "b" * 40
APK_SHA = "c" * 64
REVIEW_CONTENT_SHA = "d" * 64
REVIEW_LINEAGE_SHA = "e" * 64
AUTHORIZATION = {
    "authorizationSourceGitSha": STAGING_SHA,
    "handoffPacketSha256": "1" * 64,
    "nativeHandoffVerificationSha256": "2" * 64,
    "operatorChainSha256": "3" * 64,
}


def write_json(path: Path, payload) -> None:
    path.write_text(json.dumps(payload, indent=2, sort_keys=True) + "\n", encoding="utf-8")


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def make_preflight(root: Path) -> Path:
    path = root / "p1-publication-preflight.json"
    write_json(
        path,
        {
            "schemaVersion": 1,
            "state": "P1_PUBLICATION_PREFLIGHT_PASSED",
            "verdict": "P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION",
            "eligibleForExplicitManualPublication": True,
            "publicationPerformed": False,
            "verified": False,
            "candidate": {
                "candidateType": "local-windows-licensed-unity",
                "gitSha": CANDIDATE_SHA,
                "apkSha256": APK_SHA,
            },
            "p1Lineage": {
                "stagingSourceGitSha": STAGING_SHA,
                "directParentGitSha": STAGING_SHA,
                "candidateGitSha": CANDIDATE_SHA,
                "reviewContentSetSha256": REVIEW_CONTENT_SHA,
                "p1ReviewLineageSha256": REVIEW_LINEAGE_SHA,
                "stagingAuthorization": dict(AUTHORIZATION),
            },
            "evidence": {
                "checkpointCount": 16,
                "reviewers": {"UPER-010": "Release Owner"},
            },
        },
    )
    return path


def make_receipt(root: Path, preflight: Path) -> Path:
    path = root / "p1-publication-receipt.json"
    write_json(
        path,
        {
            "schemaVersion": 1,
            "receiptProfile": "p1-manual-publication-receipt-v1",
            "publicationPreflightSha256": sha256(preflight),
            "candidateGitSha": CANDIDATE_SHA,
            "apkSha256": APK_SHA,
            "publishedApkSha256": APK_SHA,
            "stagingSourceGitSha": STAGING_SHA,
            "reviewContentSetSha256": REVIEW_CONTENT_SHA,
            "p1ReviewLineageSha256": REVIEW_LINEAGE_SHA,
            "stagingAuthorization": dict(AUTHORIZATION),
            "releaseOwner": "Release Owner",
            "publishedAtUtc": "2026-08-17T06:00:00Z",
            "gitTag": "unity-verified-v0.1.0-build.1",
            "githubReleaseUrl": "https://github.com/example/afareet/releases/tag/unity-verified-v0.1.0-build.1",
            "apkAssetUrl": "https://github.com/example/afareet/releases/download/unity-verified-v0.1.0-build.1/afareet-unity3d-last-verified.apk",
            "publicationPerformed": True,
            "verified": False,
        },
    )
    return path


class VerifyP1PublicationReceiptTests(unittest.TestCase):
    def test_valid_human_receipt_reconciles_but_tool_never_publishes_or_verifies(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            preflight = make_preflight(root)
            receipt = make_receipt(root, preflight)
            result = MODULE.reconcile(preflight, receipt)
            self.assertEqual("P1_HUMAN_PUBLICATION_RECEIPT_RECONCILED", result["state"])
            self.assertTrue(result["humanPublicationRecorded"])
            self.assertFalse(result["publicationPerformedByTool"])
            self.assertFalse(result["verified"])
            self.assertFalse(result["runtimeVerified"])
            self.assertFalse(result["ownerAccepted"])
            self.assertFalse(result["publicationEligible"])
            self.assertTrue(result["postPublicationSmokeRequired"])
            self.assertEqual(AUTHORIZATION, result["stagingAuthorization"])
            self.assertEqual(sha256(preflight), result["publicationPreflightSha256"])

    def test_preflight_hash_mismatch_is_rejected(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            preflight = make_preflight(root)
            receipt = make_receipt(root, preflight)
            payload = json.loads(receipt.read_text(encoding="utf-8"))
            payload["publicationPreflightSha256"] = "f" * 64
            write_json(receipt, payload)
            with self.assertRaisesRegex(MODULE.P1PublicationReceiptError, "preflight SHA-256 mismatch"):
                MODULE.reconcile(preflight, receipt)

    def test_candidate_apk_and_review_fingerprint_mismatch_are_rejected(self):
        fields = {
            "candidateGitSha": "f" * 40,
            "apkSha256": "f" * 64,
            "publishedApkSha256": "f" * 64,
            "stagingSourceGitSha": "f" * 40,
            "reviewContentSetSha256": "f" * 64,
            "p1ReviewLineageSha256": "f" * 64,
        }
        for field, value in fields.items():
            with self.subTest(field=field), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                preflight = make_preflight(root)
                receipt = make_receipt(root, preflight)
                payload = json.loads(receipt.read_text(encoding="utf-8"))
                payload[field] = value
                if field == "stagingSourceGitSha":
                    payload["stagingAuthorization"]["authorizationSourceGitSha"] = value
                write_json(receipt, payload)
                with self.assertRaisesRegex(MODULE.P1PublicationReceiptError, "mismatch"):
                    MODULE.reconcile(preflight, receipt)

    def test_any_staging_authorization_fingerprint_mismatch_is_rejected(self):
        for key in ("handoffPacketSha256", "nativeHandoffVerificationSha256", "operatorChainSha256"):
            with self.subTest(key=key), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                preflight = make_preflight(root)
                receipt = make_receipt(root, preflight)
                payload = json.loads(receipt.read_text(encoding="utf-8"))
                payload["stagingAuthorization"][key] = "f" * 64
                write_json(receipt, payload)
                with self.assertRaisesRegex(MODULE.P1PublicationReceiptError, "stagingAuthorization"):
                    MODULE.reconcile(preflight, receipt)

    def test_receipt_cannot_self_verify_and_must_record_human_publication(self):
        for field, value, expected in (
            ("verified", True, "VERIFIED"),
            ("publicationPerformed", False, "human publication action"),
        ):
            with self.subTest(field=field), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                preflight = make_preflight(root)
                receipt = make_receipt(root, preflight)
                payload = json.loads(receipt.read_text(encoding="utf-8"))
                payload[field] = value
                write_json(receipt, payload)
                with self.assertRaisesRegex(MODULE.P1PublicationReceiptError, expected):
                    MODULE.reconcile(preflight, receipt)

    def test_preflight_itself_must_remain_nonpublishing_and_unverified(self):
        for field in ("publicationPerformed", "verified"):
            with self.subTest(field=field), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                preflight = make_preflight(root)
                payload = json.loads(preflight.read_text(encoding="utf-8"))
                payload[field] = True
                write_json(preflight, payload)
                receipt = make_receipt(root, preflight)
                with self.assertRaises(MODULE.P1PublicationReceiptError):
                    MODULE.reconcile(preflight, receipt)

    def test_owner_timestamp_tag_and_release_urls_are_required(self):
        mutations = {
            "releaseOwner": "",
            "publishedAtUtc": "2026-08-17T06:00:00",
            "gitTag": "",
            "githubReleaseUrl": "http://example.invalid/release",
            "apkAssetUrl": "",
        }
        for field, value in mutations.items():
            with self.subTest(field=field), tempfile.TemporaryDirectory() as tmp:
                root = Path(tmp)
                preflight = make_preflight(root)
                receipt = make_receipt(root, preflight)
                payload = json.loads(receipt.read_text(encoding="utf-8"))
                payload[field] = value
                write_json(receipt, payload)
                with self.assertRaises(MODULE.P1PublicationReceiptError):
                    MODULE.reconcile(preflight, receipt)

    def test_cli_output_is_reconciliation_only_and_refuses_overwrite(self):
        with tempfile.TemporaryDirectory() as tmp:
            root = Path(tmp)
            preflight = make_preflight(root)
            receipt = make_receipt(root, preflight)
            output = root / "reconciled.json"
            args = ["--preflight", str(preflight), "--receipt", str(receipt), "--output", str(output)]
            self.assertEqual(0, MODULE.main(args))
            payload = json.loads(output.read_text(encoding="utf-8"))
            self.assertTrue(payload["humanPublicationRecorded"])
            self.assertFalse(payload["publicationPerformedByTool"])
            self.assertFalse(payload["verified"])
            self.assertEqual(2, MODULE.main(args))

    def test_source_has_no_remote_publication_or_repository_pointer_mutation(self):
        text = MODULE_PATH.read_text(encoding="utf-8")
        for forbidden in (
            "subprocess",
            "os.system",
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
