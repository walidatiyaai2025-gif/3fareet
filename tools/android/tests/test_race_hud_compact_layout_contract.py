import re
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
HUD_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceHud.cs"


def method_body(source: str, signature: str) -> str:
    start = source.index(signature)
    brace = source.index("{", start)
    depth = 0
    for index in range(brace, len(source)):
        value = source[index]
        if value == "{":
            depth += 1
        elif value == "}":
            depth -= 1
            if depth == 0:
                return source[brace + 1:index]
    raise AssertionError(f"Unterminated method body for {signature}")


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

    def test_ready_state_hides_drive_only_telemetry_without_hiding_core_hud(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        for required in (
            'var showDriveTelemetry = race.Phase != RaceRoundPhase.Ready;',
            'SetPanelActive(spiritText, showDriveTelemetry);',
            'SetPanelActive(speedText, showDriveTelemetry);',
            'private static void SetPanelActive(Text text, bool active)',
            'panel.SetActive(active);',
            'AFAREET_UI_READY_TELEMETRY_CLEARANCE_ACTIVE',
            'driveTelemetryHiddenUntilStart=true',
            'positionTimeCareerRemainVisible=true',
        ):
            self.assertIn(required, hud)

        # Position, time and career are intentionally not toggled by the ready-state
        # clearance policy; they remain useful before the race starts.
        self.assertNotIn('SetPanelActive(positionText, showDriveTelemetry);', hud)
        self.assertNotIn('SetPanelActive(timeText, showDriveTelemetry);', hud)
        self.assertNotIn('SetPanelActive(careerText, showDriveTelemetry);', hud)

    def test_allocation_heavy_race_telemetry_is_sampled_instead_of_formatted_every_frame(self):
        hud = HUD_PATH.read_text(encoding="utf-8")

        for required in (
            'private const float TelemetryRefreshIntervalSeconds = .1f;',
            'private const float CareerRefreshIntervalSeconds = .25f;',
            'var now = Time.unscaledTime;',
            'nextTelemetryRefreshTime = now + TelemetryRefreshIntervalSeconds;',
            'nextCareerRefreshTime = now + CareerRefreshIntervalSeconds;',
            'private void RefreshTelemetry()',
            'private void RefreshCareer()',
        ):
            self.assertIn(required, hud)

        update = method_body(hud, 'private void Update()')
        telemetry = method_body(hud, 'private void RefreshTelemetry()')
        career = method_body(hud, 'private void RefreshCareer()')

        # Ranking captures allocate ordered race snapshots, so the render-loop Update
        # must never interpolate Position directly. Comments may name the property when
        # explaining the contract, therefore assert the executable interpolation token.
        self.assertNotIn('{race.Position}', update)
        self.assertEqual(telemetry.count('{race.Position}'), 1)

        # Keep the actual fill responsive every render frame while formatted strings are
        # sampled; this prevents the optimization from making the Spirit bar visibly choppy.
        self.assertIn('spiritFill.fillAmount = Mathf.Clamp01(player.NitroEnergy);', update)
        self.assertNotIn('spiritFill.fillAmount', telemetry)

        # Allocation-heavy formatted HUD strings must live in sampled helper methods, not
        # directly inside the frame-rate Update method.
        self.assertNotRegex(update, re.compile(r'\.text\s*=\s*\$"'))
        self.assertGreaterEqual(telemetry.count('.text = $"'), 4)
        self.assertGreaterEqual(career.count('.text = $"'), 3)


if __name__ == "__main__":
    unittest.main()
