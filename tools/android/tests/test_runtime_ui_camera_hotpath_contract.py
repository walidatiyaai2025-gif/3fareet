import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
CAMERA_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ChaseCamera.cs"
HUD_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceHud.cs"
FLOW_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceFlowOverlay.cs"


class RuntimeUiCameraHotpathContractTests(unittest.TestCase):
    def test_chase_camera_caches_target_vehicle_with_lazy_fallback(self):
        source = CAMERA_PATH.read_text(encoding="utf-8")
        configure_start = source.index("public void Configure(Transform target, ChaseCameraConfig cameraConfig)")
        configure = source[configure_start:]
        late_start = source.index("private void LateUpdate()")
        late_end = configure_start
        late = source[late_start:late_end]

        self.assertIn("private ArcadeCarController targetCar;", source)
        self.assertIn("targetCar = target.GetComponent<ArcadeCarController>();", configure)
        self.assertIn("if (targetCar == null)", late)
        self.assertIn("targetCar = Target.GetComponent<ArcadeCarController>();", late)
        self.assertIn("targetCar.NitroActive", late)
        self.assertNotIn("var car = Target.GetComponent<ArcadeCarController>();", late)

    def test_hud_samples_formatted_telemetry_but_keeps_spirit_fill_frame_responsive(self):
        source = HUD_PATH.read_text(encoding="utf-8")
        update_start = source.index("private void Update()")
        refresh_start = source.index("private void RefreshTelemetry()", update_start)
        update = source[update_start:refresh_start]
        resolve_start = source.index("private bool ResolveRuntime()", refresh_start)
        refresh = source[refresh_start:resolve_start]

        for required in (
            "private const float TelemetryRefreshIntervalSeconds = .1f;",
            "private float nextTelemetryRefreshTime;",
            "var now = Time.unscaledTime;",
            "if (now + .0001f >= nextTelemetryRefreshTime)",
            "RefreshTelemetry();",
            "nextTelemetryRefreshTime = now + TelemetryRefreshIntervalSeconds;",
            "spiritFill.fillAmount = Mathf.Clamp01(player.NitroEnergy);",
        ):
            self.assertIn(required, source)

        self.assertNotIn("race.Position", update)
        self.assertNotIn("positionText.text", update)
        self.assertIn('positionText.text = $"POS  {race.Position}/4";', refresh)
        self.assertIn("timeText.text", refresh)
        self.assertIn("speedText.text", refresh)
        self.assertIn("spiritText.text", refresh)
        self.assertNotIn("spiritFill.fillAmount", refresh)

    def test_results_ranking_formatting_is_guarded_by_visible_results_mode(self):
        source = FLOW_PATH.read_text(encoding="utf-8")
        start = source.index("private void RefreshPresentation()")
        end = source.index("private static bool Contains", start)
        method = source[start:end]

        self.assertIn("var showingResults = mode == RaceOverlayMode.Results;", method)
        self.assertIn("resultsPanel.SetActive(showingResults);", method)
        self.assertIn("if (!showingResults)", method)
        guard = method.index("if (!showingResults)")
        ranking = method.index("race.Position")
        finish = method.index("race.FinishTime")
        self.assertLess(guard, ranking)
        self.assertLess(guard, finish)


if __name__ == "__main__":
    unittest.main()
