import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
OVERLAY = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionCareerBriefingOverlay.cs"


class CareerBriefingOverlayLayoutContractTests(unittest.TestCase):
    def test_briefing_is_compact_and_keeps_racing_sightline_open(self):
        source = OVERLAY.read_text(encoding="utf-8")

        for required in (
            "panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);",
            "panelRect.pivot = new Vector2(0f, 1f);",
            "panelRect.sizeDelta = new Vector2(430f, 190f);",
            "panelRect.anchoredPosition = new Vector2(20f, -88f);",
            "new Color(.012f, .008f, .035f, .82f)",
            "titleText.alignment = TextAnchor.MiddleLeft;",
            "objectivesText.alignment = TextAnchor.UpperLeft;",
            "profileText.alignment = TextAnchor.MiddleLeft;",
        ):
            self.assertIn(required, source)

        self.assertNotIn("panelRect.sizeDelta = new Vector2(720f, 410f);", source)
        self.assertNotIn("panelRect.anchoredPosition = Vector2.zero;", source)

    def test_briefing_remains_ready_phase_only(self):
        source = OVERLAY.read_text(encoding="utf-8")
        self.assertIn("var visible = race.Phase == RaceRoundPhase.Ready;", source)
        self.assertIn("briefingPanel.SetActive(visible);", source)


if __name__ == "__main__":
    unittest.main()
