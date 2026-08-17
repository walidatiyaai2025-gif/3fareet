import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PLAYMODE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/PlayMode/RuntimeUiCompositionPlayModeTests.cs"


class RuntimeUiUnity6ContractTests(unittest.TestCase):
    def test_ui_composition_queries_use_unity6_non_obsolete_overload(self):
        source = PLAYMODE_PATH.read_text(encoding="utf-8")
        self.assertIn(
            "Object.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);",
            source,
        )
        self.assertIn("private static T[] FindActive<T>()", source)
        self.assertNotIn(
            "Object.FindObjectsByType<ProductionRaceHud>(FindObjectsSortMode.None)",
            source,
        )
        self.assertNotIn(
            "Object.FindObjectsByType<ProductionRaceControlsOverlay>(FindObjectsSortMode.None)",
            source,
        )


if __name__ == "__main__":
    unittest.main()
