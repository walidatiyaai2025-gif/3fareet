import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
POLICY = REPO_ROOT / "ASSET_CREATION_REQUESTS.txt"
MISSED = REPO_ROOT / "docs/MISSED_ASSETS.md"


class RootAssetCreationPolicyContractTests(unittest.TestCase):
    def test_root_policy_exists_and_declares_mandatory_programming_closure_mode(self):
        self.assertTrue(POLICY.is_file(), "root ASSET_CREATION_REQUESTS.txt is mandatory")
        text = POLICY.read_text(encoding="utf-8")
        for required in (
            "Policy ID: AFA-POL-ASSET-001",
            "Status: ACTIVE / MANDATORY",
            "PROGRAMMING CLOSURE MODE",
            "MANDATORY RULE FOR EVERY PROGRAMMER / AI AGENT",
            "No entry = no untracked external-asset dependency.",
            "DO NOT fabricate a fake production asset",
            "DO NOT weaken a production gate",
            "docs/MISSED_ASSETS.md",
        ):
            self.assertIn(required, text)

    def test_request_template_contains_all_required_handoff_fields(self):
        text = POLICY.read_text(encoding="utf-8")
        required_fields = (
            "REQUEST:",
            "RELATED:",
            "ASSET:",
            "WHY CODE IS NOT ENOUGH:",
            "TOOL:",
            "SOURCE INPUTS:",
            "TARGET SOURCE PATH:",
            "TARGET EXPORT:",
            "CREATION PROMPT:",
            "SCRIPT / PROCEDURE:",
            "ACCEPTANCE:",
            "MOBILE BUDGET:",
            "PROVENANCE:",
            "STATUS:",
        )
        template = text.split("PROGRAMMER ENTRY TEMPLATE", 1)[-1]
        for field in required_fields:
            self.assertIn(field, template)

    def test_seed_queue_covers_current_external_p1_vehicle_and_environment_blockers(self):
        text = POLICY.read_text(encoding="utf-8")
        for request_id in (
            "EXT-ASSET-001",
            "EXT-ASSET-002",
            "EXT-ASSET-003",
            "EXT-ASSET-004",
            "EXT-ASSET-005",
            "EXT-ASSET-006",
            "EXT-ASSET-007",
        ):
            self.assertRegex(text, rf"REQUEST: {re.escape(request_id)}\b")

        for gate in ("UART-003", "UART-004", "UART-005", "UART-006", "UART-007", "URAC-011"):
            self.assertIn(gate, text)

    def test_asset_registry_remains_canonical_status_source(self):
        self.assertTrue(MISSED.is_file())
        missed = MISSED.read_text(encoding="utf-8")
        self.assertIn("مصدر الحقيقة الحي", missed)
        self.assertIn("Missing / production asset register", missed)


if __name__ == "__main__":
    unittest.main()
