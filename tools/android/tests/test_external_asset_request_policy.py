import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LEDGER = REPO_ROOT / "EXTERNAL_ASSET_REQUESTS.txt"
AGENTS = REPO_ROOT / "AGENTS.md"


def normalized(text: str) -> str:
    return " ".join(text.split())


def request_block(text: str, request_id: str) -> str:
    start = text.index(f"REQUEST ID: {request_id}")
    next_request = text.find("\nREQUEST ID: EXT-ASSET-", start + 1)
    programming_only = text.find("PROGRAMMING-ONLY WORK — DO NOT ADD AS EXTERNAL ASSET REQUESTS", start + 1)
    ends = [value for value in (next_request, programming_only) if value >= 0]
    end = min(ends) if ends else len(text)
    return text[start:end]


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

    def test_hero_request_requires_both_vertex_and_triangle_lod_ranges(self):
        block = request_block(LEDGER.read_text(encoding="utf-8"), "EXT-ASSET-001")
        for required in (
            "LOD0 1500–5000 vertices AND 3500–7500 triangles",
            "LOD1 800–2800 vertices AND 1600–4000 triangles",
            "LOD2 500–1800 vertices AND 900–2500 triangles",
            "Both dimensions must pass for every LOD",
            "triangle-only budget report is insufficient",
            "within both vertex and triangle policy ranges",
            "no triangle-only or companion-only diagnostic may substitute for this gate",
            "STATUS: OPEN",
        ):
            self.assertIn(required, block)
        self.assertIn("No procedural/generated classification in final metadata.", block)
        self.assertIn("generated/procedural candidates or triangle-only budget passes cannot satisfy", block)

    def test_rival_request_matches_isolated_production_staging_contract_without_inventing_vertex_policy(self):
        block = request_block(LEDGER.read_text(encoding="utf-8"), "EXT-ASSET-002")
        for required in (
            "STATUS: OPEN",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_01_WedgeCoupe_Production.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_02_FastbackMuscle_Production.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Production/Rival_03_CompactPrototype_Production.obj",
            "RivalImportedLodResolver",
            "LOD0 1800–16000 triangles",
            "LOD1 800–8000",
            "LOD2 350–4000",
            "Do not invent or claim a vertex acceptance range that is not in RivalProductionPolicy",
            "Review/Generated/Preview/Refinement/Blockout sources are not production-authority paths",
            "copying/renaming/rebinding the review geometry does not satisfy this request",
        ):
            self.assertIn(required, block)
        for review_source_as_final in (
            "Required final Unity exchange: Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Required final Unity exchange: Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Required final Unity exchange: Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj",
        ):
            self.assertNotIn(review_source_as_final, block)

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
