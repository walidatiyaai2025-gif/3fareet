import json
import math
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
LAYOUT = REPO_ROOT / "unity_game/Assets/Afareet/Resources/Art/Tracks/CairoVerticalSlice/cairo_vertical_slice_v1.json"
BUILDER = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoTrackBuilder.cs"
RUNTIME = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/CairoVerticalSliceLayout.cs"
GATE = REPO_ROOT / "unity_game/Assets/Afareet/Editor/CairoVerticalSliceLayoutBuildGate.cs"


def _catmull_rom(p0, p1, p2, p3, t):
    t2 = t * t
    t3 = t2 * t
    return (
        0.5
        * (
            2.0 * p1[0]
            + (-p0[0] + p2[0]) * t
            + (2.0 * p0[0] - 5.0 * p1[0] + 4.0 * p2[0] - p3[0]) * t2
            + (-p0[0] + 3.0 * p1[0] - 3.0 * p2[0] + p3[0]) * t3
        ),
        0.5
        * (
            2.0 * p1[1]
            + (-p0[1] + p2[1]) * t
            + (2.0 * p0[1] - 5.0 * p1[1] + 4.0 * p2[1] - p3[1]) * t2
            + (-p0[1] + 3.0 * p1[1] - 3.0 * p2[1] + p3[1]) * t3
        ),
    )


def _sample_layout(data):
    controls = [(float(p["position"]["x"]), float(p["position"]["z"])) for p in data["points"]]
    count = len(controls)
    samples = []
    for index in range(count):
        p0 = controls[(index - 1) % count]
        p1 = controls[index]
        p2 = controls[(index + 1) % count]
        p3 = controls[(index + 2) % count]
        for sample in range(int(data["samplesPerControlPoint"])):
            samples.append(_catmull_rom(p0, p1, p2, p3, sample / float(data["samplesPerControlPoint"])))
    return samples


def _orientation(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def _segments_cross(a, b, c, d):
    return (_orientation(a, b, c) * _orientation(a, b, d) < 0.0) and (
        _orientation(c, d, a) * _orientation(c, d, b) < 0.0
    )


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

    def test_sampled_route_is_non_self_intersecting_and_miter_safe(self):
        data = json.loads(LAYOUT.read_text(encoding="utf-8"))
        samples = _sample_layout(data)
        self.assertEqual(72, len(samples))

        minimum_spacing = float("inf")
        maximum_heading_delta = 0.0
        for index, current in enumerate(samples):
            previous = samples[(index - 1) % len(samples)]
            nxt = samples[(index + 1) % len(samples)]
            incoming = (current[0] - previous[0], current[1] - previous[1])
            outgoing = (nxt[0] - current[0], nxt[1] - current[1])
            incoming_length = math.hypot(*incoming)
            outgoing_length = math.hypot(*outgoing)
            minimum_spacing = min(minimum_spacing, outgoing_length)
            cosine = max(
                -1.0,
                min(
                    1.0,
                    (incoming[0] * outgoing[0] + incoming[1] * outgoing[1])
                    / (incoming_length * outgoing_length),
                ),
            )
            maximum_heading_delta = max(maximum_heading_delta, math.degrees(math.acos(cosine)))

        self.assertGreaterEqual(minimum_spacing, 4.0)
        self.assertLessEqual(maximum_heading_delta, 55.0)

        segment_count = len(samples)
        for first in range(segment_count):
            a = samples[first]
            b = samples[(first + 1) % segment_count]
            for second in range(first + 1, segment_count):
                separation = min((second - first) % segment_count, (first - second) % segment_count)
                if separation <= 1:
                    continue
                c = samples[second]
                d = samples[(second + 1) % segment_count]
                self.assertFalse(
                    _segments_cross(a, b, c, d),
                    f"sampled centerline self-intersection between segments {first} and {second}",
                )

    def test_track_builder_uses_authored_route_and_player_disables_ellipse_fallback(self):
        text = BUILDER.read_text(encoding="utf-8")
        self.assertIn("CairoVerticalSliceLayout.TryLoadSampledPositions", text)
        self.assertIn("AFAREET_URAC011_AUTHORED_LAYOUT_ACTIVE", text)
        self.assertIn("AFAREET_URAC011_PLAYER_LAYOUT_REQUIRED", text)
        self.assertIn("ellipse-fallback-disabled", text)
        self.assertIn("AFAREET_URAC011_EDITOR_ELLIPSE_FALLBACK_ACTIVE", text)
        self.assertIn("EditorFallbackPoint", text)
        self.assertNotIn("private static Vector3 Point(float t)", text)

    def test_track_segments_use_miter_join_extension_and_forward_authored_rails(self):
        text = BUILDER.read_text(encoding="utf-8")
        self.assertIn("RoadJoinHalfWidth", text)
        self.assertIn("MiterExtension(incomingDirection, direction, RoadJoinHalfWidth)", text)
        self.assertIn("MiterExtension(direction, outgoingDirection, RoadJoinHalfWidth)", text)
        self.assertIn("startExtension + endExtension + SegmentOverlap", text)
        self.assertIn("railStart", text)
        self.assertIn("Quaternion.Euler(0f, -90f, 0f)", text)
        self.assertIn("AFAREET_URAC011_MITER_JOINS_ACTIVE", text)

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
