import json
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
        self.assertIn("fingerprint=", text)
        self.assertIn("physicsRootPreserved=true", text)
        self.assertIn("primitive-fallback-disabled", text)

    def test_primitive_variant_creation_is_editor_only(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalVariantPass.cs")
        self.assertIn("#if UNITY_EDITOR", text)
        self.assertIn("ApplyEditorBlockoutVariant", text)
        self.assertIn("GameObject.CreatePrimitive(PrimitiveType.Cube)", text)
        self.assertIn("AFAREET_UART004_EDITOR_BLOCKOUT_RIVAL_ACTIVE", text)

    def test_android_gate_requires_preexisting_external_authored_prefabs(self):
        text = self._read("unity_game/Assets/Afareet/Editor/RivalProductionBuildGate.cs")
        self.assertNotIn("RivalProductionAssetBuilder.BuildOrThrow", text)
        self.assertIn("RivalProductionPolicy.VariantCount", text)
        self.assertIn("RivalProductionPolicy.AssetPath", text)
        self.assertIn("ValidateProductionPrefab", text)
        self.assertIn("missing-external-authored-prefab", text)
        self.assertIn("AFAREET_UART004_PRODUCTION_RIVALS_GATE_BLOCKED", text)
        self.assertIn("AFAREET_UART004_PRODUCTION_RIVALS_GATE_OK", text)
        self.assertIn("external-authored-3d", text)

    def test_rival_quality_contract_requires_surface_authoring_identity_and_external_model_source(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/RivalProductionPolicy.cs")
        for required in (
            "Art/Vehicles/Rivals/Production/PF_Rival_01_Production",
            "Art/Vehicles/Rivals/Production/PF_Rival_02_Production",
            "Art/Vehicles/Rivals/Production/PF_Rival_03_Production",
            "authoredExternalSource",
            "uv0Authored",
            "normalsAuthored",
            "textureMappedMaterials",
            "assetVersion",
            "sourceFingerprint",
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
            "IsSupportedAuthoredModelSource",
            '".fbx"',
            '".obj"',
            '".blend"',
            '".glb"',
            '".gltf"',
        ):
            self.assertIn(required, text)

    def test_tracked_design_pack_is_brief_not_production_provenance(self):
        path = REPO_ROOT / "docs/assets/01_vehicles/rival_cars_production/RIVAL_DESIGN_PROFILES.json"
        pack = json.loads(path.read_text(encoding="utf-8"))
        self.assertEqual(pack["reviewState"], "BLOCKED_PENDING_UNITY_APK_VISUAL_PROOF")
        self.assertEqual(len(pack["variants"]), 3)
        self.assertEqual(len({v["id"] for v in pack["variants"]}), 3)
        self.assertEqual(len({v["displayName"] for v in pack["variants"]}), 3)
        self.assertEqual(len(pack["lodTopology"]), 3)
        self.assertTrue(pack["productionIntent"]["preserveExistingPhysics"])
        self.assertFalse(pack["productionIntent"]["playerPrimitiveFallbackAllowed"])

        readme = self._read("docs/assets/01_vehicles/rival_cars_production/README.md")
        self.assertIn("design/LOD-budget brief", readme)
        self.assertIn("not production provenance", readme)
        self.assertIn("externally authored model", readme)
        self.assertIn("does **not** synthesize production art", readme)

    def test_code_generated_production_builder_is_not_present(self):
        self.assertFalse(
            (REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionAssetBuilder.cs").exists(),
            "UART-004 must not ship a code-generated mesh builder as production provenance",
        )

    def test_generated_rival_resources_are_ignored_not_committed_as_stale_truth(self):
        ignore = self._read(".gitignore")
        self.assertIn("unity_game/Assets/Afareet/Resources/Art/Vehicles/Rivals/Production/", ignore)


if __name__ == "__main__":
    unittest.main()
