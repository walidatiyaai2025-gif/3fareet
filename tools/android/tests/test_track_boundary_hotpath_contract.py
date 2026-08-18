import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
POLICY_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/TrackBoundaryPolicy.cs"
TEST_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Race/TrackBoundaryTests.cs"


class TrackBoundaryHotPathContractTests(unittest.TestCase):
    def test_segment_sampling_uses_one_length_sqrt_without_double_normalization(self):
        source = POLICY_PATH.read_text(encoding="utf-8")
        start = source.index("public static TrackBoundarySample Sample(")
        end = source.index("private static Vector3 Flatten", start)
        sample = source[start:end]

        self.assertIn("var inverseLength = 1f / Mathf.Sqrt(lengthSquared);", sample)
        self.assertIn("delta.z * inverseLength", sample)
        self.assertIn("-delta.x * inverseLength", sample)
        self.assertEqual(sample.count("Mathf.Sqrt("), 1)
        self.assertNotIn("delta.normalized", sample)
        self.assertNotIn("Vector3.Cross", sample)
        self.assertNotIn(".normalized", sample)

    def test_nearest_segment_and_signed_distance_semantics_remain_explicit(self):
        source = POLICY_PATH.read_text(encoding="utf-8")
        tests = TEST_PATH.read_text(encoding="utf-8")

        for required in (
            "if (distanceSquared >= bestDistanceSquared) continue;",
            "bestDistanceSquared = distanceSquared;",
            "bestSegment = i;",
            "bestProgress = progress;",
            "bestSignedLateral = Vector3.Dot(offset, right);",
        ):
            self.assertIn(required, source)

        self.assertIn("Sample_PreservesSignedLateralOrientationOnHorizontalSegment", tests)
        self.assertIn("SignedLateralDistance, Is.EqualTo(2f)", tests)
        self.assertIn("SignedLateralDistance, Is.EqualTo(-2f)", tests)


if __name__ == "__main__":
    unittest.main()
