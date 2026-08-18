import json
import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
STATUS = REPO_ROOT / "docs/assets/02_tracks_environments/cairo_street_kit/ROADSIDE_CLUTTER_RUNTIME_STATUS.json"


class Uart005RoadsideClutterRuntimeContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_runtime_status_is_implemented_but_still_blocked_and_unverified(self):
        status = json.loads(STATUS.read_text(encoding="utf-8"))
        self.assertEqual("BLOCKED", status["reviewState"])
        self.assertEqual("3/3", status["sourceDeliveryProgress"])
        self.assertTrue(status["runtimeIntegrationImplemented"])
        self.assertFalse(status["runtimeIntegrated"])
        self.assertFalse(status["runtimeIntegrationVerified"])
        self.assertTrue(status["implementationContract"]["sourceBackedResourcesOnly"])
        self.assertFalse(status["implementationContract"]["primitiveFallbackAllowed"])
        self.assertFalse(status["implementationContract"]["codeGeneratedMeshAllowed"])

    def test_runtime_adapter_uses_only_three_tracked_resources_and_stable_selection(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/World/CairoAuthoredRoadsideClutter.cs")
        for required in (
            "SM_Prop_CairoPlanter_A",
            "SM_Prop_CairoCrateStack_A",
            "SM_Prop_CairoCafeTable_A",
            "Resources.Load<GameObject>",
            "StableVariantIndex",
            "stable-building-hash",
            "playerMaterials=source-authored",
            "primitives=false",
            "Facade Front V",
        ):
            self.assertIn(required, text)
        for forbidden in (
            "GameObject.CreatePrimitive",
            "new Mesh(",
            "new Mesh {",
            "RecalculateNormals",
            "sharedMaterial =",
            "sharedMaterials =",
        ):
            self.assertNotIn(forbidden, text)

    def test_runtime_pass_decorates_authored_buildings_once_and_retries_fail_closed(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/World/CairoRoadsideClutterRuntimePass.cs")
        for required in (
            "RuntimeInitializeOnLoadMethod",
            "AFAREET UART005 ROADSIDE CLUTTER PASS",
            "FindObjectsByType<Transform>",
            'StartsWith("AUTHORED CAIRO BUILDING"',
            "HashSet<GameObject> decoratedBuildings",
            "var candidateObject = candidate.gameObject",
            "decoratedBuildings.Contains(candidateObject)",
            "CairoAuthoredRoadsideClutter.TryDecorateBuilding",
            "decoratedBuildings.Add(candidateObject)",
        ):
            self.assertIn(required, text)
        self.assertNotIn("decoratedBuildingIds", text)
        self.assertNotIn("GetInstanceID()", text)
        self.assertNotIn("GameObject.CreatePrimitive", text)
        self.assertNotIn("new Mesh(", text)

    def test_android_gate_stages_then_checks_imported_uv_normals_and_textured_materials(self):
        text = self._read("unity_game/Assets/Afareet/Editor/P1ProductionRoadsideClutterBuildGate.cs")
        for required in (
            "IPreprocessBuildWithReport",
            "BuildTarget.Android",
            "P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow();",
            "SM_Prop_CairoPlanter_A",
            "SM_Prop_CairoCrateStack_A",
            "SM_Prop_CairoCafeTable_A",
            "Resources.Load<GameObject>",
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
            "AFAREET_UART005_ROADSIDE_CLUTTER_GATE_OK",
            "AFAREET_UART005_ROADSIDE_CLUTTER_GATE_BLOCKED",
            "BuildFailedException",
        ):
            self.assertIn(required, text)

    def test_runtime_scripts_and_gate_have_tracked_meta_files(self):
        for relative in (
            "unity_game/Assets/Afareet/Scripts/World/CairoAuthoredRoadsideClutter.cs",
            "unity_game/Assets/Afareet/Scripts/World/CairoRoadsideClutterRuntimePass.cs",
            "unity_game/Assets/Afareet/Editor/P1ProductionRoadsideClutterBuildGate.cs",
        ):
            path = REPO_ROOT / relative
            self.assertTrue(path.is_file(), path)
            self.assertTrue(path.with_suffix(path.suffix + ".meta").is_file(), path)


if __name__ == "__main__":
    unittest.main()
