import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uveh012HandlingRecoveryContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_low_speed_steering_floor_is_explicit_and_used(self):
        policy = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleHandlingPolicy.cs")
        controller = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs")
        self.assertIn("LowSpeedSteerFactor = 0.68f", policy)
        self.assertIn("FullSteerSpeedMetersPerSecond = 14f", policy)
        self.assertIn("SteeringSpeedFactor", policy)
        self.assertIn("VehicleHandlingPolicy.SteeringSpeedFactor(forwardSpeed)", controller)
        self.assertNotIn("Mathf.Lerp(.42f, 1f", controller)

    def test_stuck_recovery_requires_sustained_stationary_drive_intent(self):
        policy = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleRecoveryPolicy.cs")
        controller = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs")
        for required in (
            "StuckSpeedThresholdKph = 2.5f",
            "StuckDriveInputThreshold = 0.65f",
            "StuckSecondsBeforeAutoRecovery = 2.4f",
            "PostRecoveryInputLockSeconds = 0.35f",
            "meaningfulDriveIntent",
            "effectivelyStationary",
            "!grounded || brakeInput",
        ):
            self.assertIn(required, policy)
        self.assertIn("VehicleRecoveryPolicy.AdvanceStuckTimer", controller)
        self.assertIn("VehicleRecoveryPolicy.ShouldAutoRecover", controller)
        self.assertIn('RecoverToTrack("auto-stuck")', controller)

    def test_recovery_uses_safe_checkpoint_clearance_and_input_lock(self):
        recovery = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/VehicleRecoveryPolicy.cs")
        checkpoint = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/LastCheckpointTracker.cs")
        controller = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs")
        self.assertIn("RecoveryForwardOffsetMeters = 2.4f", recovery)
        self.assertIn("RecoveryUpOffsetMeters = 1.05f", recovery)
        self.assertIn("SafeRecoveryPosition", recovery)
        self.assertIn("MaximumCaptureDistanceSqr = 196f", checkpoint)
        self.assertIn("MinimumForwardAlignment = 0.15f", checkpoint)
        self.assertIn("checkpoint.RecoveryPosition", controller)
        self.assertIn("recoveryInputLockRemaining", controller)
        self.assertIn("AFAREET_UVEH012_RECOVERY", controller)

    def test_existing_mobile_brake_reverse_and_visible_recover_controls_remain(self):
        policy = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/MobileDriveInputPolicy.cs")
        hud = self._read("unity_game/Assets/Afareet/Scripts/UI/PrototypeHud.cs")
        self.assertIn("ResolveBrakeReverse", policy)
        self.assertIn('"BRAKE / REV"', hud)
        self.assertIn('"RECOVER"', hud)
        self.assertIn("RecoverPlayer", hud)
        self.assertIn("player.ResetToSpawn()", hud)


if __name__ == "__main__":
    unittest.main()
