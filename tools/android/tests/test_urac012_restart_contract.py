import unittest
from pathlib import Path


REPO = Path(__file__).resolve().parents[3]
DIRECTOR = REPO / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Race" / "RaceDirector.cs"
ROUND = REPO / "unity_game" / "Assets" / "Afareet" / "Scripts" / "Race" / "RaceRoundController.cs"
FLOW_OVERLAY = REPO / "unity_game" / "Assets" / "Afareet" / "Scripts" / "UI" / "ProductionRaceFlowOverlay.cs"
PLAYMODE = REPO / "unity_game" / "Assets" / "Afareet" / "Tests" / "PlayMode" / "RaceStartPlayModeTests.cs"


class Urac012RestartContractTests(unittest.TestCase):
    def test_restart_transaction_resets_round_rivals_grid_then_countdown(self):
        text = DIRECTOR.read_text(encoding="utf-8")
        start = text.index("public bool RestartRace()")
        end = text.index("private void PrepareRacer", start)
        block = text[start:end]

        required = [
            "Phase != RaceRoundPhase.Results",
            "round.RestartRound();",
            "racers[i].Lap.Configure(track.Waypoints.Count);",
            "racersReleased = false;",
            "FreezeRacers();",
            "ResetRacersToGrid();",
            "StartRace();",
        ]
        for token in required:
            self.assertIn(token, block)

        self.assertLess(block.index("round.RestartRound();"), block.index("racers[i].Lap.Configure(track.Waypoints.Count);"))
        self.assertLess(block.index("racers[i].Lap.Configure(track.Waypoints.Count);"), block.index("ResetRacersToGrid();"))
        self.assertLess(block.index("ResetRacersToGrid();"), block.index("StartRace();"))

    def test_round_restart_reconfigures_player_lap_before_reset_event(self):
        text = ROUND.read_text(encoding="utf-8")
        start = text.index("public void RestartRound()")
        end = text.index("private void Update()", start)
        block = text[start:end]

        self.assertIn("flow.Restart();", block)
        self.assertIn("lapTracker.Configure(checkpointCount);", block)
        self.assertIn("RoundReset?.Invoke();", block)
        self.assertLess(block.index("lapTracker.Configure(checkpointCount);"), block.index("RoundReset?.Invoke();"))

    def test_production_results_retry_uses_same_authoritative_restart_transaction(self):
        text = FLOW_OVERLAY.read_text(encoding="utf-8")
        restart_start = text.index("private void RestartRace()")
        refresh_start = text.index("private void RefreshPresentation()", restart_start)
        restart_block = text[restart_start:refresh_start]

        self.assertIn("career.RestartCurrentEvent();", restart_block)
        self.assertIn("race.RestartRace();", restart_block)
        self.assertIn("RaceUiPresentationPolicy.CanRestart(race.Phase)", text)
        self.assertIn('restartLabel.text = "RETRY";', text)

    def test_production_next_event_is_gated_by_completed_current_event(self):
        text = FLOW_OVERLAY.read_text(encoding="utf-8")
        start = text.index("private bool CanAdvanceCareer()")
        block = text[start:]

        for token in (
            "race.Phase == RaceRoundPhase.Results",
            "career.ActiveDefinition != null",
            "career.Progress.IsNodeCompleted(career.ActiveDefinition.Node.Id)",
            "!career.CampaignComplete",
        ):
            self.assertIn(token, block)
        self.assertIn("career.TryAdvanceToNextEvent();", text)

    def test_playmode_regression_finishes_partial_rival_then_restarts_fresh_round(self):
        text = PLAYMODE.read_text(encoding="utf-8")
        start = text.index("RestartRace_AfterResultsResetsEveryRacerAndRunsFreshCountdown")
        end = text.index("private TrackRuntime BuildTrack", start)
        block = text[start:end]

        for token in (
            "rivalCheckpoints.TryPassCheckpoint(1)",
            "rivalCheckpoints.TryPassCheckpoint(2)",
            "playerCheckpoints.TryPassCheckpoint(3)",
            "playerCheckpoints.TryPassCheckpoint(0)",
            "RaceRoundPhase.Results",
            "director.RestartRace()",
            "RaceRoundPhase.Countdown",
            "playerCheckpoints.ExpectedCheckpointIndex, Is.EqualTo(1)",
            "rivalCheckpoints.ExpectedCheckpointIndex, Is.EqualTo(1)",
            "playerCheckpoints.AcceptedCount, Is.Zero",
            "rivalCheckpoints.AcceptedCount, Is.Zero",
            "track.GridPosition(0)",
            "track.GridPosition(1)",
            "OneLapRacePhase.Ready",
            "OneLapRacePhase.Racing",
            "playerBody.isKinematic, Is.True",
            "rivalAi.enabled, Is.False",
            "rivalAi.enabled, Is.True",
        ):
            self.assertIn(token, block)

    def test_restart_contract_does_not_claim_device_verification(self):
        text = PLAYMODE.read_text(encoding="utf-8")
        self.assertNotIn("VERIFIED", text)
        self.assertNotIn("publicationEligible", text)


if __name__ == "__main__":
    unittest.main()
