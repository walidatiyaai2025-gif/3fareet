import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class PostP1RaceRewardSettlementContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_settlement_domain_is_unity_free_and_fail_closed(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceRewardSettlement.cs")

        for required in (
            "class RaceRewardSettlement",
            "class RaceRewardSettlementSnapshot",
            "class RaceRewardSettlementPolicy",
            "MidpointRounding.AwayFromZero",
            "throw new OverflowException",
            "CaptureRewardSettlementSnapshot(",
            "GetVehicleEffectProjection(racerId, raceTimeSeconds)",
        ):
            self.assertIn(required, source)

        self.assertNotIn("using UnityEngine;", source)

    def test_enchanted_pound_retains_legacy_double_reward_parity(self):
        defaults = self._read("unity_game/Assets/Afareet/Scripts/Race/PowerUpRuntimeDefaults.cs")
        start = defaults.index("PowerUpKind.EnchantedPound")
        end = defaults.index("PowerUpKind.EyeShield", start)
        enchanted = defaults[start:end]

        self.assertIn("magnitude: 2d", enchanted)
        self.assertIn("legacy PR #9", defaults)

    def test_race_director_captures_finish_snapshot_before_results_cleanup(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        for required in (
            "private RaceRewardSettlementSnapshot playerFinishRewardSnapshot;",
            "HasPlayerFinishRewardSnapshot",
            "SettlePlayerFinishReward(int baseRewardUnits)",
            "CapturePlayerFinishRewardSnapshot(float finishTime)",
            "powerUpRuntime.CaptureRewardSettlementSnapshot(",
            "playerFinishRewardSnapshot = null;",
        ):
            self.assertIn(required, source)

        handler_start = source.index("private void OnRoundResultsReady(float finishTime)")
        handler_end = source.index("private void OnRoundReset()", handler_start)
        handler = source[handler_start:handler_end]
        capture = handler.index("playerFinishRewardSnapshot = CapturePlayerFinishRewardSnapshot(finishTime);")
        drive_reset = handler.index("ResetPowerUpDriveModifiers();")
        publish = handler.index("ResultsReady?.Invoke(finishTime);")
        self.assertLess(capture, drive_reset)
        self.assertLess(capture, publish)

    def test_settlement_tests_and_metadata_are_present(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/RaceRewardSettlementTests.cs")
        meta = self._read("unity_game/Assets/Afareet/Tests/EditMode/Race/RaceRewardSettlementTests.cs.meta")

        for required in (
            "PrototypeEnchantedPound_RetainsLegacyDoubleRewardParity",
            "ActiveEnchantedPound_DoublesRewardAndSnapshotSurvivesRuntimeReset",
            "ExpiredEnchantedPound_ReturnsToNeutralSettlement",
            "InvalidInputAndOverflow_FailClosed",
        ):
            self.assertIn(required, tests)

        self.assertIn("fileFormatVersion: 2", meta)
        self.assertIn("guid:", meta)

    def test_compile_and_executable_contract_cover_reward_source(self):
        runtime_project = self._read("tools/android/contracts/PowerUpRuntimeCompile.csproj")
        runner_project = self._read("tools/android/contracts/RaceRewardSettlementContractRunner.csproj")
        runner = self._read("tools/android/contracts/RaceRewardSettlementContractRunner.cs")

        self.assertIn("RaceRewardSettlement.cs", runtime_project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        self.assertIn("RaceRewardSettlement.cs", runner_project)
        self.assertIn("<BaseIntermediateOutputPath>obj/RaceRewardSettlementContractRunner/</BaseIntermediateOutputPath>", runner_project)

        for required in (
            "LegacyParityContract();",
            "RuntimeSettlementContract();",
            "SnapshotSurvivesResetContract();",
            "RoundingContract();",
            "InvalidAndOverflowContract();",
            "Race reward settlement behavior contract: PASS",
        ):
            self.assertIn(required, runner)


if __name__ == "__main__":
    unittest.main()
