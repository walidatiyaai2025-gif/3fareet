import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CANONICAL = REPO_ROOT / "EXTERNAL_ASSET_REQUESTS.txt"
ALIAS = REPO_ROOT / "ASSET_CREATION_REQUESTS.txt"
MISSED = REPO_ROOT / "docs/MISSED_ASSETS.md"


class RootAssetCreationPolicyContractTests(unittest.TestCase):
    def test_canonical_root_ledger_declares_programming_first_policy(self):
        self.assertTrue(CANONICAL.is_file(), "root EXTERNAL_ASSET_REQUESTS.txt is mandatory")
        text = CANONICAL.read_text(encoding="utf-8")
        for required in (
            "EXTERNAL ASSET REQUEST POLICY & ACTIVE REQUESTS",
            "POLICY — MANDATORY FOR EVERY PROGRAMMER / AI AGENT",
            "Programming first.",
            "Do NOT silently substitute a primitive, generated placeholder",
            "Every request entry MUST contain",
            "Prompts must be ready to copy/paste",
            "docs/MISSED_ASSETS.md",
        ):
            self.assertIn(required, text)

    def test_canonical_request_template_has_copy_ready_handoff_fields(self):
        text = CANONICAL.read_text(encoding="utf-8")
        for field in (
            "REQUEST ID:",
            "STATUS:",
            "BLOCKS:",
            "ASSET NAME:",
            "PURPOSE:",
            "TOOL:",
            "HELPER SCRIPT / WORKFLOW:",
            "OUTPUT / DESTINATION:",
            "CREATION PROMPT / ART BRIEF:",
            "TECHNICAL CONSTRAINTS:",
            "ACCEPTANCE CRITERIA:",
            "PROVENANCE / LICENSE:",
            "INTEGRATION NOTES:",
        ):
            self.assertIn(field, text)

    def test_current_external_dependencies_are_registered_in_canonical_ledger(self):
        text = CANONICAL.read_text(encoding="utf-8")
        for request_id in ("EXT-ASSET-001", "EXT-ASSET-002", "EXT-ASSET-003", "EXT-ASSET-004", "EXT-ASSET-005"):
            self.assertIn(f"REQUEST ID: {request_id}", text)
        for token in ("UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011", "Asphalt Shard"):
            self.assertIn(token, text)

    def test_alias_points_to_canonical_ledger_and_contains_no_duplicate_requests(self):
        self.assertTrue(ALIAS.is_file())
        text = ALIAS.read_text(encoding="utf-8")
        self.assertIn("COMPATIBILITY POINTER — DO NOT ADD REQUESTS HERE", text)
        self.assertIn("EXTERNAL_ASSET_REQUESTS.txt", text)
        self.assertNotIn("REQUEST ID: EXT-ASSET-", text)

    def test_asset_registry_remains_detailed_status_source(self):
        self.assertTrue(MISSED.is_file())
        missed = MISSED.read_text(encoding="utf-8")
        self.assertIn("مصدر الحقيقة الحي", missed)
        self.assertIn("Missing / production asset register", missed)


if __name__ == "__main__":
    unittest.main()
