import unittest
from pathlib import Path

REPO_ROOT = Path(__file__).resolve().parents[3]
ARCADE = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeCarController.cs"
RIVAL_TEST = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/RivalProductionPolicyTests.cs"


class UnityEditModeBehaviorRegressionTests(unittest.TestCase):
    def test_recovery_lazily_binds_rigidbody_before_physics_reset(self):
        source = ARCADE.read_text(encoding="utf-8-sig")
        start = source.index("private void RecoverToTrack")
        end = source.index("private void ClearDriveInputs", start)
        recover = source[start:end]

        self.assertIn("if (body == null)", recover)
        self.assertIn("body = GetComponent<Rigidbody>();", recover)
        self.assertLess(
            recover.index("body = GetComponent<Rigidbody>();"),
            recover.index("body.linearVelocity = Vector3.zero;"),
        )

    def test_rival_positive_source_examples_stay_inside_production_root(self):
        source = RIVAL_TEST.read_text(encoding="utf-8-sig")
        positive_lines = [
            line.strip()
            for line in source.splitlines()
            if "IsSupportedAuthoredModelSource" in line and "Is.True" in line
        ]
        self.assertGreaterEqual(len(positive_lines), 2)
        for line in positive_lines:
            self.assertIn("/Rivals/Production/", line)


if __name__ == "__main__":
    unittest.main()
