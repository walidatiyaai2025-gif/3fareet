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
        self.assertIn("initialCharges: 1", enchanted)
        self.assertIn("durationSeconds: 8d", enchanted)
        self.assertIn("legacy PR #9", defaults)

    def test_race_director_captures_successful_finish_snapshot_before_results_cleanup_and_suppresses_elimination(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")

        for required in (
            "private RaceRewardSettlementSnapshot playerFinishRewardSnapshot;",
            "HasPlayerFinishRewardSnapshot",
            "SettlePlayerFinishReward(int baseRewardUnits)",
            "CapturePlayerFinishRewardSnapshot(float finishTime)",
            "powerUpRuntime.CaptureRewardSettlementSnapshot(",
            "playerFinishRewardSnapshot = null;",
            "playerFinishRewardSnapshot = playerWasEliminated",
            "? null",
            ": CapturePlayerFinishRewardSnapshot(finishTime);",
            "before a successful race finish",
        ):
            self.assertIn(required, source)

        handler_start = source.index("private void OnRoundResultsReady(float finishTime)")
        handler_end = source.index("private void OnRoundReset()", handler_start)
        handler = source[handler_start:handler_end]
        assignment = handler.index("playerFinishRewardSnapshot = playerWasEliminated")
        elimination_suppression = handler.index("? null", assignment)
        successful_capture = handler.index(": CapturePlayerFinishRewardSnapshot(finishTime);", assignment)
        drive_reset = handler.index("ResetPowerUpDriveModifiers();")
        publish = handler.index("ResultsReady?.Invoke(finishTime);")

        self.assertLess(assignment, elimination_suppression)
        self.assertLess(elimination_suppression, successful_capture)
        self.assertLess(successful_capture, drive_reset)
        self.assertLess(successful_capture, publish)

    def test_career_wallet_application_is_pure_and_rejects_phantom_or_reduced_coin_grants(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerPlayerProfileRewardApplication.cs")
        self.assertNotIn("UnityEngine", source)
        self.assertNotIn("Afareet.Race", source)
        for required in (
            "ApplyWithSettledCoins",
            "ValidateSettledCoins",
            "baseCoinsGranted == 0 && settledCoinsGranted != 0",
            "settledCoinsGranted < baseCoinsGranted",
            "nextCoins = profile.Coins + settledCoinsGranted",
            "nextSpirit = profile.Spirit + settlement.SpiritGranted",
            "settlement.UnlockedVehicleIds",
        ):
            self.assertIn(required, source)

        meta = self._read("unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerPlayerProfileRewardApplication.cs.meta")
        self.assertIn("fileFormatVersion: 2", meta)
        self.assertIn("guid:", meta)

    def test_career_session_consumes_post_race_director_results_not_raw_round_results(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerGameSession.cs")
        self.assertIn("race.ResultsReady += OnResultsReady;", source)
        self.assertIn("race.ResultsReady -= OnResultsReady;", source)
        self.assertNotIn("round.ResultsReady += OnResultsReady;", source)
        self.assertNotIn("round.ResultsReady -= OnResultsReady;", source)

        race_source = self._read("unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs")
        handler_start = race_source.index("private void OnRoundResultsReady(float finishTime)")
        handler_end = race_source.index("private void OnRoundReset()", handler_start)
        handler = race_source[handler_start:handler_end]
        snapshot = handler.index("playerFinishRewardSnapshot = playerWasEliminated")
        publish = handler.index("ResultsReady?.Invoke(finishTime);")
        self.assertLess(snapshot, publish)

    def test_career_session_applies_multiplier_only_to_successful_new_coin_claims(self):
        source = self._read("unity_game/Assets/Afareet/Scripts/CareerRuntime/CareerGameSession.cs")
        for required in (
            "public RaceRewardSettlement LastCoinRewardSettlement",
            "LastCoinRewardSettlement = null;",
            "settlement.CoinsGranted > 0 && outcome.Finished",
            "race.SettlePlayerFinishReward(settlement.CoinsGranted)",
            "LastCoinRewardSettlement.SettledRewardUnits",
            "CareerPlayerProfileRewardApplication.ApplyWithSettledCoins(",
        ):
            self.assertIn(required, source)

        handler_start = source.index("private void OnResultsReady(float finishTime)")
        handler_end = source.index("private void MarkCampaignCompleteIfNeeded()", handler_start)
        handler = source[handler_start:handler_end]
        gate = handler.index("settlement.CoinsGranted > 0 && outcome.Finished")
        settle = handler.index("race.SettlePlayerFinishReward(settlement.CoinsGranted)")
        wallet = handler.index("CareerPlayerProfileRewardApplication.ApplyWithSettledCoins")
        self.assertLess(gate, settle)
        self.assertLess(settle, wallet)

    def test_live_playmode_contract_covers_first_claim_and_replay_idempotence(self):
        tests = self._read("unity_game/Assets/Afareet/Tests/PlayMode/CareerRewardMultiplierPlayModeTests.cs")
        meta = self._read("unity_game/Assets/Afareet/Tests/PlayMode/CareerRewardMultiplierPlayModeTests.cs.meta")
        for required in (
            "EnchantedPound_DoublesFirstClaimedCoinsAndReplayGrantsNothing",
            "TryUsePlayerPowerUp(PowerUpKind.EnchantedPound)",
            "LastCoinRewardSettlement.SettledRewardUnits",
            "career.Profile.Coins, Is.EqualTo(500)",
            "career.LastSettlement.CoinsGranted, Is.Zero",
            "career.LastCoinRewardSettlement, Is.Null",
        ):
            self.assertIn(required, tests)
        self.assertIn("fileFormatVersion: 2", meta)
        self.assertIn("guid:", meta)

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

    def test_compile_and_executable_contract_cover_reward_source_and_career_wallet(self):
        runtime_project = self._read("tools/android/contracts/PowerUpRuntimeCompile.csproj")
        runner_project = self._read("tools/android/contracts/RaceRewardSettlementContractRunner.csproj")
        runner = self._read("tools/android/contracts/RaceRewardSettlementContractRunner.cs")

        self.assertIn("RaceRewardSettlement.cs", runtime_project)
        self.assertIn("<TargetFramework>net8.0</TargetFramework>", runner_project)
        for required in (
            "RaceRewardSettlement.cs",
            "CareerPlayerProfile.cs",
            "CareerPlayerProfileRewardApplication.cs",
            "CareerEventSettlement.cs",
            "ChapterOneCareerContent.cs",
        ):
            self.assertIn(required, runner_project)
        self.assertIn("<BaseIntermediateOutputPath>obj/RaceRewardSettlementContractRunner/</BaseIntermediateOutputPath>", runner_project)

        for required in (
            "LegacyParityContract();",
            "RuntimeSettlementContract();",
            "SnapshotSurvivesResetContract();",
            "CareerWalletApplicationContract();",
            "RoundingContract();",
            "InvalidAndOverflowContract();",
            "CareerPlayerProfileRewardApplication.ApplyWithSettledCoins",
            "Race reward settlement behavior contract: PASS",
        ):
            self.assertIn(required, runner)


if __name__ == "__main__":
    unittest.main()
