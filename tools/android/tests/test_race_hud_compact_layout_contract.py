import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
HUD_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceHud.cs"


class CompactRaceHudContractTests(unittest.TestCase):
    def test_telemetry_stays_in_top_band_and_bottom_controls_remain_clear(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        for required in (
            'positionText = Panel("Position", new Vector2(24f, -20f), new Vector2(170f, 48f)',
            'timeText = Panel("Time", new Vector2(-24f, -20f), new Vector2(170f, 48f)',
            'spiritText = Panel("Spirit", new Vector2(24f, -76f), new Vector2(210f, 48f)',
            'speedText = Panel("Speed", new Vector2(-24f, -76f), new Vector2(150f, 58f)',
            'careerText = CenterTopPanel("Career", new Vector2(0f, -20f), new Vector2(360f, 48f));',
            'SetAnchored(fillBg.rectTransform, new Vector2(12f, 8f), new Vector2(186f, 10f)',
            'AFAREET_UI_COMPACT_RACE_HUD_ACTIVE',
            'bottomControlBandClear=true',
        ):
            self.assertIn(required, hud)

        self.assertNotIn(
            'speedText = Panel("Speed", new Vector2(-24, 24), new Vector2(230, 105)',
            hud,
        )
        self.assertNotIn(
            'spiritText = Panel("Spirit", new Vector2(24, 24), new Vector2(275, 74)',
            hud,
        )


if __name__ == "__main__":
    unittest.main()
