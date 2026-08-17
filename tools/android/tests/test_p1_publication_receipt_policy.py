import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
LAST_VERIFIED = REPO / "docs" / "releases" / "LAST_VERIFIED_APK.md"
POLICY = REPO / "docs" / "RELEASE_POLICY.md"


class P1PublicationReceiptPolicyTests(unittest.TestCase):
    def test_last_verified_p1_procedure_uses_authoritative_profiles_and_preflight(self):
        text = LAST_VERIFIED.read_text(encoding="utf-8")
        for required in (
            "p1-final-gate-lineage-v2",
            "p1-lineage-manual-approvals-v2",
            "tools/android/verify_p1_release_publication.py",
            "P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION",
            "p1-manual-publication-receipt-v1",
            "tools/android/verify_p1_publication_receipt.py",
            "humanPublicationRecorded=true",
            "publicationPerformedByTool=false",
            "post-publication physical-device smoke/performance closure",
        ):
            self.assertIn(required, text)

        p1_section = text.split("## P1 promotion procedure", 1)[1].split("## Non-P1 compatibility", 1)[0]
        self.assertNotIn("tools/android/verify_release_publication.py", p1_section)
        self.assertNotIn("ELIGIBLE_FOR_MANUAL_PUBLICATION", p1_section)
        self.assertIn("verified=false", p1_section)

    def test_last_verified_status_is_not_promoted_by_tooling_change(self):
        text = LAST_VERIFIED.read_text(encoding="utf-8")
        self.assertIn("No Unity APK has completed", text)
        current_status = text.split("## Current status", 1)[1].split("## Required record", 1)[0]
        self.assertNotIn("Status: `DEVICE VERIFIED`", current_status)

    def test_release_policy_keeps_p1_preflight_human_only(self):
        text = POLICY.read_text(encoding="utf-8")
        self.assertIn("tools/android/verify_p1_release_publication.py", text)
        self.assertIn("P1_ELIGIBLE_FOR_EXPLICIT_MANUAL_PUBLICATION", text)
        self.assertIn("publicationPerformed=false", text)
        self.assertIn("The release owner must still perform the publication action explicitly", text)
        self.assertIn("Real-device/manual approvals + exact binary hash + successful publication + recorded release evidence", text)


if __name__ == "__main__":
    unittest.main()
