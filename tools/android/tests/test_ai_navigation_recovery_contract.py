import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
AI_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/AiRacer.cs"
RESET_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RivalResetController.cs"
BOOTSTRAP_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Core/AfareetBootstrap.cs"
DIRECTOR_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RaceDirector.cs"
PLAYMODE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/PlayMode/AiRacerNavigationRecoveryPlayModeTests.cs"
PLAYMODE_META_PATH = Path(str(PLAYMODE_PATH) + ".meta")


class AiNavigationRecoveryContractTests(unittest.TestCase):
    def test_ai_binds_checkpoint_progress_and_reset_lifecycle(self):
        source = AI_PATH.read_text(encoding="utf-8")
        for required in (
            "private RacerCheckpointTracker checkpoints;",
            "private RivalResetController resetController;",
            "public int NavigationWaypointIndex => waypointIndex;",
            "public void SynchronizeNavigation(int nextWaypointIndex)",
            "private void OnEnable()",
            "BindRaceProgressRuntime();",
            "SynchronizeWithCheckpointProgress();",
            "private void OnDisable()",
            "UnbindResetController();",
            "resetController.RivalReset += OnRivalReset;",
            "resetController.RivalReset -= OnRivalReset;",
            "SynchronizeNavigation(checkpoints.ExpectedCheckpointIndex);",
            "SynchronizeNavigation(resetWaypointIndex + 1);",
            "private static int Wrap(int index, int count)",
        ):
            self.assertIn(required, source)

    def test_reset_event_is_emitted_after_physical_recovery_state_is_applied(self):
        source = RESET_PATH.read_text(encoding="utf-8")
        method_start = source.index("private void ResetToLastAcceptedWaypoint()")
        method_end = source.index("private void CacheRuntimeComponents()")
        body = source[method_start:method_end]

        event_index = body.index("RivalReset?.Invoke(index);")
        for required in (
            "transform.SetPositionAndRotation",
            "body.linearVelocity = Vector3.zero;",
            "body.angularVelocity = Vector3.zero;",
            "car.SetAiInput(0f, 0f, false, false);",
            "LastResetWaypointIndex = index;",
            "ResetCount++;",
        ):
            self.assertIn(required, body)
            self.assertLess(body.index(required), event_index, required)

    def test_runtime_creation_order_is_covered_by_disable_enable_binding(self):
        bootstrap = BOOTSTRAP_PATH.read_text(encoding="utf-8")
        director = DIRECTOR_PATH.read_text(encoding="utf-8")

        ai_create = bootstrap.index("ai.gameObject.AddComponent<AiRacer>().Configure(track.Waypoints, i);")
        register = bootstrap.index("race.RegisterRival(ai);")
        self.assertLess(ai_create, register)

        for required in (
            "if (ai != null) ai.enabled = false;",
            "if (ai != null) ai.enabled = true;",
        ):
            self.assertIn(required, director)

    def test_playmode_regression_covers_restart_and_recovery_and_has_metadata(self):
        source = PLAYMODE_PATH.read_text(encoding="utf-8")
        for required in (
            "ReenableAfterProgressReset_UsesFreshExpectedCheckpoint",
            "RivalRecovery_ResynchronizesAiToNextExpectedCheckpoint",
            "checkpoints.ResetProgress(1);",
            "reset.Evaluate(0f, .11f)",
            "ai.NavigationWaypointIndex",
        ):
            self.assertIn(required, source)
        self.assertTrue(PLAYMODE_META_PATH.is_file())
        self.assertIn("fileFormatVersion: 2", PLAYMODE_META_PATH.read_text(encoding="utf-8"))


if __name__ == "__main__":
    unittest.main()
