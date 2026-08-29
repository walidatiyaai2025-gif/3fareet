import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
LOOKAHEAD_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/RacingLineLookahead.cs"
AI_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/AiRacer.cs"

class RacingLineHotPathContractTests(unittest.TestCase):
    def test_public_plan_validates_then_delegates_to_prevalidated_plan(self):
        source = LOOKAHEAD_PATH.read_text(encoding="utf-8")
        public_start = source.index("public static RacingLinePlan Plan(")
        hot_start = source.index("internal static RacingLinePlan PlanPrevalidated(", public_start)
        public_plan = source[public_start:hot_start]
        self.assertIn("ValidatePath(waypoints);", public_plan)
        self.assertIn("return PlanPrevalidated(waypoints, targetIndex, speedKph, lookAhead);", public_plan)

    def test_hot_plan_does_not_rescan_every_waypoint_for_nulls(self):
        source = LOOKAHEAD_PATH.read_text(encoding="utf-8")
        hot_start = source.index("internal static RacingLinePlan PlanPrevalidated(")
        validate_start = source.index("internal static void ValidatePath", hot_start)
        hot_plan = source[hot_start:validate_start]
        validate = source[validate_start:]
        self.assertIn("if (waypoints == null)", hot_plan)
        self.assertIn("if (waypoints.Count < 3)", hot_plan)
        self.assertNotIn("for (var i = 0; i < waypoints.Count; i++)", hot_plan)
        self.assertNotIn("Waypoint {i} is null", hot_plan)
        self.assertIn("for (var i = 0; i < waypoints.Count; i++)", validate)
        self.assertIn("Waypoint {i} is null", validate)

    def test_ai_validates_path_once_in_configure_and_uses_hot_plan_in_fixedupdate(self):
        source = AI_PATH.read_text(encoding="utf-8")
        configure_start = source.index("public void Configure(IReadOnlyList<Transform> path, int rivalIndex)")
        sync_start = source.index("public void SynchronizeNavigation", configure_start)
        configure = source[configure_start:sync_start]
        fixed_start = source.index("private void FixedUpdate()")
        bind_start = source.index("private void BindRaceProgressRuntime()", fixed_start)
        fixed = source[fixed_start:bind_start]
        self.assertIn("RacingLineLookahead.ValidatePath(path);", configure)
        self.assertIn("waypoints = path;", configure)
        self.assertNotIn("for (var index = 0; index < path.Count; index++)", configure)
        self.assertIn("RacingLineLookahead.PlanPrevalidated(", fixed)
        self.assertNotIn("RacingLineLookahead.Plan(waypoints", fixed)

    def test_hot_plan_preserves_corner_speed_and_nitro_decision_pipeline(self):
        source = LOOKAHEAD_PATH.read_text(encoding="utf-8")
        hot_start = source.index("internal static RacingLinePlan PlanPrevalidated(")
        validate_start = source.index("internal static void ValidatePath", hot_start)
        hot_plan = source[hot_start:validate_start]
        for required in (
            "CornerSpeedPolicy.Severity(previous, current, next)",
            "CornerSpeedPolicy.Plan(severity, speedKph)",
            "var aimOffset = severity < .2f ? 2 : severity < .55f ? 1 : 0;",
            "var nitro = severity < .18f && speedPlan.Brake01 < .05f;",
            "return new RacingLinePlan(aimIndex, speedPlan, nitro);",
        ):
            self.assertIn(required, hot_plan)

if __name__ == "__main__":
    unittest.main()
