import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
SENSOR_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/Vehicle/ArcadeGroundSurfaceSensor.cs"


class GroundSurfaceSensorHotPathContractTests(unittest.TestCase):
    def test_ground_probe_keeps_nonalloc_physics_and_classifies_only_on_collider_change(self):
        source = SENSOR_PATH.read_text(encoding="utf-8")
        probe_start = source.index("public void ProbeNow()")
        probe_end = source.index("public static ArcadeSurfaceKind Classify", probe_start)
        probe = source[probe_start:probe_end]

        for required in (
            "Physics.RaycastNonAlloc(",
            "var previousGroundCollider = GroundCollider;",
            "GroundCollider = found ? best.collider : null;",
            "if (found && previousGroundCollider != GroundCollider)",
            "CurrentSurface = Classify(GroundCollider);",
        ):
            self.assertIn(required, probe)

        self.assertEqual(probe.count("Classify("), 1)
        self.assertNotIn("Physics.RaycastAll", source)

    def test_surface_classification_stays_marker_first_and_fail_conservative(self):
        source = SENSOR_PATH.read_text(encoding="utf-8")
        classify_start = source.index("public static ArcadeSurfaceKind Classify")
        classify_end = source.index("private bool IsOwnCollider", classify_start)
        classify = source[classify_start:classify_end]

        self.assertIn("collider.GetComponentInParent<ArcadeSurfaceMarker>()", classify)
        self.assertIn("return marker.SurfaceKind;", classify)
        self.assertIn('Contains(objectName, "boost")', classify)
        self.assertIn('Contains(objectName, "road")', classify)
        self.assertIn("return ArcadeSurfaceKind.OffRoad;", classify)


if __name__ == "__main__":
    unittest.main()
