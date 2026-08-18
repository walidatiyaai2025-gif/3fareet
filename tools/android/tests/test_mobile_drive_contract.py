import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CONTROLS_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceControlsOverlay.cs"
INPUT_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceInputController.cs"
BOOTSTRAP_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Core/AfareetBootstrap.cs"
VEHICLE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs"
RACE_DIRECTOR_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs"
POLICY_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/MobileDriveInputPolicy.cs"


class MobileDriveContractTests(unittest.TestCase):
    def test_production_controls_expose_all_uveh012_touch_actions(self):
        controls = CONTROLS_PATH.read_text(encoding="utf-8")
        input_controller = INPUT_PATH.read_text(encoding="utf-8")

        for label in ('"BRAKE / REV"', '"RECOVER"', '"DRIFT"', '"SPIRIT"', '"GO"'):
            self.assertIn(label, controls)

        self.assertIn("input.ApplyDriveFrame(steer, throttle, drift, nitro, brakeReverse);", controls)
        self.assertIn("MobileDriveInputPolicy.ResolveBrakeReverse", input_controller)
        self.assertIn("player.SetPlayerInput(0f, 0f, false, false, false);", input_controller)
        self.assertIn("player.ResetToSpawn();", input_controller)

    def test_control_labels_are_visible_and_ready_action_does_not_cover_driving_center(self):
        controls = CONTROLS_PATH.read_text(encoding="utf-8")
        self.assertIn("label.text = labelText;", controls)
        self.assertIn(
            'startRect = CreateControl("START RACE", new Vector2(1f, 1f), new Vector2(-24f, -92f), new Vector2(190f, 52f), out _);',
            controls,
        )
        self.assertNotIn(
            'startRect = CreateControl("START RACE", new Vector2(.5f, .5f), Vector2.zero, new Vector2(320f, 92f), out _);',
            controls,
        )
        self.assertIn("image.color = new Color(.17f, .035f, .30f, .70f);", controls)

    def test_driving_controls_keep_center_sightline_clear(self):
        controls = CONTROLS_PATH.read_text(encoding="utf-8")
        for required in (
            'leftRect = CreateControl("◀", new Vector2(0f, 0f), new Vector2(24f, 20f), new Vector2(70f, 54f), out _);',
            'recoverRect = CreateControl("RECOVER", new Vector2(0f, 0f), new Vector2(292f, 20f), new Vector2(92f, 54f), out _);',
            'driftRect = CreateControl("DRIFT", new Vector2(1f, 0f), new Vector2(-286f, 20f), new Vector2(82f, 54f), out _);',
            'throttleRect = CreateControl("GO", new Vector2(1f, 0f), new Vector2(-106f, 20f), new Vector2(82f, 54f), out _);',
            "var startX = -192f;",
            "new Vector2(startX + index * 96f, 82f)",
            "new Vector2(88f, 44f)",
            "AFAREET_UI_COMPACT_RACE_CONTROLS_ACTIVE",
            "centerSightlineClear=true",
        ):
            self.assertIn(required, controls)

    def test_powerup_inventory_labels_are_sampled_not_render_rate(self):
        controls = CONTROLS_PATH.read_text(encoding="utf-8")
        presentation_start = controls.index("private void RefreshPresentation()")
        presentation_end = controls.index("private void RefreshPowerUpInventory()", presentation_start)
        presentation = controls[presentation_start:presentation_end]
        inventory_start = presentation_end
        inventory_end = controls.index("private void BuildUi()", inventory_start)
        inventory_refresh = controls[inventory_start:inventory_end]

        for required in (
            "private const float PowerUpInventoryRefreshIntervalSeconds = .1f;",
            "private float nextPowerUpInventoryRefreshTime;",
            "if (!driving)",
            "nextPowerUpInventoryRefreshTime = 0f;",
            "var now = Time.unscaledTime;",
            "if (now + .0001f < nextPowerUpInventoryRefreshTime)",
            "nextPowerUpInventoryRefreshTime = now + PowerUpInventoryRefreshIntervalSeconds;",
            "RefreshPowerUpInventory();",
        ):
            self.assertIn(required, controls)

        self.assertNotIn("race.GetPlayerPowerUpInventory()", presentation)
        self.assertIn("var inventory = race.GetPlayerPowerUpInventory();", inventory_refresh)
        self.assertEqual(controls.count("race.GetPlayerPowerUpInventory()"), 1)

    def test_mobile_steering_uses_reduced_policy_not_full_lock_literals(self):
        controls = CONTROLS_PATH.read_text(encoding="utf-8")
        policy = POLICY_PATH.read_text(encoding="utf-8")

        self.assertIn("MobileDriveInputPolicy.ResolveTouchSteer(-1f)", controls)
        self.assertIn("MobileDriveInputPolicy.ResolveTouchSteer(1f)", controls)
        self.assertIn("TouchSteerMagnitude = 0.60f", policy)
        self.assertIn("-TouchSteerMagnitude", policy)
        self.assertIn("TouchSteerMagnitude);", policy)

    def test_motion_drive_is_calibrated_smoothed_and_forward_tilt_is_throttle_only(self):
        input_controller = INPUT_PATH.read_text(encoding="utf-8")
        policy = POLICY_PATH.read_text(encoding="utf-8")

        for required in (
            "private ScreenOrientation motionBaselineOrientation;",
            "private float smoothedTiltSteer;",
            "private float smoothedTiltThrottle;",
            "CalibrateMotionInput();",
            "RecalibrateIfLandscapeOrientationChanged();",
            "MobileDriveInputPolicy.ResolveTiltSteer(steeringTilt)",
            "MobileDriveInputPolicy.ResolveTiltThrottle(forwardTilt)",
            "MobileDriveInputPolicy.SmoothTiltSteer(",
            "MobileDriveInputPolicy.SmoothTiltThrottle(",
            "resolvedThrottle = Mathf.Max(resolvedThrottle, smoothedTiltThrottle);",
        ):
            self.assertIn(required, input_controller)

        for required in (
            "TiltDeadZone = 0.08f",
            "TiltSteerSmoothingPerSecond = 10f",
            "TiltThrottleDeadZone = 0.06f",
            "TiltThrottleGain = 1.8f",
            "TiltThrottleSmoothingPerSecond = 6f",
            "public static float ResolveTiltThrottle(float forwardTilt)",
            "public static float SmoothTiltSteer(float current, float target, float deltaTime)",
            "public static float SmoothTiltThrottle(float current, float target, float deltaTime)",
        ):
            self.assertIn(required, policy)

        self.assertNotIn("resolvedNitro |= forwardTilt", input_controller)
        self.assertNotIn("resolvedBrake |= forwardTilt", input_controller)
        self.assertNotIn("resolvedThrottle = forwardTilt", input_controller)

    def test_runtime_composition_uses_production_input_not_prototype_hud(self):
        bootstrap = BOOTSTRAP_PATH.read_text(encoding="utf-8")

        self.assertIn("gameObject.AddComponent<ProductionRaceInputController>()", bootstrap)
        self.assertIn("ProductionRaceControlsOverlay.EnsureInstalled(transform)", bootstrap)
        self.assertIn("controls.Configure(race, input);", bootstrap)
        self.assertNotIn("AddComponent<PrototypeHud>", bootstrap)
        self.assertNotIn("PrototypeHud.EnsureInstalled", bootstrap)

    def test_recovery_clears_latched_vehicle_inputs(self):
        controller = VEHICLE_PATH.read_text(encoding="utf-8")
        reset_start = controller.index("public void ResetToSpawn()")
        reset_body = controller[reset_start:]
        for required in (
            "throttleInput = 0f;",
            "steerInput = 0f;",
            "driftInput = false;",
            "nitroInput = false;",
            "brakeInput = false;",
        ):
            self.assertIn(required, reset_body)

    def test_kinematic_racer_never_receives_runtime_velocity_writes(self):
        controller = VEHICLE_PATH.read_text(encoding="utf-8")
        fixed_start = controller.index("private void FixedUpdate()")
        fixed_end = controller.index("private void ReadDesktopInput()", fixed_start)
        fixed_body = controller[fixed_start:fixed_end]

        guard = "if (body.isKinematic)"
        first_velocity_write = "body.linearVelocity ="
        self.assertIn(guard, fixed_body)
        self.assertIn("foreach (var trail in trails) trail.emitting = false;", fixed_body)
        self.assertLess(fixed_body.index(guard), fixed_body.index(first_velocity_write))

        recovery_start = controller.index("private void RecoverToTrack(string reason)")
        recovery_body = controller[recovery_start:]
        self.assertIn("if (!body.isKinematic)", recovery_body)
        self.assertLess(
            recovery_body.index("if (!body.isKinematic)"),
            recovery_body.index("body.linearVelocity = Vector3.zero;"),
        )

    def test_race_director_freeze_and_grid_reset_are_kinematic_safe(self):
        director = RACE_DIRECTOR_PATH.read_text(encoding="utf-8")

        freeze_start = director.index("private void FreezeRacer(ArcadeCarController car)")
        freeze_end = director.index("private void SetRivalRecoveryActive", freeze_start)
        freeze_body = director[freeze_start:freeze_end]
        self.assertIn("if (!body.isKinematic)", freeze_body)
        self.assertLess(
            freeze_body.index("if (!body.isKinematic)"),
            freeze_body.index("body.linearVelocity = Vector3.zero;"),
        )
        self.assertLess(
            freeze_body.index("body.angularVelocity = Vector3.zero;"),
            freeze_body.index("body.isKinematic = true;"),
        )

        grid_start = director.index("private void ResetRacersToGrid()")
        grid_end = director.index("private IReadOnlyList<RankedRaceEntry>", grid_start)
        grid_body = director[grid_start:grid_end]
        self.assertIn("if (!body.isKinematic)", grid_body)
        self.assertIn("body.position = targetPosition;", grid_body)
        self.assertIn("body.rotation = rotation;", grid_body)
        self.assertLess(
            grid_body.index("if (!body.isKinematic)"),
            grid_body.index("body.linearVelocity = Vector3.zero;"),
        )


if __name__ == "__main__":
    unittest.main()
