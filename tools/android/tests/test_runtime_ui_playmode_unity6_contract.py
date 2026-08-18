import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
PLAYMODE_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Tests/PlayMode/RuntimeUiCompositionPlayModeTests.cs"
FLOW_OVERLAY_PATH = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/UI/ProductionRaceFlowOverlay.cs"


def method_body(source: str, signature: str) -> str:
    start = source.index(signature)
    brace = source.index("{", start)
    depth = 0
    for index in range(brace, len(source)):
        value = source[index]
        if value == "{":
            depth += 1
        elif value == "}":
            depth -= 1
            if depth == 0:
                return source[brace + 1:index]
    raise AssertionError(f"Unterminated method body for {signature}")


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

    def test_flow_overlay_never_builds_results_ranking_while_results_panel_is_hidden(self):
        source = FLOW_OVERLAY_PATH.read_text(encoding="utf-8")
        presentation = method_body(source, "private void RefreshPresentation()")

        guard = presentation.index("if (mode != RaceOverlayMode.Results)")
        position_format = presentation.index("{race.Position}")

        self.assertLess(guard, position_format)
        self.assertIn("return;", presentation[guard:position_format])
        self.assertIn("resultsPanel.SetActive(mode == RaceOverlayMode.Results);", presentation)

        # Update may call RefreshPresentation every frame for input/state responsiveness,
        # but the allocation-heavy result rank itself must remain behind the Results guard.
        update = method_body(source, "private void Update()")
        self.assertNotIn("{race.Position}", update)


if __name__ == "__main__":
    unittest.main()
