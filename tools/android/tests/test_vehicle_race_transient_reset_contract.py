import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
CAR_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs"
LAP_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/OneLapRaceState.cs"
PLAYMODE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/PlayMode/VehicleRaceTransientResetPlayModeTests.cs"
PLAYMODE_META_PATH = Path(str(PLAYMODE_PATH) + ".meta")


class VehicleRaceTransientResetContractTests(unittest.TestCase):
    def test_vehicle_reset_clears_only_attempt_scoped_drive_state(self):
        source = CAR_PATH.read_text(encoding="utf-8")
        start = source.index("public void ResetRaceTransientState()")
        end = source.index("public void ResetToSpawn()")
        body = source[start:end]

        for required in (
            "ClearDriveInputs();",
            "NitroEnergy = 1f;",
            "nitroCooldownRemaining = 0f;",
            "nitroWasActive = false;",
            "DriftEnergy = 0f;",
            "DriftChargeActive = false;",
            "stuckDriveSeconds = 0f;",
            "recoveryInputLockRemaining = 0f;",
            "trail.emitting = false;",
        ):
            self.assertIn(required, body)

        # Persistent vehicle configuration/performance state must not be mutated by a race-attempt reset.
        for forbidden in (
            "config =",
            "body.mass =",
            "body.linearDamping =",
            "body.angularDamping =",
            "body.centerOfMass =",
            "externalDriveModifier =",
        ):
            self.assertNotIn(forbidden, body)

    def test_lap_start_resets_checkpoint_then_vehicle_transients_before_race_state(self):
        source = LAP_PATH.read_text(encoding="utf-8")
        start = source.index("public void StartRace()", source.index("public sealed class OneLapRaceTracker"))
        end = source.index("public void AdvanceTime", start)
        body = source[start:end]

        checkpoint = body.index("checkpointTracker.ResetProgress(firstExpectedCheckpointIndex: 1);")
        vehicle = body.index("car?.ResetRaceTransientState();")
        state = body.index("state.StartRace();")
        event = body.index("RaceStarted?.Invoke();")
        self.assertLess(checkpoint, vehicle)
        self.assertLess(vehicle, state)
        self.assertLess(state, event)

        self.assertIn("private ArcadeCarController car;", source)
        self.assertIn("car = GetComponent<ArcadeCarController>();", source)

    def test_playmode_regression_covers_consumables_drift_inputs_and_recovery_lock(self):
        source = PLAYMODE_PATH.read_text(encoding="utf-8")
        for required in (
            "LapStart_RefillsNitroAndClearsDriftCooldownAndInputs",
            "LapStart_ClearsRecoveryLockBeforeFreshPlayerInput",
            "Assert.That(car.NitroEnergy, Is.LessThan(1f));",
            "Assert.That(car.NitroCooldownRemaining, Is.GreaterThan(0f));",
            "Assert.That(car.DriftEnergy, Is.GreaterThan(0f));",
            "Assert.That(car.RecoveryInputLockRemaining, Is.GreaterThan(0f));",
            "lap.StartRace();",
            "Assert.That(car.NitroEnergy, Is.EqualTo(1f)",
            "Assert.That(car.RecoveryInputLockRemaining, Is.Zero",
        ):
            self.assertIn(required, source)

        self.assertTrue(PLAYMODE_META_PATH.is_file())
        self.assertIn("fileFormatVersion: 2", PLAYMODE_META_PATH.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
