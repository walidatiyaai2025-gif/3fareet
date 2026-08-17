import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LEDGER = REPO_ROOT / "EXTERNAL_ASSET_REQUESTS.txt"
AGENTS = REPO_ROOT / "AGENTS.md"


def normalized(text: str) -> str:
    return " ".join(text.split())


class ExternalAssetRequestPolicyTests(unittest.TestCase):
    def test_root_ledger_exists_and_declares_mandatory_policy(self):
        self.assertTrue(LEDGER.is_file(), "EXTERNAL_ASSET_REQUESTS.txt must stay at repository root")
        text = LEDGER.read_text(encoding="utf-8")
        for required in (
            "POLICY — MANDATORY FOR EVERY PROGRAMMER / AI AGENT",
            "Programming first.",
            "Do NOT silently substitute a primitive, generated placeholder, procedural mesh",
            "add or update an entry in THIS file in the same PR/branch",
            "Prompts must be ready to copy/paste",
            "Do NOT use a script to pretend procedural/generated art is externally authored production art",
            "Production source must have a clear license/ownership statement",
            "PROGRAMMING-ONLY WORK — DO NOT ADD AS EXTERNAL ASSET REQUESTS",
            "docs/MISSED_ASSETS.md",
        ):
            self.assertIn(required, text)

    def test_agents_instructions_require_the_external_asset_ledger(self):
        self.assertTrue(AGENTS.is_file())
        text = normalized(AGENTS.read_text(encoding="utf-8"))
        for required in (
            "EXTERNAL_ASSET_REQUESTS.txt",
            "Never silently substitute a primitive, generated mesh, procedural placeholder",
            "add or update the corresponding request",
            "copy-ready creation prompt",
            "provenance/license requirement",
            "close programming, automation, validation and test gaps before visual polish",
        ):
            self.assertIn(required, text)

    def test_active_requests_have_complete_handoff_fields(self):
        text = LEDGER.read_text(encoding="utf-8")
        request_starts = list(re.finditer(r"^REQUEST ID: (EXT-ASSET-\d{3})$", text, flags=re.MULTILINE))
        self.assertGreaterEqual(len(request_starts), 8)
        ids = [match.group(1) for match in request_starts]
        self.assertEqual(len(ids), len(set(ids)), "external asset request ids must be unique")

        required_fields = (
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
        )

        for index, match in enumerate(request_starts):
            end = request_starts[index + 1].start() if index + 1 < len(request_starts) else text.find(
                "PROGRAMMING-ONLY WORK — DO NOT ADD AS EXTERNAL ASSET REQUESTS",
                match.end(),
            )
            if end < 0:
                end = len(text)
            block = text[match.start():end]
            for field in required_fields:
                self.assertIn(field, block, f"{match.group(1)} missing {field}")
            self.assertRegex(block, r"TOOL:\s*\S+")
            self.assertRegex(block, r"CREATION PROMPT / ART BRIEF:\s*\n\".+", msg=f"{match.group(1)} needs a copy-ready prompt")

    def test_current_external_dependencies_are_registered(self):
        text = LEDGER.read_text(encoding="utf-8")
        required_by_id = {
            "EXT-ASSET-001": "Afareet King",
            "EXT-ASSET-002": "Three final production Rival vehicle sources",
            "EXT-ASSET-003": "Cairo Night production vertical-slice art upgrade pack",
            "EXT-ASSET-004": "Cairo Night production soundtrack",
            "EXT-ASSET-005": "Mobile production VFX source pack",
            "EXT-ASSET-006": "Production UI and in-game branding vector source pack",
            "EXT-ASSET-007": "Licensed Arabic + Latin production font family",
            "EXT-ASSET-008": "3FAREET production app icon master",
        }
        for request_id, description in required_by_id.items():
            self.assertIn(request_id, text)
            self.assertIn(description, text)

        for blocker in (
            "Issue #127",
            "UART-004",
            "UART-005 / UART-006 / UART-007 / URAC-011",
        ):
            self.assertIn(blocker, text)

    def test_ledger_keeps_code_responsibilities_out_of_asset_requests(self):
        text = LEDGER.read_text(encoding="utf-8")
        for engineering_responsibility in (
            "Camera collision/clearance and camera state transitions.",
            "Race state, restart, checkpoints, results and progression logic.",
            "Mobile input and HUD behavior/layout implementation.",
            "Deterministic Unity import/staging/build gates.",
            "Track rail/curb/miter placement mathematics.",
            "Performance instrumentation, smoke harness and evidence tooling.",
            "Wheel rotation, suspension/body lean and other code-solvable animation hooks.",
            "AI racing line, respawn and power-up marker generation/validation.",
        ):
            self.assertIn(engineering_responsibility, text)


if __name__ == "__main__":
    unittest.main()
