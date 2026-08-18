import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
DIAGNOSTIC = REPO_ROOT / "unity_game/Assets/Afareet/Scripts/World/EditorCameraVisibilityDiagnostic.cs"


class EditorCameraVisibilityDiagnosticContractTests(unittest.TestCase):
    def test_diagnostic_is_editor_only_and_non_promotional(self):
        text = DIAGNOSTIC.read_text(encoding="utf-8")

        for required in (
            "#if UNITY_EDITOR",
            'GameObject.Find("Racing Camera")',
            'GameObject.Find("PLAYER HERO — AFAREET")',
            "GeometryUtility.CalculateFrustumPlanes(camera)",
            "GeometryUtility.TestPlanesAABB(planes, renderer.bounds)",
            "AFAREET_EDITOR_CAMERA_VISIBILITY_SCAN",
            "AFAREET_EDITOR_CAMERA_VISIBLE_CULPRIT",
            "maxDimension=",
            "distance=",
            "apparent=",
            "production=false",
        ):
            self.assertIn(required, text)

        for forbidden in (
            "P1 VERIFIED",
            "Issue #90",
            "productionGate=true",
            "ownerAcceptance=true",
            "Destroy(renderer",
            "renderer.enabled = false",
        ):
            self.assertNotIn(forbidden, text)


if __name__ == "__main__":
    unittest.main()
