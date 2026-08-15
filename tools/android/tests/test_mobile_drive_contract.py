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
