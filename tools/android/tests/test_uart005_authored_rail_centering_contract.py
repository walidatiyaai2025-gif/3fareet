import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PASS_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredRailCenteringPass.cs"
TRACK_BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"
BARRIER_SOURCE = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/source/SM_Prop_CairoBarrier_A.obj"


class AuthoredRailCenteringContractTests(unittest.TestCase):
    def test_authored_barrier_source_is_two_meters_long_on_local_x(self):
        source = BARRIER_SOURCE.read_text(encoding="utf-8")
        xs = []
        for raw in source.splitlines():
            line = raw.strip()
            if not line.startswith("v "):
                continue
            xs.append(float(line.split()[1]))

        self.assertTrue(xs)
        self.assertAlmostEqual(-1.0, min(xs), places=4)
        self.assertAlmostEqual(1.0, max(xs), places=4)

    def test_track_builder_supplies_segment_start_and_barrier_length_scale(self):
        builder = TRACK_BUILDER.read_text(encoding="utf-8")
        self.assertIn("var railStart = p - direction * (startExtension + SegmentOverlap * .5f);", builder)
        self.assertIn("CreateNeonRail(root, railStart + right * (RoadWidth * .56f)", builder)
        self.assertIn("CreateNeonRail(root, railStart - right * (RoadWidth * .56f)", builder)
        self.assertIn("rotation * Quaternion.Euler(0f, -90f, 0f)", builder)
        self.assertIn("Mathf.Max(.5f, length / 2f)", builder)

    def test_runtime_pass_recenters_only_long_track_rails(self):
        source = PASS_PATH.read_text(encoding="utf-8")
        for required in (
            'TrackRootName = "CAIRO NIGHT RUN // 3FAREET"',
            'BarrierName = "AUTHORED CAIRO BARRIER"',
            "AuthoredHalfLengthMeters = 1f",
            "MinimumRailScaleX = 1.25f",
            "ExpectedRaceRails = 72 * 2",
            "candidate.position += candidate.right * halfLength;",
            "Mathf.Abs(candidate.localScale.x) * AuthoredHalfLengthMeters",
            "candidate.parent.name != TrackRootName",
            "AFAREET_UART005_AUTHORED_RAIL_CENTERING_ACTIVE",
            "primitiveGeometry=false",
        ):
            self.assertIn(required, source)

        for forbidden in (
            "GameObject.CreatePrimitive",
            "new Mesh",
            "sharedMesh =",
            "renderer.enabled = false",
        ):
            self.assertNotIn(forbidden, source)


if __name__ == "__main__":
    unittest.main()
