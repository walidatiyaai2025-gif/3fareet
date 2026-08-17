import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class AuthoredEditorTexturePreservationContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def _assert_preservation_contract(self, text: str, marker: str):
        for required in (
            "WouldDiscardAuthoredTexture",
            "RendererHasAssignedTexture",
            "MaterialHasAssignedTexture",
            "material.GetTexturePropertyNames()",
            "material.GetTexture(propertyName)",
            marker,
            "previewOverrideSkipped=true",
            "player-preserves-source-materials=true",
        ):
            self.assertIn(required, text)
        self.assertNotIn("material.mainTexture", text)

    def test_uart006_landmarks_keep_authored_textures_during_editor_visual_review(self):
        text = self._read(
            "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredLandmarkKit.cs"
        )
        self._assert_preservation_contract(
            text, "AFAREET_UART006_EDITOR_SOURCE_TEXTURE_PRESERVED"
        )
        self.assertLess(
            text.index("WouldDiscardAuthoredTexture(renderer, selected)"),
            text.index("renderer.sharedMaterials = bindings"),
        )

    def test_uart007_track_dressing_keeps_authored_textures_during_editor_visual_review(self):
        text = self._read(
            "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredTrackDressing.cs"
        )
        self._assert_preservation_contract(
            text, "AFAREET_UART007_EDITOR_SOURCE_TEXTURE_PRESERVED"
        )
        self.assertLess(
            text.index("WouldDiscardAuthoredTexture(renderer, selected)"),
            text.index("renderer.sharedMaterials = materials"),
        )


if __name__ == "__main__":
    unittest.main()
