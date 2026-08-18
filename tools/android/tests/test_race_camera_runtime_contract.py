import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class RaceCameraRuntimeContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_bootstrap_normalizes_legacy_camera_listener_and_directional_light(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Core/AfareetBootstrap.cs")
        for required in (
            'new GameObject("Racing Camera")',
            'existingCamera.gameObject.name != "Main Camera"',
            "existingCamera.enabled = false",
            "FindObjectsByType<AudioListener>",
            "listener.enabled = false",
            "cameraObject.AddComponent<AudioListener>()",
            "activeListener.enabled = true",
            "AFAREET_RACE_CAMERA_NORMALIZED",
            "enabledAudioListeners=1",
            'light.gameObject.name != "Directional Light"',
            "light.enabled = false",
            'new GameObject("Moon Light")',
            "AFAREET_NIGHT_LIGHTING_NORMALIZED",
        ):
            self.assertIn(required, text)

    def test_chase_camera_avoids_world_occlusion_without_allocating_per_frame(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ChaseCamera.cs")
        for required in (
            "RaycastHit[] occlusionHits",
            "Physics.SphereCastNonAlloc",
            "QueryTriggerInteraction.Ignore",
            "ResolveOcclusion",
            "hit.transform.IsChildOf(Target)",
            "config.collisionRadius",
            "config.collisionPadding",
            "config.minimumOcclusionDistance",
            "Target.position + Vector3.up * config.lookHeight",
        ):
            self.assertIn(required, text)
        for forbidden in (
            "Physics.SphereCastAll",
            "Physics.RaycastAll",
            "new RaycastHit[OcclusionHitCapacity]",  # must remain a field initializer, not LateUpdate allocation
        ):
            if forbidden == "new RaycastHit[OcclusionHitCapacity]":
                self.assertEqual(text.count(forbidden), 1)
            else:
                self.assertNotIn(forbidden, text)

    def test_chase_camera_enforces_visual_body_clearance_before_occlusion_compression(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ChaseCamera.cs")
        for required in (
            "MinimumPlayableBodyClearance = 2.8f",
            "MinimumBodyClearanceDistance",
            "CalculateMinimumBodyClearance",
            "TryCalculateCombinedBounds",
            "renderer-bounds",
            "collider-bounds",
            "effectiveMinimumDistance = Mathf.Max",
            "minimumBodyClearanceDistance",
            "AFAREET_CAMERA_BODY_CLEARANCE_ACTIVE",
            "postPassMayNotCompress=true",
        ):
            self.assertIn(required, text)

        self.assertIn("renderer is TrailRenderer || renderer is LineRenderer", text)
        self.assertNotIn("GetComponentsInChildren<Renderer>(true);\n            var", text)

    def test_obstruction_post_pass_cannot_recompress_camera_inside_hero(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/CameraObstructionPass.cs")
        for required in (
            "[DefaultExecutionOrder(1000)]",
            "chase.FocusPoint",
            "chase.MinimumBodyClearanceDistance",
            "distance >= minimumDistance - .001f",
            "targetCamera.transform.position = focus + direction * minimumDistance;",
            "AFAREET_CAMERA_BODY_CLEARANCE_RECOVERED",
            "postPassClamp=true",
            "secondOcclusionSolve=false",
        ):
            self.assertIn(required, text)

        for forbidden in (
            "Physics.SphereCastAll",
            "Physics.SphereCastNonAlloc",
            "Physics.RaycastAll",
            "new RaycastHit",
        ):
            self.assertNotIn(forbidden, text)

    def test_chase_camera_config_serializes_visual_review_tuning(self):
        script = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/ChaseCameraConfig.cs")
        asset = self._read("unity_game/Assets/Afareet/Resources/Config/ChaseCameraConfig.asset")
        for required in (
            "lookHeight",
            "collisionRadius",
            "collisionPadding",
            "minimumOcclusionDistance",
        ):
            self.assertIn(required, script)
            self.assertIn(required, asset)

        for expected in (
            "offset: {x: 0, y: 3.25, z: -7.2}",
            "lookAheadMeters: 6",
            "normalFieldOfView: 68",
            "collisionRadius: 0.35",
        ):
            self.assertIn(expected, asset)


if __name__ == "__main__":
    unittest.main()
