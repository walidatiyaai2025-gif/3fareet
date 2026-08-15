import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]


class Uart003ProductionHeroContractTests(unittest.TestCase):
    def _read(self, relative: str) -> str:
        return (REPO_ROOT / relative).read_text(encoding="utf-8")

    def test_android_build_never_generates_preview_as_production(self):
        text = self._read("unity_game/Assets/Afareet/Editor/HeroCarProductionBuildPreprocessor.cs")
        self.assertNotIn("HeroCarProductionAssetBuilder.BuildOrThrow", text)
        self.assertIn("HeroCarLodPolicy.ProductionAssetPath", text)
        self.assertIn("AFAREET_UART003_PRODUCTION_GATE_BLOCKED", text)
        self.assertIn("ValidateProductionPrefab", text)
        self.assertIn("ValidateExternalSourceProvenanceOrThrow", text)

    def test_android_gate_verifies_real_source_guid_hash_and_mesh_backing(self):
        text = self._read("unity_game/Assets/Afareet/Editor/HeroCarProductionBuildPreprocessor.cs")
        for required in (
            "AssetDatabase.AssetPathToGUID",
            "AssetDatabase.GetAssetDependencyHash",
            "AssetDatabase.GetAssetPath(filter.sharedMesh)",
            "source-guid-mismatch",
            "source-dependency-hash-mismatch",
            "mesh-not-backed-by-source",
            "generated-source-is-not-production",
        ):
            self.assertIn(required, text)

    def test_production_metadata_requires_supported_external_3d_source(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionAssetMetadata.cs")
        for required in (
            "authoredExternalSource",
            "sourceAssetId",
            "assetVersion",
            "sourceGuid",
            "sourceDependencyHash",
            "IsSupportedExternalModelSource",
            '".fbx"',
            '".obj"',
            '".blend"',
            '".glb"',
            '".gltf"',
        ):
            self.assertIn(required, text)

    def test_source_binder_only_binds_meshes_backed_by_selected_model(self):
        text = self._read("unity_game/Assets/Afareet/Editor/HeroCarProductionSourceBinder.cs")
        for required in (
            "Bind UART-003 Production Hero Source",
            "HeroCarLodPolicy.ProductionAssetPath",
            "IsSupportedExternalModelSource",
            "AssetDatabase.GetAssetPath(filter.sharedMesh)",
            "AssetDatabase.AssetPathToGUID",
            "AssetDatabase.GetAssetDependencyHash",
            "metadata.Configure",
            "AFAREET_UART003_SOURCE_BIND_OK",
        ):
            self.assertIn(required, text)
        self.assertNotIn("HeroCarProductionAssetBuilder.BuildOrThrow", text)

    def test_production_and_generated_preview_resource_paths_are_distinct(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarLodPolicy.cs")
        self.assertIn('ProductionResourcePath = "Art/Vehicles/HeroCar/Production/', text)
        self.assertIn('GeneratedPreviewResourcePath = "Art/Vehicles/HeroCar/Generated/', text)
        self.assertIn("ResourcePath = ProductionResourcePath", text)
        self.assertNotIn(
            'ResourcePath = "Art/Vehicles/HeroCar/Generated/PF_Vehicle_AfareetKing_Production"',
            text,
        )

    def test_generated_v2_is_explicitly_preview_only(self):
        text = self._read("unity_game/Assets/Afareet/Editor/HeroCarProductionAssetBuilder.cs")
        self.assertIn("PF_Vehicle_AfareetKing_PreviewV2.prefab", text)
        self.assertIn("AFAREET_HERO_GENERATED_PREVIEW_V2_BUILD_OK", text)
        self.assertIn("production=false", text)
        self.assertNotIn('PF_Vehicle_AfareetKing_Production.prefab";', text)
        self.assertNotIn("root.AddComponent<HeroCarProductionVisual>()", text)

    def test_runtime_production_validation_requires_real_surface_authoring(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionVisual.cs")
        for required in (
            "HeroCarProductionAssetMetadata",
            "DeclaresProductionAuthoring",
            "mesh.uv",
            "mesh.normals",
            "material.mainTexture",
            "HeroCarProductionQualityPolicy.MeetsProductionFloor",
            "AFAREET_HERO_AUTHORED_PRODUCTION_VISUAL_ACTIVE",
        ):
            self.assertIn(required, text)

    def test_quality_policy_rejects_geometry_only_acceptance(self):
        text = self._read("unity_game/Assets/Afareet/Scripts/Vehicle/HeroCarProductionQualityPolicy.cs")
        self.assertIn("RequireUv0 = true", text)
        self.assertIn("RequireAuthoredNormals = true", text)
        self.assertIn("RequireTextureMappedMaterial = true", text)


if __name__ == "__main__":
    unittest.main()
