import json
import math
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LAYOUT = REPO_ROOT / "unity_game/Assets/Afareet/Resources/Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1.json"
BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"
RUNTIME = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoVerticalSliceLayout.cs"
GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/CairoVerticalSliceLayoutBuildGate.cs"


class Urac011VerticalSliceLayoutContractTests(unittest.TestCase):
    def test_authored_layout_has_required_route_shape(self):
        data = json.loads(LAYOUT.read_text(encoding="utf-8"))
        self.assertEqual(1, data["schemaVersion"])
        self.assertEqual("cairo-night-vertical-slice-v1", data["layoutId"])
        self.assertEqual("AUTHORED_LAYOUT", data["authoringState"])
        self.assertTrue(data["closedLoop"])
        self.assertEqual(3, data["samplesPerControlPoint"])
        points = data["points"]
        self.assertEqual(24, len(points))
        self.assertEqual(72, len(points) * data["samplesPerControlPoint"])
        self.assertGreaterEqual(len({p["sector"] for p in points}), 6)

        xs = [float(p["position"]["x"]) for p in points]
        zs = [float(p["position"]["z"]) for p in points]
        self.assertGreaterEqual(max(xs) - min(xs), 160.0)
        self.assertGreaterEqual(max(zs) - min(zs), 90.0)

        loop_length = 0.0
        min_gap = float("inf")
        for index, point in enumerate(points):
            nxt = points[(index + 1) % len(points)]
            dx = float(nxt["position"]["x"]) - float(point["position"]["x"])
            dz = float(nxt["position"]["z"]) - float(point["position"]["z"])
            gap = math.hypot(dx, dz)
            min_gap = min(min_gap, gap)
            loop_length += gap
        self.assertGreaterEqual(min_gap, 8.0)
        self.assertGreaterEqual(loop_length, 450.0)

    def test_track_builder_uses_authored_route_and_player_disables_ellipse_fallback(self):
        text = BUILDER.read_text(encoding="utf-8")
        self.assertIn("CairoVerticalSliceLayout.TryLoadSampledPositions", text)
        self.assertIn("AFAREET_URAC011_AUTHORED_LAYOUT_ACTIVE", text)
        self.assertIn("AFAREET_URAC011_PLAYER_LAYOUT_REQUIRED", text)
        self.assertIn("ellipse-fallback-disabled", text)
        self.assertIn("AFAREET_URAC011_EDITOR_ELLIPSE_FALLBACK_ACTIVE", text)
        self.assertIn("EditorFallbackPoint", text)
        self.assertNotIn("private static Vector3 Point(float t)", text)

    def test_runtime_contract_samples_authored_control_points_to_72_segments(self):
        text = RUNTIME.read_text(encoding="utf-8")
        self.assertIn("RequiredControlPoints = 24", text)
        self.assertIn("SamplesPerControlPoint = 3", text)
        self.assertIn("RuntimeSegmentCount = RequiredControlPoints * SamplesPerControlPoint", text)
        self.assertIn("CatmullRom", text)
        self.assertIn("sector-variety", text)
        self.assertIn("layout-extents", text)
        self.assertIn("layout-length", text)

    def test_android_build_gate_is_fail_closed(self):
        text = GATE.read_text(encoding="utf-8")
        self.assertIn("BuildTarget.Android", text)
        self.assertIn("AFAREET_URAC011_VERTICAL_SLICE_GATE_BLOCKED", text)
        self.assertIn("AFAREET_URAC011_VERTICAL_SLICE_GATE_OK", text)
        self.assertIn("AUTHORED_LAYOUT", text)
        self.assertIn("RequiredRuntimeSegments = 72", text)


if __name__ == "__main__":
    unittest.main()
