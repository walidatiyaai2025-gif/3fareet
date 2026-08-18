import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
AI_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/AiRacer.cs"
LOOKAHEAD_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RacingLineLookahead.cs"


class AiRacingHotPathContractTests(unittest.TestCase):
    def test_ai_validates_full_path_once_during_configuration(self):
        source = AI_PATH.read_text(encoding="utf-8")
        configure_start = source.index("public void Configure(IReadOnlyList<Transform> path, int rivalIndex)")
        configure_end = source.index("public void SynchronizeNavigation", configure_start)
        configure = source[configure_start:configure_end]

        self.assertIn("if (path == null)", configure)
        self.assertIn("if (path.Count < 3)", configure)
        self.assertIn("for (var index = 0; index < path.Count; index++)", configure)
        self.assertIn("if (path[index] == null)", configure)

    def test_fixed_update_uses_prevalidated_lookahead_without_full_path_rescan(self):
        ai = AI_PATH.read_text(encoding="utf-8")
        lookahead = LOOKAHEAD_PATH.read_text(encoding="utf-8")

        fixed_start = ai.index("private void FixedUpdate()")
        fixed_end = ai.index("private void BindRaceProgressRuntime()", fixed_start)
        fixed_update = ai[fixed_start:fixed_end]

        self.assertIn(
            "RacingLineLookahead.PlanPrevalidated(waypoints, waypointIndex, speedKph)",
            fixed_update,
        )
        self.assertNotIn("RacingLineLookahead.Plan(waypoints", fixed_update)
        self.assertEqual(fixed_update.count("car.SpeedKph"), 1)

        hot_start = lookahead.index("internal static RacingLinePlan PlanPrevalidated(")
        hot_end = lookahead.index("private static void ValidatePath", hot_start)
        hot_path = lookahead[hot_start:hot_end]
        self.assertNotIn("for (var i = 0; i < waypoints.Count; i++)", hot_path)
        self.assertNotIn("ValidatePath(waypoints)", hot_path)

    def test_public_planner_keeps_fail_closed_full_path_validation(self):
        source = LOOKAHEAD_PATH.read_text(encoding="utf-8")
        public_start = source.index("public static RacingLinePlan Plan(")
        public_end = source.index("internal static RacingLinePlan PlanPrevalidated(", public_start)
        public_plan = source[public_start:public_end]

        self.assertIn("ValidatePath(waypoints);", public_plan)
        self.assertIn("return PlanPrevalidated(waypoints, targetIndex, speedKph, lookAhead);", public_plan)
        self.assertIn("for (var i = 0; i < waypoints.Count; i++)", source)
        self.assertIn("if (waypoints[i] == null)", source)


if __name__ == "__main__":
    unittest.main()
