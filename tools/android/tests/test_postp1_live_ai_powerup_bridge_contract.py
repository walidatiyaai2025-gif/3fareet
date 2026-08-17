import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1LiveAiPowerUpBridgeContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_ai_racer_has_explicit_binding_and_no_global_lookup(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/AiRacer.cs")

        for required in (
            "BindPowerUpRuntime(RaceDirector director, string racerId)",
            "PowerUpRaceRuntime.ValidateRacerId",
            "EvaluateBoundPowerUpDecision()",
            "powerUpDirector.ExecuteBoundAiPowerUp(powerUpRacerId)",
            "HasPowerUpRuntimeBinding",
        ):
            self.assertIn(required, source)

        for forbidden in (
            "FindObjectOfType",
            "FindFirstObjectByType",
            "FindAnyObjectByType",
            "GameObject.Find(",
        ):
            self.assertNotIn(forbidden, source)

    def test_race_director_owns_runtime_stable_cadence_and_scoped_presentation(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        for required in (
            "private const double AiPowerUpDecisionCadenceSeconds = .5d;",
            "private PowerUpRaceRuntime powerUpRuntime;",
            "private bool powerUpRuntimeDirty = true;",
            "EnsurePowerUpRuntime();",
            "PowerUpRuntimeDefaults.CreatePrototypeRuleset()",
            "var racerId = racers[i].RacerId;",
            "new PowerUpRacerRegistration(",
            "PowerUpPresentationHub.CreateSink(racerId)",
            "ai.BindPowerUpRuntime(this, racers[i].RacerId);",
            "private void FixedUpdate()",
            "powerUpRuntime.TickAll(raceTimeSeconds);",
            "nextPowerUpDecisionRaceTime = raceTimeSeconds + AiPowerUpDecisionCadenceSeconds;",
            "for (var i = 1; i < racers.Count; i++)",
            "ai.EvaluateBoundPowerUpDecision();",
        ):
            self.assertIn(required, source)

        self.assertNotIn(
            "registrations.Add(new PowerUpRacerRegistration(racers[i].RacerId));",
            source,
        )

    def test_live_snapshot_uses_ranked_adjacent_racers_and_runtime_execution(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        for required in (
            "var ranked = BuildRankedRace();",
            "ranked[rankedIndex - 1].Progress.RacerId",
            "ranked[rankedIndex + 1].Progress.RacerId",
            "AiPowerUpLiveSnapshotBuilder.Build(",
            "acceptedCheckpoints: source.Checkpoints.AcceptedCount",
            "checkpointCount: track.Waypoints.Count",
            "segmentProgress: SegmentProgress(source)",
            "targetDistanceMeters:",
            "chaserDistanceMeters:",
            "return powerUpRuntime.ExecuteAiDecision(",
            "targetAhead?.RacerId",
            "chaserBehind?.RacerId",
        ):
            self.assertIn(required, source)

    def test_restart_and_roster_changes_fail_closed_and_rebuild(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        self.assertIn("powerUpRuntimeDirty = true;", source)
        self.assertIn("if (powerUpRuntime != null && !powerUpRuntimeDirty)", source)
        self.assertIn("if (powerUpRuntime != null)\n                powerUpRuntime.ResetRace();", source)
        self.assertIn("nextPowerUpDecisionRaceTime = 0d;", source)
        self.assertIn("powerUpRuntime == null || powerUpRuntimeDirty", source)

    def test_prototype_defaults_and_snapshot_builder_are_unity_free(self):
        defaults = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRuntimeDefaults.cs")
        builder = self._read("unity_game/Assets/Afareet/Scripts/Race/AiPowerUpLiveSnapshotBuilder.cs")

        for required in (
            "CreatePrototypeRuleset()",
            "PowerUpKind.AsphaltShard",
            "PowerUpKind.NitroSpirit",
            "PowerUpKind.TrafficCurse",
            "PowerUpKind.EnchantedPound",
            "PowerUpKind.EyeShield",
            "Prototype tuning only",
        ):
            self.assertIn(required, defaults)

        for required in (
            "UnknownRemainingRaceSeconds = 9999d",
            "MinimumGapReferenceSpeedKph = 28.8d",
            "EstimateGapSeconds",
            "EstimateRemainingRaceSeconds",
            "new AiPowerUpRaceSnapshot(",
        ):
            self.assertIn(required, builder)

        self.assertNotIn("using UnityEngine;", defaults)
        self.assertNotIn("using UnityEngine;", builder)

    def test_independent_compile_and_behavior_runner_cover_new_pure_sources(self):
        compile_project = self._read("tools/android/contracts/PowerUpRuntimeCompile.csproj")
        runner_project = self._read("tools/android/contracts/LiveAiPowerUpBridgeContractRunner.csproj")
        runner = self._read("tools/android/contracts/LiveAiPowerUpBridgeContractRunner.cs")

        self.assertIn("PowerUpRuntimeDefaults.cs", compile_project)
        self.assertIn("AiPowerUpLiveSnapshotBuilder.cs", compile_project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn("<BaseIntermediateOutputPath>obj/LiveAiPowerUpBridgeContractRunner/</BaseIntermediateOutputPath>", runner_project)
        self.assertIn("<BaseOutputPath>bin/LiveAiPowerUpBridgeContractRunner/</BaseOutputPath>", runner_project)
        self.assertNotIn("<ProjectReference", runner_project)
        for source_name in (
            "PowerUpPresentationHooks.cs",
            "PowerUpEffectState.cs",
            "AiPowerUpUsagePolicy.cs",
            "PowerUpRaceRuntime.cs",
            "PowerUpRuntimeDefaults.cs",
            "AiPowerUpLiveSnapshotBuilder.cs",
        ):
            self.assertIn(source_name, runner_project)

        for required in (
            "PrototypeRulesContract();",
            "LiveSnapshotContract();",
            "EarlyRaceEstimateContract();",
            "ValidationContract();",
            "Live AI power-up bridge behavior contract: PASS",
        ):
            self.assertIn(required, runner)

    def test_unity_regression_source_covers_live_snapshot_and_defaults(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/LiveAiPowerUpBridgePolicyTests.cs")
        for required in (
            "PrototypeRuleset_CoversEveryRetainedPowerUpKind",
            "SnapshotBuilder_UsesCheckpointSegmentAndRankTelemetry",
            "SnapshotBuilder_EarlyProgressDoesNotFabricateFinalPushTime",
            "SnapshotBuilder_InvalidCheckpointTelemetryFailsClosed",
        ):
            self.assertIn(required, tests)

    def test_new_unity_sources_have_metadata(self):
        for relative in (
            "unity_game/Assets/Afareet/Scripts/Race/PowerUpRuntimeDefaults.cs.meta",
            "unity_game/Assets/Afareet/Scripts/Race/AiPowerUpLiveSnapshotBuilder.cs.meta",
            "unity_game/Assets/Afareet/Tests/EditMode/Race/LiveAiPowerUpBridgePolicyTests.cs.meta",
        ):
            content = self._read(relative)
            self.assertIn("fileFormatVersion: 2", content)
            self.assertIn("guid:", content)


if __name__ == "__main__":
    unittest.main()
