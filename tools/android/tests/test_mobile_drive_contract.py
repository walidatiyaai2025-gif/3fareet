import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
HUD_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/PrototypeHud.cs"
CONTROLLER_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs"
POLICY_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/MobileDriveInputPolicy.cs"


class MobileDriveContractTests(unittest.TestCase):
    def test_mobile_hud_exposes_brake_reverse_and_recovery(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        self.assertIn('"BRAKE / REV"', hud)
        self.assertIn('"RECOVER"', hud)
        self.assertIn("MobileDriveInputPolicy.ResolveBrakeReverse", hud)
        self.assertIn("player.ResetToSpawn();", hud)

    def test_mobile_steering_uses_reduced_policy_not_full_lock_literals(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        policy = POLICY_PATH.read_text(encoding="utf-8")
        self.assertIn("MobileDriveInputPolicy.ResolveTouchSteer(-1f)", hud)
        self.assertIn("MobileDriveInputPolicy.ResolveTouchSteer(1f)", hud)
        self.assertIn("TouchSteerMagnitude = 0.60f", policy)
        self.assertIn("-TouchSteerMagnitude", policy)
        self.assertIn("TouchSteerMagnitude);", policy)

    def test_motion_drive_is_sensor_guarded_calibrated_smoothed_and_hands_free(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        policy = POLICY_PATH.read_text(encoding="utf-8")

        for required in (
            "Application.isMobilePlatform && SystemInfo.supportsAccelerometer;",
            "MotionDrivingAvailable && hasMotionBaseline;",
            "private ScreenOrientation motionBaselineOrientation;",
            "private float smoothedTiltSteer;",
            "private float smoothedTiltThrottle;",
            "CalibrateMotionInput();",
            "RecalibrateIfLandscapeOrientationChanged();",
            "private void OnApplicationFocus(bool hasFocus)",
            "MobileDriveInputPolicy.ResolveLandscapeSteeringTilt(",
            "MobileDriveInputPolicy.ResolveTiltSteer(steeringTilt)",
            "MobileDriveInputPolicy.ResolveTiltCruiseThrottle(forwardTilt)",
            "MobileDriveInputPolicy.SmoothTiltSteer(",
            "MobileDriveInputPolicy.SmoothTiltThrottle(",
            "InvalidateMotionCalibration();",
            '"HYBRID DRIVE • TILT + TOUCH"',
            '"TOUCH DRIVE"',
        ):
            self.assertIn(required, hud)

        for required in (
            "TiltDeadZone = 0.08f",
            "TiltSteerSmoothingPerSecond = 10f",
            "TiltThrottleDeadZone = 0.06f",
            "TiltCruiseThrottle = 0.58f",
            "TiltForwardBoostGain = 2.0f",
            "TiltBackwardCoastGain = 1.6f",
            "TiltThrottleSmoothingPerSecond = 6f",
            "public static float ResolveTiltCruiseThrottle(float forwardTilt)",
            "public static float SmoothTiltSteer(float current, float target, float deltaTime)",
            "public static float SmoothTiltThrottle(float current, float target, float deltaTime)",
        ):
            self.assertIn(required, policy)

        self.assertNotIn("nitroInput = forwardTilt", hud)
        self.assertNotIn("brakeInput = forwardTilt", hud)
        self.assertNotIn("throttleInput = forwardTilt", hud)

    def test_motion_mode_keeps_touch_controls_visible_and_active_for_hybrid_drive(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        draw_start = hud.index("private void DrawTouchControls()")
        pointer_start = hud.index("private void ApplyPointer(Vector2 point)")
        recover_start = hud.index("private void RecoverPlayer()")
        draw_body = hud[draw_start:pointer_start]
        pointer_body = hud[pointer_start:recover_start]

        self.assertIn("var motionDriving = MotionDrivingActive;", draw_body)
        self.assertNotIn("if (!motionDriving)", draw_body)
        for control in (
            'GUI.Box(LeftRect(), "<"',
            'GUI.Box(RightRect(), ">"',
            'GUI.Box(ThrottleRect(), "GO"',
            'GUI.Box(BrakeReverseRect(), "BRAKE / REV"',
            'GUI.Box(RecoverRect(), "RECOVER"',
            'GUI.Box(DriftRect(), "DRIFT"',
            'GUI.Box(NitroRect(), "SPIRIT"',
            '"HYBRID DRIVE • TILT + TOUCH"',
        ):
            self.assertIn(control, draw_body)

        self.assertNotIn("if (!MotionDrivingActive)", pointer_body)
        for continuous in (
            "LeftRect().Contains(point)",
            "RightRect().Contains(point)",
            "ThrottleRect().Contains(point)",
        ):
            self.assertIn(continuous, pointer_body)
        for exceptional in (
            "BrakeReverseRect().Contains(point)",
            "DriftRect().Contains(point)",
            "NitroRect().Contains(point)",
        ):
            self.assertIn(exceptional, pointer_body)

    def test_landscape_motion_steering_axis_matches_owner_device_feedback(self):
        hud = HUD_PATH.read_text(encoding="utf-8")
        policy = POLICY_PATH.read_text(encoding="utf-8")
        self.assertIn("MobileDriveInputPolicy.ResolveLandscapeSteeringTilt(", hud)
        self.assertIn(
            "return landscapeRight ? -deviceYDelta : deviceYDelta;",
            policy,
        )
        self.assertNotIn(
            "var steeringTilt = landscapeRight ? acceleration.y : -acceleration.y;",
            hud,
        )

    def test_recovery_clears_latched_vehicle_inputs(self):
        controller = CONTROLLER_PATH.read_text(encoding="utf-8")
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


if __name__ == "__main__":
    unittest.main()
