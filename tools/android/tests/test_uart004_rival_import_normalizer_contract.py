import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
NORMALIZER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionImportNormalizer.cs"
REVIEW_STAGER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalAuthoredReviewPrefabStager.cs"
STACK = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"


class RivalImportNormalizerContractTests(unittest.TestCase):
    def test_normalizer_only_targets_isolated_production_sources_without_generating_geometry(self):
        source = NORMALIZER.read_text(encoding="utf-8")
        for token in (
            "Afareet/P1/Rivals/Normalize UART-004 Production Imports",
            "RivalProductionPolicy.StagingSourcePath(variant)",
            "RivalProductionPolicy.ProductionSourceRoot",
            "RivalProductionPolicy.IsSupportedAuthoredModelSource(sourcePath)",
            "reviewSourcesRejected=true",
            "ModelImporter",
            "preserveHierarchy = true",
            "optimizeMeshPolygons = false",
            "optimizeMeshVertices = false",
            "weldVertices = false",
            "importNormals = ModelImporterNormals.Import",
            "materialImportMode = ModelImporterMaterialImportMode.ImportStandard",
            "SaveAndReimport",
            "AFAREET_UART004_IMPORT_NORMALIZE_OK",
            "AFAREET_UART004_IMPORT_NORMALIZE_ALL_OK",
            "AFAREET_UART004_IMPORT_MESH",
            "AFAREET_UART004_IMPORT_TOPOLOGY",
            "geometryGenerated=false",
            "productionPromotion=false",
        ):
            self.assertIn(token, source)

        for review_source in (
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj",
        ):
            self.assertNotIn(review_source, source)

        for forbidden in (
            "new Mesh(",
            "GameObject.CreatePrimitive",
            "PrefabUtility.SaveAsPrefabAsset",
            "AssetDatabase.CreateAsset",
        ):
            self.assertNotIn(forbidden, source)

    def test_review_sources_remain_owned_by_review_stager_not_production_normalizer(self):
        production = NORMALIZER.read_text(encoding="utf-8")
        review = REVIEW_STAGER.read_text(encoding="utf-8")
        for review_source in (
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_01_WedgeCoupe.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_02_FastbackMuscle.obj",
            "Assets/Afareet/ArtSource/Vehicles/Rivals/Rival_03_CompactPrototype.obj",
        ):
            self.assertNotIn(review_source, production)
            self.assertIn(review_source, review)
        self.assertIn("productionGate=false", review)

    def test_full_stack_stays_on_review_path_until_external_production_sources_exist(self):
        source = STACK.read_text(encoding="utf-8")
        self.assertNotIn("RivalProductionImportNormalizer.NormalizeCurrentSourcesOrThrow", source)
        self.assertNotIn("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow", source)
        self.assertIn("RivalAuthoredReviewPrefabStager.ValidateCurrentSourcesOrThrow", source)
        self.assertIn("RivalAuthoredReviewPrefabStager.StageAll", source)
        self.assertIn("rivals=authored-review-candidates", source)
        self.assertIn("productionGate=false", source)


if __name__ == "__main__":
    unittest.main()
