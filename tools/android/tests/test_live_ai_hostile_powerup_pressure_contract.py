import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RACE_DIRECTOR = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs"
POLICY = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/AiHostilePowerUpPressurePolicy.cs"


class LiveAiHostilePowerUpPressureContractTests(unittest.TestCase):
    def test_race_director_no_longer_hardcodes_hostile_pressure_false(self):
        source = RACE_DIRECTOR.read_text(encoding="utf-8")
        self.assertNotIn("incomingHostilePressure: false", source)
        for required in (
            "AiHostilePowerUpPressurePolicy.HasIncomingPressure",
            "CanRacerUsePowerUp(targetAhead, PowerUpKind.AsphaltShard",
            "CanRacerUsePowerUp(chaserBehind, PowerUpKind.TrafficCurse",
            "powerUpRuntime.GetInventorySnapshot(runtime.RacerId, raceTimeSeconds)",
            "incomingHostilePressure: incomingHostilePressure",
        ):
            self.assertIn(required, source)

    def test_pressure_policy_uses_real_inventory_usability_and_existing_ai_ranges(self):
        source = POLICY.read_text(encoding="utf-8")
        for required in (
            "PowerUpInventorySnapshot",
            "slot.IsUsable",
            "AiPowerUpLiveSnapshotBuilder.EstimateGapSeconds",
            "AiPowerUpUsagePolicy.DefensiveChaserGapSeconds",
            "AiPowerUpUsagePolicy.TrafficCurseMaxTargetGapSeconds",
            "leaderAheadCanUseAsphaltShard",
            "chaserBehindCanUseTrafficCurse",
        ):
            self.assertIn(required, source)

    def test_pressure_policy_does_not_create_visual_or_placeholder_assets(self):
        source = POLICY.read_text(encoding="utf-8")
        for forbidden in (
            "GameObject.CreatePrimitive",
            "new Mesh(",
            "Resources.Load",
            "UnityEngine",
        ):
            self.assertNotIn(forbidden, source)


if __name__ == "__main__":
    unittest.main()
