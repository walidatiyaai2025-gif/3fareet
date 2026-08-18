import unittest
from pathlib import Path


REPO_ROOT = Path(__file__).resolve().parents[3]
NORMALIZER = REPO_ROOT / "unity_game/Assets/Afareet/Editor/RivalProductionImportNormalizer.cs"
STACK = REPO_ROOT / "unity_game/Assets/Afareet/Editor/P1RaceVisualStackStager.cs"


class RivalImportNormalizerContractTests(unittest.TestCase):
    def test_normalizer_preserves_authored_obj_structure_without_generating_geometry(self):
        source = NORMALIZER.read_text(encoding="utf-8")
        for token in (
            "Afareet/P1/Rivals/Normalize UART-004 Imports",
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

        for forbidden in (
            "new Mesh(",
            "GameObject.CreatePrimitive",
            "PrefabUtility.SaveAsPrefabAsset",
            "AssetDatabase.CreateAsset",
        ):
            self.assertNotIn(forbidden, source)

    def test_full_stack_no_longer_depends_on_normalizer_after_unity6_flattening_was_proven(self):
        source = STACK.read_text(encoding="utf-8")
        self.assertNotIn("RivalProductionImportNormalizer.NormalizeCurrentSourcesOrThrow", source)
        self.assertNotIn("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow", source)
        self.assertIn("RivalAuthoredReviewPrefabStager.ValidateCurrentSourcesOrThrow", source)
        self.assertIn("RivalAuthoredReviewPrefabStager.StageAll", source)
        self.assertIn("rivals=authored-review-candidates", source)
        self.assertIn("productionGate=false", source)


if __name__ == "__main__":
    unittest.main()
