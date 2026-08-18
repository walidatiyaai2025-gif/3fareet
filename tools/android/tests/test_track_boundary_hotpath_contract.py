import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
POLICY_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/TrackBoundaryPolicy.cs"
RUNTIME_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Race/TrackBoundaryRuntime.cs"
TEST_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/EditMode/Race/TrackBoundaryTests.cs"


class TrackBoundaryHotPathContractTests(unittest.TestCase):
    def test_segment_sampling_uses_one_length_sqrt_without_double_normalization(self):
        source = POLICY_PATH.read_text(encoding="utf-8")
        start = source.index("internal static TrackBoundarySample SamplePrevalidated(")
        end = source.index("internal static void ValidatePath", start)
        sample = source[start:end]

        self.assertIn("var inverseLength = 1f / Mathf.Sqrt(lengthSquared);", sample)
        self.assertIn("delta.z * inverseLength", sample)
        self.assertIn("-delta.x * inverseLength", sample)
        self.assertEqual(sample.count("Mathf.Sqrt("), 1)
        self.assertNotIn("delta.normalized", sample)
        self.assertNotIn("Vector3.Cross", sample)
        self.assertNotIn(".normalized", sample)

    def test_monitor_validates_track_once_then_uses_prevalidated_fixedupdate_path(self):
        policy = POLICY_PATH.read_text(encoding="utf-8")
        runtime = RUNTIME_PATH.read_text(encoding="utf-8")

        public_start = policy.index("public static TrackBoundarySample Sample(")
        public_end = policy.index("internal static TrackBoundarySample SamplePrevalidated(", public_start)
        public_sample = policy[public_start:public_end]
        self.assertIn("ValidatePath(orderedWaypoints);", public_sample)
        self.assertIn(
            "return SamplePrevalidated(orderedWaypoints, worldPosition, roadHalfWidth);",
            public_sample,
        )

        configure_start = runtime.index("public void Configure(TrackRuntime runtimeTrack, float halfWidth)")
        configure_end = runtime.index("public TrackBoundarySample Refresh()", configure_start)
        configure = runtime[configure_start:configure_end]
        self.assertIn("TrackBoundaryPolicy.ValidatePath(runtimeTrack.Waypoints);", configure)

        refresh_start = configure_end
        refresh_end = runtime.index("private void FixedUpdate()", refresh_start)
        refresh = runtime[refresh_start:refresh_end]
        self.assertIn(
            "TrackBoundaryPolicy.SamplePrevalidated(track.Waypoints, transform.position, roadHalfWidth)",
            refresh,
        )
        self.assertNotIn("TrackBoundaryPolicy.Sample(track.Waypoints", refresh)

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
