import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
RUNTIME = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/PowerUpRaceRuntime.cs"
DEFAULTS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/PowerUpRuntimeDefaults.cs"
TRAPS = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/AsphaltShardTrapRuntime.cs"
DIRECTOR = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs"
COMPILE = REPO_ROOT / "tools/android/contracts/PowerUpRuntimeCompile.csproj"
RUNNER = REPO_ROOT / "tools/android/contracts/PowerUpRuntimeContractRunner.cs"
LEDGER = REPO_ROOT / "EXTERNAL_ASSET_REQUESTS.txt"


class AsphaltShardDeployableContractTests(unittest.TestCase):
    def test_asphalt_rule_is_world_deployable_not_direct_opponent_hit(self):
        runtime = RUNTIME.read_text(encoding="utf-8")
        defaults = DEFAULTS.read_text(encoding="utf-8")
        self.assertIn("WorldDeployable = 2", runtime)
        self.assertIn("return PowerUpRuntimeTargetMode.WorldDeployable;", runtime)
        self.assertIn("targetMode: PowerUpRuntimeTargetMode.WorldDeployable", defaults)
        self.assertIn("rule.TargetMode == PowerUpRuntimeTargetMode.WorldDeployable", runtime)
        self.assertIn("TryApplyDeployedEffect", runtime)
        self.assertIn("Only Asphalt Shard may be applied through the deployable-effect bridge.", runtime)

    def test_trap_lifecycle_is_pure_deterministic_and_one_shot(self):
        source = TRAPS.read_text(encoding="utf-8")
        for required in (
            "ArmDelaySeconds = 0.35d",
            "LifetimeSeconds = 8d",
            "TriggerRadiusMeters = 2.25d",
            "PlacementBehindVehicleMeters = 2.75d",
            "SourceRacerId",
            "IsConsumed",
            "TryTrigger",
            "deployment.Consume();",
            "ResetRace",
            "StringComparer.Ordinal.Equals(deployment.SourceRacerId, targetRacerId)",
        ):
            self.assertIn(required, source)

        for forbidden in (
            "using UnityEngine",
            "GameObject",
            "Collider",
            "Physics.",
            "Time.time",
            "new Mesh",
        ):
            self.assertNotIn(forbidden, source)

    def test_live_race_deploys_behind_vehicle_ticks_impacts_and_resets(self):
        source = DIRECTOR.read_text(encoding="utf-8")
        for required in (
            "private readonly AsphaltShardTrapRuntime asphaltShardTraps = new();",
            "DeployAsphaltShardTrap(source, raceTimeSeconds);",
            "TickAsphaltShardTraps(raceTimeSeconds)",
            "transform.forward * (float)AsphaltShardTrapRuntime.PlacementBehindVehicleMeters",
            "powerUpRuntime.TryApplyDeployedEffect(",
            "AFAREET_ASPHALT_SHARD_TRAP_DEPLOYED",
            "AFAREET_ASPHALT_SHARD_TRAP_TRIGGERED",
            "asphaltShardTraps.ResetRace();",
            "visualAsset=external:EXT-ASSET-005",
        ):
            self.assertIn(required, source)

        self.assertNotIn(
            "if (kind != PowerUpKind.AsphaltShard && kind != PowerUpKind.TrafficCurse)",
            source,
        )

    def test_independent_compile_and_runner_cover_trap_runtime(self):
        compile_project = COMPILE.read_text(encoding="utf-8")
        runner = RUNNER.read_text(encoding="utf-8")
        self.assertIn("AsphaltShardTrapRuntime.cs", compile_project)
        self.assertIn("DeployableTrapFlow();", runner)
        self.assertIn("TryApplyDeployedEffect", runner)
        self.assertIn("trap hit its source racer", runner)
        self.assertIn("one-shot trap triggered twice", runner)

    def test_visual_source_remains_external_asset_not_generated_gameplay_geometry(self):
        ledger = LEDGER.read_text(encoding="utf-8")
        self.assertIn("EXT-ASSET-005", ledger)
        self.assertIn("Asphalt Shard ground trap", ledger)
        trap_source = TRAPS.read_text(encoding="utf-8")
        self.assertNotIn("CreatePrimitive", trap_source)
        self.assertNotIn("Resources.Load", trap_source)


if __name__ == "__main__":
    unittest.main()
