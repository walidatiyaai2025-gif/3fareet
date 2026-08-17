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

    def test_full_stack_normalizes_rival_imports_before_rival_preflight(self):
        source = STACK.read_text(encoding="utf-8")
        normalize = source.index("RivalProductionImportNormalizer.NormalizeCurrentSourcesOrThrow")
        rival_preflight = source.index("RivalProductionSourcePreflight.ValidateCurrentSourcesOrThrow")
        first_prefab_stage = source.index("P1ProductionWorldAssetStager.StageTrackedSourcesOrThrow")
        self.assertLess(normalize, rival_preflight)
        self.assertLess(rival_preflight, first_prefab_stage)
        self.assertIn('"UART-004 deterministic rival import settings"', source)


if __name__ == "__main__":
    unittest.main()
