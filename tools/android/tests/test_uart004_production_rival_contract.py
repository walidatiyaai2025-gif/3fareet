import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uart004ProductionRivalContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_player_runtime_uses_authored_rival_resources(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs")
        self.assertIn("RivalProductionPolicy.ResourcePath", text)
        self.assertIn("ValidateProductionPrefab", text)
        self.assertIn("AFAREET_UART004_AUTHORED_RIVAL_ACTIVE", text)
        self.assertIn("primitive-fallback-disabled", text)

    def test_primitive_variant_creation_is_editor_only(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs")
        self.assertIn("#if UNITY_EDITOR", text)
        self.assertIn("ApplyEditorBlockoutVariant", text)
        self.assertIn("GameObject.CreatePrimitive(PrimitiveType.Cube)", text)
        self.assertIn("AFAREET_UART004_EDITOR_BLOCKOUT_RIVAL_ACTIVE", text)

    def test_android_gate_requires_all_three_authored_prefabs(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionBuildGate.cs")
        self.assertIn("RivalProductionPolicy.VariantCount", text)
        self.assertIn("RivalProductionPolicy.AssetPath", text)
        self.assertIn("ValidateProductionPrefab", text)
        self.assertIn("AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED", text)
        self.assertIn("AFAREET_UART004_PRODUCTION_RIVALS_GATE_OK", text)

    def test_rival_quality_contract_requires_surface_authoring(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs")
        for required in (
            "Art/Vehicles/Rivals/Production/PF_Rival_01_Production",
            "Art/Vehicles/Rivals/Production/PF_Rival_02_Production",
            "Art/Vehicles/Rivals/Production/PF_Rival_03_Production",
            "authoredExternalSource",
            "uv0Authored",
            "normalsAuthored",
            "textureMappedMaterials",
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
        ):
            self.assertIn(required, text)


if __name__ == "__main__":
    unittest.main()
