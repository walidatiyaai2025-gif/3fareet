import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CONTROLS_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceControlsOverlay.cs"
INPUT_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceInputController.cs"
BOOTSTRAP_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Core/AfareetBootstrap.cs"
VEHICLE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs"
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

    def test_mobile_steering_uses_reduced_policy_not_full_lock_literals(self):
        controls = CONTROLS_PATH.read_text(encoding="utf-8")
        policy = POLICY_PATH.read_text(encoding="utf-8")

        self.assertIn("MobileDriveInputPolicy.ResolveTouchSteer(-1f)", controls)
        self.assertIn("MobileDriveInputPolicy.ResolveTouchSteer(1f)", controls)
        self.assertIn("TouchSteerMagnitude = 0.60f", policy)
        self.assertIn("-TouchSteerMagnitude", policy)
        self.assertIn("TouchSteerMagnitude);", policy)

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


if __name__ == "__main__":
    unittest.main()
